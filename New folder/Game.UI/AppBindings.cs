using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colossal;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game.Assets;
using Game.Rendering.Utilities;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Debug;
using Game.UI.Menu;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.UI;

public class AppBindings : CompositeBinding, IDisposable
{
	private struct FrameTiming : IJsonWritable
	{
		private FrameTimeSampleHistory m_History;

		private GeneralSettings m_Settings;

		private DebugUISystem m_DebugUISystem;

		public float fps;

		public float fullFameTime;

		public float cpuMainThreadTime;

		public float cpuRenderThreadTime;

		public float gpuTime;

		public void Update()
		{
			if (m_Settings == null)
			{
				m_Settings = SharedSettings.instance?.general;
			}
			if (m_Settings != null)
			{
				switch (m_Settings.fpsMode)
				{
				case GeneralSettings.FPSMode.Simple:
					fps = math.max(1f / Time.smoothDeltaTime, 0f);
					break;
				case GeneralSettings.FPSMode.Advanced:
					fps = math.max(1f / Time.smoothDeltaTime, 0f);
					fullFameTime = 1000f / fps;
					break;
				case GeneralSettings.FPSMode.Precise:
					if (m_History == null)
					{
						m_History = HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory;
					}
					if (m_History != null)
					{
						fps = m_History.SampleAverage.FramesPerSecond;
						fullFameTime = m_History.SampleAverage.FullFrameTime;
						cpuMainThreadTime = m_History.SampleAverage.MainThreadCPUFrameTime;
						cpuRenderThreadTime = m_History.SampleAverage.RenderThreadCPUFrameTime;
						gpuTime = m_History.SampleAverage.GPUFrameTime;
						DebugManager.instance.externalDebugUIActive = true;
					}
					break;
				}
			}
			if (m_DebugUISystem == null)
			{
				m_DebugUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DebugUISystem>();
			}
			DebugManager instance = DebugManager.instance;
			instance.externalDebugUIActive |= m_DebugUISystem?.visible ?? false;
		}

		public void Dispose()
		{
			m_History = null;
			m_Settings = null;
			m_DebugUISystem = null;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("fps");
			writer.Write(fps);
			writer.PropertyName("fullFameTime");
			writer.Write(fullFameTime);
			writer.PropertyName("cpuMainThreadTime");
			writer.Write(cpuMainThreadTime);
			writer.PropertyName("cpuRenderThreadTime");
			writer.Write(cpuRenderThreadTime);
			writer.PropertyName("gpuTime");
			writer.Write(gpuTime);
			writer.TypeEnd();
		}
	}

	public const string kGroup = "app";

	public const string kBodyClassNames = "";

	private ValueBinding<string> m_BackgroundProcessMessageBinding;

	private EventBinding<ConfirmationDialogBase> m_ConfirmationDialogBinding;

	private ValueBinding<HashSet<string>> m_ActiveUIModsLocation;

	private GetterValueBinding<SaveInfo> m_CanContinueBinding;

	private ValueBinding<string[]> m_OwnedPrerequisites;

	private EventBinding m_CheckContinueGamePrerequisites;

	private Action<int> m_ConfirmationDialogCallback;

	private Action<int, bool> m_DismissibleConfirmationDialogCallback;

	private DebugUISystem m_DebugUISystem;

	private static FrameTiming m_FrameTiming;

	private ErrorDialogManager m_ErrorDialogManager;

	public bool ready { get; set; }

	public string activeUI { get; set; }

	private float GetFPS()
	{
		GeneralSettings generalSettings = SharedSettings.instance?.general;
		if (generalSettings != null && generalSettings.fpsMode == GeneralSettings.FPSMode.Precise)
		{
			return HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory?.SampleAverage.FramesPerSecond ?? (1f / Time.smoothDeltaTime);
		}
		return 1f / Time.smoothDeltaTime;
	}

	private float GetFullFrameTime()
	{
		return (HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory?.SampleAverage.FullFrameTime).GetValueOrDefault();
	}

	private float GetCPUMainThreadTime()
	{
		return (HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory?.SampleAverage.MainThreadCPUFrameTime).GetValueOrDefault();
	}

	private float GetCPURenderThreadTime()
	{
		return (HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory?.SampleAverage.RenderThreadCPUFrameTime).GetValueOrDefault();
	}

	private float GetGPUTime()
	{
		return (HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming?.m_FrameHistory?.SampleAverage.GPUFrameTime).GetValueOrDefault();
	}

	public void SetMainMenuActive()
	{
		activeUI = "Menu";
	}

	public void SetGameActive()
	{
		activeUI = "Game";
	}

	public void SetEditorActive()
	{
		activeUI = "Editor";
	}

	public void SetNoneActive()
	{
		activeUI = null;
	}

