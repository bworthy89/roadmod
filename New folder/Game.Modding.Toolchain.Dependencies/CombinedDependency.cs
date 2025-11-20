using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

[DebuggerDisplay("{state}")]
public abstract class CombinedDependency : IToolchainDependency
{
	public enum CombineType
	{
		OR,
		AND,
		ALL
	}

	private IToolchainDependency.State m_State;

	private System.Version m_Version;

	public abstract IEnumerable<IToolchainDependency> dependencies { get; }

	protected abstract bool isAsync { get; }

	public abstract CombineType type { get; }

	public string name => GetType().Name;

	public virtual LocalizedString localizedName => LocalizedString.Id("Options.OPTION[ModdingSettings." + GetType().Name + "]");

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

	public string icon => null;

	public virtual bool confirmUninstallation => false;

	public virtual bool canBeInstalled => true;

	public virtual bool canBeUninstalled => true;

	public virtual bool canChangeInstallationDirectory => false;

	public virtual string installationDirectory { get; set; } = string.Empty;

	public virtual LocalizedString description => LocalizedString.Id("Options.OPTION_DESCRIPTION[ModdingSettings." + GetType().Name + "]");

	public virtual LocalizedString installDescr => default(LocalizedString);

	public virtual LocalizedString uninstallDescr => default(LocalizedString);

	public virtual LocalizedString uninstallMessage => default(LocalizedString);

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

	IEnumerable<string> IToolchainDependency.envVariables
	{
		get
		{
			foreach (IToolchainDependency dependency in dependencies)
			{
				foreach (string envVariable in dependency.envVariables)
				{
					yield return envVariable;
				}
			}
		}
	}

	public virtual Type[] dependsOnInstallation => Array.Empty<Type>();

	public virtual Type[] dependsOnUninstallation => Array.Empty<Type>();

	public event IToolchainDependency.ProgressDelegate onNotifyProgress;

