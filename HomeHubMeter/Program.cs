using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication6
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime lastCalc = DateTime.MinValue;

            while (true)
            {
                // is there work to do?

                TimeSpan timeSinceLastRun = DateTime.Now - lastCalc;
                
                if (timeSinceLastRun > TimeSpan.FromSeconds(5))
                {
                    lastCalc = DateTime.Now;

                    var latest = GetStats();

                    if (last != null && latest.Duration > last.Duration)
                    {
                        Console.WriteLine("Downloaded in last {1} secs: {0} bytes", latest.Rx - last.Rx, (latest.Duration - last.Duration).TotalSeconds);
                    }

                    last = latest;
                }

                Thread.Sleep(100);
            }
        }

        static Stats last;

        public static Stats GetStats()
        {
            string data = "Username=admin&Password=";

            WebClient wc = new CookieAwareWebClient();

            wc.DownloadString("http://192.168.1.254/html/settings/a_internet.html");

            string output = wc.UploadString("http://192.168.1.254/index/login.cgi?RequestFile=/html/home/home.html&ActionID=95", data);

            string outputPage = wc.DownloadString("http://192.168.1.254/html/settings/a_internet.html");

            Stats stats = Extract(outputPage);

            return stats;
        }

        static TimeSpan lastRunTook;

        public static Stats Extract(string response)
        {
            var lines = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string line = lines.Single(l => l.StartsWith("var WanPPP = new"));

            var split = line.Split(',');

            // dataTrans, dataRecv,dataTx4gCnt,dataRx4gCnt
            long dataTrans = long.Parse(split[11].Replace("\"", ""));
            long dataRecv = long.Parse(split[12].Replace("\"", ""));
            int dataTx4gCnt = int.Parse(split[13].Replace("\"", ""));
            int dataRx4gCnt = int.Parse(split[14].Replace("\"", "").Replace(")", ""));
            int uptimeSecs = int.Parse(split[7].Replace("\"", ""));

            double k = 1024;
            double m = k * k; // 1048576
            double g = k * k * k; // 1073741824

            long tx = (long)(((dataTx4gCnt * 4) + ((dataTrans - (dataTrans % m)) / g)) * g);

            long rx = (long)(((dataRx4gCnt * 4) + ((dataRecv - (dataRecv % m)) / g)) * g);

            return new Stats { Tx = tx, Rx = rx, Duration = TimeSpan.FromSeconds(uptimeSecs), AsAt = DateTime.Now };
        }
    }

    public class Stats
    {
        public long Tx { get; set; }
        public long Rx { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime AsAt { get; set; }
    }
 
    public class CookieAwareWebClient : WebClient
    {

        private CookieContainer m_container = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                var hwr = (HttpWebRequest)request;
                hwr.CookieContainer = m_container;
                hwr.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                
                
                //Cookie: 
                //Language=en; 
                //FirstMenu=Admin_0; 
                //SecondMenu=Admin_0_0; 
                //ThirdMenu=Admin_0_0_0; 
                //LastFile=%2Fhtml%2Fhome%2Fhome.html; 
                //LastFile1=http%3A%2F%2F192.168.1.254%2F; 
                //LastFile2=http%3A%2F%2F192.168.1.254%2Fhtml%2Fcommon%2Fadvanced_login.html

                string path = "/", host = "192.168.1.254";

                hwr.CookieContainer.Add(new Cookie("Language", "en", path, host));
                hwr.CookieContainer.Add(new Cookie("FirstMenu", "Admin_0", path, host));
                hwr.CookieContainer.Add(new Cookie("SecondMenu", "Admin_0_0", path, host));
                hwr.CookieContainer.Add(new Cookie("ThirdMenu", "Admin_0_0_0", path, host));
                hwr.CookieContainer.Add(new Cookie("LastFile", "%2Fhtml%2Fhome%2Fhome.html", path, host));
                hwr.CookieContainer.Add(new Cookie("LastFile1", "http%3A%2F%2F192.168.1.254%2F", path, host));
                hwr.CookieContainer.Add(new Cookie("LastFile2", "http%3A%2F%2F192.168.1.254%2Fhtml%2Fcommon%2Fadvanced_login.html", path, host));
            }

            return request;
        }
    }
}
