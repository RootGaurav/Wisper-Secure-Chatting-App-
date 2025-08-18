using System.Text;
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
using System.Runtime.CompilerServices;

namespace Connectt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!File.Exists("log"))
            {
                new SignUp().Show();
                this.Close();
                return;
            }
            Session.name = File.ReadAllText("log").Trim();
            Load();

            InitializeComponent();
        }
        public async void Load()
        {
            Connectt.Session2 s2 = new Session2();
            await s2.LoadIncomingRequests();
            await s2.LoadFriends();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }
}
   


