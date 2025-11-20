using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.Input;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

public class InputRebindingUISystem : UISystemBase
{
	[Flags]
	private enum Options
	{
		None = 0,
		Unsolved = 1,
		Swap = 2,
		Unset = 4,
		Forward = 8,
		Backward = 0x10
	}

	private record BindingPair(ProxyBinding oldBinding, ProxyBinding newBinding);

	private struct ConflictInfo : IJsonWritable
	{
		public ProxyBinding binding;

		public ConflictInfoItem[] conflicts;

		public bool unsolved { get; set; }

		public bool swap { get; set; }

		public bool unset { get; set; }

		public bool batchSwap { get; set; }

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(ConflictInfo).FullName);
			writer.PropertyName("binding");
			writer.Write(binding);
			writer.PropertyName("conflicts");
			writer.Write((IList<ConflictInfoItem>)conflicts.Where((ConflictInfoItem c) => !c.isHidden).ToArray());
			writer.PropertyName("unsolved");
			writer.Write(unsolved);
			writer.PropertyName("swap");
			writer.Write(swap);
			writer.PropertyName("unset");
			writer.Write(unset);
			writer.PropertyName("batchSwap");
			writer.Write(batchSwap);
			writer.TypeEnd();
		}
	}

	private struct ConflictInfoItem : IJsonWritable
	{
		public ProxyBinding binding;

		public ProxyBinding resolution;

		public Options options;

		public bool isAlias;

		public bool isHidden;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(ConflictInfoItem).FullName);
			writer.PropertyName("binding");
			writer.Write(binding);
			writer.PropertyName("resolution");
			writer.Write(resolution);
			writer.PropertyName("isHidden");
			writer.Write(isHidden);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "inputRebinding";

	private ValueBinding<ProxyBinding?> m_ActiveRebindingBinding;

	private ValueBinding<ConflictInfo?> m_ActiveConflictBinding;

	private InputActionRebindingExtensions.RebindingOperation m_Operation;

	private InputActionRebindingExtensions.RebindingOperation m_ModifierOperation;

	private ProxyBinding? m_ActiveRebinding;

	private Action<ProxyBinding> m_OnSetBinding;

	private ProxyBinding? m_PendingRebinding;

	private Dictionary<string, ConflictInfoItem> m_Conflicts = new Dictionary<string, ConflictInfoItem>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_ActiveRebindingBinding = new ValueBinding<ProxyBinding?>("inputRebinding", "activeRebinding", null, ValueWritersStruct.Nullable(new ValueWriter<ProxyBinding>())));
		AddBinding(m_ActiveConflictBinding = new ValueBinding<ConflictInfo?>("inputRebinding", "activeConflict", null, ValueWritersStruct.Nullable(new ValueWriter<ConflictInfo>())));
		AddBinding(new TriggerBinding("inputRebinding", "cancelRebinding", Cancel));
		AddBinding(new TriggerBinding("inputRebinding", "completeAndSwapConflicts", CompleteAndSwapConflicts));
		AddBinding(new TriggerBinding("inputRebinding", "completeAndUnsetConflicts", CompleteAndUnsetConflicts));
		m_Operation = new InputActionRebindingExtensions.RebindingOperation();
		m_Operation.OnApplyBinding(OnApplyBinding);
		m_Operation.OnComplete(OnComplete);
		m_Operation.OnCancel(OnCancel);
		m_ModifierOperation = new InputActionRebindingExtensions.RebindingOperation();
		m_ModifierOperation.OnPotentialMatch(OnModifierPotentialMatch);
		m_ModifierOperation.OnApplyBinding(OnModifierApplyBinding);
		InputSystem.onDeviceChange += OnDeviceChange;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Operation.Dispose();
		m_ModifierOperation.Dispose();
		InputSystem.onDeviceChange -= OnDeviceChange;
		base.OnDestroy();
	}

	private void OnDeviceChange(InputDevice changedDevice, InputDeviceChange change)
	{
		if ((change != InputDeviceChange.Added && change != InputDeviceChange.Removed) || !m_ActiveRebinding.HasValue)
		{
			return;
		}
		foreach (InputDevice device in InputSystem.devices)
		{
			if (device.added && ((device is Keyboard && m_ActiveRebinding.Value.isKeyboard) || (device is Mouse && m_ActiveRebinding.Value.isMouse) || (device is Gamepad && m_ActiveRebinding.Value.isGamepad)))
			{
				return;
			}
		}
		Cancel();
	}

	public void Start(ProxyBinding binding, Action<ProxyBinding> onSetBinding)
	{
		if (m_ActiveRebinding == binding || onSetBinding == null)
		{
			return;
		}
		m_ActiveRebinding = binding;
		m_OnSetBinding = onSetBinding;
		m_Conflicts.Clear();
		if (m_ActiveRebinding.Value.isKeyboard)
		{
			Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.Keyboard;
		}
		else if (m_ActiveRebinding.Value.isMouse)
		{
			Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.Mouse;
		}
		else if (m_ActiveRebinding.Value.isGamepad)
		{
			Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.Gamepad;
		}
		else
		{
			Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.None;
		}
		m_ActiveRebindingBinding.Update(binding);
		m_ActiveConflictBinding.Update(null);
		m_Operation.Reset().WithMagnitudeHavingToBeGreaterThan(0.6f).OnMatchWaitForAnother(0.1f);
		m_ModifierOperation.Reset().WithMagnitudeHavingToBeGreaterThan(0.6f);
		if (binding.isKeyboard)
		{
			m_Operation.WithControlsHavingToMatchPath("<Keyboard>/<Key>").WithControlsExcluding("<Keyboard>/leftShift").WithControlsExcluding("<Keyboard>/rightShift")
				.WithControlsExcluding("<Keyboard>/leftCtrl")
				.WithControlsExcluding("<Keyboard>/rightCtrl")
				.WithControlsExcluding("<Keyboard>/leftAlt")
				.WithControlsExcluding("<Keyboard>/rightAlt")
				.WithControlsExcluding("<Keyboard>/capsLock")
				.WithControlsExcluding("<Keyboard>/leftWindows")
				.WithControlsExcluding("<Keyboard>/rightWindow")
				.WithControlsExcluding("<Keyboard>/leftMeta")
				.WithControlsExcluding("<Keyboard>/rightMeta")
				.WithControlsExcluding("<Keyboard>/numLock")
				.WithControlsExcluding("<Keyboard>/printScreen")
				.WithControlsExcluding("<Keyboard>/scrollLock")
				.WithControlsExcluding("<Keyboard>/insert")
				.WithControlsExcluding("<Keyboard>/contextMenu")
				.WithControlsExcluding("<Keyboard>/pause")
				.Start();
			if (binding.allowModifiers && binding.isModifiersRebindable)
			{
				m_ModifierOperation.WithControlsHavingToMatchPath("<Keyboard>/shift").WithControlsHavingToMatchPath("<Keyboard>/ctrl").WithControlsHavingToMatchPath("<Keyboard>/alt")
					.Start();
			}
		}
		else if (binding.isMouse)
		{
			m_Operation.WithControlsHavingToMatchPath("<Mouse>/<Button>").Start();
			if (binding.allowModifiers && binding.isModifiersRebindable)
			{
				m_ModifierOperation.WithControlsHavingToMatchPath("<Keyboard>/shift").WithControlsHavingToMatchPath("<Keyboard>/ctrl").WithControlsHavingToMatchPath("<Keyboard>/alt")
					.Start();
			}
		}
		else if (binding.isGamepad)
		{
			m_Operation.WithControlsHavingToMatchPath("<Gamepad>/<Button>").WithControlsHavingToMatchPath("<Gamepad>/*/<Button>").WithControlsExcluding("<Gamepad>/leftStickPress")
				.WithControlsExcluding("<Gamepad>/rightStickPress")
				.WithControlsExcluding("<DualSenseGamepadHID>/leftTriggerButton")
				.WithControlsExcluding("<DualSenseGamepadHID>/rightTriggerButton")
				.WithControlsExcluding("<DualSenseGamepadHID>/systemButton")
				.WithControlsExcluding("<DualSenseGamepadHID>/micButton")
				.WithControlsExcluding("<PS5DualSenseGamepad>/leftTriggerButton")
				.WithControlsExcluding("<PS5DualSenseGamepad>/rightTriggerButton")
				.WithControlsExcluding("<DualSenseGamepadPC>/leftTriggerButton")
				.WithControlsExcluding("<DualSenseGamepadPC>/rightTriggerButton")
				.Start();
			if (binding.allowModifiers && binding.isModifiersRebindable)
			{
				m_ModifierOperation.WithControlsHavingToMatchPath("<Gamepad>/leftStickPress").WithControlsHavingToMatchPath("<Gamepad>/rightStickPress").Start();
			}
		}
	}

	public void Start(ProxyBinding binding, ProxyBinding newBinding, Action<ProxyBinding> onSetBinding)
	{
		if (!(m_ActiveRebinding == binding) && onSetBinding != null)
		{
			m_ActiveRebinding = binding;
			m_OnSetBinding = onSetBinding;
			m_ActiveRebindingBinding.Update(binding);
			m_ActiveConflictBinding.Update(null);
			Process(binding, newBinding);
		}
	}

	public void Cancel()
	{
		m_Operation.Reset();
		Reset();
	}

	private void CompleteAndSwapConflicts()
	{
		if (m_PendingRebinding.HasValue)
		{
			using (Game.Input.InputManager.DeferUpdating())
			{
				IEnumerable<ProxyBinding> newBindings = from c in m_Conflicts.Values
					where !c.isAlias
					select c.resolution;
				Game.Input.InputManager.instance.SetBindings(newBindings, out var _);
				Apply(m_PendingRebinding.Value);
			}
		}
		Reset();
	}

	private void CompleteAndUnsetConflicts()
	{
		if (m_PendingRebinding.HasValue)
		{
			using (Game.Input.InputManager.DeferUpdating())
			{
				IEnumerable<ProxyBinding> newBindings = from c in m_Conflicts.Values
					where !c.isAlias
					select c.binding.WithPath(string.Empty).WithModifiers(Array.Empty<ProxyModifier>());
				Game.Input.InputManager.instance.SetBindings(newBindings, out var _);
				Apply(m_PendingRebinding.Value);
			}
		}
		Reset();
	}

	private void OnApplyBinding(InputActionRebindingExtensions.RebindingOperation operation, string path)
	{
		if (!m_ActiveRebinding.HasValue)
		{
			return;
		}
		Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.None;
		if (path != null && path.StartsWith("<DualShockGamepad>"))
		{
			path = path.Replace("<DualShockGamepad>", "<Gamepad>");
		}
		ProxyBinding oldBinding = m_ActiveRebinding.Value;
		ProxyBinding newBinding = oldBinding.Copy();
		if (newBinding.isKeyRebindable)
		{
			newBinding.path = path;
		}
		if (!newBinding.allowModifiers)
		{
			newBinding.modifiers = Array.Empty<ProxyModifier>();
		}
		else if (newBinding.isModifiersRebindable)
		{
			newBinding.modifiers = (from c in m_ModifierOperation.candidates
				where c.IsPressed()
				select new ProxyModifier
				{
					m_Component = oldBinding.component,
					m_Name = Game.Input.InputManager.GetModifierName(oldBinding.component),
					m_Path = Game.Input.InputManager.GeneratePathForControl(c)
				}).ToList();
		}
		m_ModifierOperation.Reset();
		Process(oldBinding, newBinding);
	}

	private void OnComplete(InputActionRebindingExtensions.RebindingOperation operation)
	{
		Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.None;
		if (!m_PendingRebinding.HasValue)
		{
			Reset();
		}
	}

	private void OnCancel(InputActionRebindingExtensions.RebindingOperation operation)
	{
		Game.Input.InputManager.instance.blockedControlTypes = Game.Input.InputManager.DeviceType.None;
		Reset();
	}

	private void Process(ProxyBinding oldBinding, ProxyBinding newBinding)
	{
		UISystemBase.log.InfoFormat("Rebinding from {0} to {1}", oldBinding, newBinding);
		if (newBinding.action == null)
		{
			Reset();
			return;
		}
		if (!NeedAskUser(newBinding))
		{
			Apply(newBinding);
			Reset();
			return;
		}
		m_Conflicts.Clear();
		GetRebindOptions(m_Conflicts, oldBinding, newBinding, out var unsolved, out var batchSwap, out var swap, out var unset);
		if (m_Conflicts.Count == 0)
		{
			Apply(newBinding);
			Reset();
			return;
		}
		m_PendingRebinding = newBinding;
		m_ActiveConflictBinding.Update(new ConflictInfo
		{
			binding = newBinding,
			conflicts = m_Conflicts.Values.OrderBy((ConflictInfoItem b) => b.binding.mapName).ToArray(),
			unsolved = unsolved,
			swap = swap,
			unset = unset,
			batchSwap = batchSwap
		});
		static bool NeedAskUser(ProxyBinding binding)
		{
			ProxyAction action = binding.action;
			if (action.m_LinkedActions.Count != 0)
			{
				return true;
			}
			foreach (UIBaseInputAction uIAlias in action.m_UIAliases)
			{
				if (!uIAlias.showInOptions)
				{
					break;
				}
				foreach (UIInputActionPart actionPart in uIAlias.actionParts)
				{
					if ((actionPart.m_Mask & binding.device) != Game.Input.InputManager.DeviceType.None)
					{
						return true;
					}
				}
			}
			return binding.hasConflicts != ProxyBinding.ConflictType.None;
		}
	}

	private void GetRebindOptions(Dictionary<string, ConflictInfoItem> conflictInfos, ProxyBinding oldBinding, ProxyBinding newBinding, out bool unsolved, out bool batchSwap, out bool swap, out bool unset)
	{
		unsolved = false;
		batchSwap = false;
		swap = false;
		unset = false;
		if (!CollectLinkedBindings(oldBinding, newBinding, out var rebindingMap))
		{
			return;
		}
		ProxyBinding oldBinding2;
		List<BindingPair> value;
		foreach (KeyValuePair<ProxyBinding, List<BindingPair>> item in rebindingMap)
		{
			item.Deconstruct(out oldBinding2, out value);
			List<BindingPair> list = value;
			GetRebindOptions(conflictInfos, list);
		}
		unsolved = conflictInfos.Values.Any((ConflictInfoItem c) => (c.options & Options.Unsolved) != 0);
		swap = conflictInfos.Values.All((ConflictInfoItem c) => (c.options & Options.Swap) != Options.None && (c.options & Options.Backward) == 0);
		unset = conflictInfos.Values.All((ConflictInfoItem c) => (c.options & Options.Unset) != Options.None && (c.options & Options.Backward) == 0);
		batchSwap = conflictInfos.Values.Any((ConflictInfoItem c) => (c.options & Options.Backward) != 0);
		int count = conflictInfos.Count;
		foreach (KeyValuePair<ProxyBinding, List<BindingPair>> item2 in rebindingMap)
		{
			item2.Deconstruct(out oldBinding2, out value);
			List<BindingPair> list2 = value;
			foreach (BindingPair item3 in list2)
			{
				item3.Deconstruct(out oldBinding2, out var newBinding2);
				ProxyBinding binding = oldBinding2;
				ProxyBinding resolution = newBinding2;
				ConflictInfoItem conflictInfoItem = new ConflictInfoItem
				{
					binding = binding,
					resolution = resolution
				};
				if (!conflictInfos.ContainsKey(conflictInfoItem.binding.title))
				{
					CollectAliases(conflictInfos, conflictInfoItem);
					if ((rebindingMap.Count > 1 || list2.Count > 1 || conflictInfos.Count != count) | batchSwap)
					{
						conflictInfos.TryAdd(conflictInfoItem.binding.title, conflictInfoItem);
					}
				}
			}
		}
		batchSwap |= conflictInfos.Count != count;
		swap &= !batchSwap;
		unset &= !batchSwap;
	}

	private void GetRebindOptions(Dictionary<string, ConflictInfoItem> conflictInfos, List<BindingPair> list)
	{
		List<BindingPair> list2 = new List<BindingPair>();
		List<BindingPair> list3 = new List<BindingPair>();
		Usages otherUsages = new Usages(0, readOnly: false);
		Usages otherUsages2 = new Usages(0, readOnly: false);
		ProxyBinding newBinding;
		ProxyBinding oldBinding;
		foreach (BindingPair item in list)
		{
			item.Deconstruct(out newBinding, out oldBinding);
			ProxyBinding proxyBinding = newBinding;
			ProxyBinding proxyBinding2 = oldBinding;
			CollectBindingConflicts(list2, proxyBinding, proxyBinding2);
			CollectBindingConflicts(list3, proxyBinding2, proxyBinding);
			otherUsages2 = Usages.Combine(otherUsages2, proxyBinding2.usages);
		}
		foreach (BindingPair item2 in list)
		{
			item2.Deconstruct(out oldBinding, out newBinding);
			ProxyBinding x = oldBinding;
			ProxyBinding y = newBinding;
			if (ProxyBinding.PathEquals(x, y))
			{
				ProcessConflict(conflictInfos, list3, ref otherUsages2, ref otherUsages2, Options.None, out var _);
			}
		}
		bool changed2 = true;
		while (changed2)
		{
			ProcessConflict(conflictInfos, list3, ref otherUsages2, ref otherUsages, Options.Forward, out changed2);
			if (changed2)
			{
				ProcessConflict(conflictInfos, list2, ref otherUsages, ref otherUsages2, Options.Backward, out changed2);
				if (!changed2)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	private void CollectBindingConflicts(List<BindingPair> conflicts, ProxyBinding toCheck, ProxyBinding resolution)
	{
		if (!Game.Input.InputManager.instance.keyActionMap.TryGetValue(toCheck.path, out var value))
		{
			return;
		}
		ProxyAction action = toCheck.action;
		foreach (ProxyAction item3 in value)
		{
			foreach (var (_, proxyComposite2) in item3.composites)
			{
				if (proxyComposite2.isDummy || proxyComposite2.m_Device != toCheck.device)
				{
					continue;
				}
				bool flag = Game.Input.InputManager.CanConflict(action, item3, proxyComposite2.m_Device);
				foreach (var (_, proxyBinding2) in proxyComposite2.bindings)
				{
					if ((!flag && ProxyBinding.componentComparer.Equals(proxyBinding2, toCheck)) || !ProxyBinding.PathEquals(proxyBinding2, toCheck) || proxyBinding2.usages.isNone)
					{
						continue;
					}
					BindingPair item = new BindingPair(proxyBinding2, resolution);
					if (!conflicts.Contains(item))
					{
						conflicts.Add(item);
					}
					foreach (ProxyAction.LinkInfo linkedAction in item3.m_LinkedActions)
					{
						if (linkedAction.m_Device == proxyBinding2.device && linkedAction.m_Action.TryGetComposite(proxyBinding2.device, out var composite) && composite.TryGetBinding(proxyBinding2.component, out var foundBinding) && !foundBinding.usages.isNone)
						{
							ProxyBinding newBinding = resolution.Copy();
							if (!foundBinding.isKeyRebindable)
							{
								newBinding.path = foundBinding.path;
							}
							if (!foundBinding.isModifiersRebindable)
							{
								newBinding.modifiers = foundBinding.modifiers;
							}
							BindingPair item2 = new BindingPair(foundBinding, newBinding);
							if (!conflicts.Contains(item2))
							{
								conflicts.Add(item2);
							}
						}
					}
				}
			}
		}
	}

	private bool CollectLinkedBindings(ProxyBinding oldBinding, ProxyBinding newBinding, out Dictionary<ProxyBinding, List<BindingPair>> rebindingMap)
	{
		rebindingMap = new Dictionary<ProxyBinding, List<BindingPair>>(ProxyBinding.pathAndModifiersComparer) { 
		{
			oldBinding,
			new List<BindingPair>
			{
				new BindingPair(oldBinding, newBinding)
			}
		} };
		ProxyAction action = oldBinding.action;
		if (action == null)
		{
			return true;
		}
		foreach (ProxyAction.LinkInfo linkedAction in action.m_LinkedActions)
		{
			if (linkedAction.m_Device != oldBinding.device)
			{
				continue;
			}
			foreach (var (_, proxyComposite2) in linkedAction.m_Action.composites)
			{
				if (proxyComposite2.isDummy)
				{
					continue;
				}
				foreach (var (_, proxyBinding2) in proxyComposite2.bindings)
				{
					if (ProxyBinding.componentComparer.Equals(oldBinding, proxyBinding2))
					{
						if (!proxyBinding2.isRebindable)
						{
							return false;
						}
						ProxyBinding newBinding2 = proxyBinding2.Copy();
						if (newBinding2.isKeyRebindable)
						{
							newBinding2.path = newBinding.path;
						}
						if (!newBinding2.allowModifiers)
						{
							newBinding2.modifiers = Array.Empty<ProxyModifier>();
						}
						else if (newBinding2.isModifiersRebindable)
						{
							newBinding2.modifiers = newBinding.modifiers;
						}
						if (!rebindingMap.TryGetValue(proxyBinding2, out var value))
						{
							value = new List<BindingPair>();
							rebindingMap[proxyBinding2] = value;
						}
						value.Add(new BindingPair(proxyBinding2, newBinding2));
					}
				}
			}
		}
		return true;
	}

	private void CollectAliases(Dictionary<string, ConflictInfoItem> conflictInfos, ConflictInfoItem mainInfo)
	{
		foreach (UIBaseInputAction uIAlias in mainInfo.binding.action.m_UIAliases)
		{
			if (mainInfo.binding.alies == uIAlias || !uIAlias.showInOptions)
			{
				continue;
			}
			foreach (UIInputActionPart actionPart in uIAlias.actionParts)
			{
				if ((actionPart.m_Transform == UIBaseInputAction.Transform.None || (mainInfo.binding.component.ToTransform() & actionPart.m_Transform) != UIBaseInputAction.Transform.None) && (actionPart.m_Mask & mainInfo.binding.device) != Game.Input.InputManager.DeviceType.None)
				{
					ProxyBinding binding = mainInfo.binding.Copy();
					binding.alies = uIAlias;
					ProxyBinding resolution = mainInfo.resolution.Copy();
					resolution.alies = uIAlias;
					conflictInfos.TryAdd(binding.title, new ConflictInfoItem
					{
						binding = binding,
						resolution = resolution,
						options = mainInfo.options,
						isAlias = true
					});
				}
			}
		}
		if (mainInfo.binding.isAlias)
		{
			ProxyBinding binding2 = mainInfo.binding.Copy();
			binding2.alies = null;
			ProxyBinding resolution2 = mainInfo.resolution.Copy();
			resolution2.alies = null;
			conflictInfos.TryAdd(binding2.title, new ConflictInfoItem
			{
				binding = binding2,
				resolution = resolution2,
				options = mainInfo.options,
				isAlias = true
			});
		}
	}

	private void ProcessConflict(Dictionary<string, ConflictInfoItem> conflictInfos, List<BindingPair> bindingConflicts, ref Usages usages, ref Usages otherUsages, Options direction, out bool changed)
	{
		changed = false;
		for (int i = 0; i < bindingConflicts.Count; i++)
		{
			var (y, x) = bindingConflicts[i];
			if (!Usages.TestAny(usages, y.usages))
			{
				continue;
			}
			bool flag = CanSwap(x, y, direction == Options.Backward);
			bool canBeEmpty = y.canBeEmpty;
			if (!flag && !canBeEmpty)
			{
				AddToConflictInfos(new ConflictInfoItem
				{
					binding = y.Copy(),
					resolution = y.Copy(),
					options = (direction | Options.Unsolved)
				});
				changed = true;
			}
			else if (flag)
			{
				ProxyBinding resolution = y.Copy();
				resolution.path = x.path;
				if (!y.allowModifiers)
				{
					resolution.modifiers = Array.Empty<ProxyModifier>();
				}
				else if (y.isModifiersRebindable)
				{
					resolution.modifiers = x.modifiers;
				}
				AddToConflictInfos(new ConflictInfoItem
				{
					binding = y.Copy(),
					resolution = resolution,
					options = (direction | Options.Swap)
				});
				changed = true;
				otherUsages = Usages.Combine(otherUsages, y.usages);
				foreach (ProxyAction.LinkInfo linkedAction in y.action.m_LinkedActions)
				{
					if (linkedAction.m_Device == x.device && linkedAction.m_Action.TryGetComposite(x.device, out var composite) && composite.TryGetBinding(x.component, out var foundBinding))
					{
						usages = Usages.Combine(usages, foundBinding.usages);
					}
				}
			}
			else if (canBeEmpty)
			{
				ProxyBinding resolution2 = y.Copy();
				resolution2.path = string.Empty;
				resolution2.modifiers = Array.Empty<ProxyModifier>();
				AddToConflictInfos(new ConflictInfoItem
				{
					binding = y.Copy(),
					resolution = resolution2,
					options = (direction | Options.Unset)
				});
				changed = true;
			}
			bindingConflicts.RemoveAt(i);
			i--;
		}
		void AddToConflictInfos(ConflictInfoItem info)
		{
			if (conflictInfos.TryAdd(info.binding.title, info))
			{
				CollectAliases(conflictInfos, info);
			}
		}
	}

	private static bool CanSwap(ProxyBinding x, ProxyBinding y, bool checkUsage)
	{
		if (!x.isSet || !y.isSet)
		{
			return false;
		}
		if (!x.isRebindable || !y.isRebindable)
		{
			return false;
		}
		if (checkUsage && Usages.TestAny(x.usages, y.usages))
		{
			return false;
		}
		bool flag = ProxyBinding.PathEquals(x, y);
		bool flag2 = ProxyBinding.defaultModifiersComparer.Equals(x.modifiers, y.modifiers);
		if (flag && flag2)
		{
			return false;
		}
		bool num = flag || (x.isKeyRebindable && y.isKeyRebindable);
		bool flag3 = flag2 || (x.allowModifiers && x.isModifiersRebindable && y.allowModifiers && y.isModifiersRebindable);
		return num && flag3;
	}

	private void Apply(ProxyBinding newBinding)
	{
		using (Game.Input.InputManager.DeferUpdating())
		{
			m_OnSetBinding?.Invoke(newBinding);
		}
	}

	private void Reset()
	{
		m_ActiveRebindingBinding.Update(null);
		m_ActiveConflictBinding.Update(null);
		m_ModifierOperation.Reset();
		m_ActiveRebinding = null;
		m_OnSetBinding = null;
		m_PendingRebinding = null;
		m_Conflicts.Clear();
	}

	private void OnModifierPotentialMatch(InputActionRebindingExtensions.RebindingOperation operation)
	{
	}

	private void OnModifierApplyBinding(InputActionRebindingExtensions.RebindingOperation operation, string path)
	{
	}

	[Preserve]
	public InputRebindingUISystem()
	{
	}
}
