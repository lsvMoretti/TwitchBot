using System;
using System.Globalization;
using System.Timers;
using TwitchLib.Client;
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

            client.Connect();

            Timer timer = new Timer(500) { AutoReset = true };
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

            Console.ReadLine();
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString(CultureInfo.CurrentCulture)}: {e.BotUsername} - {e.Data}");
        }
    }
}