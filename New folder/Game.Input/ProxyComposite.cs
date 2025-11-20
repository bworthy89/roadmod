using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Game.Input;

[DebuggerDisplay("{m_Device}")]
public class ProxyComposite
{
	public struct Info
	{
		public InputManager.DeviceType m_Device;

		public CompositeInstance m_Source;

		public List<ProxyBinding> m_Bindings;
	}

	private readonly CompositeInstance m_Source;

	public readonly InputManager.DeviceType m_Device;

	public readonly ActionType m_Type;

	internal readonly HashSet<ProxyAction> m_LinkedActions = new HashSet<ProxyAction>();

	private readonly Dictionary<ActionComponent, ProxyBinding> m_Bindings = new Dictionary<ActionComponent, ProxyBinding>();

	public IReadOnlyDictionary<ActionComponent, ProxyBinding> bindings => m_Bindings;

	public bool isSet => m_Bindings.Any((KeyValuePair<ActionComponent, ProxyBinding> b) => b.Value.isSet);

	public bool isBuiltIn => m_Source.builtIn;

	internal bool isDummy => m_Source.isDummy;

	internal bool isHidden => m_Source.isHidden;

	public bool isKeyRebindable => m_Source.isKeyRebindable;

	public bool isModifiersRebindable => m_Source.isModifiersRebindable;

	public bool canBeEmpty => m_Source.canBeEmpty;

	public bool developerOnly => m_Source.developerOnly;

	public Usages usage => m_Source.usages;

	internal ProxyComposite(InputManager.DeviceType device, ActionType type, CompositeInstance source, IList<ProxyBinding> bindings)
	{
		m_Device = device;
		m_Type = type;
		m_Source = source;
		foreach (ProxyBinding binding in bindings)
		{
			m_Bindings[binding.component] = binding;
		}
	}

	public bool TryGetBinding(ProxyBinding sampleBinding, out ProxyBinding foundBinding)
	{
		return m_Bindings.TryGetValue(sampleBinding.component, out foundBinding);
	}

	public bool TryGetBinding(ActionComponent component, out ProxyBinding foundBinding)
	{
		return m_Bindings.TryGetValue(component, out foundBinding);
	}

	public override string ToString()
	{
		return $"{m_Device} ({m_Type})";
	}
}
