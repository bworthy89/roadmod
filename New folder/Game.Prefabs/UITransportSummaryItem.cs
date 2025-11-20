using System;
using Game.City;

namespace Game.Prefabs;

[Serializable]
public class UITransportSummaryItem : UITransportItem
{
	public StatisticType m_Statistic;

	public bool m_ShowLines = true;
}
