using System;
using System.Diagnostics;
using System.Threading;
using Timer = System.Timers.Timer;

namespace UnsociableBot
{
    internal class Program
    {
        private static bool _quitFlag = false;

        private static void Main(string[] args)
        {
            Console.Title = $"UnsociableBot";
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Clear();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _quitFlag = true;
            };

            Settings.Default.Reload();

            Debug.WriteLine($"Upgrade Required: {Settings.Default.UpgradeRequired}.");

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            Settings.Default.Reload();
            Console.WriteLine("Starting the UnsociableBot!");

            if (string.IsNullOrEmpty(Settings.Default.TwitchUsername))
            {
                Console.WriteLine($"What is your twitch username?");
                string twitchUsername = Console.ReadLine();

                bool userConfirm = false;

                Console.WriteLine($"Confirm your username is: {twitchUsername}. Y/N");
                string twitchUsernameConfirm = Console.ReadLine();
                if (twitchUsernameConfirm.ToLower() == "y")
                {
                    userConfirm = true;
                }

                while (!userConfirm)
                {
                    Console.WriteLine($"What is your twitch username?");
                    twitchUsername = Console.ReadLine();
                    Console.WriteLine($"Confirm your username is: {twitchUsername}. Y/N");
                    twitchUsernameConfirm = Console.ReadLine();
                    if (twitchUsernameConfirm.ToLower() == "y")
                    {
                        userConfirm = true;
                    }
                }

                Settings.Default.TwitchUsername = twitchUsername;
                Settings.Default.Save();
            }

            Settings.Default.Reload();

            Console.WriteLine("Settings have been reloaded!");

            TwitchBot bot = new TwitchBot();

            while (!_quitFlag)
            {
                Thread.Sleep(1);
            }
        }
    }
}