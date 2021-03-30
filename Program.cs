using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Reddit;
using File = System.IO.File;
using Reddit.Controllers;
using ZelyaDushitelBot.Handlers;

namespace ZelyaDushitelBot
{
    class Program
    {
        static string Token = "";
        static readonly Regex YoutubeRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex BotTranslateRegex = new Regex(@"^бот,?( сколько)?( сейчас)?( будет)? (.+?) (доллар|бакс|гр|евр|бит)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotWeatherRegex = new Regex(@"^(бот,? )?(какая )?погода в (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotForecastRegex = new Regex(@"^бот,? ?прогноз (.+?)$");
        static readonly Regex BotWeatherSmallRegex = new Regex(@"^(бот, )?(какая )?погода(.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotCalculateRegex = new Regex(@"^(бот,? )?посчитай (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotConvertRegex = new Regex(@"^(бот,? )?конв(ертируй)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotMudroomRegex = new Regex(@"^склад грязи$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotOpensourceRegex = new Regex(@"^(open(-)source|oss|опенсурс|опенсорс)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotLetsSeeRegex = new Regex(@"^(ну )?(посмотрим|поглядим|увидим|пожив[её]м(-| )увидим)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotRedditRegex = new Regex(@"реддит|reddit (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static RedditClient redditClient;
        static int letsSeeCooldown = 0;
        static object lockObject = new object();
        static readonly string[] Stickers = {"CAADAgADBAAD9SbqFq83NbkmenTRFgQ",
                                             "CAADAgADBQAD9SbqFjlymYiX2Bj7FgQ",
                                             "CAADAgADBgAD9SbqFoVc73WZyzaDFgQ",
                                             "CAADAgADCQAD9SbqFsQl4dm30mvRFgQ"};
        static ITelegramBotClient _client;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            Token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();
            var appid = File.ReadAllText(AppContext.BaseDirectory + "appid").Trim();
            var appsecret = File.ReadAllText(AppContext.BaseDirectory + "appsecret").Trim();
            var refreshtoken = File.ReadAllText(AppContext.BaseDirectory + "refreshtoken").Trim();
            redditClient = new RedditClient(appid, refreshtoken, appsecret,userAgent:"CSharp:EldariumScript v.1.0.0 by /u/eldarium");
            _client = new TelegramBotClient(Token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessage;// OnMessageEdited;
            AppDomain.CurrentDomain.UnhandledException += async (object sender, UnhandledExceptionEventArgs args2) =>
            {
                await _client.SendTextMessageAsync(new ChatId(91740825), $"Unhandled exception!\n{args2.ExceptionObject}\n{(args2.ExceptionObject as Exception).InnerException?.Message}", disableNotification: true);
            };
            _client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.ReadLine();
        }

        static async void AddOffence(Message message)
        {
            //await _client.SendStickerAsync(message.Chat.Id, Stickers[new Random((int)DateTime.Now.Ticks).Next(0, 4)], replyToMessageId: message.MessageId);
        }

        private static HttpStatusCode _lastStatusCode;
        static async Task<List<(string, decimal, decimal)>> GetRatesValuesPrivat()
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

        static async Task<List<(string, decimal, decimal)>> GetRatesValuesMono()
        {
            var client = new HttpClient();
            var list = new List<(string, decimal, decimal)>();
            client.BaseAddress = new Uri("https://api.monobank.ua/bank/currency");
            int[] curCodes = new int[] { 980, 840, 978 };
            string[] curNames = new string[] { "UAH", "USD", "EUR" };
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
                var rates = JsonConvert.DeserializeObject<MonoCurrencyInfo[]>(p);
                foreach (var r in rates.Where(a => a.CurrencyCodeB == curCodes[0] && curCodes.Contains(a.CurrencyCodeA)))
                {
                    list.Add((curNames.ElementAt(Array.IndexOf(curCodes, r.CurrencyCodeA)),
                    ((decimal?)r.RateBuy) ?? 0,
                    ((decimal?)r.RateSell) ?? 0));
                }
            }
            else
            {
                _lastStatusCode = v.StatusCode;
            }
            return list;
        }

        class MonoCurrencyInfo
        {
            public int CurrencyCodeA { get; set; }
            public int CurrencyCodeB { get; set; }
            public long Date { get; set; }
            public double? RateSell { get; set; }
            public double? RateBuy { get; set; }
            public double? RateCross { get; set; }
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            var rh = new RateHandler();
            rh.Handle(new MessageWrapper(e.Message), _client);
            /*
            var message = e.Message;
            if (message.HasCommand("/paporotnik"))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "Встречаются два папоротника в непригодных для размножения условиях, и один другому говорит: \"Ну, тут не поспоришь\"");
                return;
            }
            if (message.Text?.Equals("Я ПОПАЛА БЫ В РОШАНА", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "С ЗАКРЫТЫМИ ГЛАЗАМИ");
            }
            if ((message.Text?.Equals("дождь из стрел?", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
            (message.Text?.Equals("дождь из стрел", StringComparison.InvariantCultureIgnoreCase) ?? false))
            {
                var origPhrase = "дОжДь Из сТрЕл???";
                StringBuilder rainingArrowsSB = new StringBuilder();
                foreach (var letter in origPhrase.Select(a=>a.ToString()))
                {
                    rainingArrowsSB.Append(new Random().Next(0,2) == 1 ? letter.ToUpper() :letter.ToLower());
                }
                await _client.SendTextMessageAsync(message.Chat.Id, rainingArrowsSB.ToString());
            }
            if (message.Text?.Equals("ну тут не поспоришь", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "НУ ТУТ НЕ ПОСПОРИШЬ");
                return;
            }
            if ((message.Text?.Contains("джаггер", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
            (message.Text?.Contains("джагер", StringComparison.InvariantCultureIgnoreCase) ?? false))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "я джаггернаут, СУКА");
                return;
            }
            if (message.HasAuthor("alexvojander"))
            {
                if (message.Type == MessageType.Video)
                {
                    AddOffence(message);
                }
                if (message.HasCommand("spam"))
                {
                    await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    return;
                }
            }
            if (message.Type == MessageType.Sticker && message.Sticker != null)
            {
                Console.WriteLine($"[{message.Chat.Id} ({message.Chat.Title})] {message.From.Username}: [sticker] {message.Sticker.SetName} emoji {message.Sticker.Emoji} file id {message.Sticker.FileId}");

                if (((message.Sticker.SetName?.Contains("Sharij", StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (message.Sticker.SetName?.Contains("Shariy", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    await _client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
            }
            if (!(message.Document is null) && (message.Document?.FileName?.EndsWith("fb2") ?? false))
            {
                var ctsource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                var ru = "йцукенгшщзхъфывапролджэячсмитьбю" + "йцукенгшщзхъфывапролджэячсмитьбю".ToUpper();
                var en = "icukengsszh-fyvaproldzeacsmin-bu" + "icukengsszh-fyvaproldzeacsmin-bu".ToUpper();
                string rfn = message.Document.FileName;
                for (int i = 0; i < ru.Length; i++)
                    rfn = rfn.Replace(ru[i], en[i]);
                var bookFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rfn);
                using (var fs = File.OpenWrite(bookFilePath))
                {
                    await _client.GetInfoAndDownloadFileAsync(message.Document.FileId, fs, ctsource.Token);
                    fs.Flush();
                }
                var a = new Process();
                string outp = "";
                string newbookFilePath = null;
                Regex rr = new Regex("failed: [1-9]+?");
                try
                {
                    a.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"translator\fb2pdf.cmd");
                    a.StartInfo.Arguments = $"\"{bookFilePath}\"";
                    a.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    a.StartInfo.RedirectStandardOutput = true;
                    a.Start();
                    outp = a.StandardOutput.ReadToEnd();
                    a.WaitForExit();
                    newbookFilePath = bookFilePath.Replace("fb2", "pdf");
                    using (var fs = File.OpenRead(newbookFilePath))
                    {
                        await _client.SendDocumentAsync(message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, rfn.Replace("fb2", "pdf")));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (rr.IsMatch(outp))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "errors when converting");
                }
                File.Delete(bookFilePath);
                if (newbookFilePath != null)
                {
                    File.Delete(newbookFilePath);
                    File.Delete(bookFilePath + "pdf");
                }
            }
            if (string.IsNullOrEmpty(message.Text)) return;
            if(message.HasRegexIgnoreMention(BotRedditRegex)) {
                try {
                var subReddit = message.Text.Split(' ').Last();
                var hotposts = redditClient.Subreddit(subReddit).Posts.GetHot();
                var index = new Random().Next(0, hotposts.Count);
                var gotPost = hotposts[index];
                if (gotPost.NSFW) {
                    await _client.SendTextMessageAsync(message.Chat.Id, @"уберите от экрана женщин и детей, сейчас вылетит чья-то птичка");
                }
                var messageToSend = $"{gotPost.Title}\n\n{(gotPost.Listing.IsSelf ? ((SelfPost)gotPost).SelfText : ((LinkPost)gotPost).URL)}";
                await _client.SendTextMessageAsync(message.Chat.Id, messageToSend);
                return;
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine($"[{message.Chat.Id} ({message.Chat.Title})] {message.From.Username}: {message.Text}");
            if (message.HasAuthor("alexvojander"))
            {
                var isBanned = message.Entities != null && message.Entities.Any(en => en.Type == MessageEntityType.Url);
                if (isBanned)
                {
                    AddOffence(message);
                    return;
                }
            }
            if (message.HasRegexIgnoreMention(BotMudroomRegex))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "склад грязи");
                return;
            }
            if (message.HasRegexIgnoreMention(BotLetsSeeRegex))
            {
                lock (lockObject)
                {
                    if (letsSeeCooldown > 0)
                    {
                        letsSeeCooldown--;
                        return;
                    }
                    letsSeeCooldown = 2;
                }
                await _client.SendTextMessageAsync(message.Chat.Id, "а там видно будет");

                return;
            }
            if (message.HasRegexIgnoreMention(BotOpensourceRegex))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "https://github.com/eldarium/antizelyabot");
                return;
            }
            if (message.HasRegexIgnoreMention(BotCalculateRegex))
            {
                var match = BotCalculateRegex.Match(message.Text);
                if (!match.Success)
                    match = BotCalculateRegex.Match(message.TextToLayout());
                DataTable dt = new DataTable();
                var expr = match.Groups.Last().Value;
                try
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "" + dt.Compute(expr, null));
                }
                catch (EvaluateException eex)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "не могу посчитать");
                    Console.WriteLine(eex.ToString());
                }
                catch (Exception ex)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "чтото пошло не так " + ex.Message);
                    await _client.SendTextMessageAsync(new ChatId(91740825), $"can't calculate: {ex}", disableNotification: true);
                }
            }
            if (message.HasRegexIgnoreMention(BotTranslateRegex))
            {
                var values = await GetRatesValuesPrivat();
                var match = BotTranslateRegex.Match(message.Text);
                if (!match.Success)
                    match = BotTranslateRegex.Match(message.TextToLayout());
                if (!decimal.TryParse(match.Groups[4].Value, out var valueNumber) ||
                !decimal.TryParse(match.Groups[4].Value.Replace('.', ','), out valueNumber))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id,
                        "не могу понять число");
                    return;
                }
                try
                {
                    if (match.Groups[5].Value.Equals("доллар", StringComparison.InvariantCultureIgnoreCase) ||
                    match.Groups[5].Value.Equals("бакс", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, item2, item3) = values.First(v => v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} USD\nПродать: {valueNumber * item2} грн\nКупить: {valueNumber * item3} грн");
                    }
                    if (match.Groups[5].Value.Equals("евр", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, item2, item3) = values.First(v => v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} EUR\nПродать: {valueNumber * item2} грн\nКупить: {valueNumber * item3} грн");
                    }
                    if (match.Groups[5].Value.Equals("гр", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var (_, _, item3) = values.First(v => v.Item1.Equals("USD", StringComparison.InvariantCultureIgnoreCase));
                        var (_, _, item4) = values.First(v => v.Item1.Equals("EUR", StringComparison.InvariantCultureIgnoreCase));
                        await _client.SendTextMessageAsync(message.Chat.Id, $"{valueNumber} грн\nВ баксах: {Math.Round(valueNumber / item3, 2)} USD\nВ евро: {Math.Round(valueNumber / item4, 2)} EUR");
                    }
                    if (match.Groups[5].Value.Equals("бит", StringComparison.InvariantCultureIgnoreCase))
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
            if (message.HasRegexIgnoreMention(BotWeatherRegex) || message.HasRegexIgnoreMention(BotWeatherSmallRegex))
            {
                var we = new WeatherWorker();
                var mm = BotWeatherRegex.Match(message.Text);
                if (!mm.Success)
                    mm = BotWeatherRegex.Match(message.TextToLayout());
                if (!mm.Success)
                    mm = BotWeatherSmallRegex.Match(message.Text);
                if (!mm.Success)
                    mm = BotWeatherSmallRegex.Match(message.TextToLayout());
                var m = mm.Groups.Last();
                if (m.Value.Contains("киев", StringComparison.InvariantCultureIgnoreCase))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, we.GetWeather("kyiv"));
                }
                if (m.Value.Contains("днепр", StringComparison.InvariantCultureIgnoreCase))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, we.GetWeather("dnipro"));
                }
            }
            if (message.HasRegexIgnoreMention(BotForecastRegex))
            {
                var we = new WeatherWorker();
                var mm = BotForecastRegex.Match(message.Text);
                if (!mm.Success)
                    mm = BotForecastRegex.Match(message.TextToLayout());
                var m = mm.Groups.Last();
                if (m.Value.Contains("киев", StringComparison.InvariantCultureIgnoreCase))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, we.GetForecast("kyiv"));
                }
                if (m.Value.Contains("днепр", StringComparison.InvariantCultureIgnoreCase))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, we.GetForecast("dnipro"));
                }
            }
            if (message.HasCommand("/command4"))
                await _client.SendTextMessageAsync(message.Chat.Id,
                    $"эта команда подкидывает маму зелика - результат {new Random().Next(0, 2) == 1}");*/
        }
    }
}
