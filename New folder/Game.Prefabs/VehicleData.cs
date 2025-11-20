using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct VehicleData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public int m_SteeringBoneIndex;
}
