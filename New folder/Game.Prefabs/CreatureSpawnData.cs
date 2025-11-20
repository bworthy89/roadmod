using Unity.Entities;

namespace Game.Prefabs;

public struct CreatureSpawnData : IComponentData, IQueryTypeParameter
{
	public int m_MaxGroupCount;
}
