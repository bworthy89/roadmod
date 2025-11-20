using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetPieceArea : IBufferElementData, IComparable<NetPieceArea>
{
	public NetAreaFlags m_Flags;

	public float3 m_Position;

	public float m_Width;

	public float3 m_SnapPosition;

	public float m_SnapWidth;

	public int CompareTo(NetPieceArea other)
	{
		return math.select(0, math.select(-1, 1, m_Position.x > other.m_Position.x), m_Position.x != other.m_Position.x);
	}
}
