using System;
using Colossal;
using Colossal.Collections;
using Colossal.Mathematics;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Common;

public struct QuadTreeBoundsXZ : IEquatable<QuadTreeBoundsXZ>, IBounds2<QuadTreeBoundsXZ>
{
	public struct DebugIterator<TItem> : INativeQuadTreeIterator<TItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<TItem, QuadTreeBoundsXZ> where TItem : unmanaged, IEquatable<TItem>
	{
		private Bounds3 m_Bounds;

		private GizmoBatcher m_GizmoBatcher;

		public DebugIterator(Bounds3 bounds, GizmoBatcher gizmoBatcher)
		{
			m_Bounds = bounds;
			m_GizmoBatcher = gizmoBatcher;
		}

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				return false;
			}
			float3 center = MathUtils.Center(bounds.m_Bounds);
			float3 size = MathUtils.Size(bounds.m_Bounds);
			m_GizmoBatcher.DrawWireCube(center, size, Color.white);
			return true;
		}

		public void Iterate(QuadTreeBoundsXZ bounds, TItem edgeEntity)
		{
			if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				float3 center = MathUtils.Center(bounds.m_Bounds);
				float3 size = MathUtils.Size(bounds.m_Bounds);
				m_GizmoBatcher.DrawWireCube(center, size, Color.red);
			}
		}
	}

	public Bounds3 m_Bounds;

	public BoundsMask m_Mask;

	public byte m_MinLod;

	public byte m_MaxLod;

	public QuadTreeBoundsXZ(Bounds3 bounds)
	{
		m_Bounds = bounds;
		m_Mask = BoundsMask.Debug | BoundsMask.NormalLayers | BoundsMask.NotOverridden | BoundsMask.NotWalkThrough;
		m_MinLod = 1;
		m_MaxLod = 1;
	}

	public QuadTreeBoundsXZ(Bounds3 bounds, BoundsMask mask, int lod)
	{
		m_Bounds = bounds;
		m_Mask = mask;
		m_MinLod = (byte)lod;
		m_MaxLod = (byte)lod;
	}

	public QuadTreeBoundsXZ(Bounds3 bounds, BoundsMask mask, int minLod, int maxLod)
	{
		m_Bounds = bounds;
		m_Mask = mask;
		m_MinLod = (byte)minLod;
		m_MaxLod = (byte)maxLod;
	}

	public bool Equals(QuadTreeBoundsXZ other)
	{
		return m_Bounds.Equals(other.m_Bounds) & (m_Mask == other.m_Mask) & m_MinLod.Equals(other.m_MinLod) & m_MaxLod.Equals(other.m_MaxLod);
	}

	public void Reset()
	{
		m_Bounds.min = float.MaxValue;
		m_Bounds.max = float.MinValue;
		m_Mask = (BoundsMask)0;
		m_MinLod = byte.MaxValue;
		m_MaxLod = 0;
	}

	public float2 Center()
	{
		return MathUtils.Center(m_Bounds).xz;
	}

	public float2 Size()
	{
		return MathUtils.Size(m_Bounds).xz;
	}

	public QuadTreeBoundsXZ Merge(QuadTreeBoundsXZ other)
	{
		return new QuadTreeBoundsXZ(m_Bounds | other.m_Bounds, m_Mask | other.m_Mask, math.min((int)m_MinLod, (int)other.m_MinLod), math.max((int)m_MaxLod, (int)other.m_MaxLod));
	}

	public bool Intersect(QuadTreeBoundsXZ other)
	{
		return MathUtils.Intersect(m_Bounds, other.m_Bounds);
	}
}
