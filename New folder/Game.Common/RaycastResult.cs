using Colossal.Collections;
using Unity.Entities;

namespace Game.Common;

public struct RaycastResult : IAccumulable<RaycastResult>
{
	public RaycastHit m_Hit;

	public Entity m_Owner;

	public void Accumulate(RaycastResult other)
	{
		if (m_Owner == Entity.Null || (other.m_Owner != Entity.Null && (other.m_Hit.m_NormalizedDistance < m_Hit.m_NormalizedDistance || (other.m_Hit.m_NormalizedDistance == m_Hit.m_NormalizedDistance && other.m_Hit.m_HitEntity.Index < m_Hit.m_HitEntity.Index))))
		{
			this = other;
		}
	}
}
