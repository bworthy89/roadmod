using Unity.Entities;

namespace Game.Prefabs;

public struct TelecomParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_TelecomServicePrefab;
}
