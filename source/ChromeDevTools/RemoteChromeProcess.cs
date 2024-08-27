using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MasterDevs.ChromeDevTools
{
	public class RemoteChromeProcess : IChromeProcess
    {
        private readonly HttpClient http;

        public RemoteChromeProcess(string remoteDebuggingUri, TimeSpan? timeout = null)
            : this(new Uri(remoteDebuggingUri), timeout)
        {

        }

        public RemoteChromeProcess(Uri remoteDebuggingUri, TimeSpan? timeout = null)
        {
            RemoteDebuggingUri = remoteDebuggingUri;

			http = new HttpClient
			{
				BaseAddress     = RemoteDebuggingUri,
				Timeout         = timeout.HasValue ? timeout.Value : Timeout.InfiniteTimeSpan
            };
        }

        public Uri RemoteDebuggingUri { get; }

        public virtual void Dispose()
        {
            http.Dispose();
        }

        public async Task<ChromeSessionInfo[]> GetSessionInfo()
        {
            string json = await http.GetStringAsync("/json");
            return JsonConvert.DeserializeObject<ChromeSessionInfo[]>(json);
        }

        public async Task<ChromeSessionInfo> StartNewSession()
        {
            string json = await http.GetStringAsync("/json/new");
            return JsonConvert.DeserializeObject<ChromeSessionInfo>(json);
        }

        public async Task EndSession(ChromeSessionInfo si)
        {
            string json         = await http.GetStringAsync("/json/close/"+si.Id);
        }
    }
}