using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class WhenToJoinHandler : RegexHandler {
        protected override Regex NeededRegex => new Regex("^(скаж(е|и)те (, )?)?(когда заходить)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "сейчас", replyToMessageId:message.MessageId);
        }
    }
}