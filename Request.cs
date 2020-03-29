using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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
    public class Request
    {
        public static async Task<string> GetContent(string url)
        {
            using (var client = new HttpClient())
            {
                var res = await client.GetStringAsync(url);
                return res;
            }
        }

        public static async Task<int> Download(string url, string destination)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");

                var download = await client.GetAsync(url);
                var data = await download.Content.ReadAsByteArrayAsync();

                if (download.IsSuccessStatusCode)
                    File.WriteAllBytes(destination, data);

                return (int)download.StatusCode;
            }
        }
    }
}
