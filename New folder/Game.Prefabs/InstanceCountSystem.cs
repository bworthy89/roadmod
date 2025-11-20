using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class InstanceCountSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	private struct UpdateCountsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		public NativeParallelHashMap<Entity, int> m_InstanceCounts;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					PrefabRef prefabRef = nativeArray[i];
					if (m_InstanceCounts.TryGetValue(prefabRef.m_Prefab, out var item))
					{
						if (--item > 0)
						{
							m_InstanceCounts[prefabRef.m_Prefab] = item;
						}
						else
						{
							m_InstanceCounts.Remove(prefabRef.m_Prefab);
						}
					}
				}
				return;
			}
			for (int j = 0; j < nativeArray.Length; j++)
			{
				PrefabRef prefabRef2 = nativeArray[j];
				if (m_InstanceCounts.TryGetValue(prefabRef2.m_Prefab, out var item2))
				{
					m_InstanceCounts[prefabRef2.m_Prefab] = item2 + 1;
				}
				else
				{
					m_InstanceCounts.Add(prefabRef2.m_Prefab, 1);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedInstancesQuery;

	private EntityQuery m_AllInstancesQuery;

	private NativeParallelHashMap<Entity, int> m_InstanceCounts;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedInstancesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabRef>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllInstancesQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>());
		m_InstanceCounts = new NativeParallelHashMap<Entity, int>(100, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_InstanceCounts.Dispose();
		base.OnDestroy();
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
		EntityQuery query = (GetLoaded() ? m_AllInstancesQuery : m_UpdatedInstancesQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			JobHandle jobHandle = JobChunkExtensions.Schedule(new UpdateCountsJob
			{
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InstanceCounts = GetInstanceCounts(readOnly: false, out dependencies)
			}, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
			AddCountWriter(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	public NativeParallelHashMap<Entity, int> GetInstanceCounts(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		return m_InstanceCounts;
	}

	public void AddCountReader(JobHandle jobHandle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
	}

	public void AddCountWriter(JobHandle jobHandle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, jobHandle);
	}

	public void PreDeserialize(Context context)
	{
		JobHandle dependencies;
		NativeParallelHashMap<Entity, int> instanceCounts = GetInstanceCounts(readOnly: false, out dependencies);
		dependencies.Complete();
		instanceCounts.Clear();
		m_Loaded = true;
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
	public InstanceCountSystem()
	{
	}
}
