using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace AmazonEbookGetter
{
    public class AmazonEbookGetter
    {
        IWebDriver driver;
        List<string> LinksList;
        double MoneySaved;

        /// <summary>
        /// Starts the Selenium Firefox driver, allows the user to sign in to their Amazon account, then navigate to latest url.
        /// </summary>
        /// <param name="firefoxDir">Path to Firefox exe</param>
        public void StartBrowser(string firefoxDir)
        {
            driver = new FirefoxDriver(Environment.CurrentDirectory, new FirefoxOptions() { BrowserExecutableLocation = firefoxDir });
            Login();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Finding page url...");
            if (!TryGetUrl(out string url))
            {
                Console.WriteLine("No url file found, using default...");
                url = "https://www.amazon.com/s?rh=n%3A133140011%2Cn%3A%212334093011%2Cn%3A%212334155011%2Cn%3A%2120795439011%2Cn%3A20102661011&page=2&qid=1586538576&ref=lp_20102661011_pg_2";
                SaveLatestUrl(url);
            }
            else
            {
                Console.WriteLine("Url file found, using latest...");
            }
            driver.Url = url;
            LinksList = new List<string>();
            return;
        }

        /// <summary>
        /// Takes the user to the login page and asks for confirmation that they are signed in
        /// </summary>
        private void Login()
        {
            driver.Url = @"https://www.amazon.co.uk";
            driver.FindElement(By.CssSelector("#nav-link-accountList")).Click();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Please sign in to your Amazon UK account (remember to tick 'Keep me signed in' to allow the script to run as long as it needs to)");
            Console.WriteLine("Press any key after signing in...");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Main loop. Finds & 'buys' free ebooks, continues to next page, saves latest page url and repeats.
        /// </summary>
        /// <param name="token">Token checked for user cancellation</param>
        public void Run(CancellationToken token)
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Press any key to cancel...");
                LinksList.Clear();
                FindEbooks();
                GetEbooks(token);
                if (token.IsCancellationRequested)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Task cancelled");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"Money saved: {MoneySaved.ToString("C", CultureInfo.CurrentCulture)}");
                    return;
                }
                TryGetUrl(out var currUrl);
                driver.Url = currUrl;
                if (FindNextPageButton(out var nextUrl))
                {
                    driver.Url = nextUrl;
                    Console.WriteLine($"Saving latest url...");
                    SaveLatestUrl(nextUrl);
                }
                else
                {
                    break;
                }
            };
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No more pages!");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Money saved: {MoneySaved.ToString("C", CultureInfo.CurrentCulture)}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press any key to finish...");
        }

        /// <summary>
        /// Retrieves all ebook URLs from current results page
        /// </summary>
        private void FindEbooks()
        {
            var EbookElementList = driver.FindElement(By.CssSelector("div.s-result-list:nth-child(1)")).FindElements(By.XPath("./div"));
            for (int i = 0; i < EbookElementList.Count - 1; i++)
            {
                var link = EbookElementList[i].FindElement(By.XPath($"./ div / span / div / div / div[2] / div[2] / div / div[1] / div / div / div[1] / h2 / a")).GetAttribute("href");
                LinksList.Add(link.Replace(".com", ".co.uk"));
            }
        }

        /// <summary>
        /// 'Buys' all free ebooks in LinksList
        /// </summary>
        private void GetEbooks(CancellationToken token)
        {
            foreach (var EbookLink in LinksList)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                // Create 5 second wait to prevent flagging from Amazon
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(5))
                {
                    continue;
                }

                driver.Url = EbookLink;
                try
                {
                    var KindlePrice = driver.FindElement(By.CssSelector(".kindle-price")).FindElement(By.XPath("./td[2]/span")).Text;
                    if (KindlePrice.Equals("£0.00"))
                    {
                        // Try to find the "Saved £X.YZ (100%)" element under the price to keep a counter of money saved
                        try
                        {
                            string saved = driver.FindElement(By.CssSelector(".kindle-price")).FindElement(By.XPath("./td[2]/p")).Text;
                            saved = Regex.Match(saved, @"\d+[.]\d+").Value;
                            if (double.TryParse(saved, out double amount))
                            {
                                MoneySaved += amount;
                            }
                        }
                        catch (Exception) { }; // Not worried if it fails
                        driver.FindElement(By.CssSelector("#one-click-button")).Click(); // Click the buy button
                    }
                }
                catch (NoSuchElementException)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Finds the "Next" button at the bottom of the page and outputs the url link to <paramref name="nextUrl"/>. Returns true if it's found, false otherwise.
        /// </summary>
        /// <param name="nextUrl">Link to next page if it's found. Empty string otherwise.</param>
        /// <returns>True if the next page url is found (there is a next page), false otherwise</returns>
        private bool FindNextPageButton(out string nextUrl)
        {
            try
            {
                nextUrl = driver.FindElement(By.CssSelector(".a-last")).FindElement(By.XPath("./a")).GetAttribute("href");
                return true;
            }
            catch (NoSuchElementException)
            {
                nextUrl = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Quit the Selenium driver
        /// </summary>
        public void CloseBrowser()
        {
            driver.Quit();
        }

        /// <summary>
        /// Saves the latest page of ebooks to a file "url.txt" in the working directory, for retrieval next time the program's run.
        /// </summary>
        /// <param name="url">Url written into url.txt</param>
        public void SaveLatestUrl(string url)
        {
            string path = Environment.CurrentDirectory + "\\url.txt";
            File.WriteAllText(path, url.Trim());
        }

        /// <summary>
        /// Checks if url.txt exists. If it does set <paramref name="url"/> to the contents of the file and return true. Else return false and assign an empty string to <paramref name="url"/>
        /// </summary>
        /// <param name="url">Set to the contents of url.txt, or an empty string if the file doesn't exist</param>
        /// <returns>True if url.txt is found, false otherwise.</returns>
        public bool TryGetUrl(out string url)
        {
            string path = Environment.CurrentDirectory + "\\url.txt";
            if (File.Exists(path))
            {
                var temp = File.ReadAllText(path).Trim();
                if (Uri.IsWellFormedUriString(temp, UriKind.Absolute))
                {
                    url = temp;
                    return true;
                }
                else
                {
                    url = "https://www.amazon.com/s?rh=n%3A133140011%2Cn%3A%212334093011%2Cn%3A%212334155011%2Cn%3A%2120795439011%2Cn%3A20102661011&page=2&qid=1586538576&ref=lp_20102661011_pg_2";
                    SaveLatestUrl(url);
                    return true;
                }

            }
            url = string.Empty;
            return false;
        }
    }
}
