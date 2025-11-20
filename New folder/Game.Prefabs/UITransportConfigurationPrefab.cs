using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class UITransportConfigurationPrefab : PrefabBase
{
	public InfoviewPrefab m_TransportInfoview;

	public InfomodePrefab m_RoutesInfomode;

	public PolicyPrefab m_TicketPricePolicy;

	public PolicyPrefab m_OutOfServicePolicy;

	public PolicyPrefab m_VehicleCountPolicy;

	public PolicyPrefab m_DayRoutePolicy;

	public PolicyPrefab m_NightRoutePolicy;

	public UITransportSummaryItem[] m_PassengerSummaryItems;

	public UITransportSummaryItem[] m_CargoSummaryItems;

	public UITransportItem[] m_PassengerLineTypes;

	public UITransportItem[] m_CargoLineTypes;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_TransportInfoview);
		prefabs.Add(m_TicketPricePolicy);
		prefabs.Add(m_OutOfServicePolicy);
		prefabs.Add(m_VehicleCountPolicy);
		prefabs.Add(m_DayRoutePolicy);
		prefabs.Add(m_NightRoutePolicy);
		for (int i = 0; i < m_PassengerLineTypes.Length; i++)
		{
			prefabs.Add(m_PassengerLineTypes[i].m_Unlockable);
		}
		for (int j = 0; j < m_CargoLineTypes.Length; j++)
		{
			prefabs.Add(m_CargoLineTypes[j].m_Unlockable);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UITransportConfigurationData>());
	}
}
