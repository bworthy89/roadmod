using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public class ProxyAction : IProxyAction
{
	public struct Info
	{
		public string m_Name;

		public string m_Map;

		public ActionType m_Type;

		public List<ProxyComposite.Info> m_Composites;
	}

	internal struct LinkInfo : IEquatable<LinkInfo>
	{
		public ProxyAction m_Action;

		public InputManager.DeviceType m_Device;

		public bool Equals(LinkInfo other)
		{
			if (object.Equals(m_Action, other.m_Action))
			{
				return m_Device == other.m_Device;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LinkInfo other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_Action, (int)m_Device);
		}
	}

	internal class DeferActionUpdatingWrapper : IDisposable
	{
		private static int sDeferUpdating;

		private static readonly HashSet<ProxyAction> sUpdateQueue = new HashSet<ProxyAction>();

		public bool isDeferred => sDeferUpdating != 0;

		public void AddToUpdateQueue(ProxyAction action)
		{
			sUpdateQueue.Add(action);
		}

		public void Acquire()
		{
			sDeferUpdating++;
		}

		public void Dispose()
		{
			if (sDeferUpdating > 0)
			{
				sDeferUpdating--;
			}
			if (sDeferUpdating != 0)
			{
				return;
			}
			try
			{
				sDeferUpdating++;
				while (sUpdateQueue.Count != 0)
				{
					ProxyAction[] array = sUpdateQueue.ToArray();
					sUpdateQueue.Clear();
					ProxyAction[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						array2[i].Update(ignoreDefer: true);
					}
				}
			}
			finally
			{
				sDeferUpdating--;
			}
		}
	}

	internal class DeferActionStateUpdatingWrapper : IDisposable
	{
		private static int sDeferUpdating;

		private static readonly HashSet<ProxyAction> sUpdateQueue = new HashSet<ProxyAction>();

		public bool isDeferred => sDeferUpdating != 0;

		public void AddToUpdateQueue(ProxyAction action)
		{
			sUpdateQueue.Add(action);
		}

		public void Acquire()
		{
			sDeferUpdating++;
		}

		public void Dispose()
		{
			if (sDeferUpdating > 0)
			{
				sDeferUpdating--;
			}
			if (sDeferUpdating != 0)
			{
				return;
			}
			try
			{
				sDeferUpdating++;
				while (sUpdateQueue.Count != 0)
				{
					ProxyAction[] array = sUpdateQueue.ToArray();
					sUpdateQueue.Clear();
					ProxyAction[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						array2[i].UpdateState(ignoreDefer: true);
					}
				}
			}
			finally
			{
				sDeferUpdating--;
			}
		}
	}

	private static int counter;

	internal readonly int m_GlobalIndex;

	private readonly InputAction m_SourceAction;

	private readonly ProxyActionMap m_Map;

	internal readonly HashSet<InputBarrier> m_Barriers = new HashSet<InputBarrier>();

	internal readonly HashSet<InputActivator> m_Activators = new HashSet<InputActivator>();

	internal readonly HashSet<DisplayNameOverride> m_DisplayOverrides = new HashSet<DisplayNameOverride>();

	internal readonly HashSet<UIBaseInputAction> m_UIAliases = new HashSet<UIBaseInputAction>();

	internal readonly HashSet<LinkInfo> m_LinkedActions = new HashSet<LinkInfo>();

	private InputActivator m_DefaultActivator;

	private InputActivator m_DefaultBuiltInActivator;

	internal bool m_PreResolvedEnable;

	private InputManager.DeviceType m_AvailableMask;

	private InputManager.DeviceType m_PreResolvedMask;

	private InputManager.DeviceType m_Mask;

	private bool m_IsSystemAction;

	private readonly Dictionary<InputManager.DeviceType, ProxyComposite> m_Composites = new Dictionary<InputManager.DeviceType, ProxyComposite>();

	private readonly List<ProxyBinding> m_Bindings = new List<ProxyBinding>();

	private Action<ProxyAction, InputActionPhase> m_OnInteraction;

	internal static readonly DeferActionUpdatingWrapper sDeferUpdatingWrapper = new DeferActionUpdatingWrapper();

	internal static readonly DeferActionStateUpdatingWrapper sDeferStateUpdatingWrapper = new DeferActionStateUpdatingWrapper();

	public ProxyActionMap map => m_Map;

	internal InputAction sourceAction => m_SourceAction;

	public IReadOnlyDictionary<InputManager.DeviceType, ProxyComposite> composites => m_Composites;

	public int compositesCount => m_Composites.Count;

	internal IReadOnlyCollection<InputBarrier> barriers => m_Barriers;

	internal IReadOnlyCollection<InputActivator> activators => m_Activators;

	public IEnumerable<ProxyBinding> bindings
	{
		get
		{
			foreach (var (_, proxyComposite2) in m_Composites)
			{
				foreach (KeyValuePair<ActionComponent, ProxyBinding> binding in proxyComposite2.bindings)
				{
					binding.Deconstruct(out var _, out var value);
					yield return value;
				}
			}
		}
	}

	public bool isSet => m_Composites.Any((KeyValuePair<InputManager.DeviceType, ProxyComposite> c) => c.Value.isSet);

	public bool isBuiltIn => m_Composites.Any((KeyValuePair<InputManager.DeviceType, ProxyComposite> c) => c.Value.isBuiltIn);

	internal bool isDummy
	{
		get
		{
			if (m_Composites.Count != 0)
			{
				return m_Composites.Any((KeyValuePair<InputManager.DeviceType, ProxyComposite> c) => c.Value.isDummy);
			}
			return true;
		}
	}

	internal bool isSystemAction
	{
		get
		{
			if (isBuiltIn)
			{
				if (!m_IsSystemAction)
				{
					return m_UIAliases.Count == 0;
				}
				return true;
			}
			return false;
		}
	}

	public InputManager.DeviceType availableDevices => m_AvailableMask;

	public bool isKeyboardAction => (m_AvailableMask & InputManager.DeviceType.Keyboard) != 0;

	public bool isMouseAction => (m_AvailableMask & InputManager.DeviceType.Mouse) != 0;

	public bool isGamepadAction => (m_AvailableMask & InputManager.DeviceType.Gamepad) != 0;

	public bool isOnlyKeyboardAction => m_AvailableMask == InputManager.DeviceType.Keyboard;

	public bool isOnlyMouseAction => m_AvailableMask == InputManager.DeviceType.Mouse;

	public bool isOnlyGamepadAction => m_AvailableMask == InputManager.DeviceType.Gamepad;

	public bool isMultiDeviceAction => compositesCount > 1;

	public string name => m_SourceAction.name;

	public string mapName => m_Map.name;

	public string title => mapName + "/" + name;

	public Type valueType
	{
		get
		{
			string expectedControlType = m_SourceAction.expectedControlType;
			switch (expectedControlType)
			{
			default:
				if (expectedControlType.Length != 0)
				{
					break;
				}
				goto case "Button";
			case "Dpad":
				return typeof(Vector2);
			case "Stick":
				return typeof(Vector2);
			case "Vector2":
				return typeof(Vector2);
			case "Axis":
				return typeof(float);
			case "Button":
				return typeof(float);
			case null:
				break;
			}
			return typeof(float);
		}
	}

	public bool enabled
	{
		get
		{
			return m_SourceAction.enabled;
		}
		internal set
		{
			if (m_DefaultBuiltInActivator != null)
			{
				m_DefaultBuiltInActivator.enabled = value;
			}
			else if (value)
			{
				m_DefaultBuiltInActivator = new InputActivator(ignoreIsBuiltIn: true, "Default built-in (" + name + ")", this, InputManager.DeviceType.All, enabled: true);
			}
		}
	}

	public bool shouldBeEnabled
	{
		get
		{
			if (m_DefaultActivator != null)
			{
				return m_DefaultActivator.enabled;
			}
			return false;
		}
		set
		{
			if (isBuiltIn)
			{
				throw new Exception("Built-in actions can not be enabled directly");
			}
			if (m_DefaultActivator != null)
			{
				m_DefaultActivator.enabled = value;
			}
			else if (value)
			{
				m_DefaultActivator = new InputActivator(ignoreIsBuiltIn: false, "Default (" + name + ")", this, InputManager.DeviceType.All, enabled: true);
			}
		}
	}

	internal bool preResolvedEnable => m_PreResolvedEnable;

	public InputManager.DeviceType mask => m_Mask;

	internal InputManager.DeviceType preResolvedMask => m_PreResolvedMask;

	public DisplayNameOverride displayOverride { get; private set; }

	public IEnumerable<string> usedKeys => (from b in bindings
		where b.isSet
		select b.path).Distinct();

	public event Action<ProxyAction> onChanged;

	public event Action<ProxyAction, InputActionPhase> onInteraction
	{
		add
		{
			if (m_OnInteraction == null)
			{
				m_OnInteraction = (Action<ProxyAction, InputActionPhase>)Delegate.Combine(m_OnInteraction, value);
				m_SourceAction.started += SourceOnStarted;
				m_SourceAction.performed += SourceOnPerformed;
				m_SourceAction.canceled += SourceOnCanceled;
			}
			else
			{
				m_OnInteraction = (Action<ProxyAction, InputActionPhase>)Delegate.Combine(m_OnInteraction, value);
			}
		}
		remove
		{
			m_OnInteraction = (Action<ProxyAction, InputActionPhase>)Delegate.Remove(m_OnInteraction, value);
			if (m_OnInteraction == null)
			{
				m_SourceAction.started -= SourceOnStarted;
				m_SourceAction.performed -= SourceOnPerformed;
				m_SourceAction.canceled -= SourceOnCanceled;
			}
		}
	}

	internal ProxyAction(ProxyActionMap map, InputAction sourceAction)
	{
		m_GlobalIndex = counter++;
		m_Map = map ?? throw new ArgumentNullException("map");
		m_SourceAction = sourceAction ?? throw new ArgumentNullException("sourceAction");
		InputManager.instance.actionIndex[m_GlobalIndex] = this;
		Update();
	}

	public unsafe T ReadValue<T>() where T : struct
	{
		InputActionState state = m_SourceAction.GetOrCreateActionMap().m_State;
		if (state == null)
		{
			return default(T);
		}
		InputActionState.TriggerState* ptr = state.actionStates + m_SourceAction.m_ActionIndexInState;
		return state.ReadValue<T>(ptr->bindingIndex, ptr->controlIndex);
	}

	public object ReadValueAsObject()
	{
		return m_SourceAction.ReadValueAsObject();
	}

	internal unsafe T ReadRawValue<T>(bool disableAll = true) where T : struct
	{
		InputActionState state = m_SourceAction.GetOrCreateActionMap().m_State;
		if (state == null)
		{
			return default(T);
		}
		InputActionState.TriggerState* ptr = state.actionStates + m_SourceAction.m_ActionIndexInState;
		int bindingIndex = ptr->bindingIndex;
		ref InputActionState.BindingState reference = ref state.bindingStates[bindingIndex];
		if (reference.isPartOfComposite)
		{
			reference = ref state.bindingStates[reference.compositeOrCompositeBindingIndex];
		}
		int processorCount = reference.processorCount;
		try
		{
			if (disableAll)
			{
				reference.processorCount = 0;
			}
			else
			{
				for (int i = 0; i < processorCount; i++)
				{
					if (state.processors[reference.processorStartIndex + i] is IDisableableProcessor disableableProcessor)
					{
						disableableProcessor.disabled = true;
					}
				}
			}
			return state.ReadValue<T>(ptr->bindingIndex, ptr->controlIndex);
		}
		finally
		{
			if (disableAll)
			{
				reference.processorCount = processorCount;
			}
			else
			{
				for (int j = 0; j < processorCount; j++)
				{
					if (state.processors[reference.processorStartIndex + j] is IDisableableProcessor disableableProcessor2)
					{
						disableableProcessor2.disabled = false;
					}
				}
			}
		}
	}

	public unsafe float GetMagnitude()
	{
		InputActionState state = m_SourceAction.GetOrCreateActionMap().m_State;
		if (state != null)
		{
			InputActionState.TriggerState* ptr = state.actionStates + m_SourceAction.m_ActionIndexInState;
			if (ptr->haveMagnitude)
			{
				return ptr->magnitude;
			}
		}
		return 0f;
	}

	public bool IsPressed()
	{
		return m_SourceAction.IsPressed();
	}

	public bool IsInProgress()
	{
		return m_SourceAction.IsInProgress();
	}

	public bool WasPressedThisFrame()
	{
		return m_SourceAction.WasPressedThisFrame();
	}

	public bool WasReleasedThisFrame()
	{
		return m_SourceAction.WasReleasedThisFrame();
	}

	public bool WasPerformedThisFrame()
	{
		return m_SourceAction.WasPerformedThisFrame();
	}

	private void SourceOnStarted(InputAction.CallbackContext context)
	{
		m_OnInteraction?.Invoke(this, InputActionPhase.Started);
	}

	private void SourceOnPerformed(InputAction.CallbackContext context)
	{
		m_OnInteraction?.Invoke(this, InputActionPhase.Performed);
	}

	private void SourceOnCanceled(InputAction.CallbackContext context)
	{
		m_OnInteraction?.Invoke(this, InputActionPhase.Canceled);
	}

	internal void UpdateState(bool ignoreDefer = false)
	{
		if (m_SourceAction == null)
		{
			return;
		}
		if (sDeferStateUpdatingWrapper.isDeferred && !ignoreDefer)
		{
			sDeferStateUpdatingWrapper.AddToUpdateQueue(this);
			return;
		}
		m_PreResolvedMask = (m_Map.enabled ? (m_AvailableMask & map.mask) : InputManager.DeviceType.None);
		if (m_PreResolvedMask != InputManager.DeviceType.None)
		{
			InputManager.DeviceType deviceType = InputManager.DeviceType.None;
			foreach (InputActivator activator in m_Activators)
			{
				if (activator.enabled)
				{
					deviceType |= activator.mask & m_PreResolvedMask;
					if ((deviceType & m_PreResolvedMask) == m_PreResolvedMask)
					{
						break;
					}
				}
			}
			if (deviceType != InputManager.DeviceType.None)
			{
				foreach (InputBarrier barrier in m_Barriers)
				{
					if (barrier.blocked)
					{
						deviceType &= ~barrier.mask;
						if (deviceType == InputManager.DeviceType.None)
						{
							break;
						}
					}
				}
			}
			m_PreResolvedMask &= deviceType;
		}
		m_PreResolvedEnable = m_Map.enabled && m_PreResolvedMask != InputManager.DeviceType.None;
		if (InputManager.instance != null && (m_PreResolvedEnable != enabled || m_PreResolvedMask != mask))
		{
			InputManager.instance.OnPreResolvedActionChanged();
		}
		if (isSystemAction)
		{
			ApplyState(m_PreResolvedEnable, m_PreResolvedMask);
		}
	}

	internal void ApplyState(bool newEnable, InputManager.DeviceType newMask)
	{
		bool num = newMask != m_Mask;
		bool flag = newEnable != m_SourceAction.enabled;
		if (num)
		{
			m_Mask = newMask;
			InputManager.instance?.OnActionMasksChanged();
		}
		if (flag)
		{
			if (newEnable)
			{
				m_SourceAction.Enable();
			}
			else
			{
				m_SourceAction.Disable();
			}
			UpdateDisplay();
			InputManager.instance?.OnEnabledActionsChanged();
		}
	}

	internal void UpdateDisplay()
	{
		displayOverride = (enabled ? (from n in m_DisplayOverrides
			where n.active
			orderby n.priority
			select n).FirstOrDefault() : null);
		InputManager.instance?.OnActionDisplayNamesChanged();
	}

	internal void Update(bool ignoreDefer = false)
	{
		if (sDeferUpdatingWrapper.isDeferred && !ignoreDefer)
		{
			sDeferUpdatingWrapper.AddToUpdateQueue(this);
			return;
		}
		m_Composites.Clear();
		m_AvailableMask = InputManager.DeviceType.None;
		foreach (ProxyComposite composite in InputManager.instance.GetComposites(sourceAction))
		{
			foreach (LinkInfo linkedAction in m_LinkedActions)
			{
				if (linkedAction.m_Device == composite.m_Device)
				{
					composite.m_LinkedActions.Add(linkedAction.m_Action);
				}
			}
			m_Composites[composite.m_Device] = composite;
			m_AvailableMask |= composite.m_Device;
		}
		m_Bindings.Clear();
		foreach (KeyValuePair<InputManager.DeviceType, ProxyComposite> composite2 in m_Composites)
		{
			composite2.Deconstruct(out var _, out var value);
			foreach (var (_, item) in value.bindings)
			{
				m_Bindings.Add(item);
			}
		}
		m_IsSystemAction = mapName == "Splash screen" || mapName == "Engagement" || mapName == "Camera" || mapName == "Tool" || mapName == "Editor";
		InputManager.instance.UpdateActionInKeyActionMap(this);
		InputManager.instance.OnActionChanged();
		this.onChanged?.Invoke(this);
	}

	public bool TryGetComposite(InputManager.DeviceType device, out ProxyComposite composite)
	{
		return m_Composites.TryGetValue(device, out composite);
	}

	public bool TryGetBinding(ProxyBinding sampleBinding, out ProxyBinding foundBinding)
	{
		if (TryGetComposite(sampleBinding.device, out var composite))
		{
			return composite.TryGetBinding(sampleBinding, out foundBinding);
		}
		foundBinding = default(ProxyBinding);
		return false;
	}

	public InputBarrier CreateBarrier(string barrierName = null, InputManager.DeviceType barrierMask = InputManager.DeviceType.All)
	{
		return new InputBarrier(barrierName, this, barrierMask);
	}

	public InputActivator CreateActivator(string activatorName = null, InputManager.DeviceType activatorMask = InputManager.DeviceType.All)
	{
		return new InputActivator(ignoreIsBuiltIn: false, activatorName, this, activatorMask);
	}

	public bool ContainsComposite(InputManager.DeviceType device)
	{
		return (m_AvailableMask & device) != 0;
	}

	internal static void LinkActions(LinkInfo action1, LinkInfo action2)
	{
		LinkActions(action1, action2, addToOther: true);
	}

	private static void LinkActions(LinkInfo link1, LinkInfo link2, bool addToOther)
	{
		if (link1.m_Action == null || link2.m_Action == null || link1.m_Device != link2.m_Device || link1.m_Action == link2.m_Action)
		{
			return;
		}
		if (addToOther)
		{
			if (!link1.m_Action.m_LinkedActions.Contains(link2))
			{
				foreach (LinkInfo linkedAction in link1.m_Action.m_LinkedActions)
				{
					LinkActions(link2, linkedAction, addToOther: false);
				}
			}
			if (!link2.m_Action.m_LinkedActions.Contains(link1))
			{
				foreach (LinkInfo linkedAction2 in link2.m_Action.m_LinkedActions)
				{
					LinkActions(link1, linkedAction2, addToOther: false);
				}
			}
		}
		link1.m_Action.m_LinkedActions.Add(link2);
		link2.m_Action.m_LinkedActions.Add(link1);
	}

	public override string ToString()
	{
		return mapName + "/" + name + " ( " + string.Join(" | ", from b in bindings
			where !string.IsNullOrEmpty(b.path)
			select string.Format("{0}: {1}{2}", b.component, string.Join("", b.modifiers.Select((ProxyModifier m) => m.m_Path + " + ")), b.path)) + " )";
	}

	internal static DeferActionStateUpdatingWrapper DeferStateUpdating()
	{
		sDeferStateUpdatingWrapper.Acquire();
		return sDeferStateUpdatingWrapper;
	}
}
