using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
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
public class RoadsInfoviewUISystem : InfoviewUISystemBase
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
		public BufferTypeHandle<Game.Net.SubNet> m_SubNetHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectHandle;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveFromEntity;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarFromEntity;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneFromEntity;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_ParkingLaneDataFromEntity;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLaneFromEntity;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectFromEntity;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjectFromEntity;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneHandle);
			BufferAccessor<Game.Net.SubNet> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubNetHandle);
			BufferAccessor<Game.Objects.SubObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubObjectHandle);
			int num = 0;
			int parked = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				int slots = 0;
				if (bufferAccessor.Length != 0)
				{
					CheckParkingLanes(bufferAccessor[i], ref slots, ref parked);
				}
				if (bufferAccessor2.Length != 0)
				{
					CheckParkingLanes(bufferAccessor2[i], ref slots, ref parked);
				}
				if (bufferAccessor3.Length != 0)
				{
					CheckParkingLanes(bufferAccessor3[i], ref slots, ref parked);
				}
				num += math.select(0, slots, slots > 0);
			}
			m_Results[0] += num;
			m_Results[1] += parked;
		}

		private void CheckParkingLanes(DynamicBuffer<Game.Objects.SubObject> subObjects, ref int slots, ref int parked)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_SubLaneFromEntity.TryGetBuffer(subObject, out var bufferData))
				{
					CheckParkingLanes(bufferData, ref slots, ref parked);
				}
				if (m_SubObjectFromEntity.TryGetBuffer(subObject, out var bufferData2))
				{
					CheckParkingLanes(bufferData2, ref slots, ref parked);
				}
			}
		}

		private void CheckParkingLanes(DynamicBuffer<Game.Net.SubNet> subNets, ref int slots, ref int parked)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (m_SubLaneFromEntity.TryGetBuffer(subNet, out var bufferData))
				{
					CheckParkingLanes(bufferData, ref slots, ref parked);
				}
			}
		}

		private void CheckParkingLanes(DynamicBuffer<Game.Net.SubLane> subLanes, ref int slots, ref int parked)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				GarageLane componentData2;
				if (m_ParkingLaneFromEntity.TryGetComponent(subLane, out var componentData))
				{
					if ((componentData.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
					{
						continue;
					}
					Entity prefab = m_PrefabRefFromEntity[subLane].m_Prefab;
					Curve curve = m_CurveFromEntity[subLane];
					DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjectFromEntity[subLane];
					ParkingLaneData prefabParkingLane = m_ParkingLaneDataFromEntity[prefab];
					if (prefabParkingLane.m_SlotInterval != 0f)
					{
						int parkingSlotCount = NetUtils.GetParkingSlotCount(curve, componentData, prefabParkingLane);
						slots += parkingSlotCount;
					}
					else
					{
						slots = -1000000;
					}
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						if (m_ParkedCarFromEntity.HasComponent(dynamicBuffer[j].m_LaneObject))
						{
							parked++;
						}
					}
				}
				else if (m_GarageLaneFromEntity.TryGetComponent(subLane, out componentData2))
				{
					slots += componentData2.m_VehicleCapacity;
					parked += componentData2.m_VehicleCount;
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
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
		}
	}

	private const string kGroup = "roadsInfo";

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ValueBinding<float> m_ParkingCapacity;

	private ValueBinding<int> m_ParkedCars;

	private ValueBinding<int> m_ParkingIncome;

	private ValueBinding<IndicatorValue> m_ParkingAvailability;

	private EntityQuery m_ParkingFacilityQuery;

	private EntityQuery m_ParkingFacilityModifiedQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_ParkedCars.active && !m_ParkingAvailability.active && !m_ParkingCapacity.active)
			{
				return m_ParkingIncome.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_ParkingFacilityModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ParkingFacilityQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<CarParkingFacility>() },
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
			All = new ComponentType[1] { ComponentType.ReadOnly<CarParking>() },
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
			All = new ComponentType[1] { ComponentType.ReadOnly<CarParkingFacility>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<CarParking>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_ParkingCapacity = new ValueBinding<float>("roadsInfo", "parkingCapacity", 0f));
		AddBinding(m_ParkedCars = new ValueBinding<int>("roadsInfo", "parkedCars", 0));
		AddBinding(m_ParkingIncome = new ValueBinding<int>("roadsInfo", "parkingIncome", 0));
		AddBinding(m_ParkingAvailability = new ValueBinding<IndicatorValue>("roadsInfo", "parkingAvailability", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
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
		UpdateCapacity();
		UpdateAvailability();
		UpdateIncome();
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
	}

	private void UpdateCapacity()
	{
		ResetResults();
		JobChunkExtensions.Schedule(new UpdateParkingJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SubNetHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubLaneHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubObjectHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurveFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLaneFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjectFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_ParkingFacilityQuery, base.Dependency).Complete();
		m_ParkingCapacity.Update(m_Results[0]);
		m_ParkedCars.Update(m_Results[1]);
	}

	private void UpdateAvailability()
	{
		m_ParkingAvailability.Update(IndicatorValue.Calculate(m_ParkingCapacity.value, m_ParkedCars.value));
	}

	private void UpdateIncome()
	{
		m_ParkingIncome.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.Income, 9));
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
	public RoadsInfoviewUISystem()
	{
	}
}
