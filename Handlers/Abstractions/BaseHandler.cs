using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers 
{
    public abstract class BaseHandler {
        public BaseHandler NextHandler { get; set; }
        public abstract void Handle(MessageWrapper message, ITelegramBotClient client);
    }
}