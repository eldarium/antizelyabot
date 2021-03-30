using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class OpensourceHandler : BaseHandler
    {
        private readonly Regex NeededRegex = new Regex(@"^(open(-)source|oss|опенсурс|опенсорс)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "https://github.com/eldarium/antizelyabot");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}