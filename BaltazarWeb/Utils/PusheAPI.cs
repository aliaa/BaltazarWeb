using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BaltazarWeb.Utils
{
    public class PusheAPI : IPushNotificationProvider
    {
        class MessageData
        {
            public string title { get; set; }
            public string content { get; set; }
        }

        class Action
        {
            public string url { get; set; }
            public string action_type { get; set; } = "A";
        }

        class PushNotificationMessage
        {
            public Action action { get; set; } = new Action();
            public string[] app_ids { get; set; }
            public MessageData data { get; set; }
        }

        class PushFilters
        {
            public List<string> device_id { get; set; }
        }

        class FilteredPushNotificationMessage : PushNotificationMessage
        {
            public PushFilters filters { get; set; }
        }

        private readonly string token;
        private readonly string packageName;
        private const string NOTIFICATIONS_URI = "https://api.pushe.co/v2/messaging/notifications/";
        private readonly HttpClient client;

        public PusheAPI(string token, string packageName)
        {
            this.token = token;
            this.packageName = packageName;
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        }

        public bool SendMessageToAll(string title, string message)
        {
            var msg = new PushNotificationMessage
            {
                app_ids = new string[] { packageName },
                data = new MessageData
                {
                    title = title,
                    content = message
                }
            };
            return SendJson(msg);
        }

        private bool SendJson<T>(T data)
        {
            try
            {
                var task = client.PostAsJsonAsync(NOTIFICATIONS_URI, data);
                if (!task.Wait(2000))
                    return false;
                var response = task.Result;
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait(1000);
                return (int)response.StatusCode / 100 == 2;
            }
            catch
            {
                return false;
            }
        }

        public bool SendMessageToUser(string title, string message, string pusheId)
        {
            List<string> pusheIds = new List<string>();
            pusheIds.Add(pusheId);
            return SendMessageToUsers(title, message, pusheIds);
        }

        public bool SendMessageToUsers(string title, string message, List<string> pusheIds)
        {
            var msg = new FilteredPushNotificationMessage
            {
                app_ids = new string[] { packageName },
                data = new MessageData
                {
                    title = title,
                    content = message
                },
                filters = new PushFilters
                {
                    device_id = pusheIds
                }
            };
            return SendJson(msg);
        }
    }
}
