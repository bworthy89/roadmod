#define UNITY_ASSERTIONS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Colossal;
using Colossal.Logging;
using Colossal.PSI.Common;
using Game.Modding;
using Game.PSI;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;

namespace Game.Input;

public class InputManager : IDisposable, IInputActionCollection, IEnumerable<InputAction>, IEnumerable
{
	public readonly struct CompositeData
	{
		public readonly string m_TypeName;

		public readonly ActionType m_ActionType;

		public readonly IReadOnlyDictionary<ActionComponent, CompositeComponentData> m_Data;

		public CompositeData(string typeName, ActionType actionType, CompositeComponentData[] data)
		{
			m_TypeName = typeName;
			m_ActionType = actionType;
			m_Data = new ReadOnlyDictionary<ActionComponent, CompositeComponentData>(data.ToDictionary((CompositeComponentData d) => d.m_Component));
		}

		public bool TryGetData(ActionComponent component, out CompositeComponentData data)
		{
			return m_Data.TryGetValue(component, out data);
		}

		public bool TryFindByBindingName(string bindingName, out CompositeComponentData data)
		{
			foreach (CompositeComponentData value in m_Data.Values)
			{
				if (value.m_BindingName == bindingName)
				{
					data = value;
					return true;
				}
			}
			data = default(CompositeComponentData);
			return false;
		}
	}

	public readonly struct CompositeComponentData
	{
		public static CompositeComponentData defaultData = new CompositeComponentData(ActionComponent.Press, "binding", "modifier");

		public readonly ActionComponent m_Component;

		public readonly string m_BindingName;

		public readonly string m_ModifierName;

		public CompositeComponentData(ActionComponent component, string bindingName, string modifierName)
		{
			m_Component = component;
			m_BindingName = bindingName;
			m_ModifierName = modifierName;
		}
	}

	public delegate void ActiveDeviceChanged(InputDevice newDevice, InputDevice oldDevice, bool schemeChanged);

	public enum PathType
	{
		Effective,
		Original,
		Overridden
	}

	[Flags]
	public enum BindingOptions
	{
		None = 0,
		OnlyOriginal = 1,
		OnlyRebindable = 2,
		OnlyRebound = 4,
		OnlyBuiltIn = 8,
		ExcludeDummy = 0x10,
		ExcludeHidden = 0x20
	}

	public enum ControlScheme : byte
	{
		KeyboardAndMouse,
		Gamepad
	}

	[Flags]
	public enum DeviceType
	{
		None = 0,
		Keyboard = 1,
		Mouse = 2,
		Gamepad = 4,
		All = 7
	}

	public enum GamepadType
	{
		Xbox,
		PS
	}

	internal class DeferManagerUpdatingWrapper : IDisposable
	{
		private static int sDeferUpdating;

		private readonly InputActionRebindingExtensions.DeferBindingResolutionWrapper m_BindingResolution;

		public bool isDeferred => sDeferUpdating != 0;

		internal DeferManagerUpdatingWrapper()
		{
			m_BindingResolution = new InputActionRebindingExtensions.DeferBindingResolutionWrapper();
		}

		public void Acquire()
		{
			sDeferUpdating++;
			m_BindingResolution.Acquire();
			ProxyAction.sDeferUpdatingWrapper.Acquire();
		}

		public void Dispose()
		{
			m_BindingResolution.Dispose();
			if (InputActionMap.s_DeferBindingResolution == 0 && instance != null)
			{
				foreach (KeyValuePair<string, ProxyActionMap> map in instance.m_Maps)
				{
					map.Deconstruct(out var _, out var value);
					value.sourceMap.ResolveBindingsIfNecessary();
				}
			}
			ProxyAction.sDeferUpdatingWrapper.Dispose();
			if (sDeferUpdating > 0)
			{
				sDeferUpdating--;
			}
			if (sDeferUpdating == 0)
			{
				try
				{
					sDeferUpdating++;
					instance?.ProcessActionsUpdate(ignoreDefer: true);
				}
				finally
				{
					sDeferUpdating--;
				}
			}
		}
	}

	private bool m_NeedUpdate;

	private static IReadOnlyList<CompositeData> m_Composites = Array.Empty<CompositeData>();

	private static InputControlLayout.Cache m_LayoutCache;

	private static StringBuilder m_PathBuilder;

	public const string kShiftName = "<Keyboard>/shift";

	public const string kCtrlName = "<Keyboard>/ctrl";

	public const string kAltName = "<Keyboard>/alt";

	public const string kLeftStick = "<Gamepad>/leftStickPress";

	public const string kRightStick = "<Gamepad>/rightStickPress";

	public const string kSplashScreenMap = "Splash screen";

	public const string kNavigationMap = "Navigation";

	public const string kMenuMap = "Menu";

	public const string kCameraMap = "Camera";

	public const string kToolMap = "Tool";

	public const string kShortcutsMap = "Shortcuts";

	public const string kPhotoModeMap = "Photo mode";

	public const string kEditorMap = "Editor";

	public const string kDebugMap = "Debug";

	public const string kEngagementMap = "Engagement";

	public const int kIdleDelay = 30;

