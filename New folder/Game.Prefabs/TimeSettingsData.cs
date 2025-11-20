using Unity.Entities;

namespace Game.Prefabs;

public struct TimeSettingsData : IComponentData, IQueryTypeParameter
{
	public int m_DaysPerYear;
}
