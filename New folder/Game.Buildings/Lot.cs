using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Buildings;

public struct Lot : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public float3 m_FrontHeights;

	public float3 m_RightHeights;

	public float3 m_BackHeights;

	public float3 m_LeftHeights;
}
