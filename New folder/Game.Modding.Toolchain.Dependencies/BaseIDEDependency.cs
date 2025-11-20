using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.SceneFlow;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public abstract class BaseIDEDependency : BaseDependency
{
	public abstract string minVersion { get; }

	public virtual bool isMinVersion
	{
		get
		{
			if (System.Version.TryParse(version, out var result) && System.Version.TryParse(minVersion, out var result2))
			{
				return result >= result2;
			}
			return false;
		}
	}

	public override string version
	{
		get
		{
			if (base.version == null)
			{
				Task.Run(async () => await GetVersion(GameManager.instance.terminationToken)).Wait();
			}
			return base.version;
		}
		protected set
		{
			base.version = value;
		}
	}

	protected abstract Task<string> GetIDEVersion(CancellationToken token);

	public async Task<string> GetVersion(CancellationToken token)
	{
		string text = base.version;
		if (text == null)
		{
			text = await GetIDEVersion(token).ConfigureAwait(continueOnCapturedContext: false);
		}
		version = text;
		return version;
	}

	public override async Task<bool> IsInstalled(CancellationToken token)
	{
		return !string.IsNullOrEmpty(await GetVersion(token).ConfigureAwait(continueOnCapturedContext: false));
	}

	public override async Task<bool> IsUpToDate(CancellationToken token)
	{
		if (System.Version.TryParse(await GetVersion(token).ConfigureAwait(continueOnCapturedContext: false), out var result) && System.Version.TryParse(minVersion, out var result2))
		{
			return result >= result2;
		}
		return false;
	}

	public override LocalizedString GetLocalizedState(bool includeProgress)
	{
		return base.state.m_State switch
		{
			DependencyState.Installed => LocalizedString.Id("Options.STATE_TOOLCHAIN[Detected]"), 
			DependencyState.NotInstalled => LocalizedString.Id("Options.STATE_TOOLCHAIN[NotDetected]"), 
			DependencyState.Outdated => new LocalizedString("Options.STATE_TOOLCHAIN[DetectedOutdated]", null, new Dictionary<string, ILocElement> { 
			{
				"VERSION",
				LocalizedString.Value(version)
			} }), 
			_ => base.GetLocalizedState(includeProgress), 
		};
	}

	public override LocalizedString GetLocalizedVersion()
	{
		if (base.state.m_State == DependencyState.Installed)
		{
			return base.GetLocalizedVersion();
		}
		return new LocalizedString("Options.WARN_TOOLCHAIN_MIN_VERSION", null, new Dictionary<string, ILocElement> { 
		{
			"MIN_VERSION",
			LocalizedString.Value(minVersion)
		} });
	}
}
