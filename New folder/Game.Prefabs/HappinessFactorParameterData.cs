using Unity.Entities;

namespace Game.Prefabs;

public struct HappinessFactorParameterData : IBufferElementData
{
	public int m_BaseLevel;

	public Entity m_LockedEntity;
}
