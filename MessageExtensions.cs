using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot
{
    public static class MessageExtensions
    {
        public static bool HasAuthor(this Message message) => message.From != null;

        public static bool HasAuthor(this Message message, string authorName) =>
            message.HasAuthor() && message.From.Username.Contains(authorName,
                StringComparison.InvariantCultureIgnoreCase);

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
            return string.Equals(message.EntityValues.ToArray()[i].Substring(0, message.EntityValues.ToArray()[i].IndexOf("@")), commandString,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasRegex(this Message message, Regex regex) =>
            message.Text != null && regex.IsMatch(message.Text);

        public static bool HasMention(this Message message, string name) =>
            message.Text != null && message.Text.Contains(name.StartsWith("@") ? name : "@" + name);

        public static string RemoveMention(this Message message) => message.Text != null ?
            string.Join(' ', message.Text.Split(" ").Where(a => !a.StartsWith("@")).ToArray()) :
            null;

        public static bool HasRegexIgnoreMention(this Message message, Regex regex) => message.Text != null && regex.IsMatch(message.RemoveMention());
    }
}