using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetPieceLane : IBufferElementData, IComparable<NetPieceLane>
{
	public Entity m_Lane;

	public float3 m_Position;

	public LaneFlags m_ExtraFlags;

	public int CompareTo(NetPieceLane other)
	{
		return math.select(0, math.select(-1, 1, m_Position.x > other.m_Position.x), m_Position.x != other.m_Position.x);
	}
}
