using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StandaloneNotifier.VRCX.IPC;
using StandaloneNotifier.VRCX;
using System.Diagnostics;
using System.Net;

namespace StandaloneNotifier
{
    internal class Program
    {
        internal static double Version = 24;

        internal static readonly object _lock = new object();

        internal static readonly Random random = new Random();
        internal static DateTime RateLimitEnd = DateTime.MinValue;
        private static RateLimitList _rateLimitHandler = new RateLimitList(15, 60);
        private static readonly Dictionary<string, (DateTime, Yoinker?)> yoinkerCheckCache = new Dictionary<string, (DateTime, Yoinker?)>();
        private static readonly IPCClient ipcClient = new IPCClient();
        private static readonly IPCClientReceive ipcClientRec = new IPCClientReceive();
        private static bool _autoUpdate = false;

        private static void LoadConfigVars()
        {
            string? configPath = Path.GetDirectoryName(Environment.ProcessPath) +
                "/config.txt";
            if (configPath == null) return;
            string configVariableSeperator = ":"
                ;
            if (File.Exists(configPath))
            {
                foreach (var ln in File.ReadAllLines(configPath))
                {
                    string[] parts = ln.Split(configVariableSeperator[0]);
                    string id = parts[0].ToLower();
                    if (id == "autoupdate")
                    {
                        if (bool.TryParse(parts[1], out bool res)) _autoUpdate = res;
                    }
                }
            }
            else
            {
                Console.WriteLine("Do you want automatic updates? Y/N");
                string input = Console.ReadLine().ToLower();
                if (input == "y" ||
                    input == "yes")
                    _autoUpdate = true;

                File.WriteAllText(configPath,
                    "AutoUpdate:" +
                    _autoUpdate);
            }
        }

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;
            LoadConfigVars();

            if (File.Exists("update"))
            {
                File.Delete("update");
            }

            Thread rateLimitUpdateThread = new Thread(() =>
            {
                while (true)
                {
                    int rateLimit = 15; // fallback value in case web request fails
                    try
                    {
                        HttpResponseMessage? rateLimitResponse = Extensions.HttpClientExtensions.GetAsync("https://yd.just-h.party/downloads/standalonenotifier/rate_limit").GetAwaiter().GetResult();
                        if (rateLimitResponse != null && rateLimitResponse.IsSuccessStatusCode)
                        {
                            var rateLimitText = rateLimitResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            int.TryParse(rateLimitText, out rateLimit);
                        }
                    } catch { }
                    _rateLimitHandler.SetRateLimit(rateLimit);
                    Thread.Sleep(10000);
                }
            });
            rateLimitUpdateThread.Start();

#if !DEBUG

