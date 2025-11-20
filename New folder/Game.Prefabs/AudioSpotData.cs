using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AudioSpotData : IComponentData, IQueryTypeParameter
{
	public float2 m_Interval;
}
