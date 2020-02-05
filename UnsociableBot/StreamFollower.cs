using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchLib.Api.V5.Models.Channels;

namespace UnsociableBot
{
    public class StreamFollower
    {
        public static List<ChannelFollow> StreamFollowers = new List<ChannelFollow>();

        private static readonly string StreamFollowersPath = $"{Program.LocalFilePath}/StreamFollowers.json";

        public static void InitFollowers()
        {
            List<ChannelFollow> followers = TwitchApi.FetchFollowers(Settings.Default.TwitchUsername).Result;

            bool fileExists = File.Exists(StreamFollowersPath);

            if (!fileExists)
            {
                File.WriteAllText(StreamFollowersPath, JsonConvert.SerializeObject(followers, Formatting.Indented));
                Console.WriteLine($"You have {followers.Count} Followers!");

                StreamFollowers = followers;
                return;
            }

            using (StreamReader sr = new StreamReader(StreamFollowersPath))
            {
                string contents = sr.ReadToEnd();

                sr.Dispose();
                List<ChannelFollow> fileFollowers = JsonConvert.DeserializeObject<List<ChannelFollow>>(contents);

                int followerGain = 0;
                int followerLost = 0;

                foreach (ChannelFollow channelFollow in followers)
                {
                    ChannelFollow fileFollower =
                        fileFollowers.FirstOrDefault(x => x.User.Name == channelFollow.User.Name);

                    if (fileFollower == null)
                    {
                        Console.WriteLine($"New Follower: {channelFollow.User.DisplayName}");
                        followerGain++;
                    }
                }

                foreach (ChannelFollow fileFollower in fileFollowers)
                {
                    ChannelFollow follower = followers.FirstOrDefault(x => x.User.Name == fileFollower.User.Name);

                    if (follower == null)
                    {
                        Console.WriteLine($"Lost Follower: {fileFollower.User.DisplayName}");
                        followerLost++;
                    }
                }

                Console.WriteLine($"You have gained {followerGain} and lost {followerLost} followers!");

                StreamFollowers = followers;

                File.WriteAllText(StreamFollowersPath, JsonConvert.SerializeObject(StreamFollowers, Formatting.Indented));
            }
        }

        public static async Task UpdateFollowers()
        {
            string username = Settings.Default.TwitchUsername;
            List<ChannelFollow> channelFollows = await TwitchApi.FetchFollowers(username);

            int difference = channelFollows.Count - StreamFollowers.Count;

            Debug.WriteIf(difference != 0, $"Difference: {difference}");

            if (difference > 0)
            {
                // New Follower
                foreach (ChannelFollow channelFollow in channelFollows)
                {
                    ChannelFollow currentFollower =
                        StreamFollowers.FirstOrDefault(x => x.User.Name == channelFollow.User.Name);

                    if (currentFollower == null)
                    {
                        // Not in list
                        Console.WriteLine($"New Follower: {channelFollow.User.DisplayName}");
                        TwitchBot.SendMessage(username, $"Thanks for the follow {channelFollow.User.DisplayName}!");
                    }
                }
            }

            if (difference < 0)
            {
                // Lost Follower
                foreach (ChannelFollow streamFollower in StreamFollowers)
                {
                    ChannelFollow follower =
                        channelFollows.FirstOrDefault(x => x.User.Name == streamFollower.User.Name);

                    if (follower == null)
                    {
                        // No longer in channel
                        Console.WriteLine($"Lost Follower: {streamFollower.User.DisplayName}");
                    }
                }
            }

            if (difference != 0)
            {
                StreamFollowers = channelFollows;

                File.WriteAllText(StreamFollowersPath, JsonConvert.SerializeObject(StreamFollowers, Formatting.Indented));
            }
        }
    }
}