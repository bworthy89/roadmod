using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI;

public class InputActionBindings : CompositeBinding, IDisposable
{
	private class ActionState : IComparable<ActionState>, IDisposable
	{
		public enum State
		{
			Enabled,
			Disabled,
			DisabledNoConsumer,
			DisabledNotSet,
			DisabledMaskMismatch,
			DisabledConflict,
			DisabledDuplicate
		}

		private bool m_Disposed;

		private readonly string m_Name;

		private readonly ProxyAction m_Action;

		private readonly UIBaseInputAction.ProcessAs m_ProcessAs;

		private readonly InputActivator m_Activator;

		private readonly DisplayNameOverride m_NameOverride;

		private readonly Game.Input.InputManager.DeviceType m_Mask;

		private int m_Priority;

		private State m_State;

		private readonly UIBaseInputAction.Transform m_Transform;

		public bool isDisposed => m_Disposed;

		public ProxyAction action => m_Action;

		public string name => m_Name;

		public Game.Input.InputManager.DeviceType mask => m_Mask;

		public int priority
		{
			get
			{
				return m_Priority;
			}
			set
			{
				if (!m_Disposed && value != m_Priority)
				{
					m_Priority = value;
					this.onChanged?.Invoke();
				}
			}
		}

		public State state
		{
			get
			{
				return m_State;
			}
			set
			{
				if (!m_Disposed && value != m_State)
				{
					m_State = value;
					this.onChanged?.Invoke();
				}
			}
		}

		public string context { get; set; }

		public UIBaseInputAction.Transform transform => m_Transform;

		public UIBaseInputAction.ProcessAs processAs => m_ProcessAs;

		public event Action onChanged;

		public ActionState(ProxyAction action, string name, DisplayNameOverride displayOverride, UIBaseInputAction.ProcessAs processAs = UIBaseInputAction.ProcessAs.AutoDetect, UIBaseInputAction.Transform transform = UIBaseInputAction.Transform.None, Game.Input.InputManager.DeviceType mask = Game.Input.InputManager.DeviceType.All)
		{
			m_Action = action ?? throw new ArgumentNullException("action");
			m_Name = name ?? throw new ArgumentNullException("name");
			m_Mask = mask;
			m_Activator = new InputActivator(ignoreIsBuiltIn: true, m_Name, action, mask);
			m_Priority = -1;
			m_NameOverride = displayOverride;
			m_ProcessAs = processAs;
			m_Transform = transform;
			UpdateState();
		}

		public void Dispose()
		{
			if (!m_Disposed)
			{
				m_Disposed = true;
				m_Activator?.Dispose();
				m_NameOverride?.Dispose();
				this.onChanged = null;
			}
		}

		public void UpdateState()
		{
			if (!m_Action.isSet)
			{
				state = State.DisabledNotSet;
			}
			else if ((m_Action.availableDevices & Game.Input.InputManager.instance.mask & m_Mask) == 0)
			{
				state = State.DisabledMaskMismatch;
			}
			else if (m_Priority == -1)
			{
				state = State.DisabledNoConsumer;
			}
			else
			{
				state = State.Enabled;
			}
		}

		public int CompareTo(ActionState other)
		{
			return -m_Priority.CompareTo(other.m_Priority);
		}

		public void Apply()
		{
			if (!m_Disposed)
			{
				if (m_Activator != null)
				{
					m_Activator.enabled = state == State.Enabled;
				}
				if (m_NameOverride != null)
				{
					m_NameOverride.active = state == State.Enabled;
				}
			}
		}

		public override string ToString()
		{
			return $"{m_Name} ({m_Action})";
		}
	}

	private interface IEventTrigger : IDisposable
	{
		HashSet<ActionState> states { get; }

		static IEventTrigger GetTrigger(InputActionBindings parent, ProxyAction action, UIBaseInputAction.ProcessAs processAs)
		{
			string expectedControlType = action.sourceAction.expectedControlType;
			switch (expectedControlType)
			{
			default:
				if (expectedControlType.Length != 0)
				{
					break;
				}
				goto case "Button";
			case "Dpad":
			case "Stick":
			case "Vector2":
				return processAs switch
				{
					UIBaseInputAction.ProcessAs.Button => new Vector2ToButtonEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Axis => new Vector2ToAxisEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Vector2 => new Vector2EventTrigger(parent, action), 
					_ => new Vector2EventTrigger(parent, action), 
				};
			case "Axis":
				return processAs switch
				{
					UIBaseInputAction.ProcessAs.Button => new AxisToButtonEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Axis => new AxisEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Vector2 => new AxisToVector2EventTrigger(parent, action), 
					_ => new AxisEventTrigger(parent, action), 
				};
			case "Button":
				return processAs switch
				{
					UIBaseInputAction.ProcessAs.Button => new ButtonEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Axis => new ButtonToAxisEventTrigger(parent, action), 
					UIBaseInputAction.ProcessAs.Vector2 => new ButtonToVector2EventTrigger(parent, action), 
					_ => new ButtonEventTrigger(parent, action), 
				};
			case null:
				break;
			}
			return new DefaultEventTrigger(parent, action);
		}
	}

