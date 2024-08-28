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
		private bool _inited;
		private ChromeVersionInfo? _versionInfo;
		private NewSessionMethod _newSessionMethod      = NewSessionMethod.Auto;

		public RemoteChromeProcess(string remoteDebuggingUri, TimeSpan? timeout = null, NewSessionMethod newSession = NewSessionMethod.Auto)
			: this(new Uri(remoteDebuggingUri), timeout, newSession)
		{

		}

		public RemoteChromeProcess(Uri remoteDebuggingUri, TimeSpan? timeout = null, NewSessionMethod newSession = NewSessionMethod.Auto)
		{
			RemoteDebuggingUri	= remoteDebuggingUri;
			_newSessionMethod   = newSession;
			_inited             = false;

			http = new HttpClient
			{
				BaseAddress     = RemoteDebuggingUri,
				Timeout         = timeout.HasValue ? timeout.Value : Timeout.InfiniteTimeSpan
			};
		}

		public Uri RemoteDebuggingUri { get; }

		public ChromeVersionInfo VersionInfo { get { _ensureInit(); return _versionInfo; } }

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
			_ensureInit();

			string json         = _newSessionMethod==NewSessionMethod.Get ? await http.GetStringAsync("/json/new")
								: _newSessionMethod==NewSessionMethod.Put ? await PutAsyncAndGetString("/json/new", null)
								: throw new NotSupportedException("NewSessionMethod="+_newSessionMethod);
			return JsonConvert.DeserializeObject<ChromeSessionInfo>(json);
		}

		private void _ensureInit()
		{
			if (_inited) return;
			lock (_sync)
			{
				if (_inited) return;

				var json        = http.GetStringAsync("/json/version").Result;
				_versionInfo	= JsonConvert.DeserializeObject<ChromeVersionInfo>(json);

				if (_newSessionMethod==NewSessionMethod.Auto)
				{
					var version = _versionInfo.Browser!=null ? new Regex("[^\\/]+\\/(\\d+)").Match(_versionInfo.Browser).Groups[1].Value : "-1";
					_newSessionMethod	= 111<=int.Parse(version) ? NewSessionMethod.Put : NewSessionMethod.Get;
				}

				_inited         = true;
			}
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