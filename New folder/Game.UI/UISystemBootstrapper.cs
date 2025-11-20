using System;
using System.Diagnostics;
using cohtml.Net;
using Colossal.FileSystem;
using Colossal.PSI.Environment;
using Colossal.UI;
using Colossal.UI.Fatal;
using Game.Input;
using Game.SceneFlow;
using Game.UI.Localization;
using UnityEngine;

namespace Game.UI;

public class UISystemBootstrapper : MonoBehaviour, IUIViewComponent
{
	private UIView m_View;

	private UIView m_FatalView;

	private UIManager m_UIManager;

	private UIInputSystem m_UIInputSystem;

	private UIInputSystem m_FallbackUIInputSystem;

	private InputBindings m_InputBindings;

	public bool m_EnableFatalUI;

	public string m_Url;

	public string m_FatalUrl;

	public View View => m_View.View;

	public IUnityViewListener Listener => m_View.Listener;

	private async void Awake()
	{
		UnityEngine.Debug.LogWarning("UISystemBootstrapper is only meant for development purpose");
		await Capabilities.CacheCapabilities();
		UIManager.log.Info("Bootstrapping cohtmlUISystem");
		InputManager.CreateInstance();
		m_UIManager = new UIManager(developerMode: true);
		Colossal.UI.UISystem.Settings settings = Colossal.UI.UISystem.Settings.New;
		settings.enableDebugger = true;
		if (GameManager.instance != null)
		{
			settings.localizationManager = new UILocalizationManager(GameManager.instance.localizationManager);
		}
		settings.resourceHandler = new GameUIResourceHandler(this);
		settings.enableDebugger = true;
		Colossal.UI.UISystem uISystem = m_UIManager.CreateUISystem(settings);
		uISystem.AddHostLocation("gameui", EnvPath.kContentPath + "/Game/UI");
		m_View = uISystem.CreateView(m_Url, UIView.Settings.New, GetComponent<Camera>());
		m_View.enabled = true;
		m_View.AudioSource = GetComponent<AudioSource>();
		m_View.Listener.ReadyForBindings += OnReadyForBindings;
		m_UIInputSystem = new UIInputSystem(uISystem);
		m_InputBindings = new InputBindings();
		if (!m_EnableFatalUI)
		{
			return;
		}
		ErrorPage errorPage = new ErrorPage();
		errorPage.AddAction("quit", delegate
		{
			Application.Quit();
		});
		errorPage.AddAction("visit", delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://pdxint.at/3Do979W",
					UseShellExecute = true
				});
			}
			catch
			{
				Application.Quit();
			}
		});
		errorPage.SetRoot(EnvPath.kContentPath + "/Game/UI/.fatal", EnvPath.kContentPath + "/Game/.fatal");
		errorPage.SetFonts(EnvPath.kContentPath + "/Game/UI/Fonts", EnvPath.kContentPath + "/Game/Fonts.cok");
		errorPage.SetStopCode(new AggregateException());
		Colossal.UI.UISystem.Settings settings2 = Colossal.UI.UISystem.Settings.New;
		settings2.resourceHandler = new FatalResourceHandler(errorPage);
		settings2.enableDebugger = true;
		settings2.debuggerPort = 9445;
		Colossal.UI.UISystem uISystem2 = m_UIManager.CreateUISystem(settings2);
		UIView.Settings settings3 = UIView.Settings.New;
		settings3.liveReload = true;
		m_FatalView = uISystem2.CreateView(m_FatalUrl, settings3, GetComponent<Camera>());
		m_FatalView.enabled = true;
		m_FallbackUIInputSystem = new UIInputSystem(uISystem2);
	}

	private void Update()
	{
		if (m_FatalView != null)
		{
			m_FatalView.enabled = m_EnableFatalUI;
		}
		InputManager.instance?.Update();
		m_UIManager?.Update();
		m_InputBindings?.Update();
	}

	private void LateUpdate()
	{
		m_UIInputSystem?.DispatchInputEvents();
		m_FallbackUIInputSystem?.DispatchInputEvents();
	}

	private void OnReadyForBindings()
	{
		m_InputBindings?.Attach(m_View.View);
	}

	private void OnDestroy()
	{
		if (m_View != null)
		{
			m_View.Listener.ReadyForBindings -= OnReadyForBindings;
		}
		m_InputBindings?.Detach();
		m_InputBindings?.Dispose();
		m_UIInputSystem?.Dispose();
		m_FallbackUIInputSystem?.Dispose();
		m_UIManager?.Dispose();
		InputManager.DestroyInstance();
	}

	bool IUIViewComponent.get_enabled()
	{
		return base.enabled;
	}
}
