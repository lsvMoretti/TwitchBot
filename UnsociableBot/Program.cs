using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Timer = System.Timers.Timer;

namespace UnsociableBot
{
    internal class Program
    {
        public static bool QuitFlag = false;
        public static string LocalFilePath = "";

        private static void Main(string[] args)
        {
            Console.Title = $"UnsociableBot";
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Clear();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                QuitFlag = true;
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

            if (string.IsNullOrEmpty(Settings.Default.TwitchClientToken))
            {
                Console.WriteLine($"You can get a token from: https://twitchtokengenerator.com/");
                Console.WriteLine($"What is your token?");
                string token = Console.ReadLine();

                bool userConfirm = false;
                Console.WriteLine($"Confirm your token is: {token}. Y/N");
                string tokenConfirm = Console.ReadLine();
                if (tokenConfirm.ToLower() == "y")
                {
                    userConfirm = true;
                }

                while (!userConfirm)
                {
                    Console.WriteLine($"What is your twitch token?");
                    token = Console.ReadLine();
                    Console.WriteLine($"Confirm your token is {token}. Y/N");
                    tokenConfirm = Console.ReadLine();
                    if (tokenConfirm.ToLower() == "y")
                    {
                        userConfirm = true;
                    }
                }

                Settings.Default.TwitchClientToken = token;
                Settings.Default.Save();
            }

            Console.Clear();

            Settings.Default.Reload();

            Console.WriteLine("Settings have been reloaded!");

            string longPath = GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal);

            string[] filePathSplit = longPath.Split(@"\");

            string filePath = string.Join(@"\", filePathSplit.SkipLast(2));

            LocalFilePath = filePath;

            TwitchBot bot = new TwitchBot();

            while (!QuitFlag)
            {
                Thread.Sleep(0);
            }
        }

        public static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
        {
            try
            {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return UserConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                return e.Filename;
            }
        }
    }
}