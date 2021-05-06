using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class OgarHandler : RegexHandler {
        protected override Regex NeededRegex => new Regex("^(я ог(а)?р)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "я огар");
        }
    }
}