using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceChirpData : IComponentData, IQueryTypeParameter
{
	public Entity m_Account;
}
