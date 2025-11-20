using Unity.Entities;
using Unity.Mathematics;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct WaypointDefinition : IBufferElementData
{
	public float3 m_Position;

	public Entity m_Connection;

	public Entity m_Original;

	public WaypointDefinition(float3 position)
	{
		m_Position = position;
		m_Connection = Entity.Null;
		m_Original = Entity.Null;
	}
}
