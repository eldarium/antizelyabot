using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class PaporotnikSmallHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^ну тут не поспоришь$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "НУ ТУТ НЕ ПОСПОРИШЬ");
        }
    }
}