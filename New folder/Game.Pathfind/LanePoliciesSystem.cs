using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Prefabs;
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
public class LanePoliciesSystem : GameSystemBase
{
	private enum LaneCheckMask
	{
		ParkingUnknown = 1,
		CarUnknown = 2,
		PedestrianUnknown = 4
	}

	[BurstCompile]
	private struct CheckDistrictLanesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> m_BorderDistrictType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public NativeParallelHashMap<Entity, LaneCheckMask> m_CheckDistricts;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<BorderDistrict> nativeArray = chunk.GetNativeArray(ref m_BorderDistrictType);
			BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				BorderDistrict borderDistrict = nativeArray[i];
				LaneCheckMask laneCheckMask = (LaneCheckMask)0;
				if (m_CheckDistricts.TryGetValue(borderDistrict.m_Left, out var item))
				{
					laneCheckMask |= item;
				}
				if (m_CheckDistricts.TryGetValue(borderDistrict.m_Right, out var item2))
				{
					laneCheckMask |= item2;
				}
				if (laneCheckMask == (LaneCheckMask)0)
				{
					continue;
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity subLane = dynamicBuffer[j].m_SubLane;
					if ((laneCheckMask & LaneCheckMask.ParkingUnknown) != 0 && m_ParkingLaneData.HasComponent(subLane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, subLane);
					}
					if ((laneCheckMask & LaneCheckMask.CarUnknown) != 0 && m_CarLaneData.HasComponent(subLane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, subLane);
					}
					if ((laneCheckMask & LaneCheckMask.PedestrianUnknown) != 0 && m_PedestrianLaneData.HasComponent(subLane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, subLane);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckBuildingLanesJob : IJobParallelFor
	{
		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public NativeArray<Entity> m_CheckBuildings;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_CheckBuildings[index];
			if (m_SubLanes.HasBuffer(entity))
			{
				CheckParkingLanes(index, m_SubLanes[entity]);
			}
			if (m_SubNets.HasBuffer(entity))
			{
				CheckParkingLanes(index, m_SubNets[entity]);
			}
			if (m_SubObjects.HasBuffer(entity))
			{
				CheckParkingLanes(index, m_SubObjects[entity]);
			}
		}

		private void CheckParkingLanes(int jobIndex, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_SubLanes.HasBuffer(subObject))
				{
					CheckParkingLanes(jobIndex, m_SubLanes[subObject]);
				}
				if (m_SubObjects.HasBuffer(subObject))
				{
					CheckParkingLanes(jobIndex, m_SubObjects[subObject]);
				}
			}
		}

