using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct GateData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_BypassPathPrefab;
}
