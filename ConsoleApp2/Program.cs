using System.Text.Json;

namespace FileDownloader
{
    internal class Program
    {
        private static string? _downloadFileName = "";
        static async Task Main(string[] args)
        {
            #region Get URL 

            using var http = new HttpClient();

            string dateFormatted = DateTime.Today.ToString("yyyy-MM-dd");
            string url = $"https://echo.tradelab.co.in/api/file/status/all?date={dateFormatted}";

            string json = await http.GetStringAsync(url);

            string? link = GetFileUrl(json);

            if (string.IsNullOrWhiteSpace(link))
            {
                Console.WriteLine("No matching file found.");
                return;
            }

            #endregion

            #region Download File Name & Save Path

            Console.WriteLine($"Downloading file: {_downloadFileName}");

            string folderPath = File.ReadAllText("settings.txt").Trim();

            if (!folderPath.EndsWith("\\"))
                folderPath += "\\";

            string savePath = Path.Combine(folderPath, _downloadFileName);

            Console.WriteLine($"Saved to: {savePath}");

            if (string.IsNullOrWhiteSpace(savePath))
            {
                Console.WriteLine("Invalid path. Exiting.");
                return;
            }

            #endregion

            #region Download the File

            byte[] fileBytes = await http.GetByteArrayAsync(link);
            await File.WriteAllBytesAsync(savePath, fileBytes);

            #endregion
            
            Console.WriteLine("\nHURRAY!!! Download complete.");
            Console.ReadLine();
        }

        // ---------------------------------------------------------
        //  Core logic with recursion (cleaned, not a crime scene)
        // ---------------------------------------------------------
        static string? GetFileUrl(string json)
        {
            var root = JsonSerializer.Deserialize<Root>(json);
            var items = root?.data?.response;

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("No data found in JSON.");
                return null;
            }

            // Group by exchange
            var exchanges = items
                .GroupBy(x => x.exchange)
                .ToDictionary(g => g.Key!, g => g.ToList());

            Console.WriteLine("Available exchanges:");
            int iter = 1;
            foreach (var ex in exchanges.Keys)
                Console.WriteLine($"{iter++} - {ex}");

            Console.WriteLine("\n1 - Enter file name directly");
            Console.WriteLine("2 - Browse by exchange");
            Console.Write("Enter: ");
            string? choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.Write("Enter the exact filename: ");
                string? name = Console.ReadLine();

                var item = items.FirstOrDefault(x =>
                    x.downloaded_file_name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (item == null)
                {
                    if (AskRetry(name))
                        return GetFileUrl(json);
                    return null;
                }

                _downloadFileName = item.downloaded_file_name;

                return item.file_url;
            }

            if (choice == "2")
            {
                Console.Write("Enter exchange name or number: ");
                string? entry = Console.ReadLine();

                string? exchangeKey = null;

                // Number?
                if (int.TryParse(entry, out int exNum))
                {
                    if (exNum < 1 || exNum > exchanges.Count)
                    {
                        if (AskRetry(entry))
                            return GetFileUrl(json);
                        return null;
                    }

                    exchangeKey = exchanges.Keys.ElementAt(exNum - 1);
                }
                else
                {
                    if (!exchanges.ContainsKey(entry))
                    {
                        if (AskRetry(entry))
                            return GetFileUrl(json);
                        return null;
                    }

                    exchangeKey = entry;
                }

                var files = exchanges[exchangeKey];

                Console.WriteLine($"\nFiles under: {exchangeKey}");
                for (int i = 0; i < files.Count; i++)
                    Console.WriteLine($"{i + 1} - {files[i].downloaded_file_name}");

                Console.Write("\nEnter file number or name: ");
                string? fileEntry = Console.ReadLine();

                // Try number first
                if (int.TryParse(fileEntry, out int fileNum))
                {
                    if (fileNum < 1 || fileNum > files.Count)
                    {
                        if (AskRetry(fileEntry))
                            return GetFileUrl(json);
                        return null;
                    }

                    _downloadFileName = files[fileNum - 1].downloaded_file_name;

                    return files[fileNum - 1].file_url;
                }
                else
                {
                    var match = files.FirstOrDefault(x =>
                        x.downloaded_file_name.Equals(fileEntry, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        if (AskRetry(fileEntry))
                            return GetFileUrl(json);
                        return null;
                    }

                    _downloadFileName = match.downloaded_file_name;

                    return match.file_url;
                }
            }

            // Invalid primary choice
            if (AskRetry(choice))
                return GetFileUrl(json);

            return null;
        }

        // ---------------------------------------------------------
        // Helper: Recursion-safe retry prompt
        // ---------------------------------------------------------
        static bool AskRetry(string? wrong)
        {
            Console.WriteLine($"\nInvalid input: {wrong}");
            Console.Write("Press 0 to retry, anything else to exit: ");

            return Console.ReadLine() == "0";
        }
    }

    // ---------------------------------------------------------
    // Models
    // ---------------------------------------------------------
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
