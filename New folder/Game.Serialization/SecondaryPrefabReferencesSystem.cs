using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class SecondaryPrefabReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct FixSpawnableBuildingJob : IJobChunk
	{
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<SpawnableBuildingData> nativeArray = chunk.GetNativeArray(ref m_SpawnableBuildingType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				SpawnableBuildingData value = nativeArray[nextIndex];
				if (value.m_ZonePrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_ZonePrefab);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixPlaceholderBuildingJob : IJobChunk
	{
		public ComponentTypeHandle<PlaceholderBuildingData> m_PlaceholderBuildingType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PlaceholderBuildingData> nativeArray = chunk.GetNativeArray(ref m_PlaceholderBuildingType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				PlaceholderBuildingData value = nativeArray[nextIndex];
				if (value.m_ZonePrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_ZonePrefab);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixServiceObjectDataJob : IJobChunk
	{
		public ComponentTypeHandle<ServiceObjectData> m_ServiceObjectType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ServiceObjectData> nativeArray = chunk.GetNativeArray(ref m_ServiceObjectType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				ServiceObjectData value = nativeArray[nextIndex];
				if (value.m_Service != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_Service);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixNetLaneDataJob : IJobChunk
	{
		public ComponentTypeHandle<NetLaneData> m_NetLaneType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetLaneData> nativeArray = chunk.GetNativeArray(ref m_NetLaneType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				NetLaneData value = nativeArray[nextIndex];
				if (value.m_PathfindPrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_PathfindPrefab);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixTransportLineDataJob : IJobChunk
	{
		public ComponentTypeHandle<TransportLineData> m_TransportLineType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<TransportLineData> nativeArray = chunk.GetNativeArray(ref m_TransportLineType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				TransportLineData value = nativeArray[nextIndex];
				if (value.m_PathfindPrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_PathfindPrefab);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixContentPrerequisiteDataJob : IJobChunk
	{
		public ComponentTypeHandle<ContentPrerequisiteData> m_ContentPrerequisiteType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ContentPrerequisiteData> nativeArray = chunk.GetNativeArray(ref m_ContentPrerequisiteType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				ContentPrerequisiteData value = nativeArray[nextIndex];
				if (value.m_ContentPrerequisite != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_ContentPrerequisite);
				}
				nativeArray[nextIndex] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetLaneData> __Game_Prefabs_NetLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TransportLineData> __Game_Prefabs_TransportLineData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ContentPrerequisiteData> __Game_Prefabs_ContentPrerequisiteData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_SpawnableBuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>();
			__Game_Prefabs_PlaceholderBuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceholderBuildingData>();
			__Game_Prefabs_ServiceObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceObjectData>();
			__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneData>();
			__Game_Prefabs_TransportLineData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TransportLineData>();
			__Game_Prefabs_ContentPrerequisiteData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ContentPrerequisiteData>();
		}
	}

	private CheckPrefabReferencesSystem m_CheckPrefabReferencesSystem;

	private EntityQuery m_SpawnableBuildingQuery;

	private EntityQuery m_PlaceholderBuildingQuery;

	private EntityQuery m_ServiceObjectQuery;

	private EntityQuery m_NetLaneQuery;

	private EntityQuery m_TransportLineQuery;

	private EntityQuery m_ContentPrerequisiteQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CheckPrefabReferencesSystem = base.World.GetOrCreateSystemManaged<CheckPrefabReferencesSystem>();
		m_SpawnableBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<SpawnableBuildingData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		m_PlaceholderBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<PlaceholderBuildingData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		m_ServiceObjectQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceObjectData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		m_NetLaneQuery = GetEntityQuery(ComponentType.ReadOnly<NetLaneData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		m_TransportLineQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLineData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		m_ContentPrerequisiteQuery = GetEntityQuery(ComponentType.ReadOnly<ContentPrerequisiteData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		PrefabReferences prefabReferences = m_CheckPrefabReferencesSystem.GetPrefabReferences(this, out dependencies);
		dependencies = JobHandle.CombineDependencies(base.Dependency, dependencies);
		FixSpawnableBuildingJob jobData = new FixSpawnableBuildingJob
		{
			m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		FixPlaceholderBuildingJob jobData2 = new FixPlaceholderBuildingJob
		{
			m_PlaceholderBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		FixServiceObjectDataJob jobData3 = new FixServiceObjectDataJob
		{
			m_ServiceObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		FixNetLaneDataJob jobData4 = new FixNetLaneDataJob
		{
			m_NetLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		FixTransportLineDataJob jobData5 = new FixTransportLineDataJob
		{
			m_TransportLineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportLineData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		FixContentPrerequisiteDataJob jobData6 = new FixContentPrerequisiteDataJob
		{
			m_ContentPrerequisiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ContentPrerequisiteData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = prefabReferences
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_SpawnableBuildingQuery, dependencies);
		JobHandle job2 = JobChunkExtensions.ScheduleParallel(jobData2, m_PlaceholderBuildingQuery, dependencies);
		JobHandle job3 = JobChunkExtensions.ScheduleParallel(jobData3, m_ServiceObjectQuery, dependencies);
		JobHandle job4 = JobChunkExtensions.ScheduleParallel(jobData4, m_NetLaneQuery, dependencies);
		JobHandle job5 = JobChunkExtensions.ScheduleParallel(jobData5, m_TransportLineQuery, dependencies);
		JobHandle job6 = JobChunkExtensions.ScheduleParallel(jobData6, m_ContentPrerequisiteQuery, dependencies);
		dependencies = JobUtils.CombineDependencies(job, job2, job3, job4, job5, job6);
		m_CheckPrefabReferencesSystem.AddPrefabReferencesUser(dependencies);
		base.Dependency = dependencies;
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
	public SecondaryPrefabReferencesSystem()
	{
	}
}
