using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RegexHandler : BaseHandler
    {
        protected virtual Regex NeededRegex { get; }
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
        protected virtual void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
        }
    }
}