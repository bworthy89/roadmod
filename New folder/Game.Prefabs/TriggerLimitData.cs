using Unity.Entities;

namespace Game.Prefabs;

public struct TriggerLimitData : IComponentData, IQueryTypeParameter
{
	public uint m_FrameInterval;
}
