using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class ForecastHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^бот,? ?прогноз (.+?)$");
        protected override Regex NeededRegex { get => regex; }
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            var we = new WeatherWorker();
            var mm = NeededRegex.Match(message.CurrentMessage);
            if (!mm.Success)
                mm = NeededRegex.Match(message.MessageInLayouts[1]);
            var m = mm.Groups.Last();
            if (m.Value.Contains("киев", StringComparison.InvariantCultureIgnoreCase))
            {
                await client.SendTextMessageAsync(message.Chat.Id, we.GetForecast("kyiv"));
            }
            if (m.Value.Contains("днепр", StringComparison.InvariantCultureIgnoreCase))
            {
                await client.SendTextMessageAsync(message.Chat.Id, we.GetForecast("dnipro"));
            }
        }
    }
}