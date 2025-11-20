using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct AvailabilityResult
{
	public Entity m_Target;

	public float2 m_Availability;
}
