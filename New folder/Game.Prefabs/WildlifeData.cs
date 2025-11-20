using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct WildlifeData : IComponentData, IQueryTypeParameter
{
	public Bounds1 m_TripLength;

	public Bounds1 m_IdleTime;

	public int2 m_GroupMemberCount;
}
