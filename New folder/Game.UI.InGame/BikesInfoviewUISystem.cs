using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class BikesInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		Slots,
		Parked,
		ResultCount
	}

	[BurstCompile]
	private struct UpdateParkingJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_ParkingLaneDataLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLaneLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjectLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNetLookup;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			int parkingCapacity = 0;
			int parkedVehicleCount = 0;
			int laneCount = 0;
			int parkingFee = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				VehicleUtils.GetParkingData(nativeArray[i], ref laneCount, ref parkingCapacity, ref parkedVehicleCount, ref parkingFee, ref m_ParkingLaneLookup, ref m_PrefabRefLookup, ref m_CurveLookup, ref m_ParkingLaneDataLookup, ref m_ParkedCarLookup, ref m_GarageLaneLookup, ref m_LaneObjectLookup, ref m_SubLaneLookup, ref m_SubNetLookup, ref m_SubObjectLookup);
			}
			m_Results[0] += parkingCapacity;
			m_Results[1] += parkedVehicleCount;
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
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
		}
	}

	private const string kGroup = "bikesInfo";

	private ValueBinding<IndicatorValue> m_ParkingAvailability;

	private ValueBinding<int2> m_Parking;

	private EntityQuery m_ParkingFacilityQuery;

	private EntityQuery m_ParkingFacilityModifiedQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_ParkingAvailability.active)
			{
				return m_Parking.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_ParkingFacilityModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ParkingFacilityQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<BicycleParkingFacility>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.SubLane>(),
				ComponentType.ReadOnly<Game.Net.SubNet>(),
				ComponentType.ReadOnly<Game.Objects.SubObject>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<BicycleParking>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Net.SubLane>(),
				ComponentType.ReadOnly<Game.Objects.SubObject>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ParkingFacilityModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<BicycleParkingFacility>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<BicycleParking>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_ParkingAvailability = new ValueBinding<IndicatorValue>("bikesInfo", "bikeParkingAvailability", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		AddBinding(m_Parking = new ValueBinding<int2>("bikesInfo", "bikeParking", int2.zero));
		m_Results = new NativeArray<int>(2, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		ResetResults();
		UpdateAvailability();
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
	}

	private void UpdateAvailability()
	{
		JobChunkExtensions.Schedule(new UpdateParkingJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurveLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLaneLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjectLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNetLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_ParkingFacilityQuery, base.Dependency).Complete();
		int num = m_Results[0];
		int num2 = m_Results[1];
		m_ParkingAvailability.Update(IndicatorValue.Calculate(num, num2, 0f));
		m_Parking.Update(new int2(num2, num));
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
	public BikesInfoviewUISystem()
	{
	}
}
