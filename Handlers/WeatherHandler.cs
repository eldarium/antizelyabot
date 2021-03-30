using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class WeatherHandler : BaseHandler
    {
        static readonly Regex BotWeatherSmallRegex = new Regex(@"^(бот, )?(какая )?погода(.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex BotWeatherRegex = new Regex(@"^(бот,? )?(какая )?погода в (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(BotWeatherRegex) || message.HasRegexIgnoreMention(BotWeatherSmallRegex))
            {
                var we = new WeatherWorker();
                var mm = BotWeatherRegex.Match(message.CurrentMessage);
                if (!mm.Success)
                    mm = BotWeatherRegex.Match(message.MessageInLayouts[1]);
                if (!mm.Success)
                    mm = BotWeatherSmallRegex.Match(message.CurrentMessage);
                if (!mm.Success)
                    mm = BotWeatherSmallRegex.Match(message.MessageInLayouts[1]);
                var m = mm.Groups.Last();
                if (m.Value.Contains("киев", StringComparison.InvariantCultureIgnoreCase))
                {
                    await client.SendTextMessageAsync(message.Chat.Id, we.GetWeather("kyiv"));
                }
                if (m.Value.Contains("днепр", StringComparison.InvariantCultureIgnoreCase))
                {
                    await client.SendTextMessageAsync(message.Chat.Id, we.GetWeather("dnipro"));
                }
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}