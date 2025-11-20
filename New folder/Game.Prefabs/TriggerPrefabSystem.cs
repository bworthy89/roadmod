using System.Runtime.CompilerServices;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class TriggerPrefabSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateTriggerPrefabDataJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public BufferTypeHandle<TriggerData> m_TriggerType;

		public TriggerPrefabData m_TriggerPrefabData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<TriggerData> bufferAccessor = chunk.GetBufferAccessor(ref m_TriggerType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity prefab = nativeArray[i];
					DynamicBuffer<TriggerData> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						m_TriggerPrefabData.RemovePrefab(prefab, dynamicBuffer[j]);
					}
				}
				return;
			}
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity prefab2 = nativeArray[k];
				DynamicBuffer<TriggerData> dynamicBuffer2 = bufferAccessor[k];
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					m_TriggerPrefabData.AddPrefab(prefab2, dynamicBuffer2[l]);
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
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TriggerData> __Game_Prefabs_TriggerData_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_TriggerData_RO_BufferTypeHandle = state.GetBufferTypeHandle<TriggerData>(isReadOnly: true);
		}
	}

	private TriggerPrefabData m_PrefabData;

	private EntityQuery m_PrefabQuery;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<TriggerData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_PrefabData = new TriggerPrefabData(Allocator.Persistent);
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_PrefabData.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.Schedule(new UpdateTriggerPrefabDataJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_TriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TriggerPrefabData = m_PrefabData
		}, m_PrefabQuery, JobHandle.CombineDependencies(base.Dependency, m_ReadDependencies));
		m_ReadDependencies = default(JobHandle);
		m_WriteDependencies = jobHandle;
		base.Dependency = jobHandle;
	}

	public TriggerPrefabData ReadTriggerPrefabData(out JobHandle dependencies)
	{
		dependencies = m_WriteDependencies;
		return m_PrefabData;
	}

	public void AddReader(JobHandle handle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, handle);
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
	public TriggerPrefabSystem()
	{
	}
}
