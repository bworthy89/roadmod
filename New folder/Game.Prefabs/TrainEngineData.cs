using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct TrainEngineData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public int2 m_Count;

	public TrainEngineData(int minCount, int maxCount)
	{
		m_Count = new int2(minCount, maxCount);
	}
}
