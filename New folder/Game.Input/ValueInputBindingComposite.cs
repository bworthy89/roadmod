using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

public abstract class ValueInputBindingComposite<T> : InputBindingComposite<T>, ICustomComposite where T : struct
{
	public RebindOptions m_RebindOptions = RebindOptions.All;

	public ModifierOptions m_ModifierOptions = ModifierOptions.Allow;

	public bool m_CanBeEmpty = true;

	public bool m_DeveloperOnly;

	public Platform m_Platform = Platform.All;

	public bool m_BuiltIn = true;

	public bool m_IsDummy;

	public bool m_IsHidden;

	public OptionGroupOverride m_OptionGroupOverride;

	public BuiltInUsages m_Usages = BuiltInUsages.DefaultTool | BuiltInUsages.Overlay | BuiltInUsages.Tool | BuiltInUsages.CancelableTool;

	public long m_LinkGuid1;

	public long m_LinkGuid2;

	public bool isRebindable => (m_RebindOptions & RebindOptions.Key) != 0;

	public bool isModifiersRebindable => (m_RebindOptions & RebindOptions.Modifiers) != 0;

	public bool allowModifiers => m_ModifierOptions == ModifierOptions.Allow;

	public RebindOptions rebindOptions => m_RebindOptions;

	public ModifierOptions modifierOptions => m_ModifierOptions;

	public bool canBeEmpty => m_CanBeEmpty;

	public bool developerOnly => m_DeveloperOnly;

	public Platform platform => m_Platform;

	public bool builtIn => m_BuiltIn;

	public bool isDummy => m_IsDummy;

	public bool isHidden => m_IsHidden;

	public OptionGroupOverride optionGroupOverride => m_OptionGroupOverride;

	public Guid linkedGuid
	{
		get
		{
			return CompositeUtility.GetGuid(m_LinkGuid1, m_LinkGuid2);
		}
		set
		{
			CompositeUtility.SetGuid(value, out m_LinkGuid1, out m_LinkGuid2);
		}
	}

	public virtual NameAndParameters parameters => new NameAndParameters
	{
		name = CompositeUtility.GetCompositeTypeName(GetType()),
		parameters = new ReadOnlyArray<NamedValue>(GetParameters().ToArray())
	};

	public Usages usages => new Usages(m_Usages);

	protected virtual IEnumerable<NamedValue> GetParameters()
	{
		yield return NamedValue.From("m_RebindOptions", m_RebindOptions);
		yield return NamedValue.From("m_ModifierOptions", m_ModifierOptions);
		yield return NamedValue.From("m_CanBeEmpty", m_CanBeEmpty);
		yield return NamedValue.From("m_DeveloperOnly", m_DeveloperOnly);
		yield return NamedValue.From("m_BuiltIn", m_BuiltIn);
		yield return NamedValue.From("m_IsDummy", m_IsDummy);
		yield return NamedValue.From("m_IsHidden", m_IsHidden);
		yield return NamedValue.From("m_OptionGroupOverride", m_OptionGroupOverride);
	}
}
