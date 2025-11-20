using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Input;
using Game.PSI;
using Game.SceneFlow;
using Game.Serialization;
using Game.Settings;
using Game.UI.Localization;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class GameScreenUISystem : UISystemBase, IPreDeserialize
{
	public enum GameScreen
	{
		Main = 0,
		FreeCamera = 1,
		PauseMenu = 10,
		SaveGame = 11,
		NewGame = 12,
		LoadGame = 13,
		Options = 14
	}

	private const string kSavingGameNotificationTitle = "SavingGame";

	private const string kGroup = "game";

	private ValueBinding<GameScreen> m_ActiveScreenBinding;

	private ValueBinding<bool> m_CanUseSaveSystem;

	public GameScreen activeScreen
	{
		get
		{
			return m_ActiveScreenBinding.value;
		}
		set
		{
			m_ActiveScreenBinding.Update(value);
		}
	}

	public bool isMenuActive => m_ActiveScreenBinding.value >= GameScreen.PauseMenu;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_ActiveScreenBinding = new ValueBinding<GameScreen>("game", "activeScreen", GameScreen.Main, new EnumWriter<GameScreen>()));
		AddBinding(new TriggerBinding<GameScreen>("game", "setActiveScreen", SetScreen, new EnumReader<GameScreen>()));
		AddBinding(m_CanUseSaveSystem = new ValueBinding<bool>("game", "canUseSaveSystem", initialValue: true));
		GameManager.instance.onGameSaveLoad += SaveLoadInProgress;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		GameManager.instance.onGameSaveLoad -= SaveLoadInProgress;
		base.OnDestroy();
	}

	private void SaveLoadInProgress(string name, bool start, bool success)
	{
		if (start)
		{
			string identifier = "SavingGame" + name;
			LocalizedString? text = LocalizedString.Value(name);
			ProgressState? progressState = ProgressState.Indeterminate;
			NotificationSystem.Push(identifier, null, text, "SavingGame", null, null, progressState);
		}
		else
		{
			string identifier2 = "SavingGame" + name;
			LocalizedString? text = LocalizedString.Value(name);
			ProgressState? progressState = (success ? ProgressState.Complete : ProgressState.Failed);
			NotificationSystem.Pop(identifier2, 1f, null, text, "SavingGame", null, null, progressState);
		}
		m_CanUseSaveSystem.Update(!start);
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	public void PreDeserialize(Context context)
	{
		SetScreen(GameScreen.Main);
	}

	public void SetScreen(GameScreen screen)
	{
		InputManager.instance.hideCursor = screen == GameScreen.FreeCamera;
		InputManager instance = InputManager.instance;
		CursorLockMode cursorLockMode = (((uint)screen <= 1u) ? SharedSettings.instance.graphics.cursorMode.ToUnityCursorMode() : CursorLockMode.None);
		instance.cursorLockMode = cursorLockMode;
		m_ActiveScreenBinding.Update(screen);
	}

	[Preserve]
	public GameScreenUISystem()
	{
	}
}
