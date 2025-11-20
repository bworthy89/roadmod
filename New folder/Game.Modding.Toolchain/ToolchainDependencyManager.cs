using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Colossal.IO;
using Colossal.Logging;
using Colossal.Logging.Diagnostics;
using Colossal.PSI.Common;
using Colossal.PSI.Environment;
using Game.Modding.Toolchain.Dependencies;
using Game.PSI;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using Microsoft.Win32;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Modding.Toolchain;

public class ToolchainDependencyManager : IEnumerable<IToolchainDependency>, IEnumerable
{
	internal static class UserEnvironmentVariableManager
	{
		private const string kRegistryKeyPath = "Environment";

		private const uint WM_SETTINGCHANGE = 26u;

		private static readonly IntPtr HWND_BROADCAST = (IntPtr)65535;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam);

		public static void BroadcastUserEnvironmentVariableChange()
		{
			SendNotifyMessage(HWND_BROADCAST, 26u, UIntPtr.Zero, "Environment");
		}

		public static void SetUserEnvironmentVariableNoBroadcast(string variableName, string value)
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Environment", writable: true);
			if (registryKey == null)
			{
				throw new InvalidOperationException("Failed to open registry key: HKEY_CURRENT_USER\\Environment");
			}
			registryKey.SetValue(variableName, value, RegistryValueKind.String);
		}

		public static void RemoveUserEnvironmentVariableNoBroadcast(string variableName)
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Environment", writable: true);
			if (registryKey == null)
			{
				throw new InvalidOperationException("Failed to open registry key: HKEY_CURRENT_USER\\Environment");
			}
			if (registryKey.GetValue(variableName) != null)
			{
				registryKey.DeleteValue(variableName);
			}
		}

		public static void SetEnvVars(params string[] requiredEnvVariables)
		{
			if (requiredEnvVariables.Length == 0)
			{
				return;
			}
			foreach (string item in requiredEnvVariables.Append("CSII_PATHSET"))
			{
				if (IToolchainDependency.envVars.TryGetValue(item, out var value))
				{
					log.DebugFormat("Set environment variable {0}={1}", item, value);
					SetUserEnvironmentVariableNoBroadcast(item, value);
				}
			}
			BroadcastUserEnvironmentVariableChange();
		}

		public static void RemoveEnvVars(params string[] requiredEnvVariables)
		{
			object obj;
			if (requiredEnvVariables.Length != 0)
			{
				obj = requiredEnvVariables.Append("CSII_PATHSET");
			}
			else
			{
				obj = requiredEnvVariables;
			}
			IEnumerable<string> second = (IEnumerable<string>)obj;
			foreach (string item in IToolchainDependency.envVars.Keys.Except(second))
			{
				if (!requiredEnvVariables.Contains(item))
				{
					log.DebugFormat("Remove environment variable {0}", item);
					RemoveUserEnvironmentVariableNoBroadcast(item);
				}
			}
			BroadcastUserEnvironmentVariableChange();
		}
	}

	public struct State
	{
		public ModdingToolStatus m_Status;

		public DeploymentState m_State;

		public int m_CurrentStage;

		public int m_TotalStages;

		public int? m_Progress;

		public LocalizedString m_Details;

		public IToolchainDependency.State toDependencyState
		{
			get
			{
				DependencyState state = ((m_Status == ModdingToolStatus.Installing) ? DependencyState.Installing : ((m_Status != ModdingToolStatus.Uninstalling) ? (m_State switch
				{
					DeploymentState.Unknown => DependencyState.Unknown, 
					DeploymentState.Installed => DependencyState.Installed, 
					DeploymentState.NotInstalled => DependencyState.NotInstalled, 
					DeploymentState.Outdated => DependencyState.Outdated, 
					DeploymentState.Invalid => DependencyState.Unknown, 
					_ => DependencyState.Unknown, 
				}) : DependencyState.Removing));
				return new IToolchainDependency.State(state, null, m_Progress);
			}
		}

		public State WithStatus(ModdingToolStatus status)
		{
			m_Status = status;
			return this;
		}

		public State WithState(DeploymentState state)
		{
			m_State = state;
			return this;
		}

		public State WithStages(int stages)
		{
			m_TotalStages = stages;
			m_CurrentStage = 0;
			return this;
		}

		public State WithNextStage()
		{
			m_CurrentStage = Math.Min(m_CurrentStage + 1, m_TotalStages);
			m_Progress = null;
			m_Details = null;
			return this;
		}

		public State WithProgress(int? progress, LocalizedString details = default(LocalizedString))
		{
			m_Progress = progress;
			m_Details = details;
			return this;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_State, m_CurrentStage, m_TotalStages, m_Progress, m_Details);
		}

		public LocalizedString GetLocalizedState(bool includeProgress)
		{
			if (m_Status == ModdingToolStatus.Idle)
			{
				return LocalizedString.Id($"Options.STATE_TOOLCHAIN[{m_State}]");
			}
			string value = ((!m_Progress.HasValue || !includeProgress) ? "[{CURRENT_STAGE}/{TOTAL_STAGES}] {DETAILS}" : "[{CURRENT_STAGE}/{TOTAL_STAGES}] {DETAILS} {PROGRESS}%");
			return new LocalizedString(null, value, new Dictionary<string, ILocElement>
			{
				{
					"CURRENT_STAGE",
					LocalizedString.Value(m_CurrentStage.ToString())
				},
				{
					"TOTAL_STAGES",
					LocalizedString.Value(m_TotalStages.ToString())
				},
				{ "DETAILS", m_Details },
				{
					"PROGRESS",
					LocalizedString.Value(m_Progress.ToString())
				}
			});
		}

		public static implicit operator DeploymentState(State state)
		{
			return state.m_State;
		}

		public static explicit operator State(DeploymentState state)
		{
			return new State
			{
				m_State = state
			};
		}
	}

	[DebuggerDisplay("{m_Dependency} {result}")]
	public class DependencyFilter
	{
		public readonly DeploymentAction m_Action;

		public readonly IToolchainDependency m_Dependency;

		public readonly HashSet<Type> m_DependsOn = new HashSet<Type>();

		public readonly HashSet<Type> m_IsDependencyOf = new HashSet<Type>();

		public FilterResult result { get; private set; }

		public DependencyFilter(DeploymentAction action, IToolchainDependency dependency)
		{
			m_Action = action;
			m_Dependency = dependency;
		}

		public static (List<IToolchainDependency> accepted, List<IToolchainDependency> discarded) Process(DeploymentAction action, List<IToolchainDependency> dependenciesToFilter)
		{
			if (dependenciesToFilter == null)
			{
				dependenciesToFilter = ToolchainDeployment.dependencyManager.m_Dependencies;
			}
			Dictionary<Type, DependencyFilter> dictionary = new Dictionary<Type, DependencyFilter>();
			for (int i = 0; i < dependenciesToFilter.Count; i++)
			{
				IToolchainDependency dependency = dependenciesToFilter[i];
				DependencyFilter dependencyFilter = new DependencyFilter(action, dependency);
				dictionary[dependency.GetType()] = dependencyFilter;
				switch (action)
				{
				case DeploymentAction.Install:
				case DeploymentAction.Update:
				case DeploymentAction.Repair:
				{
					Type[] dependsOnInstallation = dependency.dependsOnInstallation;
					foreach (Type subDependencyType in dependsOnInstallation)
					{
						dependencyFilter.m_DependsOn.Add(subDependencyType);
						if (dependenciesToFilter.Find((IToolchainDependency d) => d.GetType() == subDependencyType) == null)
						{
							IToolchainDependency toolchainDependency = ToolchainDeployment.dependencyManager.m_Dependencies.Find((IToolchainDependency d) => d.GetType() == subDependencyType);
							if (toolchainDependency != null)
							{
								dependenciesToFilter.Add(toolchainDependency);
							}
						}
					}
					break;
				}
				case DeploymentAction.Uninstall:
					foreach (IToolchainDependency subDependency in ToolchainDeployment.dependencyManager.m_Dependencies.Where((IToolchainDependency d) => d.dependsOnUninstallation.Contains(dependency.GetType())))
					{
						dependencyFilter.m_IsDependencyOf.Add(subDependency.GetType());
						if (dependenciesToFilter.Find((IToolchainDependency d) => d.GetType() == subDependency.GetType()) == null)
						{
							dependenciesToFilter.Add(subDependency);
						}
					}
					break;
				}
			}
			foreach (DependencyFilter value in dictionary.Values)
			{
				value.SetBackwardDependencies(dictionary);
			}
			foreach (DependencyFilter value2 in dictionary.Values)
			{
				value2.CheckIfCanBeProcessed(dictionary);
			}
			foreach (DependencyFilter value3 in dictionary.Values)
			{
				value3.CheckIfNeedToBeProcessed(dictionary);
			}
			List<IToolchainDependency> list = (from f in dictionary.Values
				where f.result != FilterResult.Complete && f.result != FilterResult.Invalid
				select f.m_Dependency).ToList();
			List<IToolchainDependency> item = dependenciesToFilter.Except(list).ToList();
			return (accepted: list, discarded: item);
		}

		private void SetBackwardDependencies(Dictionary<Type, DependencyFilter> filters)
		{
			switch (m_Action)
			{
			case DeploymentAction.Install:
			case DeploymentAction.Update:
			case DeploymentAction.Repair:
			{
				foreach (Type item in m_DependsOn)
				{
					if (filters.TryGetValue(item, out var value2))
					{
						value2.m_IsDependencyOf.Add(m_Dependency.GetType());
					}
				}
				break;
			}
			case DeploymentAction.Uninstall:
			{
				foreach (Type item2 in m_IsDependencyOf)
				{
					if (filters.TryGetValue(item2, out var value))
					{
						value.m_DependsOn.Add(m_Dependency.GetType());
					}
				}
				break;
			}
			}
		}

		private void CheckIfCanBeProcessed(Dictionary<Type, DependencyFilter> filters)
		{
			if (result != FilterResult.Unchecked)
			{
				return;
			}
			result = FilterResult.InProgress;
			switch (m_Action)
			{
			case DeploymentAction.Install:
			case DeploymentAction.Update:
			case DeploymentAction.Repair:
				if (m_Dependency.state.m_State != DependencyState.Installed && !m_Dependency.canBeInstalled)
				{
					result = FilterResult.Invalid;
					return;
				}
				if (m_Dependency.state.m_State == DependencyState.Installed)
				{
					result = FilterResult.Complete;
					return;
				}
				foreach (Type item in m_DependsOn)
				{
					if (!filters.TryGetValue(item, out var value2))
					{
						result = FilterResult.Invalid;
						return;
					}
					value2.CheckIfCanBeProcessed(filters);
					if (value2.result == FilterResult.Invalid)
					{
						result = FilterResult.Invalid;
						return;
					}
				}
				break;
			case DeploymentAction.Uninstall:
				if (!m_Dependency.canBeUninstalled)
				{
					result = FilterResult.Invalid;
					return;
				}
				if (m_Dependency.state.m_State == DependencyState.NotInstalled)
				{
					result = FilterResult.Complete;
					return;
				}
				foreach (Type item2 in m_IsDependencyOf)
				{
					if (filters.TryGetValue(item2, out var value))
					{
						value.CheckIfCanBeProcessed(filters);
					}
				}
				break;
			}
			result = FilterResult.Valid;
		}

		private void CheckIfNeedToBeProcessed(Dictionary<Type, DependencyFilter> filters)
		{
			switch (m_Action)
			{
			case DeploymentAction.Install:
			case DeploymentAction.Update:
			case DeploymentAction.Repair:
				if (result != FilterResult.Invalid)
				{
					break;
				}
				{
					foreach (Type item in m_IsDependencyOf)
					{
						if (filters.TryGetValue(item, out var value2))
						{
							value2.result = FilterResult.Invalid;
						}
					}
					break;
				}
			case DeploymentAction.Uninstall:
				if (result != FilterResult.Invalid)
				{
					break;
				}
				{
					foreach (Type item2 in m_IsDependencyOf)
					{
						if (filters.TryGetValue(item2, out var value))
						{
							value.result = FilterResult.Invalid;
						}
					}
					break;
				}
			}
		}
	}

	public enum FilterResult
	{
		Unchecked,
		Valid,
		Invalid,
		Complete,
		InProgress
	}

	private const string kToolchain = "Toolchain";

	private const string kInstallingToolchain = "InstallingToolchain";

	private const string kUninstallingToolchain = "UninstallingToolchain";

	private const string kInstallingToolchainFailed = "InstallingToolchainFailed";

	private const string kUninstallingToolchainFailed = "InstallingToolchainFailed";

	public static readonly string kUserToolingPath = Path.Combine(EnvPath.kCacheDataPath, "Modding");

	public static readonly string kGameToolingPath = Path.Combine(EnvPath.kContentPath, "Game", ".ModdingToolchain");

	private readonly List<IToolchainDependency> m_Dependencies = new List<IToolchainDependency>();

	public static readonly ILog log = LogManager.GetLogger("Modding").SetShowsErrorsInUI(showsErrorsInUI: false);

	public static readonly MainDependency m_MainDependency = new MainDependency();

	public State m_State;

	private const string kInstalledKey = "SOFTWARE\\Colossal Order\\Cities Skylines II\\";

	private const string kInstalledValue = "ModdingToolchainInstalled";

	public bool isInProgress { get; private set; }

	public IReadOnlyList<IToolchainDependency> dependencies => m_Dependencies;

	public State cachedState
	{
		get
		{
			return m_State;
		}
		set
		{
			m_State = value;
			this.OnStateChanged?.Invoke(value);
			if (value.m_Status != ModdingToolStatus.Idle)
			{
				LocalizedString? text = value.GetLocalizedState(includeProgress: false);
				ProgressState? progressState = (value.m_Progress.HasValue ? ProgressState.Progressing : ProgressState.Indeterminate);
				NotificationSystem.Push(progress: value.m_Progress.GetValueOrDefault(), identifier: "Toolchain", title: null, text: text, titleId: "Toolchain", textId: null, thumbnail: null, progressState: progressState);
			}
		}
	}

	private static bool isInstalled
	{
		get
		{
			return SharedSettings.instance.modding.isInstalled;
		}
		set
		{
			SharedSettings.instance.modding.isInstalled = value;
		}
	}

	public event Action<State> OnStateChanged;

	public IEnumerator<IToolchainDependency> GetEnumerator()
	{
		return m_Dependencies.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public async Task<State> GetCurrentState()
	{
		DeploymentState state = await Task.Run(async () => await GetDeploymentState(GameManager.instance.terminationToken, forceRefresh: true));
		cachedState = cachedState.WithState(state);
		return cachedState;
	}

	public void Register<T>() where T : IToolchainDependency, new()
	{
		T val = new T();
		m_Dependencies.Add(val);
	}

	public async Task Install(List<IToolchainDependency> dependenciesToInstall, CancellationToken token)
	{
		if (isInProgress)
		{
			return;
		}
		try
		{
			_ = 4;
			try
			{
				isInProgress = true;
				log.Info("Start modding toolchain installation");
				ProgressState? progressState = ProgressState.Indeterminate;
				Action onClicked = OpenOptions;
				NotificationSystem.Push("Toolchain", null, null, "Toolchain", "InstallingToolchain", null, progressState, null, onClicked);
				cachedState = cachedState.WithStatus(ModdingToolStatus.Installing).WithState(await GetDeploymentState(token)).WithStages(dependenciesToInstall.Count((IToolchainDependency d) => d.state.m_State != DependencyState.Installed))
					.WithProgress(null);
				dependenciesToInstall.Sort(IToolchainDependency.InstallSorting);
				List<IToolchainDependency.DiskSpaceRequirements> list = new List<IToolchainDependency.DiskSpaceRequirements>();
				foreach (IToolchainDependency item in dependenciesToInstall)
				{
					list.AddRange(item.spaceRequirements);
				}
				if (!CheckFreeSpace(list, out var message))
				{
					throw new ToolchainException(ToolchainError.NotEnoughSpace, null, message);
				}
				foreach (IToolchainDependency item2 in dependenciesToInstall)
				{
					item2.state = (IToolchainDependency.State)DependencyState.Queued;
				}
				foreach (IToolchainDependency dependency in dependenciesToInstall)
				{
					cachedState = cachedState.WithNextStage();
					dependency.onNotifyProgress += SetProgress;
					await DownloadDependency(dependency, token);
					await InstallDependency(dependency, token);
					UserEnvironmentVariableManager.SetEnvVars(dependency.envVariables.ToArray());
					await dependency.Refresh(token);
					await Task.Yield();
				}
				isInstalled = true;
				progressState = ProgressState.Complete;
				NotificationSystem.Pop("Toolchain", 1f, null, null, "Toolchain", "InstallingToolchain", null, progressState);
			}
			catch (OperationCanceledException)
			{
				log.Info("Installation canceled");
				SetFailedNotification(ToolchainError.Install);
			}
			catch (AggregateException ex2)
			{
				foreach (Exception innerException in ex2.InnerExceptions)
				{
					if (innerException is ToolchainException ex3)
					{
						ProcessToolchainException(ex3);
					}
					else
					{
						ProcessException(ToolchainError.Install, innerException);
					}
				}
				SetFailedNotification(ToolchainError.Install);
			}
			catch (ToolchainException ex4)
			{
				ProcessToolchainException(ex4);
				SetFailedNotification(ToolchainError.Install);
			}
			catch (Exception ex5)
			{
				ProcessException(ToolchainError.Install, ex5);
				SetFailedNotification(ToolchainError.Install);
			}
		}
		finally
		{
			cachedState = cachedState.WithStatus(ModdingToolStatus.Idle).WithState(await GetDeploymentState(token, forceRefresh: true, throwException: false)).WithStages(0)
				.WithProgress(null);
			isInProgress = false;
		}
	}

	public async Task Uninstall(List<IToolchainDependency> dependenciesToUninstall, CancellationToken token)
	{
		if (isInProgress)
		{
			return;
		}
		try
		{
			_ = 4;
			try
			{
				isInProgress = true;
				log.Info("Start modding toolchain uninstallation");
				ProgressState? progressState = ProgressState.Indeterminate;
				Action onClicked = OpenOptions;
				NotificationSystem.Push("Toolchain", null, null, "Toolchain", "UninstallingToolchain", null, progressState, null, onClicked);
				cachedState = cachedState.WithStatus(ModdingToolStatus.Uninstalling).WithState(await GetDeploymentState(token)).WithStages(dependenciesToUninstall.Count((IToolchainDependency d) => d.state.m_State != DependencyState.NotInstalled))
					.WithProgress(null);
				dependenciesToUninstall.Sort(IToolchainDependency.UninstallSorting);
				foreach (IToolchainDependency item in dependenciesToUninstall)
				{
					item.state = (IToolchainDependency.State)DependencyState.Queued;
				}
				foreach (IToolchainDependency dependency in dependenciesToUninstall)
				{
					cachedState = cachedState.WithNextStage();
					dependency.onNotifyProgress += SetProgress;
					bool flag = !dependency.confirmUninstallation;
					if (!flag)
					{
						LocalizedString message = dependency.uninstallMessage;
						if (message.Equals(default(LocalizedString)))
						{
							message = new LocalizedString("Options.WARN_TOOLCHAIN_DEPENDENCY_UNINSTALL", null, new Dictionary<string, ILocElement> { { "DEPENDENCY_NAME", dependency.localizedName } });
						}
						ConfirmationDialog dialog = new ConfirmationDialog("Common.DIALOG_TITLE[Warning]", message, "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]");
						flag = await GameManager.instance.userInterface.appBindings.ShowConfirmationDialogAndWait(dialog) == 0;
					}
					if (flag)
					{
						await UninstallDependency(dependency, token);
					}
					dependency.onNotifyProgress -= SetProgress;
					await dependency.Refresh(token);
					await Task.Yield();
				}
				UserEnvironmentVariableManager.RemoveEnvVars(dependencies.Where((IToolchainDependency d) => d.state.m_State != DependencyState.NotInstalled).SelectMany((IToolchainDependency d) => d.envVariables).Distinct()
					.ToArray());
				progressState = ProgressState.Complete;
				NotificationSystem.Pop("Toolchain", 1f, null, null, "Toolchain", "UninstallingToolchain", null, progressState);
			}
			catch (OperationCanceledException)
			{
				log.Info("Uninstallation canceled");
				SetFailedNotification(ToolchainError.Uninstall);
			}
			catch (AggregateException ex2)
			{
				foreach (Exception innerException in ex2.InnerExceptions)
				{
					if (innerException is ToolchainException ex3)
					{
						ProcessToolchainException(ex3);
					}
					else
					{
						ProcessException(ToolchainError.Uninstall, innerException);
					}
				}
				SetFailedNotification(ToolchainError.Uninstall);
			}
			catch (ToolchainException ex4)
			{
				ProcessToolchainException(ex4);
				SetFailedNotification(ToolchainError.Uninstall);
			}
			catch (Exception ex5)
			{
				ProcessException(ToolchainError.Uninstall, ex5);
				SetFailedNotification(ToolchainError.Uninstall);
			}
		}
		finally
		{
			cachedState = cachedState.WithStatus(ModdingToolStatus.Idle).WithState(await GetDeploymentState(token, forceRefresh: true, throwException: false)).WithStages(0)
				.WithProgress(null);
			isInProgress = false;
		}
	}

	private async Task DownloadDependency(IToolchainDependency dependency, CancellationToken token)
	{
		_ = 1;
		try
		{
			if (dependency.needDownload)
			{
				dependency.state = (IToolchainDependency.State)DependencyState.Downloading;
				await dependency.Download(token);
				await Task.Yield();
			}
		}
		catch (ToolchainException ex)
		{
			if (ex.isFatal)
			{
				throw;
			}
			ProcessToolchainException(ex);
		}
	}

	private async Task InstallDependency(IToolchainDependency dependency, CancellationToken token)
	{
		_ = 1;
		try
		{
			dependency.state = (IToolchainDependency.State)DependencyState.Installing;
			await dependency.Install(token);
			await Task.Yield();
			dependency.onNotifyProgress -= SetProgress;
		}
		catch (ToolchainException ex)
		{
			if (ex.isFatal)
			{
				throw;
			}
			ProcessToolchainException(ex);
		}
	}

	private async Task UninstallDependency(IToolchainDependency dependency, CancellationToken token)
	{
		_ = 1;
		try
		{
			dependency.state = (IToolchainDependency.State)DependencyState.Removing;
			await dependency.Uninstall(token);
			await Task.Yield();
		}
		catch (ToolchainException ex)
		{
			if (ex.isFatal)
			{
				throw;
			}
			ProcessToolchainException(ex);
		}
	}

	public static void ProcessToolchainException(ToolchainException ex)
	{
		switch (ex.error)
		{
		case ToolchainError.Download:
			log.Error(ex.InnerException, "Error while downloading dependency \"" + ex.source.name + "\": " + ex.Message);
			ShowErrorDialog(new LocalizedString(string.IsNullOrEmpty(ex.Message) ? "Options.ERROR_TOOLCHAIN_DEPENDENCY_DOWNLOAD" : "Options.ERROR_TOOLCHAIN_DEPENDENCY_DOWNLOAD_DETAILS", null, new Dictionary<string, ILocElement>
			{
				{
					"DEPENDENCY_NAME",
					ex.source.localizedName
				},
				{
					"DETAILS",
					LocalizedString.Value(ex.Message)
				}
			}), ex.InnerException);
			break;
		case ToolchainError.Install:
			log.Error(ex.InnerException, "Error while installing dependency \"" + ex.source.name + "\": " + ex.Message);
			ShowErrorDialog(new LocalizedString(string.IsNullOrEmpty(ex.Message) ? "Options.ERROR_TOOLCHAIN_DEPENDENCY_INSTALL" : "Options.ERROR_TOOLCHAIN_DEPENDENCY_INSTALL_DETAILS", null, new Dictionary<string, ILocElement>
			{
				{
					"DEPENDENCY_NAME",
					ex.source.localizedName
				},
				{
					"DETAILS",
					LocalizedString.Value(ex.Message)
				}
			}), ex.InnerException);
			break;
		case ToolchainError.NotEnoughSpace:
			log.Error((Exception)null, (object)("Not enough space on disk to install modding toolchain:\n" + ex.Message));
			ShowErrorDialog(new LocalizedString(string.IsNullOrEmpty(ex.Message) ? "Options.ERROR_TOOLCHAIN_NO_SPACE" : "Options.ERROR_TOOLCHAIN_NO_SPACE_DETAILS", null, new Dictionary<string, ILocElement> { 
			{
				"DETAILS",
				LocalizedString.Value(ex.Message)
			} }));
			break;
		case ToolchainError.Uninstall:
			log.Error(ex.InnerException, $"Error while uninstalling dependency \"{ex.source}\": {ex.Message}");
			ShowErrorDialog(new LocalizedString(string.IsNullOrEmpty(ex.Message) ? "Options.ERROR_TOOLCHAIN_DEPENDENCY_UNINSTALL" : "Options.ERROR_TOOLCHAIN_DEPENDENCY_UNINSTALL_DETAILS", null, new Dictionary<string, ILocElement>
			{
				{
					"DEPENDENCY_NAME",
					ex.source.localizedName
				},
				{
					"DETAILS",
					LocalizedString.Value(ex.Message)
				}
			}), ex.InnerException);
			break;
		}
	}

	private static void ProcessException(ToolchainError error, Exception ex)
	{
		switch (error)
		{
		case ToolchainError.NotEnoughSpace:
		case ToolchainError.Download:
		case ToolchainError.Install:
			log.Error(ex, "Unknown error while modding toolchain installation");
			ShowErrorDialog(LocalizedString.Id("Options.ERROR_TOOLCHAIN_INSTALL_UNKNOWN"), ex);
			break;
		case ToolchainError.Uninstall:
			log.Error(ex, "Unknown error while modding toolchain uninstallation");
			ShowErrorDialog(LocalizedString.Id("Options.ERROR_TOOLCHAIN_UNINSTALL_UNKNOWN"), ex);
			break;
		}
	}

	private static void SetFailedNotification(ToolchainError error)
	{
		switch (error)
		{
		case ToolchainError.NotEnoughSpace:
		case ToolchainError.Download:
		case ToolchainError.Install:
		{
			ProgressState? progressState = ProgressState.Failed;
			NotificationSystem.Pop("Toolchain", 5f, null, null, null, "InstallingToolchainFailed", null, progressState);
			break;
		}
		case ToolchainError.Uninstall:
		{
			ProgressState? progressState = ProgressState.Failed;
			NotificationSystem.Pop("Toolchain", 5f, null, null, null, "InstallingToolchainFailed", null, progressState);
			break;
		}
		}
	}

	private static void OpenOptions()
	{
		World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<OptionsUISystem>()?.OpenPage("Modding", null, isAdvanced: false);
	}

	private static void ShowErrorDialog(LocalizedString message, Exception ex = null)
	{
		ErrorDialog dialog = new ErrorDialog
		{
			localizedTitle = LocalizedString.Id("Options.ERROR_TOOLCHAIN"),
			localizedMessage = message,
			actions = ErrorDialog.ActionBits.Continue,
			severity = ErrorDialog.Severity.Error,
			errorDetails = ((ex != null) ? StackTraceHelper.ExtractStackTraceFromException(ex) : null)
		};
		GameManager.instance.userInterface.appBindings.ShowErrorDialog(dialog);
	}

	private void SetProgress(IToolchainDependency dependency, IToolchainDependency.State dependencyState)
	{
		cachedState = cachedState.WithProgress(dependencyState.m_Progress, dependencyState.m_Details);
	}

	private async Task<DeploymentState> GetDeploymentState(CancellationToken token, bool forceRefresh = false, bool throwException = true)
	{
		try
		{
			token.ThrowIfCancellationRequested();
			bool isAnyDependencyOutdated = false;
			bool isAnyDependencyNotInstalled = false;
			IToolchainDependency.UpdateProcessEnvVarPathValue();
			List<Task> list = new List<Task>();
			foreach (IToolchainDependency dependency in m_Dependencies)
			{
				if (forceRefresh || (DependencyState)dependency.state == DependencyState.Unknown)
				{
					list.Add(dependency.Refresh(token));
				}
			}
			await Task.WhenAll(list);
			using (List<IToolchainDependency>.Enumerator enumerator = m_Dependencies.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					switch (enumerator.Current.state.m_State)
					{
					case DependencyState.Outdated:
						isAnyDependencyOutdated = true;
						break;
					case DependencyState.NotInstalled:
						isAnyDependencyNotInstalled = true;
						break;
					}
				}
			}
			if (!isInstalled)
			{
				return DeploymentState.NotInstalled;
			}
			if (isAnyDependencyNotInstalled)
			{
				return DeploymentState.Invalid;
			}
			if (isAnyDependencyOutdated)
			{
				return DeploymentState.Outdated;
			}
			return DeploymentState.Installed;
		}
		catch (OperationCanceledException)
		{
			return DeploymentState.Unknown;
		}
		catch (Exception exception)
		{
			log.Warn(exception, "Exception occured during GetDeploymentState");
			if (throwException)
			{
				throw;
			}
			return DeploymentState.Unknown;
		}
	}

	private static bool CheckFreeSpace(List<IToolchainDependency.DiskSpaceRequirements> requirements, out string message)
	{
		Dictionary<string, long> dictionary = new Dictionary<string, long>();
		foreach (IToolchainDependency.DiskSpaceRequirements requirement in requirements)
		{
			string pathRoot = Path.GetPathRoot(Path.GetFullPath(requirement.m_Path));
			if (!dictionary.ContainsKey(pathRoot))
			{
				dictionary[pathRoot] = 0L;
			}
			dictionary[pathRoot] += requirement.m_Size;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, long> item in dictionary)
		{
			IOUtils.GetStorageStatus(item.Key, out var _, out var available);
			if (available < item.Value)
			{
				if (item.Value < 1000)
				{
					list.Add($"Disk {item.Key[0]}: {item.Value}B");
				}
				else if (item.Value < 1000000)
				{
					list.Add($"Disk {item.Key[0]}: {math.ceil((float)item.Value / 100f) / 10f:F1}KB");
				}
				else if (item.Value < 1000000000)
				{
					list.Add($"Disk {item.Key[0]}: {math.ceil((float)item.Value / 100000f) / 10f:F1}MB");
				}
				else
				{
					list.Add($"Disk {item.Key[0]}: {math.ceil((float)item.Value / 100000000f) / 10f:F1}GB");
				}
			}
		}
		message = string.Join("\n", list);
		return list.Count == 0;
	}
}
