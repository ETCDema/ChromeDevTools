using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MasterDevs.ChromeDevTools
{
	public class RemoteChromeProcess : IChromeProcess
	{
		public enum NewSessionMethod
		{
			Auto				= -1,
			Get					= 0,
			Put					= 1,
		}

		private readonly object _sync	= new();
		private readonly HttpClient http;
		private NewSessionMethod _newSessionMethod      = NewSessionMethod.Auto;

		public RemoteChromeProcess(string remoteDebuggingUri, TimeSpan? timeout = null, NewSessionMethod newSession = NewSessionMethod.Auto)
			: this(new Uri(remoteDebuggingUri), timeout, newSession)
		{

		}

		public RemoteChromeProcess(Uri remoteDebuggingUri, TimeSpan? timeout = null, NewSessionMethod newSession = NewSessionMethod.Auto)
		{
			RemoteDebuggingUri	= remoteDebuggingUri;
			_newSessionMethod   = newSession;

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
			if (_newSessionMethod==NewSessionMethod.Auto)
			{
				lock (_sync)
				{
					if (_newSessionMethod==NewSessionMethod.Auto)
						_newSessionMethod		= _detectNewSessionMethod().Result;
				}
			}

			string json         = _newSessionMethod==NewSessionMethod.Get ? await http.GetStringAsync("/json/new")
								: _newSessionMethod==NewSessionMethod.Put ? await PutAsyncAndGetString("/json/new", null)
								: throw new NotSupportedException("NewSessionMethod="+_newSessionMethod);
			return JsonConvert.DeserializeObject<ChromeSessionInfo>(json);
		}

		private async Task<NewSessionMethod> _detectNewSessionMethod()
		{
			var json			= await http.GetStringAsync("/json/version");
			var version			= new Regex("\"Browser\": \"[^\\/]+\\/(\\d+)").Match(json).Groups[1].Value;
			return 111<=int.Parse(version) ? NewSessionMethod.Put : NewSessionMethod.Get;
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