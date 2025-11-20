using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Areas;

[InternalBufferCapacity(0)]
public struct LabelVertex : IBufferElementData, IEmptySerializable
{
	public float3 m_Position;

	public float2 m_UV0;

	public float2 m_UV1;

	public Color32 m_Color;

	public int m_Material;
}
