using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathTargetMoved : IComponentData, IQueryTypeParameter
{
	public Entity m_Target;

	public float3 m_OldLocation;

	public float3 m_NewLocation;

	public PathTargetMoved(Entity target, float3 oldLocation, float3 newLocation)
	{
		m_Target = target;
		m_OldLocation = oldLocation;
		m_NewLocation = newLocation;
	}
}
