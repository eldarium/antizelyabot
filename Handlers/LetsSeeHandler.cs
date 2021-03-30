using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class LetsSeeHandler : BaseHandler
    {
        private object lockObject = new object();
        private int letsSeeCooldown = 2;
        static readonly Regex NeededRegex = new Regex(@"^(ну )?(посмотрим|поглядим|увидим|пожив[её]м(-| )увидим)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                lock (lockObject)
                {
                    if (letsSeeCooldown > 0)
                    {
                        letsSeeCooldown--;
                        return;
                    }
                    letsSeeCooldown = 2;
                }
                await client.SendTextMessageAsync(message.Chat.Id, "а там видно будет");
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}