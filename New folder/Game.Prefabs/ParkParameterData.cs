using Unity.Entities;

namespace Game.Prefabs;

public struct ParkParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_ParkServicePrefab;
}
