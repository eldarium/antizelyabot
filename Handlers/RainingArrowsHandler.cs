using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RainingArrowsHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^дождь из стрел(\?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            var origPhrase = "дОжДь Из сТрЕл???";
            StringBuilder rainingArrowsSB = new StringBuilder();
            foreach (var letter in origPhrase.Select(a => a.ToString()))
            {
                rainingArrowsSB.Append(new Random().Next(0, 2) == 1 ? letter.ToUpper() : letter.ToLower());
            }
            await client.SendTextMessageAsync(message.Chat.Id, rainingArrowsSB.ToString());
        }
    }
}