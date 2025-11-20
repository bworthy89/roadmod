using System;
using Unity.Entities;

namespace Game.Effects;

public struct SourceInfo : IEquatable<SourceInfo>
{
	public Entity m_Entity;

	public int m_EffectIndex;

	public SourceInfo(Entity entity, int effectIndex)
	{
		m_Entity = entity;
		m_EffectIndex = effectIndex;
	}

	public bool Equals(SourceInfo other)
	{
		if (m_Entity == other.m_Entity)
		{
			return m_EffectIndex == other.m_EffectIndex;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Entity.GetHashCode() ^ m_EffectIndex;
	}
}
