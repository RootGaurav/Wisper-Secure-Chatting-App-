using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows.Threading;

namespace Connectt
{
    public partial class ChatUserControl : UserControl
    {
        private string? selectedFriend;
        private DispatcherTimer messageTimer;

        private void StartMessagePolling(string friendName)
        {
            if (messageTimer != null)
            {
                messageTimer.Stop();
                messageTimer = null;
            }

            string capturedFriendName = friendName;

            messageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };

            messageTimer.Tick += async (sender, e) =>
            {
                if (capturedFriendName != selectedFriend) return;

                await ReloadMessagesSafely(capturedFriendName);
            };

            messageTimer.Start();
        }


        private HashSet<string> displayedMessages = new HashSet<string>();

        private async Task ReloadMessagesSafely(string friendName)
        {
            displayedMessages.Clear();
            MessagesPanel.Children.Clear();
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://127.0.0.1:5000/get_messages?name={Session.name}&from={friendName}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(json);

                foreach (var msg in messages)
                {
                    string decryptedMessage;
                    try
                    {
                        decryptedMessage = DecryptString(msg.message);
                    }
                    catch
                    {
                        decryptedMessage = "[Error decrypting message]";
                    }

                    string messageKey = $"{msg.sender}:{decryptedMessage}";
                    if (displayedMessages.Contains(messageKey))
                        continue;

                    displayedMessages.Add(messageKey);

                    bool isSender = msg.sender == Session.name;
                    string prefix = isSender ? "You" : friendName;

                    var border = new Border
                    {
                        Background = isSender ? Brushes.MediumSlateBlue : Brushes.Gray,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10),
                        Margin = new Thickness(5),
                        MaxWidth = 300,
                        HorizontalAlignment = isSender ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };

                    var textBlock = new TextBlock
                    {
                        Text = $"{prefix}: {decryptedMessage}",
                        Foreground = Brushes.White,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    };

                    border.Child = textBlock;
                    MessagesPanel.Children.Add(border);
                }
            }
        }
        public ChatUserControl()
        {
            InitializeComponent();
            FriendsList.ItemsSource = Session.Friends;
        }


        public class ChatMessage
        {
            public string sender { get; set; }
            public string message { get; set; }
        }


        private async void FriendsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FriendsList.SelectedItem is FriendModel friend)
            {
                selectedFriend = friend.Name;
               MessagesPanel.Children.Clear();
                await ReloadMessagesSafely(selectedFriend);
                StartMessagePolling(selectedFriend);
            }
        }


        public async Task LoadMessagesFromFriend(string friendName)
        {
            displayedMessages.Clear();
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://127.0.0.1:5000/get_messages?name={Session.name}&from={friendName}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(json);

                File.WriteAllText($"messages_{friendName}.txt", string.Empty);

                foreach (var msg in messages)
                {
                    string decryptedMessage;
                    try
                    {
                        decryptedMessage = DecryptString(msg.message);
                    }
                    catch (Exception ex)
                    {
                        decryptedMessage = "[Error decrypting message]";
                        Console.WriteLine($"Decryption failed: {ex.Message}");
                    }

                    bool isSender = msg.sender == Session.name;
                    string prefix = isSender ? "You" : friendName;

                    var border = new Border
                    {
                        Background = isSender ? Brushes.MediumSlateBlue : Brushes.Gray,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10),
                        Margin = new Thickness(5),
                        MaxWidth = 300,
                        HorizontalAlignment = isSender ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };

                    var textBlock = new TextBlock
                    {
                        Text = $"{prefix}: {decryptedMessage}",
                        Foreground = Brushes.White,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    };

                    border.Child = textBlock;
                    MessagesPanel.Children.Add(border);

                    File.AppendAllText($"messages_{friendName}.txt", $"{prefix}: {decryptedMessage}{Environment.NewLine}");
                }
            }
        }


        public static string DecryptString(string cipherText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes("CphS2dXaGKwVE13oMqYfLBJTR7ztUn60"); 
            byte[] ivBytes = Encoding.UTF8.GetBytes("zXwRQ7TpYVeNcKj1");

            byte[] buffer = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }



        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MessageTextBox.Text) && !string.IsNullOrWhiteSpace(selectedFriend))
            {
                string message = MessageTextBox.Text.Trim();
                string fileName = $"messages_{selectedFriend}.txt";

                
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(108, 92, 231)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    MaxWidth = 300,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var textBlock = new TextBlock
                {
                    Text = $"You: {message}",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                };

                border.Child = textBlock;
                MessagesPanel.Children.Add(border);

        
                string encryptedMessage = CryptoHelper.Encrypt(message);

                
                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    sender = Session.name,
                    receiver = selectedFriend,
                    message = encryptedMessage
                }), Encoding.UTF8, "application/json");

                var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://127.0.0.1:5000/send_message", content);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Failed to send message to backend");
                }

               
                File.AppendAllText(fileName, $"You: {message}{Environment.NewLine}");

                
               MessageTextBox.Clear();
            }
        }

        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: integrate file‐pick and send logic later.
            MessageBox.Show("Attach file clicked – feature coming soon!",
                            "File Attach", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }

}