	private abstract class EventTrigger<TRawValue, TValue> : IEventTrigger, IDisposable where TRawValue : struct where TValue : struct
	{
		private bool m_Disposed;

		private readonly InputActionBindings m_Parent;

		private readonly ProxyAction m_Action;

		private readonly IWriter<TValue> m_ValueWriter;

		public HashSet<ActionState> states { get; } = new HashSet<ActionState>();

		public EventTrigger(InputActionBindings parent, ProxyAction action, IWriter<TValue> valueWriter = null)
		{
			m_Parent = parent;
			m_Action = action ?? throw new ArgumentNullException("action");
			m_ValueWriter = valueWriter ?? ValueWriters.Create<TValue>();
			m_Action.onInteraction += OnInteraction;
		}

		private void OnInteraction(ProxyAction _, InputActionPhase phase)
		{
			if (m_Disposed || phase switch
			{
				InputActionPhase.Performed => 1, 
				InputActionPhase.Canceled => (m_Action.sourceAction.type == InputActionType.PassThrough) ? 1 : 0, 
				_ => 0, 
			} == 0)
			{
				return;
			}
			TRawValue value = m_Action.ReadValue<TRawValue>();
			foreach (ActionState state in states)
			{
				if (state.state != ActionState.State.Enabled)
				{
					continue;
				}
				switch (phase)
				{
				case InputActionPhase.Performed:
				{
					TValue value2 = TransformValue(value, state.transform);
					if (GetMagnitude(value2) != 0f)
					{
						TriggerEvent(m_Parent.m_ActionPerformedBinding, state.name, value2);
					}
					else if (m_Action.sourceAction.type == InputActionType.PassThrough)
					{
						TriggerEvent(m_Parent.m_ActionReleasedBinding, state.name, value2);
					}
					break;
				}
				case InputActionPhase.Canceled:
					if (m_Action.sourceAction.type == InputActionType.PassThrough)
					{
						TriggerEvent(m_Parent.m_ActionReleasedBinding, state.name, default(TValue));
					}
					break;
				}
			}
		}

		private void TriggerEvent(RawEventBinding binding, string action, TValue value)
		{
			IJsonWriter jsonWriter = binding.EventBegin();
			jsonWriter.TypeBegin("input.InputActionEvent");
			jsonWriter.PropertyName("action");
			jsonWriter.Write(action);
			jsonWriter.PropertyName("value");
			m_ValueWriter.Write(jsonWriter, value);
			jsonWriter.TypeEnd();
			binding.EventEnd();
		}

		protected abstract TValue TransformValue(TRawValue value, UIBaseInputAction.Transform transform);

		protected abstract float GetMagnitude(TValue value);

		public void Dispose()
		{
			if (!m_Disposed)
			{
				m_Disposed = true;
				states.Clear();
				m_Action.onInteraction -= OnInteraction;
			}
		}
	}

