using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game.Modding.Toolchain.Dependencies;
using Game.UI.Localization;
using Microsoft.Win32;

namespace Game.Modding.Toolchain;

public interface IToolchainDependency
{
	public delegate void ProgressDelegate(IToolchainDependency dependency, State state);

	[DebuggerDisplay("{m_Path}: {m_Size}")]
	public struct DiskSpaceRequirements
	{
		public string m_Path;

		public long m_Size;
	}

	[DebuggerDisplay("{m_State}: {m_Progress}")]
	public struct State
	{
		public DependencyState m_State;

		public int? m_Progress;

		public LocalizedString m_Details;

		public State(DependencyState state, string details = null, int? progress = null)
		{
			m_State = state;
			m_Progress = progress;
			m_Details = (string.IsNullOrEmpty(details) ? default(LocalizedString) : LocalizedString.Id("Options.STATE_TOOLCHAIN[" + details + "]"));
		}

		public static implicit operator DependencyState(State state)
		{
			return state.m_State;
		}

		public static explicit operator State(DependencyState state)
		{
			return new State(state);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_State, m_Progress, m_Details);
		}
	}

	const string CSII_PATHSET = "CSII_PATHSET";

	const string CSII_INSTALLATIONPATH = "CSII_INSTALLATIONPATH";

	const string CSII_USERDATAPATH = "CSII_USERDATAPATH";

	const string CSII_TOOLPATH = "CSII_TOOLPATH";

	const string CSII_LOCALMODSPATH = "CSII_LOCALMODSPATH";

	const string CSII_UNITYMODPROJECTPATH = "CSII_UNITYMODPROJECTPATH";

	const string CSII_UNITYVERSION = "CSII_UNITYVERSION";

	const string CSII_ENTITIESVERSION = "CSII_ENTITIESVERSION";

	const string CSII_MODPOSTPROCESSORPATH = "CSII_MODPOSTPROCESSORPATH";

	const string CSII_MODPUBLISHERPATH = "CSII_MODPUBLISHERPATH";

	const string CSII_MANAGEDPATH = "CSII_MANAGEDPATH";

	const string CSII_PDXCACHEPATH = "CSII_PDXCACHEPATH";

	const string CSII_PDXMODSPATH = "CSII_PDXMODSPATH";

	const string CSII_ASSEMBLYSEARCHPATH = "CSII_ASSEMBLYSEARCHPATH";

	const string CSII_MSCORLIBPATH = "CSII_MSCORLIBPATH";

	const string kEntitiesVersion = "1.3.10";

	protected static ILog log => ToolchainDependencyManager.log;

	string name { get; }

	LocalizedString localizedName { get; }

	string version { get; protected set; }

	State state { get; set; }

	bool needDownload { get; protected set; }

	List<DiskSpaceRequirements> spaceRequirements { get; protected set; }

	bool confirmUninstallation => false;

	bool canBeInstalled => false;

	bool canBeUninstalled => false;

	string installationDirectory => string.Empty;

	bool canChangeInstallationDirectory => false;

	string icon { get; }

	DeploymentAction availableActions
	{
		get
		{
			DeploymentAction deploymentAction = DeploymentAction.None;
			if (installAvailable)
			{
				deploymentAction |= DeploymentAction.Install;
			}
			if (updateAvailable)
			{
				deploymentAction |= DeploymentAction.Update;
			}
			if (uninstallAvailable)
			{
				deploymentAction |= DeploymentAction.Uninstall;
			}
			return deploymentAction;
		}
	}

	bool installAvailable
	{
		get
		{
			if (canBeInstalled)
			{
				return state.m_State == DependencyState.NotInstalled;
			}
			return false;
		}
	}

	bool uninstallAvailable
	{
		get
		{
			if (canBeUninstalled)
			{
				return state.m_State != DependencyState.NotInstalled;
			}
			return false;
		}
	}

	bool updateAvailable
	{
		get
		{
			if (canBeInstalled)
			{
				return state.m_State == DependencyState.Outdated;
			}
			return false;
		}
	}

	LocalizedString description => default(LocalizedString);

	LocalizedString installDescr => default(LocalizedString);

	LocalizedString uninstallDescr => default(LocalizedString);

	LocalizedString uninstallMessage => default(LocalizedString);

	Type[] dependsOnInstallation => Array.Empty<Type>();

	Type[] dependsOnUninstallation => Array.Empty<Type>();

	IEnumerable<string> envVariables { get; }

	static IReadOnlyDictionary<string, string> envVars => new Dictionary<string, string>
	{
		{
			"CSII_INSTALLATIONPATH",
			Environment.CurrentDirectory
		},
		{
			"CSII_USERDATAPATH",
			Path.GetFullPath(EnvPath.kUserDataPath)
		},
		{
			"CSII_TOOLPATH",
			Path.GetFullPath(ToolchainDependencyManager.kUserToolingPath)
		},
		{
			"CSII_LOCALMODSPATH",
			Path.GetFullPath(Path.Combine(EnvPath.kUserDataPath, "Mods"))
		},
		{
			"CSII_UNITYMODPROJECTPATH",
			Path.GetFullPath(Path.Combine(ToolchainDependencyManager.kUserToolingPath, "UnityModsProject"))
		},
		{
			"CSII_UNITYVERSION",
			UnityDependency.sUnityVersion
		},
		{ "CSII_ENTITIESVERSION", "1.3.10" },
		{
			"CSII_MODPOSTPROCESSORPATH",
			Path.GetFullPath(Path.Combine(ToolchainDependencyManager.kGameToolingPath, "ModPostProcessor", "ModPostProcessor.exe"))
		},
		{
			"CSII_MODPUBLISHERPATH",
			Path.GetFullPath(Path.Combine(ToolchainDependencyManager.kGameToolingPath, "ModPublisher", "ModPublisher.exe"))
		},
		{
			"CSII_PDXCACHEPATH",
			Path.GetFullPath(Path.Combine(EnvPath.kUserDataPath, ".pdxsdk"))
		},
		{
			"CSII_PDXMODSPATH",
			Path.GetFullPath(Path.Combine(EnvPath.kCacheDataPath, "Mods"))
		},
		{ "CSII_PATHSET", "Build" },
		{
			"CSII_MANAGEDPATH",
			Path.Combine(Environment.CurrentDirectory, "Cities2_Data", "Managed")
		},
		{
			"CSII_ASSEMBLYSEARCHPATH",
			string.Empty
		},
		{
			"CSII_MSCORLIBPATH",
			Path.Combine(Environment.CurrentDirectory, "Cities2_Data", "Managed", "mscorlib.dll")
		}
	};

	event ProgressDelegate onNotifyProgress;

	static async Task<long> GetDownloadSizeAsync(string url, CancellationToken token, int timeout = 3000)
	{
		using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout);
		using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token);
		try
		{
			using HttpClient client = new HttpClient();
			HttpResponseMessage httpResponseMessage = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), combinedTokenSource.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (httpResponseMessage.IsSuccessStatusCode && httpResponseMessage.Content.Headers.ContentLength.HasValue)
			{
				return httpResponseMessage.Content.Headers.ContentLength.Value;
			}
			return -1L;
		}
		catch (OperationCanceledException)
		{
			if (timeoutTokenSource.IsCancellationRequested)
			{
				log.Error("Get download size request timeout: " + url);
			}
			return -1L;
		}
		catch (Exception exception)
		{
			log.Error(exception, "Get download size error: " + url);
			return -1L;
		}
	}

	Task<bool> IsInstalled(CancellationToken token);

	Task<bool> IsUpToDate(CancellationToken token);

	Task<bool> NeedDownload(CancellationToken token);

	Task Download(CancellationToken token);

	Task Install(CancellationToken token);

	Task Uninstall(CancellationToken token);

	Task<List<DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token);

	void OverrideInstallationDirectory(string directory)
	{
	}

	static bool CheckEnvVariables(IToolchainDependency dependency, bool checkValue = false)
	{
		return dependency.envVariables.All(delegate(string envVariable)
		{
			string environmentVariable = Environment.GetEnvironmentVariable(envVariable, EnvironmentVariableTarget.User);
			return (envVariable == "CSII_PATHSET" || checkValue) ? (envVars[envVariable] == environmentVariable) : (environmentVariable != null);
		});
	}

	Task Refresh(CancellationToken token);

	static async Task Refresh(IToolchainDependency dependency, CancellationToken token)
	{
		dependency.version = null;
		if (!(await dependency.IsInstalled(token)))
		{
			dependency.state = (State)DependencyState.NotInstalled;
		}
		else if (!(await dependency.IsUpToDate(token)))
		{
			dependency.state = (State)DependencyState.Outdated;
		}
		else if (!CheckEnvVariables(dependency))
		{
			dependency.state = (State)DependencyState.Outdated;
		}
		else
		{
			dependency.state = (State)DependencyState.Installed;
		}
		if ((DependencyState)dependency.state != DependencyState.Installed)
		{
			IToolchainDependency toolchainDependency = dependency;
			toolchainDependency.needDownload = await dependency.NeedDownload(token);
			toolchainDependency = dependency;
			toolchainDependency.spaceRequirements = await dependency.GetRequiredDiskSpace(token);
		}
		else
		{
			dependency.needDownload = false;
			dependency.spaceRequirements = new List<DiskSpaceRequirements>();
		}
	}

	static int InstallSorting(IToolchainDependency x, IToolchainDependency y)
	{
		if (x.dependsOnInstallation.Contains(y.GetType()))
		{
			return 1;
		}
		if (y.dependsOnInstallation.Contains(x.GetType()))
		{
			return -1;
		}
		return 0;
	}

	static int UninstallSorting(IToolchainDependency x, IToolchainDependency y)
	{
		if (x.dependsOnUninstallation.Contains(y.GetType()))
		{
			return -1;
		}
		if (y.dependsOnUninstallation.Contains(x.GetType()))
		{
			return 1;
		}
		return 0;
	}

	LocalizedString GetLocalizedState(bool includeProgress)
	{
		return GetLocalizedState(state, includeProgress);
	}

	LocalizedString GetLocalizedVersion()
	{
		return LocalizedString.Value(version);
	}

	static LocalizedString GetLocalizedState(State state, bool includeProgress)
	{
		switch (state.m_State)
		{
		case DependencyState.Downloading:
			if (!state.m_Progress.HasValue)
			{
				break;
			}
			goto IL_0051;
		case DependencyState.Installing:
			if (!state.m_Progress.HasValue)
			{
				break;
			}
			goto IL_0051;
		case DependencyState.Removing:
			{
				if (!state.m_Progress.HasValue)
				{
					break;
				}
				goto IL_0051;
			}
			IL_0051:
			return new LocalizedString(null, includeProgress ? "{STATE} {PROGRESS}%" : "{STATE}", new Dictionary<string, ILocElement>
			{
				{
					"STATE",
					LocalizedString.Id($"Options.STATE_TOOLCHAIN[{state.m_State}]")
				},
				{
					"PROGRESS",
					LocalizedString.Value(state.m_Progress.ToString())
				}
			});
		}
		return LocalizedString.Id($"Options.STATE_TOOLCHAIN[{state.m_State}]");
	}

	static RegistryKey GetUninstaller(Dictionary<string, string> check, out string keyName)
	{
		RegistryKey uninstaller = GetUninstaller("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", check, out keyName);
		if (uninstaller != null)
		{
			return uninstaller;
		}
		uninstaller = GetUninstaller("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall", check, out keyName);
		if (uninstaller != null)
		{
			return uninstaller;
		}
		return null;
	}

	static RegistryKey GetUninstaller(string uninstallKeyName, Dictionary<string, string> check, out string keyName)
	{
		keyName = null;
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(uninstallKeyName);
		if (registryKey == null)
		{
			return null;
		}
		string[] subKeyNames = registryKey.GetSubKeyNames();
		foreach (string text in subKeyNames)
		{
			RegistryKey registryKey2 = registryKey.OpenSubKey(text);
			if (registryKey2 == null)
			{
				continue;
			}
			bool flag = false;
			foreach (KeyValuePair<string, string> item in check)
			{
				if (registryKey2.GetValue(item.Key) as string != item.Value)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				keyName = text;
				return registryKey2;
			}
		}
		return null;
	}

	static void UpdateProcessEnvVarPathValue()
	{
		HashSet<string> allVars = new HashSet<string>();
		Add(EnvironmentVariableTarget.Process);
		Add(EnvironmentVariableTarget.User);
		Add(EnvironmentVariableTarget.Machine);
		string value = string.Join(';', allVars);
		Environment.SetEnvironmentVariable("PATH", value, EnvironmentVariableTarget.Process);
		internal void Add(EnvironmentVariableTarget target)
		{
			string environmentVariable = Environment.GetEnvironmentVariable("PATH", target);
			if (environmentVariable != null)
			{
				string[] array = environmentVariable.Split(';', StringSplitOptions.RemoveEmptyEntries);
				foreach (string item in array)
				{
					allVars.Add(item);
				}
			}
		}
	}
}
