using System;
using System.Globalization;
using System.Windows.Data;

namespace Connectt
{
    public class LastSeenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FriendModel friend)
            {
                if (friend.IsOnline)
                    return "Online";

                if (!string.IsNullOrEmpty(friend.LastSeen))
                    return $"Last seen: {friend.LastSeen}";

                return "Offline";
            }

            return "Offline";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
