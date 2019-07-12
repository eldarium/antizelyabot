using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using YoutubeExplode;

namespace ZelyaDushitelBot
{
    class Program
    {
        const string token = "559676146:AAHaP5_hctycQiB-73KR8PKNlMueicHRdW4";
        static Regex youtubeRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        static YoutubeClient youtubeClient = new YoutubeClient();
        static ITelegramBotClient client;
        static ConcurrentBag<string> BannedAuthors;
        static string lastAuthor = "";
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            BannedAuthors = new ConcurrentBag<string>();
            UpdateAuthors();
            client = new TelegramBotClient(token);
            client.OnMessage += OnMessage;
            client.OnUpdate += OnUpdate;
            client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.Read();
            File.WriteAllText(AppContext.BaseDirectory + "authors.txt", string.Join('\n', BannedAuthors.ToArray()));
        }

        static async void UpdateAuthors()
        {
            var text = await File.ReadAllTextAsync(AppContext.BaseDirectory + "authors.txt");
            foreach (var item in text.Split('\n').ToList())
            {
                BannedAuthors.Add(item);
            }
        }

        static async void OnUpdate(object sender, UpdateEventArgs e)
        {
            if (e.Update.Type == UpdateType.EditedMessage)
            {
                if (e.Update.EditedMessage.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
                {
                    var isBanned = await FindYoutube(e.Update.EditedMessage.Text);
                    if (isBanned)
                    {
                        await client.DeleteMessageAsync(e.Update.EditedMessage.Chat.Id, e.Update.EditedMessage.MessageId);
                    }
                }
            }
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message.Text)) return;
            Console.WriteLine(e.Message.From.Username + ": " + e.Message.Text);
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                var isBanned = await FindYoutube(e.Message.Text);
                if (isBanned)
                {
                    await client.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                }
                return;
            }
            if (e.Message.From.Username.Contains("Eldarium", StringComparison.OrdinalIgnoreCase))
            {
                switch (e.Message.Text)
                {
                    case "/command1":
                    case "/command1@EldariumBot":
                        // add last video's author to banned authors
                        if (!string.IsNullOrEmpty(lastAuthor))
                            BannedAuthors.Add(lastAuthor);
                        break;
                    case "/command2":
                    case "/command2@EldariumBot":
                        // forgot lulw
                        break;
                }
            }
        }

        static async Task<bool> FindYoutube(string text)
        {
            if (youtubeRegex.IsMatch(text))
            {
                var url = youtubeRegex.Matches(text)[0].Value.Trim('/');
                Uri uri = new Uri("https://" + url);
                string id = "";
                var q = HttpUtility.ParseQueryString(uri.Query);
                if (q.AllKeys.Contains("v"))
                {
                    id = q["v"];
                }
                else
                {
                    id = uri.Segments.Last();
                }
                var videoInfo = await youtubeClient.GetVideoAsync(id);
                var author = videoInfo.Author;
                lastAuthor = author;
                var isBanned = BannedAuthors.Contains(author);
                Console.WriteLine($"{author} @ {DateTime.Now} - {isBanned}");
                return isBanned;
            }
            return false;
        }
    }
}
