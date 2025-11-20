using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class UIHighlightSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	private struct HighlightJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Unlock> m_UnlockType;

		[ReadOnly]
		public ComponentLookup<UIObjectData> m_ObjectDatas;

		[ReadOnly]
		public ComponentLookup<UIAssetCategoryData> m_AssetCategories;

		[ReadOnly]
		public ComponentLookup<UIToolbarGroupData> m_ToolbarGroups;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_ServiceUpgrade;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Unlock> nativeArray = chunk.GetNativeArray(ref m_UnlockType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Unlock unlock = nativeArray[i];
				if (m_ObjectDatas.TryGetComponent(unlock.m_Prefab, out var componentData))
				{
					Entity entity = componentData.m_Group;
					if (m_AssetCategories.TryGetComponent(entity, out var componentData2) && !m_ServiceUpgrade.HasComponent(unlock.m_Prefab))
					{
						m_CommandBuffer.AddComponent<UIHighlight>(unlock.m_Prefab);
						m_CommandBuffer.AddComponent<UIHighlight>(entity);
						m_CommandBuffer.AddComponent<UIHighlight>(componentData2.m_Menu);
					}
					else if (m_ToolbarGroups.HasComponent(entity) || m_ServiceUpgrade.HasComponent(unlock.m_Prefab))
					{
						m_CommandBuffer.AddComponent<UIHighlight>(unlock.m_Prefab);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Unlock> __Game_Prefabs_Unlock_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<UIObjectData> __Game_Prefabs_UIObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UIAssetCategoryData> __Game_Prefabs_UIAssetCategoryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UIToolbarGroupData> __Game_Prefabs_UIToolbarGroupData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_Unlock_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unlock>(isReadOnly: true);
			__Game_Prefabs_UIObjectData_RO_ComponentLookup = state.GetComponentLookup<UIObjectData>(isReadOnly: true);
			__Game_Prefabs_UIAssetCategoryData_RO_ComponentLookup = state.GetComponentLookup<UIAssetCategoryData>(isReadOnly: true);
			__Game_Prefabs_UIToolbarGroupData_RO_ComponentLookup = state.GetComponentLookup<UIToolbarGroupData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_UnlockedPrefabQuery;

	private bool m_SkipUpdate = true;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_UnlockedPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		RequireForUpdate(m_UnlockedPrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_SkipUpdate)
		{
			m_SkipUpdate = false;
			return;
		}
		HighlightJob jobData = new HighlightJob
		{
			m_UnlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Unlock_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UIObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AssetCategories = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UIAssetCategoryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ToolbarGroups = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UIToolbarGroupData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgrade = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_UnlockedPrefabQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
	}

	public void PreDeserialize(Context context)
	{
		EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UIHighlight>());
		try
		{
			base.EntityManager.RemoveComponent<UIHighlight>(entityQuery);
		}
		finally
		{
			entityQuery.Dispose();
		}
		m_SkipUpdate = true;
	}

	public void SkipUpdate()
	{
		m_SkipUpdate = true;
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
	public UIHighlightSystem()
	{
	}
}
