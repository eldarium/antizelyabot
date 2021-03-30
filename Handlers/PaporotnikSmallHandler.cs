using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class PaporotnikSmallHandler : BaseHandler
    {
        private readonly Regex NeededRegex = new Regex(@"^ну тут не поспоришь$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "НУ ТУТ НЕ ПОСПОРИШЬ");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}