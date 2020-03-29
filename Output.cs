using System;
using System.Linq;

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
    public class Output
    {
        public enum OutputTypes
        {
            DEBUG,
            INFO,
            SUCCESS,
            WARN,
            ERROR
        }

        /// <summary>
        /// Accepts yes or no from user input
        /// </summary>
        /// <returns>true if yes, false if no</returns>
        public static bool YesNoPrompt(string preface = null, bool defaultChoice = false, OutputTypes type = OutputTypes.DEBUG)
        {
            string yes = defaultChoice ? "[Y]" : "Y";
            string no  = defaultChoice ? "N" : "[N]";
            string prompt = $" {yes}/{no}:";
            if (preface != null)
                prompt = (preface + prompt);

            Write(prompt.Trim() + " ", type);
            return Console.ReadLine().Trim().ToLower() == "y";
        }

        public static void WriteLine(string text = "", OutputTypes type = OutputTypes.DEBUG)
        {
            SetColor(type);
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void Write(string text = "", OutputTypes type = OutputTypes.DEBUG)
        {
            SetColor(type);
            Console.Write(text);
            Console.ResetColor();
        }

        public static void Separator(string separator = null, int numSeparators = 16)
        {
            if (separator != null)
                Console.WriteLine(string.Concat(Enumerable.Repeat(separator, numSeparators)));
            else
                Console.WriteLine();
        }

        private static void SetColor(OutputTypes type)
        {
            ConsoleColor foreground;
            ConsoleColor background;
            switch (type)
            {
                default:
                case OutputTypes.DEBUG:
                    foreground = ConsoleColor.White;
                    background = ConsoleColor.Black;
                    break;
                case OutputTypes.INFO:
                    foreground = ConsoleColor.Blue;
                    background = ConsoleColor.Black;
                    break;
                case OutputTypes.SUCCESS:
                    foreground = ConsoleColor.Green;
                    background = ConsoleColor.Black;
                    break;
                case OutputTypes.WARN:
                    foreground = ConsoleColor.Yellow;
                    background = ConsoleColor.Black;
                    break;
                case OutputTypes.ERROR:
                    foreground = ConsoleColor.White;
                    background = ConsoleColor.Red;
                    break;
            }

            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
        }
    }
}
