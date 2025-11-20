using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetPollutionData : IComponentData, IQueryTypeParameter
{
	public float2 m_Factors;
}
