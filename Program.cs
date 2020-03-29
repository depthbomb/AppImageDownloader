using System;
using System.IO;
using System.Media;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// App Image Downloader
// Copyright (C) 2020  Caprine Logic
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace AppImageDownloader
{
    class Program
    {
        static Regex IdRegex     = new Regex(@"7656119[0-9]{10}|\[U:[0-1]:[0-9]{1,10}\]");
        static Regex UrlRegex    = new Regex(@"(?:https?:\/\/)?steamcommunity\.com\/(?:profiles|id)\/[a-zA-Z0-9]+");
        static Regex GameIdRegex = new Regex(@"apps\\/(\d+)");

        static string SteamProfile;
        static dynamic ImageType;
        static bool DownloadAll       = false;
        static bool ThrottleDownloads = true;
        static string DownloadFolder  = Path.Combine(KnownFolders.GetPath(KnownFolders.KnownFolder.Downloads), "AppImageDownloader");

        static List<int> GameIds = new List<int>();
        static List<KeyValuePair<string, string>> Images = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("logo.png", "https://steamcdn-a.akamaihd.net/steam/apps/"),
            new KeyValuePair<string, string>("header.jpg", "https://steamcdn-a.akamaihd.net/steam/apps/"),
            new KeyValuePair<string, string>("library_hero.jpg", "https://steamcdn-a.akamaihd.net/steam/apps/"),
            new KeyValuePair<string, string>("library_hero_blur.jpg", "https://steamcdn-a.akamaihd.net/steam/apps/"),
            new KeyValuePair<string, string>("library_600x900.jpg", "https://steamcdn-a.akamaihd.net/steam/apps/"),
            new KeyValuePair<string, string>("library_600x900_2x.jpg", "https://steamcdn-a.akamaihd.net/steam/apps/"),
        };
        static List<KeyValuePair<string, string>> Queue = new List<KeyValuePair<string, string>>();

        static async Task Main(string[] args)
        {
            #region Intro
            Console.Title = "AppImageDownloader";
            Output.WriteLine("This tool will download all web assets for all games on the provided Steam profile.");
            Output.WriteLine($"These files will be downloaded into your Downloads folder located at {DownloadFolder}");
            Output.WriteLine("Press any key to continue.");

            Console.ReadKey();
            Console.Clear();

            if (!Directory.Exists(DownloadFolder))
                Directory.CreateDirectory(DownloadFolder);

            Output.WriteLine("First, enter a SteamID64 or profile URL.");
            Output.WriteLine("You can paste into this window by right clicking.");
            #endregion

            #region Enter SteamID
            AskForSteamId:

            Output.Separator("-", 32);
            Output.Write("SteamID64/Profile URL: ");

            SteamProfile = Console.ReadLine().Trim();

            if (!IdRegex.Match(SteamProfile).Success)
            {
                // Not a SteamID64/SteamID3, check if it is a profile URL...
                if (!UrlRegex.Match(SteamProfile).Success)
                {
                    Output.WriteLine("The SteamID/Profile URL you provided is invalid. Please try again.", Output.OutputTypes.ERROR);

                    goto AskForSteamId;
                }
                else
                {
                    SteamProfile = SteamProfile.TrimEnd('/');
                }
            }
            else
            {
                SteamProfile = $"https://steamcommunity.com/profiles/{SteamProfile}";
            }
            #endregion

            #region Enter Image Type
            Output.Separator("-", 32);
            Output.WriteLine("Now we will need to know what type of images you want to download for your games.");

            AskForImageType:

            foreach (var image in Images)
                Output.WriteLine($"{Images.IndexOf(image)} = {image.Key}");

            Output.Write("Note that some games will not have some image types.");
            Output.Separator();
            Output.Write("Image type (enter A for all types): ");

            ImageType = Console.ReadLine().Trim().ToLower();

            if (ImageType == "a")
            {
                DownloadAll = true;
            }
            else
            {
                if (int.TryParse(ImageType, out int result))
                {
                    if (result < 0 || result >= Images.Count)
                    {
                        Output.WriteLine("Image type is invalid.", Output.OutputTypes.ERROR);
                        goto AskForImageType;
                    }
                }
                else
                {
                    Output.WriteLine("Image type must be a number unless you wish to download all types, if so then enter A", Output.OutputTypes.ERROR);
                    goto AskForImageType;

                }
            }
            #endregion

            #region Choose ThrottleDownloads value
            Output.Separator("-", 32);
            Output.WriteLine("Would you like to throttle downloads? This adds a small delay between each download in the queue to help prevent you from hitting any rate limits put in place by Steam.");

            ThrottleDownloads = Output.YesNoPrompt();
            #endregion

            #region Download queue
            DoWork:

            Output.Separator("-", 32);
            Output.WriteLine("Requesting profile games page...", Output.OutputTypes.INFO);
            string gamesPage = await Request.GetContent(SteamProfile + "/games?tab=all");
            var gameMatches = GameIdRegex.Matches(gamesPage);

            if (gameMatches.Count > 0)
            {
                Output.WriteLine($"Loading {gameMatches.Count} game IDs...", Output.OutputTypes.INFO);
                foreach (Match gameId in gameMatches)
                    GameIds.Add(int.Parse(gameId.Groups[1].Value));

                Output.WriteLine("Adding downloads to queue...", Output.OutputTypes.INFO);

                if (DownloadAll)
                {
                    foreach (var kv in Images)
                    {
                        int type = Images.IndexOf(kv);
                        foreach (int id in GameIds)
                            PrepareDownload(id, type);
                    }
                }
                else
                {
                    foreach (int id in GameIds)
                        PrepareDownload(id, int.Parse(ImageType));
                }

                if (Queue.Count > 0)
                {
                    Output.WriteLine($"Starting download of {Queue.Count} items...", Output.OutputTypes.INFO);
                    Output.Separator("-", 32);

                    await DownloadQueue();

                    Output.Separator("-", 32);
                    Output.WriteLine("Finished download queue, cleaning up...", Output.OutputTypes.INFO);
                    SystemSounds.Asterisk.Play();
                }
                else
                {
                    Output.WriteLine("Well then, this is odd... There were no items added to the queue. This really shouldn't happen, so pat yourself on the back for finding this Easter Egg.", Output.OutputTypes.ERROR);
                }
            }
            else
            {
                Output.WriteLine("The games page of that profile appears to be empty. This usually happens if Steam is having problems or the profile is private.");
                
                if (Output.YesNoPrompt("Would you like to try again?"))
                    goto DoWork;
                else
                    ExitState();
            }
            #endregion

            #region Cleanup
            Cleanup(out int foldersCleaned);
            Output.WriteLine(foldersCleaned > 0 ? $"Finished cleanup, removed {foldersCleaned} empty folder(s)." : "Finished cleanup", Output.OutputTypes.INFO);
            #endregion

            #region Finish
            Output.Separator("-", 32);
            if (Output.YesNoPrompt("Operation complete! Would you like to view the downloads folder?", true, Output.OutputTypes.SUCCESS))
                Process.Start(DownloadFolder);

            ExitState();
            #endregion
        }

        static void PrepareDownload(int id, int imageType)
        {
            string gameDirectory = Path.Combine(DownloadFolder, id.ToString());

            if (!Directory.Exists(gameDirectory))
                Directory.CreateDirectory(gameDirectory);

            string imageName = Images[imageType].Key;
            string imageBase = Images[imageType].Value;
            string imageUrl = imageBase + id + "/" + imageName;
            string downloadDest = Path.Combine(DownloadFolder, id.ToString(), imageName);

            Queue.Add(new KeyValuePair<string, string>(imageUrl, downloadDest));
        }

        static async Task DownloadQueue()
        {
            int i = 1;
            foreach (KeyValuePair<string, string> item in Queue)
            {
                string url = item.Key;
                string dest = item.Value;
                if (!File.Exists(dest))
                {
                    int status = await Request.Download(url, dest);

                    if (status == 200)
                        Output.WriteLine($"[{i}/{Queue.Count}] Downloaded {url}", Output.OutputTypes.SUCCESS);
                    else
                        Output.WriteLine($"[{i}/{Queue.Count}] Failed to download {url} - {status}", Output.OutputTypes.WARN);

                    if (ThrottleDownloads)
                        await Task.Delay(250);
                }
                else
                {
                    Output.WriteLine($"[{i}/{Queue.Count}] Skipping {url} as it already exists", Output.OutputTypes.INFO);
                }

                i++;
            }
        }

        static void Cleanup(out int numDeleted)
        {
            numDeleted = 0;
            Stack<string> folders = new Stack<string>();

            folders.Push(DownloadFolder);

            while (folders.Count > 0)
            {
                string currentFolder = folders.Pop();
                string[] subFolders = Directory.GetDirectories(currentFolder);
                string[] files = Directory.GetFiles(currentFolder);

                if (files.Length == 0 && currentFolder != DownloadFolder)
                {
                    Directory.Delete(currentFolder);
                    numDeleted++;
                }

                foreach (string str in subFolders)
                    folders.Push(str);
            }
        }

        static void ExitState()
        {
            Output.Separator();
            Output.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
