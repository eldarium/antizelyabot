using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RoshanHandler : BaseHandler
    {
        private readonly Regex RoshanRegex = new Regex(@"^я попала бы в рошана$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(RoshanRegex))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "С ЗАКРЫТЫМИ ГЛАЗАМИ");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}