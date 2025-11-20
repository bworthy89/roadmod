using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(9)]
public struct MapFeatureData : IBufferElementData
{
	public float m_Cost;

	public MapFeatureData(float cost)
	{
		m_Cost = cost;
	}
}
