using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public class MainDependency : IToolchainDependency
{
	public string name => GetType().Name;

	public LocalizedString localizedName => LocalizedString.Id("Options.OPTION[ModdingSettings.ToolchainDeployment]");

	public DeploymentAction availableActions => DeploymentAction.None;

	public IToolchainDependency.State state
	{
		get
		{
			return ToolchainDeployment.dependencyManager.cachedState.toDependencyState;
		}
		set
		{
		}
	}

	string IToolchainDependency.version { get; set; }

	bool IToolchainDependency.needDownload { get; set; }

	List<IToolchainDependency.DiskSpaceRequirements> IToolchainDependency.spaceRequirements { get; set; }

	IEnumerable<string> IToolchainDependency.envVariables
	{
		get
		{
			yield break;
		}
	}

	public string icon => null;

	public bool confirmUninstallation { get; }

	public bool canBeInstalled { get; }

	public bool canBeUninstalled { get; }

	public bool canChangeInstallationDirectory => false;

	public string installationDirectory { get; set; } = string.Empty;

	public LocalizedString description => LocalizedString.Id("Options.OPTION_DESCRIPTION[ModdingSettings.ToolchainDeployment]");

	public LocalizedString installDescr { get; }

	public LocalizedString uninstallDescr { get; }

	public LocalizedString uninstallMessage { get; }

	public Type[] dependsOnInstallation { get; }

	public Type[] dependsOnUninstallation { get; }

	public event IToolchainDependency.ProgressDelegate onNotifyProgress;

	public LocalizedString GetLocalizedState(bool includeProgress)
	{
		return ToolchainDeployment.dependencyManager.cachedState.GetLocalizedState(includeProgress);
	}

	public override int GetHashCode()
	{
		return ToolchainDeployment.dependencyManager.cachedState.GetHashCode();
	}

	public Task<bool> IsInstalled(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task<bool> IsUpToDate(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task<bool> NeedDownload(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task Download(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task Install(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task Uninstall(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task<List<IToolchainDependency.DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token)
	{
		throw new NotSupportedException();
	}

	public Task Refresh(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
