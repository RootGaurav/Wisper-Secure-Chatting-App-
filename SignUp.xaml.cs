using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;

namespace Connectt
{
    public partial class SignUp : Window
    {
        private static readonly HttpClient client = new HttpClient();

        // Keep original endpoints intact
        private readonly string otpapi = "http://127.0.0.1:5000/register";
        private readonly string VerifyAPI = "http://127.0.0.1:5000/verify";
        private readonly string SignInAPI = "http://127.0.0.1:5000/signin";

        // Make fields nullable to satisfy C# nullable rules; actual values are set from UI
        private static string? name = null;
        private static string? gml = null;

        // Kept from original; warning about unused can be ignored or removed if not needed
        private static int pointt = 0;

        public SignUp()
        {
            InitializeComponent();
        }

        // Close (✕) button handler from updated XAML
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Called by the "Sign up" button (same as original "GetInfo" hook)
        private void GetInfo(object sender, RoutedEventArgs e)
        {
            // Keep original x:Name controls: Name, gmail
            name = NameTextBox.Text?.ToString();
            gml = gmail.Text?.ToString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(gml))
            {
                ErrorTextBlock.Text = "Please fill in all fields.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                Register(name, gml);
            }
        }

        public async void Register(string name, string gml)
        {
            var userData = new
            {
                name = name,
                gmail = gml
            };

            try
            {
                var response = await RegisterUserAsync(userData);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                // Debug output
                Debug.WriteLine($"Response Status: {response.StatusCode}");
                Debug.WriteLine($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = "OTP sent to your Gmail. Please enter it below.";
                    ErrorTextBlock.Visibility = Visibility.Visible;

                    // Show OTP controls - THIS WAS THE MAIN ISSUE
                    OtpPanel.Visibility = Visibility.Visible;  // Show the entire StackPanel
                    VerifyButton.Visibility = Visibility.Visible;
                    SignUpButton.Visibility = Visibility.Collapsed; // Hide signup button
                }
                else
                {
                    // Try to parse error message from response
                    try
                    {
                        dynamic errorResponse = JsonConvert.DeserializeObject(responseContent);
                        ErrorTextBlock.Text = errorResponse?.error?.ToString() ?? "Registration failed";
                    }
                    catch
                    {
                        ErrorTextBlock.Text = "Name already exists";
                    }
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
                ErrorTextBlock.Text = "Error: " + ex.Message;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        // Matches original signature but returns Task<HttpResponseMessage>
        private async Task<HttpResponseMessage> RegisterUserAsync(object userData)
        {
            string json = JsonConvert.SerializeObject(userData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Add timeout and better error handling
            client.Timeout = TimeSpan.FromSeconds(30);
            HttpResponseMessage response = await client.PostAsync(otpapi, content);
            return response;
        }

        // Called by "Sign Up" tab button
        private void ShowSignUp_Click(object sender, RoutedEventArgs e)
        {
            SignUpForm.Visibility = Visibility.Visible;
            SignInForm.Visibility = Visibility.Collapsed;
            TabSignUp.Style = (Style)Resources["PrimaryButton"];
            TabSignIn.Style = (Style)Resources["SecondaryButton"];
        }

        // Called by "Sign In" tab button  
        private void ShowSignIn_Click(object sender, RoutedEventArgs e)
        {
            SignUpForm.Visibility = Visibility.Collapsed;
            SignInForm.Visibility = Visibility.Visible;
            TabSignUp.Style = (Style)Resources["SecondaryButton"];
            TabSignIn.Style = (Style)Resources["PrimaryButton"];
        }

        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = SignInUsernameTextBox.Text.Trim();
            string userOTP = SignInPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userOTP))
            {
                SignInError.Text = "Enter both username and OTP";
                SignInError.Visibility = Visibility.Visible;
                return;
            }

            var payload = new
            {
                name = username,
                otp = userOTP
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(SignInAPI, content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    SignInError.Text = "Login successful! Redirecting...";
                    SignInError.Visibility = Visibility.Visible;

                    // Persist session exactly like VerifyOtp_Click
                    File.WriteAllText("log", username);
                    Session.name = username;

                    await Task.Delay(1000);

                    new MainWindow().Show();
                    this.Close();
                }
                else
                {
                    dynamic result = JsonConvert.DeserializeObject(body);
                    SignInError.Text = result?.error ?? "Sign-in failed";
                    SignInError.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                SignInError.Text = "Error: " + ex.Message;
                SignInError.Visibility = Visibility.Visible;
            }
        }



        private async void VerifyOtp_Click(object sender, RoutedEventArgs e)
        {
            // PasswordBox has Password, not Text
            string otp = OTPBox.Password.Trim();

            if (string.IsNullOrEmpty(otp))
            {
                ErrorTextBlock.Text = "Please enter OTP.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            var verifyData = new
            {
                name = name,
                otp = otp,
                gmail = gml
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(verifyData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(VerifyAPI, content);

                if (response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = "Verification successful! Redirecting...";
                    ErrorTextBlock.Visibility = Visibility.Visible;

                    // Persist session name (as per original code)
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        File.WriteAllText("log", name);
                        Session.name = name; // Set session name
                    }

                    // Small delay to show success message
                    await Task.Delay(1000);
                    
                    new MainWindow().Show();
                    this.Close();
                }
                else
                {
                    var resText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        dynamic errorResponse = JsonConvert.DeserializeObject(resText);
                        ErrorTextBlock.Text = errorResponse?.error?.ToString() ?? "Verification Failed";
                    }
                    catch
                    {
                        ErrorTextBlock.Text = "Verification Failed: Invalid OTP";
                    }
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Verify Exception: {ex.Message}");
                ErrorTextBlock.Text = "Error: " + ex.Message;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
