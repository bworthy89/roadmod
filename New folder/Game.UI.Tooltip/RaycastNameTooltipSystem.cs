using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class RaycastNameTooltipSystem : TooltipSystemBase
{
	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private NameSystem m_NameSystem;

	private ImageSystem m_ImageSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private NameTooltip m_Tooltip;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_Tooltip = new NameTooltip
		{
			path = "raycastName",
			nameBinder = m_NameSystem
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool == m_DefaultTool && m_ToolRaycastSystem.GetRaycastResult(out var result) && (base.EntityManager.HasComponent<Building>(result.m_Owner) || base.EntityManager.HasComponent<Game.Routes.TransportStop>(result.m_Owner) || base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(result.m_Owner) || base.EntityManager.HasComponent<Route>(result.m_Owner) || base.EntityManager.HasComponent<Creature>(result.m_Owner) || base.EntityManager.HasComponent<Vehicle>(result.m_Owner) || base.EntityManager.HasComponent<Aggregate>(result.m_Owner) || base.EntityManager.HasComponent<Game.Objects.NetObject>(result.m_Owner)) && base.EntityManager.TryGetComponent<PrefabRef>(result.m_Owner, out var component))
		{
			Entity instance = result.m_Owner;
			Entity prefab = component.m_Prefab;
			AdjustTargets(ref instance, ref prefab);
			m_Tooltip.icon = m_ImageSystem.GetInstanceIcon(instance, prefab);
			m_Tooltip.entity = instance;
			AddMouseTooltip(m_Tooltip);
		}
	}

	private void AdjustTargets(ref Entity instance, ref Entity prefab)
	{
		if (base.EntityManager.TryGetComponent<Game.Creatures.Resident>(instance, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Citizen, out var component2))
		{
			instance = component.m_Citizen;
			prefab = component2.m_Prefab;
		}
		if (base.EntityManager.TryGetComponent<Controller>(instance, out var component3) && base.EntityManager.TryGetComponent<PrefabRef>(component3.m_Controller, out var component4))
		{
			instance = component3.m_Controller;
			prefab = component4.m_Prefab;
		}
		if (base.EntityManager.TryGetComponent<Game.Creatures.Pet>(instance, out var component5) && base.EntityManager.TryGetComponent<PrefabRef>(component5.m_HouseholdPet, out var component6))
		{
			instance = component5.m_HouseholdPet;
			prefab = component6.m_Prefab;
		}
	}

	[Preserve]
	public RaycastNameTooltipSystem()
	{
	}
}
