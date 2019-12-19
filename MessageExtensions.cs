using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot
{
    public static class MessageExtensions
    {
        public static bool HasAuthor(this Message message) => message.From != null;

        public static bool HasAuthor(this Message message, string authorName) =>
            message.HasAuthor() && (message.From.Username?.Contains(authorName,
                StringComparison.InvariantCultureIgnoreCase)??false);

        public static bool HasCommand(this Message message, string commandString)
        {
            if (message.Entities == null) return false;
            int i = -1;
            for (int j = 0; j < message.Entities.Length; j++)
            {
                if (message.Entities[j].Type != MessageEntityType.BotCommand) continue;
                i = j;
                break;
            }

            if (i < 0) return false;
            return string.Equals(message.EntityValues.ToArray()[i].Substring(0, message.EntityValues.ToArray()[i].IndexOf("@") > 0 ? message.EntityValues.ToArray()[i].IndexOf("@") : message.EntityValues.ToArray()[i].Length), commandString,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasRegex(this Message message, Regex regex) =>
            message.Text != null && (regex.IsMatch(message.Text) || regex.IsMatch(message.TextToLayout()));

        public static bool HasMention(this Message message, string name) =>
            message.Text != null && (message.Text.Contains(name.StartsWith("@") ? name : "@" + name)
            || message.TextToLayout().Contains(name.StartsWith("@") ? name : "@" + name));

        public static string RemoveMention(this Message message) => message.Text != null ?
            TextWithoutMention(message.Text) :
            null;

        private static string TextWithoutMention(string str) => string.Join(' ', str.Split(" ").Where(a => !a.StartsWith("@")).ToArray());

        public static bool HasRegexIgnoreMention(this Message message, Regex regex) => message.Text != null && (regex.IsMatch(message.RemoveMention())
        || regex.IsMatch(TextWithoutMention(message.TextToLayout())));

        public static string TextToLayout(this Message message)
        {
            if (message.Text == null)
                return null;
            StringBuilder sb = new StringBuilder();
            var engToRus = message.Text.Any(a => _engLayout.Contains(a));
            foreach (var mchar in message.Text)
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
            return sb.ToString();
        }
        private static string _engLayout = @"`qwertyuiop[]asdfghjkl;'zxcvbnm,.?";
        private static string _ruLayout = @"ёйцукенгшщзхъфывапролджэячсмитьбю,";
    }
}