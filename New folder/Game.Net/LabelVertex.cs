using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct LabelVertex : IBufferElementData, IEmptySerializable
{
	public float3 m_Position;

	public float2 m_UV0;

	public float2 m_UV1;

	public int2 m_Material;

	public Color32 m_Color;
}
