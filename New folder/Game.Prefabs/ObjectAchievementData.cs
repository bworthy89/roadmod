using Colossal.PSI.Common;
using Unity.Entities;

namespace Game.Prefabs;

public struct ObjectAchievementData : IBufferElementData
{
	public AchievementId m_ID;

	public bool m_BypassCounter;

	public bool m_AbsoluteCounter;
}