	private class DefaultEventTrigger : EventTrigger<float, float>
	{
		public DefaultEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return value;
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class ButtonEventTrigger : EventTrigger<float, float>
	{
		public ButtonEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return Mathf.Clamp(value, 0f, 1f);
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class AxisEventTrigger : EventTrigger<float, float>
	{
		public AxisEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return value;
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class Vector2EventTrigger : EventTrigger<Vector2, Vector2>
	{
		public Vector2EventTrigger(InputActionBindings parent, ProxyAction action, IWriter<Vector2> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override Vector2 TransformValue(Vector2 value, UIBaseInputAction.Transform transform)
		{
			return value;
		}

		protected override float GetMagnitude(Vector2 value)
		{
			return value.magnitude;
		}
	}

	private class AxisToButtonEventTrigger : EventTrigger<float, float>
	{
		public AxisToButtonEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Negative => Mathf.Clamp(0f - value, 0f, 1f), 
				UIBaseInputAction.Transform.Positive => Mathf.Clamp(value, 0f, 1f), 
				_ => Mathf.Abs(value), 
			};
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class Vector2ToButtonEventTrigger : EventTrigger<Vector2, float>
	{
		public Vector2ToButtonEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(Vector2 value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Down => Mathf.Clamp(0f - value.y, 0f, 1f), 
				UIBaseInputAction.Transform.Up => Mathf.Clamp(value.y, 0f, 1f), 
				UIBaseInputAction.Transform.Left => Mathf.Clamp(0f - value.x, 0f, 1f), 
				UIBaseInputAction.Transform.Right => Mathf.Clamp(value.x, 0f, 1f), 
				UIBaseInputAction.Transform.Horizontal => Mathf.Abs(value.x), 
				UIBaseInputAction.Transform.Vertical => Mathf.Abs(value.y), 
				_ => value.magnitude, 
			};
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class ButtonToAxisEventTrigger : EventTrigger<float, float>
	{
		public ButtonToAxisEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Negative => 0f - value, 
				UIBaseInputAction.Transform.Positive => value, 
				UIBaseInputAction.Transform.Down => 0f - value, 
				UIBaseInputAction.Transform.Up => value, 
				UIBaseInputAction.Transform.Left => 0f - value, 
				UIBaseInputAction.Transform.Right => value, 
				_ => value, 
			};
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class Vector2ToAxisEventTrigger : EventTrigger<Vector2, float>
	{
		public Vector2ToAxisEventTrigger(InputActionBindings parent, ProxyAction action, IWriter<float> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override float TransformValue(Vector2 value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Horizontal => value.x, 
				UIBaseInputAction.Transform.Vertical => value.y, 
				_ => value.magnitude, 
			};
		}

		protected override float GetMagnitude(float value)
		{
			return Mathf.Abs(value);
		}
	}

	private class ButtonToVector2EventTrigger : EventTrigger<float, Vector2>
	{
		public ButtonToVector2EventTrigger(InputActionBindings parent, ProxyAction action, IWriter<Vector2> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override Vector2 TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Left => new Vector2(0f - value, 0f), 
				UIBaseInputAction.Transform.Right => new Vector2(value, 0f), 
				UIBaseInputAction.Transform.Down => new Vector2(0f, 0f - value), 
				UIBaseInputAction.Transform.Up => new Vector2(0f, value), 
				_ => new Vector2(value, value), 
			};
		}

		protected override float GetMagnitude(Vector2 value)
		{
			return value.magnitude;
		}
	}

	private class AxisToVector2EventTrigger : EventTrigger<float, Vector2>
	{
		public AxisToVector2EventTrigger(InputActionBindings parent, ProxyAction action, IWriter<Vector2> valueWriter = null)
			: base(parent, action, valueWriter)
		{
		}

		protected override Vector2 TransformValue(float value, UIBaseInputAction.Transform transform)
		{
			return transform switch
			{
				UIBaseInputAction.Transform.Horizontal => new Vector2(value, 0f), 
				UIBaseInputAction.Transform.Vertical => new Vector2(0f, value), 
				_ => Vector2.zero, 
			};
		}

		protected override float GetMagnitude(Vector2 value)
		{
			return value.magnitude;
		}
	}

	private const int kDisabledPriority = -1;

	private const string kGroup = "input";

	private RawEventBinding m_ActionPerformedBinding;

	private RawEventBinding m_ActionReleasedBinding;

	private EventBinding m_ActionRefreshedBinding;

	private readonly GetterMapBinding<InputHintBindings.InputHintQuery, string> m_ActionContextBinding;

	private readonly GetterMapBinding<InputHintBindings.InputHintQuery, bool> m_ActiveActionsBinding;

	private readonly List<ActionState> m_UIActionStates = new List<ActionState>();

	private readonly Dictionary<(ProxyAction, UIBaseInputAction.ProcessAs), IEventTrigger> m_Triggers = new Dictionary<(ProxyAction, UIBaseInputAction.ProcessAs), IEventTrigger>();

	private readonly Dictionary<string, int> m_ActionOrder = new Dictionary<string, int>();

	private readonly Dictionary<ProxyAction, List<ActionState>> m_ActionStateMap = new Dictionary<ProxyAction, List<ActionState>>();

	private bool m_ActionsDirty = true;

	private bool m_ConflictsDirty = true;

	private bool m_ActionContextDirty = true;

	private bool m_UpdateInProgress;

	public InputActionBindings()
	{
		AddBinding(m_ActionPerformedBinding = new RawEventBinding("input", "onActionPerformed"));
		AddBinding(m_ActionReleasedBinding = new RawEventBinding("input", "onActionReleased"));
		AddBinding(new TriggerBinding<string, string, int>("input", "setActionPriority", SetActionPriority));
		string[] initialValue = Game.Input.InputManager.instance.uiActionCollection.m_InputActions.Select((UIBaseInputAction a) => a.aliasName).ToArray();
		AddBinding(new ValueBinding<string[]>("input", "actionNames", initialValue, new ArrayWriter<string>()));
		AddBinding(m_ActionRefreshedBinding = new EventBinding("input", "onActionsRefreshed"));
		AddBinding(m_ActionContextBinding = new GetterMapBinding<InputHintBindings.InputHintQuery, string>("input", "actionContext", GetContext, new ValueReader<InputHintBindings.InputHintQuery>(), new ValueWriter<InputHintBindings.InputHintQuery>(), ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_ActiveActionsBinding = new GetterMapBinding<InputHintBindings.InputHintQuery, bool>("input", "activeActions", isActiveAction, new ValueReader<InputHintBindings.InputHintQuery>(), new ValueWriter<InputHintBindings.InputHintQuery>()));
		Game.Input.InputManager.instance.EventActionsChanged += OnActionsChanged;
		Game.Input.InputManager.instance.EventControlSchemeChanged += OnControlSchemeChanged;
		Game.Input.InputManager.instance.EventConflictResolved += OnConflictResolved;
	}

	public void Dispose()
	{
		Game.Input.InputManager.instance.EventActionsChanged -= OnActionsChanged;
		Game.Input.InputManager.instance.EventControlSchemeChanged -= OnControlSchemeChanged;
		Game.Input.InputManager.instance.EventConflictResolved -= OnConflictResolved;
		foreach (ActionState uIActionState in m_UIActionStates)
		{
			uIActionState.Dispose();
		}
		foreach (IEventTrigger value in m_Triggers.Values)
		{
			value.Dispose();
		}
		m_Triggers.Clear();
	}

	private void SetActionPriority(string action, string context, int priority)
	{
		if (!m_ActionOrder.TryGetValue(action, out var i))
		{
			return;
		}
		for (; i < m_UIActionStates.Count && m_UIActionStates[i].name == action; i++)
		{
			m_UIActionStates[i].priority = priority;
			if (m_UIActionStates[i].context != context)
			{
				m_UIActionStates[i].context = context;
				m_ActionContextDirty = true;
			}
		}
	}

	private string GetContext(InputHintBindings.InputHintQuery query)
	{
		if (!m_ActionOrder.TryGetValue(query.action, out var i))
		{
			return null;
		}
		for (; i < m_UIActionStates.Count && m_UIActionStates[i].name == query.action; i++)
		{
			if (!m_UIActionStates[i].action.enabled || (m_UIActionStates[i].action.mask & query.controlScheme.ToDeviceType()) == 0 || !m_ActionStateMap.TryGetValue(m_UIActionStates[i].action, out var value))
			{
				continue;
			}
			foreach (ActionState item in value)
			{
				if (item.state == ActionState.State.Enabled)
				{
					return item.context;
				}
			}
		}
		return null;
	}

	private bool isActiveAction(InputHintBindings.InputHintQuery query)
	{
		if (!m_ActionOrder.TryGetValue(query.action, out var i))
		{
			return false;
		}
		for (; i < m_UIActionStates.Count && m_UIActionStates[i].name == query.action; i++)
		{
			if (m_UIActionStates[i].state == ActionState.State.Enabled && m_UIActionStates[i].action.enabled && (m_UIActionStates[i].action.mask & query.controlScheme.ToDeviceType()) != Game.Input.InputManager.DeviceType.None)
			{
				return true;
			}
		}
		return false;
	}

	private void SetConflictsDirty()
	{
		if (!m_UpdateInProgress)
		{
			m_ConflictsDirty = true;
		}
	}

	private void OnActionsChanged()
	{
		if (!m_UpdateInProgress)
		{
			m_ActionsDirty = true;
			m_ConflictsDirty = true;
		}
	}

	private void OnControlSchemeChanged(Game.Input.InputManager.ControlScheme scheme)
	{
		if (!m_UpdateInProgress)
		{
			m_ConflictsDirty = true;
		}
	}

	private void OnConflictResolved()
	{
		m_ActiveActionsBinding.UpdateAll();
		m_ActionContextBinding.UpdateAll();
		m_ActionContextDirty = false;
	}

	public override bool Update()
	{
		m_UpdateInProgress = true;
		if (m_ActionsDirty)
		{
			RefreshActions();
			m_ActionsDirty = false;
			m_ActionRefreshedBinding.Trigger();
		}
		if (m_ConflictsDirty)
		{
			ResolveConflicts();
			m_ConflictsDirty = false;
		}
		if (m_ActionContextDirty)
		{
			m_ActionContextBinding.UpdateAll();
			m_ActionContextDirty = false;
		}
		m_UpdateInProgress = false;
		return base.Update();
	}

	private void RefreshActions()
	{
		for (int i = 0; i < m_UIActionStates.Count; i++)
		{
			m_UIActionStates[i].Dispose();
			if (m_Triggers.TryGetValue((m_UIActionStates[i].action, m_UIActionStates[i].processAs), out var value))
			{
				value.states.Remove(m_UIActionStates[i]);
				if (value.states.Count == 0)
				{
					value.Dispose();
					m_Triggers.Remove((m_UIActionStates[i].action, m_UIActionStates[i].processAs));
				}
			}
		}
		m_UIActionStates.Clear();
		m_ActionOrder.Clear();
		m_ActionStateMap.Clear();
		for (int j = 0; j < Game.Input.InputManager.instance.uiActionCollection.m_InputActions.Length; j++)
		{
			UIBaseInputAction uIBaseInputAction = Game.Input.InputManager.instance.uiActionCollection.m_InputActions[j];
			int count = m_UIActionStates.Count;
			for (int k = 0; k < uIBaseInputAction.actionParts.Count; k++)
			{
				UIInputActionPart uIInputActionPart = uIBaseInputAction.actionParts[k];
				ProxyAction proxyAction = uIInputActionPart.GetProxyAction();
				if (proxyAction.isSet)
				{
					DisplayNameOverride displayName = uIBaseInputAction.GetDisplayName(uIInputActionPart, "InputActionBindings");
					ActionState actionState = new ActionState(proxyAction, uIBaseInputAction.aliasName, displayName, uIInputActionPart.m_ProcessAs, uIInputActionPart.m_Transform, uIInputActionPart.m_Mask);
					actionState.onChanged += SetConflictsDirty;
					if (!m_Triggers.TryGetValue((actionState.action, actionState.processAs), out var value2))
					{
						value2 = IEventTrigger.GetTrigger(this, actionState.action, actionState.processAs);
						m_Triggers.Add((actionState.action, actionState.processAs), value2);
					}
					value2.states.Add(actionState);
					m_UIActionStates.Add(actionState);
					if (!m_ActionStateMap.TryGetValue(proxyAction, out var value3))
					{
						value3 = new List<ActionState>();
						m_ActionStateMap[proxyAction] = value3;
					}
					value3.Add(actionState);
				}
			}
			if (count != m_UIActionStates.Count)
			{
				m_ActionOrder[uIBaseInputAction.aliasName] = count;
			}
		}
	}

	private void ResolveConflicts()
	{
		ActionState[] array = m_UIActionStates.OrderBy((ActionState a) => a).ToArray();
		Game.Input.InputManager.DeviceType mask = Game.Input.InputManager.instance.mask;
		for (int num = 0; num < m_UIActionStates.Count; num++)
		{
			m_UIActionStates[num].UpdateState();
		}
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			ActionState actionState = array[num2];
			if (actionState.state != ActionState.State.Enabled)
			{
				continue;
			}
			for (int num3 = num2 + 1; num3 < array.Length; num3++)
			{
				ActionState actionState2 = array[num3];
				if (actionState2.state != ActionState.State.Enabled)
				{
					continue;
				}
				if (actionState2.action == actionState.action)
				{
					if (actionState2.transform == actionState.transform || (actionState2.transform & actionState.transform) != UIBaseInputAction.Transform.None)
					{
						actionState2.state = ActionState.State.DisabledDuplicate;
					}
				}
				else if (Game.Input.InputManager.HasConflicts(actionState2.action, actionState.action, actionState.mask & mask, actionState2.mask & mask))
				{
					actionState2.state = ActionState.State.DisabledConflict;
				}
			}
		}
		using (ProxyAction.DeferStateUpdating())
		{
			for (int num4 = 0; num4 < m_UIActionStates.Count; num4++)
			{
				m_UIActionStates[num4].Apply();
			}
		}
		m_ActiveActionsBinding.UpdateAll();
	}
}
