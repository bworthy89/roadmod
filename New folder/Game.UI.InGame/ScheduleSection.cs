using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Routes;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ScheduleSection : InfoSectionBase
{
	private PoliciesUISystem m_PoliciesUISystem;

	private Entity m_NightRoutePolicy;

	private Entity m_DayRoutePolicy;

	private EntityQuery m_ConfigQuery;

	protected override string group => "ScheduleSection";

	private RouteSchedule schedule { get; set; }

	protected override void Reset()
	{
		schedule = RouteSchedule.DayAndNight;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		AddBinding(new TriggerBinding<int>(group, "setSchedule", OnSetSchedule));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (!m_ConfigQuery.IsEmptyIgnoreFilter)
		{
			UITransportConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_ConfigQuery);
			m_DayRoutePolicy = m_PrefabSystem.GetEntity(singletonPrefab.m_DayRoutePolicy);
			m_NightRoutePolicy = m_PrefabSystem.GetEntity(singletonPrefab.m_NightRoutePolicy);
		}
	}

	private void OnSetSchedule(int newSchedule)
	{
		switch ((RouteSchedule)newSchedule)
		{
		case RouteSchedule.Day:
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_NightRoutePolicy, active: false);
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_DayRoutePolicy, active: true);
			break;
		case RouteSchedule.Night:
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_NightRoutePolicy, active: true);
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_DayRoutePolicy, active: false);
			break;
		default:
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_NightRoutePolicy, active: false);
			m_PoliciesUISystem.SetPolicy(selectedEntity, m_DayRoutePolicy, active: false);
			break;
		}
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<TransportLine>(selectedEntity))
		{
			return base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Route componentData = base.EntityManager.GetComponentData<Route>(selectedEntity);
		schedule = ((!RouteUtils.CheckOption(componentData, RouteOption.Day)) ? (RouteUtils.CheckOption(componentData, RouteOption.Night) ? RouteSchedule.Night : RouteSchedule.DayAndNight) : RouteSchedule.Day);
		base.tooltipTags.Add("TransportLine");
		base.tooltipTags.Add("CargoRoute");
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("schedule");
		writer.Write((int)schedule);
	}

	[Preserve]
	public ScheduleSection()
	{
	}
}
