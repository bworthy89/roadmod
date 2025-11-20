using Game.City;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct StatisticsData : IComponentData, IQueryTypeParameter
{
	public Entity m_Category;

	public Entity m_Group;

	public StatisticType m_StatisticType;

	public StatisticCollectionType m_CollectionType;

	public StatisticUnitType m_UnitType;

	public Color m_Color;

	public bool m_Stacked;
}
