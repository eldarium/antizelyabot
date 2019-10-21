using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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
        static readonly Regex RateRegex = new Regex(@"^(ч(е|ё) с курсом|курс|rehc)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex YoutubeRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex BotTranslateRegex = new Regex(@"^бот, сколько( сейчас)?( будет)? (\d+.?\d+?) (доллар|бакс|гр|евр|бит)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly YoutubeClient YoutubeClient = new YoutubeClient();
        static readonly string[] Stickers = {"CAADAgADBAAD9SbqFq83NbkmenTRFgQ",
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
        private static ConcurrentDictionary<string, string> FailedVideos = new ConcurrentDictionary<string, string>();
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            UpdateAuthors();
            UpdateChannels();
            GetLastDate();
            Token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();
            _client = new TelegramBotClient(Token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessage;// OnMessageEdited;
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
            var datetime = DateTime.Parse(date);
            if (datetime.Date == DateTime.Now.Date)
                _vidosNumber = 1;
        }

        private static HttpStatusCode _lastStatusCode;
        static async Task<List<(string, decimal, decimal)>> GetRatesValues()
        {
            var client = new HttpClient();
            var list = new List<(string, decimal, decimal)>();
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
                var doc = new XmlDocument();
                doc.LoadXml(p);
                var xpath = "exchangerates/row/exchangerate";
                var nodex = doc.SelectNodes(xpath);
                for (var i = 0; i < nodex.Count; i++)
                {
                    if (nodex[i].Attributes["ccy"] == null || nodex[i].Attributes["ccy"].Value == "RUR") continue;
                    list.Add((nodex[i].Attributes["ccy"].Value,
                    Math.Round(decimal.Parse(nodex[i].Attributes["buy"].Value, new CultureInfo("us-US")), decimals: 3),
                    Math.Round(decimal.Parse(nodex[i].Attributes["sale"].Value, new CultureInfo("us-US")), decimals: 3)));
                }
            }
            else
            {
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
                for (var i = 0; i < values.Count; i++)
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
            var message = e.Message;
            if (message.HasAuthor("alexvojander"))
            {
                if (message.Type == MessageType.Video)
                {
                    AddOffence(message);
                    //Remove(message);
                }
                Chat probableChat;
                if ((probableChat = message.ForwardFromChat) != null)
                {
                    if (BannedChannels.Contains(probableChat.Id))
                    {
                        AddOffence(message);
                        Remove(message);
                    }
                    _lastChannelId = probableChat.Id;
                    _lastNewChannedMessageId = message.MessageId;
                }
            }
            if (message.Type == MessageType.Sticker && message.Sticker != null)
            {
                Console.WriteLine(message.From.Username + ": [sticker] " + message.Sticker.SetName + $" emoji {message.Sticker.Emoji}" + " file id " + message.Sticker.FileId);

                if (((message.Sticker.SetName?.Contains("Sharij", StringComparison.OrdinalIgnoreCase) ?? false) || (message.Sticker.SetName?.Contains("Shariy", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
            }
            if (string.IsNullOrEmpty(message.Text)) return;
            Console.WriteLine(message.From.Username + ": " + message.Text);
            if (message.HasAuthor("alexvojander"))
            {
                var isBanned = await FindYoutube(message) || message.Entities != null && message.Entities.Any(en => en.Type == MessageEntityType.Url);
                if (isBanned)
                {
                    AddOffence(message);
                    //Remove(message);
                    return;
                }
            }
            if (message.HasRegexIgnoreMention(RateRegex))
            {
                GetExchangeRates(message);
                return;
            }
            if (message.HasRegexIgnoreMention(BotTranslateRegex))
            {
                var values = await GetRatesValues();
                var match = BotTranslateRegex.Match(message.Text);
                if (!decimal.TryParse(match.Groups[3].Value, out var valueNumber) ||
                !decimal.TryParse(match.Groups[3].Value.Replace('.', ','), out valueNumber))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id,
                        "не могу понять число");
                    return;
                }
                try
                {
                    if (match.Groups[4].Value.Equals("доллар", StringComparison.InvariantCultureIgnoreCase) || match.Groups[4].Value.Equals("бакс", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, item2, item3) = values.First(v => v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} USD\nПродать: {valueNumber * item2} грн\nКупить: {valueNumber * item3} грн");
                    }
                    if (match.Groups[4].Value.Equals("евр", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, item2, item3) = values.First(v => v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} EUR\nПродать: {valueNumber * item2} грн\nКупить: {valueNumber * item3} грн");
                    }
                    if (match.Groups[4].Value.Equals("гр", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, _, item3) = values.First(v => v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                        var (_, _, item4) = values.First(v => v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} грн\nВ баксах: {Math.Round(valueNumber / item3, 2)} USD\nВ евро: {Math.Round(valueNumber / item4, 2)} EUR");
                    }
                    if (match.Groups[4].Value.Equals("бит", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, item2, item3) = values.First(v => v.Item1.Equals("BTC", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} BTC\nПродать: {valueNumber * item2} USD\nКупить: {valueNumber * item3} USD");
                    }
                }
                catch (OverflowException)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "не балуйся");
                }
            }

            if (message.HasCommand("/command4"))
                await _client.SendTextMessageAsync(message.Chat.Id,
                    $"эта команда подкидывает маму зелика - результат {new Random().Next(0, 2) == 1}");

            if (message.HasAuthor("daneldarium"))
            {
                if (message.HasCommand("/command1"))
                {
                    // add last video's author to banned authors
                    if (!string.IsNullOrEmpty(_lastAuthor))
                    {
                        BannedAuthors.Add(_lastAuthor);
                        await _client.DeleteMessageAsync(message.Chat.Id, _lastNewAuthorMessageId);
                    }
                }
                else if (message.HasCommand("/command2"))
                {
                    // add last channel to banned channels
                    if (_lastChannelId != -1)
                    {
                        BannedChannels.Add(_lastChannelId);
                        await _client.DeleteMessageAsync(message.Chat.Id, _lastNewChannedMessageId);
                    }
                }
                else if (message.HasCommand("/command3"))
                {
                    _vidosNumber++;
                    await _client.SendTextMessageAsync(message.Chat.Id,
                        "@alexvojander первая за сегодня добавлена рофланЕбало");
                }
            }
        }

        static async Task<bool> FindYoutube(Message message)
        {
            if (YoutubeRegex.IsMatch(message.Text))
            {
                var url = YoutubeRegex.Matches(message.Text)[0].Value.Trim('/');
                var uri = new Uri("https://" + url);
                var id = "";
                var q = HttpUtility.ParseQueryString(uri.Query);
                if (q.AllKeys.Contains("v"))
                {
                    id = q["v"];
                }
                else
                {
                    id = uri.Segments.Last();
                }
                var videoInfo = new YoutubeExplode.Models.Video(
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
                    return true;
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
