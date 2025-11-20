using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class AreaToolTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private AreaToolSystem m_AreaTool;

	private ResourceSystem m_ResourceSystem;

	private EntityQuery m_TempQuery;

	private IntTooltip m_Resources;

	private IntTooltip m_AreaSizeToolTip;

	private IntTooltip m_Storage;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaTool = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Area>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		RequireForUpdate(m_TempQuery);
		m_AreaSizeToolTip = new IntTooltip
		{
			path = "areaToolAreaSize",
			label = LocalizedString.Id("Tools.AREASIZE_LABEL"),
			unit = "area"
		};
		m_Resources = new IntTooltip
		{
			path = "areaToolResources",
			label = LocalizedString.Id("Tools.RESOURCES_LABEL"),
			unit = "weight"
		};
		m_Storage = new IntTooltip
		{
			path = "areaToolStorage",
			label = LocalizedString.Id("Tools.STORAGECAPACITY_LABEL"),
			unit = "weight"
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool != m_AreaTool || m_AreaTool.tooltip == AreaToolSystem.Tooltip.None || !ShouldShowResources(m_AreaTool.tooltip))
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_TempQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(entity);
				if (base.EntityManager.TryGetComponent<Extractor>(entity, out var component))
				{
					if (((base.EntityManager.TryGetComponent<Owner>(entity, out var component2) && base.EntityManager.TryGetComponent<Owner>(component2.m_Owner, out var component3)) || base.EntityManager.TryGetComponent<Owner>(entity, out component3)) && base.EntityManager.TryGetComponent<PrefabRef>(component3.m_Owner, out var component4) && base.EntityManager.TryGetComponent<BuildingPropertyData>(component4.m_Prefab, out var component5) && base.EntityManager.TryGetComponent<ResourceData>(m_ResourceSystem.GetPrefab(component5.m_AllowedManufactured), out var component6) && component6.m_RequireNaturalResource && base.EntityManager.TryGetComponent<ExtractorAreaData>(componentData.m_Prefab, out var component7) && component7.m_RequireNaturalResource)
					{
						num2 += (int)math.round(component.m_ResourceAmount);
						flag = true;
					}
					else
					{
						BufferLookup<Game.Areas.SubArea> subAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef);
						BufferLookup<InstalledUpgrade> installedUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef);
						ComponentLookup<Game.Areas.Lot> lots = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef);
						ComponentLookup<Geometry> geometries = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef);
						float area = ExtractorAISystem.GetArea(entity, ref subAreas, ref installedUpgrades, ref lots, ref geometries);
						num += (int)math.round(area);
						flag3 = true;
					}
				}
				if (!base.EntityManager.HasComponent<Storage>(entity))
				{
					continue;
				}
				Geometry componentData2 = base.EntityManager.GetComponentData<Geometry>(entity);
				StorageAreaData componentData3 = base.EntityManager.GetComponentData<StorageAreaData>(componentData.m_Prefab);
				int num4 = AreaUtils.CalculateStorageCapacity(componentData2, componentData3);
				if (base.EntityManager.TryGetComponent<Owner>(entity, out var component8) && base.EntityManager.TryGetComponent<PrefabRef>(component8.m_Owner, out var component9) && base.EntityManager.TryGetComponent<GarbageFacilityData>(component9.m_Prefab, out var component10))
				{
					if (base.EntityManager.TryGetBuffer(component8.m_Owner, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
					{
						UpgradeUtils.CombineStats(base.EntityManager, ref component10, buffer);
					}
					num4 += component10.m_GarbageCapacity;
				}
				num3 += num4;
				flag2 = true;
			}
			if (flag)
			{
				m_Resources.value = num2;
				AddMouseTooltip(m_Resources);
			}
			if (flag3)
			{
				m_AreaSizeToolTip.value = num;
				AddMouseTooltip(m_AreaSizeToolTip);
			}
			if (flag2)
			{
				m_Storage.value = num3;
				AddMouseTooltip(m_Storage);
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private static bool ShouldShowResources(AreaToolSystem.Tooltip tooltip)
	{
		if ((uint)(tooltip - 6) <= 4u)
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AreaToolTooltipSystem()
	{
	}
}
