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

        Session2 s2;
        private static readonly HttpClient client = new HttpClient();
        private static readonly string APIDenUrl = "http://127.0.0.1:5000/respond_request";
        private static readonly string ApiSed_Req = "http://127.0.0.1:5000/send_friend_request";
        private static readonly string APIAcceptUrl = "http://127.0.0.1:5000/respond_request";

        // Initialize the collection so it’s never null
        public static ObservableCollection<FriendRequestModel>? FriendRequests { get; set; }
            = new ObservableCollection<FriendRequestModel>();

        public FriendRequestControl()
        {
            InitializeComponent();

            // Bind and load on startup
            RequestsListView.ItemsSource = FriendRequests;
            _ = LoadIncomingRequests();
        }

        private async void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string senderName = Session.name;
            string receiverName = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(receiverName))
            {
                MessageBox.Show("Please enter a name.");
                return;
            }

            try
            {
                var data = new { sender = senderName, receiver = receiverName };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var result = await client.PostAsync(ApiSed_Req, content);

                if (result.IsSuccessStatusCode)
                {
                    MessageBox.Show("Friend request sent.");
                    await LoadIncomingRequests();
                }
                else
                {
                    MessageBox.Show("Failed to send request.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task LoadIncomingRequests()
        {
            string user = Session.name;

            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:5000/get_requests?name={user}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var requests = JsonConvert.DeserializeObject<string[]>(json);

                    if (requests != null)
                    {
                        FriendRequests?.Clear();
                        foreach (var name in requests)
                        {
                            FriendRequests?.Add(new FriendRequestModel { Name = name, Id = name });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load requests: " + ex.Message);
            }
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            string senderName = (string)((Button)sender).Tag;
            string receiverName = Session.name;

            var data = new { sender = senderName, receiver = receiverName, status = 1 };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(APIAcceptUrl, content);

            MessageBox.Show("Request accepted.");
            await LoadIncomingRequests();
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            string senderName = (string)((Button)sender).Tag;
            string receiverName = Session.name;

            var data = new { sender = senderName, receiver = receiverName };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(APIDenUrl, content);

            MessageBox.Show("Request denied.");
            await LoadIncomingRequests();
        }
    }
}
