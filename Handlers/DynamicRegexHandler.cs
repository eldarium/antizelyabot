using System;
using System.IO;
using System.Text.RegularExpressions;
using Telegram.Bot;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ZelyaDushitelBot.Handlers
{
    public class DynamicRegexHandler : BaseHandler
    {
        private static FileSystemWatcher RegexFileWatcher;
        private DynamicRegexInfo[] DynamicRegexInfos { get; set; }
        private static Regex RefreshRegex = new Regex("^refresh regexes$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex ListRegexesRegex = new Regex("^list regexes$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DynamicRegexHandler()
        {
            RegexFileWatcher = new FileSystemWatcher
            {
                Filter = "regexes",
                Path = AppContext.BaseDirectory,
            };
            var regexesPath = Path.Combine(AppContext.BaseDirectory, "regexes");
            if (File.Exists(regexesPath))
            {
                var rawStr = File.ReadAllText(regexesPath);
                DynamicRegexInfos = JsonConvert.DeserializeObject<DynamicRegexInfo[]>(rawStr);
                Console.WriteLine($"Initialized {DynamicRegexInfos.Count()} regexes");
            }
            else
            {
                using (var fs = new StreamWriter(File.Create(regexesPath)))
                {
                    DynamicRegexInfos = new[] {
                        new DynamicRegexInfo() { Regex = new Regex("^example$", RegexOptions.IgnoreCase), Reaction = "example" }
                        };
                    fs.WriteLine(JsonConvert.SerializeObject(DynamicRegexInfos));
                }
                Console.WriteLine($"Initialized 0 regexes");
            }
            RegexFileWatcher.Changed += OnChanged;
            RegexFileWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;
            RefreshRegexes();
        }

        private void RefreshRegexes()
        {
            var regexesPath = Path.Combine(AppContext.BaseDirectory, "regexes");
            if (File.Exists(regexesPath))
            {
                var rawStr = File.ReadAllText(regexesPath);
                var oldCount = DynamicRegexInfos.Count();
                try
                {
                    DynamicRegexInfos = JsonConvert.DeserializeObject<DynamicRegexInfo[]>(rawStr);
                    Console.WriteLine($"Refreshed {DynamicRegexInfos.Count() - oldCount} regexes");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not refresh regexes: {0}", e.Message);
                }
            }
        }

        private async Task ListRegexes(long chatId, ITelegramBotClient client)
        {
            if (DynamicRegexInfos.Length == 1)
            {
                await client.SendTextMessageAsync(chatId, "No regexes loaded");
                return;
            }
            var allRegexesList = string.Join("\n", DynamicRegexInfos.Skip(1).Select(reg => $"Regex {reg.Regex.ToString()} with reaction {reg.Reaction}"));
            await client.SendTextMessageAsync(chatId, allRegexesList);
        }

        public override async void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasAuthor("daneldarium") && message.HasRegex(RefreshRegex))
            {
                RefreshRegexes();
                await client.SendTextMessageAsync(message.Chat, "refreshed");
            }
            else if (message.HasAuthor("daneldarium") && message.HasRegex(ListRegexesRegex))
            {
                await ListRegexes(message.Chat.Id, client);
            }
            else if ((DynamicRegexInfos.Skip(1).FirstOrDefault(r => message.HasRegex(r.Regex)) is DynamicRegexInfo foundRegexInfo))
            {
                await client.SendTextMessageAsync(message.Chat, foundRegexInfo.Reaction);
                return;
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }

        public class DynamicRegexInfo
        {
            public Regex Regex { get; set; }
            public string Reaction { get; set; }
        }
    }
}