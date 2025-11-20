using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cohtml.Net;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Colossal.UI;
using Game.Input;
using Game.SceneFlow;
using Game.Settings;
using PDX.ModsUI;
using PDX.ModsUI.Adapters;
using PDX.ModsUI.Services;
using PDX.SDK.Contracts.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PSI.PdxSdk;

public class PdxModsUI : IPdxModsUI, IDisposable
{
	private class ColossalUIViewAdapter : ICohtmlViewAdapter, IDisposable
	{
		private readonly InputBarrier m_InputBarrier;

		private UIView m_View;

		private bool isAvailable
		{
			get
			{
				if (m_View != null && m_View.enabled)
				{
					return m_View.View.IsReadyForBindings();
				}
				return false;
			}
		}

		public bool IsActiveAndEnabled => m_View?.enabled ?? false;

		public event Action ReadyForBindings;

		public ColossalUIViewAdapter()
		{
			m_InputBarrier = Game.Input.InputManager.instance.CreateGlobalBarrier("ColossalUIViewAdapter");
			UIView.Settings settings = UIView.Settings.New;
			settings.textInputHandler = GameManager.instance.userInterface.virtualKeyboard;
			m_View = UIManager.defaultUISystem.CreateView(kModsUIUri, settings);
			m_View.Listener.ReadyForBindings += OnReadyForBindings;
			m_View.Listener.TextInputTypeChanged += OnTextInputTypeChanged;
			m_View.Listener.CaretRectChanged += OnCaretRectChanged;
		}

		public void Disable()
		{
			m_View.enabled = false;
			m_InputBarrier.blocked = false;
		}

		public void Enable()
		{
			m_InputBarrier.blocked = true;
			m_View.enabled = true;
		}

		public void Reload()
		{
			m_View.View.Reload();
		}

		public BoundEventHandle BindCall(string callName, Delegate handler)
		{
			if (!isAvailable)
			{
				throw new Exception("Not ready for bindings");
			}
			return m_View.View.BindCall(callName, handler);
		}

		public BoundEventHandle RegisterForEvent(string callName, Delegate handler)
		{
			if (!isAvailable)
			{
				throw new Exception("Not ready for bindings");
			}
			return m_View.View.RegisterForEvent(callName, handler);
		}

		public void UnbindCall(BoundEventHandle boundEventHandle)
		{
			if (isAvailable)
			{
				m_View.View.UnbindCall(boundEventHandle);
			}
		}

		public void UnregisterFromEvent(BoundEventHandle boundEventHandle)
		{
			if (isAvailable)
			{
				m_View.View.UnregisterFromEvent(boundEventHandle);
			}
		}

		public void TriggerEvent<T>(string eventName, T message)
		{
			if (isAvailable)
			{
				m_View.View.TriggerEvent(eventName, message);
			}
		}

		public void AddHostLocation(string key, List<string> value)
		{
			m_View.uiSystem.AddHostLocation(key, value.Select((string x) => (x: x, 0)), shouldWatch: false);
		}

		public void RemoveHostLocation(string key)
		{
			m_View.uiSystem.RemoveHostLocation(key);
		}

		public void Dispose()
		{
			m_View.Listener.ReadyForBindings -= OnReadyForBindings;
			m_View.Listener.TextInputTypeChanged -= OnTextInputTypeChanged;
			m_View.Listener.CaretRectChanged -= OnCaretRectChanged;
			m_View.uiSystem.DestroyView(m_View);
			m_View = null;
			m_InputBarrier.Dispose();
		}

		private void OnReadyForBindings()
		{
			this.ReadyForBindings?.Invoke();
		}

		private void OnTextInputTypeChanged(ControlType type)
		{
			Game.Input.InputManager.instance.hasInputFieldFocus = type == ControlType.TextInput;
		}

		private void OnCaretRectChanged(int x, int y, uint width, uint height)
		{
			Game.Input.InputManager.instance.caretRect = (new Vector2(x, y), new Vector2(width, height));
		}
	}

	private class ModsUILogger : LogService
	{
		public ModsUILogger(LogLevel level)
			: base(level)
		{
		}

