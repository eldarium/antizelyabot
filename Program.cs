using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
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
        static readonly ConcurrentBag<long> BannedChannels = new ConcurrentBag<long>();
        static string _lastAuthor = "";
        private static int _lastNewAuthorMessageId = -1;
        private static int _lastNewChannedMessageId = -1;
        private static long _lastChannelId = -1;
        private static int _vidosNumber = 0;
        private static DateTime _runDate = DateTime.Now;
        static bool isTesting;
        private static ConcurrentDictionary<string, string> FailedVideos = new ConcurrentDictionary<string, string>();
        static void Main(string[] args)
        {
            isTesting = args.Length == 1 && args[0] == "/t";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            UpdateAuthors();
            UpdateChannels();
            GetLastDate();
            Token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();
            _client = new TelegramBotClient(Token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessageEdited;
            _client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.Read();
            File.WriteAllText(AppContext.BaseDirectory + "authors.txt", string.Join("\r\n", BannedAuthors.ToArray()));
            File.WriteAllText(AppContext.BaseDirectory + "channels.txt", string.Join("\r\n", BannedChannels.ToArray()));
            File.WriteAllText(AppContext.BaseDirectory + "failed_videos.txt", string.Join("\r\n\r\n",
                FailedVideos.Select(a => $"{a.Key} info:\r\n{a.Value}").ToArray())
            );
        }

        static async void AddOffenceAndRemove(Message message)
        {
            if (DateTime.Now.Date > _runDate.Date)
            {
                _vidosNumber = 0;
                _runDate = DateTime.Now;
            }
            _vidosNumber++;
            await _client.SendStickerAsync(message.Chat.Id, "CAADAgADBAAD9SbqFq83NbkmenTRAg", replyToMessageId: message.MessageId);    
            if (_vidosNumber > 1)
            {
                if (_vidosNumber == 2)
                    await File.WriteAllTextAsync(AppContext.BaseDirectory + "date.txt", DateTime.Now.ToString());
                await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }

        static void UpdateChannels()
        {
            var text = File.ReadAllText(AppContext.BaseDirectory + "channels.txt");
            foreach (var item in text.Split(Environment.NewLine).ToList())
            {
                if (!string.IsNullOrEmpty(item))
                    BannedChannels.Add(long.Parse(item));
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
        
        static async void GetExchangeRates(Message m){
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.privatbank.ua/p24api/exchange_rates");
            var v = await client.GetAsync($"?date={DateTime.Now.Date:dd.MM.yyyy}");
            if (v.IsSuccessStatusCode)
            {
                var p = await v.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(p);
                string xpath = "exchangerates/exchangerate";
                var nodex = doc.SelectNodes(xpath);
                for (int i = 0; i < nodex.Count; i++)
                {
                    if (nodex[i].Attributes["currency"] == null || nodex[i].Attributes["currency"].Value != "USD") continue;
                    await _client.SendTextMessageAsync(m.Chat.Id, $"USD (приват)\nПродажа {Math.Round(decimal.Parse(nodex[i].Attributes["saleRate"].Value.Replace('.',',')), decimals:3)}\nПокупка {Math.Round(decimal.Parse(nodex[i].Attributes["purchaseRate"].Value.Replace('.',',')), decimals:3)}");
                    break;
                }
            } else{
                await _client.SendTextMessageAsync(m.Chat.Id, $"Еще нет курса на сегодня (наверное): статус ответа {v.StatusCode}");
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
                    if (BannedChannels.Contains(probableChat.Id))
                        AddOffenceAndRemove(e.Message);
                    _lastChannelId = probableChat.Id;
                    _lastNewChannedMessageId = e.Message.MessageId;
                }
            }
            if (e.Message.Type == MessageType.Sticker && e.Message.Sticker != null)
            {
                Console.WriteLine(e.Message.From.Username + ": [sticker] " + e.Message.Sticker.SetName);

                if ((e.Message.Sticker.SetName.Contains("Sharij", StringComparison.OrdinalIgnoreCase) || e.Message.Sticker.SetName.Contains("Shariy", StringComparison.OrdinalIgnoreCase)))
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
            switch (e.Message.Text){
                    case "че с курсом":
                    case "чё с курсом":
                    case "курс":
                        GetExchangeRates(e.Message);
                        break;     
                    case "/command4":
                    case "/command4@PolitikaDushitelBot":
                        await _client.SendTextMessageAsync(e.Message.Chat.Id,
                            $"эта команда подкидывает монетку - результат {new Random().Next(0, 2) == 1}");
                        break;           
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
                        // add last channel to banned channels
                        if (_lastChannelId != -1)
                        {
                            BannedChannels.Add(_lastChannelId);
                            await _client.DeleteMessageAsync(e.Message.Chat.Id, _lastNewChannedMessageId);
                        }
                        break;
                    case "/command3":
                    case "/command3@PolitikaDushitelBot":
                        if (_vidosNumber == 0)
                        {
                            _vidosNumber++;
                            await _client.SendTextMessageAsync(e.Message.Chat.Id,
                                "@alexvojander первая за сегодня добавлена рофланЕбало");
                        }
                        break;
                    case "test":
                        System.Threading.Timer t1 = new System.Threading.Timer(async state=>{await _client.SendTextMessageAsync(e.Message.Chat.Id,
                            $"hello!");}, null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
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
                YoutubeExplode.Models.Video videoInfo = new YoutubeExplode.Models.Video(
                    "", "", new DateTimeOffset(), "", "", new YoutubeExplode.Models.ThumbnailSet(""),
                    TimeSpan.FromSeconds(0), new List<string>(), new YoutubeExplode.Models.Statistics(0, 0, 0));
                try
                {
                    videoInfo = await YoutubeClient.GetVideoAsync(id);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Video {id} failed");
                    FailedVideos.GetOrAdd(id, $"message from {message.From} id {message.MessageId}" +
                                              $"at {DateTime.Now}\r\n" +
                                              $"exception {e}\r\n" +
                                              $"{e.StackTrace}");
                }
                var author = videoInfo.Author;
                if (!string.IsNullOrEmpty(author))
                {
                    _lastAuthor = author;
                    _lastNewAuthorMessageId = message.MessageId;
                    var isBanned = BannedAuthors.Contains(author);
                    Console.WriteLine($"{author} @ {DateTime.Now} - {isBanned}");
                    return isBanned;
                }
            }
            return false;
        }
    }
}
