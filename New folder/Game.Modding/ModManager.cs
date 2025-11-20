using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Logging.Diagnostics;
using Colossal.PSI.Common;
using Colossal.Reflection;
using Colossal.UI;
using Game.PSI;
using Game.SceneFlow;
using Game.Serialization;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using Unity.Entities;

namespace Game.Modding;

public class ModManager : IEnumerable<ModManager.ModInfo>, IEnumerable, IDisposable
{
	public class ModInfo
	{
		public enum State
		{
			Unknown,
			Loaded,
			Disposed,
			IsNotModWarning,
			IsNotUniqueWarning,
			GeneralError,
			MissedDependenciesError,
			LoadAssemblyError,
			LoadAssemblyReferenceError
		}

		private readonly List<IMod> m_Instances = new List<IMod>();

		public IReadOnlyList<IMod> instances => m_Instances;

		public ExecutableAsset asset { get; private set; }

		public bool isValid
		{
			get
			{
				if (asset.isMod && asset.isEnabled)
				{
					return asset.isUnique;
				}
				return false;
			}
		}

		public bool isLoaded => asset.isLoaded;

		public bool isBursted => asset.isBursted;

		public string name => asset.fullName;

		public string assemblyFullName => asset.assembly.FullName;

		public State state { get; private set; }

		public string loadError { get; private set; }

		public ModInfo(ExecutableAsset asset)
		{
			this.asset = asset;
		}

		public void Preload(Assembly[] assemblies)
		{
		}

		public void Load(UpdateSystem updateSystem)
		{
			try
			{
				if (state != State.Unknown || !asset.isEnabled || !asset.isRequired)
				{
					return;
				}
				if (!asset.isMod && !asset.isReference)
				{
					state = State.IsNotModWarning;
					return;
				}
				if (asset.isMod && !asset.isUnique)
				{
					state = State.IsNotUniqueWarning;
					return;
				}
				if (asset.isMod && !asset.canBeLoaded)
				{
					state = State.MissedDependenciesError;
					loadError = string.Join("\n", from r in asset.references
						where r.Value == null
						select r.Key);
					return;
				}
				asset.LoadAssembly(AfterLoadAssembly, out var uniqueAsset);
				asset = uniqueAsset;
				foreach (Type item in asset.assembly.GetTypesDerivedFrom<IMod>())
				{
					m_Instances.Add((IMod)FormatterServices.GetUninitializedObject(item));
				}
				OnLoad(updateSystem);
				state = State.Loaded;
			}
			catch (ExecutableAsset.LoadExecutableException exception)
			{
				state = State.LoadAssemblyError;
				loadError = StackTraceHelper.ExtractStackTraceFromException(exception);
				throw;
			}
			catch (ExecutableAsset.LoadExecutableReferenceException exception2)
			{
				state = State.LoadAssemblyReferenceError;
				loadError = StackTraceHelper.ExtractStackTraceFromException(exception2);
				throw;
			}
			catch (Exception exception3)
			{
				state = State.GeneralError;
				loadError = StackTraceHelper.ExtractStackTraceFromException(exception3);
				throw;
			}
		}

		private static void AfterLoadAssembly(Assembly assembly)
		{
			TypeManager.InitializeAdditionalTypes(assembly);
			World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SerializerSystem>().SetDirty();
		}

		private void OnLoad(UpdateSystem updateSystem)
		{
			foreach (IMod instance in m_Instances)
			{
				instance.OnLoad(updateSystem);
			}
		}

		private void OnDispose()
		{
			foreach (IMod instance in m_Instances)
			{
				instance.OnDispose();
			}
			m_Instances.Clear();
		}

		public void Dispose()
		{
			OnDispose();
			state = State.Disposed;
		}
	}

	private const string kBurstSuffix = "_win_x86_64";

	private static ILog log = LogManager.GetLogger("Modding").SetShowsErrorsInUI(showsErrorsInUI: false);

	private readonly List<ModInfo> m_ModsInfos = new List<ModInfo>();

	private bool m_Disabled;

	private bool m_Initialized;

	private bool m_IsInProgress;

	public bool isInitialized => m_Initialized;

	public bool restartRequired { get; private set; }

