using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Localization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class HomelessTooltipSystem : TooltipSystemBase
{
	private IntTooltip m_HomelessCountTooltip;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_ConfigQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIInfoviewsConfigurationData>());
		m_HomelessCountTooltip = new IntTooltip
		{
			path = "HomelessCount",
			label = LocalizedString.Id("Infoviews.INFOVIEW[HomelessCount]"),
			unit = "integer"
		};
		RequireForUpdate(m_ConfigQuery);
	}

	private bool IsInfomodeActivated()
	{
		Entity singletonEntity = m_ConfigQuery.GetSingletonEntity();
		if (m_PrefabSystem.TryGetPrefab<UIInfoviewsConfigurationPrefab>(singletonEntity, out var prefab))
		{
			Entity entity = m_PrefabSystem.GetEntity(prefab.m_HomelessInfomodePrefab);
			return base.EntityManager.HasComponent<InfomodeActive>(entity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!IsInfomodeActivated())
		{
			return;
		}
		CompleteDependency();
		m_HomelessCountTooltip.value = 0;
		if (m_ToolRaycastSystem.GetRaycastResult(out var result) && BuildingUtils.IsHomelessShelterBuilding(base.EntityManager, result.m_Owner) && base.EntityManager.TryGetBuffer(result.m_Owner, isReadOnly: true, out DynamicBuffer<Renter> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Renter renter = buffer[i];
				if (!base.EntityManager.HasComponent<HomelessHousehold>(renter.m_Renter) || !base.EntityManager.TryGetBuffer(renter.m_Renter, isReadOnly: true, out DynamicBuffer<HouseholdCitizen> buffer2))
				{
					continue;
				}
				for (int j = 0; j < buffer2.Length; j++)
				{
					HouseholdCitizen householdCitizen = buffer2[j];
					if (!CitizenUtils.IsDead(base.EntityManager, householdCitizen.m_Citizen))
					{
						m_HomelessCountTooltip.value++;
					}
				}
			}
		}
		if (m_HomelessCountTooltip.value > 0)
		{
			AddMouseTooltip(m_HomelessCountTooltip);
		}
	}

	[Preserve]
	public HomelessTooltipSystem()
	{
	}
}
