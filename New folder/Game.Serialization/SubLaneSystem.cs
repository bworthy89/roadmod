using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class SubLaneSystem : GameSystemBase
{
	[BurstCompile]
	private struct SubLaneJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Net.ParkingLane> nativeArray3 = chunk.GetNativeArray(ref m_ParkingLaneType);
			NativeArray<Game.Net.ConnectionLane> nativeArray4 = chunk.GetNativeArray(ref m_ConnectionLaneType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			PathMethod pathMethod = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
			if (chunk.Has(ref m_PedestrianLaneType))
			{
				pathMethod |= PathMethod.Pedestrian;
			}
			if (chunk.Has(ref m_TrackLaneType))
			{
				pathMethod |= PathMethod.Track;
			}
			bool flag = chunk.Has(ref m_CarLaneType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity lane = nativeArray[i];
				Owner owner = nativeArray2[i];
				if (!m_SubLanes.TryGetBuffer(owner.m_Owner, out var bufferData))
				{
					continue;
				}
				PathMethod pathMethod2 = pathMethod;
				if (flag)
				{
					PrefabRef prefabRef = nativeArray5[i];
					if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						if ((componentData.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Road;
						}
						if ((componentData.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Bicycle;
						}
					}
				}
				if (CollectionUtils.TryGet(nativeArray3, i, out var value))
				{
					PrefabRef prefabRef2 = nativeArray5[i];
					if (m_PrefabParkingLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						if ((componentData2.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 = (((value.m_Flags & ParkingLaneFlags.SpecialVehicles) == 0) ? (pathMethod2 | (PathMethod.Parking | PathMethod.Boarding)) : (pathMethod2 | (PathMethod.Boarding | PathMethod.SpecialParking)));
						}
						if ((componentData2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.BicycleParking;
						}
					}
				}
				if (CollectionUtils.TryGet(nativeArray4, i, out var value2))
				{
					if ((value2.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						pathMethod2 |= PathMethod.Pedestrian;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						pathMethod2 |= PathMethod.Road;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Track) != 0)
					{
						pathMethod2 |= PathMethod.Track;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Parking) != 0)
					{
						if ((value2.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Parking | PathMethod.Boarding;
						}
						if ((value2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.BicycleParking;
						}
					}
				}
				bufferData.Add(new Game.Net.SubLane(lane, pathMethod2));
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RW_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Owner>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SubLaneJob jobData = new SubLaneJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public SubLaneSystem()
	{
	}
}
