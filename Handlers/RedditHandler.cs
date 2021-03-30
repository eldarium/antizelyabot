using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Reddit;
using Reddit.Controllers;
using Telegram.Bot;

namespace ZelyaDushitelBot.Handlers
{
    public class RedditHandler : BaseHandler
    {
        private static RedditClient redditClient;
        static readonly Regex NeededRegex = new Regex(@"реддит|reddit (.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static RedditHandler()
        {
            var appid = File.ReadAllText(AppContext.BaseDirectory + "appid").Trim();
            var appsecret = File.ReadAllText(AppContext.BaseDirectory + "appsecret").Trim();
            var refreshtoken = File.ReadAllText(AppContext.BaseDirectory + "refreshtoken").Trim();
            redditClient = new RedditClient(appid, refreshtoken, appsecret, userAgent: "CSharp:EldariumScript v.1.0.0 by /u/eldarium");
        }
        public async override void Handle(MessageWrapper message, ITelegramBotClient _client)
        {

            if (message.HasRegexIgnoreMention(NeededRegex))
            {
                try
                {
                    var subReddit = message.CurrentMessage.Split(' ').Last();
                    var hotposts = redditClient.Subreddit(subReddit).Posts.GetHot();
                    var index = new Random().Next(0, hotposts.Count);
                    var gotPost = hotposts[index];
                    if (gotPost.NSFW)
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, @"уберите от экрана женщин и детей, сейчас вылетит чья-то птичка");
                    }
                    var messageToSend = $"{gotPost.Title}\n\n{(gotPost.Listing.IsSelf ? ((SelfPost)gotPost).SelfText : ((LinkPost)gotPost).URL)}";
                    await _client.SendTextMessageAsync(message.Chat.Id, messageToSend);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                NextHandler?.Handle(message, _client);
            }
        }
    }
}