using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Unity.Entities;

namespace Game.Rendering;

public struct CullingInfo : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Bounds3 m_Bounds;

	public float m_Radius;

	public int m_CullingIndex;

	public BoundsMask m_Mask;

	public byte m_MinLod;

	public byte m_PassedCulling;
}
