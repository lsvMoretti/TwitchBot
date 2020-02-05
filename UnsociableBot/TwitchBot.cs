using System;
using System.Globalization;
using System.Timers;
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

            Console.ReadLine();
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
            Console.WriteLine($"{e.Username} has connected to {e.Channel}");
            if (e.Channel.ToLower() == Settings.Default.TwitchUsername)
            {
                client.SendMessage(e.Channel, $"Welcome {e.Username}!");
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
            Console.WriteLine($"{e.DateTime.ToString(CultureInfo.CurrentCulture)}: {e.BotUsername} - {e.Data}");
#endif
        }

        private void JoinChannels()
        {
            Console.WriteLine($"Joining Additional Channels");
            client.JoinChannel("gingyplus");
        }
    }
}