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
    /// <summary>
    /// Interaction logic for SignUp.xaml
    /// </summary>
    public partial class SignUp : Window
    {
        private static readonly HttpClient client = new HttpClient();

        // Keep original endpoints intact
        private readonly string otpapi = "https://connect-api-4.onrender.com/register";
        private readonly string VerifyAPI = "https://connect-api-4.onrender.com/verify";

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
            name = Name.Text?.ToString();
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

                if (response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = "OTP sent to your Gmail. Please enter it below.";
                    ErrorTextBlock.Visibility = Visibility.Visible;

                    // Show OTP controls
                    OTPLabel.Visibility = Visibility.Visible;
                    OTPBox.Visibility = Visibility.Visible;
                    VerifyButton.Visibility = Visibility.Visible;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // The original logic set "Name already exists" on any failure; keep it
                    ErrorTextBlock.Text = "Name already exists";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = "Error: " + ex.Message;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        // Matches original signature but returns Task<HttpResponseMessage>
        private async Task<HttpResponseMessage> RegisterUserAsync(object userData)
        {
            string json = JsonConvert.SerializeObject(userData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(otpapi, content);
            return response;
        }

        // Called by "Verify OTP" button
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
                var client = new HttpClient();
                var response = await client.PostAsync(VerifyAPI, content);

                if (response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = "Verification successful! Redirecting...";
                    ErrorTextBlock.Visibility = Visibility.Visible;

                    // Persist session name (as per original code)
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        File.WriteAllText("log", name);
                    }

                    new MainWindow().Show();
                    this.Close();
                }
                else
                {
                    var resText = await response.Content.ReadAsStringAsync();
                    ErrorTextBlock.Text = "Verification Failed: " + resText;
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = "Error: " + ex.Message;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
