using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;
using System.Web;

class Program
{
    static string authToken = "4a638636a9a7d2b5-c55c32572b366f6a-f6d14fc688f52727";
    static string adminReceiverId = "By6xPkauA5vN9EuQbo5g5A==";
    static List<string> receiverIds = [adminReceiverId, "SbPiUX9+UlMHhKy+d/qz5Q=="];
    static string track_url = "http://www.ghb.by/ru/construction/price_apartments/";
    static string _filePath = "links.txt";
    static string _fileErrorsPath = "errors.txt";
    static DateTime _lastSuccess = DateTime.Now;

    static async Task Main()
    {
        Console.WriteLine("Service started!");
        while (true)
        {
            try
            {
                using var client = new HttpClient();
                var currentState = await (await client.GetAsync(track_url)).Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(currentState);
                var currentLinks = doc.DocumentNode.SelectSingleNode("//div[@class='content']").SelectNodes(".//a").Select(l => HttpUtility.HtmlDecode(l.InnerText.Trim())).ToList();

                var prevLinks = ReadLinks();
                var newLinks = prevLinks != null ? currentLinks.Except(prevLinks).ToList() : [];
                if (newLinks.Any())
                {
                    foreach (var receiverId in receiverIds)
                    {
                        await SendToViber($"Go to {track_url} now! New links:\r\n\r\n {string.Join("\r\n\r\n", newLinks)}", receiverId);
                    }
                }

                WriteLinks(currentLinks.ToArray());
                _lastSuccess = DateTime.Now;
            }
            catch (Exception ex)
            {
                AppendError(ex);
                if ((DateTime.Now - _lastSuccess).TotalHours > 3)
                {
                    await SendToViber("Service stopped!", adminReceiverId);
                    Console.WriteLine("Service stopped!");
                    break;
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(10));
        }
    }

    static async Task SendToViber(string text, string receiver)
    {
        try
        {
            var payload = new
            {
                receiver = receiver,
                min_api_version = 1,
                sender = new { name = "MyBot" },
                tracking_data = "tracking data",
                type = "text",
                text = text
            };

            string json = System.Text.Json.JsonSerializer.Serialize(payload);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Viber-Auth-Token", authToken);

            await client.PostAsync(
                "https://chatapi.viber.com/pa/send_message",
                new StringContent(json, Encoding.UTF8, "application/json")
            );
        }
        catch (Exception ex)
        {
            AppendError(ex);
        }
    }

    static void WriteLinks(string[] lines)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(_filePath))
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }
        catch (Exception ex)
        {
            AppendError(ex);
        }
    }

    static string[] ReadLinks()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            return File.ReadAllLines(_filePath);
        }
        catch (Exception ex)
        {
            AppendError(ex);
            return Array.Empty<string>();
        }
    }

    static void AppendError(Exception error)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(_fileErrorsPath, true))
            {
                writer.WriteLine($"{DateTime.Now:G} {error.Message}");
                writer.WriteLine(JsonConvert.SerializeObject(error));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:G} Error when write error: {ex.Message} \n\r {ex.InnerException?.Message}");
        }
    }
}