		public override void WriteLogEntry(string message, LogLevel logLevel, string source = null, string callerFilePath = null)
		{
			if (logLevel >= base.LogLevel && logLevel != LogLevel.L9_None)
			{
				if (source == null && callerFilePath != null)
				{
					source = Path.GetFileNameWithoutExtension(callerFilePath);
				}
				string message2 = ((source == null) ? message : (source + ": " + message));
				switch (logLevel)
				{
				case LogLevel.L2_Warning:
					log.Warn(message2);
					break;
				case LogLevel.L3_Error:
					log.Error(message2);
					break;
				default:
					log.Info(message2);
					break;
				}
			}
		}
	}

	private static ILog log = LogManager.GetLogger("PdxModsUI").SetShowsErrorsInUI(showsErrorsInUI: false);

	private static readonly string kModsUIHost = "ModsUI".ToLowerInvariant();

	private static readonly string kModsUIUri = "assetdb://" + kModsUIHost + "/index.html";

	private PdxSdkPlatform m_PdxPlatform;

	public PdxSdkPlatform platform => m_PdxPlatform;

	public string locale => GameManager.instance.localizationManager.activeLocaleId.ToPdxLanguage().ToString().Replace('_', '-');

	public ICohtmlViewAdapter uiViewAdapter => new ColossalUIViewAdapter();

	public ILogService logger => new ModsUILogger(LogLevel.L2_Warning);

	public bool isActive
	{
		get
		{
			if (m_PdxPlatform != null)
			{
				return m_PdxPlatform.isModsUIActive;
			}
			return false;
		}
	}

	public PdxModsUI()
	{
		Game.Input.InputManager.instance.EventActiveDeviceChanged += OnActiveDeviceChanged;
		GameManager.instance.localizationManager.onActiveDictionaryChanged += UpdateLocale;
		GameManager.instance.onFullscreenOverlayOpened += Hide;
		m_PdxPlatform = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
		m_PdxPlatform?.SetPdxModsUI(this);
		PlatformManager.instance.onPlatformRegistered += delegate(IPlatformServiceIntegration psi)
		{
			if (psi is PdxSdkPlatform pdxPlatform)
			{
				m_PdxPlatform = pdxPlatform;
				m_PdxPlatform.SetPdxModsUI(this);
			}
		};
	}

	public void Show()
	{
		m_PdxPlatform?.ShowModsUI();
	}

	public void Hide()
	{
		m_PdxPlatform?.HideModsUI();
	}

	public void Destroy()
	{
		m_PdxPlatform?.DestroyModsUI();
	}

	private void OnActiveDeviceChanged(InputDevice newDevice, InputDevice oldDevice, bool schemeChanged)
	{
		if (schemeChanged || Game.Input.InputManager.instance.activeControlScheme == Game.Input.InputManager.ControlScheme.Gamepad)
		{
			m_PdxPlatform?.UpdateInputMode();
		}
	}

	private void UpdateLocale()
	{
		m_PdxPlatform?.ChangeModsUILanguage(locale);
	}

	public void Dispose()
	{
		Game.Input.InputManager.instance.EventActiveDeviceChanged -= OnActiveDeviceChanged;
		GameManager.instance.localizationManager.onActiveDictionaryChanged -= UpdateLocale;
		GameManager.instance.onFullscreenOverlayOpened -= Hide;
	}

	public InputMode GetInputMode()
	{
		Game.Input.InputManager.ControlScheme activeControlScheme = Game.Input.InputManager.instance.activeControlScheme;
		Game.Input.InputManager.GamepadType finalInputHintsType = SharedSettings.instance.userInterface.GetFinalInputHintsType();
		return activeControlScheme switch
		{
			Game.Input.InputManager.ControlScheme.KeyboardAndMouse => InputMode.KeyboardAndMouse, 
			Game.Input.InputManager.ControlScheme.Gamepad => finalInputHintsType switch
			{
				Game.Input.InputManager.GamepadType.Xbox => InputMode.XboxSeriesXS, 
				Game.Input.InputManager.GamepadType.PS => InputMode.PS5, 
				_ => throw new Exception($"Unknown control scheme {activeControlScheme} with gamepad {finalInputHintsType}"), 
			}, 
			_ => throw new Exception($"Unknown control scheme {activeControlScheme}"), 
		};
	}
}
