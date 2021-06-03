using System;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot.Handlers
{
    public class ShittyStickerHandler : BaseHandler
    {
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.Type == MessageType.Sticker && message.Sticker != null)
            {
                Console.WriteLine($"[{message.Chat.Id} ({message.Chat.Title})] {message.From.Username}: [sticker] {message.Sticker.SetName} emoji {message.Sticker.Emoji} file id {message.Sticker.FileId}");
                if (((message.Sticker.SetName?.Contains("Sharij", StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (message.Sticker.SetName?.Contains("Shariy", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}