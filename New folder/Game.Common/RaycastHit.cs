using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Common;

public struct RaycastHit : IEquatable<RaycastHit>
{
	public Entity m_HitEntity;

	public float3 m_Position;

	public float3 m_HitPosition;

	public float3 m_HitDirection;

	public int2 m_CellIndex;

	public float m_NormalizedDistance;

	public float m_CurvePosition;

	public bool Equals(RaycastHit other)
	{
		if (m_HitEntity.Equals(other.m_HitEntity) && m_Position.Equals(other.m_Position) && m_HitPosition.Equals(other.m_HitPosition) && m_HitDirection.Equals(other.m_HitDirection) && m_CellIndex.Equals(other.m_CellIndex) && m_NormalizedDistance == other.m_NormalizedDistance)
		{
			return m_CurvePosition == other.m_CurvePosition;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((((17 * 31 + m_HitEntity.GetHashCode()) * 31 + m_Position.GetHashCode()) * 31 + m_HitPosition.GetHashCode()) * 31 + m_HitDirection.GetHashCode()) * 31 + m_CellIndex.GetHashCode()) * 31 + m_NormalizedDistance.GetHashCode()) * 31 + m_CurvePosition.GetHashCode();
	}
}
