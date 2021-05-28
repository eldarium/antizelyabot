using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenDotaDotNet.Models.Teams;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    class DotaTeamHandler : RegexHandler
    {
        private readonly Regex regex = new Regex(@"^(бот, )?состав (команды )?(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex replaceRegex = new Regex("[\\W_-[\\s]]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected override Regex NeededRegex => regex;
        protected IEnumerable<Team> cachedTeams = null;
        protected ConcurrentDictionary<int, IEnumerable<TeamPlayer>> cachedPlayers = null;
        protected static OpenDotaDotNet.OpenDotaApi openDota = new OpenDotaDotNet.OpenDotaApi();
        protected async override void ConcreteRegexHandler(MessageWrapper message, ITelegramBotClient client)
        {
            if (cachedTeams == null)
            {
                cachedTeams = await openDota.Teams.GetTeamsAsync();
                cachedPlayers = new ConcurrentDictionary<int, IEnumerable<TeamPlayer>>();
            }
            var match = NeededRegex.Match(message.CurrentMessage).Groups[3].Value.ToLowerInvariant();
            var neededTeam = cachedTeams.FirstOrDefault(t => t.Name.Equals(match, System.StringComparison.InvariantCultureIgnoreCase)
             || t.Tag.Equals(match, System.StringComparison.InvariantCultureIgnoreCase));
            if (neededTeam == null)
                neededTeam = cachedTeams.FirstOrDefault(t => FuzzySharp.Fuzz.Ratio(match, replaceRegex.Replace(t.Name, "").ToLowerInvariant()) > 50 ||
                FuzzySharp.Fuzz.Ratio(match, replaceRegex.Replace(t.Tag, "").ToLowerInvariant()) > 50);
            if (neededTeam == null)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "не нашел команду - попробуй ввести полное название (на английском)");
                return;
            }
            if (!cachedPlayers.ContainsKey(neededTeam.TeamId))
            {
                var players = await openDota.Teams.GetTeamPlayersByIdAsync(neededTeam.TeamId);
                cachedPlayers.TryAdd(neededTeam.TeamId, players.Where(p => p.IsCurrentTeamMember ?? false));
            }
            var rString = string.Join(", ", cachedPlayers[neededTeam.TeamId].Select(p => p.Name));
            await client.SendTextMessageAsync(message.Chat.Id, $"Состав команды {neededTeam.Name}: {rString}");
        }
    }
}