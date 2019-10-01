using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Unicode;
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
        static readonly Regex RateRegex = new Regex(@"^(ч(е|ё) с курсом|курс)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex YoutubeRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex BotTranslateRegex = new Regex(@"^бот, сколько( сейчас)?( будет)? (\d+) (доллар|бакс|гр|евр|бит)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly MyHttpHandler MyHttpHandler = new MyHttpHandler(new HttpClientHandler());
        static readonly YoutubeClient YoutubeClient = new YoutubeClient(/*new HttpClient(MyHttpHandler)*/);
        static readonly string[] Stickers = new string[]{"CAADAgADBAAD9SbqFq83NbkmenTRFgQ",
                                                         "CAADAgADBQAD9SbqFjlymYiX2Bj7FgQ",
                                                         "CAADAgADBgAD9SbqFoVc73WZyzaDFgQ",
                                                         "CAADAgADCQAD9SbqFsQl4dm30mvRFgQ"};
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
            Console.ReadLine();
            File.WriteAllText(AppContext.BaseDirectory + "authors.txt", string.Join("\r\n", BannedAuthors.ToArray()));
            File.WriteAllText(AppContext.BaseDirectory + "channels.txt", string.Join("\r\n", BannedChannels.ToArray()));
            File.WriteAllText(AppContext.BaseDirectory + "failed_videos.txt", string.Join("\r\n\r\n",
                FailedVideos.Select(a => $"{a.Key} info:\r\n{a.Value}").ToArray())
            );
        }

        static async void AddOffence(Message message)
        {
            if (DateTime.Now.Date > _runDate.Date)
            {
                _vidosNumber = 0;
                _runDate = DateTime.Now;
            }
            _vidosNumber++;
            await _client.SendStickerAsync(message.Chat.Id, Stickers[new Random((int)DateTime.Now.Ticks).Next(0, 4)], replyToMessageId: message.MessageId);
            if (_vidosNumber > 1)
            {
                if (_vidosNumber == 2)
                    await File.WriteAllTextAsync(AppContext.BaseDirectory + "date.txt", DateTime.Now.ToString());

            }
        }

        static async void Remove(Message message) => await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);

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
                var isBanned = /* await FindYoutube(e.Message) ||*/ e.Message.Entities != null && e.Message.Entities.Any(en => en.Type == MessageEntityType.Url);
                if (isBanned)
                {
                    AddOffence(e.Message);
                }
            }
            var st = e.Message.Text.IndexOf("@PolitikaDushitelBot");
            if (st >= 0)
            {
                var newText = (e.Message.Text.Replace("@PolitikaDushitelBot", "")).Trim();
                if (RateRegex.IsMatch(newText))
                {
                    GetExchangeRates(e.Message);
                    return;
                }
            }
            if (RateRegex.IsMatch(e.Message.Text))
            {
                GetExchangeRates(e.Message);
                return;
            }
        }
        static private HttpStatusCode _lastStatusCode;
        static async Task<List<(string, decimal, decimal)>> GetRatesValues()
        {
            HttpClient client = new HttpClient();
            List<(string, decimal, decimal)> list = new List<(string, decimal, decimal)>();
            client.BaseAddress = new Uri("https://api.privatbank.ua/p24api/pubinfo?exchange&coursid=5");
            HttpResponseMessage v;
            try
            {
                v = await client.GetAsync("");
            }
            catch
            {
                throw;
            }
            if (v.IsSuccessStatusCode)
            {
                var p = await v.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(p);
                string xpath = "exchangerates/row/exchangerate";
                var nodex = doc.SelectNodes(xpath);
                for (int i = 0; i < nodex.Count; i++)
                {
                    if (nodex[i].Attributes["ccy"] == null || nodex[i].Attributes["ccy"].Value == "RUR") continue;
                    list.Add((nodex[i].Attributes["ccy"].Value,
                    Math.Round(decimal.Parse(nodex[i].Attributes["buy"].Value.Replace('.', ',')), decimals: 3),
                    Math.Round(decimal.Parse(nodex[i].Attributes["sale"].Value.Replace('.', ',')), decimals: 3)));
                }
            } else{
                _lastStatusCode = v.StatusCode;
            }
            return list;
        }

        static async void GetExchangeRates(Message m)
        {

            HttpResponseMessage v;
            List<(string, decimal, decimal)> values;
            try
            {
                values = await GetRatesValues();
            }
            catch (Exception e)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает приват курс! спасибо зеленский");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
                return;
            }
            if (values.Any())
            {
                var rates = "";
                for (int i = 0; i < values.Count; i++)
                {
                    rates += $"{values[i].Item1} (приват)\nПродажа {values[i].Item2}\nПокупка {values[i].Item3}";
                    rates += "\n\n";
                }
                await _client.SendTextMessageAsync(m.Chat.Id, rates, disableNotification: true);
                return;
            }
            else
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"Еще нет курса на сегодня (наверное): статус ответа {_lastStatusCode}");
            }
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Message.Type == MessageType.Video)
                {
                    AddOffence(e.Message);
                    //Remove(e.Message);
                }
                Chat probableChat;
                if ((probableChat = e.Message.ForwardFromChat) != null)
                {
                    if (BannedChannels.Contains(probableChat.Id))
                    {
                        AddOffence(e.Message);
                        Remove(e.Message);
                    }
                    _lastChannelId = probableChat.Id;
                    _lastNewChannedMessageId = e.Message.MessageId;
                }
            }
            if (e.Message.Type == MessageType.Sticker && e.Message.Sticker != null)
            {
                Console.WriteLine(e.Message.From.Username + ": [sticker] " + e.Message.Sticker.SetName + $" emoji {e.Message.Sticker.Emoji}" + " file id " + e.Message.Sticker.FileId);

                if ((e.Message.Sticker.SetName.Contains("Sharij", StringComparison.OrdinalIgnoreCase) || e.Message.Sticker.SetName.Contains("Shariy", StringComparison.OrdinalIgnoreCase)))
                {
                    await _client.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                }
            }
            if (string.IsNullOrEmpty(e.Message.Text)) return;
            Console.WriteLine(e.Message.From.Username + ": " + e.Message.Text);
            if (e.Message.From.Username.Contains("alexvojander", StringComparison.OrdinalIgnoreCase))
            {
                var isBanned = /* await FindYoutube(e.Message) ||*/ e.Message.Entities != null && e.Message.Entities.Any(en => en.Type == MessageEntityType.Url);
                if (isBanned)
                {
                    AddOffence(e.Message);
                    //Remove(e.Message);
                    return;
                }
            }
            var st = e.Message.Text.IndexOf("@PolitikaDushitelBot");
            if (st >= 0)
            {
                var newText = (e.Message.Text.Replace("@PolitikaDushitelBot", "")).Trim();
                if (RateRegex.IsMatch(newText))
                {
                    GetExchangeRates(e.Message);
                    return;
                }
            }
            if (RateRegex.IsMatch(e.Message.Text))
            {
                GetExchangeRates(e.Message);
                return;
            }
            if(BotTranslateRegex.IsMatch(e.Message.Text)){
                var values = await GetRatesValues();
                var match = BotTranslateRegex.Match(e.Message.Text);       
                decimal valueNumber;
                if(!decimal.TryParse(match.Groups[3].Value, out valueNumber)){
                        await _client.SendTextMessageAsync(e.Message.Chat.Id,
                            "не могу понять число");
                            return;
                }
                try{
                if(match.Groups[4].Value.Equals("доллар", StringComparison.InvariantCultureIgnoreCase) || match.Groups[4].Value.Equals("бакс", StringComparison.InvariantCultureIgnoreCase)){
                    var rate = values.First(v=>v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, $"{valueNumber} USD\nПродать: {valueNumber * rate.Item2} грн\nКупить: {valueNumber * rate.Item3} грн");
                }
                if(match.Groups[4].Value.Equals("евр", StringComparison.InvariantCultureIgnoreCase)){
                    var rate = values.First(v=>v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, $"{valueNumber} EUR\nПродать: {valueNumber * rate.Item2} грн\nКупить: {valueNumber * rate.Item3} грн");
                }
                if(match.Groups[4].Value.Equals("гр", StringComparison.InvariantCultureIgnoreCase)){
                    var rate1 = values.First(v=>v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                    var rate2 = values.First(v=>v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, $"{valueNumber} грн\nВ баксах: {Math.Round(valueNumber / rate1.Item3, 2)} USD\nВ евро: {Math.Round(valueNumber / rate2.Item3, 2)} EUR");
                }
                if(match.Groups[4].Value.Equals("бит", StringComparison.InvariantCultureIgnoreCase)){
                    var rate = values.First(v=>v.Item1.Equals("BTC", StringComparison.InvariantCultureIgnoreCase));
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, $"{valueNumber} BTC\nПродать: {valueNumber * rate.Item2} USD\nКупить: {valueNumber * rate.Item3} USD");
                }
                } catch(OverflowException){
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "не балуйся");
                }
            }
            switch (e.Message.Text)
            {
                case "/command4":
                case "/command4@PolitikaDushitelBot":
                    await _client.SendTextMessageAsync(e.Message.Chat.Id,
                        $"эта команда подкидывает маму зелика - результат {new Random().Next(0, 2) == 1}");
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
                        _vidosNumber++;
                        await _client.SendTextMessageAsync(e.Message.Chat.Id,
                            "@alexvojander первая за сегодня добавлена рофланЕбало");
                        break;
                }
            }
        }

        static async Task<bool> FindYoutube(Message message)
        {
            if (YoutubeRegex.IsMatch(message.Text))
            {
                return true;
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
                    Console.WriteLine("video success!");
                }
                catch (Exception e)
                {
                    var ex = e as HttpRequestException;
                    await _client.SendTextMessageAsync(new ChatId(91740825), $"Video {id} failed : {e}", disableNotification: true);
                    Console.WriteLine($"Video {id} failed : {e}");
                    FailedVideos.GetOrAdd(id, $"message from {message.From} id {message.MessageId}" +
                                              $"at {DateTime.Now}\r\n" +
                                              $"exception {e}\r\n" +
                                              $"{e.StackTrace}");
                    Thread.Sleep(1000);
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
