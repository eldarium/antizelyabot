using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class JuggerHandler : BaseHandler
    {
        private readonly Regex NeededRegex = new Regex(@"^джаг(г)?ер$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "я джаггернаут, СУКА");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}