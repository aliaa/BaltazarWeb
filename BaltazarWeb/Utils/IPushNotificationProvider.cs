using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Utils
{
    public interface IPushNotificationProvider
    {
        bool SendMessageToAll(string title, string message, out string responseStr);
        bool SendMessageToUser(string title, string message, string id, out string responseStr);
        bool SendMessageToUsers(string title, string message, List<string> ids, out string responseStr);
    }
}
