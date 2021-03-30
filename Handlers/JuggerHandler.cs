using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class JuggerHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^джаг(г)?ер$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "я джаггернаут, СУКА");
        }
    }
}