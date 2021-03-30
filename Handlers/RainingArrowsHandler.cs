using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RainingArrowsHandler : BaseHandler
    {
        private readonly Regex NeededRegex = new Regex(@"^дождь из стрел(\?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                var origPhrase = "дОжДь Из сТрЕл???";
                StringBuilder rainingArrowsSB = new StringBuilder();
                foreach (var letter in origPhrase.Select(a => a.ToString()))
                {
                    rainingArrowsSB.Append(new Random().Next(0, 2) == 1 ? letter.ToUpper() : letter.ToLower());
                }
                await client.SendTextMessageAsync(message.Chat.Id, rainingArrowsSB.ToString());
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}