using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ZelyaDushitelBot.Handlers
{
    public class RateHandler : RegexHandler
    {
        private class MonoCurrencyInfo
        {
            public int CurrencyCodeA { get; set; }
            public int CurrencyCodeB { get; set; }
            public long Date { get; set; }
            public double? RateSell { get; set; }
            public double? RateBuy { get; set; }
            public double? RateCross { get; set; }
        }
        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class PrivatCurrencyInfo
    {
        public string ccy { get; set; }
        public string base_ccy { get; set; }
        public string buy { get; set; }
        public string sale { get; set; }
    }


        private HttpStatusCode _lastStatusCode;
        private readonly Regex regex = new Regex(@"^(ч(е|ё) с курсом|курс|rehc)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }
        protected override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            GetExchangeRates(message.OriginalMessage, client);
        }

        private async void GetExchangeRates(Message m, ITelegramBotClient _client)
        {
            List<(string, decimal, decimal)> values = new List<(string, decimal, decimal)>();
            var rates = "";
            try
            {
                // TODO fix
                values = await GetRatesValuesPrivat();
            }
            catch (Exception e)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает приват курс! спасибо путлер");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
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
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает моно курс! спасибо путлер");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
            }
            if (values.Any())
            {
                for (var i = 0; i < values.Count; i++)
                {
                    rates += $"{values[i].Item1} (моно)\nПродажа {values[i].Item2}\nПокупка {values[i].Item3}";
                    rates += "\n\n";
                }
            }
            decimal bananRate = -1;
            try
            {
                bananRate = await GetRateBanan();
            }
            catch (Exception e)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, $"не возвращает банан курс! спасибо маск");
                await _client.SendTextMessageAsync(new ChatId(91740825), e + "", disableNotification: true);
            } 
            if (bananRate > 0) {
                rates += $"Курс банана (USDTUAH): {bananRate}";
            }
            if (rates != string.Empty)
            {
                await _client.SendTextMessageAsync(m.Chat.Id, rates, disableNotification: true);
                return;
            }
            await _client.SendTextMessageAsync(m.Chat.Id, $"Еще нет курса на сегодня (наверное): статус ответа {_lastStatusCode}");
        }

        protected async Task<List<(string, decimal, decimal)>> GetRatesValuesPrivat()
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
                var rates = JsonConvert.DeserializeObject<PrivatCurrencyInfo[]>(p);
                for (var i = 0; i < rates.Length; i++)
                {
                    list.Add((rates[i].ccy,
                    Math.Round(decimal.Parse(rates[i].buy, new CultureInfo("us-US")), decimals: 3),
                    Math.Round(decimal.Parse(rates[i].sale, new CultureInfo("us-US")), decimals: 3)));
                }
            }
            else
            {
                _lastStatusCode = v.StatusCode;
            }
            return list;
        }

        protected async Task<List<(string, decimal, decimal)>> GetRatesValuesMono()
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
        
        private async Task<decimal> GetRateBanan() {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.binance.com/api/v3/avgPrice?symbol=USDTUAH");
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
                var rate2 = new {mins = -1,price = 0M};
                var rate = JsonConvert.DeserializeAnonymousType(p, rate2);
                return rate.price;
            }
            else
            {
                _lastStatusCode = v.StatusCode;
            }
            return -1;
        }
    }
}