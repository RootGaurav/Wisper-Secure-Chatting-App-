using Newtonsoft.Json; 
using System;
using System.Collections.Generic;
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
            public string ?Name { get; set; }
            public string? Id { get; set; }
        }
        Session2 s2;
        private static readonly HttpClient client = new HttpClient();
        private static readonly string APIDenUrl = "https://connect-api-4.onrender.com/respond_request";
        private static readonly string ApiSed_Req = "https://connect-api-4.onrender.com/send_friend_request";
        private static readonly string APIAcceptUrl = "https://connect-api-4.onrender.com/respond_request";
        public static  ObservableCollection<FriendRequestModel>? FriendRequests { get; set; }
        
        public FriendRequestControl()
        {
            InitializeComponent();
           // FriendRequests = new ObservableCollection<FriendRequestModel>();
            RequestsListView.ItemsSource = FriendRequests;
           // RequestsListView.ItemsSource = Session2.FriendRequests;

            //LoadIncomingRequests();
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
               
                    var data = new
                    {
                        sender = senderName,
                        receiver = receiverName
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var result = await client.PostAsync(ApiSed_Req, content);

                    if (result.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Friend request sent.");
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

        private async void LoadIncomingRequests()
        {
            string user = Session.name;

            try
            {
                var response = await client.GetAsync($"https://connect-api-4.onrender.com/get_requests?name={user}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var requests = JsonConvert.DeserializeObject<string[]>(json);

                 
                    if (requests != null && requests.Any())
                    {
                        FriendRequests.Clear();
                        foreach (var name in requests)
                        {
                            FriendRequests.Add(new FriendRequestModel { Name = name ,Id=name});
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

            var data = new
            {
                sender = senderName,
                receiver = receiverName,
                status = 1 
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(APIAcceptUrl, content);

            MessageBox.Show("Request accepted.");
            s2 = new Session2();
            await s2.LoadIncomingRequests();
            FriendRequests.Clear();
            return;
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            string senderName = (string)((Button)sender).Tag;
            string receiverName = Session.name;

            var data = new
            {
                sender = senderName,
                receiver = receiverName
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(APIDenUrl, content);

            MessageBox.Show("Request denied.");
            s2 = new Session2();
            await s2.LoadIncomingRequests();
        }
    }
}
