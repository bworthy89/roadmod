using System;

namespace Game.Prefabs;

[Serializable]
public struct EffectCondition
{
	public EffectConditionFlags m_RequiredFlags;

	public EffectConditionFlags m_ForbiddenFlags;

	public EffectConditionFlags m_IntensityFlags;
}
