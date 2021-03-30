using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RegexHandler : BaseHandler
    {
        private Regex _regex = new Regex("(?!)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected virtual Regex NeededRegex => _regex;
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