            try
            {
                HttpResponseMessage? response = Extensions.HttpClientExtensions.GetAsync("https://yd.just-h.party/downloads/standalonenotifier/version").GetAwaiter().GetResult();
                if (response != null)
                {
                    var resp = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var take = resp;
                    if (resp.Split(" ").Length > 1)
                    {
                        take = resp.Split(" ")[0];
                    }
                    double respVersion = Convert.ToDouble(take);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    if (respVersion != Version)
                    {
                    string msg2 = 
                    "A different version is available. Installed -> " +
                    Version +
                    " | Available -> " +
                    resp +
                    "\n" +
                    "Check https://yd.just-h.party/ for the download URL.";
                        Console.WriteLine(msg2);

                        if (_autoUpdate)
                        {
                            string downloadUrl = "https://yd.just-h.party/downloads/StandaloneNotifier.exe"
                            ;
                            string fileName = Path.GetFileName(Environment.ProcessPath);
                            string dir = Path.GetDirectoryName(Environment.ProcessPath);

                            using (var webClient = new WebClient())
                            {
                                webClient.Headers.Add("User-Agent",
                                    "StandaloneNotifier V" + Version);
                                Console.WriteLine("Downloading update... Please don't close the application!");

                                webClient.DownloadFile(downloadUrl,
                                    "update");
                            }
                            File.Move(fileName, fileName + ".old", true);
                            File.Move("update", fileName, true);
                            Console.WriteLine("Update finished... Restarting!");
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = fileName,
                                    UseShellExecute = true
                                }
                            };
                            process.Start();
                            return;
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch { }
#endif
            LogWatcher.Instance.Init();
            ipcClient.Connect();
            ipcClientRec.Connect();
            Console.WriteLine("\0The application is running! Type 'exit' to close the program.");
            Console.Title = "YoinkerDetector V" + 
                Version;
            while (Console.ReadLine().ToLower() != 
                "exit") Thread.Sleep(10);
            Process.GetCurrentProcess().Kill();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void HandleJoin(string? userName, string? userId = null)
        {
            start:
            lock (_lock)
            {
                string search = userName == null ? userId : userName;
                if (yoinkerCheckCache.Count > 512) yoinkerCheckCache.Clear();
                if (yoinkerCheckCache.TryGetValue(search, out var data))
                {
                    if (DateTime.UtcNow.Subtract(data.Item1).Minutes > 30) yoinkerCheckCache.Remove(search);
                    else
                    {
                        Yoinker? yoinker = data.Item2;
                        if (yoinker != null)
                        {
                            string msg = "User " +
                                yoinker.UserName +
                                " has been found " +
                                yoinker.Reason +
                                ". (detection year: " +
                                yoinker.Year +
                                ")";
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine(msg);
                            Console.ForegroundColor = ConsoleColor.White;
                            if (ipcClient == null || !ipcClient.Connected) Extensions.Console.Beep();
                            else
                            {
                                ipcClient.SetCustomTag(userId,
                                    "Yoinker",
                                    "#ff0000");
                                if (userId == null) ipcClient.SendMessage(msg, yoinker.UserId, yoinker.UserName);
                            }
                        }
                        else
                        {
                            string msg = "User " +
                            search +
                            " was not found in any yoinker list.";
                            Console.WriteLine(msg);
                        }

                        return;
                    }
                }

                try
                {
                    string check = Convert.ToBase64String(SHA256.HashData(userId == null ? Encoding.BigEndianUnicode.GetBytes(userName) : Encoding.UTF8.GetBytes(userId))).Replace("/", 
                        "-");
                    HttpResponseMessage? response = Extensions.HttpClientExtensions.GetAsync(Constants.YOINKER_CHECK_ENDPOINT + check).GetAwaiter().GetResult();
                    _rateLimitHandler.RequestMade();
                    if (response == null) return;
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        if (DateTime.Now > RateLimitEnd) RateLimitEnd = DateTime.Now.AddHours(1);
                        string msg = "You are being RATE LIMITED " +
                            RateLimitEnd.Subtract(DateTime.Now).ToString("HH:mm:ss") +
                            " until your requests will be served again.";
                        Console.WriteLine(msg);
                        return;
                    }
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        yoinkerCheckCache.Add(search, (DateTime.UtcNow, null));
                        string msg = "User " +
                            search +
                            " was not found in any yoinker list.";
                        Console.WriteLine(msg);
                        return;
                    }

                    if((int)response.StatusCode >= 500 &&  (int)response.StatusCode <= 599)
                    {
                        goto start;
                        return;
                    }

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("There was an error while receiving the response");
                        return;
                    }

                    string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Yoinker yoinker = JsonSerializer.Deserialize(json, YoinkerJsonContext.Default.Yoinker);
                    if (yoinker == null)
                    {
                        return;
                    }

                    if (yoinker.IsYoinker)
                    {
                        yoinkerCheckCache.Add(search, (DateTime.UtcNow, yoinker));
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        string msg = "User " +
                            yoinker.UserName +
                            " has been found " +
                            yoinker.Reason +
                            ". (detection year: " +
                                yoinker.Year +
                                ")";
                        Console.WriteLine(msg);
                        Console.ForegroundColor = ConsoleColor.White;
                        if (ipcClient == null || !ipcClient.Connected) Extensions.Console.Beep();
                        else
                        {
                           ipcClient.SetCustomTag(userId,
                                "Yoinker",
                                "#ff0000");
                            if(userId == null) ipcClient.SendMessage(msg, yoinker.UserId, yoinker.UserName);
                        }
                    }
                    else
                    {
                        yoinkerCheckCache.Add(search, (DateTime.UtcNow, null));
                        string msg = "User " +
                            search +
                            " was not found in any yoinker list.";
                        Console.WriteLine(msg);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was an error while receiving the response!");
                }
                finally
                {
                    while (_rateLimitHandler.IsRateLimitExceeded())
                        Thread.Sleep(250);
                }
            }
        }
    }
}