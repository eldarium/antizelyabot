using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ZelyaDushitelBot.Handlers
{
    public class CalculateHandler : RegexHandler
    {
        private Regex neededRegex = new Regex(@"^(бот,? )?посчитай (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex { get => neededRegex; }
        protected override async void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient _client)
        {
            var match = NeededRegex.Match(message.CurrentMessage);
            if (!match.Success)
                match = NeededRegex.Match(message.MessageInLayouts[1]);
            DataTable dt = new DataTable();
            var expr = match.Groups.Last().Value;
            try
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "" + dt.Compute(expr, null));
            }
            catch (EvaluateException eex)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "не могу посчитать");
                Console.WriteLine(eex.ToString());
            }
            catch (Exception ex)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "чтото пошло не так " + ex.Message);
                await _client.SendTextMessageAsync(new ChatId(91740825), $"can't calculate: {ex}", disableNotification: true);
            }
        }
    }
}