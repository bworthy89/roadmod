using System.Runtime.CompilerServices;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class InitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct AssignRouteNumbersJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_RouteChunks;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<RouteNumber> m_RouteNumberType;

		public void Execute()
		{
			for (int i = 0; i < m_RouteChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_RouteChunks[i];
				if (archetypeChunk.Has(ref m_CreatedType))
				{
					NativeArray<RouteNumber> nativeArray = archetypeChunk.GetNativeArray(ref m_RouteNumberType);
					NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						RouteNumber value = nativeArray[j];
						value.m_Number = FindFreeRouteNumber(nativeArray2[j].m_Prefab);
						nativeArray[j] = value;
					}
				}
			}
		}

		private int FindFreeRouteNumber(Entity prefab)
		{
			int num = 0;
			for (int i = 0; i < m_RouteChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_RouteChunks[i];
				if (!archetypeChunk.Has(ref m_CreatedType))
				{
					NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						num += math.select(0, 1, nativeArray[j].m_Prefab == prefab);
					}
				}
			}
			if (num > 0)
			{
				NativeBitArray nativeBitArray = new NativeBitArray(num + 1, Allocator.Temp);
				for (int k = 0; k < m_RouteChunks.Length; k++)
				{
					ArchetypeChunk archetypeChunk2 = m_RouteChunks[k];
					if (archetypeChunk2.Has(ref m_CreatedType))
					{
						continue;
					}
					NativeArray<RouteNumber> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_RouteNumberType);
					NativeArray<PrefabRef> nativeArray3 = archetypeChunk2.GetNativeArray(ref m_PrefabRefType);
					for (int l = 0; l < nativeArray3.Length; l++)
					{
						if (nativeArray3[l].m_Prefab == prefab)
						{
							RouteNumber routeNumber = nativeArray2[l];
							if (routeNumber.m_Number <= num)
							{
								nativeBitArray.Set(routeNumber.m_Number, value: true);
							}
						}
					}
				}
				for (int m = 1; m <= num; m++)
				{
					if (!nativeBitArray.IsSet(m))
					{
						return m;
					}
				}
				nativeBitArray.Dispose();
			}
			return num + 1;
		}
	}

	[BurstCompile]
	private struct SelectVehicleJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<VehicleModel> m_VehicleModelType;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_PrefabTransportLineData;

		[ReadOnly]
		public ComponentLookup<WorkRouteData> m_PrefabWorkRouteData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		[ReadOnly]
		public WorkVehicleSelectData m_WorkVehicleSelectData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<VehicleModel> bufferAccessor = chunk.GetBufferAccessor(ref m_VehicleModelType);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			VehicleModel elem = default(VehicleModel);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<VehicleModel> dynamicBuffer = bufferAccessor[i];
				PrefabRef prefabRef = nativeArray[i];
				dynamicBuffer.Clear();
				WorkRouteData componentData2;
				if (m_PrefabTransportLineData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					PublicTransportPurpose publicTransportPurpose = (componentData.m_PassengerTransport ? PublicTransportPurpose.TransportLine : ((PublicTransportPurpose)0));
					Resource cargoResources = (Resource)(componentData.m_CargoTransport ? 8 : 0);
					int2 passengerCapacity = (componentData.m_PassengerTransport ? new int2(1, int.MaxValue) : ((int2)0));
					int2 cargoCapacity = (componentData.m_CargoTransport ? new int2(1, int.MaxValue) : ((int2)0));
					m_TransportVehicleSelectData.SelectVehicle(ref random, componentData.m_TransportType, EnergyTypes.FuelAndElectricity, componentData.m_SizeClass, publicTransportPurpose, cargoResources, out elem.m_PrimaryPrefab, out elem.m_SecondaryPrefab, ref passengerCapacity, ref cargoCapacity);
					dynamicBuffer.Add(elem);
				}
				else if (m_PrefabWorkRouteData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
				{
					VehicleModel elem2 = default(VehicleModel);
					m_WorkVehicleSelectData.SelectVehicle(ref random, componentData2.m_RoadType, componentData2.m_SizeClass, VehicleWorkType.Harvest, componentData2.m_MapFeature, Resource.NoResource, out elem2.m_PrimaryPrefab);
					dynamicBuffer.Add(elem2);
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
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<RouteNumber> __Game_Routes_RouteNumber_RW_ComponentTypeHandle;

		public BufferTypeHandle<VehicleModel> __Game_Routes_VehicleModel_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkRouteData> __Game_Prefabs_WorkRouteData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_RouteNumber_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RouteNumber>();
			__Game_Routes_VehicleModel_RW_BufferTypeHandle = state.GetBufferTypeHandle<VehicleModel>();
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_WorkRouteData_RO_ComponentLookup = state.GetComponentLookup<WorkRouteData>(isReadOnly: true);
		}
	}

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_CreatedQuery;

	private EntityQuery m_RouteQuery;

	private EntityQuery m_TransportVehiclePrefabQuery;

	private EntityQuery m_WorkVehiclePrefabQuery;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private WorkVehicleSelectData m_WorkVehicleSelectData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_WorkVehicleSelectData = new WorkVehicleSelectData(this);
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.ReadOnly<RouteNumber>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		m_RouteQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.ReadOnly<RouteNumber>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_TransportVehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_WorkVehiclePrefabQuery = GetEntityQuery(WorkVehicleSelectData.GetEntityQueryDesc());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> routeChunks = m_RouteQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_TransportVehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		m_WorkVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_WorkVehiclePrefabQuery, Allocator.TempJob, out var jobHandle2);
		AssignRouteNumbersJob jobData = new AssignRouteNumbersJob
		{
			m_RouteChunks = routeChunks,
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteNumberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_RouteNumber_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		SelectVehicleJob jobData2 = new SelectVehicleJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleModelType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_VehicleModel_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabTransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkRouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_WorkVehicleSelectData = m_WorkVehicleSelectData
		};
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		JobHandle jobHandle4 = JobChunkExtensions.ScheduleParallel(jobData2, m_CreatedQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle, jobHandle2));
		m_TransportVehicleSelectData.PostUpdate(jobHandle4);
		m_WorkVehicleSelectData.PostUpdate(jobHandle4);
		routeChunks.Dispose(jobHandle3);
		base.Dependency = JobHandle.CombineDependencies(jobHandle3, jobHandle4);
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
	public InitializeSystem()
	{
	}
}
