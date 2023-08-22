using System.Runtime.CompilerServices;

namespace StandaloneNotifier
{
    internal class Extensions
    {
        internal class Console
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void Beep()
            {
                System.Console.Beep();
            }
        }

        internal class HttpClientExtensions
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static async Task<HttpResponseMessage?> GetAsync(string url)
            {
                try
                {
                    using (HttpClientHandler httpClientHandler = new HttpClientHandler())
                    {
                        using (HttpClient httpClient = new HttpClient(httpClientHandler))
                        {
                            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
                                "StanaloneNotifier V" + Program.Version);
                            return await httpClient.GetAsync(url);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("There was an error while getting a response from the server.");
                }
                return null;
            }
        }
    }
}
