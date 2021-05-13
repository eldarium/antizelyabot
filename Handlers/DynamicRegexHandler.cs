using System;
using System.IO;
using System.Text.RegularExpressions;
using Telegram.Bot;
using System.Linq;
using Newtonsoft.Json;

namespace ZelyaDushitelBot.Handlers
{
    public class DynamicRegexHandler : BaseHandler
    {
        private DynamicRegexInfo[] DynamicRegexInfos { get; set; }
        private static Regex RefreshRegex = new Regex("refresh regexes", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DynamicRegexHandler()
        {
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
        }

        private void RefreshRegexes()
        {
            var regexesPath = Path.Combine(AppContext.BaseDirectory, "regexes");
            if (File.Exists(regexesPath))
            {
                var rawStr = File.ReadAllText(regexesPath);
                var oldCount = DynamicRegexInfos.Count();
                DynamicRegexInfos = JsonConvert.DeserializeObject<DynamicRegexInfo[]>(rawStr);
                Console.WriteLine($"Refreshed {DynamicRegexInfos.Count() - oldCount} regexes");
            }
        }

        public override async void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            if (message.HasAuthor("daneldarium") && message.HasRegex(RefreshRegex))
            {
                RefreshRegexes();
                await client.SendTextMessageAsync(message.Chat, "refreshed");
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