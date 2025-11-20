using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Game.UI.Localization;
using UnityEngine.Networking;

namespace Game.Modding.Toolchain.Dependencies;

[DebuggerDisplay("{m_State}")]
public abstract class BaseDependency : IToolchainDependency
{
	private IToolchainDependency.State m_State;

	public virtual LocalizedString localizedName => LocalizedString.Id("Options.OPTION[ModdingSettings." + GetType().Name + "]");

	public virtual string name => GetType().Name;

	public virtual string version { get; protected set; }

	string IToolchainDependency.version
	{
		get
		{
			return version;
		}
		set
		{
			version = value;
		}
	}

	public virtual string icon => null;

	public IToolchainDependency.State state
	{
		get
		{
			return m_State;
		}
		set
		{
			m_State = value;
			this.onNotifyProgress?.Invoke(this, value);
		}
	}

	public bool needDownload { get; protected set; }

	bool IToolchainDependency.needDownload
	{
		get
		{
			return needDownload;
		}
		set
		{
			needDownload = value;
		}
	}

	public List<IToolchainDependency.DiskSpaceRequirements> spaceRequirements { get; protected set; } = new List<IToolchainDependency.DiskSpaceRequirements>();

	List<IToolchainDependency.DiskSpaceRequirements> IToolchainDependency.spaceRequirements
	{
		get
		{
			return spaceRequirements;
		}
		set
		{
			spaceRequirements = value;
		}
	}

	public virtual string installationDirectory { get; protected set; }

	public virtual bool confirmUninstallation => false;

	public virtual bool canBeInstalled => true;

	public virtual bool canBeUninstalled => true;

	public virtual bool canChangeInstallationDirectory => false;

	public virtual LocalizedString description => LocalizedString.Id("Options.OPTION_DESCRIPTION[ModdingSettings." + GetType().Name + "]");

	public virtual LocalizedString installDescr => localizedName;

	public virtual LocalizedString uninstallDescr => localizedName;

	public virtual LocalizedString uninstallMessage => default(LocalizedString);

	public virtual Type[] dependsOnInstallation => Array.Empty<Type>();

	public virtual Type[] dependsOnUninstallation => Array.Empty<Type>();

	public virtual IEnumerable<string> envVariables
	{
		get
		{
			yield break;
		}
	}

	public event IToolchainDependency.ProgressDelegate onNotifyProgress;

	public virtual Task<bool> IsInstalled(CancellationToken token)
	{
		return Task.FromResult(result: false);
	}

	public virtual Task<bool> IsUpToDate(CancellationToken token)
	{
		return Task.FromResult(result: true);
	}

	public virtual Task<bool> NeedDownload(CancellationToken token)
	{
		return Task.FromResult(result: false);
	}

	public virtual Task Download(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public virtual Task Install(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public virtual Task Uninstall(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public virtual Task Refresh(CancellationToken token)
	{
		return IToolchainDependency.Refresh(this, token);
	}

	public virtual Task<List<IToolchainDependency.DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token)
	{
		return Task.FromResult(new List<IToolchainDependency.DiskSpaceRequirements>());
	}

	public void OverrideInstallationDirectory(string directory)
	{
		if (canChangeInstallationDirectory)
		{
			installationDirectory = directory;
			return;
		}
		throw new NotSupportedException("Installation directory can not be changed");
	}

	public virtual LocalizedString GetLocalizedState(bool includeProgress)
	{
		return IToolchainDependency.GetLocalizedState(state, includeProgress);
	}

	public virtual LocalizedString GetLocalizedVersion()
	{
		return LocalizedString.Value(version);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(m_State, ((IToolchainDependency)this).availableActions);
	}

	public override string ToString()
	{
		return name;
	}

	protected static async Task Download(BaseDependency dependency, CancellationToken token, string url, string pathOnDisk, string detail)
	{
		token.ThrowIfCancellationRequested();
		IToolchainDependency.log.DebugFormat("Downloading {0} from url '{1}' to '{2}'", dependency.name, url, pathOnDisk);
		dependency.state = new IToolchainDependency.State(DependencyState.Downloading, detail);
		try
		{
			UnityWebRequest webRequest = UnityWebRequest.Get(url);
			webRequest.downloadHandler = new DownloadHandlerFile(pathOnDisk)
			{
				removeFileOnAbort = true
			};
			UnityWebRequest.Result result = await webRequest.SendWebRequest().ConfigureAwait(UpdateProgress, token, 7f);
			token.ThrowIfCancellationRequested();
			switch (result)
			{
			case UnityWebRequest.Result.ConnectionError:
				throw new ToolchainException(ToolchainError.Download, dependency, "Connection Error: " + webRequest.error);
			case UnityWebRequest.Result.DataProcessingError:
				throw new ToolchainException(ToolchainError.Download, dependency, "Error: " + webRequest.error);
			case UnityWebRequest.Result.ProtocolError:
				throw new ToolchainException(ToolchainError.Download, dependency, "HTTP Error: " + webRequest.error);
			}
			IToolchainDependency.log.DebugFormat("{0} download finished", dependency.name);
		}
		catch (ToolchainException)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new ToolchainException(ToolchainError.Download, dependency, innerException);
		}
		bool UpdateProgress(UnityWebRequestAsyncOperation asyncOperation)
		{
			bool isDone = asyncOperation.isDone;
			if (!isDone)
			{
				dependency.state = new IToolchainDependency.State(DependencyState.Downloading, detail, (int)(asyncOperation.progress * 100f));
			}
			return isDone;
		}
	}
}
