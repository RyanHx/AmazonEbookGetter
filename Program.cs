using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AmazonEbookGetter
{
    static class Program
    {
        static readonly AmazonEbookGetter ebookGetter = new AmazonEbookGetter();
        static readonly CancellationTokenSource token = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            try
            {
                string fireDir = string.Empty;
                while (!File.Exists(fireDir))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(@"Enter firefox exe directory (e.g. C:\Program Files\Mozilla Firefox\firefox.exe):");
                    Console.ForegroundColor = ConsoleColor.White;
                    fireDir = Console.ReadLine();
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Finding page url...");
                string url = string.Empty;
                if (!ebookGetter.TryGetUrl(out url))
                {
                    Console.WriteLine("No url file found, using default...");
                    url = "https://www.amazon.com/s?rh=n%3A133140011%2Cn%3A%212334093011%2Cn%3A%212334155011%2Cn%3A%2120795439011%2Cn%3A20102661011&page=2&qid=1586538576&ref=lp_20102661011_pg_2";
                    ebookGetter.SaveLatestUrl(url);
                }
                else
                {
                    Console.WriteLine("Url file found, using latest...");
                }

                if (!ebookGetter.StartBrowser(fireDir))
                {
                    Console.ReadKey(true);
                    ebookGetter.CloseBrowser();
                    token.Dispose();
                    Environment.Exit(-1);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }

            try
            {
                var t = Task.Run(() => ebookGetter.Run(token.Token), token.Token);
                Console.ReadKey(true);
                if (!t.IsCompleted)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Cancelling...\nPlease wait for \"Press enter to close...\" to appear before closing this window");
                    token.Cancel();
                    await t;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Please wait for \"Press enter to close...\" to appear before closing this window");
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
            }
            finally
            {
                ebookGetter.CloseBrowser();
                token.Dispose();
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }
    }
}