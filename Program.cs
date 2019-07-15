using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YoutubeExplode;
using File = System.IO.File;

namespace ZelyaDushitelBot
{
    class Program
    {
        static string Token = "";
        static readonly Regex YoutubeRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly YoutubeClient YoutubeClient = new YoutubeClient();
        static ITelegramBotClient _client;
        static readonly ConcurrentBag<string> BannedAuthors = new ConcurrentBag<string>();
        static string _lastAuthor = "";
        private static int _lastNewAuthorMessageId = -1;
        private static int _vidosNumber = 0;
        private static DateTime _runDate = DateTime.Now;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            UpdateAuthors();
            GetLastDate();
            Token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();
            _client = new TelegramBotClient(Token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessageEdited;
            _client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.Read();
            File.WriteAllText(AppContext.BaseDirectory + "authors.txt", string.Join("\r\n", BannedAuthors.ToArray()));
        }

        static async void AddOffenceAndRemove(Message message)
        {
            if (DateTime.Now.Date > _runDate.Date)
            {
                _vidosNumber = 0;
                _runDate = DateTime.Now;
            }
            _vidosNumber++;
            if (_vidosNumber > 1)
            {
                if (_vidosNumber == 2)
                    await File.WriteAllTextAsync(AppContext.BaseDirectory + "date.txt", DateTime.Now.ToString());
                await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }

        static void UpdateAuthors()
        {
            var text = File.ReadAllText(AppContext.BaseDirectory + "authors.txt");
            foreach (var item in text.Split(Environment.NewLine).ToList())
            {
                BannedAuthors.Add(item);
            }
        }

        static async void GetLastDate()
        {
            if (!File.Exists(AppContext.BaseDirectory + "date.txt"))
                await File.WriteAllTextAsync(AppContext.BaseDirectory + "date.txt", DateTime.Now.ToString());
            var date = await File.ReadAllTextAsync(AppContext.BaseDirectory + "date.txt");
            DateTime datetime = DateTime.Parse(date);
            if (datetime.Date == DateTime.Now.Date)
                _vidosNumber = 1;
        }

        static async void OnMessageEdited(object sender, MessageEventArgs e)
        {
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                var isBanned = await FindYoutube(e.Message);
                if (isBanned)
                {
                    AddOffenceAndRemove(e.Message);
                }
            }
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Message.Type == MessageType.Video)
                    AddOffenceAndRemove(e.Message);
                Chat probableChat;
                if ((probableChat = e.Message.ForwardFromChat) != null)
                {
                    if (probableChat.Title.Contains("шари", StringComparison.InvariantCultureIgnoreCase))
                        AddOffenceAndRemove(e.Message);
                }
            }
            if (e.Message.Type == MessageType.Sticker && e.Message.Sticker != null)
            {
                Console.WriteLine(e.Message.From.Username + ": [sticker] " + e.Message.Sticker.SetName);

                if ((e.Message.Sticker.SetName == "SharijNeGonit" || e.Message.Sticker.SetName == "ShariyFunsMemes"))
                {
                    await _client.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                }
            }
            if (string.IsNullOrEmpty(e.Message.Text)) return;
            Console.WriteLine(e.Message.From.Username + ": " + e.Message.Text);
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                var isBanned = await FindYoutube(e.Message);
                if (isBanned)
                {
                    AddOffenceAndRemove(e.Message);
                    return;
                }
            }
            if (e.Message.From.Username.Contains("Eldarium", StringComparison.OrdinalIgnoreCase))
            {
                switch (e.Message.Text)
                {
                    case "/command1":
                    case "/command1@PolitikaDushitelBot":
                        // add last video's author to banned authors
                        if (!string.IsNullOrEmpty(_lastAuthor))
                        {
                            BannedAuthors.Add(_lastAuthor);
                            await _client.DeleteMessageAsync(e.Message.Chat.Id, _lastNewAuthorMessageId);
                        }
                        break;
                    case "/command2":
                    case "/command2@PolitikaDushitelBot":
                        // forgot lulw
                        await _client.SendTextMessageAsync(e.Message.Chat.Id, "я короче придумал фичу но пока писал остальной код забыл ее хдддд");
                        break;
                }
            }
        }

        static async Task<bool> FindYoutube(Message message)
        {
            if (YoutubeRegex.IsMatch(message.Text))
            {
                var url = YoutubeRegex.Matches(message.Text)[0].Value.Trim('/');
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
                var videoInfo = await YoutubeClient.GetVideoAsync(id);
                var author = videoInfo.Author;
                _lastAuthor = author;
                _lastNewAuthorMessageId = message.MessageId;
                var isBanned = BannedAuthors.Contains(author);
                Console.WriteLine($"{author} @ {DateTime.Now} - {isBanned}");
                return isBanned;
            }
            return false;
        }
    }
}