	public IEnumerator<ModInfo> GetEnumerator()
	{
		return m_ModsInfos.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private ModManager()
	{
	}

	public static bool AreModsEnabled()
	{
		GameManager instance = GameManager.instance;
		if ((object)instance == null)
		{
			return false;
		}
		return instance.modManager?.ListModsEnabled().Length > 0;
	}

	public static string[] GetModsEnabled()
	{
		return GameManager.instance?.modManager?.ListModsEnabled();
	}

	public string[] ListModsEnabled()
	{
		return (from x in m_ModsInfos
			where x.isLoaded
			select x.name).Concat(from x in AssetDatabase.global.GetAssets(default(SearchFilter<UIModuleAsset>))
			select x.name).ToArray();
	}

	public ModManager(bool disabled)
	{
		m_Disabled = disabled;
		if (!disabled)
		{
			ProgressState? progressState = ProgressState.Indeterminate;
			NotificationSystem.Push("ModLoadingStatus", null, null, "ModsLoading", "ModsLoadingWaiting", null, progressState);
		}
	}

	public void Initialize(UpdateSystem updateSystem)
	{
		if (m_Disabled || m_Initialized || m_IsInProgress)
		{
			return;
		}
		try
		{
			m_IsInProgress = true;
			LocalizedString? text = "Initializing mods";
			ProgressState? progressState = ProgressState.Indeterminate;
			NotificationSystem.Push("ModLoadingStatus", null, text, "ModsLoading", null, null, progressState);
			RegisterMods();
			InitializeMods(updateSystem);
			m_Initialized = true;
			int num = 0;
			foreach (ModInfo modsInfo in m_ModsInfos)
			{
				ModInfo modInfo = modsInfo;
				if (modInfo.state < ModInfo.State.IsNotModWarning)
				{
					continue;
				}
				num++;
				string id = modInfo.asset.GetHashCode().ToString();
				string text2 = (string.IsNullOrEmpty(modInfo.asset.mod.thumbnailPath) ? null : $"{modInfo.asset.mod.thumbnailPath}?width={NotificationUISystem.width})");
				ProgressState progressState2 = modInfo.state switch
				{
					ModInfo.State.IsNotModWarning => ProgressState.Warning, 
					ModInfo.State.IsNotUniqueWarning => ProgressState.Warning, 
					ModInfo.State.GeneralError => ProgressState.Failed, 
					ModInfo.State.MissedDependenciesError => ProgressState.Failed, 
					ModInfo.State.LoadAssemblyError => ProgressState.Failed, 
					ModInfo.State.LoadAssemblyReferenceError => ProgressState.Failed, 
					_ => ProgressState.Failed, 
				};
				string identifier = id;
				LocalizedString? title = modInfo.asset.mod.displayName;
				string thumbnail = text2;
				progressState = progressState2;
				NotificationSystem.Push(identifier, title, null, null, "ModsLoadingFailed", thumbnail, progressState, null, delegate
				{
					string text3 = "Common.DIALOG_TITLE_MODDING[" + ((progressState2 == ProgressState.Warning) ? "ModLoadingWarning" : "ModLoadingError") + "]";
					LocalizedString message = new LocalizedString($"Common.DIALOG_MESSAGE_MODDING[{modInfo.state}]", null, new Dictionary<string, ILocElement> { 
					{
						"MODNAME",
						LocalizedString.Value(modInfo.asset.mod.displayName)
					} });
					LocalizedString[] otherActions = (modInfo.asset.isLocal ? Array.Empty<LocalizedString>() : new LocalizedString[2]
					{
						LocalizedString.Id("Common.DIALOG_MESSAGE_MODDING[ModPage]"),
						LocalizedString.Id("Common.DIALOG_MESSAGE_MODDING[Disable]")
					});
					if (modInfo.loadError != null)
					{
						MessageDialog dialog = new MessageDialog(text3, message, LocalizedString.Value(modInfo.loadError.Replace("\\", "\\\\").Replace("*", "\\*")), copyButton: true, LocalizedString.Id("Common.OK"), otherActions);
						GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog, Callback);
					}
					else
					{
						MessageDialog dialog2 = new MessageDialog(text3, message, LocalizedString.Id("Common.OK"), otherActions);
						GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, Callback);
					}
				});
				void Callback(int msg)
				{
					switch (msg)
					{
					case 0:
						NotificationSystem.Pop(id);
						break;
					case 2:
						NotificationSystem.Pop(id);
						modInfo.asset.mod.onClick();
						break;
					case 3:
						NotificationSystem.Pop(id);
						modInfo.asset.mod.onEnable(obj: false);
						break;
					case 1:
						break;
					}
				}
			}
			LocalizedString value = ((m_ModsInfos.Count == 0) ? LocalizedString.Id(NotificationUISystem.GetText("ModsLoadingDoneZero")) : new LocalizedString(NotificationUISystem.GetText("ModsLoadingDone"), null, new Dictionary<string, ILocElement>
			{
				{
					"LOADED",
					new LocalizedNumber<int>(m_ModsInfos.Count - num, "integer")
				},
				{
					"TOTAL",
					new LocalizedNumber<int>(m_ModsInfos.Count, "integer")
				}
			}));
			text = value;
			progressState = ProgressState.Complete;
			NotificationSystem.Pop("ModLoadingStatus", 5f, null, text, "ModsLoading", null, null, progressState);
		}
		catch (Exception exception)
		{
			log.Error(exception);
			LocalizedString? text = LocalizedString.Id(NotificationUISystem.GetText("ModsLoadingAllFailed"));
			ProgressState? progressState = ProgressState.Failed;
			NotificationSystem.Pop("ModLoadingStatus", 5f, null, text, "ModsLoading", null, null, progressState);
		}
		finally
		{
			m_IsInProgress = false;
		}
	}

	private void RegisterMods()
	{
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat("Mods registered in {0}ms", t.TotalMilliseconds);
		}))
		{
			m_ModsInfos.Clear();
			ExecutableAsset[] modAssets = ExecutableAsset.GetModAssets(typeof(IMod));
			foreach (ExecutableAsset executableAsset in modAssets)
			{
				try
				{
					m_ModsInfos.Add(new ModInfo(executableAsset));
				}
				catch (Exception exception)
				{
					log.ErrorFormat(exception, "Error registering mod {0}", executableAsset.fullName);
				}
			}
		}
	}

	private void InitializeMods(UpdateSystem updateSystem)
	{
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat($"Mods initialized in {0}ms", t.TotalMilliseconds);
		}))
		{
			foreach (ModInfo modInfo in m_ModsInfos)
			{
				try
				{
					using (PerformanceCounter.Start(delegate(TimeSpan t)
					{
						log.InfoFormat($"Loaded {{1}} in {0}ms", t.TotalMilliseconds, modInfo.name);
					}))
					{
						modInfo.Load(updateSystem);
					}
				}
				catch (Exception exception)
				{
					modInfo.Dispose();
					log.ErrorFormat(exception, "Error initializing mod {0} ({1})", modInfo.name, modInfo.assemblyFullName);
				}
			}
		}
		InitializeUIModules();
	}

	private void InitializeUIModules()
	{
		UIModuleAsset[] array = AssetDatabase.global.GetAssets(default(SearchFilter<UIModuleAsset>)).ToArray();
		List<string> list = new List<string>();
		UIModuleAsset[] array2 = array;
		foreach (UIModuleAsset uIModuleAsset in array2)
		{
			if (uIModuleAsset.isEnabled)
			{
				UIManager.defaultUISystem.AddHostLocation("ui-mods", Path.GetDirectoryName(uIModuleAsset.path), uIModuleAsset.isLocal);
				log.InfoFormat("Registered UI Module {0} from {1}", uIModuleAsset.moduleInfo, uIModuleAsset);
				list.Add(uIModuleAsset.couiPath);
			}
		}
		GameManager.instance.userInterface.appBindings.AddActiveUIModLocation(list);
	}

	public void AddUIModule(UIModuleAsset uiModule)
	{
		if (m_Initialized)
		{
			UIManager.defaultUISystem.AddHostLocation("ui-mods", Path.GetDirectoryName(uiModule.path), uiModule.isLocal);
			GameManager.instance.userInterface.appBindings.AddActiveUIModLocation(new string[1] { uiModule.couiPath });
			log.InfoFormat("Registered UI Module {0} from {1}", uiModule.moduleInfo, uiModule);
		}
	}

	public void RemoveUIModule(UIModuleAsset uiModule)
	{
		if (m_Initialized)
		{
			UIManager.defaultUISystem.RemoveHostLocation("ui-mods", Path.GetDirectoryName(uiModule.path));
			GameManager.instance.userInterface.appBindings.RemoveActiveUIModLocation(new string[1] { uiModule.couiPath });
			log.InfoFormat("Unregistered UI Module {0}", uiModule.moduleInfo);
		}
	}

	public void Dispose()
	{
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat($"Mods disposed in {0}ms", t.TotalMilliseconds);
		}))
		{
			foreach (ModInfo modInfo in m_ModsInfos)
			{
				try
				{
					using (PerformanceCounter.Start(delegate(TimeSpan t)
					{
						log.InfoFormat($"Disposed {{1}} in {0}ms", t.TotalMilliseconds, modInfo.name);
					}))
					{
						modInfo.Dispose();
					}
				}
				catch (Exception exception)
				{
					log.ErrorFormat(exception, "Error disposing mod {0} ({1})", modInfo.name, modInfo.assemblyFullName);
				}
			}
			m_ModsInfos.Clear();
		}
	}

	public void RequireRestart()
	{
		if (!m_Initialized || restartRequired)
		{
			return;
		}
		restartRequired = true;
		log.Info("Restart required");
		ProgressState? progressState = ProgressState.Warning;
		NotificationSystem.Push("RestartRequired", null, null, "EnabledModsChanged", "EnabledModsChanged", null, progressState, null, delegate
		{
			ConfirmationDialog dialog = new ConfirmationDialog("Common.DIALOG_TITLE[Warning]", DialogMessage.GetId("EnabledModsChanged"), "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]");
			GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(dialog, delegate(int msg)
			{
				if (msg == 0)
				{
					restartRequired = false;
					GameManager.QuitGame();
				}
			});
		});
	}

	public bool TryGetExecutableAsset(IMod mod, out ExecutableAsset asset)
	{
		foreach (ModInfo modsInfo in m_ModsInfos)
		{
			foreach (IMod instance in modsInfo.instances)
			{
				if (instance == mod)
				{
					asset = modsInfo.asset;
					return true;
				}
			}
		}
		asset = null;
		return false;
	}

	public bool TryGetExecutableAsset(Assembly assembly, out ExecutableAsset asset)
	{
		foreach (ModInfo modsInfo in m_ModsInfos)
		{
			if (modsInfo.asset.assembly == assembly)
			{
				asset = modsInfo.asset;
				return true;
			}
		}
		asset = null;
		return false;
	}
}
