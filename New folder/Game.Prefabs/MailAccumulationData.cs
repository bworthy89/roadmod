using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct MailAccumulationData : IComponentData, IQueryTypeParameter
{
	public bool m_RequireCollect;

	public float2 m_AccumulationRate;
}
