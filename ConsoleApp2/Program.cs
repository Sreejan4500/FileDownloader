using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDownloader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var http = new HttpClient();

            DateTime date = DateTime.Today.Date;

            string dateFormatted = date.ToString("yyyy-MM-dd");

            string url = $"https://echo.tradelab.co.in/api/file/status/all?date={dateFormatted}";

            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            string? link = GetFileUrl(json);

            if (string.IsNullOrWhiteSpace(link))
            {
                Console.WriteLine("No matching file found.");
                return;
            }

            byte[] fileBytes = await http.GetByteArrayAsync(link);

            await File.WriteAllBytesAsync(@"F:\contract.gz", fileBytes);

            Console.WriteLine($"Downloaded: {link}");
            Console.ReadLine();
        }

        static string? GetFileUrl(string json)
        {
            var root = JsonSerializer.Deserialize<Root>(json);
            var items = root?.data?.response;

            var groupByExchanges = items?
                .GroupBy(x => x.exchange)
                .ToDictionary(g => g.Key, g => g.ToList());

            Console.WriteLine("Provided exchanges:");

            int iteration = 1;
            foreach (var group in groupByExchanges)
            {
                string exchange = group.Key;
                Console.WriteLine($"{iteration++} - {exchange}");
            }

            Console.WriteLine();
            Console.WriteLine("Do you want to enter the file name or search the exchanges?");
            Console.WriteLine("Press 1 - Enter the file name");
            Console.WriteLine("Press 2 - Search the exchanges");
            Console.Write("Enter: ");

            string? input = Console.ReadLine();
            string? downloadedFileName = "";

            if (input == "1")
            {
                Console.Write("Enter the file name (as it is): ");
                downloadedFileName = Console.ReadLine();
            }
            else if (input == "2")
            {
                Console.Write("Enter the exchange or its number from the list above: ");
                string exchange = Console.ReadLine();
                // you can expand this block later to filter by exchange
            }

            return items?
                .FirstOrDefault(x =>
                    x.downloaded_file_name.Equals(downloadedFileName, StringComparison.OrdinalIgnoreCase))
                ?.file_url;
        }
    }

    public class Root
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public int count { get; set; }
        public List<MyItem> response { get; set; }
    }

    public class MyItem
    {
        public string? downloaded_file_name { get; set; }
        public string? exchange { get; set; }
        public string? file_date_used { get; set; }
        public string? file_downloaded_at { get; set; }
        public string? file_type { get; set; }
        public string? file_url { get; set; }
    }
}