	private async Task<bool> GetCombinedResult(CancellationToken token, Func<IToolchainDependency, CancellationToken, Task<bool>> getTaskPredicate)
	{
		if (isAsync)
		{
			using CancellationTokenSource anyTokenSource = new CancellationTokenSource();
			CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, anyTokenSource.Token);
			try
			{
				List<Task<bool>> tasks = dependencies.Select((IToolchainDependency d) => getTaskPredicate(d, combinedTokenSource.Token)).ToList();
				CombineType combineType = type;
				if (combineType == CombineType.OR)
				{
					while (tasks.Count > 0)
					{
						Task<bool> task = await Task.WhenAny(tasks);
						tasks.Remove(task);
						if (task.IsCompletedSuccessfully && task.Result)
						{
							anyTokenSource.Cancel();
							return true;
						}
					}
					return false;
				}
				if (combineType == CombineType.AND)
				{
					while (tasks.Count > 0)
					{
						Task<bool> task2 = await Task.WhenAny(tasks);
						tasks.Remove(task2);
						if (task2.IsFaulted || !task2.Result)
						{
							anyTokenSource.Cancel();
							return false;
						}
					}
					return true;
				}
			}
			finally
			{
				if (combinedTokenSource != null)
				{
					((IDisposable)combinedTokenSource).Dispose();
				}
			}
		}
		else
		{
			switch (type)
			{
			case CombineType.OR:
				foreach (IToolchainDependency dependency in dependencies)
				{
					if (await getTaskPredicate(dependency, token))
					{
						return true;
					}
				}
				return false;
			case CombineType.AND:
				foreach (IToolchainDependency dependency2 in dependencies)
				{
					if (!(await getTaskPredicate(dependency2, token)))
					{
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}

	private async Task GetCombinedResult(CombineType combineType, CancellationToken token, Func<IToolchainDependency, CancellationToken, Task> getTaskPredicate)
	{
		if (isAsync)
		{
			using (CancellationTokenSource anyTokenSource = new CancellationTokenSource())
			{
				CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, anyTokenSource.Token);
				try
				{
					List<Task> tasks = dependencies.Select((IToolchainDependency d) => getTaskPredicate(d, combinedTokenSource.Token)).ToList();
					switch (combineType)
					{
					case CombineType.OR:
					{
						List<Exception> errors = new List<Exception>();
						while (tasks.Count > 0)
						{
							Task task2 = await Task.WhenAny(tasks);
							tasks.Remove(task2);
							if (task2.IsCompletedSuccessfully)
							{
								anyTokenSource.Cancel();
								return;
							}
							if (task2.Exception != null)
							{
								errors.AddRange(task2.Exception.InnerExceptions);
							}
						}
						if (errors.Count != 0)
						{
							throw new AggregateException(errors);
						}
						return;
					}
					case CombineType.AND:
						while (tasks.Count > 0)
						{
							Task task = await Task.WhenAny(tasks);
							tasks.Remove(task);
							if (task.IsFaulted)
							{
								throw task.Exception;
							}
						}
						return;
					case CombineType.ALL:
						await Task.WhenAll(tasks);
						return;
					}
				}
				finally
				{
					if (combinedTokenSource != null)
					{
						((IDisposable)combinedTokenSource).Dispose();
					}
				}
			}
			return;
		}
		switch (combineType)
		{
		case CombineType.OR:
		{
			List<Exception> errors = new List<Exception>();
			foreach (IToolchainDependency dependency in dependencies)
			{
				Task task3 = getTaskPredicate(dependency, token);
				await task3;
				if (task3.IsCompletedSuccessfully)
				{
					return;
				}
				if (task3.Exception != null)
				{
					errors.AddRange(task3.Exception.InnerExceptions);
				}
			}
			if (errors.Count != 0)
			{
				throw new AggregateException(errors);
			}
			break;
		}
		case CombineType.AND:
			foreach (IToolchainDependency dependency2 in dependencies)
			{
				Task task3 = getTaskPredicate(dependency2, token);
				await task3;
				if (task3.IsFaulted)
				{
					throw task3.Exception;
				}
			}
			break;
		case CombineType.ALL:
			foreach (IToolchainDependency dependency3 in dependencies)
			{
				await getTaskPredicate(dependency3, token);
			}
			break;
		}
	}

	public virtual async Task Refresh(CancellationToken token)
	{
		await GetCombinedResult(CombineType.ALL, token, (IToolchainDependency d, CancellationToken t) => d.Refresh(t));
		await IToolchainDependency.Refresh(this, token);
	}

	public Task<bool> IsInstalled(CancellationToken token)
	{
		return GetCombinedResult(token, (IToolchainDependency d, CancellationToken t) => d.IsInstalled(t));
	}

	public Task<bool> IsUpToDate(CancellationToken token)
	{
		return GetCombinedResult(token, (IToolchainDependency d, CancellationToken t) => d.IsUpToDate(t));
	}

	public Task<bool> NeedDownload(CancellationToken token)
	{
		return GetCombinedResult(token, (IToolchainDependency d, CancellationToken t) => d.NeedDownload(t));
	}

	public Task Download(CancellationToken token)
	{
		return GetCombinedResult(type, token, (IToolchainDependency d, CancellationToken t) => d.Download(t));
	}

	public Task Install(CancellationToken token)
	{
		return GetCombinedResult(type, token, (IToolchainDependency d, CancellationToken t) => d.Install(t));
	}

	public Task Uninstall(CancellationToken token)
	{
		return GetCombinedResult(type, token, (IToolchainDependency d, CancellationToken t) => d.Download(t));
	}

	public Task<List<IToolchainDependency.DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token)
	{
		return Task.FromResult(new List<IToolchainDependency.DiskSpaceRequirements>());
	}

	public virtual LocalizedString GetLocalizedState(bool includeProgress)
	{
		return IToolchainDependency.GetLocalizedState(state, includeProgress);
	}
}
