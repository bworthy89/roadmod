using Unity.Entities;

namespace Game.Tutorials;

public struct UIActivationData : IComponentData, IQueryTypeParameter
{
	public bool m_CanDeactivate;

	public UIActivationData(bool canDeactivate)
	{
		m_CanDeactivate = canDeactivate;
	}
}
