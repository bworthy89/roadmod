using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Debug;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CountVehicleDataSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct VehicleCountData : IAccumulable<VehicleCountData>, ISerializable
	{
		public int m_BicycleOnCarRoad;

		public int m_BicycleOnBicycleOnlyRoad;

		public int m_TotalBicycleParkingSpace;

		public int m_OccupiedBicycleParkingSpace;

		public void Accumulate(VehicleCountData other)
		{
			m_BicycleOnCarRoad += other.m_BicycleOnCarRoad;
			m_BicycleOnBicycleOnlyRoad += other.m_BicycleOnBicycleOnlyRoad;
			m_TotalBicycleParkingSpace += other.m_TotalBicycleParkingSpace;
			m_OccupiedBicycleParkingSpace += other.m_OccupiedBicycleParkingSpace;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int value = m_BicycleOnCarRoad;
			writer.Write(value);
			int value2 = m_BicycleOnBicycleOnlyRoad;
			writer.Write(value2);
			int value3 = m_TotalBicycleParkingSpace;
			writer.Write(value3);
			int value4 = m_OccupiedBicycleParkingSpace;
			writer.Write(value4);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref int value = ref m_BicycleOnCarRoad;
			reader.Read(out value);
			ref int value2 = ref m_BicycleOnBicycleOnlyRoad;
			reader.Read(out value2);
			ref int value3 = ref m_TotalBicycleParkingSpace;
			reader.Read(out value3);
			ref int value4 = ref m_OccupiedBicycleParkingSpace;
			reader.Read(out value4);
		}
	}

	[BurstCompile]
	private struct CountVehicleJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneDatas;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLanes;

		public NativeAccumulator<VehicleCountData>.ParallelWriter m_VehicleCountData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			VehicleCountData value = default(VehicleCountData);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				bool flag = chunk.Has<Bicycle>();
				if (!m_CarCurrentLanes.TryGetComponent(entity, out var componentData))
				{
					continue;
				}
				PrefabRef componentData2;
				NetLaneData componentData3;
				bool flag2 = m_PrefabRefs.TryGetComponent(componentData.m_Lane, out componentData2) && m_NetLaneDatas.TryGetComponent(componentData2.m_Prefab, out componentData3) && (componentData3.m_Flags & LaneFlags.BicyclesOnly) != 0;
				if (flag)
				{
					if (flag2)
					{
						value.m_BicycleOnBicycleOnlyRoad++;
					}
					else
					{
						value.m_BicycleOnCarRoad++;
					}
				}
			}
			m_VehicleCountData.Accumulate(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountVehicleParkingJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<Curve> m_Curves;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLanes;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_ParkingLaneDatas;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCars;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjectBufs;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLaneBufs;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNetBufs;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBufs;

		public NativeAccumulator<VehicleCountData>.ParallelWriter m_VehicleCountData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			VehicleCountData value = default(VehicleCountData);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				int laneCount = 0;
				int parkingCapacity = 0;
				int parkedVehicleCount = 0;
				int parkingFee = 0;
				VehicleUtils.GetParkingData(entity, ref laneCount, ref parkingCapacity, ref parkedVehicleCount, ref parkingFee, ref m_ParkingLanes, ref m_PrefabRefs, ref m_Curves, ref m_ParkingLaneDatas, ref m_ParkedCars, ref m_GarageLanes, ref m_LaneObjectBufs, ref m_SubLaneBufs, ref m_SubNetBufs, ref m_SubObjectBufs);
				if (chunk.Has<BicycleParking>() || chunk.Has<BicycleParkingFacility>())
				{
					value.m_TotalBicycleParkingSpace += parkingCapacity;
					value.m_OccupiedBicycleParkingSpace += parkedVehicleCount;
				}
			}
			m_VehicleCountData.Accumulate(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SendVehicleTriggerJob : IJob
	{
		[ReadOnly]
		public NativeAccumulator<VehicleCountData> m_VehicleCountData;

		public NativeQueue<TriggerAction> m_TriggerQueue;

		[ReadOnly]
		public uint m_FrameIndex;

		public NativeValue<uint> m_LastParkingCheckFrameIndex;

		public void Execute()
		{
			VehicleCountData result = m_VehicleCountData.GetResult();
			if (m_FrameIndex - m_LastParkingCheckFrameIndex.value > 131072)
			{
				if (result.m_TotalBicycleParkingSpace > 0)
				{
					m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.BikeParking, Entity.Null, (float)result.m_OccupiedBicycleParkingSpace * 1f / (float)result.m_TotalBicycleParkingSpace));
				}
				m_LastParkingCheckFrameIndex.value = m_FrameIndex;
			}
			int num = result.m_BicycleOnBicycleOnlyRoad + result.m_BicycleOnCarRoad;
			if (num > 0)
			{
				float value = (float)result.m_BicycleOnBicycleOnlyRoad * 1f / (float)num;
				m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.BicycleRoadUsage, Entity.Null, value));
				float value2 = (float)result.m_BicycleOnCarRoad * 1f / (float)num;
				m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.BicycleCarRoadUsage, Entity.Null, value2));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentLookup;

		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RW_ComponentLookup;

		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentLookup;

		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RW_ComponentLookup;

		public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RW_ComponentLookup;

		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RW_ComponentLookup;

		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RW_ComponentLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RW_BufferLookup;

		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RW_BufferLookup;

		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RW_ComponentLookup = state.GetComponentLookup<PrefabRef>();
			__Game_Prefabs_NetLaneData_RW_ComponentLookup = state.GetComponentLookup<NetLaneData>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentLookup = state.GetComponentLookup<CarCurrentLane>();
			__Game_Net_ParkingLane_RW_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>();
			__Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>();
			__Game_Prefabs_ParkingLaneData_RW_ComponentLookup = state.GetComponentLookup<ParkingLaneData>();
			__Game_Vehicles_ParkedCar_RW_ComponentLookup = state.GetComponentLookup<ParkedCar>();
			__Game_Net_GarageLane_RW_ComponentLookup = state.GetComponentLookup<GarageLane>();
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
			__Game_Net_SubLane_RW_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>();
			__Game_Net_SubNet_RW_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>();
			__Game_Objects_SubObject_RW_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>();
		}
	}

	public const int UPDATES_PER_DAY = 64;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_VehicleQuery;

	private EntityQuery m_ParkingQuery;

	private NativeValue<uint> m_LastParkingCheckFrameIndex;

	[DebugWatchDeps]
	private JobHandle m_VehicleDataReadDependencies;

	private NativeAccumulator<VehicleCountData> m_VehicleCountData;

	private VehicleCountData m_LastVehicleCountData;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	[DebugWatchValue]
	public int BicycleOnCarRoad => m_LastVehicleCountData.m_BicycleOnCarRoad;

	[DebugWatchValue]
	public int BicycleOnBicycleOnlyRoad => m_LastVehicleCountData.m_BicycleOnBicycleOnlyRoad;

	[DebugWatchValue]
	public int TotalBicycleParkingSpace => m_LastVehicleCountData.m_TotalBicycleParkingSpace;

	[DebugWatchValue]
	public int OccupiedBicycleParkingSpace => m_LastVehicleCountData.m_OccupiedBicycleParkingSpace;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	public VehicleCountData GetVehicleCountData()
	{
		return m_LastVehicleCountData;
	}

	public void AddVehicleDataReader(JobHandle reader)
	{
		m_VehicleDataReadDependencies = JobHandle.CombineDependencies(m_VehicleDataReadDependencies, reader);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_VehicleQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[1] { ComponentType.ReadOnly<Bicycle>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ParkingQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<BicycleParking>(),
				ComponentType.ReadOnly<BicycleParkingFacility>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LastParkingCheckFrameIndex = new NativeValue<uint>(Allocator.Persistent);
		m_VehicleCountData = new NativeAccumulator<VehicleCountData>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LastParkingCheckFrameIndex.Dispose();
		m_VehicleCountData.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		VehicleCountData value = m_LastVehicleCountData;
		writer.Write(value);
		uint value2 = m_LastParkingCheckFrameIndex.value;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref VehicleCountData value = ref m_LastVehicleCountData;
		reader.Read(out value);
		reader.Read(out uint value2);
		m_LastParkingCheckFrameIndex.value = value2;
	}

	private void Reset()
	{
		if (!m_WasReset)
		{
			m_LastVehicleCountData = default(VehicleCountData);
			m_WasReset = true;
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_VehicleCountData.Clear();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_VehicleQuery.IsEmptyIgnoreFilter)
		{
			Reset();
			return;
		}
		m_WasReset = false;
		m_LastVehicleCountData = m_VehicleCountData.GetResult();
		m_VehicleCountData.Clear();
		CountVehicleJob jobData = new CountVehicleJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
			m_NetLaneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleCountData = m_VehicleCountData.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		CountVehicleParkingJob jobData2 = new CountVehicleParkingJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Curves = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCars = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RW_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjectBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_SubLaneBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RW_BufferLookup, ref base.CheckedStateRef),
			m_SubNetBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RW_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_VehicleCountData = m_VehicleCountData.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ParkingQuery, base.Dependency);
		SendVehicleTriggerJob jobData3 = new SendVehicleTriggerJob
		{
			m_VehicleCountData = m_VehicleCountData,
			m_TriggerQueue = m_TriggerSystem.CreateActionBuffer(),
			m_FrameIndex = m_SimulationSystem.frameIndex,
			m_LastParkingCheckFrameIndex = m_LastParkingCheckFrameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData3, base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CountVehicleDataSystem()
	{
	}
}
