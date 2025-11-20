using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class LaneDataUnknownEscalateSystem : GameSystemBase
{
	[BurstCompile]
	private struct LaneDataUnknownEscalateJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Lane> nativeArray = chunk.GetNativeArray(ref m_LaneType);
			NativeArray<SlaveLane> nativeArray2 = chunk.GetNativeArray(ref m_SlaveLaneType);
			NativeArray<CarLane> nativeArray3 = chunk.GetNativeArray(ref m_CarLaneType);
			NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					SlaveLane slaveLane = nativeArray2[i];
					CarLane carLane = nativeArray3[i];
					Owner owner = nativeArray4[i];
					Lane lane = default(Lane);
					DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subLane = dynamicBuffer[j].m_SubLane;
						if (m_CarLaneData.HasComponent(subLane) && m_CarLaneData[subLane].m_CarriagewayGroup == carLane.m_CarriagewayGroup)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, default(PathfindUpdated));
							if (m_MasterLaneData.HasComponent(subLane) && m_MasterLaneData[subLane].m_Group == slaveLane.m_Group)
							{
								lane = m_LaneData[subLane];
							}
						}
					}
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity subLane2 = dynamicBuffer[k].m_SubLane;
						if (m_LaneData[subLane2].m_StartNode.EqualsIgnoreCurvePos(lane.m_MiddleNode) && m_ParkingLaneData.HasComponent(subLane2))
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane2, default(PathfindUpdated));
						}
					}
				}
				return;
			}
			for (int l = 0; l < nativeArray.Length; l++)
			{
				CarLane carLane2 = nativeArray3[l];
				Lane lane2 = nativeArray[l];
				Owner owner2 = nativeArray4[l];
				DynamicBuffer<SubLane> dynamicBuffer2 = m_SubLanes[owner2.m_Owner];
				for (int m = 0; m < dynamicBuffer2.Length; m++)
				{
					Entity subLane3 = dynamicBuffer2[m].m_SubLane;
					Lane lane3 = m_LaneData[subLane3];
					if (m_CarLaneData.HasComponent(subLane3) && m_CarLaneData[subLane3].m_CarriagewayGroup == carLane2.m_CarriagewayGroup)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane3, default(PathfindUpdated));
					}
					if (lane3.m_StartNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode) && m_ParkingLaneData.HasComponent(subLane3))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane3, default(PathfindUpdated));
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
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarLane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<ParkingLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_LaneQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PathfindUpdated>(),
				ComponentType.ReadOnly<Lane>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<CarLane>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_LaneQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new LaneDataUnknownEscalateJob
		{
			m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_LaneQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public LaneDataUnknownEscalateSystem()
	{
	}
}
