using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ActionsSection : InfoSectionBase
{
	private ToolSystem m_ToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private UIInitializeSystem m_UIInitializeSystem;

	private PoliciesUISystem m_PoliciesUISystem;

	private LifePathEventSystem m_LifePathEventSystem;

	private GamePanelUISystem m_GamePanelUISystem;

	private TrafficRoutesSystem m_TrafficRoutesSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_RouteConfigQuery;

	private PolicyPrefab m_RouteOutOfServicePolicy;

	private PolicyPrefab m_BuildingOutOfServicePolicy;

	private PolicyPrefab m_EmptyingPolicy;

	private AreaPrefab m_LotPrefab;

	private bool m_EditingLot;

	private Color32[] m_TrafficRouteColors;

	private ValueBinding<bool> m_MovingBinding;

	private ValueBinding<bool> m_EditingLotBinding;

	private ValueBinding<bool> m_TrafficRoutesVisibleBinding;

	private ValueBinding<Color32[]> m_TrafficRouteColorsBinding;

	private RawValueBinding m_MoveableObjectName;

	protected override string group => "ActionsSection";

	public bool editingLot => m_EditingLot;

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForUpgrades => true;

	private bool focusable { get; set; }

	private bool focusing { get; set; }

	private bool following { get; set; }

	private bool followable { get; set; }

	private bool moveable { get; set; }

	private bool deletable { get; set; }

	private bool disabled { get; set; }

	private bool disableable { get; set; }

	private bool hasTutorial { get; set; }

	private bool emptying { get; set; }

	private bool emptiable { get; set; }

	private bool hasLotTool { get; set; }

	private bool hasTrafficRoutes { get; set; }

	protected override void Reset()
	{
		focusable = false;
		focusing = false;
		following = false;
		followable = false;
		moveable = false;
		deletable = false;
		disabled = false;
		disableable = false;
		hasTutorial = false;
		emptying = false;
		emptiable = false;
		hasLotTool = false;
		hasTrafficRoutes = false;
		m_LotPrefab = null;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_UIInitializeSystem = base.World.GetOrCreateSystemManaged<UIInitializeSystem>();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_LifePathEventSystem = base.World.GetOrCreateSystemManaged<LifePathEventSystem>();
		m_GamePanelUISystem = base.World.GetOrCreateSystemManaged<GamePanelUISystem>();
		m_TrafficRoutesSystem = base.World.GetOrCreateSystemManaged<TrafficRoutesSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_RouteConfigQuery = GetEntityQuery(ComponentType.ReadOnly<RouteConfigurationData>());
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		AddBinding(new TriggerBinding(group, "focus", OnFocus));
		AddBinding(new TriggerBinding(group, "toggleMove", OnToggleMove));
		AddBinding(new TriggerBinding(group, "follow", OnFollow));
		AddBinding(new TriggerBinding(group, "delete", OnDelete));
		AddBinding(new TriggerBinding(group, "toggle", OnToggle));
		AddBinding(new TriggerBinding(group, "toggleEmptying", OnToggleEmptying));
		AddBinding(new TriggerBinding(group, "toggleLotTool", OnToggleLotTool));
		AddBinding(new TriggerBinding(group, "toggleTrafficRoutes", OnToggleTrafficRoutes));
		AddBinding(m_MovingBinding = new ValueBinding<bool>(group, "moving", initialValue: false));
		AddBinding(m_EditingLotBinding = new ValueBinding<bool>(group, "editingLot", initialValue: false));
		AddBinding(m_MoveableObjectName = new RawValueBinding(group, "moveableObjectName", BindObjectName));
		AddBinding(m_TrafficRouteColorsBinding = new ValueBinding<Color32[]>(group, "trafficRouteColors", m_TrafficRouteColors, new ArrayWriter<Color32>()));
		AddBinding(m_TrafficRoutesVisibleBinding = new ValueBinding<bool>(group, "trafficRoutesVisible", m_TrafficRoutesSystem.routesVisible));
	}

	private void OnToggleTrafficRoutes()
	{
		m_TrafficRoutesSystem.routesVisible = !m_TrafficRoutesSystem.routesVisible;
		m_InfoUISystem.SetDirty();
	}

	private void BindObjectName(IJsonWriter binder)
	{
		if (m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Move)
		{
			m_NameSystem.BindName(binder, m_InfoUISystem.selectedEntity);
		}
		else
		{
			binder.WriteNull();
		}
	}

	private void OnToolChanged(ToolBaseSystem tool)
	{
		if (tool != m_AreaToolSystem)
		{
			m_EditingLot = false;
		}
		m_MoveableObjectName.Update();
		m_MovingBinding.Update(tool == m_ObjectToolSystem && m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Move);
		m_EditingLotBinding.Update(tool == m_AreaToolSystem && hasLotTool && m_EditingLot);
	}

	private void OnToggleLotTool()
	{
		if (m_EditingLot)
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
		}
		else if (m_LotPrefab != null)
		{
			m_EditingLot = true;
			m_AreaToolSystem.prefab = m_LotPrefab;
			m_ToolSystem.activeTool = m_AreaToolSystem;
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		foreach (PolicyPrefab policy in m_UIInitializeSystem.policies)
		{
			switch (policy.name)
			{
			case "Route Out of Service":
				m_RouteOutOfServicePolicy = policy;
				break;
			case "Out of Service":
				m_BuildingOutOfServicePolicy = policy;
				break;
			case "Empty":
				m_EmptyingPolicy = policy;
				break;
			}
		}
	}

	private void OnToggle()
	{
		if (base.EntityManager.HasComponent<Route>(selectedEntity) && (base.EntityManager.HasComponent<TransportLine>(selectedEntity) || base.EntityManager.HasComponent<WorkRoute>(selectedEntity)) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity))
		{
			m_PoliciesUISystem.SetSelectedInfoPolicy(m_PrefabSystem.GetEntity(m_RouteOutOfServicePolicy), !disabled);
		}
		else if ((base.EntityManager.HasComponent<Building>(selectedEntity) && base.EntityManager.HasComponent<Policy>(selectedEntity) && base.EntityManager.HasComponent<CityServiceUpkeep>(selectedEntity) && base.EntityManager.HasComponent<Efficiency>(selectedEntity)) || base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity))
		{
			m_PoliciesUISystem.SetSelectedInfoPolicy(m_PrefabSystem.GetEntity(m_BuildingOutOfServicePolicy), !disabled);
		}
	}

	private void OnToggleEmptying()
	{
		m_PoliciesUISystem.SetSelectedInfoPolicy(m_PrefabSystem.GetEntity(m_EmptyingPolicy), !emptying);
	}

	private void OnDelete()
	{
		if (base.EntityManager.Exists(selectedEntity))
		{
			if (base.EntityManager.HasComponent<Building>(selectedEntity))
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_BulldozeSound);
			}
			else
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_DeletetEntitySound);
			}
			m_EndFrameBarrier.CreateCommandBuffer().AddComponent<Deleted>(selectedEntity);
		}
	}

	private void OnFocus()
	{
		m_InfoUISystem.Focus((!focusing) ? selectedEntity : Entity.Null);
	}

	private void OnToggleMove()
	{
		if (moveable)
		{
			if (m_ToolSystem.activeTool == m_ObjectToolSystem)
			{
				m_ToolSystem.activeTool = m_DefaultToolSystem;
				return;
			}
			m_ObjectToolSystem.StartMoving(selectedEntity);
			m_ToolSystem.activeTool = m_ObjectToolSystem;
		}
	}

	private void OnFollow()
	{
		if (!base.EntityManager.HasComponent<Followed>(selectedEntity))
		{
			m_LifePathEventSystem.FollowCitizen(selectedEntity);
			m_GamePanelUISystem.ShowPanel<LifePathPanel>(selectedEntity);
		}
		else
		{
			m_LifePathEventSystem.UnfollowCitizen(selectedEntity);
		}
		m_InfoUISystem.SetDirty();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = selectedEntity != Entity.Null;
	}

	protected override void OnProcess()
	{
		focusable = !base.EntityManager.HasComponent<BuildingExtensionData>(selectedPrefab) && (!base.EntityManager.HasComponent<Household>(selectedEntity) || base.EntityManager.HasComponent<PropertyRenter>(selectedEntity));
		focusing = SelectedInfoUISystem.s_CameraController != null && SelectedInfoUISystem.s_CameraController.controllerEnabled && SelectedInfoUISystem.s_CameraController.followedEntity == selectedEntity;
		moveable = base.EntityManager.HasComponent<Game.Objects.Object>(selectedEntity) && base.EntityManager.HasComponent<Static>(selectedEntity) && !base.EntityManager.HasComponent<Native>(selectedEntity) && ((!base.EntityManager.HasComponent<Building>(selectedEntity)) ? (!base.EntityManager.HasComponent<Owner>(selectedEntity) && !base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(selectedEntity)) : (!base.EntityManager.HasComponent<Game.Buildings.WaterPowered>(selectedEntity) && (!base.EntityManager.HasComponent<Owner>(selectedEntity) || base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity)) && (!base.EntityManager.HasComponent<SpawnableBuildingData>(selectedPrefab) || base.EntityManager.HasComponent<SignatureBuildingData>(selectedPrefab))));
		followable = base.EntityManager.HasComponent<Citizen>(selectedEntity);
		following = base.EntityManager.HasComponent<Followed>(selectedEntity);
		deletable = base.EntityManager.HasComponent<District>(selectedEntity) || base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity) || (base.EntityManager.HasComponent<TransportLine>(selectedEntity) && base.EntityManager.HasComponent<Route>(selectedEntity));
		disabled = (base.EntityManager.TryGetComponent<Building>(selectedEntity, out var component) && BuildingUtils.CheckOption(component, BuildingOption.Inactive)) || (base.EntityManager.TryGetComponent<Extension>(selectedEntity, out var component2) && (component2.m_Flags & ExtensionFlags.Disabled) != ExtensionFlags.None) || (base.EntityManager.TryGetComponent<Route>(selectedEntity, out var component3) && RouteUtils.CheckOption(component3, RouteOption.Inactive));
		if (base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity))
		{
			disableable = true;
			base.tooltipKeys.Add("Building");
		}
		else if (base.EntityManager.HasComponent<Policy>(selectedEntity))
		{
			if (base.EntityManager.HasComponent<Building>(selectedEntity) && base.EntityManager.HasComponent<CityServiceUpkeep>(selectedEntity) && base.EntityManager.HasComponent<Efficiency>(selectedEntity))
			{
				disableable = true;
				base.tooltipKeys.Add("Building");
			}
			if (base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<TransportLine>(selectedEntity) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity))
			{
				disableable = true;
			}
		}
		emptying = base.EntityManager.TryGetComponent<Building>(selectedEntity, out component) && BuildingUtils.CheckOption(component, BuildingOption.Empty);
		emptiable = base.EntityManager.HasComponent<Policy>(selectedEntity) && base.EntityManager.HasComponent<Building>(selectedEntity) && base.EntityManager.TryGetComponent<GarbageFacilityData>(selectedPrefab, out var component4) && component4.m_LongTermStorage;
		if (base.EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<Game.Prefabs.SubArea> buffer) && buffer.Length > 0)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (m_PrefabSystem.TryGetPrefab<AreaPrefab>(buffer[i].m_Prefab, out var prefab) && prefab.Has<LotPrefab>())
				{
					m_LotPrefab = prefab;
					hasLotTool = ((LotPrefab)prefab).m_AllowEditing;
					break;
				}
			}
		}
		if (!hasLotTool && base.EntityManager.HasComponent<SpawnableBuildingData>(selectedPrefab) && base.EntityManager.TryGetComponent<Attached>(selectedEntity, out var component5) && base.EntityManager.TryGetBuffer(component5.m_Parent, isReadOnly: true, out DynamicBuffer<Game.Areas.SubArea> buffer2) && buffer2.Length > 0)
		{
			for (int j = 0; j < buffer2.Length; j++)
			{
				if (base.EntityManager.HasComponent<Game.Areas.Lot>(buffer2[j].m_Area) && base.EntityManager.TryGetComponent<PrefabRef>(buffer2[j].m_Area, out var component6) && m_PrefabSystem.TryGetPrefab<AreaPrefab>(component6.m_Prefab, out var prefab2))
				{
					m_LotPrefab = prefab2;
					hasLotTool = true;
					break;
				}
			}
		}
		if (m_TrafficRouteColors == null)
		{
			RouteConfigurationData singleton = m_RouteConfigQuery.GetSingleton<RouteConfigurationData>();
			m_TrafficRouteColors = new Color32[6]
			{
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_CarPathVisualization).color,
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_WatercraftPathVisualization).color,
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_AircraftPathVisualization).color,
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_TrainPathVisualization).color,
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_HumanPathVisualization).color,
				m_PrefabSystem.GetPrefab<LivePathPrefab>(singleton.m_BicyclePathVisualization).color
			};
			m_TrafficRouteColorsBinding.Update(m_TrafficRouteColors);
		}
		hasTrafficRoutes = base.EntityManager.HasComponent<Building>(selectedEntity) || base.EntityManager.HasComponent<Aggregate>(selectedEntity) || base.EntityManager.HasComponent<Game.Net.Node>(selectedEntity) || base.EntityManager.HasComponent<Edge>(selectedEntity) || base.EntityManager.HasComponent<Game.Routes.TransportStop>(selectedEntity) || base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(selectedEntity) || base.EntityManager.HasComponent<Human>(selectedEntity) || base.EntityManager.HasComponent<Vehicle>(selectedEntity) || base.EntityManager.HasComponent<Citizen>(selectedEntity) || base.EntityManager.HasComponent<Household>(selectedEntity);
		m_TrafficRoutesVisibleBinding.Update(m_TrafficRoutesSystem.routesVisible);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("focusable");
		writer.Write(focusable);
		writer.PropertyName("focusing");
		writer.Write(focusing);
		writer.PropertyName("following");
		writer.Write(following);
		writer.PropertyName("followable");
		writer.Write(followable);
		writer.PropertyName("moveable");
		writer.Write(moveable);
		writer.PropertyName("deletable");
		writer.Write(deletable);
		writer.PropertyName("disabled");
		writer.Write(disabled);
		writer.PropertyName("disableable");
		writer.Write(disableable);
		writer.PropertyName("emptying");
		writer.Write(emptying);
		writer.PropertyName("emptiable");
		writer.Write(emptiable);
		writer.PropertyName("hasLotTool");
		writer.Write(hasLotTool);
		writer.PropertyName("hasTrafficRoutes");
		writer.Write(hasTrafficRoutes);
	}

	[Preserve]
	public ActionsSection()
	{
	}
}
