using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Areas;

public struct Geometry : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Bounds3 m_Bounds;

	public float3 m_CenterPosition;

	public float m_SurfaceArea;
}
