using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot.Handlers
{
    public class MessageWrapper
    {
        private static string _engLayout = @"`qwertyuiop[]asdfghjkl;'zxcvbnm,.?/";
        private static string _ruLayout = @"ёйцукенгшщзхъфывапролджэячсмитьбю,.";
        private string currentMessage;
        public Message OriginalMessage { get; set; }
        public string CurrentMessage
        {
            get => currentMessage;
            set
            {
                currentMessage = TextWithoutMention(value);
                PopulateLayouts();
            }
        }
        public string[] MessageInLayouts { get; set; }
        public MessageWrapper(Message message)
        {
            OriginalMessage = message;
            CurrentMessage = message.Text;
        }
        public bool HasAuthor() => OriginalMessage.From != null;

        public bool HasAuthor(string authorName) =>
            HasAuthor() && (OriginalMessage.From.Username?.Contains(authorName,
                StringComparison.InvariantCultureIgnoreCase) ?? false);

        public bool HasCommand(string commandString)
        {
            if (OriginalMessage.Entities == null) return false;
            int i = -1;
            for (int j = 0; j < OriginalMessage.Entities.Length; j++)
            {
                if (OriginalMessage.Entities[j].Type != MessageEntityType.BotCommand) continue;
                i = j;
                break;
            }

            if (i < 0) return false;
            return string.Equals(OriginalMessage.EntityValues.ToArray()[i].Substring(0, OriginalMessage.EntityValues.ToArray()[i].IndexOf("@") > 0 ? OriginalMessage.EntityValues.ToArray()[i].IndexOf("@") : OriginalMessage.EntityValues.ToArray()[i].Length), commandString,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public bool HasRegex(Regex regex) =>
            CurrentMessage != null && MessageInLayouts.Any(regex.IsMatch);

        public bool HasMention(string name) =>
            CurrentMessage != null && MessageInLayouts.Any(a => a.Contains(name.StartsWith("@") ? name : "@" + name));

        public string RemoveMention() => CurrentMessage != null ?
            TextWithoutMention(CurrentMessage) :
            null;

        private string TextWithoutMention(string str) => string.Join(' ', str.Split(" ").Where(a => !a.StartsWith("@")).ToArray());

        public bool HasRegexIgnoreMention(Regex regex) => CurrentMessage != null && MessageInLayouts.Any(a => regex.IsMatch(TextWithoutMention(a)));

        private void PopulateLayouts()
        {
            if (currentMessage == null)
                return;
            string[] rv = new string[2];
            rv[0] = currentMessage;
            StringBuilder sb = new StringBuilder();
            var engToRus = currentMessage.Any(a => _engLayout.Contains(a));
            foreach (var mchar in currentMessage)
            {
                var index = engToRus ? _engLayout.IndexOf(mchar) : _ruLayout.IndexOf(mchar);
                if (index < 0)
                {
                    sb.Append(mchar);
                    continue;
                }
                sb.Append(engToRus ?
                _ruLayout.ElementAt(_engLayout.IndexOf(mchar)) :
                _engLayout.ElementAt(_ruLayout.IndexOf(mchar)));
            }
            rv[1] = sb.ToString();
        }
    }
}