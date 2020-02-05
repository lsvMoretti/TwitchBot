using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Users;

namespace UnsociableBot
{
    public class TwitchApi
    {
        private static TwitchAPI _api;

        public static List<string> StreamNotifications = new List<string>();

        private static Dictionary<string, bool> _lastStreamStatus = new Dictionary<string, bool>();

        public TwitchApi()
        {
            _api = new TwitchAPI();
            _api.Settings.ClientId = Settings.Default.TwitchUsername;
            _api.Settings.AccessToken = Settings.Default.TwitchClientToken;
        }

        public static void StartApi()
        {
            //TwitchApi api = new TwitchApi();

            string streamNotificationsPath = $"{Program.LocalFilePath}/StreamNotifications.json";

            bool streamNotificationFileExists = File.Exists(streamNotificationsPath);

            if (!streamNotificationFileExists)
            {
                StreamNotifications = new List<string>
                {
                    Settings.Default.TwitchUsername.ToLower()
                };

                File.WriteAllText(streamNotificationsPath, JsonConvert.SerializeObject(StreamNotifications, Formatting.Indented));
            }
            else
            {
                using (StreamReader sr = new StreamReader(streamNotificationsPath))
                {
                    string fileContents = sr.ReadToEnd();

                    StreamNotifications = JsonConvert.DeserializeObject<List<string>>(fileContents);
                }
            }
            Console.WriteLine($"You have notifications for {StreamNotifications.Count} streamers!");

            StreamFollower.InitFollowers();

            Task.Run(async () =>
            {
                while (true)
                {
                    await CallsAsync();
                }
            });
        }

        public static void SaveStreamNotifications()
        {
            string streamNotificationsPath = $"{Program.LocalFilePath}/StreamNotifications.json";
            File.WriteAllText(streamNotificationsPath, JsonConvert.SerializeObject(StreamNotifications, Formatting.Indented));
        }

        private static async Task CallsAsync()
        {
            await StreamFollower.UpdateFollowers();

            foreach (string streamNotification in StreamNotifications)
            {
                if (_lastStreamStatus.ContainsKey(streamNotification))
                {
                    var kvp = _lastStreamStatus.FirstOrDefault(x => x.Key == streamNotification);

                    var user = await _api.V5.Users.GetUserByNameAsync(streamNotification.ToLower());
                    if (user.Total == 0)
                    {
                        continue;
                    }

                    bool isStreaming = await _api.V5.Streams.BroadcasterOnlineAsync(user.Matches[0].Id);

                    if (isStreaming != kvp.Value)
                    {
                        if (isStreaming)
                        {
                            Console.WriteLine($"{user.Matches[0].DisplayName} is streaming!");
                        }

                        _lastStreamStatus.Remove(streamNotification);
                        _lastStreamStatus.Add(streamNotification, isStreaming);
                    }
                }
                else
                {
                    var user = await _api.V5.Users.GetUserByNameAsync(streamNotification.ToLower());
                    if (user.Total == 0)
                    {
                        continue;
                    }

                    bool isStreaming = await _api.V5.Streams.BroadcasterOnlineAsync(user.Matches[0].Id);
                    if (isStreaming)
                    {
                        Console.WriteLine($"{user.Matches[0].DisplayName} is streaming!");
                    }

                    _lastStreamStatus.Add(streamNotification, isStreaming);
                }
            }

            return;
        }

        public static async Task<List<ChannelFollow>> FetchFollowers(string username)
        {
            new TwitchApi();

            var user = await _api.V5.Users.GetUserByNameAsync(username.ToLower());
            if (user.Total == 0) return null;
            return await _api.V5.Channels.GetAllFollowersAsync(user.Matches[0].Id);
        }

        public static async Task<User> FetchUser(string username)
        {
            new TwitchApi();

            Users users = await _api.V5.Users.GetUserByNameAsync(username.ToLower());

            if (users.Total == 0)
            {
                return null;
            }

            return users.Matches[0];
        }
    }
}