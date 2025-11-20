using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct CraneData : IComponentData, IQueryTypeParameter
{
	public Bounds1 m_DistanceRange;
}
