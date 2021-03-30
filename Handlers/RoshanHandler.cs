using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RoshanHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^я попала бы в рошана$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex => regex;
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "С ЗАКРЫТЫМИ ГЛАЗАМИ");
        }
    }
}