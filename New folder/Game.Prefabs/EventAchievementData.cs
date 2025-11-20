using Colossal.PSI.Common;
using Unity.Entities;

namespace Game.Prefabs;

public struct EventAchievementData : IBufferElementData
{
	public AchievementId m_ID;

	public uint m_FrameDelay;

	public bool m_BypassCounter;
}
