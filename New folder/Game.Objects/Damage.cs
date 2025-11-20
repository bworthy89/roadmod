using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct Damage : IComponentData, IQueryTypeParameter
{
	public Entity m_Object;

	public float3 m_Delta;

	public Damage(Entity _object, float3 delta)
	{
		m_Object = _object;
		m_Delta = delta;
	}
}
