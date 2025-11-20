using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Colossal.Json;
using Colossal.UI.Binding;
using UnityEngine.InputSystem;

namespace Game.Input;

public struct ProxyBinding : IEquatable<ProxyBinding>, IJsonWritable
{
	public class Comparer : IEqualityComparer<ProxyBinding>
	{
		[Flags]
		public enum Options
		{
			MapName = 1,
			ActionName = 2,
			Name = 4,
			Path = 8,
			Modifiers = 0x10,
			Usages = 0x20,
			Device = 0x40,
			Component = 0x80
		}

		public static readonly Comparer defaultComparer = new Comparer();

		private readonly ModifiersListComparer m_ModifiersListComparer;

		public readonly Options m_Options;

		public Comparer(Options options = Options.MapName | Options.ActionName | Options.Name | Options.Device, ModifiersListComparer modifiersListComparer = null)
		{
			m_Options = options;
			m_ModifiersListComparer = modifiersListComparer ?? ModifiersListComparer.defaultComparer;
		}

		public bool Equals(ProxyBinding x, ProxyBinding y)
		{
			if ((m_Options & Options.MapName) != 0 && x.m_MapName != y.m_MapName)
			{
				return false;
			}
			if ((m_Options & Options.ActionName) != 0 && x.m_ActionName != y.m_ActionName)
			{
				return false;
			}
			if ((m_Options & Options.Name) != 0 && x.m_Name != y.m_Name)
			{
				return false;
			}
			if ((m_Options & Options.Path) != 0 && x.m_Path != y.m_Path)
			{
				return false;
			}
			if ((m_Options & Options.Modifiers) != 0 && !m_ModifiersListComparer.Equals((IReadOnlyCollection<ProxyModifier>)(object)x.m_Modifiers, (IReadOnlyCollection<ProxyModifier>)(object)y.m_Modifiers))
			{
				return false;
			}
			if ((m_Options & Options.Usages) != 0 && !Usages.Comparer.defaultComparer.Equals(x.usages, y.usages))
			{
				return false;
			}
			if ((m_Options & Options.Device) != 0 && x.m_Device != y.m_Device)
			{
				return false;
			}
			if ((m_Options & Options.Component) != 0 && x.m_Component != y.m_Component)
			{
				return false;
			}
			return true;
		}

		public int GetHashCode(ProxyBinding obj)
		{
			HashCode hashCode = default(HashCode);
			if ((m_Options & Options.MapName) != 0)
			{
				hashCode.Add(obj.m_MapName);
			}
			if ((m_Options & Options.ActionName) != 0)
			{
				hashCode.Add(obj.m_ActionName);
			}
			if ((m_Options & Options.Name) != 0)
			{
				hashCode.Add(obj.m_Name);
			}
			if ((m_Options & Options.Path) != 0)
			{
				hashCode.Add(obj.m_Path);
			}
			if ((m_Options & Options.Modifiers) != 0)
			{
				hashCode.Add((IReadOnlyCollection<ProxyModifier>)(object)obj.m_Modifiers, m_ModifiersListComparer);
			}
			if ((m_Options & Options.Usages) != 0)
			{
				hashCode.Add(obj.usages, Usages.Comparer.defaultComparer);
			}
			if ((m_Options & Options.Device) != 0)
			{
				hashCode.Add(obj.m_Device);
			}
			if ((m_Options & Options.Component) != 0)
			{
				hashCode.Add(obj.m_Component);
			}
			return hashCode.ToHashCode();
		}
	}

	public class ModifiersListComparer : IEqualityComparer<IReadOnlyCollection<ProxyModifier>>
	{
		public static readonly ModifiersListComparer defaultComparer = new ModifiersListComparer();

		private readonly ProxyModifier.Comparer m_ModifierComparer;

		public ModifiersListComparer(ProxyModifier.Comparer modifierComparer = null)
		{
			m_ModifierComparer = modifierComparer ?? ProxyModifier.Comparer.defaultComparer;
		}

