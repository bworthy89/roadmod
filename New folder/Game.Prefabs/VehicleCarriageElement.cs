using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct VehicleCarriageElement : IBufferElementData
{
	public Entity m_Prefab;

	public int2 m_Count;

	public VehicleCarriageDirection m_Direction;

	public VehicleCarriageElement(Entity carriage, int minCount, int maxCount, VehicleCarriageDirection direction)
	{
		m_Prefab = carriage;
		m_Count = new int2(minCount, maxCount);
		m_Direction = direction;
	}
}
