using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

namespace ZelyaDushitelBot.Handlers
{
    public class AutodestructionHandler : RegexHandler
    {
        protected override Regex NeededRegex => new Regex("^autodestruction$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendPhotoAsync(message.Chat.Id, new InputOnlineFile("AgACAgIAAxkBAAIEC2MBRmwOU7kWN86aJcaUm5Pz4IsBAAJNwTEbrFAJSMU5qQYb_d9bAQADAgADeQADKQQ"));
        }
    }
}