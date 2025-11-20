using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TempXPTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlacedSignatureBuildingData> __Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlacedSignatureBuildingData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private EntityQuery m_TempQuery;

	private EntityQuery m_LockedMilestoneQuery;

	private IntTooltip m_Tooltip;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>());
		m_LockedMilestoneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<MilestoneData>(),
				ComponentType.ReadOnly<Locked>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_Tooltip = new IntTooltip
		{
			path = "xp",
			icon = "Media/Game/Icons/Trophy.svg",
			unit = "xp",
			signed = true,
			color = TooltipColor.Success
		};
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.actionMode.IsEditor() || m_LockedMilestoneQuery.IsEmpty)
		{
			return;
		}
		CompleteDependency();
		int num = 0;
		ComponentLookup<PlaceableObjectData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ServiceUpgradeData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PlacedSignatureBuildingData> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			foreach (ArchetypeChunk item in nativeArray)
			{
				NativeArray<Temp> nativeArray2 = item.GetNativeArray(ref typeHandle);
				NativeArray<PrefabRef> nativeArray3 = item.GetNativeArray(ref typeHandle2);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & TempFlags.Create) != 0)
					{
						Entity prefab = nativeArray3[i].m_Prefab;
						if (componentLookup.HasComponent(prefab) && !componentLookup3.HasComponent(prefab) && temp.m_Original == Entity.Null)
						{
							num += componentLookup[prefab].m_XPReward;
						}
						if (componentLookup2.HasComponent(prefab) && temp.m_Original == Entity.Null)
						{
							num += componentLookup2[prefab].m_XPReward;
						}
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		if (num > 0)
		{
			m_Tooltip.value = num;
			AddMouseTooltip(m_Tooltip);
		}
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
	public TempXPTooltipSystem()
	{
	}
}
