using System;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot.Handlers
{
    public class ConsoleWriterHandler : BaseHandler
    {
        public override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.Type == MessageType.Sticker && message.Sticker != null)
            {
                Console.WriteLine($"[{message.Chat.Id} ({message.Chat.Title})] {message.From.Username}: [sticker] {message.Sticker.SetName} emoji {message.Sticker.Emoji} file id {message.Sticker.FileId}");
            }
            if (message.CurrentMessage != null)
            {
                Console.WriteLine($"[{message.Chat.Id} ({message.Chat.Title})] {message.From.Username}: {message.CurrentMessage}");
            }
            NextHandler?.Handle(message, client);
        }
    }
}