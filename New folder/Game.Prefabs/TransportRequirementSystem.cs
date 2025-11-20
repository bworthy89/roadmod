using System.Runtime.CompilerServices;
using Game.Common;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class TransportRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct TransportRequirementJob : IJobChunk
	{
		[ReadOnly]
		public NativeHashMap<Entity, TransportUsageData> m_BuildingPrefabTransportUsageData;

		[ReadOnly]
		public NativeHashMap<int, TransportUsageData> m_FilteredPrefabTransportUsageData;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TransportRequirementData> m_TransportRequirementType;

		public ComponentTypeHandle<UnlockRequirementData> m_UnlockRequirementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TransportRequirementData> nativeArray2 = chunk.GetNativeArray(ref m_TransportRequirementType);
			NativeArray<UnlockRequirementData> nativeArray3 = chunk.GetNativeArray(ref m_UnlockRequirementType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				TransportRequirementData transportRequirement = nativeArray2[nextIndex];
				UnlockRequirementData unlockRequirement = nativeArray3[nextIndex];
				if (ShouldUnlock(transportRequirement, ref unlockRequirement))
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_UnlockEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Unlock(nativeArray[nextIndex]));
				}
				nativeArray3[nextIndex] = unlockRequirement;
			}
		}

		private bool ShouldUnlock(TransportRequirementData transportRequirement, ref UnlockRequirementData unlockRequirement)
		{
			long num = 0L;
			long num2 = 0L;
			if (transportRequirement.m_FilterID != 0 && m_FilteredPrefabTransportUsageData.TryGetValue(transportRequirement.m_FilterID, out var item))
			{
				switch (transportRequirement.m_TransportType)
				{
				case TransportType.Airplane:
					num2 = item.m_AirplaneTransportPassenger;
					num = item.m_AirplaneTransportCargo;
					break;
				case TransportType.Ship:
					num2 = item.m_ShipTransportPassenger;
					num = item.m_ShipTransportCargo;
					break;
				case TransportType.Train:
					num2 = item.m_TrainTransportPassenger;
					num = item.m_TrainTransportCargo;
					break;
				case TransportType.None:
					num2 = item.GetTotalPassenger();
					num = item.GetTotalCargo();
					break;
				}
			}
			else if (m_BuildingPrefabTransportUsageData.TryGetValue(transportRequirement.m_BuildingPrefab, out item))
			{
				switch (transportRequirement.m_TransportType)
				{
				case TransportType.Airplane:
					num2 = item.m_AirplaneTransportPassenger;
					num = item.m_AirplaneTransportCargo;
					break;
				case TransportType.Ship:
					num2 = item.m_ShipTransportPassenger;
					num = item.m_ShipTransportCargo;
					break;
				case TransportType.Train:
					num2 = item.m_TrainTransportPassenger;
					num = item.m_TrainTransportCargo;
					break;
				case TransportType.None:
					num2 = item.GetTotalPassenger();
					num = item.GetTotalCargo();
					break;
				}
			}
			if (transportRequirement.m_MinimumTransportedPassenger > 0)
			{
				if (num2 >= transportRequirement.m_MinimumTransportedPassenger)
				{
					unlockRequirement.m_Progress = transportRequirement.m_MinimumTransportedPassenger;
					return true;
				}
				unlockRequirement.m_Progress = (int)num2;
				return false;
			}
			if (transportRequirement.m_MinimumTransportedCargo > 0)
			{
				if (num >= transportRequirement.m_MinimumTransportedCargo)
				{
					unlockRequirement.m_Progress = transportRequirement.m_MinimumTransportedPassenger;
					return true;
				}
				unlockRequirement.m_Progress = (int)num;
				return false;
			}
			return false;
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
		public ComponentTypeHandle<TransportRequirementData> __Game_Prefabs_TransportRequirementData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_TransportRequirementData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportRequirementData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private TransportUsageTrackSystem m_TransportUsageTrackSystem;

	private EntityQuery m_RequirementQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TransportUsageTrackSystem = base.World.GetOrCreateSystemManaged<TransportUsageTrackSystem>();
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<TransportRequirementData>(), ComponentType.ReadWrite<UnlockRequirementData>(), ComponentType.ReadOnly<Locked>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_RequirementQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TransportRequirementJob
		{
			m_BuildingPrefabTransportUsageData = m_TransportUsageTrackSystem.GetBuildingPrefabTransportUsageData(out dependencies),
			m_FilteredPrefabTransportUsageData = m_TransportUsageTrackSystem.GetFilteredTransportUsageData(out dependencies2),
			m_UnlockEventArchetype = m_UnlockEventArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransportRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportRequirementData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnlockRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_RequirementQuery, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		m_TransportUsageTrackSystem.AddTransportUsageDataReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public TransportRequirementSystem()
	{
	}
}
