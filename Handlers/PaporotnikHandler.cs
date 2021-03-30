using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class PaporotnikHandler : BaseHandler
    {
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasCommand("/paporotnik"))
            {
                await client.SendTextMessageAsync(message.OriginalMessage.Chat.Id, "Встречаются два папоротника в непригодных для размножения условиях, и один другому говорит: \"Ну, тут не поспоришь\"");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}