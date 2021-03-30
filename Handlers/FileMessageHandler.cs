using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ZelyaDushitelBot.Handlers
{
    public class FileMessageHandler : BaseHandler
    {
        public async override void Handle(MessageWrapper message, ITelegramBotClient client)
        {
            
            if (!(message.Document is null) && (message.Document?.FileName?.EndsWith("fb2") ?? false))
            {
                var ctsource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                var ru = "йцукенгшщзхъфывапролджэячсмитьбю" + "йцукенгшщзхъфывапролджэячсмитьбю".ToUpper();
                var en = "icukengsszh-fyvaproldzeacsmin-bu" + "icukengsszh-fyvaproldzeacsmin-bu".ToUpper();
                string rfn = message.Document.FileName;
                for (int i = 0; i < ru.Length; i++)
                    rfn = rfn.Replace(ru[i], en[i]);
                var bookFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rfn);
                using (var fs = File.OpenWrite(bookFilePath))
                {
                    await client.GetInfoAndDownloadFileAsync(message.Document.FileId, fs, ctsource.Token);
                    fs.Flush();
                }
                var a = new Process();
                string outp = "";
                string newbookFilePath = null;
                Regex rr = new Regex("failed: [1-9]+?");
                try
                {
                    a.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"translator\fb2pdf.cmd");
                    a.StartInfo.Arguments = $"\"{bookFilePath}\"";
                    a.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    a.StartInfo.RedirectStandardOutput = true;
                    a.Start();
                    outp = a.StandardOutput.ReadToEnd();
                    a.WaitForExit();
                    newbookFilePath = bookFilePath.Replace("fb2", "pdf");
                    using (var fs = File.OpenRead(newbookFilePath))
                    {
                        await client.SendDocumentAsync(message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, rfn.Replace("fb2", "pdf")));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (rr.IsMatch(outp))
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "errors when converting");
                }
                File.Delete(bookFilePath);
                if (newbookFilePath != null)
                {
                    File.Delete(newbookFilePath);
                    File.Delete(bookFilePath + "pdf");
                }
            }
            else
            {
                NextHandler?.Handle(message, client);
            }
        }
    }
}