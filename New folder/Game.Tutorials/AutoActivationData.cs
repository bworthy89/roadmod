using Unity.Entities;

namespace Game.Tutorials;

public struct AutoActivationData : IComponentData, IQueryTypeParameter
{
	public Entity m_RequiredUnlock;
}
