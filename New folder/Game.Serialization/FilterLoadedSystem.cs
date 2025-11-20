using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class FilterLoadedSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckLanesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Owner owner = nativeArray2[i];
				if (m_EditorContainerData.HasComponent(owner.m_Owner))
				{
					Entity e = nativeArray[i];
					if (m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
					{
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, componentData);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Owner>(unfilteredChunkIndex, e);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_NetLaneQuery;

	private EntityQuery m_EditorContainerQuery;

	private EntityQuery m_LocalTransformCacheQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_NetLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Owner>());
		m_EditorContainerQuery = GetEntityQuery(ComponentType.ReadOnly<EditorContainer>());
		m_LocalTransformCacheQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransformCache>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Purpose purpose = m_LoadGameSystem.context.purpose;
		if (purpose == Purpose.NewGame && !m_EditorContainerQuery.IsEmptyIgnoreFilter)
		{
			if (!m_NetLaneQuery.IsEmptyIgnoreFilter)
			{
				EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
				JobChunkExtensions.ScheduleParallel(new CheckLanesJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CommandBuffer = entityCommandBuffer.AsParallelWriter()
				}, m_NetLaneQuery, base.Dependency).Complete();
				entityCommandBuffer.Playback(base.EntityManager);
				entityCommandBuffer.Dispose();
			}
			base.EntityManager.DestroyEntity(m_EditorContainerQuery);
		}
		if (purpose != Purpose.NewMap && purpose != Purpose.LoadMap)
		{
			base.EntityManager.RemoveComponent<LocalTransformCache>(m_LocalTransformCacheQuery);
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
	public FilterLoadedSystem()
	{
	}
}
