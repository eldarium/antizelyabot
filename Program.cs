using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Reddit;
using File = System.IO.File;
using Reddit.Controllers;
using ZelyaDushitelBot.Handlers;

namespace ZelyaDushitelBot
{
    class Program
    {
        static readonly string[] Stickers = {"CAADAgADBAAD9SbqFq83NbkmenTRFgQ",
                                             "CAADAgADBQAD9SbqFjlymYiX2Bj7FgQ",
                                             "CAADAgADBgAD9SbqFoVc73WZyzaDFgQ",
                                             "CAADAgADCQAD9SbqFsQl4dm30mvRFgQ"};
        static IEnumerable<BaseHandler> Handlers;
        static ITelegramBotClient _client;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            var token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();  
            Handlers = ReflectiveEnumerator.GetEnumerableOfType<BaseHandler>();          
            _client = new TelegramBotClient(token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessage;// OnMessageEdited;
            AppDomain.CurrentDomain.UnhandledException += async (object sender, UnhandledExceptionEventArgs args2) =>
            {
                await _client.SendTextMessageAsync(new ChatId(91740825), $"Unhandled exception!\n{args2.ExceptionObject}\n{(args2.ExceptionObject as Exception).InnerException?.Message}", disableNotification: true);
            };
            _client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.ReadLine();
        }

        static async void AddOffence(Message message)
        {
            //await _client.SendStickerAsync(message.Chat.Id, Stickers[new Random((int)DateTime.Now.Ticks).Next(0, 4)], replyToMessageId: message.MessageId);
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            var mWrapper = new MessageWrapper(e.Message);
            foreach(var handler in Handlers) {
                handler.Handle(mWrapper, _client);
            }
            /*
            var message = e.Message;*/
        }
    }
}
