using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.Input;
using UnityEngine.InputSystem;

namespace Game.UI;

public class InputHintBindings : CompositeBinding, IDisposable
{
	internal class InputHint : IJsonWritable
	{
		public readonly ProxyAction action;

		public readonly int version = Game.Input.InputManager.instance.actionVersion;

		public string name;

		public int priority;

		public bool show;

		public readonly List<InputHintItem> items = new List<InputHintItem>();

		public InputHint(ProxyAction action)
		{
			this.action = action;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().Name);
			writer.PropertyName("name");
			writer.Write(name);
			writer.PropertyName("items");
			writer.Write((IList<InputHintItem>)items);
			writer.PropertyName("show");
			writer.Write(show);
			writer.TypeEnd();
		}

		public static InputHint Create(ProxyAction action)
		{
			DisplayNameOverride displayOverride = action.displayOverride;
			if (displayOverride != null)
			{
				return new InputHint(action)
				{
					name = displayOverride.displayName,
					priority = displayOverride.priority,
					show = (displayOverride.active && displayOverride.priority > 0)
				};
			}
			return new InputHint(action)
			{
				name = action.title,
				priority = -1,
				show = false
			};
		}
	}

	internal class InputHintItem : IJsonWritable
	{
		public ControlPath[] bindings;

		public ControlPath[] modifiers;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().Name);
			writer.PropertyName("bindings");
			writer.Write((IList<ControlPath>)bindings);
			writer.PropertyName("modifiers");
			writer.Write((IList<ControlPath>)modifiers);
			writer.TypeEnd();
		}
	}

	private struct TutorialInputHintQuery : IJsonReadable, IJsonWritable
	{
		public string map;

		public string action;

		public int index;

		public Game.Input.InputManager.ControlScheme controlScheme;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("map");
			reader.Read(out map);
			reader.ReadProperty("action");
			reader.Read(out action);
			reader.ReadProperty("controlScheme");
			reader.Read(out int value);
			controlScheme = (Game.Input.InputManager.ControlScheme)value;
			reader.ReadProperty("index");
			reader.Read(out index);
			reader.ReadMapEnd();
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(TutorialInputHintQuery).FullName);
			writer.PropertyName("map");
			writer.Write(map);
			writer.PropertyName("action");
			writer.Write(action);
			writer.PropertyName("controlScheme");
			writer.Write((int)controlScheme);
			writer.PropertyName("index");
			writer.Write(index);
			writer.TypeEnd();
		}
	}

	internal struct InputHintQuery : IJsonReadable, IJsonWritable, IEquatable<InputHintQuery>
	{
		public string action;

		public Game.Input.InputManager.ControlScheme controlScheme;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("action");
			reader.Read(out action);
			reader.ReadProperty("controlScheme");
			reader.Read(out int value);
			controlScheme = (Game.Input.InputManager.ControlScheme)value;
			reader.ReadMapEnd();
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(InputHintQuery).FullName);
			writer.PropertyName("action");
			writer.Write(action);
			writer.PropertyName("controlScheme");
			writer.Write((int)controlScheme);
			writer.TypeEnd();
		}

		public bool Equals(InputHintQuery other)
		{
			if (other.action == action)
			{
				return other.controlScheme == controlScheme;
			}
			return false;
		}
	}

	private const string kGroup = "input";

	private static readonly string[] axisControls = new string[3] { "<Gamepad>/leftStick", "<Gamepad>/rightStick", "<Gamepad>/dpad" };

	private static readonly string[] allDirs = new string[4] { "/up", "/down", "/left", "/right" };

	private static readonly string[] horizontal = new string[2] { "/left", "/right" };

	private static readonly string[] vertical = new string[2] { "/up", "/down" };

	private static readonly string[] axes = new string[2] { "/x", "/y" };

	private readonly ValueBinding<InputHint[]> m_ActiveHintsBinding;

	private readonly GetterMapBinding<InputHintQuery, InputHint> m_HintsMapBinding;

	private readonly ValueBinding<int> m_GamepadTypeBinding;

	private readonly GetterMapBinding<TutorialInputHintQuery, InputHint[]> m_TutorialHints;

	private Dictionary<(string name, int priority), InputHint> m_Hints = new Dictionary<(string, int), InputHint>();

	private bool m_HintsDirty = true;

	private bool m_TutorialHintsDirty = true;

	private static Dictionary<IReadOnlyList<ProxyModifier>, List<ProxyBinding>> modifiersGroups = new Dictionary<IReadOnlyList<ProxyModifier>, List<ProxyBinding>>(new ProxyBinding.ModifiersListComparer(ProxyModifier.pathComparer));

	public event Action<ProxyAction> onInputHintPerformed;

	public InputHintBindings()
	{
		AddBinding(m_HintsMapBinding = new GetterMapBinding<InputHintQuery, InputHint>("input", "hints", GetInputHint, new ValueReader<InputHintQuery>(), new ValueWriter<InputHintQuery>(), new ValueWriter<InputHint>()));
		AddBinding(m_ActiveHintsBinding = new ValueBinding<InputHint[]>("input", "activeHints", Array.Empty<InputHint>(), new ArrayWriter<InputHint>(new ValueWriter<InputHint>())));
		AddBinding(m_GamepadTypeBinding = new ValueBinding<int>("input", "gamepadType", (int)Game.Input.InputManager.instance.GetActiveGamepadType()));
		AddBinding(m_TutorialHints = new GetterMapBinding<TutorialInputHintQuery, InputHint[]>("input", "tutorialHints", GetTutorialHints, new ValueReader<TutorialInputHintQuery>(), new ValueWriter<TutorialInputHintQuery>(), new ArrayWriter<InputHint>(new ValueWriter<InputHint>())));
		AddBinding(new TriggerBinding<string>("input", "onInputHintPerformed", HandleInputHintPerformed));
		Game.Input.InputManager.instance.EventActionsChanged += OnActionsChanged;
		Game.Input.InputManager.instance.EventEnabledActionsChanged += OnEnabledActionsChanged;
		Game.Input.InputManager.instance.EventActionDisplayNamesChanged += OnActionDisplayNamesChanged;
		Game.Input.InputManager.instance.EventControlSchemeChanged += OnControlSchemeChanged;
		Game.Input.InputManager.instance.EventActiveDeviceChanged += OnActiveDeviceChanged;
	}

	public void Dispose()
	{
		Game.Input.InputManager.instance.EventActionsChanged -= OnActionsChanged;
		Game.Input.InputManager.instance.EventEnabledActionsChanged -= OnEnabledActionsChanged;
		Game.Input.InputManager.instance.EventActionDisplayNamesChanged -= OnActionDisplayNamesChanged;
		Game.Input.InputManager.instance.EventControlSchemeChanged -= OnControlSchemeChanged;
		Game.Input.InputManager.instance.EventActiveDeviceChanged -= OnActiveDeviceChanged;
	}

	private void OnActionsChanged()
	{
		m_HintsDirty = true;
		m_TutorialHintsDirty = true;
		m_HintsMapBinding.UpdateAll();
	}

	private void OnEnabledActionsChanged()
	{
		m_HintsDirty = true;
	}

	private void OnActionDisplayNamesChanged()
	{
		m_HintsDirty = true;
	}

	private void OnControlSchemeChanged(Game.Input.InputManager.ControlScheme controlScheme)
	{
		m_HintsDirty = true;
	}

	private void OnActiveDeviceChanged(InputDevice newDevice, InputDevice oldDevice, bool schemeChanged)
	{
		if (Game.Input.InputManager.instance.activeControlScheme == Game.Input.InputManager.ControlScheme.Gamepad)
		{
			m_GamepadTypeBinding.Update((int)Game.Input.InputManager.instance.GetActiveGamepadType());
		}
	}

	public override bool Update()
	{
		if (m_TutorialHintsDirty)
		{
			m_TutorialHints.Update();
			m_TutorialHintsDirty = false;
		}
		if (m_HintsDirty)
		{
			m_HintsDirty = false;
			RebuildHints();
			m_ActiveHintsBinding.Update(m_Hints.Values.OrderBy((InputHint h) => h.priority).ToArray());
		}
		return base.Update();
	}

	private void HandleInputHintPerformed(string action)
	{
		foreach (InputHint value in m_Hints.Values)
		{
			if (value.name == action)
			{
				this.onInputHintPerformed?.Invoke(value.action);
				break;
			}
		}
	}

	private void RebuildHints()
	{
		m_Hints.Clear();
		foreach (ProxyAction action in Game.Input.InputManager.instance.actions)
		{
			if (action.displayOverride != null)
			{
				Game.Input.InputManager.ControlScheme activeControlScheme = Game.Input.InputManager.instance.activeControlScheme;
				CollectHints(m_Hints, action, activeControlScheme switch
				{
					Game.Input.InputManager.ControlScheme.Gamepad => Game.Input.InputManager.DeviceType.Gamepad, 
					Game.Input.InputManager.ControlScheme.KeyboardAndMouse => Game.Input.InputManager.DeviceType.Keyboard | Game.Input.InputManager.DeviceType.Mouse, 
					_ => Game.Input.InputManager.DeviceType.None, 
				});
			}
		}
	}

	private static void CollectHints(Dictionary<(string name, int priority), InputHint> hints, ProxyAction action, Game.Input.InputManager.DeviceType device, bool ignoreMask = false)
	{
		string item = action.displayOverride?.displayName ?? action.title;
		int item2 = action.displayOverride?.priority ?? (-1);
		if (!hints.TryGetValue((item, item2), out var value))
		{
			value = InputHint.Create(action);
			hints[(item, item2)] = value;
		}
		CollectHintItems(value, action, device, action.displayOverride?.transform ?? UIBaseInputAction.Transform.None, ignoreMask);
	}

	internal static void CollectHintItems(InputHint hint, ProxyAction action, Game.Input.InputManager.DeviceType device, UIBaseInputAction.Transform transform, bool ignoreMask = true)
	{
		foreach (var (_, proxyComposite2) in action.composites)
		{
			if ((!ignoreMask && (proxyComposite2.m_Device & action.mask) == 0) || (proxyComposite2.m_Device & device) == 0)
			{
				continue;
			}
			modifiersGroups.Clear();
			ProxyBinding value;
			foreach (KeyValuePair<ActionComponent, ProxyBinding> binding in proxyComposite2.bindings)
			{
				binding.Deconstruct(out var _, out value);
				ProxyBinding item = value;
				if (item.isSet && !item.isDummy && (transform == UIBaseInputAction.Transform.None || (item.component.ToTransform() & transform) != UIBaseInputAction.Transform.None))
				{
					if (!modifiersGroups.TryGetValue(item.modifiers, out var value2))
					{
						value2 = new List<ProxyBinding>();
						modifiersGroups[item.modifiers] = value2;
					}
					value2.Add(item);
				}
			}
			foreach (KeyValuePair<IReadOnlyList<ProxyModifier>, List<ProxyBinding>> modifiersGroup in modifiersGroups)
			{
				modifiersGroup.Deconstruct(out var key2, out var value3);
				IReadOnlyList<ProxyModifier> readOnlyList = key2;
				List<ProxyBinding> list = value3;
				string[] paths = new string[list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					string[] array = paths;
					int num = i;
					value = list[i];
					array[num] = value.path;
				}
				SimplifyPaths(ref paths);
				ControlPath[] array2 = new ControlPath[paths.Length];
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j] = ControlPath.Get(paths[j]);
				}
				ControlPath[] array3 = new ControlPath[readOnlyList.Count];
				for (int k = 0; k < array3.Length; k++)
				{
					array3[k] = ControlPath.Get(readOnlyList[k].m_Path);
				}
				hint.items.Add(new InputHintItem
				{
					bindings = array2,
					modifiers = array3
				});
			}
		}
	}

	private static InputHint[] GetTutorialHints(TutorialInputHintQuery query)
	{
		ProxyAction action = Game.Input.InputManager.instance.FindAction(query.map, query.action);
		if (action == null)
		{
			return Array.Empty<InputHint>();
		}
		if (query.controlScheme == Game.Input.InputManager.ControlScheme.Gamepad)
		{
			Dictionary<(string, int), InputHint> dictionary = new Dictionary<(string, int), InputHint>();
			Game.Input.InputManager.ControlScheme controlScheme = query.controlScheme;
			CollectHints(dictionary, action, controlScheme switch
			{
				Game.Input.InputManager.ControlScheme.Gamepad => Game.Input.InputManager.DeviceType.Gamepad, 
				Game.Input.InputManager.ControlScheme.KeyboardAndMouse => Game.Input.InputManager.DeviceType.Keyboard | Game.Input.InputManager.DeviceType.Mouse, 
				_ => Game.Input.InputManager.DeviceType.None, 
			}, ignoreMask: true);
			return dictionary.Values.OrderBy((InputHint h) => h.priority).ToArray();
		}
		if (query.index >= 0)
		{
			ProxyBinding proxyBinding = action.bindings.Where((ProxyBinding b) => MatchesControlScheme(b, query.controlScheme)).Skip(query.index).FirstOrDefault();
			return new InputHint[1]
			{
				new InputHint(action)
				{
					name = action.title,
					items = 
					{
						new InputHintItem
						{
							bindings = new ControlPath[1] { ControlPath.Get(proxyBinding.path) },
							modifiers = proxyBinding.modifiers.Select((ProxyModifier m) => ControlPath.Get(m.m_Path)).ToArray()
						}
					}
				}
			};
		}
		return (from b in action.bindings
			where b.isSet && MatchesControlScheme(b, query.controlScheme)
			select new InputHint(action)
			{
				name = action.title,
				items = 
				{
					new InputHintItem
					{
						bindings = new ControlPath[1] { ControlPath.Get(b.path) },
						modifiers = b.modifiers.Select((ProxyModifier m) => ControlPath.Get(m.m_Path)).ToArray()
					}
				}
			}).ToArray();
	}

	private InputHint GetInputHint(InputHintQuery query)
	{
		if (m_HintsMapBinding.values.TryGetValue(query, out var value) && value.version == Game.Input.InputManager.instance.actionVersion)
		{
			return value;
		}
		UIBaseInputAction[] inputActions = Game.Input.InputManager.instance.uiActionCollection.m_InputActions;
		UIBaseInputAction uIBaseInputAction = null;
		for (int i = 0; i < inputActions.Length; i++)
		{
			if (inputActions[i].aliasName == query.action)
			{
				uIBaseInputAction = inputActions[i];
				break;
			}
		}
		if (uIBaseInputAction == null)
		{
			return null;
		}
		value = new InputHint(null)
		{
			name = uIBaseInputAction.aliasName,
			priority = uIBaseInputAction.displayPriority,
			show = true
		};
		foreach (UIInputActionPart actionPart in uIBaseInputAction.actionParts)
		{
			if (Game.Input.InputManager.instance.TryFindAction(actionPart.m_Action, out var proxyAction))
			{
				Game.Input.InputManager.ControlScheme controlScheme = query.controlScheme;
				CollectHintItems(value, proxyAction, controlScheme switch
				{
					Game.Input.InputManager.ControlScheme.Gamepad => Game.Input.InputManager.DeviceType.Gamepad, 
					Game.Input.InputManager.ControlScheme.KeyboardAndMouse => Game.Input.InputManager.DeviceType.Keyboard | Game.Input.InputManager.DeviceType.Mouse, 
					_ => Game.Input.InputManager.DeviceType.None, 
				}, actionPart.m_Transform);
			}
		}
		return value;
	}

	private static bool MatchesControlScheme(ProxyBinding binding, Game.Input.InputManager.ControlScheme controlScheme)
	{
		if (controlScheme != Game.Input.InputManager.ControlScheme.Gamepad || !binding.isGamepad)
		{
			if (controlScheme == Game.Input.InputManager.ControlScheme.KeyboardAndMouse)
			{
				if (!binding.isKeyboard)
				{
					return binding.isMouse;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static void SimplifyPaths(ref string[] paths)
	{
		for (int i = 0; i < axisControls.Length; i++)
		{
			string text = axisControls[i];
			if (MatchesDirections(paths, text, allDirs) || MatchesDirections(paths, text, axes))
			{
				paths = new string[1] { text };
				break;
			}
			if (MatchesDirections(paths, text, horizontal))
			{
				paths = new string[1] { text + "/x" };
				break;
			}
			if (MatchesDirections(paths, text, vertical))
			{
				paths = new string[1] { text + "/y" };
				break;
			}
		}
	}

	private static bool MatchesDirections(string[] bindings, string basePath, string[] dirs)
	{
		if (bindings.Length != dirs.Length)
		{
			return false;
		}
		foreach (string text in dirs)
		{
			bool flag = false;
			foreach (string text2 in bindings)
			{
				if (text2.Length == basePath.Length + text.Length && text2.StartsWith(basePath) && text2.EndsWith(text))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}
}
