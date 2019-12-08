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
using Newtonsoft.Json;
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
        static readonly Regex BotTranslateRegex = new Regex(@"^бот,? сколько( сейчас)?( будет)? (.+?) (доллар|бакс|гр|евр|бит)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotWeatherRegex = new Regex(@"^(бот,? )?(какая )?погода в (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotForecastRegex = new Regex(@"^бот,? ?прогноз (.+?)$");
        static readonly Regex BotWeatherSmallRegex = new Regex(@"^(бот, )?(какая )?погода$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            await _client.SendStickerAsync(message.Chat.Id, Stickers[new Random((int)DateTime.Now.Ticks).Next(0, 4)], replyToMessageId: message.MessageId);
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

        static async void GetExchangeRates(Message m)
        {
            List<(string, decimal, decimal)> values;
            var rates = "";
            try
            {
                values = await GetRatesValuesPrivat();
            }
            catch (Exception e)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает приват курс! спасибо зеленский");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
                return;
            }
            if (values.Any())
            {
                for (var i = 0; i < values.Count; i++)
                {
                    rates += $"{values[i].Item1} (приват)\nПродажа {values[i].Item2}\nПокупка {values[i].Item3}";
                    rates += "\n\n";
                }
            }
            try
            {
                values = await GetRatesValuesMono();
            }
            catch (Exception e)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает моно курс! спасибо зеленский");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
                return;
            }
            if (values.Any())
            {
                for (var i = 0; i < values.Count; i++)
                {
                    rates += $"{values[i].Item1} (моно)\nПродажа {values[i].Item2}\nПокупка {values[i].Item3}";
                    rates += "\n\n";
                }
            }
            if (rates != string.Empty)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, rates, disableNotification: true);
                return;
            }
            await _client.SendTextMessageAsync(m.Chat.Id, $"Еще нет курса на сегодня (наверное): статус ответа {_lastStatusCode}");

        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            if (message.HasAuthor("alexvojander"))
            {
                if (message.Type == MessageType.Video)
                {
                    AddOffence(message);
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
            if (string.IsNullOrEmpty(message.Text)) return;
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
            if (message.HasRegexIgnoreMention(RateRegex))
            {
                GetExchangeRates(message);
                return;
            }
            if (message.HasRegexIgnoreMention(BotTranslateRegex))
            {
                var values = await GetRatesValuesPrivat();
                var match = BotTranslateRegex.Match(message.Text);
                if (!match.Success)
                    match = BotTranslateRegex.Match(message.TextToLayout());
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
            if (message.HasRegexIgnoreMention(BotWeatherRegex))
            {
                var we = new WeatherWorker();
                var mm = BotWeatherRegex.Match(message.Text);
                if (!mm.Success)
                    mm = BotWeatherRegex.Match(message.TextToLayout());
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
                    $"эта команда подкидывает маму зелика - результат {new Random().Next(0, 2) == 1}");
        }
    }
}
