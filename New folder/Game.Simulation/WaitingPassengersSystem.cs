using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaitingPassengersSystem : GameSystemBase
{
	[BurstCompile]
	private struct ClearWaitingPassengersJob : IJobChunk
	{
		public ComponentTypeHandle<WaitingPassengers> m_WaitingPassengersType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaitingPassengers> nativeArray = chunk.GetNativeArray(ref m_WaitingPassengersType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref WaitingPassengers reference = ref nativeArray.ElementAt(i);
				reference.m_Count = 0;
				reference.m_OngoingAccumulation = 0;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountWaitingPassengersJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Resident> m_ResidentType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> m_GroupCreatureType;

		[ReadOnly]
		public BufferTypeHandle<Queue> m_QueueType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<WaitingPassengers> m_WaitingPassengersData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<HumanCurrentLane> nativeArray = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			NativeArray<Resident> nativeArray2 = chunk.GetNativeArray(ref m_ResidentType);
			NativeArray<PathOwner> nativeArray3 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<GroupCreature> bufferAccessor2 = chunk.GetBufferAccessor(ref m_GroupCreatureType);
			BufferAccessor<Queue> bufferAccessor3 = chunk.GetBufferAccessor(ref m_QueueType);
			Entity lastStop = Entity.Null;
			int2 accumulation = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				HumanCurrentLane currentLane = nativeArray[i];
				DynamicBuffer<GroupCreature> groupCreatures = default(DynamicBuffer<GroupCreature>);
				if (bufferAccessor2.Length != 0)
				{
					groupCreatures = bufferAccessor2[i];
				}
				Resident resident = default(Resident);
				if (nativeArray2.Length != 0)
				{
					resident = nativeArray2[i];
				}
				if (CreatureUtils.TransportStopReached(currentLane))
				{
					PathOwner pathOwner = nativeArray3[i];
					DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[i];
					if (dynamicBuffer.Length >= pathOwner.m_ElementIndex + 2)
					{
						Entity target = dynamicBuffer[pathOwner.m_ElementIndex].m_Target;
						if (target != Entity.Null)
						{
							AddPassengers(target, resident, groupCreatures, ref lastStop, ref accumulation);
						}
					}
					continue;
				}
				DynamicBuffer<Queue> dynamicBuffer2 = bufferAccessor3[i];
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					Queue queue = dynamicBuffer2[j];
					if (!(queue.m_TargetArea.radius <= 0f))
					{
						Entity targetEntity = queue.m_TargetEntity;
						if (targetEntity != Entity.Null)
						{
							AddPassengers(targetEntity, resident, groupCreatures, ref lastStop, ref accumulation);
						}
					}
				}
			}
			if (lastStop != Entity.Null)
			{
				AddPassengers(Entity.Null, default(Resident), default(DynamicBuffer<GroupCreature>), ref lastStop, ref accumulation);
			}
		}

		private void AddPassengers(Entity stop, Resident resident, DynamicBuffer<GroupCreature> groupCreatures, ref Entity lastStop, ref int2 accumulation)
		{
			if (stop != lastStop)
			{
				if (m_WaitingPassengersData.HasComponent(lastStop))
				{
					ref WaitingPassengers valueRW = ref m_WaitingPassengersData.GetRefRW(lastStop).ValueRW;
					Interlocked.Add(ref valueRW.m_Count, accumulation.x);
					Interlocked.Add(ref valueRW.m_OngoingAccumulation, accumulation.y);
				}
				lastStop = stop;
				accumulation = 0;
			}
			int2 @int = default(int2);
			@int.x = 1;
			if (groupCreatures.IsCreated)
			{
				@int.x += groupCreatures.Length;
			}
			@int.y = (int)((float)(resident.m_Timer * @int.x) * (2f / 15f));
			accumulation += @int;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct TickWaitingPassengersJob : IJobChunk
	{
		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<WaitingPassengers> m_WaitingPassengersType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WaitingPassengers> nativeArray2 = chunk.GetNativeArray(ref m_WaitingPassengersType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				ref WaitingPassengers reference = ref nativeArray2.ElementAt(i);
				if (random.NextInt(64) == 0 && reference.m_SuccessAccumulation < ushort.MaxValue)
				{
					reference.m_SuccessAccumulation++;
				}
				int2 @int = new int2(reference.m_OngoingAccumulation, reference.m_ConcludedAccumulation);
				int2 int2 = math.max(y: new int2(reference.m_Count, reference.m_SuccessAccumulation), x: 1);
				int2 x = (@int + int2 - 1) / int2;
				int num = math.cmax(x);
				num = math.min(65535, num - num % 5);
				if (num != reference.m_AverageWaitingTime)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(PathfindUpdated));
				}
				int num2 = reference.m_SuccessAccumulation + random.NextInt(256) >> 8;
				reference.m_ConcludedAccumulation = math.max(0, reference.m_ConcludedAccumulation - num2 * x.y);
				reference.m_SuccessAccumulation -= (ushort)num2;
				reference.m_AverageWaitingTime = (ushort)num;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<WaitingPassengers> __Game_Routes_WaitingPassengers_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> __Game_Creatures_GroupCreature_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Queue> __Game_Creatures_Queue_RO_BufferTypeHandle;

		public ComponentLookup<WaitingPassengers> __Game_Routes_WaitingPassengers_RW_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Routes_WaitingPassengers_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaitingPassengers>();
			__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Resident>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Creatures_GroupCreature_RO_BufferTypeHandle = state.GetBufferTypeHandle<GroupCreature>(isReadOnly: true);
			__Game_Creatures_Queue_RO_BufferTypeHandle = state.GetBufferTypeHandle<Queue>(isReadOnly: true);
			__Game_Routes_WaitingPassengers_RW_ComponentLookup = state.GetComponentLookup<WaitingPassengers>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_StopQuery;

	private EntityQuery m_ResidentQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetExistingSystemManaged<EndFrameBarrier>();
		m_StopQuery = GetEntityQuery(ComponentType.ReadWrite<WaitingPassengers>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ResidentQuery = GetEntityQuery(ComponentType.ReadOnly<HumanCurrentLane>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<GroupMember>());
		RequireForUpdate(m_StopQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ClearWaitingPassengersJob jobData = new ClearWaitingPassengersJob
		{
			m_WaitingPassengersType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_WaitingPassengers_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		CountWaitingPassengersJob jobData2 = new CountWaitingPassengersJob
		{
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_GroupCreatureType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_GroupCreature_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_QueueType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_Queue_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_WaitingPassengersData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WaitingPassengers_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TickWaitingPassengersJob
		{
			m_RandomSeed = RandomSeed.Next(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_WaitingPassengersType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_WaitingPassengers_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, dependsOn: JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData, m_StopQuery, base.Dependency), jobData: jobData2, query: m_ResidentQuery), query: m_StopQuery);
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
	public WaitingPassengersSystem()
	{
	}
}
