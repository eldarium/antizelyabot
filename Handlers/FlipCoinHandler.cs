using System;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class FlipCoinHandler : BaseHandler
    {
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {

            if (message.HasCommand("/command4"))
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    $"эта команда подкидывает маму зелика - результат {new Random().Next(0, 2) == 1}");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}