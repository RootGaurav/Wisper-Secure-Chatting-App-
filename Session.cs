using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Connectt.FriendRequestControl;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace Connectt
{
    public static class Session
    {
        public static string name { get; set; }
        public static ObservableCollection<FriendRequestModel>? FriendRequests { get; set; }
        public static ObservableCollection<FriendModel> Friends { get; set; }
    }
    public class FriendModel
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
    }
    public  class Session2
    {
        
        public class FriendRequestModels
        {
            public string? Name { get; set; }
            public string? Id { get; set; }

            
        }
       //public static ObservableCollection<FriendRequestModel> FriendRequests { get; set; }
        private static readonly HttpClient client = new HttpClient();
       
        
        public Session2()
        {
            FriendRequestControl.FriendRequests = new ObservableCollection<FriendRequestModel>();

        }

        public async Task LoadIncomingRequests()
        {

            string user = Session.name;

            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:5000/get_requests?name={user}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var requests = JsonConvert.DeserializeObject<string[]>(json);


                    if (requests != null && requests.Any())
                    {
                        if(FriendRequests==null)
                        {
                            FriendRequestControl.FriendRequests = new ObservableCollection<FriendRequestModel>();
                        }
                        
                        FriendRequests.Clear();
                        foreach (var name in requests)
                        {
                            FriendRequests.Add(new FriendRequestModel { Name = name, Id = name });
                        }

                        

                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load requests: " + ex.Message);
            }
        }

        public async Task LoadFriends()
        {
            string user = Session.name;
            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:5000/get_friends?name={user}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var friends = JsonConvert.DeserializeObject<string[]>(json);

                    if (friends != null)
                    {
                        Session.Friends = new ObservableCollection<FriendModel>(
                            friends.Select(f => new FriendModel { Name = f, Id = f })
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load friends: " + ex.Message);
            }
        }
    }
}
