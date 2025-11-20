using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ObjectBuiltRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct UnlockOnBuildJob : IJobChunk
	{
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_Buffer;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedTypeHandle;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedDataFromEntity;

		[ReadOnly]
		public BufferLookup<UnlockOnBuildData> m_UnlockOnBuildDataFromEntity;

		[ReadOnly]
		public ComponentLookup<UnlockRequirementData> m_UnlockRequirementDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ObjectBuiltRequirementData> m_UnlockOnBuildRequirementDataFromEntity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefTypeHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				if (!m_UnlockOnBuildDataFromEntity.TryGetBuffer(prefab, out var bufferData))
				{
					continue;
				}
				bool flag = chunk.Has(ref m_DeletedTypeHandle);
				for (int j = 0; j < bufferData.Length; j++)
				{
					Entity entity = bufferData[j].m_Entity;
					ObjectBuiltRequirementData objectBuiltRequirementData = m_UnlockOnBuildRequirementDataFromEntity[entity];
					UnlockRequirementData component = m_UnlockRequirementDataFromEntity[entity];
					int num = math.max(component.m_Progress + ((!flag) ? 1 : (-1)), 0);
					component.m_Progress = math.min(objectBuiltRequirementData.m_MinimumCount, num);
					m_Buffer.SetComponent(unfilteredChunkIndex, entity, component);
					if (m_LockedDataFromEntity.HasEnabledComponent(entity) && objectBuiltRequirementData.m_MinimumCount <= num)
					{
						Entity e = m_Buffer.CreateEntity(i, m_UnlockEventArchetype);
						m_Buffer.SetComponent(i, e, new Unlock(entity));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<UnlockOnBuildData> __Game_Prefabs_UnlockOnBuildData_RO_BufferLookup;

		public ComponentLookup<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectBuiltRequirementData> __Game_Prefabs_ObjectBuiltRequirementData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Prefabs_UnlockOnBuildData_RO_BufferLookup = state.GetBufferLookup<UnlockOnBuildData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentLookup = state.GetComponentLookup<UnlockRequirementData>();
			__Game_Prefabs_ObjectBuiltRequirementData_RO_ComponentLookup = state.GetComponentLookup<ObjectBuiltRequirementData>(isReadOnly: true);
		}
	}

	private ModificationEndBarrier m_ModificationEndBarrier;

	private EntityQuery m_ChangedQuery;

	private EntityQuery m_AllQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_ChangedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabRef>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Native>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_AllQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Temp>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllQuery : m_ChangedQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			UnlockOnBuildJob jobData = new UnlockOnBuildJob
			{
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_Buffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_PrefabRefTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LockedDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UnlockOnBuildDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockOnBuildData_RO_BufferLookup, ref base.CheckedStateRef),
				m_UnlockRequirementDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_UnlockOnBuildRequirementDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectBuiltRequirementData_RO_ComponentLookup, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
			m_ModificationEndBarrier.AddJobHandleForProducer(base.Dependency);
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
	public ObjectBuiltRequirementSystem()
	{
	}
}
