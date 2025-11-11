using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Connectt
{
    public partial class SignUp : Window
    {
        private static readonly HttpClient client = new HttpClient();

        // === API endpoints ===
        private readonly string otpapi = "http://127.0.0.1:5000/register";
        private readonly string VerifyAPI = "http://127.0.0.1:5000/verify";
        private readonly string SignInAPI = "http://127.0.0.1:5000/signin";
        private readonly string ForgotPasswordAPI = "http://127.0.0.1:5000/forgot_password";
        private readonly string ResetPasswordAPI = "http://127.0.0.1:5000/reset_password";

        private static string? name = null;
        private static string? gml = null;

        public SignUp()
        {
            InitializeComponent();
        }

        // === Close Window ===
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // === Utility Functions ===
        private void HideAllForms()
        {
            SignUpForm.Visibility = Visibility.Collapsed;
            SignInForm.Visibility = Visibility.Collapsed;
            ForgotPasswordPanel.Visibility = Visibility.Collapsed;
        }

        private void ClearErrors()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            SignInErrorContainer.Visibility = Visibility.Collapsed;
            ForgotErrorContainer.Visibility = Visibility.Collapsed;
        }

        private void ShowBlockMessage(TextBlock block, string msg, bool isError = true)
        {
            if (block == null) return;
            block.Text = msg;
            block.Visibility = Visibility.Visible;

            if (isError)
                block.Foreground = (Brush)FindResource("Warn");
            else
                block.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20"));
        }

        // === Navigation ===
        private void ShowSignUp_Click(object sender, RoutedEventArgs e)
        {
            HideAllForms();
            ClearErrors();
            SignUpForm.Visibility = Visibility.Visible;
            TabSignUp.Style = (Style)Resources["PrimaryButton"];
            TabSignIn.Style = (Style)Resources["SecondaryButton"];
        }

        private void ShowSignIn_Click(object sender, RoutedEventArgs e)
        {
            HideAllForms();
            ClearErrors();
            SignInForm.Visibility = Visibility.Visible;
            TabSignIn.Style = (Style)Resources["PrimaryButton"];
            TabSignUp.Style = (Style)Resources["SecondaryButton"];
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            HideAllForms();
            ClearErrors();
            ForgotPasswordPanel.Visibility = Visibility.Visible;

            ForgotUsernameBox.Text = "";
            ForgotGmailBox.Text = "";
            ResetOtpBox.Password = "";
            NewPasswordBox.Password = "";
            ForgotButtonsPanel.Visibility = Visibility.Visible;
            ResetStep2Panel.Visibility = Visibility.Collapsed;
        }

        private void BackToSignIn_Click(object sender, RoutedEventArgs e)
        {
            HideAllForms();
            ClearErrors();
            SignInForm.Visibility = Visibility.Visible;
            TabSignIn.Style = (Style)Resources["PrimaryButton"];
            TabSignUp.Style = (Style)Resources["SecondaryButton"];
        }

        // === SIGN UP ===
        private void GetInfo(object sender, RoutedEventArgs e)
        {
            name = NameTextBox.Text?.Trim();
            gml = gmail.Text?.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(gml))
            {
                ShowBlockMessage(ErrorTextBlock, "Please fill in all fields.");
                return;
            }

            Register(name, gml);
        }

        private async void Register(string name, string gml)
        {
            var userData = new { name, gmail = gml };

            try
            {
                var json = JsonConvert.SerializeObject(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(otpapi, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowBlockMessage(ErrorTextBlock, "OTP sent to your Gmail.", false);
                    OtpPanel.Visibility = Visibility.Visible;
                    VerifyButton.Visibility = Visibility.Visible;
                    SignUpButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    dynamic res = JsonConvert.DeserializeObject(responseContent);
                    ShowBlockMessage(ErrorTextBlock, res?.error?.ToString() ?? "Registration failed");
                }
            }
            catch (Exception ex)
            {
                ShowBlockMessage(ErrorTextBlock, "Error: " + ex.Message);
            }
        }

        private async void VerifyOtp_Click(object sender, RoutedEventArgs e)
        {
            string otp = OTPBox.Password.Trim();
            if (string.IsNullOrEmpty(otp))
            {
                ShowBlockMessage(ErrorTextBlock, "Please enter OTP.");
                return;
            }

            var verifyData = new { name, gmail = gml, otp };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(verifyData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(VerifyAPI, content);

                if (response.IsSuccessStatusCode)
                {
                    ShowBlockMessage(ErrorTextBlock, "Verification successful! Redirecting...", false);
                    await Task.Delay(1000);
                    BackToSignIn_Click(null, null); // ✅ Redirect to SignIn
                }
                else
                {
                    string text = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(text);
                    ShowBlockMessage(ErrorTextBlock, result?.error?.ToString() ?? "Invalid OTP");
                    ResendOtpButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowBlockMessage(ErrorTextBlock, "Error verifying: " + ex.Message);
            }
        }

        private async void ResendOtp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(gml))
            {
                ShowBlockMessage(ErrorTextBlock, "Please enter username and email first.");
                return;
            }

            ShowBlockMessage(ErrorTextBlock, "Resending OTP...", false);
            ResendOtpButton.IsEnabled = false;

            try
            {
                var userData = new { name = name, gmail = gml };
                var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(otpapi, content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowBlockMessage(ErrorTextBlock, "New OTP sent to your Gmail.", false);
                    ResendOtpButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    dynamic errorResponse = JsonConvert.DeserializeObject(responseText);
                    ShowBlockMessage(ErrorTextBlock, errorResponse?.error?.ToString() ?? "Failed to resend OTP.");
                }
            }
            catch (Exception ex)
            {
                ShowBlockMessage(ErrorTextBlock, "Error resending OTP: " + ex.Message);
            }
            finally
            {
                ResendOtpButton.IsEnabled = true;
            }
        }

        // === SIGN IN ===
        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            string username = SignInUsernameTextBox.Text.Trim();
            string password = SignInPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SignInErrorContainer.Visibility = Visibility.Visible;
                SignInError.Text = "Please enter both username and password.";
                return;
            }

            var payload = new { name = username, otp = password };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(SignInAPI, content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    SignInErrorContainer.Visibility = Visibility.Visible;
                    SignInError.Foreground = new SolidColorBrush(Colors.Green);
                    SignInError.Text = "Login successful! Redirecting...";

                    File.WriteAllText("log", username);
                    Session.name = username;


                    // 🟢 Notify server: user is online
                    await SetUserOnlineStatus(username, true);

                    await Task.Delay(1000);
                    new MainWindow().Show();
                    this.Close();
                }
                else
                {
                    dynamic result = JsonConvert.DeserializeObject(body);
                    SignInErrorContainer.Visibility = Visibility.Visible;
                    SignInError.Text = result?.error?.ToString() ?? "Invalid credentials";
                }
            }
            catch (Exception ex)
            {
                SignInErrorContainer.Visibility = Visibility.Visible;
                SignInError.Text = "Error: " + ex.Message;
            }
        }

        //online status update
        private async Task SetUserOnlineStatus(string username, bool online)
        {
            try
            {
                var payload = new { name = username, online = online };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                await client.PostAsync("http://127.0.0.1:5000/set_status", content);
            }
            catch { /* ignore minor network errors */ }
        }


        // === FORGOT PASSWORD ===
        private async void SendResetOtp_Click(object sender, RoutedEventArgs e)
        {
            string uname = ForgotUsernameBox.Text.Trim();
            string email = ForgotGmailBox.Text.Trim();

            if (string.IsNullOrEmpty(uname) || string.IsNullOrEmpty(email))
            {
                ShowBlockMessage(ForgotErrorText, "Please fill all fields.");
                ForgotErrorContainer.Visibility = Visibility.Visible;
                return;
            }

            var payload = new { name = uname, gmail = email };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ForgotPasswordAPI, content);
                string resp = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowBlockMessage(ForgotErrorText, "OTP sent! Please check your email.", false);
                    ForgotErrorContainer.Visibility = Visibility.Visible;
                    ForgotButtonsPanel.Visibility = Visibility.Collapsed;
                    ResetStep2Panel.Visibility = Visibility.Visible;
                }
                else
                {
                    dynamic result = JsonConvert.DeserializeObject(resp);
                    ShowBlockMessage(ForgotErrorText, result?.error?.ToString() ?? "Failed to send OTP.");
                    ForgotErrorContainer.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ForgotErrorContainer.Visibility = Visibility.Visible;
                ForgotErrorText.Text = "Error: " + ex.Message;
            }
        }

        private async void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            string uname = ForgotUsernameBox.Text.Trim();
            string email = ForgotGmailBox.Text.Trim();
            string otp = ResetOtpBox.Password.Trim();
            string newPass = NewPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(uname) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPass))
            {
                ShowBlockMessage(ForgotErrorText, "All fields are required.");
                ForgotErrorContainer.Visibility = Visibility.Visible;
                return;
            }

            var payload = new { name = uname, gmail = email, otp, new_password = newPass };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ResetPasswordAPI, content);
                string resp = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowBlockMessage(ForgotErrorText, "Password reset successful! Redirecting to sign in...", false);
                    ForgotErrorContainer.Visibility = Visibility.Visible;

                    await Task.Delay(1500);
                    BackToSignIn_Click(null, null);
                }
                else
                {
                    dynamic result = JsonConvert.DeserializeObject(resp);
                    ShowBlockMessage(ForgotErrorText, result?.error?.ToString() ?? "Reset failed.");
                    ForgotErrorContainer.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ForgotErrorContainer.Visibility = Visibility.Visible;
                ForgotErrorText.Text = "Error: " + ex.Message;
            }
        }

        private void gmail_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