	private static Dictionary<DeviceType, HashSet<string>> kModifiers = new Dictionary<DeviceType, HashSet<string>>
	{
		{
			DeviceType.Keyboard,
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "<Keyboard>/shift", "<Keyboard>/ctrl", "<Keyboard>/alt" }
		},
		{
			DeviceType.Mouse,
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "<Keyboard>/shift", "<Keyboard>/ctrl", "<Keyboard>/alt" }
		},
		{
			DeviceType.Gamepad,
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "<Gamepad>/leftStickPress", "<Gamepad>/rightStickPress" }
		}
	};

	private static InputManager s_Instance;

	public static readonly ILog log = LogManager.GetLogger("InputManager");

	private readonly InputConflictResolution m_ConflictResolution = new InputConflictResolution();

	private readonly InputActionAsset m_ActionAsset;

	private readonly UIInputActionCollection m_UIActionCollection;

	private readonly UIInputActionCollection m_ToolActionCollection;

	private readonly Dictionary<string, ProxyActionMap> m_Maps = new Dictionary<string, ProxyActionMap>();

	private Dictionary<InputDevice, DeviceListener> m_DeviceListeners;

	private InputDevice m_LastActiveDevice;

	private bool m_MouseOverUI;

	private float m_AccumulatedIdleDelay;

	private bool m_WasWorldReady;

	private bool m_Idle;

	private bool m_HasFocus;

	private bool m_HasInputFieldFocus;

	private bool m_OverlayActive;

	private bool m_HideCursor;

	private ControlScheme m_ActiveControlScheme;

	private InputActionMap.DeviceArray m_Devices;

	private DeviceType m_ConnectedDeviceTypes;

	private DeviceType m_BlockedControlTypes;

	private DeviceType m_Mask;

	private readonly Dictionary<ProxyBinding, ProxyBinding.Watcher> m_ProxyBindingWatchers = new Dictionary<ProxyBinding, ProxyBinding.Watcher>(new ProxyBinding.Comparer(ProxyBinding.Comparer.Options.MapName | ProxyBinding.Comparer.Options.ActionName | ProxyBinding.Comparer.Options.Name | ProxyBinding.Comparer.Options.Device | ProxyBinding.Comparer.Options.Component));

	private static string m_ProhibitionModifierProcessor;

	private const string kKeyBindingConflict = "KeyBindingConflict";

	private const string kKeyBindingConflictResolved = "KeyBindingConflictResolved";

	private static readonly DeferManagerUpdatingWrapper sDeferUpdatingWrapper = new DeferManagerUpdatingWrapper();

	public IEnumerable<ProxyAction> actions
	{
		get
		{
			foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
			{
				map.Deconstruct(out var key, out var value);
				ProxyActionMap proxyActionMap = value;
				foreach (KeyValuePair<string, ProxyAction> action in proxyActionMap.actions)
				{
					action.Deconstruct(out key, out var value2);
					yield return value2;
				}
			}
		}
	}

	public static InputManager instance => s_Instance;

	public bool mouseOverUI
	{
		get
		{
			return m_MouseOverUI;
		}
		set
		{
			if (value != m_MouseOverUI)
			{
				m_MouseOverUI = value;
				this.EventMouseOverUIChanged?.Invoke(value);
			}
		}
	}

	public bool hasInputFieldFocus
	{
		get
		{
			return m_HasInputFieldFocus;
		}
		set
		{
			if (value != m_HasInputFieldFocus)
			{
				log.VerboseFormat("Has input field focus: {0}", value);
			}
			m_HasInputFieldFocus = value;
		}
	}

	public bool overlayActive => m_OverlayActive;

	public (Vector2, Vector2) caretRect { get; set; }

	public bool controlOverWorld
	{
		get
		{
			if (!mouseOverUI || activeControlScheme != ControlScheme.KeyboardAndMouse)
			{
				return mouseOnScreen;
			}
			return false;
		}
	}

	InputBinding? IInputActionCollection.bindingMask
	{
		get
		{
			return mask.ToInputBinding();
		}
		set
		{
			mask = value.ToDeviceType();
		}
	}

	ReadOnlyArray<InputDevice>? IInputActionCollection.devices
	{
		get
		{
			return m_Devices.Get();
		}
		set
		{
			if (!m_Devices.Set(value))
			{
				return;
			}
			if (value.HasValue)
			{
				foreach (InputDevice item in value.Value)
				{
					log.VerboseFormat("Device: {0}", item.description.ToString());
				}
			}
			else
			{
				log.VerboseFormat("Device: null");
			}
			foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
			{
				map.Deconstruct(out var _, out var value2);
				value2.sourceMap.devices = value;
			}
		}
	}

	ReadOnlyArray<InputControlScheme> IInputActionCollection.controlSchemes => m_ActionAsset.controlSchemes;

	public ControlScheme activeControlScheme
	{
		get
		{
			return m_ActiveControlScheme;
		}
		private set
		{
			if (m_ActiveControlScheme != value)
			{
				log.VerboseFormat("Active control scheme set: {0}", value);
				m_ActiveControlScheme = value;
				UpdateCursorVisibility();
				RefreshActiveControl();
				this.EventControlSchemeChanged?.Invoke(value);
				Telemetry.ControlSchemeChanged(value);
			}
		}
	}

	public bool isGamepadControlSchemeActive => activeControlScheme == ControlScheme.Gamepad;

	public bool isKeyboardAndMouseControlSchemeActive => activeControlScheme == ControlScheme.KeyboardAndMouse;

	public DeviceType connectedDeviceTypes => m_ConnectedDeviceTypes;

	internal DeviceType mask
	{
		get
		{
			return m_Mask;
		}
		set
		{
			if (value == m_Mask)
			{
				return;
			}
			log.VerboseFormat("Set mask: {0}", value);
			m_Mask = value;
			foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
			{
				map.Deconstruct(out var _, out var value2);
				value2.mask = value;
			}
		}
	}

	internal DeviceType blockedControlTypes
	{
		get
		{
			return m_BlockedControlTypes;
		}
		set
		{
			if (value != m_BlockedControlTypes)
			{
				log.VerboseFormat("Block control types: {0}", value);
				m_BlockedControlTypes = value;
				RefreshActiveControl();
			}
		}
	}

	public bool mouseOnScreen
	{
		get
		{
			if (mousePosition.x >= 0f && mousePosition.x < (float)Screen.width && mousePosition.y >= 0f && mousePosition.y < (float)Screen.height)
			{
				return m_HasFocus;
			}
			return false;
		}
	}

	public Vector2 gamepadPointerPosition => new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);

	public Vector3 mousePosition
	{
		get
		{
			Mouse current = Mouse.current;
			if (activeControlScheme == ControlScheme.KeyboardAndMouse && current != null)
			{
				return current.position.ReadValue();
			}
			return gamepadPointerPosition;
		}
	}

	public bool hideCursor
	{
		get
		{
			return m_HideCursor;
		}
		set
		{
			if (value != m_HideCursor)
			{
				m_HideCursor = value;
				UpdateCursorVisibility();
			}
		}
	}

	public CursorLockMode cursorLockMode
	{
		get
		{
			return Cursor.lockState;
		}
		set
		{
			Cursor.lockState = value;
		}
	}

	public InputUser inputUser { get; private set; }

	public int actionVersion { get; private set; }

	internal Dictionary<string, HashSet<ProxyAction>> keyActionMap { get; } = new Dictionary<string, HashSet<ProxyAction>>(StringComparer.OrdinalIgnoreCase);

	internal Dictionary<ProxyAction, HashSet<string>> actionKeyMap { get; } = new Dictionary<ProxyAction, HashSet<string>>();

	internal Dictionary<int, ProxyAction> actionIndex { get; } = new Dictionary<int, ProxyAction>();

	private static string prohibitionModifierProcessor => m_ProhibitionModifierProcessor ?? (m_ProhibitionModifierProcessor = InputProcessor.s_Processors.FindNameForType(typeof(ProhibitionModifierProcessor)));

	internal UIInputActionCollection uiActionCollection => m_UIActionCollection;

	internal UIInputActionCollection toolActionCollection => m_ToolActionCollection;

	public DeviceType bindingConflicts { get; private set; }

	public static bool IsKeyboardConnected => (instance.connectedDeviceTypes & DeviceType.Keyboard) != 0;

	public static bool IsMouseConnected => (instance.connectedDeviceTypes & DeviceType.Mouse) != 0;

	public static bool IsGamepadConnected => (instance.connectedDeviceTypes & DeviceType.Gamepad) != 0;

	public event Action<ControlScheme> EventControlSchemeChanged;

	public event ActiveDeviceChanged EventActiveDeviceChanged;

	public event Action EventActiveDeviceDisconnected;

	public event Action EventActiveDeviceAssociationLost;

	public event Action EventDevicePaired;

	public event Action EventActionsChanged;

	public event Action EventEnabledActionsChanged;

	public event Action EventActionMasksChanged;

	public event Action EventActionDisplayNamesChanged;

	public event Action<bool> EventMouseOverUIChanged;

	internal event Action EventPreResolvedActionChanged;

	public event Action EventConflictResolved;

	public ProxyAction FindAction(string mapName, string actionName)
	{
		return FindActionMap(mapName)?.FindAction(actionName);
	}

	public bool TryFindAction(string mapName, string actionName, out ProxyAction action)
	{
		action = FindAction(mapName, actionName);
		return action != null;
	}

	public ProxyAction FindAction(ProxyBinding binding)
	{
		return FindActionMap(binding.mapName)?.FindAction(binding.actionName);
	}

	public bool TryFindAction(ProxyBinding binding, out ProxyAction action)
	{
		action = FindAction(binding.mapName, binding.actionName);
		return action != null;
	}

	public ProxyAction FindAction(InputAction action)
	{
		return FindActionMap(action?.actionMap)?.FindAction(action);
	}

	public bool TryFindAction(InputAction action, out ProxyAction proxyAction)
	{
		proxyAction = FindAction(action);
		return proxyAction != null;
	}

	public ProxyAction FindAction(Guid guid)
	{
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var key, out var value);
			foreach (KeyValuePair<string, ProxyAction> action in value.actions)
			{
				action.Deconstruct(out key, out var value2);
				ProxyAction proxyAction = value2;
				if (proxyAction.sourceAction.id == guid)
				{
					value2 = proxyAction;
					return value2;
				}
			}
		}
		return null;
	}

	public bool TryFindAction(Guid guid, out ProxyAction proxyAction)
	{
		proxyAction = FindAction(guid);
		return proxyAction != null;
	}

	internal bool TryFindAction(int index, out ProxyAction action)
	{
		return actionIndex.TryGetValue(index, out action);
	}

	private void RefreshActiveControl()
	{
		mask = GetMaskForControlScheme();
		if (m_ActiveControlScheme == ControlScheme.KeyboardAndMouse && Keyboard.current != null && Keyboard.current.added)
		{
			UnityEngine.Input.imeCompositionMode = (hasInputFieldFocus ? IMECompositionMode.On : IMECompositionMode.Off);
			Keyboard.current.SetIMEEnabled(hasInputFieldFocus);
			Keyboard.current.SetIMECursorPosition(caretRect.Item1 + caretRect.Item2);
		}
	}

	private DeviceType GetMaskForControlScheme()
	{
		return (DeviceType)((activeControlScheme switch
		{
			ControlScheme.KeyboardAndMouse => (!overlayActive) ? (hasInputFieldFocus ? 2 : 3) : 0, 
			ControlScheme.Gamepad => (!overlayActive) ? 4 : 0, 
			_ => 0, 
		}) & (int)(~blockedControlTypes));
	}

	internal void OnActionChanged()
	{
		if (sDeferUpdatingWrapper.isDeferred)
		{
			m_NeedUpdate = true;
		}
		else
		{
			ProcessActionsUpdate();
		}
	}

	private void ProcessActionsUpdate(bool ignoreDefer = false)
	{
		if ((!sDeferUpdatingWrapper.isDeferred || ignoreDefer) && m_NeedUpdate)
		{
			m_NeedUpdate = false;
			actionVersion++;
			CheckConflicts();
			this.EventActionsChanged?.Invoke();
		}
	}

	internal void AddActions(ProxyAction.Info[] actionsToAdd)
	{
		ProxyAction[] array = new ProxyAction[actionsToAdd.Length];
		using (DeferUpdating())
		{
			for (int i = 0; i < actionsToAdd.Length; i++)
			{
				ProxyActionMap orCreateMap = GetOrCreateMap(actionsToAdd[i].m_Map);
				array[i] = orCreateMap.AddAction(actionsToAdd[i], bulk: true);
			}
		}
		ProxyActionMap[] array2 = array.Select((ProxyAction a) => a.map).Distinct().ToArray();
		for (int num = 0; num < array2.Length; num++)
		{
			array2[num].UpdateState();
		}
	}

	internal void UpdateActionInKeyActionMap(ProxyAction action)
	{
		string[] array;
		string[] array2;
		if (!actionKeyMap.TryGetValue(action, out var value))
		{
			array = action.usedKeys.ToArray();
			array2 = Array.Empty<string>();
			actionKeyMap[action] = new HashSet<string>(array);
		}
		else
		{
			HashSet<string> hashSet = action.usedKeys.ToHashSet();
			array = hashSet.Except(value).ToArray();
			array2 = value.Except(hashSet).ToArray();
			actionKeyMap[action] = hashSet;
		}
		string[] array3 = array2;
		foreach (string key in array3)
		{
			if (keyActionMap.TryGetValue(key, out var value2))
			{
				value2.Remove(action);
			}
		}
		array3 = array;
		foreach (string key2 in array3)
		{
			if (!keyActionMap.TryGetValue(key2, out var value3))
			{
				value3 = new HashSet<ProxyAction>();
				keyActionMap.Add(key2, value3);
			}
			value3.Add(action);
		}
	}

	public static bool HasConflicts(ProxyAction action1, ProxyAction action2, DeviceType? maskOverride1 = null, DeviceType? maskOverride2 = null)
	{
		DeviceType deviceType = maskOverride1 ?? action1.mask;
		DeviceType deviceType2 = maskOverride2 ?? action2.mask;
		foreach (KeyValuePair<DeviceType, ProxyComposite> composite in action1.composites)
		{
			composite.Deconstruct(out var key, out var value);
			ProxyComposite proxyComposite = value;
			if ((proxyComposite.m_Device & deviceType) == 0)
			{
				continue;
			}
			foreach (KeyValuePair<DeviceType, ProxyComposite> composite2 in action2.composites)
			{
				composite2.Deconstruct(out key, out value);
				ProxyComposite proxyComposite2 = value;
				if ((proxyComposite2.m_Device & deviceType2) == 0)
				{
					continue;
				}
				foreach (KeyValuePair<ActionComponent, ProxyBinding> binding in proxyComposite.bindings)
				{
					binding.Deconstruct(out var key2, out var value2);
					ProxyBinding x = value2;
					foreach (KeyValuePair<ActionComponent, ProxyBinding> binding2 in proxyComposite2.bindings)
					{
						binding2.Deconstruct(out key2, out value2);
						ProxyBinding y = value2;
						if ((action1 != action2 || x.component != y.component) && ProxyBinding.ConflictsWith(x, y, checkUsage: false))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static bool CanConflict(ProxyAction action1, ProxyAction action2, DeviceType device)
	{
		if (action1 == action2)
		{
			return false;
		}
		if (action1.m_LinkedActions.Contains(new ProxyAction.LinkInfo
		{
			m_Action = action2,
			m_Device = device
		}))
		{
			return false;
		}
		if (action2.m_LinkedActions.Contains(new ProxyAction.LinkInfo
		{
			m_Action = action1,
			m_Device = device
		}))
		{
			return false;
		}
		return true;
	}

	public List<ProxyBinding> GetBindings(PathType pathType, BindingOptions bindingOptions)
	{
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.TraceFormat("Get {1} bindings {2} in {0}ms", t.TotalMilliseconds, pathType, bindingOptions);
		}))
		{
			List<ProxyBinding> bindingsList = new List<ProxyBinding>();
			foreach (ProxyActionMap value in m_Maps.Values)
			{
				InputAction[] array = value.sourceMap.m_Actions;
				foreach (InputAction action in array)
				{
					action.ForEachCompositeOfAction(delegate(InputActionSetupExtensions.BindingSyntax iterator)
					{
						if (TryGetComposite(action, iterator, pathType, bindingOptions, out var proxyComposite))
						{
							foreach (var (_, item) in proxyComposite.bindings)
							{
								bindingsList.Add(item);
							}
						}
						return true;
					});
				}
			}
			return bindingsList;
		}
	}

	public bool TryGetBinding(ProxyBinding bindingToGet, PathType pathType, BindingOptions bindingOptions, out ProxyBinding foundBinding)
	{
		foundBinding = default(ProxyBinding);
		if (!TryFindAction(bindingToGet, out var action) || action.sourceAction == null)
		{
			return false;
		}
		if (!TryGetIterators(bindingToGet, action.sourceAction, out var compositeIterator, out var bindingIterator, out var compositeInstance, out var componentData))
		{
			return false;
		}
		return TryGetBinding(action.sourceAction, compositeIterator, bindingIterator, compositeInstance, componentData, pathType, bindingOptions, out foundBinding);
	}

	private bool TryGetBinding(InputAction action, InputActionSetupExtensions.BindingSyntax compositeIterator, InputActionSetupExtensions.BindingSyntax bindingIterator, CompositeInstance compositeInstance, CompositeComponentData componentData, PathType pathType, BindingOptions bindingOptions, out ProxyBinding foundBinding)
	{
		bool num = (bindingOptions & BindingOptions.OnlyRebound) != 0;
		InputBinding binding = bindingIterator.binding;
		bool flag = TryGetMainBinding(bindingIterator, pathType, out var currentPath, out var originalPath);
		flag |= TryGetModifierBindings(action, compositeInstance, compositeIterator, bindingIterator, pathType, componentData, out var currentModifiers, out var originalModifiers);
		if (num && !flag)
		{
			foundBinding = default(ProxyBinding);
			return false;
		}
		foundBinding = new ProxyBinding(action, componentData.m_Component, binding.name, compositeInstance)
		{
			path = currentPath,
			modifiers = currentModifiers,
			originalPath = originalPath,
			originalModifiers = originalModifiers,
			device = compositeIterator.binding.name.ToDeviceType()
		};
		return true;
	}

	public bool TryGetModifierBindings(InputAction action, CompositeInstance compositeInstance, InputActionSetupExtensions.BindingSyntax compositeIterator, InputActionSetupExtensions.BindingSyntax iterator, PathType pathType, CompositeComponentData componentData, out ProxyModifier[] currentModifiers, out ProxyModifier[] originalModifiers)
	{
		currentModifiers = null;
		originalModifiers = null;
		if (compositeInstance.modifierOptions != ModifierOptions.Allow)
		{
			return false;
		}
		if (!kModifiers.TryGetValue(compositeIterator.binding.name.ToDeviceType(), out var supportedModifiers))
		{
			return false;
		}
		bool isRebound = false;
		List<ProxyModifier> currentModifierList = new List<ProxyModifier>();
		List<ProxyModifier> originalModifierList = new List<ProxyModifier>();
		action.ForEachPartOfCompositeWithName(compositeIterator, componentData.m_ModifierName, delegate(InputActionSetupExtensions.BindingSyntax modifierIterator)
		{
			InputBinding binding = modifierIterator.binding;
			if (string.IsNullOrEmpty(binding.path))
			{
				return true;
			}
			if (!supportedModifiers.Contains(binding.path))
			{
				return true;
			}
			isRebound |= binding.overrideProcessors != null;
			if (!binding.GetProcessors(pathType).Contains(prohibitionModifierProcessor))
			{
				currentModifierList.Add(new ProxyModifier
				{
					m_Component = componentData.m_Component,
					m_Name = binding.name,
					m_Path = binding.path
				});
			}
			if (!binding.processors.Contains(prohibitionModifierProcessor))
			{
				originalModifierList.Add(new ProxyModifier
				{
					m_Component = componentData.m_Component,
					m_Name = binding.name,
					m_Path = binding.path
				});
			}
			return true;
		}, out var _);
		currentModifiers = currentModifierList.ToArray();
		originalModifiers = originalModifierList.ToArray();
		return isRebound;
	}

	public bool TryGetMainBinding(InputActionSetupExtensions.BindingSyntax iterator, PathType pathType, out string currentPath, out string originalPath)
	{
		InputBinding binding = iterator.binding;
		currentPath = binding.GetPath(pathType);
		originalPath = binding.path;
		return binding.overridePath != null;
	}

	public bool SetBindings(IEnumerable<ProxyBinding> newBindings, out List<ProxyBinding> resultBindings)
	{
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.TraceFormat("Set bindings in {0}ms", t.TotalMilliseconds);
		}))
		{
			resultBindings = new List<ProxyBinding>();
			using (DeferUpdating())
			{
				foreach (ProxyBinding newBinding2 in newBindings)
				{
					SetBindingImpl(newBinding2, out var newBinding);
					resultBindings.Add(newBinding);
				}
			}
			return true;
		}
	}

	public bool SetBinding(ProxyBinding newBinding, out ProxyBinding result)
	{
		string bindingName = newBinding.ToString();
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.TraceFormat("Set binding {1} in {0}ms", t.TotalMilliseconds, bindingName);
		}))
		{
			using (DeferUpdating())
			{
				if (!SetBindingImpl(newBinding, out result))
				{
					return false;
				}
			}
			return true;
		}
	}

	private bool SetBindingImpl(ProxyBinding bindingToSet, out ProxyBinding newBinding)
	{
		if (!TryFindAction(bindingToSet.mapName, bindingToSet.actionName, out var action) || action.sourceAction == null)
		{
			newBinding = default(ProxyBinding);
			return false;
		}
		if (!TryGetIterators(bindingToSet, action.sourceAction, out var compositeIterator, out var bindingIterator, out var compositeInstance, out var componentData))
		{
			newBinding = default(ProxyBinding);
			return false;
		}
		switch (compositeInstance.rebindOptions)
		{
		case RebindOptions.None:
			newBinding = default(ProxyBinding);
			return false;
		case RebindOptions.Key:
		{
			if (TryGetModifierBindings(action.sourceAction, compositeInstance, compositeIterator, bindingIterator, PathType.Original, componentData, out var _, out var originalModifiers) && !ProxyBinding.ModifiersListComparer.defaultComparer.Equals(bindingToSet.modifiers, (IReadOnlyCollection<ProxyModifier>)(object)originalModifiers))
			{
				newBinding = default(ProxyBinding);
				return false;
			}
			break;
		}
		case RebindOptions.Modifiers:
		{
			if (TryGetMainBinding(bindingIterator, PathType.Original, out var _, out var originalPath) && bindingToSet.path != originalPath)
			{
				newBinding = default(ProxyBinding);
				return false;
			}
			break;
		}
		}
		if (string.IsNullOrEmpty(bindingToSet.path) && !compositeInstance.canBeEmpty)
		{
			newBinding = default(ProxyBinding);
			return false;
		}
		if (!TrySetMainBinding(bindingToSet, action.sourceAction, bindingIterator, out var changed) || !TrySetModifierBindings(bindingToSet, action.sourceAction, compositeInstance, componentData, compositeIterator, bindingIterator, out var changed2))
		{
			newBinding = default(ProxyBinding);
			return false;
		}
		if (!changed && !changed2)
		{
			newBinding = default(ProxyBinding);
			return false;
		}
		action.Update();
		return TryGetBinding(action.sourceAction, compositeIterator, bindingIterator, compositeInstance, componentData, PathType.Effective, BindingOptions.None, out newBinding);
	}

	private bool TrySetMainBinding(ProxyBinding bindingToSet, InputAction action, InputActionSetupExtensions.BindingSyntax bindingIterator, out bool changed)
	{
		InputBinding binding = bindingIterator.binding;
		if (bindingToSet.path == binding.path)
		{
			if (binding.overridePath != null)
			{
				binding.overridePath = null;
				action.actionMap.ApplyBindingOverride(bindingIterator.m_BindingIndexInMap, binding);
				changed = true;
				return true;
			}
		}
		else if (bindingToSet.path != binding.overridePath)
		{
			binding.overridePath = bindingToSet.path;
			action.actionMap.ApplyBindingOverride(bindingIterator.m_BindingIndexInMap, binding);
			changed = true;
			return true;
		}
		changed = false;
		return true;
	}

	private bool TrySetModifierBindings(ProxyBinding bindingToSet, InputAction action, CompositeInstance compositeInstance, CompositeComponentData componentData, InputActionSetupExtensions.BindingSyntax compositeIterator, InputActionSetupExtensions.BindingSyntax bindingIterator, out bool changed)
	{
		if (compositeInstance.modifierOptions != ModifierOptions.Allow)
		{
			changed = false;
			return true;
		}
		if (!kModifiers.TryGetValue(compositeIterator.binding.name.ToDeviceType(), out var supportedModifiers))
		{
			supportedModifiers = new HashSet<string>();
		}
		bool changedModifier = false;
		IReadOnlyList<ProxyModifier> modifiers = bindingToSet.modifiers;
		action.ForEachPartOfCompositeWithName(compositeIterator, componentData.m_ModifierName, delegate(InputActionSetupExtensions.BindingSyntax modifierIterator)
		{
			InputBinding modifierBinding = modifierIterator.binding;
			if (string.IsNullOrEmpty(modifierBinding.path))
			{
				return true;
			}
			if (!supportedModifiers.Contains(modifierBinding.path))
			{
				return true;
			}
			bool allow = modifiers.Any((ProxyModifier m) => StringComparer.OrdinalIgnoreCase.Equals(m.m_Path, modifierBinding.path));
			changedModifier |= TrySetBindingModifierProcessor(action, modifierIterator, allow);
			return true;
		}, out var _);
		changed = changedModifier;
		return true;
	}

	private bool TrySetBindingModifierProcessor(InputAction action, InputActionSetupExtensions.BindingSyntax modifierIterator, bool allow)
	{
		InputBinding binding = modifierIterator.binding;
		string text;
		if (allow)
		{
			if (string.IsNullOrEmpty(binding.effectiveProcessors) || binding.effectiveProcessors == prohibitionModifierProcessor)
			{
				text = string.Empty;
			}
			else
			{
				string[] source = binding.effectiveProcessors.Split(';', StringSplitOptions.RemoveEmptyEntries);
				text = string.Join(";", source.Select((string p) => p != prohibitionModifierProcessor));
			}
		}
		else if (string.IsNullOrEmpty(binding.effectiveProcessors) || binding.effectiveProcessors == prohibitionModifierProcessor)
		{
			text = prohibitionModifierProcessor;
		}
		else
		{
			string[] source2 = binding.effectiveProcessors.Split(';', StringSplitOptions.RemoveEmptyEntries);
			text = (source2.Any((string p) => p == prohibitionModifierProcessor) ? binding.effectiveProcessors : string.Join(";", source2.Append(prohibitionModifierProcessor)));
		}
		if (text == binding.processors)
		{
			if (binding.overrideProcessors != null)
			{
				binding.overrideProcessors = null;
				action.actionMap.ApplyBindingOverride(modifierIterator.m_BindingIndexInMap, binding);
				return true;
			}
		}
		else if (text != binding.overrideProcessors)
		{
			binding.overrideProcessors = text;
			action.actionMap.ApplyBindingOverride(modifierIterator.m_BindingIndexInMap, binding);
			return true;
		}
		return false;
	}

	public void ResetAllBindings(bool onlyBuiltIn = true)
	{
		List<ProxyBinding> bindings = GetBindings(PathType.Original, (BindingOptions)(4 | (onlyBuiltIn ? 8 : 0)));
		SetBindings(bindings, out var _);
	}

	private bool TryGetIterators(ProxyBinding bindingSample, InputAction action, out InputActionSetupExtensions.BindingSyntax compositeIterator, out InputActionSetupExtensions.BindingSyntax bindingIterator, out CompositeInstance compositeInstance, out CompositeComponentData componentData)
	{
		compositeIterator = default(InputActionSetupExtensions.BindingSyntax);
		bindingIterator = default(InputActionSetupExtensions.BindingSyntax);
		compositeInstance = null;
		componentData = default(CompositeComponentData);
		if (!action.TryGetCompositeOfActionWithName(bindingSample.device.ToString(), out compositeIterator))
		{
			return false;
		}
		if (!TryGetCompositeInstance(compositeIterator, out compositeInstance))
		{
			return false;
		}
		if (bindingSample.component == ActionComponent.None)
		{
			if (!compositeInstance.compositeData.TryFindByBindingName(bindingSample.name, out componentData))
			{
				return false;
			}
		}
		else if (!compositeInstance.compositeData.TryGetData(bindingSample.component, out componentData))
		{
			return false;
		}
		bindingIterator = compositeIterator.NextPartBinding(componentData.m_BindingName);
		return bindingIterator.valid;
	}

	public void ResetGroupBindings(DeviceType device, bool onlyBuiltIn = true)
	{
		List<ProxyBinding> bindings = GetBindings(PathType.Original, (BindingOptions)(4 | (onlyBuiltIn ? 8 : 0)));
		SetBindings(bindings.Where((ProxyBinding b) => b.device == device), out var _);
	}

	internal ProxyBinding.Watcher GetOrCreateBindingWatcher(ProxyBinding binding)
	{
		if (!m_ProxyBindingWatchers.TryGetValue(binding, out var value))
		{
			value = new ProxyBinding.Watcher(binding);
			if (value.isValid)
			{
				m_ProxyBindingWatchers[binding] = value;
			}
		}
		return value;
	}

	public List<ProxyComposite> GetComposites(InputAction action)
	{
		List<ProxyComposite> composites = new List<ProxyComposite>();
		action.ForEachCompositeOfAction(delegate(InputActionSetupExtensions.BindingSyntax iterator)
		{
			if (TryGetComposite(action, iterator, PathType.Effective, BindingOptions.None, out var proxyComposite))
			{
				composites.Add(proxyComposite);
			}
			return true;
		});
		return composites;
	}

	private bool TryGetComposite(InputAction action, InputActionSetupExtensions.BindingSyntax compositeIterator, PathType pathType, BindingOptions bindingOptions, out ProxyComposite proxyComposite)
	{
		List<ProxyBinding> bindingsList = new List<ProxyBinding>();
		proxyComposite = null;
		if (!TryGetCompositeInstance(compositeIterator, out var compositeInstance))
		{
			return false;
		}
		if (compositeInstance.developerOnly && GameManager.instance != null && !GameManager.instance.configuration.developerMode)
		{
			return false;
		}
		if (!compositeInstance.platform.IsPlatformSet(Application.platform))
		{
			return false;
		}
		if (!compositeInstance.builtIn && (bindingOptions & BindingOptions.OnlyBuiltIn) != BindingOptions.None)
		{
			return false;
		}
		if (!compositeInstance.isKeyRebindable && (bindingOptions & BindingOptions.OnlyRebindable) != BindingOptions.None)
		{
			return false;
		}
		if (compositeInstance.isDummy && (bindingOptions & BindingOptions.ExcludeDummy) != BindingOptions.None)
		{
			return false;
		}
		if (compositeInstance.isHidden && (bindingOptions & BindingOptions.ExcludeHidden) != BindingOptions.None)
		{
			return false;
		}
		foreach (CompositeComponentData componentData in compositeInstance.compositeData.m_Data.Values)
		{
			action.ForEachPartOfCompositeWithName(compositeIterator, componentData.m_BindingName, delegate(InputActionSetupExtensions.BindingSyntax bindingIterator)
			{
				if (TryGetBinding(action, compositeIterator, bindingIterator, compositeInstance, componentData, pathType, bindingOptions, out var foundBinding))
				{
					bindingsList.Add(foundBinding);
				}
				return true;
			}, out var _);
		}
		if (bindingsList.Count == 0)
		{
			return false;
		}
		proxyComposite = new ProxyComposite(compositeIterator.binding.name.ToDeviceType(), compositeInstance.compositeData.m_ActionType, compositeInstance, bindingsList);
		return true;
	}

	private bool TryGetCompositeInstance(InputActionSetupExtensions.BindingSyntax compositeIterator, out CompositeInstance compositeInstance)
	{
		NameAndParameters[] array = NameAndParameters.ParseMultiple(compositeIterator.binding.effectivePath).ToArray();
		if (array.Length == 2 && array[1].name == "Usages")
		{
			compositeInstance = new CompositeInstance(array[0], array[1]);
		}
		else
		{
			compositeInstance = new CompositeInstance(array[0]);
		}
		return compositeInstance != null;
	}

	public static string GeneratePathForControl(InputControl control)
	{
		InputDevice device = control.device;
		UnityEngine.Debug.Assert(control != device, "Control must not be a device");
		InternedString internedString = InputControlLayout.s_Layouts.FindLayoutThatIntroducesControl(control, m_LayoutCache);
		if (m_PathBuilder == null)
		{
			m_PathBuilder = new StringBuilder();
		}
		m_PathBuilder.Length = 0;
		control.BuildPath(internedString, m_PathBuilder);
		return m_PathBuilder.ToString();
	}

	internal static bool TryGetCompositeData(string name, out CompositeData data)
	{
		for (int i = 0; i < m_Composites.Count; i++)
		{
			if (m_Composites[i].m_TypeName == name)
			{
				data = m_Composites[i];
				return true;
			}
		}
		data = default(CompositeData);
		return false;
	}

	internal static bool TryGetCompositeData(ActionType actionType, out CompositeData data)
	{
		return TryGetCompositeData(actionType.GetCompositeTypeName(), out data);
	}

	public static string GetBindingName(ActionComponent component)
	{
		if (TryGetCompositeData(component.GetActionType(), out var data) && data.TryGetData(component, out var data2))
		{
			return data2.m_BindingName;
		}
		return CompositeComponentData.defaultData.m_BindingName;
	}

	public static string GetModifierName(ActionComponent component)
	{
		if (TryGetCompositeData(component.GetActionType(), out var data) && data.TryGetData(component, out var data2))
		{
			return data2.m_ModifierName;
		}
		return CompositeComponentData.defaultData.m_ModifierName;
	}

	public static void CreateInstance()
	{
		s_Instance = new InputManager();
		s_Instance.Initialize();
	}

	public static void DestroyInstance()
	{
		s_Instance?.Dispose();
		s_Instance = null;
	}

	public InputManager()
	{
		log.Debug("Creating InputManager");
		OnFocusChanged(Application.isFocused);
		m_ActionAsset = Resources.Load<InputActionAsset>("Input/InputActions");
		m_UIActionCollection = Resources.Load<UIInputActionCollection>("Input/UI Input Actions");
		m_ToolActionCollection = Resources.Load<UIInputActionCollection>("Input/Tool Input Actions");
		InputActionMap[] actionMaps = m_ActionAsset.m_ActionMaps;
		foreach (InputActionMap obj in actionMaps)
		{
			obj.m_Asset = null;
			ProxyActionMap proxyActionMap = new ProxyActionMap(obj);
			m_Maps.Add(proxyActionMap.name, proxyActionMap);
		}
		m_ConflictResolution.EventConflictResolved += delegate
		{
			this.EventConflictResolved?.Invoke();
		};
	}

	public void Dispose()
	{
		log.Debug("Disposing InputManager");
		if (inputUser.valid)
		{
			inputUser.UnpairDevicesAndRemoveUser();
		}
		InputSystem.onDeviceChange -= OnDeviceChange;
		foreach (KeyValuePair<InputDevice, DeviceListener> deviceListener in m_DeviceListeners)
		{
			deviceListener.Deconstruct(out var _, out var value);
			value.StopListening();
		}
		PlatformManager.instance.onDeviceAssociationChanged -= OnDeviceAssociationChanged;
		PlatformManager.instance.onOverlayStateChanged -= OnOverlayStateChanged;
	}

	public void Update()
	{
		if (!m_OverlayActive)
		{
			foreach (KeyValuePair<InputDevice, DeviceListener> deviceListener in m_DeviceListeners)
			{
				deviceListener.Deconstruct(out var _, out var value);
				value.Tick();
			}
		}
		if (m_ActiveControlScheme == ControlScheme.KeyboardAndMouse)
		{
			Mouse current = Mouse.current;
			if (current != null && current.delta.value.magnitude > 0.2f)
			{
				m_AccumulatedIdleDelay = 0f;
				if (m_Idle)
				{
					m_Idle = false;
					Telemetry.InputIdleEnd();
				}
			}
		}
		if (!m_Idle)
		{
			if (GameManager.instance.state == GameManager.State.WorldReady)
			{
				if (m_WasWorldReady)
				{
					m_AccumulatedIdleDelay += Time.unscaledDeltaTime;
				}
				m_WasWorldReady = true;
			}
			else
			{
				m_AccumulatedIdleDelay = 0f;
				m_WasWorldReady = false;
			}
			if (m_AccumulatedIdleDelay >= 30f)
			{
				m_AccumulatedIdleDelay = 30f;
				m_Idle = true;
				log.Debug("Input idle");
				Telemetry.InputIdleStart();
			}
		}
		m_ConflictResolution.Update();
		RefreshActiveControl();
	}

	private void UpdateCursorVisibility()
	{
		Cursor.visible = activeControlScheme == ControlScheme.KeyboardAndMouse && !hideCursor;
	}

	internal void CheckConflicts()
	{
		if (GameManager.instance != null && GameManager.instance.state < GameManager.State.UIReady)
		{
			return;
		}
		bindingConflicts = DeviceType.None;
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var key, out var value);
			ProxyActionMap proxyActionMap = value;
			bool flag = false;
			foreach (KeyValuePair<string, ProxyAction> action in proxyActionMap.actions)
			{
				action.Deconstruct(out key, out var value2);
				ProxyAction proxyAction = value2;
				if ((proxyAction.availableDevices & ~bindingConflicts) == 0)
				{
					continue;
				}
				foreach (var (_, proxyComposite2) in proxyAction.composites)
				{
					if ((proxyComposite2.m_Device & ~bindingConflicts) == 0)
					{
						continue;
					}
					foreach (var (_, proxyBinding2) in proxyComposite2.bindings)
					{
						if ((proxyBinding2.hasConflicts & ProxyBinding.ConflictType.WithBuiltIn) != ProxyBinding.ConflictType.None)
						{
							if (proxyBinding2.isBuiltIn)
							{
								bindingConflicts |= proxyBinding2.device;
							}
							else
							{
								flag = true;
							}
						}
					}
				}
				if (bindingConflicts == DeviceType.All && flag)
				{
					break;
				}
			}
			SetModConflictNotification(proxyActionMap, flag);
		}
		SetBuiltInConflictNotification(bindingConflicts != DeviceType.None);
	}

	private void SetBuiltInConflictNotification(bool conflict)
	{
		if (conflict == NotificationSystem.Exist("KeyBindingConflict"))
		{
			return;
		}
		if (conflict)
		{
			ProgressState? progressState = ProgressState.Warning;
			NotificationSystem.Push("KeyBindingConflict", null, null, "KeyBindingConflict", "KeyBindingConflict", null, progressState, null, delegate
			{
				LocalizedString value = LocalizedString.Id("Common.DIALOG_TITLE_INPUT");
				LocalizedString message = LocalizedString.Id("Common.DIALOG_MESSAGE_INPUT");
				LocalizedString confirmAction = LocalizedString.Id("Common.OK");
				LocalizedString localizedString = LocalizedString.Id("Common.DIALOG_ACTION_INPUT[Reset]");
				LocalizedString localizedString2 = LocalizedString.Id("Common.DIALOG_ACTION_INPUT[OpenOptions]");
				MessageDialog dialog = new MessageDialog(value, message, confirmAction, localizedString, localizedString2);
				GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog, Callback);
			});
		}
		else
		{
			ProgressState? progressState = ProgressState.Complete;
			NotificationSystem.Pop("KeyBindingConflict", 2f, null, null, null, "KeyBindingConflictResolved", null, progressState);
		}
		void Callback(int msg)
		{
			switch (msg)
			{
			case 0:
				NotificationSystem.Pop("KeyBindingConflict");
				break;
			case 2:
				ResetAllBindings();
				break;
			case 3:
			{
				OptionsUISystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<OptionsUISystem>();
				string sectionID = bindingConflicts switch
				{
					DeviceType.Keyboard => "Keyboard", 
					DeviceType.Mouse => "Mouse", 
					DeviceType.Gamepad => "Gamepad", 
					DeviceType.Keyboard | DeviceType.Mouse => "Keyboard", 
					DeviceType.Keyboard | DeviceType.Gamepad => (activeControlScheme == ControlScheme.Gamepad) ? "Gamepad" : "Keyboard", 
					DeviceType.Mouse | DeviceType.Gamepad => (activeControlScheme == ControlScheme.Gamepad) ? "Gamepad" : "Mouse", 
					DeviceType.All => (activeControlScheme == ControlScheme.Gamepad) ? "Gamepad" : "Keyboard", 
					_ => null, 
				};
				orCreateSystemManaged?.OpenPage("Input", sectionID, isAdvanced: false);
				break;
			}
			case 1:
				break;
			}
		}
	}

	private void SetModConflictNotification(ProxyActionMap map, bool conflict)
	{
		if (conflict == NotificationSystem.Exist(map.name))
		{
			return;
		}
		if (conflict)
		{
			string text = null;
			Action action = null;
			LocalizedString value = LocalizedString.IdWithFallback("Options.INPUT_MAP[" + map.name + "]", map.name);
			if (ModSetting.instances.TryGetValue(map.name, out var value2) && GameManager.instance.modManager.TryGetExecutableAsset(value2.mod, out var asset))
			{
				value = LocalizedString.Value(asset.mod.displayName);
				if (!string.IsNullOrEmpty(asset.mod.thumbnailPath))
				{
					text = $"{asset.mod.thumbnailPath}?width={NotificationUISystem.width})";
				}
				action = delegate
				{
					World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<OptionsUISystem>()?.OpenPage(map.name, null, isAdvanced: false);
				};
			}
			string name = map.name;
			LocalizedString? title = value;
			string thumbnail = text;
			ProgressState? progressState = ProgressState.Warning;
			Action onClicked = action;
			NotificationSystem.Push(name, title, null, null, "KeyBindingConflict", thumbnail, progressState, null, onClicked);
		}
		else
		{
			string name2 = map.name;
			ProgressState? progressState = ProgressState.Complete;
			NotificationSystem.Pop(name2, 2f, null, null, null, "KeyBindingConflictResolved", null, progressState);
		}
	}

	public void OnFocusChanged(bool hasFocus)
	{
		log.VerboseFormat("Has focus {0}", hasFocus);
		m_HasFocus = hasFocus;
	}

	private void OnOverlayStateChanged(IOverlaySupport psi, bool active)
	{
		log.VerboseFormat("Overlay active {0}", active);
		m_OverlayActive = active;
		if (!active)
		{
			return;
		}
		ReadOnlyArray<InputDevice>? readOnlyArray = m_Devices.Get();
		if (!readOnlyArray.HasValue)
		{
			return;
		}
		foreach (Keyboard item in readOnlyArray.OfType<Keyboard>())
		{
			InputSystem.ResetDevice(item);
		}
	}

	bool IInputActionCollection.Contains(InputAction action)
	{
		InputActionMap sourceMap = action?.actionMap;
		if (sourceMap != null)
		{
			return m_Maps.Any((KeyValuePair<string, ProxyActionMap> m) => m.Value.sourceMap == sourceMap);
		}
		return false;
	}

	void IInputActionCollection.Enable()
	{
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var _, out var value);
			value.sourceMap.Enable();
		}
	}

	void IInputActionCollection.Disable()
	{
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var _, out var value);
			value.sourceMap.Disable();
		}
	}

	IEnumerator<InputAction> IEnumerable<InputAction>.GetEnumerator()
	{
		return m_Maps.SelectMany((KeyValuePair<string, ProxyActionMap> map) => map.Value.sourceMap.actions).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<InputAction>)this).GetEnumerator();
	}

	internal void OnEnabledActionsChanged()
	{
		this.EventEnabledActionsChanged?.Invoke();
	}

	internal void OnActionMasksChanged()
	{
		this.EventActionMasksChanged?.Invoke();
	}

	internal void OnActionDisplayNamesChanged()
	{
		this.EventActionDisplayNamesChanged?.Invoke();
	}

	internal void OnPreResolvedActionChanged()
	{
		this.EventPreResolvedActionChanged?.Invoke();
	}

	internal static DeferManagerUpdatingWrapper DeferUpdating()
	{
		sDeferUpdatingWrapper.Acquire();
		return sDeferUpdatingWrapper;
	}

	public void SetDefaultControlScheme()
	{
		activeControlScheme = ControlScheme.KeyboardAndMouse;
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange change)
	{
		switch (change)
		{
		case InputDeviceChange.Added:
			OnAddDevice(device);
			break;
		case InputDeviceChange.Removed:
			OnRemoveDevice(device);
			break;
		}
	}

	private void OnAddDevice(InputDevice device)
	{
		if (!m_DeviceListeners.TryGetValue(device, out var value))
		{
			value = new DeviceListener(device, 50f);
			value.EventDeviceActivated.AddListener(OnDeviceActivated);
			m_DeviceListeners.Add(device, value);
			value.StartListening();
		}
		value.StartListening();
		TryPairDevice(device);
	}

	private void OnRemoveDevice(InputDevice device)
	{
		if (m_DeviceListeners.TryGetValue(device, out var value))
		{
			value.StopListening();
		}
		if (TryUnpairDevice(device) && ((activeControlScheme == ControlScheme.KeyboardAndMouse && (device is Keyboard || device is Mouse)) || (activeControlScheme == ControlScheme.Gamepad && device is Gamepad)))
		{
			this.EventActiveDeviceDisconnected?.Invoke();
		}
	}

	private void OnDeviceActivated(InputDevice newDevice)
	{
		if (newDevice != m_LastActiveDevice)
		{
			InputDevice lastActiveDevice = m_LastActiveDevice;
			ControlScheme controlScheme = activeControlScheme;
			m_LastActiveDevice = newDevice;
			if (!(newDevice is Mouse) && !(newDevice is Keyboard))
			{
				if (newDevice is Gamepad)
				{
					activeControlScheme = ControlScheme.Gamepad;
				}
			}
			else
			{
				activeControlScheme = ControlScheme.KeyboardAndMouse;
			}
			this.EventActiveDeviceChanged?.Invoke(newDevice, lastActiveDevice, activeControlScheme != controlScheme);
		}
		if (m_Idle)
		{
			m_Idle = false;
			Telemetry.InputIdleEnd();
		}
		m_AccumulatedIdleDelay = 0f;
		if (!Enumerable.Contains(inputUser.pairedDevices, newDevice))
		{
			OnUnpairedDeviceUsed(newDevice);
		}
	}

	private void OnUnpairedDeviceUsed(InputDevice device)
	{
		if (!(device is Mouse))
		{
			if (!(device is Keyboard))
			{
				if (device is Gamepad)
				{
					if (!PlatformManager.instance.IsDeviceAssociated(device))
					{
						return;
					}
					UnpairAll<Gamepad>();
				}
			}
			else
			{
				UnpairAll<Keyboard>();
			}
		}
		else
		{
			UnpairAll<Mouse>();
		}
		PairDevice(device);
	}

	private void OnDeviceAssociationChanged(IPlatformServiceIntegration psi, DeviceAssociationChange change)
	{
		if (!PlatformManager.instance.IsPrincipalDeviceAssociationIntegration(psi))
		{
			return;
		}
		InputDevice inputDevice = InputSystem.devices.FirstOrDefault((InputDevice device) => device.deviceId == change.deviceId) ?? InputSystem.disconnectedDevices.FirstOrDefault((InputDevice device) => device.deviceId == change.deviceId);
		if (inputDevice != null)
		{
			if (!change.associated)
			{
				if (TryUnpairDevice(inputDevice))
				{
					this.EventActiveDeviceAssociationLost?.Invoke();
				}
			}
			else if (change.associated)
			{
				TryPairDevice(inputDevice);
			}
		}
		else
		{
			log.Error($"No matching device found with ID: {change.deviceId}.");
		}
	}

	public void AddInitialDevices()
	{
		foreach (InputDevice device in InputSystem.devices)
		{
			OnAddDevice(device);
		}
	}

	private bool TryPairDevice(InputDevice device)
	{
		if ((device is Mouse && !IsDeviceTypePaired<Mouse>()) || (device is Keyboard && !IsDeviceTypePaired<Keyboard>()) || (device is Gamepad && !IsDeviceTypePaired<Gamepad>() && PlatformManager.instance.IsDeviceAssociated(device)))
		{
			PairDevice(device);
			return true;
		}
		return false;
	}

	private void PairDevice(InputDevice device)
	{
		log.InfoFormat("Pair {0} [{1}]", device.displayName, device.deviceId);
		InputUser.PerformPairingWithDevice(device, inputUser);
		this.EventDevicePaired?.Invoke();
		UpdateConnectedDeviceTypes();
	}

	private bool TryUnpairDevice(InputDevice device)
	{
		if (Enumerable.Contains(inputUser.pairedDevices, device) || Enumerable.Contains(inputUser.lostDevices, device))
		{
			UnpairDevice(device);
			return true;
		}
		return false;
	}

	private void UnpairDevice(InputDevice device)
	{
		log.InfoFormat("Unpair {0} [{1}]", device.displayName, device.deviceId);
		inputUser.UnpairDevice(device);
		UpdateConnectedDeviceTypes();
	}

	private void UnpairAll<T>() where T : InputDevice
	{
		foreach (InputDevice item in inputUser.pairedDevices.Where((InputDevice x) => x is T))
		{
			UnpairDevice(item);
		}
		foreach (InputDevice item2 in inputUser.lostDevices.Where((InputDevice x) => x is T))
		{
			UnpairDevice(item2);
		}
	}

	private bool IsDeviceTypePaired<T>() where T : InputDevice
	{
		if (inputUser.pairedDevices.Any((InputDevice d) => d is T))
		{
			return true;
		}
		if (inputUser.lostDevices.Any((InputDevice d) => d is T))
		{
			return true;
		}
		return false;
	}

	public static bool IsGamepadActive()
	{
		return instance.activeControlScheme == ControlScheme.Gamepad;
	}

	private void UpdateConnectedDeviceTypes()
	{
		m_ConnectedDeviceTypes = inputUser.pairedDevices.Aggregate(DeviceType.None, delegate(DeviceType result, InputDevice device)
		{
			DeviceType deviceType = ((device is Keyboard) ? DeviceType.Keyboard : ((device is Mouse) ? DeviceType.Mouse : ((device is Gamepad) ? DeviceType.Gamepad : DeviceType.None)));
			return result | deviceType;
		});
	}

	public GamepadType GetActiveGamepadType()
	{
		return GetGamepadType(Gamepad.current);
	}

	public GamepadType GetGamepadType(Gamepad gamepad)
	{
		if (!(gamepad is DualShockGamepad))
		{
			if (gamepad is XInputController)
			{
				return GamepadType.Xbox;
			}
			return GamepadType.Xbox;
		}
		return GamepadType.PS;
	}

	public void Initialize()
	{
		m_DeviceListeners = new Dictionary<InputDevice, DeviceListener>();
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat("Input initialized in {0}ms", t.TotalMilliseconds);
		}))
		{
			using (DeferUpdating())
			{
				InitializeComposites();
				InitializeModifiers();
				foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
				{
					map.Deconstruct(out var _, out var value);
					value.InitActions();
				}
				InitializeMasks();
				InitializeAliases();
				InitializeLinkedActions();
			}
		}
		inputUser = InputUser.CreateUserWithoutPairedDevices();
		AssociateActionsWithUser(associate: true);
		InputSystem.onDeviceChange += OnDeviceChange;
		AddInitialDevices();
		PlatformManager.instance.onDeviceAssociationChanged += OnDeviceAssociationChanged;
		PlatformManager.instance.onOverlayStateChanged += OnOverlayStateChanged;
		m_ConflictResolution.Initialize();
	}

	private void InitializeComposites()
	{
		m_Composites = new List<CompositeData>
		{
			AxisSeparatedWithModifiersComposite.GetCompositeData(),
			AxisWithModifiersComposite.GetCompositeData(),
			ButtonWithModifiersComposite.GetCompositeData(),
			CameraVector2WithModifiersComposite.GetCompositeData(),
			Vector2SeparatedWithModifiersComposite.GetCompositeData(),
			Vector2WithModifiersComposite.GetCompositeData()
		};
	}

	private void InitializeModifiers()
	{
		foreach (InputAction action in m_Maps.Values.SelectMany((ProxyActionMap map) => map.sourceMap.actions))
		{
			action.ForEachCompositeOfAction(delegate(InputActionSetupExtensions.BindingSyntax iterator)
			{
				CompositeInstance compositeInstance = new CompositeInstance(NameAndParameters.Parse(iterator.binding.effectivePath));
				InitializeModifiers(iterator, action, compositeInstance);
				return true;
			});
		}
	}

	private void InitializeModifiers(InputActionSetupExtensions.BindingSyntax compositeIterator, InputAction action, CompositeInstance compositeInstance)
	{
		if (compositeInstance.modifierOptions != ModifierOptions.Allow)
		{
			return;
		}
		foreach (CompositeComponentData componentData in compositeInstance.compositeData.m_Data.Values)
		{
			action.ForEachPartOfCompositeWithName(compositeIterator, componentData.m_BindingName, delegate(InputActionSetupExtensions.BindingSyntax mainIterator)
			{
				InputBinding binding = mainIterator.binding;
				if (!kModifiers.TryGetValue(compositeIterator.binding.name.ToDeviceType(), out var value))
				{
					return true;
				}
				HashSet<string> missedModifiers = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
				action.ForEachPartOfCompositeWithName(mainIterator, componentData.m_ModifierName, delegate(InputActionSetupExtensions.BindingSyntax modifierIterator)
				{
					InputBinding binding2 = modifierIterator.binding;
					if (!string.Equals(binding2.name, componentData.m_ModifierName, StringComparison.Ordinal))
					{
						return true;
					}
					if (string.IsNullOrEmpty(binding2.path))
					{
						return true;
					}
					missedModifiers.Remove(binding2.path);
					return true;
				}, out var endIterator2);
				foreach (string item in missedModifiers)
				{
					endIterator2 = endIterator2.InsertPartBinding(componentData.m_ModifierName, item).WithGroups(binding.groups).WithProcessor(prohibitionModifierProcessor)
						.Triggering(action);
				}
				return true;
			}, out var _);
		}
	}

	private void InitializeMasks()
	{
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var key, out var value);
			foreach (KeyValuePair<string, ProxyAction> action2 in value.actions)
			{
				action2.Deconstruct(out key, out var value2);
				ProxyAction action = value2;
				InitializeMasks(action);
			}
		}
	}

	internal void InitializeMasks(ProxyAction action)
	{
		InputAction sourceAction = action.sourceAction;
		string expectedControlType = sourceAction.expectedControlType;
		Type typeFromHandle;
		switch (expectedControlType)
		{
		default:
			if (expectedControlType.Length == 0)
			{
				goto case "Button";
			}
			goto case null;
		case "Dpad":
			typeFromHandle = typeof(MaskVector2Processor);
			break;
		case "Stick":
			typeFromHandle = typeof(MaskVector2Processor);
			break;
		case "Vector2":
			typeFromHandle = typeof(MaskVector2Processor);
			break;
		case "Axis":
			typeFromHandle = typeof(MaskFloatProcessor);
			break;
		case "Button":
			typeFromHandle = typeof(MaskFloatProcessor);
			break;
		case null:
			throw new ArgumentException("Unexpected type of control", "expectedControlType");
		}
		InternedString processorName = InputProcessor.s_Processors.FindNameForType(typeFromHandle);
		sourceAction.ForEachCompositeOfAction(delegate(InputActionSetupExtensions.BindingSyntax iterator)
		{
			NameAndParameters nameAndParameters = new NameAndParameters
			{
				name = processorName,
				parameters = new ReadOnlyArray<NamedValue>(new NamedValue[2]
				{
					NamedValue.From("m_Index", action.m_GlobalIndex),
					NamedValue.From("m_Mask", iterator.binding.name.ToDeviceType())
				})
			};
			sourceAction.m_ActionMap.m_Bindings[iterator.m_BindingIndexInMap].processors = (string.IsNullOrEmpty(iterator.binding.processors) ? nameAndParameters.ToString() : string.Format("{0}{1}{2}", iterator.binding.processors, ",", nameAndParameters));
			sourceAction.m_ActionMap.OnBindingModified();
			return true;
		});
	}

	private void InitializeAliases()
	{
		UIBaseInputAction[] inputActions = uiActionCollection.m_InputActions;
		foreach (UIBaseInputAction uIBaseInputAction in inputActions)
		{
			foreach (UIInputActionPart actionPart in uIBaseInputAction.actionParts)
			{
				if (actionPart.TryGetProxyAction(out var action))
				{
					action.m_UIAliases.Add(uIBaseInputAction);
				}
			}
		}
		inputActions = toolActionCollection.m_InputActions;
		foreach (UIBaseInputAction uIBaseInputAction2 in inputActions)
		{
			foreach (UIInputActionPart actionPart2 in uIBaseInputAction2.actionParts)
			{
				if (actionPart2.TryGetProxyAction(out var action2))
				{
					action2.m_UIAliases.Add(uIBaseInputAction2);
				}
			}
		}
	}

	private void InitializeLinkedActions()
	{
		foreach (KeyValuePair<string, ProxyActionMap> map in m_Maps)
		{
			map.Deconstruct(out var key, out var value);
			foreach (KeyValuePair<string, ProxyAction> action2 in value.actions)
			{
				action2.Deconstruct(out key, out var value2);
				ProxyAction action = value2;
				action.sourceAction.ForEachCompositeOfAction(delegate(InputActionSetupExtensions.BindingSyntax iterator)
				{
					DeviceType deviceType = iterator.binding.name.ToDeviceType();
					if (deviceType == DeviceType.None)
					{
						return true;
					}
					CompositeInstance compositeInstance = new CompositeInstance(NameAndParameters.Parse(iterator.binding.effectivePath));
					if (compositeInstance.linkedGuid != Guid.Empty && TryFindAction(compositeInstance.linkedGuid, out var proxyAction))
					{
						ProxyAction.LinkActions(new ProxyAction.LinkInfo
						{
							m_Action = action,
							m_Device = deviceType
						}, new ProxyAction.LinkInfo
						{
							m_Action = proxyAction,
							m_Device = deviceType
						});
					}
					return true;
				});
			}
		}
	}

	internal void CreateCompositeBinding(InputAction action, ProxyComposite.Info info)
	{
		string composite = $"{info.m_Source.parameters}{';'}{info.m_Source.usages.parameters}";
		string interactions = string.Join(";", info.m_Source.interactions);
		string processors = string.Join(";", info.m_Source.processors);
		InputActionSetupExtensions.CompositeSyntax compositeSyntax = action.AddCompositeBinding(composite, interactions, processors);
		new InputActionSetupExtensions.BindingSyntax(action.m_ActionMap, action.BindingIndexOnActionToBindingIndexOnMap(compositeSyntax.bindingIndex), action).WithName(info.m_Device.ToString());
		foreach (ProxyBinding binding in info.m_Bindings)
		{
			if (!info.m_Source.compositeData.TryGetData(binding.component, out var data))
			{
				continue;
			}
			compositeSyntax.With(data.m_BindingName, binding.path, binding.device.ToString());
			if (info.m_Source.modifierOptions != ModifierOptions.Allow || !kModifiers.TryGetValue(binding.device, out var value))
			{
				continue;
			}
			foreach (string supportedModifier in value)
			{
				string processors2 = (binding.modifiers.Any((ProxyModifier m) => m.m_Path == supportedModifier) ? string.Empty : prohibitionModifierProcessor);
				compositeSyntax.With(data.m_ModifierName, supportedModifier, binding.device.ToString(), processors2);
			}
		}
	}

	public InputBarrier CreateGlobalBarrier(string barrierName)
	{
		return new InputBarrier(barrierName, m_Maps.Values.ToArray());
	}

	public InputBarrier CreateOverlayBarrier(string barrierName)
	{
		ProxyActionMap[] maps = m_Maps.Values.Where((ProxyActionMap actionMap) => actionMap.name != "Engagement" && actionMap.name != "Splash screen").ToArray();
		return new InputBarrier(barrierName, maps, DeviceType.All, blocked: true);
	}

	public InputBarrier CreateMapBarrier(string map, string barrierName)
	{
		return new InputBarrier(barrierName, FindActionMap(map));
	}

	public InputBarrier CreateActionBarrier(string map, string name, string barrierName)
	{
		return new InputBarrier(barrierName, FindAction(map, name));
	}

	public ProxyActionMap FindActionMap(string name)
	{
		if (!m_Maps.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public bool TryFindActionMap(string name, out ProxyActionMap map)
	{
		return m_Maps.TryGetValue(name, out map);
	}

	internal ProxyActionMap FindActionMap(InputActionMap map)
	{
		return FindActionMap(map?.name);
	}

	internal bool TryFindActionMap(InputActionMap map, out ProxyActionMap proxyMap)
	{
		return TryFindActionMap(map.name, out proxyMap);
	}

	private ProxyActionMap AddActionMap(string name)
	{
		using (DeferUpdating())
		{
			InputActionMap inputActionMap = new InputActionMap(name);
			inputActionMap.GenerateId();
			ProxyActionMap proxyActionMap = new ProxyActionMap(inputActionMap);
			m_Maps.Add(proxyActionMap.name, proxyActionMap);
			return proxyActionMap;
		}
	}

	private ProxyActionMap GetOrCreateMap(string name)
	{
		if (!TryFindActionMap(name, out var map))
		{
			return AddActionMap(name);
		}
		return map;
	}

	public void AssociateActionsWithUser(bool associate)
	{
		if (inputUser.valid)
		{
			if (associate)
			{
				inputUser.AssociateActionsWithUser(this);
			}
			else
			{
				inputUser.AssociateActionsWithUser(null);
			}
		}
	}
}
