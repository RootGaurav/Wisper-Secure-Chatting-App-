using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Connectt
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // 🔗 Flask endpoints for online tracking
        private readonly string setStatusAPI = "http://127.0.0.1:5000/set_status";
        private readonly string getStatusesAPI = "http://127.0.0.1:5000/get_statuses";

        public MainWindow()
        {
            // Ensure user session file exists
            if (!File.Exists("log"))
            {
                new SignUp().Show();
                this.Close();
                return;
            }

            Session.name = File.ReadAllText("log").Trim();

            InitializeComponent();
            this.Closing += async (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(Session.name))
                {
                    await SetUserOnlineStatus(Session.name, false);
                }
            };

            LoadUserName();
            Load();
            // ✅ Set default content: Friend Requests page
            MainContent.Content = new FriendRequestControl();

            // 🟢 Mark user online
            _ = SetUserOnlineStatus(Session.name, true);

            // 🔁 Start background friend status polling
            StartStatusPolling();
        }

        private void LoadUserName()
        {
            try
            {
                string loggedUser = "";
                if (File.Exists("log"))
                    loggedUser = File.ReadAllText("log").Trim();

                if (!string.IsNullOrWhiteSpace(loggedUser))
                {
                    UserTitle.Text = $"Welcome, {loggedUser}";
                    this.Title = $"Wisper – {loggedUser}";
                }
                else
                {
                    UserTitle.Text = "Wisper";
                }
            }
            catch
            {
                UserTitle.Text = "Wisper";
            }
        }

        public async void Load()
        {
            try
            {
                Session2 s2 = new Session2();
                await s2.LoadIncomingRequests();
                await s2.LoadFriends();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        private void FriendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            FriendRequestControl requestControl = new FriendRequestControl();
            MainContent.Content = requestControl;
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ChatUserControl();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            try
            {
                // 🔴 Mark offline before logout
                await SetUserOnlineStatus(Session.name, false);

                // Clear session
                if (File.Exists("log"))
                    File.Delete("log");

                Session.name = null;

                // Redirect to login/signup
                new SignUp().Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during logout: " + ex.Message, "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🟢 Set user online/offline in backend
        private async Task SetUserOnlineStatus(string username, bool online)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return;

                var payload = new { name = username, online = online };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                await client.PostAsync(setStatusAPI, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting user status: " + ex.Message);
            }
        }

        // 🔁 Poll for friend statuses every 5s
        private async void StartStatusPolling()
        {
            while (true)
            {
                try
                {
                    await UpdateFriendStatuses();
                }
                catch
                {
                    // ignore temporary network errors
                }

                await Task.Delay(5000);
            }
        }

        // 🧠 Fetch friends’ online/offline states
        private async Task UpdateFriendStatuses()
        {
            if (Session.Friends == null || Session.Friends.Count == 0)
                return;

            try
            {
                var friendNames = Session.Friends.Select(f => f.Name).ToList();
                var payload = new { friends = friendNames };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(getStatusesAPI, content);
                if (!response.IsSuccessStatusCode)
                    return;

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                foreach (var friend in Session.Friends)
                {
                    if (friend.Name != null && result.ContainsKey(friend.Name))
                    {
                        friend.IsOnline = result[friend.Name]["online"];
                        friend.LastSeen = result[friend.Name]["last_seen"];
                    }
                }

                // Optionally refresh friend list UI if bound
                // Example: FriendsListView.Items.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating friend statuses: " + ex.Message);
            }
        }

        // 🔴 Ensure offline when app closes
        protected override async void OnClosed(EventArgs e)
        {
            await SetUserOnlineStatus(Session.name, false);
            base.OnClosed(e);
        }
    }
}