		private void CheckParkingLanes(int jobIndex, DynamicBuffer<Game.Net.SubNet> subNets)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (m_SubLanes.HasBuffer(subNet))
				{
					CheckParkingLanes(jobIndex, m_SubLanes[subNet]);
				}
			}
		}

		private void CheckParkingLanes(int jobIndex, DynamicBuffer<Game.Net.SubLane> subLanes)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				if (m_ParkingLaneData.HasComponent(subLane) || m_GarageLaneData.HasComponent(subLane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, subLane);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_BorderDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BorderDistrict>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_PolicyModifyQuery;

	private EntityQuery m_LaneOwnerQuery;

	private EntityQuery m_CarLaneQuery;

	private EntityQuery m_ParkingLaneQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_PolicyModifyQuery = GetEntityQuery(ComponentType.ReadOnly<Modify>());
		m_LaneOwnerQuery = GetEntityQuery(ComponentType.ReadOnly<BorderDistrict>(), ComponentType.ReadOnly<Game.Net.SubLane>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Updated>(), ComponentType.Exclude<Deleted>());
		m_CarLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.CarLane>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Updated>(), ComponentType.Exclude<Deleted>());
		m_ParkingLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ParkingLane>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Updated>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_PolicyModifyQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<Modify> nativeArray = m_PolicyModifyQuery.ToComponentDataArray<Modify>(Allocator.TempJob);
		NativeParallelHashMap<Entity, LaneCheckMask> checkDistricts = default(NativeParallelHashMap<Entity, LaneCheckMask>);
		NativeList<Entity> nativeList = default(NativeList<Entity>);
		LaneCheckMask laneCheckMask = (LaneCheckMask)0;
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Modify modify = nativeArray[i];
			LaneCheckMask laneCheckMask2 = (LaneCheckMask)0;
			bool flag = false;
			if (base.EntityManager.HasComponent<Game.City.City>(modify.m_Entity))
			{
				if (base.EntityManager.TryGetComponent<CityOptionData>(modify.m_Policy, out var component))
				{
					if (CityUtils.HasOption(component, CityOption.UnlimitedHighwaySpeed))
					{
						laneCheckMask |= LaneCheckMask.CarUnknown;
					}
					if (CityUtils.HasOption(component, CityOption.PaidTaxiStart))
					{
						laneCheckMask |= LaneCheckMask.ParkingUnknown;
					}
				}
				if (base.EntityManager.TryGetBuffer(modify.m_Policy, isReadOnly: true, out DynamicBuffer<CityModifierData> buffer))
				{
					for (int j = 0; j < buffer.Length; j++)
					{
						if (buffer[j].m_Type == CityModifierType.TaxiStartingFee)
						{
							laneCheckMask |= LaneCheckMask.ParkingUnknown;
						}
					}
				}
			}
			if (base.EntityManager.HasComponent<District>(modify.m_Entity))
			{
				if (base.EntityManager.TryGetComponent<DistrictOptionData>(modify.m_Policy, out var component2))
				{
					if (AreaUtils.HasOption(component2, DistrictOption.PaidParking))
					{
						laneCheckMask2 |= LaneCheckMask.ParkingUnknown;
					}
					if (AreaUtils.HasOption(component2, DistrictOption.ForbidCombustionEngines))
					{
						laneCheckMask2 |= LaneCheckMask.CarUnknown;
					}
					if (AreaUtils.HasOption(component2, DistrictOption.ForbidTransitTraffic))
					{
						laneCheckMask2 |= (LaneCheckMask)6;
					}
					if (AreaUtils.HasOption(component2, DistrictOption.ForbidHeavyTraffic))
					{
						laneCheckMask2 |= LaneCheckMask.CarUnknown;
					}
					if (AreaUtils.HasOption(component2, DistrictOption.ForbidBicycles))
					{
						laneCheckMask2 |= LaneCheckMask.CarUnknown;
					}
				}
				if (base.EntityManager.TryGetBuffer(modify.m_Policy, isReadOnly: true, out DynamicBuffer<DistrictModifierData> buffer2))
				{
					for (int k = 0; k < buffer2.Length; k++)
					{
						switch (buffer2[k].m_Type)
						{
						case DistrictModifierType.ParkingFee:
							laneCheckMask2 |= LaneCheckMask.ParkingUnknown;
							break;
						case DistrictModifierType.StreetSpeedLimit:
							laneCheckMask2 |= LaneCheckMask.CarUnknown;
							break;
						}
					}
				}
			}
			if (base.EntityManager.HasComponent<Building>(modify.m_Entity))
			{
				if (base.EntityManager.TryGetComponent<BuildingOptionData>(modify.m_Policy, out var component3) && BuildingUtils.HasOption(component3, BuildingOption.PaidParking))
				{
					flag = true;
				}
				if (base.EntityManager.TryGetBuffer(modify.m_Policy, isReadOnly: true, out DynamicBuffer<BuildingModifierData> buffer3))
				{
					for (int l = 0; l < buffer3.Length; l++)
					{
						if (buffer3[l].m_Type == BuildingModifierType.ParkingFee)
						{
							flag = true;
							break;
						}
					}
				}
			}
			if (laneCheckMask2 != 0)
			{
				if (!checkDistricts.IsCreated)
				{
					checkDistricts = new NativeParallelHashMap<Entity, LaneCheckMask>(nativeArray.Length, Allocator.TempJob);
				}
				if (!checkDistricts.TryAdd(modify.m_Entity, laneCheckMask2))
				{
					checkDistricts[modify.m_Entity] = checkDistricts[modify.m_Entity] | laneCheckMask2;
				}
			}
			if (flag)
			{
				if (!nativeList.IsCreated)
				{
					nativeList = new NativeList<Entity>(nativeArray.Length, Allocator.TempJob);
				}
				nativeList.Add(in modify.m_Entity);
			}
		}
		nativeArray.Dispose();
		JobHandle jobHandle = base.Dependency;
		if (laneCheckMask != 0)
		{
			EntityCommandBuffer entityCommandBuffer = m_ModificationBarrier.CreateCommandBuffer();
			if ((laneCheckMask & LaneCheckMask.CarUnknown) != 0)
			{
				entityCommandBuffer.AddComponent<PathfindUpdated>(m_CarLaneQuery, EntityQueryCaptureMode.AtPlayback);
			}
			if ((laneCheckMask & LaneCheckMask.ParkingUnknown) != 0)
			{
				entityCommandBuffer.AddComponent<PathfindUpdated>(m_ParkingLaneQuery, EntityQueryCaptureMode.AtPlayback);
			}
		}
		if (checkDistricts.IsCreated)
		{
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new CheckDistrictLanesJob
			{
				m_BorderDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CheckDistricts = checkDistricts,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_LaneOwnerQuery, base.Dependency);
			checkDistricts.Dispose(jobHandle2);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		if (nativeList.IsCreated)
		{
			JobHandle jobHandle3 = IJobParallelForExtensions.Schedule(new CheckBuildingLanesJob
			{
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CheckBuildings = nativeList.AsArray(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, nativeList.Length, 1, base.Dependency);
			nativeList.Dispose(jobHandle3);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
		}
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
	public LanePoliciesSystem()
	{
	}
}
