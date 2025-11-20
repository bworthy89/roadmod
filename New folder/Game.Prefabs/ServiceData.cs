using Game.City;
using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceData : IComponentData, IQueryTypeParameter
{
	public CityService m_Service;

	public bool m_BudgetAdjustable;
}
