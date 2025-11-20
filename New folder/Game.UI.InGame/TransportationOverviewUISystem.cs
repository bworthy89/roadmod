using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TransportationOverviewUISystem : UISystemBase
{
	private const string kGroup = "transportationOverview";

	private NameSystem m_NameSystem;

	private UnlockSystem m_UnlockSystem;

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private PoliciesUISystem m_PoliciesUISystem;

	private SelectedInfoUISystem m_SelectedInfoUISystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private Entity m_OutOfServicePolicy;

	private Entity m_DayRoutePolicy;

	private Entity m_NightRoutePolicy;

	private EntityQuery m_ConfigQuery;

	private EntityQuery m_LineQuery;

	private EntityQuery m_LinePrefabQuery;

	private EntityQuery m_ModifiedLineQuery;

	private EntityQuery m_UnlockQuery;

	private EntityArchetype m_ColorUpdateArchetype;

	private RawValueBinding m_TransportLines;

	private RawValueBinding m_PassengerTypes;

	private RawValueBinding m_CargoTypes;

	private ValueBinding<string> m_SelectedCargoType;

	private ValueBinding<string> m_SelectedPassengerType;

	private UITransportConfigurationPrefab m_Config;

	private UIUpdateState m_UpdateState;

	public override GameMode gameMode => GameMode.Game;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_UnlockSystem = base.World.GetOrCreateSystemManaged<UnlockSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_SelectedInfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		m_LinePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLineData>());
		m_LineQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadWrite<TransportLine>(),
				ComponentType.ReadOnly<RouteWaypoint>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ModifiedLineQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadWrite<TransportLine>(),
				ComponentType.ReadOnly<RouteWaypoint>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UnlockQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_ColorUpdateArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ColorUpdated>());
		AddBinding(m_TransportLines = new RawValueBinding("transportationOverview", "lines", BindLines));
		AddBinding(m_PassengerTypes = new RawValueBinding("transportationOverview", "passengerTypes", BindPassengerTypes));
		AddBinding(m_CargoTypes = new RawValueBinding("transportationOverview", "cargoTypes", BindCargoTypes));
		AddBinding(m_SelectedPassengerType = new ValueBinding<string>("transportationOverview", "selectedPassengerType", Enum.GetName(typeof(TransportType), TransportType.Bus)));
		AddBinding(m_SelectedCargoType = new ValueBinding<string>("transportationOverview", "selectedCargoType", "None"));
		AddBinding(new TriggerBinding<Entity>("transportationOverview", "delete", DeleteLine));
		AddBinding(new TriggerBinding<Entity>("transportationOverview", "select", SelectLine));
		AddBinding(new TriggerBinding<Entity, Color32>("transportationOverview", "setColor", SetLineColor));
		AddBinding(new TriggerBinding<Entity, string>("transportationOverview", "rename", SetLineName));
		AddBinding(new TriggerBinding<Entity, bool>("transportationOverview", "setActive", SetLineState));
		AddBinding(new TriggerBinding<Entity, bool>("transportationOverview", "showLine", ShowLine));
		AddBinding(new TriggerBinding<Entity, bool>("transportationOverview", "hideLine", HideLine));
		AddBinding(new TriggerBinding<Entity, int>("transportationOverview", "setSchedule", SetLineSchedule));
		AddBinding(new TriggerBinding("transportationOverview", "resetVisibility", ResetLinesVisibility));
		AddBinding(new TriggerBinding<Entity>("transportationOverview", "toggleHighlight", ToggleHighlight));
		AddBinding(new TriggerBinding<string>("transportationOverview", "setSelectedPassengerType", SetSelectedPassengerType));
		AddBinding(new TriggerBinding<string>("transportationOverview", "setSelectedCargoType", SetSelectedCargoType));
		m_UpdateState = UIUpdateState.Create(base.World, 256);
	}

	private string GetInitialSelectedType()
	{
		UITransportItem[] cargoLineTypes = m_Config.m_CargoLineTypes;
		foreach (UITransportItem uITransportItem in cargoLineTypes)
		{
			if (!m_UnlockSystem.IsLocked(uITransportItem.m_Unlockable))
			{
				return Enum.GetName(typeof(TransportType), uITransportItem.m_Type);
			}
		}
		return Enum.GetName(typeof(TransportType), TransportType.None);
	}

	private void SetSelectedPassengerType(string type)
	{
		m_SelectedPassengerType.Update(type);
	}

	private void SetSelectedCargoType(string type)
	{
		m_SelectedCargoType.Update(type);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (base.Enabled)
		{
			m_Config = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_ConfigQuery);
			m_OutOfServicePolicy = m_PrefabSystem.GetEntity(m_Config.m_OutOfServicePolicy);
			m_DayRoutePolicy = m_PrefabSystem.GetEntity(m_Config.m_DayRoutePolicy);
			m_NightRoutePolicy = m_PrefabSystem.GetEntity(m_Config.m_NightRoutePolicy);
			m_CargoTypes.Update();
			m_PassengerTypes.Update();
			m_TransportLines.Update();
			m_SelectedCargoType.Update(GetInitialSelectedType());
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ModifiedLineQuery.IsEmptyIgnoreFilter || m_UpdateState.Advance())
		{
			m_TransportLines.Update();
		}
		if (PrefabUtils.HasUnlockedPrefab<RouteData>(base.EntityManager, m_UnlockQuery))
		{
			m_CargoTypes.Update();
			m_PassengerTypes.Update();
		}
	}

	public void RequestUpdate()
	{
		m_UpdateState.ForceUpdate();
	}

	private void DeleteLine(Entity entity)
	{
		if (base.EntityManager.Exists(entity))
		{
			m_EndFrameBarrier.CreateCommandBuffer().AddComponent(entity, default(Deleted));
		}
	}

	private void SelectLine(Entity entity)
	{
		if (base.EntityManager.Exists(entity))
		{
			m_SelectedInfoUISystem.SetSelection(entity);
		}
	}

	private void SetLineColor(Entity entity, Color32 color)
	{
		if (!base.EntityManager.Exists(entity))
		{
			return;
		}
		EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
		entityCommandBuffer.SetComponent(entity, new Game.Routes.Color(color));
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RouteVehicle> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				entityCommandBuffer.AddComponent(buffer[i].m_Vehicle, new Game.Routes.Color(color));
			}
		}
		Entity e = entityCommandBuffer.CreateEntity(m_ColorUpdateArchetype);
		entityCommandBuffer.SetComponent(e, new ColorUpdated(entity));
		RequestUpdate();
	}

	private void SetLineName(Entity entity, string name)
	{
		if (base.EntityManager.Exists(entity))
		{
			m_NameSystem.SetCustomName(entity, name);
			RequestUpdate();
		}
	}

	public void SetLineState(Entity entity, bool state)
	{
		if (base.EntityManager.Exists(entity))
		{
			m_PoliciesUISystem.SetPolicy(entity, m_OutOfServicePolicy, !state);
			RequestUpdate();
		}
	}

	private void SetLineSchedule(Entity entity, int schedule)
	{
		if (base.EntityManager.Exists(entity))
		{
			switch ((RouteSchedule)schedule)
			{
			case RouteSchedule.Day:
				m_PoliciesUISystem.SetPolicy(entity, m_NightRoutePolicy, active: false);
				m_PoliciesUISystem.SetPolicy(entity, m_DayRoutePolicy, active: true);
				break;
			case RouteSchedule.Night:
				m_PoliciesUISystem.SetPolicy(entity, m_NightRoutePolicy, active: true);
				m_PoliciesUISystem.SetPolicy(entity, m_DayRoutePolicy, active: false);
				break;
			default:
				m_PoliciesUISystem.SetPolicy(entity, m_NightRoutePolicy, active: false);
				m_PoliciesUISystem.SetPolicy(entity, m_DayRoutePolicy, active: false);
				break;
			}
			RequestUpdate();
		}
	}

	public void ShowLine(Entity entity, bool hideOthers)
	{
		if (base.EntityManager.Exists(entity))
		{
			EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
			if (hideOthers)
			{
				entityCommandBuffer.AddComponent<HiddenRoute>(m_LineQuery, EntityQueryCaptureMode.AtPlayback);
			}
			entityCommandBuffer.RemoveComponent<HiddenRoute>(entity);
			RequestUpdate();
		}
	}

	public void HideLine(Entity entity, bool showOthers)
	{
		if (base.EntityManager.Exists(entity))
		{
			EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
			if (showOthers)
			{
				entityCommandBuffer.RemoveComponent<HiddenRoute>(m_LineQuery, EntityQueryCaptureMode.AtPlayback);
			}
			entityCommandBuffer.AddComponent<HiddenRoute>(entity);
			RequestUpdate();
		}
	}

	public void ToggleHighlight(Entity entity)
	{
		if (base.EntityManager.Exists(entity))
		{
			EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
			if (!base.EntityManager.HasComponent<Highlighted>(entity))
			{
				entityCommandBuffer.AddComponent<Highlighted>(entity);
			}
			else
			{
				entityCommandBuffer.RemoveComponent<Highlighted>(entity);
			}
		}
	}

	public void ResetLinesVisibility()
	{
		EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
		entityCommandBuffer.RemoveComponent<HiddenRoute>(m_LineQuery, EntityQueryCaptureMode.AtPlayback);
		entityCommandBuffer.RemoveComponent<Highlighted>(m_LineQuery, EntityQueryCaptureMode.AtPlayback);
		RequestUpdate();
	}

	private void BindPassengerTypes(IJsonWriter writer)
	{
		BindTypes(writer, m_Config.m_PassengerLineTypes);
	}

	private void BindCargoTypes(IJsonWriter writer)
	{
		BindTypes(writer, m_Config.m_CargoLineTypes);
	}

	private void BindTypes(IJsonWriter writer, UITransportItem[] items)
	{
		NativeArray<Entity> lineDatas = m_LinePrefabQuery.ToEntityArray(Allocator.Temp);
		int size = items.Count((UITransportItem item) => TransportUIUtils.ShouldBindTransportType(base.EntityManager, m_PrefabSystem, item.m_Type, lineDatas));
		writer.ArrayBegin(size);
		foreach (UITransportItem uITransportItem in items)
		{
			if (TransportUIUtils.ShouldBindTransportType(base.EntityManager, m_PrefabSystem, uITransportItem.m_Type, lineDatas))
			{
				new UITransportType(m_PrefabSystem.GetEntity(uITransportItem.m_Unlockable), Enum.GetName(typeof(TransportType), uITransportItem.m_Type), uITransportItem.m_Icon, m_UnlockSystem.IsLocked(uITransportItem.m_Unlockable)).Write(m_PrefabUISystem, writer);
			}
		}
		writer.ArrayEnd();
		lineDatas.Dispose();
	}

	private void BindLines(IJsonWriter binder)
	{
		NativeArray<UITransportLineData> sortedLines = TransportUIUtils.GetSortedLines(m_LineQuery, base.EntityManager, m_PrefabSystem);
		binder.ArrayBegin(sortedLines.Length);
		for (int i = 0; i < sortedLines.Length; i++)
		{
			BindLine(sortedLines[i], binder);
		}
		binder.ArrayEnd();
	}

	private void BindLine(UITransportLineData lineData, IJsonWriter binder)
	{
		binder.TypeBegin("Game.UI.InGame.UITransportLine");
		binder.PropertyName("name");
		m_NameSystem.BindName(binder, lineData.entity);
		binder.PropertyName("vkName");
		m_NameSystem.BindNameForVirtualKeyboard(binder, lineData.entity);
		binder.PropertyName("lineData");
		binder.Write(lineData);
		binder.TypeEnd();
	}

	[Preserve]
	public TransportationOverviewUISystem()
	{
	}
}
