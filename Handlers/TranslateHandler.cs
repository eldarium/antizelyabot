using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot.Handlers
{
    public class TranslateHandler : RateHandler
    {
        private readonly Regex regex = new Regex(@"^бот,?( сколько)?( сейчас)?( будет)? (.+?) (доллар|бакс|гр|евр|бит)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex => regex;
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient _client)
        {
            var values = await GetRatesValuesPrivat();
            var match = NeededRegex.Match(message.CurrentMessage);
            if (!match.Success)
                match = NeededRegex.Match(message.MessageInLayouts[1]);
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
    }
}