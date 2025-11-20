using System;
using System.Collections.Generic;
using Colossal;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

public class CompositeInstance : ICustomComposite
{
	private RebindOptions m_RebindOptions = RebindOptions.All;

	private ModifierOptions m_ModifierOptions = ModifierOptions.Allow;

	private bool m_CanBeEmpty = true;

	private bool m_DeveloperOnly;

	private Platform m_Platform = Platform.All;

	private bool m_BuiltIn = true;

	private bool m_IsDummy;

	private bool m_IsHidden;

	public OptionGroupOverride m_OptionGroupOverride;

	private Usages m_Usages = ICustomComposite.defaultUsages;

	public string typeName { get; internal set; }

	public bool isKeyRebindable
	{
		get
		{
			return (rebindOptions & RebindOptions.Key) != 0;
		}
		set
		{
			rebindOptions = (value ? (rebindOptions | RebindOptions.Key) : (rebindOptions & ~RebindOptions.Key));
		}
	}

	public bool isModifiersRebindable
	{
		get
		{
			return (rebindOptions & RebindOptions.Modifiers) != 0;
		}
		set
		{
			rebindOptions = (value ? (rebindOptions | RebindOptions.Modifiers) : (rebindOptions & ~RebindOptions.Modifiers));
		}
	}

	public RebindOptions rebindOptions
	{
		get
		{
			if (isDummy)
			{
				return RebindOptions.All;
			}
			return m_RebindOptions;
		}
		set
		{
			m_RebindOptions = value;
		}
	}

	public ModifierOptions modifierOptions
	{
		get
		{
			if (isDummy)
			{
				return ModifierOptions.Allow;
			}
			return m_ModifierOptions;
		}
		set
		{
			m_ModifierOptions = value;
		}
	}

	public bool canBeEmpty
	{
		get
		{
			if (isDummy)
			{
				return true;
			}
			return m_CanBeEmpty;
		}
		set
		{
			m_CanBeEmpty = value;
		}
	}

	public bool developerOnly
	{
		get
		{
			return m_DeveloperOnly;
		}
		set
		{
			m_DeveloperOnly = value;
		}
	}

	public Platform platform
	{
		get
		{
			return m_Platform;
		}
		set
		{
			m_Platform = value;
		}
	}

	public bool builtIn
	{
		get
		{
			return m_BuiltIn;
		}
		set
		{
			m_BuiltIn = value;
		}
	}

	public bool isDummy
	{
		get
		{
			return m_IsDummy;
		}
		set
		{
			m_IsDummy = value;
		}
	}

	public bool isHidden
	{
		get
		{
			return m_IsHidden;
		}
		set
		{
			m_IsHidden = value;
		}
	}

	public OptionGroupOverride optionGroupOverride
	{
		get
		{
			if (isHidden)
			{
				return OptionGroupOverride.None;
			}
			return m_OptionGroupOverride;
		}
		set
		{
			m_OptionGroupOverride = value;
		}
	}

	public Usages usages
	{
		get
		{
			return m_Usages.Copy();
		}
		set
		{
			m_Usages.SetFrom(value);
		}
	}

	public Guid linkedGuid { get; set; }

	public List<NameAndParameters> processors { get; } = new List<NameAndParameters>();

	public List<NameAndParameters> interactions { get; } = new List<NameAndParameters>();

	public Mode mode { get; set; }

	public InputManager.CompositeData compositeData
	{
		get
		{
			if (!InputManager.TryGetCompositeData(typeName, out var data))
			{
				return default(InputManager.CompositeData);
			}
			return data;
		}
	}

	public NameAndParameters parameters
	{
		get
		{
			CompositeUtility.SetGuid(linkedGuid, out var part, out var part2);
			return new NameAndParameters
			{
				name = typeName,
				parameters = new ReadOnlyArray<NamedValue>(new NamedValue[12]
				{
					NamedValue.From("m_RebindOptions", rebindOptions),
					NamedValue.From("m_ModifierOptions", modifierOptions),
					NamedValue.From("m_CanBeEmpty", canBeEmpty),
					NamedValue.From("m_DeveloperOnly", developerOnly),
					NamedValue.From("m_Platform", platform),
					NamedValue.From("m_BuiltIn", builtIn),
					NamedValue.From("m_Mode", mode),
					NamedValue.From("m_IsDummy", isDummy),
					NamedValue.From("m_IsHidden", isHidden),
					NamedValue.From("m_OptionGroupOverride", optionGroupOverride),
					NamedValue.From("m_LinkGuid1", part),
					NamedValue.From("m_LinkGuid2", part2)
				})
			};
		}
		set
		{
			long part = 0L;
			long part2 = 0L;
			foreach (NamedValue parameter in value.parameters)
			{
				switch (parameter.name)
				{
				case "m_RebindOptions":
					rebindOptions = (RebindOptions)parameter.value.ToInt32();
					break;
				case "m_ModifierOptions":
					modifierOptions = (ModifierOptions)parameter.value.ToInt32();
					break;
				case "m_CanBeEmpty":
					canBeEmpty = parameter.value.ToBoolean();
					break;
				case "m_DeveloperOnly":
					developerOnly = parameter.value.ToBoolean();
					break;
				case "m_Platform":
					platform = (Platform)parameter.value.ToInt32();
					break;
				case "m_BuiltIn":
					builtIn = parameter.value.ToBoolean();
					break;
				case "m_Mode":
					mode = (Mode)parameter.value.ToInt32();
					break;
				case "m_Usages":
					m_Usages = new Usages((BuiltInUsages)parameter.value.ToInt32());
					break;
				case "m_IsDummy":
					isDummy = parameter.value.ToBoolean();
					break;
				case "m_IsHidden":
					isHidden = parameter.value.ToBoolean();
					break;
				case "m_OptionGroupOverride":
					optionGroupOverride = (OptionGroupOverride)parameter.value.ToInt32();
					break;
				case "m_LinkGuid1":
					part = parameter.value.ToInt64();
					break;
				case "m_LinkGuid2":
					part2 = parameter.value.ToInt64();
					break;
				}
			}
			linkedGuid = CompositeUtility.GetGuid(part, part2);
		}
	}

	public CompositeInstance(string typeName)
	{
		this.typeName = typeName;
	}

	public CompositeInstance(NameAndParameters parameters)
		: this(parameters.name)
	{
		m_Usages = ICustomComposite.defaultUsages;
		this.parameters = parameters;
		m_Usages.MakeReadOnly();
	}

	public CompositeInstance(NameAndParameters parameters, NameAndParameters usages)
		: this(parameters.name)
	{
		m_Usages = new Usages(0, readOnly: false);
		this.parameters = parameters;
		m_Usages.parameters = usages;
		m_Usages.MakeReadOnly();
	}
}