	public AppBindings(ErrorDialogManager errorDialogManager)
	{
		m_ErrorDialogManager = errorDialogManager;
		m_ErrorDialogManager.CreateBindings(this);
		AddUpdateBinding(new GetterValueBinding<bool>("app", "ready", () => ready));
		AddUpdateBinding(new GetterValueBinding<string>("app", "activeUI", () => activeUI, ValueWriters.Nullable(new StringWriter())));
		AddBinding(new ValueBinding<string>("app", "bodyClassNames", ""));
		AddUpdateBinding(new GetterValueBinding<int>("app", "fpsMode", () => (int)(SharedSettings.instance?.general.fpsMode ?? GeneralSettings.FPSMode.Off)));
		AddUpdateBinding(new GetterValueBinding<FrameTiming>("app", "frameStats", () => m_FrameTiming, new ValueWriter<FrameTiming>()));
		AddUpdateBinding(new GetterValueBinding<string>("app", "activeLocale", () => GameManager.instance.localizationManager.activeDictionary.localeID));
		AddBinding(m_BackgroundProcessMessageBinding = new ValueBinding<string>("app", "backgroundProcessMessage", null, ValueWriters.Nullable(new StringWriter())));
		AddBinding(new TriggerBinding<string>("app", "setClipboard", SetClipboard));
		AddBinding(new TriggerBinding("app", "exitApplication", ExitApplication));
		AddBinding(new TriggerBinding<string>("app", "errorAction", OnErrorAction));
		AddBinding(m_ConfirmationDialogBinding = new EventBinding<ConfirmationDialogBase>("app", "confirmationDialog", new ValueWriter<ConfirmationDialogBase>()));
		AddBinding(new TriggerBinding<int>("app", "confirmationDialogCallback", OnConfirmationDialogCallback));
		AddBinding(new TriggerBinding<int, bool>("app", "dismissibleConfirmationDialogCallback", OnDismissibleConfirmationDialogCallback));
		AddBinding(m_ActiveUIModsLocation = new ValueBinding<HashSet<string>>("app", "activeUIModsLocation", new HashSet<string>(), new CollectionWriter<string>()));
		AddBinding(new GetterValueBinding<int>("app", "platform", () => (int)Application.platform.ToPlatform()));
		AddBinding(m_CanContinueBinding = new GetterValueBinding<SaveInfo>("app", "canContinueGame", GetLastSaveInfo, ValueWriters.Nullable(new ValueWriter<SaveInfo>())));
		AddBinding(m_OwnedPrerequisites = new ValueBinding<string[]>("app", "ownedPrerequisites", null, new NullableWriter<string[]>(new ArrayWriter<string>())));
		AddBinding(new CallBinding<string[], bool>("app", "arePrerequisitesMet", GameManager.instance.ArePrerequisitesMet, new NullableReader<string[]>(new ArrayReader<string>())));
		AddBinding(m_CheckContinueGamePrerequisites = new EventBinding("app", "checkContinueGamePrerequisites"));
	}

	internal Task<bool> LauncherContinueGame()
	{
		m_CheckContinueGamePrerequisites.Trigger();
		return Task.FromResult(result: true);
	}

	public void UpdateCanContinueBinding()
	{
		m_CanContinueBinding.Update();
	}

	public void UpdateOwnedPrerequisiteBinding()
	{
		string[] availablePrerequisitesNames = GameManager.instance.GetAvailablePrerequisitesNames();
		m_OwnedPrerequisites.Update(availablePrerequisitesNames);
	}

	private SaveInfo GetLastSaveInfo()
	{
		SaveGameMetadata lastSaveGameMetadata = GameManager.instance.settings.userState.lastSaveGameMetadata;
		if (lastSaveGameMetadata != null && lastSaveGameMetadata.isValidSaveGame)
		{
			return lastSaveGameMetadata.target;
		}
		return null;
	}

	public void UpdateActiveUIModsLocation(IList<string> locations)
	{
		HashSet<string> newValue = new HashSet<string>(locations);
		m_ActiveUIModsLocation.Update(newValue);
	}

	public void AddActiveUIModLocation(IList<string> locations)
	{
		int count = m_ActiveUIModsLocation.value.Count;
		foreach (string location in locations)
		{
			m_ActiveUIModsLocation.value.Add(location);
		}
		if (m_ActiveUIModsLocation.value.Count != count)
		{
			m_ActiveUIModsLocation.TriggerUpdate();
		}
	}

	public void RemoveActiveUIModLocation(IList<string> locations)
	{
		int count = m_ActiveUIModsLocation.value.Count;
		foreach (string location in locations)
		{
			m_ActiveUIModsLocation.value.Remove(location);
		}
		if (m_ActiveUIModsLocation.value.Count != count)
		{
			m_ActiveUIModsLocation.TriggerUpdate();
		}
	}

