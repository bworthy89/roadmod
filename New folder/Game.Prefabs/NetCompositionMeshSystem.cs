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
public class NetCompositionMeshSystem : GameSystemBase
{
	[BurstCompile]
	private struct CompositionMeshJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<NetCompositionMeshData> m_NetCompositionMeshDataType;

		public NativeParallelMultiHashMap<int, Entity> m_MeshEntities;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<NetCompositionMeshData> nativeArray2 = chunk.GetNativeArray(ref m_NetCompositionMeshDataType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (!m_MeshEntities.TryGetFirstValue(nativeArray2[i].m_Hash, out var item, out var it))
					{
						continue;
					}
					do
					{
						if (item == entity)
						{
							m_MeshEntities.Remove(it);
							break;
						}
					}
					while (m_MeshEntities.TryGetNextValue(out item, ref it));
				}
			}
			else
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					m_MeshEntities.Add(nativeArray2[j].m_Hash, nativeArray[j]);
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
		public ComponentTypeHandle<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCompositionMeshData>(isReadOnly: true);
		}
	}

	private EntityQuery m_MeshQuery;

	private NativeParallelMultiHashMap<int, Entity> m_MeshEntities;

	private JobHandle m_Dependencies;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_MeshQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<NetCompositionMeshData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_MeshEntities = new NativeParallelMultiHashMap<int, Entity>(100, Allocator.Persistent);
		RequireForUpdate(m_MeshQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Dependencies.Complete();
		m_MeshEntities.Dispose();
		base.OnDestroy();
	}

	public NativeParallelMultiHashMap<int, Entity> GetMeshEntities(out JobHandle dependencies)
	{
		dependencies = m_Dependencies;
		return m_MeshEntities;
	}

	public void AddMeshEntityReader(JobHandle dependencies)
	{
		m_Dependencies = dependencies;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.Dependency = (m_Dependencies = JobChunkExtensions.Schedule(new CompositionMeshJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionMeshDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MeshEntities = m_MeshEntities
		}, m_MeshQuery, JobHandle.CombineDependencies(base.Dependency, m_Dependencies)));
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
	public NetCompositionMeshSystem()
	{
	}
}
