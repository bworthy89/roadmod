using Unity.Entities;

namespace Game.Prefabs;

public struct LimitSettingData : IComponentData, IQueryTypeParameter
{
	public int m_MaxChirpsLimit;
}
