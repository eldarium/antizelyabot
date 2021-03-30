using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class MudroomHandler : BaseHandler
    {
        private readonly Regex NeededRegex = new Regex(@"^склад грязи$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "склад грязи");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}