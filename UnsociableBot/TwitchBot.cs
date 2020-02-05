using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Timers;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace UnsociableBot
{
    public class TwitchBot
    {
        private TwitchClient client;

        public TwitchBot()
        {
            bool connected = false;

            Console.WriteLine($"Connecting to Twitch..");

            ConnectionCredentials credentials = new ConnectionCredentials(Settings.Default.TwitchUsername, Settings.Default.TwitchClientToken);

            ClientOptions clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);

            client = new TwitchClient(customClient);
            client.Initialize(credentials, Settings.Default.TwitchUsername);

            client.OnLog += Client_OnLog;
            client.OnConnected += Client_OnConnected;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnUserJoined += Client_OnUserJoined;
            client.OnBeingHosted += Client_OnBeingHosted;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;

            client.Connect();

            Timer timer = new Timer(5) { AutoReset = true };
            timer.Start();

            timer.Elapsed += (sender, args) =>
            {
                Console.WriteLine("..");
            };

            while (!connected)
            {
                connected = client.IsConnected;
            }

            timer.Dispose();

            Console.WriteLine($"Connected to Twitch.");

            timer = new Timer(1000) { AutoReset = false };
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Dispose();
#if RELEASE
                JoinChannels();
#endif
            };

            TwitchApi api = new TwitchApi();

            TwitchApi.StartApi();

            while (!Program.QuitFlag)
            {
                string consoleInput = Console.ReadLine();

                if (string.IsNullOrEmpty(consoleInput)) continue;

                if (consoleInput.StartsWith('!'))
                {
                    string[] commandSplit = consoleInput.Split(' ');

                    if (commandSplit[0].ToLower() == "!help" || commandSplit[0].ToLower() == "!commands")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"--- Unsociable's Bot Help ---");
                        Console.WriteLine("!say [Message] - Says a message in your chat!");
                        Console.WriteLine($"!addnotification [StreamerName] - Adds a streamer to your system when they start streaming");
                        Console.WriteLine("!removenotification [StreamerName] - Removes a streamer from your system");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    }

                    if (commandSplit[0].ToLower() == "!say")
                    {
                        if (commandSplit.Length < 2)
                        {
                            Console.WriteLine($"You need to input a message!");
                            continue;
                        }

                        string joined = string.Join(' ', commandSplit.Skip(1));

                        client.SendMessage(Settings.Default.TwitchUsername, joined);

                        Console.WriteLine($"Message sent to {Settings.Default.TwitchUsername} channel: {joined}");
                    }

                    if (commandSplit[0].ToLower() == "!addnotification")
                    {
                        if (commandSplit.Length < 2)
                        {
                            Console.WriteLine($"You need to input a streamers channel name!");
                            continue;
                        }

                        string joined = string.Join(' ', commandSplit.Skip(1));

                        if (TwitchApi.StreamNotifications.Contains(joined.ToLower()))
                        {
                            Console.WriteLine($"You already have {joined} in your notifications!");
                            continue;
                        }

                        TwitchApi.StreamNotifications.Add(joined.ToLower());
                        TwitchApi.SaveStreamNotifications();
                        Console.WriteLine($"You've added {joined} to your notifications!");
                    }

                    if (commandSplit[0].ToLower() == "!removenotification")
                    {
                        if (commandSplit.Length < 2)
                        {
                            Console.WriteLine($"You need to input a streamers channel name!");
                            continue;
                        }

                        string joined = string.Join(' ', commandSplit.Skip(1));

                        if (!TwitchApi.StreamNotifications.Contains(joined.ToLower()))
                        {
                            Console.WriteLine($"You don't have {joined} in your notifications!");
                            continue;
                        }

                        TwitchApi.StreamNotifications.Remove(joined.ToLower());
                        TwitchApi.SaveStreamNotifications();
                        Console.WriteLine($"You've removed {joined} from your notifications!");
                    }
                }

                if (consoleInput.ToLower() == "quit")
                {
                    Program.QuitFlag = true;
                }
            }
        }

        private void Client_OnNewSubscriber(object sender, TwitchLib.Client.Events.OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            {
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the subscribers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            }
            else
            {
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the subscribers! You just earned 500 points!");
            }
        }

        private void Client_OnWhisperReceived(object sender, TwitchLib.Client.Events.OnWhisperReceivedArgs e)
        {
            Console.WriteLine($"{e.WhisperMessage.Username} has whispered {e.WhisperMessage.Message}.");
        }

        private void Client_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Channel == Settings.Default.TwitchUsername)
            {
                if (e.ChatMessage.Message.StartsWith('!'))
                {
                    Console.WriteLine($"{e.ChatMessage.Username} has typed {e.ChatMessage.Message}");
                }

                if (e.ChatMessage.Message.Contains("badword"))
                {
                    client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromSeconds(5));
                    client.SendMessage(e.ChatMessage.Channel, $"Naughty {e.ChatMessage.Username}!");
                }
            }
        }

        private void Client_OnBeingHosted(object sender, TwitchLib.Client.Events.OnBeingHostedArgs e)
        {
            if (e.BeingHostedNotification.Channel == Settings.Default.TwitchUsername)
            {
                Console.WriteLine($"{e.BeingHostedNotification.HostedByChannel} is hosting {e.BeingHostedNotification.Channel} with {e.BeingHostedNotification.Viewers} viewers!");
            }
        }

        private void Client_OnUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            if (e.Username != client.TwitchUsername)
            {
                Console.WriteLine($"{e.Username} has connected to {e.Channel}");
                if (e.Channel.ToLower() == Settings.Default.TwitchUsername)
                {
                    User user = TwitchApi.FetchUser(e.Username).Result;

                    if (user == null) return;

                    Debug.WriteLine($"User {user.DisplayName} type: {user.Type}.");

                    //client.SendMessage(e.Channel, $"Welcome {e.Username}!");
                }
            }
        }

        private void Client_OnJoinedChannel(object sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Channel Joined: {e.Channel}.");

            client.SendMessage(e.Channel, "Hey guys!");
        }

        private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Console.WriteLine($"{e.BotUsername} has connected to {e.AutoJoinChannel}");
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
#if DEBUG
            //Console.WriteLine($"{e.DateTime.ToString(CultureInfo.CurrentCulture)}: {e.BotUsername} - {e.Data}");
#endif
        }

        private void JoinChannels()
        {
            Console.WriteLine($"Joining Additional Channels");
            client.JoinChannel("gingyplus");
        }
    }
}