using System;
using System.Threading.Tasks;
using cohtml.Net;
using Colossal.Localization;
using Colossal.Logging;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.Input;
using Game.PSI;
using Game.Settings;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using UnityEngine;

namespace Game.SceneFlow;

public class UserInterface : IDisposable
{
	private static ILog log = UIManager.log;

	private UICursorCollection m_CursorCollection;

	private CompositeBinding m_Bindings;

	private TaskCompletionSource<bool> m_BindingsReady;

	public UIView view { get; private set; }

	public LocalizationBindings localizationBindings { get; private set; }

	public OverlayBindings overlayBindings { get; private set; }

	public AppBindings appBindings { get; private set; }

	public InputHintBindings inputHintBindings { get; private set; }

	public ParadoxBindings paradoxBindings { get; private set; }

	public VirtualKeyboard virtualKeyboard { get; private set; }

	public IBindingRegistry bindings => m_Bindings;

	public UserInterface(string url, LocalizationManager localizationManager, ErrorDialogManager errorDialogManager, Colossal.UI.UISystem uiSystem)
	{
		m_BindingsReady = new TaskCompletionSource<bool>();
		m_CursorCollection = Resources.Load<UICursorCollection>("Input/UI Cursors");
		m_Bindings = new CompositeBinding();
		virtualKeyboard = new VirtualKeyboard();
		UIView.Settings settings = UIView.Settings.New;
		settings.textInputHandler = virtualKeyboard;
		settings.acceptsInput = true;
		view = uiSystem.CreateView(url, settings);
		view.Listener.ReadyForBindings += OnReadyForBindings;
		view.Listener.NavigateTo += OnNavigateTo;
		view.Listener.NodeMouseEvent += OnNodeMouseEvent;
		view.Listener.CursorChanged += OnCursorChanged;
		view.Listener.TextInputTypeChanged += OnTextInputTypeChanged;
		view.Listener.CaretRectChanged += OnCaretRectChanged;
		view.enabled = true;
		m_Bindings.AddUpdateBinding(this.localizationBindings = new LocalizationBindings(localizationManager));
		m_Bindings.AddUpdateBinding(this.appBindings = new AppBindings(errorDialogManager));
		m_Bindings.AddUpdateBinding(this.overlayBindings = new OverlayBindings());
		m_Bindings.AddUpdateBinding(new AudioBindings());
		m_Bindings.AddUpdateBinding(new UserBindings());
		m_Bindings.AddUpdateBinding(new InputBindings());
		m_Bindings.AddUpdateBinding(new InputActionBindings());
		m_Bindings.AddUpdateBinding(this.inputHintBindings = new InputHintBindings());
		m_Bindings.AddUpdateBinding(this.paradoxBindings = new ParadoxBindings());
		this.overlayBindings.hintMessages = localizationManager.activeDictionary.GetIndexedLocaleIDs("Loading.HINTMESSAGE");
		if (view.View.IsReadyForBindings())
		{
			OnReadyForBindings();
		}
		SharedSettings.instance.userState.onSettingsApplied += OnSettingsApplied;
	}

	private void OnSettingsApplied(Setting setting)
	{
		if (setting is UserState)
		{
			GameManager.instance.RunOnMainThread(delegate
			{
				appBindings.UpdateCanContinueBinding();
			});
		}
	}

	public void Update()
	{
		m_Bindings.Update();
	}

	public void Dispose()
	{
		SharedSettings.instance.userState.onSettingsApplied -= OnSettingsApplied;
		overlayBindings.DeactivateAllScreens();
		appBindings.activeUI = null;
		m_Bindings.DisposeBindings();
		if (m_Bindings.attached)
		{
			m_Bindings.Detach();
		}
		if (view != null)
		{
			view.Listener.ReadyForBindings -= OnReadyForBindings;
			view.Listener.NavigateTo -= OnNavigateTo;
			view.Listener.NodeMouseEvent -= OnNodeMouseEvent;
			view.Listener.CursorChanged -= OnCursorChanged;
			view.Listener.TextInputTypeChanged -= OnTextInputTypeChanged;
			view.uiSystem.DestroyView(view);
			view = null;
		}
	}

	public Task WaitForBindings()
	{
		return m_BindingsReady.Task;
	}

	private void OnReadyForBindings()
	{
		log.Debug("Ready for bindings");
		m_Bindings.Attach(view.View);
		appBindings.ready = true;
		m_BindingsReady.TrySetResult(result: true);
	}

	private bool OnNavigateTo(string url)
	{
		m_Bindings.Detach();
		return true;
	}

	private Actions OnNodeMouseEvent(INodeProxy node, IMouseEventData ev, IntPtr userData, PhaseType phaseType)
	{
		if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse && phaseType == PhaseType.AT_TARGET)
		{
			if (node.GetTag() == HTMLTag.BODY)
			{
				InputManager.instance.mouseOverUI = false;
			}
			else
			{
				InputManager.instance.mouseOverUI = true;
			}
		}
		return Actions.ContinueHandling;
	}

	private void OnCursorChanged(Cursors cursor, string url)
	{
		if (m_CursorCollection == null)
		{
			UICursorCollection.ResetCursor();
		}
		else if (cursor == Cursors.URL && url != null)
		{
			m_CursorCollection.SetCursor(url);
		}
		else
		{
			m_CursorCollection.SetCursor(cursor);
		}
	}

	private void OnTextInputTypeChanged(ControlType type)
	{
		InputManager.instance.hasInputFieldFocus = type == ControlType.TextInput;
	}

	private void OnCaretRectChanged(int x, int y, uint width, uint height)
	{
		InputManager.instance.caretRect = (new Vector2(x, y), new Vector2(width, height));
	}
}
