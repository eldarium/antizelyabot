using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using ZelyaDushitelBot.Handlers;
using System.Linq;

namespace ZelyaDushitelBot
{
    class Program
    {
        static readonly string[] Stickers = {"CAADAgADBAAD9SbqFq83NbkmenTRFgQ",
                                             "CAADAgADBQAD9SbqFjlymYiX2Bj7FgQ",
                                             "CAADAgADBgAD9SbqFoVc73WZyzaDFgQ",
                                             "CAADAgADCQAD9SbqFsQl4dm30mvRFgQ"};
        static List<BaseHandler> Handlers;
        static ITelegramBotClient _client;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Hello World!");
            var token = File.ReadAllText(AppContext.BaseDirectory + "token").Trim();
            Handlers = new List<BaseHandler>() { new ConsoleWriterHandler() }
                .Concat(ReflectiveEnumerator.GetListOfType<BaseHandler>().Where(a => !(a is ConsoleWriterHandler)))
                .ToList();
            for (int i = 0; i < Handlers.Count - 1; i++)
            {
                Handlers[i].NextHandler = Handlers[i + 1];
            }
            _client = new TelegramBotClient(token);
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessage;
            AppDomain.CurrentDomain.UnhandledException += async (object sender, UnhandledExceptionEventArgs args2) =>
            {
                await _client.SendTextMessageAsync(new ChatId(91740825), $"Unhandled exception!\n{args2.ExceptionObject}\n{(args2.ExceptionObject as Exception).InnerException?.Message}", disableNotification: true);
            };
            _client.StartReceiving(new[] { UpdateType.EditedMessage, UpdateType.Message });
            Console.ReadLine();
        }

        static void OnMessage(object sender, MessageEventArgs e)
        {
            var mWrapper = new MessageWrapper(e.Message);
            Handlers[0].Handle(mWrapper, _client);
        }
    }
}