	public void Dispose()
	{
		m_ErrorDialogManager.Dispose();
		m_FrameTiming.Dispose();
	}

	public override bool Update()
	{
		m_FrameTiming.Update();
		AdaptiveDynamicResolutionScale instance = AdaptiveDynamicResolutionScale.instance;
		DebugManager.instance.adaptiveDRSActive = instance.isEnabled && instance.isAdaptive;
		DebugFrameTiming debugFrameTiming = HDRenderPipeline.currentPipeline?.debugDisplaySettings?.debugFrameTiming;
		if (debugFrameTiming != null)
		{
			FrameTimeSample sample = debugFrameTiming.m_Sample;
			instance.UpdateDRS(sample.FullFrameTime, sample.MainThreadCPUFrameTime, sample.RenderThreadCPUFrameTime, sample.GPUFrameTime);
		}
		m_ErrorDialogManager.Update();
		return base.Update();
	}

	private void ExitApplication()
	{
		GameManager.QuitGame();
	}

	private async void OnErrorAction(string action)
	{
		_ = 2;
		try
		{
			switch (action)
			{
			case "Ignore":
			case "Continue":
				m_ErrorDialogManager.DismissCurrentError();
				break;
			case "Quit":
				ExitApplication();
				break;
			case "SaveAndQuit":
				await SaveBackupImpl();
				ExitApplication();
				break;
			case "SaveAndContinue":
				await SaveBackupImpl();
				m_ErrorDialogManager.DismissCurrentError();
				break;
			case "Rename":
				await m_ErrorDialogManager.RenameCorruptedPackagesAsync();
				m_ErrorDialogManager.DismissCurrentError();
				break;
			case "Mute":
				m_ErrorDialogManager.DismissCurrentError(-1);
				break;
			}
		}
		catch (Exception exception)
		{
			m_ErrorDialogManager.DismissCurrentError();
			CompositeBinding.log.Error(exception);
		}
	}

	private async Task SaveBackupImpl()
	{
		RenderTexture preview = ScreenCaptureHelper.CreateRenderTarget("PreviewSaveGame-Exit", 680, 383);
		ScreenCaptureHelper.CaptureScreenshot(Camera.main, preview, new MenuHelpers.SaveGamePreviewSettings());
		MenuUISystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MenuUISystem>();
		string saveName = "SaveRecovery" + DateTime.Now.ToString("dd-MMMM-HH-mm-ss");
		try
		{
			await GameManager.instance.Save(saveName, existingSystemManaged.GetSaveInfo(autoSave: false), AssetDatabase.user, preview);
		}
		catch (Exception exception)
		{
			CompositeBinding.log.Error(exception);
		}
		finally
		{
			CoreUtils.Destroy(preview);
		}
	}

	private void SetClipboard(string text)
	{
		GUIUtility.systemCopyBuffer = text;
	}

	public void ShowErrorDialog(ErrorDialog dialog)
	{
		m_ErrorDialogManager.ShowError(dialog);
	}

	public void DismissAllErrors()
	{
		m_ErrorDialogManager.DismissAllErrors();
	}

	public void ShowConfirmationDialog([NotNull] ConfirmationDialog dialog, [NotNull] Action<int> callback)
	{
		m_ConfirmationDialogCallback = callback;
		m_ConfirmationDialogBinding.Trigger(dialog);
	}

	public async Task<int> ShowConfirmationDialogAndWait([NotNull] ConfirmationDialog dialog)
	{
		TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
		m_ConfirmationDialogCallback = taskCompletionSource.SetResult;
		m_ConfirmationDialogBinding.Trigger(dialog);
		return await taskCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false);
	}

	public void ShowMessageDialog([NotNull] MessageDialog dialog, Action<int> callback)
	{
		m_ConfirmationDialogCallback = callback;
		m_ConfirmationDialogBinding.Trigger(dialog);
	}

	public void ShowConfirmationDialog([NotNull] DismissibleConfirmationDialog dialog, [NotNull] Action<int, bool> callback)
	{
		m_DismissibleConfirmationDialogCallback = callback;
		m_ConfirmationDialogBinding.Trigger(dialog);
	}

	private void OnConfirmationDialogCallback(int msg)
	{
		if (m_ConfirmationDialogCallback != null)
		{
			Action<int> confirmationDialogCallback = m_ConfirmationDialogCallback;
			m_ConfirmationDialogCallback = null;
			confirmationDialogCallback(msg);
		}
	}

	private void OnDismissibleConfirmationDialogCallback(int msg, bool dontShowAgain)
	{
		if (m_DismissibleConfirmationDialogCallback != null)
		{
			Action<int, bool> dismissibleConfirmationDialogCallback = m_DismissibleConfirmationDialogCallback;
			m_DismissibleConfirmationDialogCallback = null;
			dismissibleConfirmationDialogCallback(msg, dontShowAgain);
		}
	}
}
