using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Connectt
{
    public partial class FriendRequestControl : UserControl
    {
        public class FriendRequestModel
        {
            public string? Name { get; set; }
            public string? Id { get; set; }
        }

        private static readonly HttpClient client = new HttpClient();

        // ✅ API Endpoints
        private static readonly string ApiSendRequest = "http://127.0.0.1:5000/send_friend_request";
        private static readonly string ApiRespondRequest = "http://127.0.0.1:5000/respond_request";
        private static readonly string ApiGetRequests = "http://127.0.0.1:5000/get_requests";
        private static readonly string ApiGetSuggestions = "http://127.0.0.1:5000/get_suggestions";

        // ✅ Collections
        public static ObservableCollection<FriendRequestModel> FriendRequests { get; set; } = new();
        public static ObservableCollection<string> FriendSuggestions { get; set; } = new();

        public FriendRequestControl()
        {
            InitializeComponent();

            RequestsListView.ItemsSource = FriendRequests;
            SuggestionsListView.ItemsSource = FriendSuggestions;

            _ = LoadAllData();
        }

        // === LOAD BOTH REQUESTS AND SUGGESTIONS ===
        private async Task LoadAllData()
        {
            await LoadIncomingRequests();
            await LoadFriendSuggestions();
        }

        // === SEND FRIEND REQUEST ===
        private async void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string senderName = Session.name;
            string receiverName = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(receiverName))
            {
                MessageBox.Show("Please enter a username.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (receiverName.Equals(senderName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("You cannot send a friend request to yourself.", "Invalid", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                var data = new { sender = senderName, receiver = receiverName };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var result = await client.PostAsync(ApiSendRequest, content);
                string resp = await result.Content.ReadAsStringAsync();

                if (result.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ Friend request sent!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllData();
                }
                else
                {
                    dynamic error = JsonConvert.DeserializeObject(resp);
                    MessageBox.Show($"❌ {error?.error ?? "Failed to send request"}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === LOAD INCOMING REQUESTS ===
        private async Task LoadIncomingRequests()
        {
            try
            {
                string user = Session.name;
                var response = await client.GetAsync($"{ApiGetRequests}?name={user}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var requests = JsonConvert.DeserializeObject<string[]>(json);

                    FriendRequests.Clear();
                    if (requests != null)
                    {
                        foreach (var req in requests)
                        {
                            FriendRequests.Add(new FriendRequestModel { Name = req, Id = req });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load requests: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === LOAD FRIEND SUGGESTIONS ===
        private async Task LoadFriendSuggestions()
        {
            try
            {
                string user = Session.name;
                var response = await client.GetAsync($"{ApiGetSuggestions}?name={user}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var suggestions = JsonConvert.DeserializeObject<string[]>(json);

                    FriendSuggestions.Clear();
                    if (suggestions != null)
                    {
                        foreach (var s in suggestions)
                            FriendSuggestions.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load suggestions: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === ACCEPT REQUEST ===
        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string senderName)
            {
                try
                {
                    var receiverName = Session.name;
                    var data = new { sender = senderName, receiver = receiverName, status = 1 };
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var result = await client.PostAsync(ApiRespondRequest, content);
                    if (result.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"✅ You are now friends with {senderName}!", "Friend Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadIncomingRequests();
                        await LoadFriendSuggestions();
                        Session2 s2 = new Session2();
                        await s2.LoadFriends();

                    }
                    else
                    {
                        MessageBox.Show("Failed to accept request.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error accepting request: " + ex.Message);
                }
            }
        }

        // === DENY REQUEST ===
        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string senderName)
            {
                try
                {
                    var receiverName = Session.name;
                    var data = new { sender = senderName, receiver = receiverName, status = 0 };
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var result = await client.PostAsync(ApiRespondRequest, content);
                    if (result.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"❌ Request from {senderName} denied.", "Denied", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadIncomingRequests();
                        await LoadFriendSuggestions(); 
                    }
                    else
                    {
                        MessageBox.Show("Failed to deny request.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error denying request: " + ex.Message);
                }
            }
        }

        // === ADD FRIEND FROM SUGGESTIONS ===
        private async void AddSuggestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string suggestedUser)
            {
                try
                {
                    string senderName = Session.name;
                    var data = new { sender = senderName, receiver = suggestedUser };
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var result = await client.PostAsync(ApiSendRequest, content);
                    string resp = await result.Content.ReadAsStringAsync();

                    if (result.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"✅ Friend request sent to {suggestedUser}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadFriendSuggestions();
                    }
                    else
                    {
                        dynamic err = JsonConvert.DeserializeObject(resp);
                        MessageBox.Show($"❌ {err?.error ?? "Failed to send request."}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending suggestion request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
