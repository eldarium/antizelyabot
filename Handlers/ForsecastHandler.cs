using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class ForecastHandler : BaseHandler
    {
        static readonly Regex BotForecastRegex = new Regex(@"^бот,? ?прогноз (.+?)$");
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(BotForecastRegex))
            {
                var we = new WeatherWorker();
                var mm = BotForecastRegex.Match(message.CurrentMessage);
                if (!mm.Success)
                    mm = BotForecastRegex.Match(message.MessageInLayouts[1]);
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
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}