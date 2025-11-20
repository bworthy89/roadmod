using Game.City;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct UIStatisticsGroupData : IComponentData, IQueryTypeParameter
{
	public Entity m_Category;

	public Color m_Color;

	public StatisticUnitType m_UnitType;

	public bool m_Stacked;
}
