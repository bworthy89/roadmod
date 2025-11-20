using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct CargoTransportStationData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public float m_WorkMultiplier;
}
