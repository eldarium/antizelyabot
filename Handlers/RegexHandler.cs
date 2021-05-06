using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public abstract class RegexHandler : BaseHandler
    {
        protected abstract Regex NeededRegex { get; }
        public override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                ConcreteRegexHandler(message, client);
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
        protected abstract void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client);
    }
}