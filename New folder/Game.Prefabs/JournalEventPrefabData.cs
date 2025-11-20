using Unity.Entities;

namespace Game.Prefabs;

public struct JournalEventPrefabData : IComponentData, IQueryTypeParameter
{
	public int m_DataFlags;

	public int m_EffectFlags;
}
