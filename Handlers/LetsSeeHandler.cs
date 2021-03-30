using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class LetsSeeHandler : RegexHandler
    {
        private object lockObject = new object();
        private int letsSeeCooldown = 2;
        private readonly Regex regex = new Regex(@"^(ну )?(посмотрим|поглядим|увидим|пожив[её]м(-| )увидим)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => regex; }

        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
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
    }
}