using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

namespace ZelyaDushitelBot.Handlers
{
    public class DuceHandler : RegexHandler
    {
        protected override Regex NeededRegex => new Regex("^дуче$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            await client.SendDocumentAsync(message.Chat.Id,
             new InputOnlineFile("CgACAgIAAx0CVP-Q_wACyIRj5XTOk0bXH-z5dCZPZyHQDLnuqgACviMAAhi4iEj-MvvAkeudri4E"));
        }
    }
}