using System;
using System.IO;
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
			string json         = await PutAsyncAndGetString("/json/new", null);
			return JsonConvert.DeserializeObject<ChromeSessionInfo>(json);
		}

		public async Task EndSession(ChromeSessionInfo si)
		{
			string json         = await http.GetStringAsync("/json/close/"+si.Id);
		}

		private async Task<string> PutAsyncAndGetString(string url, HttpContent? content)
		{
			var response		= await http.PutAsync(url, content).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		}
	}
}