		public bool Equals(IReadOnlyCollection<ProxyModifier> x, IReadOnlyCollection<ProxyModifier> y)
		{
			if (x == null)
			{
				return y == null;
			}
			if (y == null)
			{
				return false;
			}
			if (x.Count != y.Count)
			{
				return false;
			}
			if (x.Count == 0 && y.Count == 0)
			{
				return true;
			}
			foreach (ProxyModifier item in x)
			{
				if (!y.Contains(item, m_ModifierComparer))
				{
					return false;
				}
			}
			foreach (ProxyModifier item2 in y)
			{
				if (!x.Contains(item2, m_ModifierComparer))
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(IReadOnlyCollection<ProxyModifier> list)
		{
			return list.Count.GetHashCode();
		}
	}

	public class Watcher : IDisposable
	{
		private static readonly Comparer comparer = new Comparer(Comparer.Options.Path | Comparer.Options.Modifiers | Comparer.Options.Usages, new ModifiersListComparer(ProxyModifier.pathComparer));

		private bool m_Disposed;

		private ProxyBinding m_Binding;

		private readonly ProxyAction m_Action;

		private readonly Action<ProxyBinding> m_OnChange;

		public ProxyBinding binding => m_Binding;

		public bool isValid => m_Action != null;

		public Watcher(ProxyBinding binding, Action<ProxyBinding> onChange = null)
		{
			m_Binding = binding;
			m_Action = binding.action;
			m_OnChange = onChange;
			if (m_Action != null)
			{
				m_Action.onChanged += OnChanged;
				OnChanged(forceUpdate: true);
			}
		}

		private void OnChanged(ProxyAction action)
		{
			OnChanged(forceUpdate: false);
		}

		private void OnChanged(bool forceUpdate)
		{
			if (m_Action.TryGetBinding(m_Binding, out var foundBinding) && (forceUpdate || !comparer.Equals(m_Binding, foundBinding)))
			{
				m_Binding = foundBinding;
				m_OnChange?.Invoke(foundBinding);
			}
		}

		public void Dispose()
		{
			if (!m_Disposed)
			{
				m_Disposed = true;
				if (m_Action != null)
				{
					m_Action.onChanged -= OnChanged;
				}
			}
		}
	}

	[Flags]
	public enum ConflictType
	{
		None = 0,
		WithBuiltIn = 1,
		WithNotBuiltIn = 2,
		All = 3
	}

	public static readonly Comparer pathAndModifiersComparer = new Comparer(Comparer.Options.Path | Comparer.Options.Modifiers, new ModifiersListComparer(ProxyModifier.pathComparer));

	public static readonly Comparer onlyPathComparer = new Comparer(Comparer.Options.Path);

	internal static readonly Comparer componentComparer = new Comparer(Comparer.Options.Device | Comparer.Options.Component);

	[Include]
	private string m_MapName;

	[Include]
	private string m_ActionName;

	[Include]
	private ActionComponent m_Component;

	[Include]
	private string m_Name;

	[Include]
	[DecodeAlias(new string[] { "group", "m_Group" })]
	private InputManager.DeviceType m_Device;

	[Include]
	private string m_Path;

	[Include]
	[DiscardDefaultArrayExtraItems]
	private ProxyModifier[] m_Modifiers;

	[Exclude]
	private string m_OriginalPath;

	[Exclude]
	private ProxyModifier[] m_OriginalModifiers;

	[Exclude]
	private CompositeInstance m_Source;

	[Exclude]
	private UIBaseInputAction m_Alies;

	private int m_HasConflictVersion;

	private ConflictType m_HasConflicts;

	private int m_ConflictVersion;

	private IList<ProxyBinding> m_Conflicts;

	public static Comparer defaultComparer => Comparer.defaultComparer;

	public static ModifiersListComparer defaultModifiersComparer => ModifiersListComparer.defaultComparer;

	public string mapName => m_MapName;

	public string actionName => m_ActionName;

	public ActionComponent component => m_Component;

	public string name => m_Name;

	internal ProxyAction action => InputManager.instance.FindAction(m_MapName, m_ActionName);

	public bool isBuiltIn
	{
		get
		{
			if (m_Source != null)
			{
				return m_Source.builtIn;
			}
			return false;
		}
	}

	public bool isRebindable => rebindOptions != RebindOptions.None;

	public bool isKeyRebindable => (rebindOptions & RebindOptions.Key) != 0;

	public bool isModifiersRebindable => (rebindOptions & RebindOptions.Modifiers) != 0;

	public bool disallowModifiers => modifierOptions == ModifierOptions.Disallow;

	public bool allowModifiers => modifierOptions == ModifierOptions.Allow;

	public bool ignoreModifiers => modifierOptions == ModifierOptions.Ignore;

	public RebindOptions rebindOptions => m_Source?.rebindOptions ?? RebindOptions.None;

	public ModifierOptions modifierOptions => m_Source?.modifierOptions ?? ModifierOptions.Disallow;

	public bool canBeEmpty
	{
		get
		{
			if (m_Source != null)
			{
				return m_Source.canBeEmpty;
			}
			return false;
		}
	}

	public bool developerOnly
	{
		get
		{
			if (m_Source != null)
			{
				return m_Source.developerOnly;
			}
			return false;
		}
	}

	internal bool isDummy
	{
		get
		{
			if (m_Source != null)
			{
				return m_Source.isDummy;
			}
			return false;
		}
	}

	internal bool isHidden
	{
		get
		{
			if ((object)m_Alies != null)
			{
				return !m_Alies.showInOptions;
			}
			if (m_Source != null)
			{
				return m_Source.isHidden;
			}
			return false;
		}
	}

	internal OptionGroupOverride optionGroupOverride
	{
		get
		{
			OptionGroupOverride optionGroupOverride = ((m_Source != null) ? m_Source.optionGroupOverride : OptionGroupOverride.None);
			if ((object)m_Alies != null && optionGroupOverride == OptionGroupOverride.None)
			{
				optionGroupOverride = m_Alies.optionGroupOverride;
			}
			return optionGroupOverride;
		}
	}

	public Usages usages => m_Source?.usages ?? Usages.empty;

	public ProxyBinding original
	{
		get
		{
			ProxyBinding result = Copy();
			result.m_Path = m_OriginalPath ?? m_Path;
			result.m_Modifiers = m_OriginalModifiers ?? m_Modifiers;
			return result;
		}
	}

	public bool isOriginal
	{
		get
		{
			if (m_Path == m_OriginalPath)
			{
				return defaultModifiersComparer.Equals((IReadOnlyCollection<ProxyModifier>)(object)m_Modifiers, (IReadOnlyCollection<ProxyModifier>)(object)m_OriginalModifiers);
			}
			return false;
		}
	}

	public ConflictType hasConflicts
	{
		get
		{
			if (m_HasConflictVersion == InputManager.instance.actionVersion)
			{
				return m_HasConflicts;
			}
			m_HasConflictVersion = InputManager.instance.actionVersion;
			m_HasConflicts = ConflictType.None;
			if (!isSet)
			{
				return m_HasConflicts;
			}
			if (!InputManager.instance.keyActionMap.TryGetValue(path, out var value))
			{
				return m_HasConflicts;
			}
			if (!InputManager.instance.TryFindAction(m_MapName, m_ActionName, out var action))
			{
				return m_HasConflicts;
			}
			foreach (ProxyAction item in value)
			{
				foreach (var (_, proxyComposite2) in item.composites)
				{
					if (proxyComposite2.isDummy)
					{
						continue;
					}
					bool flag = InputManager.CanConflict(action, item, proxyComposite2.m_Device);
					foreach (var (_, y) in proxyComposite2.bindings)
					{
						if ((flag || !componentComparer.Equals(this, y)) && ConflictsWith(this, y, checkUsage: true))
						{
							m_HasConflicts |= (ConflictType)(item.isBuiltIn ? 1 : 2);
						}
					}
				}
			}
			return m_HasConflicts;
		}
	}

	public IList<ProxyBinding> conflicts
	{
		get
		{
			if (m_ConflictVersion == InputManager.instance.actionVersion)
			{
				return m_Conflicts;
			}
			m_ConflictVersion = InputManager.instance.actionVersion;
			m_Conflicts = Array.Empty<ProxyBinding>();
			if (!isSet)
			{
				return m_Conflicts;
			}
			if (!InputManager.instance.keyActionMap.TryGetValue(path, out var value))
			{
				return m_Conflicts;
			}
			if (!InputManager.instance.TryFindAction(m_MapName, m_ActionName, out var action))
			{
				return m_Conflicts;
			}
			m_Conflicts = new List<ProxyBinding>();
			foreach (ProxyAction item in value)
			{
				foreach (var (_, proxyComposite2) in item.composites)
				{
					if (proxyComposite2.isDummy)
					{
						continue;
					}
					bool flag = InputManager.CanConflict(action, item, proxyComposite2.m_Device);
					foreach (var (_, proxyBinding2) in proxyComposite2.bindings)
					{
						if ((flag || !componentComparer.Equals(this, proxyBinding2)) && ConflictsWith(this, proxyBinding2, checkUsage: true))
						{
							m_Conflicts.Add(proxyBinding2);
						}
					}
				}
			}
			return m_Conflicts;
		}
	}

	[Exclude]
	public string path
	{
		get
		{
			return m_Path;
		}
		set
		{
			m_Path = value;
			ResetConflictCache();
		}
	}

	[Exclude]
	public IReadOnlyList<ProxyModifier> modifiers
	{
		get
		{
			return m_Modifiers ?? (m_Modifiers = Array.Empty<ProxyModifier>());
		}
		set
		{
			SetModifiers(out m_Modifiers, value);
			ResetConflictCache();
		}
	}

	[Exclude]
	public string originalPath
	{
		get
		{
			return m_OriginalPath;
		}
		set
		{
			m_OriginalPath = value;
			ResetConflictCache();
		}
	}

	[Exclude]
	public IReadOnlyList<ProxyModifier> originalModifiers
	{
		get
		{
			ProxyModifier[] array = m_OriginalModifiers;
			if (array == null)
			{
				ProxyModifier[] obj = m_Modifiers ?? Array.Empty<ProxyModifier>();
				ProxyModifier[] array2 = obj;
				m_OriginalModifiers = obj;
				array = array2;
			}
			return array;
		}
		set
		{
			SetModifiers(out m_OriginalModifiers, value);
			ResetConflictCache();
		}
	}

	[Exclude]
	[Obsolete("Use device instead. It will be removed eventually")]
	public string group
	{
		get
		{
			return m_Device.ToString();
		}
		set
		{
			m_Device = value.ToDeviceType();
		}
	}

	[Exclude]
	public InputManager.DeviceType device
	{
		get
		{
			return m_Device;
		}
		set
		{
			m_Device = value;
		}
	}

	public bool isKeyboard => (device & InputManager.DeviceType.Keyboard) != 0;

	public bool isMouse => (device & InputManager.DeviceType.Mouse) != 0;

	public bool isGamepad => (device & InputManager.DeviceType.Gamepad) != 0;

	public bool isSet => !string.IsNullOrEmpty(m_Path);

	[Exclude]
	internal UIBaseInputAction alies
	{
		get
		{
			return m_Alies;
		}
		set
		{
			m_Alies = value;
		}
	}

	internal bool isAlias => (object)m_Alies != null;

	public string title
	{
		get
		{
			if (!isAlias)
			{
				return m_MapName + "/" + m_ActionName + "/" + m_Name?.ToLower();
			}
			return m_Alies.aliasName + "/" + m_Name?.ToLower();
		}
	}

	private static void SupportValueTypesForAOT()
	{
		JSON.SupportTypeForAOT<ProxyBinding>();
	}

	public ProxyBinding(string mapName, string actionName, ActionComponent component, string name, CompositeInstance source)
	{
		m_MapName = mapName;
		m_ActionName = actionName;
		m_Component = component;
		m_Name = name;
		m_Source = source;
		m_Device = InputManager.DeviceType.None;
		m_Path = string.Empty;
		m_Modifiers = Array.Empty<ProxyModifier>();
		m_OriginalPath = null;
		m_OriginalModifiers = null;
		m_Alies = null;
		m_HasConflicts = ConflictType.None;
		m_Conflicts = Array.Empty<ProxyBinding>();
		m_ConflictVersion = -1;
		m_HasConflictVersion = -1;
	}

	public ProxyBinding(InputAction action, ActionComponent component, string name, CompositeInstance source)
		: this(action.actionMap.name, action.name, component, name, source)
	{
	}

	public ProxyBinding Copy()
	{
		return new ProxyBinding
		{
			m_Source = m_Source,
			m_Component = m_Component,
			m_MapName = m_MapName,
			m_ActionName = m_ActionName,
			m_Name = m_Name,
			m_Device = m_Device,
			m_Path = m_Path,
			modifiers = modifiers,
			m_OriginalPath = m_OriginalPath,
			m_OriginalModifiers = m_OriginalModifiers,
			m_Alies = m_Alies,
			m_HasConflicts = m_HasConflicts,
			m_Conflicts = ((m_Conflicts.Count == 0) ? Array.Empty<ProxyBinding>() : m_Conflicts.ToArray()),
			m_ConflictVersion = m_ConflictVersion,
			m_HasConflictVersion = m_HasConflictVersion
		};
	}

	public static bool ConflictsWith(ProxyBinding x, ProxyBinding y, bool checkUsage)
	{
		if (!x.isSet || !y.isSet)
		{
			return false;
		}
		if (x.m_Device != y.m_Device)
		{
			return false;
		}
		if (!PathEquals(x, y))
		{
			return false;
		}
		if (checkUsage && !Usages.TestAny(x.usages, y.usages))
		{
			return false;
		}
		return true;
	}

	public IEnumerable<string> ToHumanReadablePath()
	{
		if (!string.IsNullOrEmpty(m_Path))
		{
			ProxyModifier[] array = m_Modifiers;
			for (int i = 0; i < array.Length; i++)
			{
				ProxyModifier proxyModifier = array[i];
				yield return ControlPath.ToHumanReadablePath(proxyModifier.m_Path);
			}
			yield return ControlPath.ToHumanReadablePath(m_Path);
		}
	}

	public ProxyBinding WithPath(string newPath)
	{
		path = newPath;
		return this;
	}

	public ProxyBinding WithModifiers(IReadOnlyList<ProxyModifier> newModifiers)
	{
		modifiers = newModifiers;
		return this;
	}

	internal Watcher CreateWatcher(Action<ProxyBinding> onChange = null)
	{
		return new Watcher(this, onChange);
	}

	private void SetModifiers(out ProxyModifier[] field, IReadOnlyList<ProxyModifier> value)
	{
		if (value == null || value.Count == 0)
		{
			field = Array.Empty<ProxyModifier>();
			return;
		}
		if (value.Count == 1)
		{
			field = value.ToArray();
			return;
		}
		field = value.Distinct(ProxyModifier.pathComparer).ToArray();
		Array.Sort(field, ProxyModifier.pathComparer);
	}

	public void ResetConflictCache()
	{
		m_ConflictVersion = -1;
		m_HasConflictVersion = -1;
	}

	internal string GetOptionsGroup()
	{
		if (optionGroupOverride == OptionGroupOverride.None)
		{
			return mapName;
		}
		FieldInfo field = typeof(OptionGroupOverride).GetField(optionGroupOverride.ToString());
		if (field == null)
		{
			return mapName;
		}
		DescriptionAttribute descriptionAttribute = field.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().FirstOrDefault();
		if (descriptionAttribute == null)
		{
			return mapName;
		}
		return descriptionAttribute.Description;
	}

	public override string ToString()
	{
		return string.Format("{0}/{1}/{2} - [{3}] ({4})", m_MapName, m_ActionName, m_Name, string.IsNullOrEmpty(m_Path) ? "Not set" : string.Join(" + ", m_Modifiers.Select((ProxyModifier m) => m.m_Path).Append(m_Path)), usages);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(ProxyBinding).FullName);
		writer.PropertyName("binding");
		writer.Write(ControlPath.Get(m_Path));
		writer.PropertyName("modifiers");
		writer.Write((IList<ControlPath>)m_Modifiers.Select((ProxyModifier m) => ControlPath.Get(m.m_Path)).ToArray());
		writer.PropertyName("name");
		writer.Write(m_Name);
		writer.PropertyName("map");
		writer.Write(m_MapName);
		writer.PropertyName("action");
		writer.Write(m_ActionName);
		writer.PropertyName("title");
		writer.Write(title);
		writer.PropertyName("optionGroup");
		writer.Write(GetOptionsGroup());
		writer.PropertyName("device");
		writer.Write(device.ToString());
		writer.PropertyName("isBuiltIn");
		writer.Write(isBuiltIn);
		writer.PropertyName("canBeEmpty");
		writer.Write(canBeEmpty);
		writer.PropertyName("rebindOptions");
		writer.Write((int)rebindOptions);
		writer.PropertyName("modifierOptions");
		writer.Write((int)modifierOptions);
		writer.PropertyName("isOriginal");
		writer.Write(isOriginal);
		writer.PropertyName("hasConflicts");
		writer.Write((int)hasConflicts);
		writer.TypeEnd();
	}

	public bool Equals(ProxyBinding other)
	{
		return Comparer.defaultComparer.Equals(this, other);
	}

	public override int GetHashCode()
	{
		return Comparer.defaultComparer.GetHashCode(this);
	}

	public static bool PathEquals(ProxyBinding x, ProxyBinding y)
	{
		return ((x.disallowModifiers || y.disallowModifiers) ? onlyPathComparer : pathAndModifiersComparer).Equals(x, y);
	}

	public override bool Equals(object obj)
	{
		if (obj is ProxyBinding y)
		{
			return Comparer.defaultComparer.Equals(this, y);
		}
		return false;
	}

	public static bool operator ==(ProxyBinding left, ProxyBinding right)
	{
		return Comparer.defaultComparer.Equals(left, right);
	}

	public static bool operator !=(ProxyBinding left, ProxyBinding right)
	{
		return !Comparer.defaultComparer.Equals(left, right);
	}
}
