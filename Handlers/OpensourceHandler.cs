using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class OpensourceHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^(open(-)source|oss|опенсурс|опенсорс)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "https://github.com/eldarium/antizelyabot");
        }
    }
}