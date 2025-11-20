#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
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
public class AdjustElectricityConsumptionSystem : GameSystemBase
{
	[BurstCompile]
	public struct AdjustElectricityConsumptionJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CityServiceUpkeep> m_CityServiceType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_UpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

		[ReadOnly]
		public ComponentTypeHandle<StorageProperty> m_StoragePropertyType;

		public ComponentTypeHandle<ElectricityConsumer> m_ConsumerType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ServiceConsumption;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_Fees;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public NativeQueue<Entity>.ParallelWriter m_UpdatedEdges;

		public ServiceFeeParameterData m_FeeParameters;

		public BuildingEfficiencyParameterData m_EfficiencyParameters;

		public RandomSeed m_RandomSeed;

		public Entity m_City;

		public float m_TemperatureMultiplier;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<CurrentDistrict> nativeArray3 = chunk.GetNativeArray(ref m_CurrentDistrictType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_UpgradeType);
			NativeArray<ElectricityBuildingConnection> nativeArray4 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RenterType);
			NativeArray<ElectricityConsumer> nativeArray5 = chunk.GetNativeArray(ref m_ConsumerType);
			BufferAccessor<Efficiency> bufferAccessor3 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			bool flag = chunk.Has(ref m_ParkType);
			bool flag2 = chunk.Has(ref m_StoragePropertyType);
			Random random = m_RandomSeed.GetRandom(1 + unfilteredChunkIndex);
			float num;
			float efficiency;
			if (chunk.Has(ref m_CityServiceType))
			{
				num = 1f;
				efficiency = 1f;
			}
			else
			{
				float relativeFee = ServiceFeeSystem.GetFee(PlayerResource.Electricity, m_Fees[m_City]) / m_FeeParameters.m_ElectricityFee.m_Default;
				num = GetFeeConsumptionMultiplier(relativeFee, in m_FeeParameters);
				efficiency = GetFeeEfficiencyFactor(relativeFee, in m_EfficiencyParameters);
			}
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				m_ServiceConsumption.TryGetComponent(prefab, out var componentData);
				if (bufferAccessor.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor[i], ref m_Prefabs, ref m_ServiceConsumption);
				}
				float electricityConsumption = componentData.m_ElectricityConsumption;
				electricityConsumption *= m_TemperatureMultiplier;
				electricityConsumption *= num;
				if (nativeArray3.Length != 0)
				{
					Entity district = nativeArray3[i].m_District;
					if (m_DistrictModifiers.TryGetBuffer(district, out var bufferData))
					{
						AreaUtils.ApplyModifier(ref electricityConsumption, bufferData, DistrictModifierType.EnergyConsumptionAwareness);
					}
				}
				if (!flag && !flag2 && bufferAccessor2.Length != 0)
				{
					bool flag3 = electricityConsumption > 0f;
					electricityConsumption *= FlowUtils.GetRenterConsumptionMultiplier(prefab, bufferAccessor2[i], ref m_HouseholdCitizens, ref m_Employees, ref m_Citizens, ref m_SpawnableDatas);
					electricityConsumption = math.select(electricityConsumption, 1f, flag3 && electricityConsumption < 1f);
				}
				else
				{
					electricityConsumption = math.select(electricityConsumption, 1f, electricityConsumption > 0f && electricityConsumption < 1f);
				}
				ref ElectricityConsumer reference = ref nativeArray5.ElementAt(i);
				int num2 = ((electricityConsumption > 0f) ? MathUtils.RoundToIntRandom(ref random, electricityConsumption) : 0);
				if (BuildingUtils.CheckOption(nativeArray2[i], BuildingOption.Inactive))
				{
					num2 /= 10;
				}
				if (num2 != reference.m_WantedConsumption)
				{
					reference.m_WantedConsumption = num2;
					if (nativeArray2[i].m_RoadEdge != Entity.Null)
					{
						m_UpdatedEdges.Enqueue(nativeArray2[i].m_RoadEdge);
					}
					if (nativeArray4.Length != 0)
					{
						Entity consumerEdge = nativeArray4[i].m_ConsumerEdge;
						if (consumerEdge != Entity.Null)
						{
							ElectricityFlowEdge value = m_FlowEdges[consumerEdge];
							value.m_Capacity = num2;
							m_FlowEdges[consumerEdge] = value;
						}
					}
				}
				if (bufferAccessor3.Length != 0)
				{
					BuildingUtils.SetEfficiencyFactor(bufferAccessor3[i], EfficiencyFactor.ElectricityFee, efficiency);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateEdgesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_NodeConnections;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_Consumers;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> m_BuildingConnections;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		public NativeQueue<Entity> m_UpdatedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public Entity m_SinkNode;

		public void Execute()
		{
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(m_UpdatedEdges.Count, Allocator.Temp);
			Entity item;
			while (m_UpdatedEdges.TryDequeue(out item))
			{
				if (nativeParallelHashSet.Add(item) && m_NodeConnections.TryGetComponent(item, out var componentData) && ElectricityGraphUtils.TryGetFlowEdge(componentData.m_ElectricityNode, m_SinkNode, ref m_FlowConnections, ref m_FlowEdges, out var entity, out var edge))
				{
					edge.m_Capacity = GetNonConnectedBuildingConsumption(item);
					m_FlowEdges[entity] = edge;
				}
			}
		}

		private int GetNonConnectedBuildingConsumption(Entity roadEdge)
		{
			int num = 0;
			if (m_ConnectedBuildings.TryGetBuffer(roadEdge, out var bufferData))
			{
				foreach (ConnectedBuilding item in bufferData)
				{
					if (m_Consumers.TryGetComponent(item.m_Building, out var componentData) && !m_BuildingConnections.HasComponent(item.m_Building))
					{
						num += componentData.m_WantedConsumption;
					}
				}
			}
			return num;
		}
	}

	private struct TypeHandle
	{
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StorageProperty> __Game_Buildings_StorageProperty_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CityServiceUpkeep>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_StorageProperty_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StorageProperty>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>();
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
		}
	}

	private const int kFullUpdatesPerDay = 128;

	private ClimateSystem m_ClimateSystem;

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private EntityQuery m_ConsumerQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_653552652_0;

	private EntityQuery __query_653552652_1;

	private EntityQuery __query_653552652_2;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 0;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		Assert.IsTrue(GetUpdateInterval(SystemUpdatePhase.GameSimulation) >= 128);
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_ConsumerQuery = GetEntityQuery(ComponentType.ReadWrite<ElectricityConsumer>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_ConsumerQuery);
		RequireForUpdate<ServiceFeeParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Entity> updatedEdges = new NativeQueue<Entity>(Allocator.TempJob);
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 128, 16);
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new AdjustElectricityConsumptionJob
		{
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CityServiceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoragePropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_StorageProperty_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceConsumption = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Fees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedEdges = updatedEdges.AsParallelWriter(),
			m_FeeParameters = __query_653552652_0.GetSingleton<ServiceFeeParameterData>(),
			m_EfficiencyParameters = __query_653552652_1.GetSingleton<BuildingEfficiencyParameterData>(),
			m_RandomSeed = RandomSeed.Next(),
			m_City = m_CitySystem.City,
			m_TemperatureMultiplier = GetTemperatureMultiplier(m_ClimateSystem.temperature),
			m_UpdateFrameIndex = updateFrame
		}, m_ConsumerQuery, base.Dependency);
		UpdateEdgesJob jobData = new UpdateEdgesJob
		{
			m_NodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Consumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedEdges = updatedEdges,
			m_SinkNode = m_ElectricityFlowSystem.sinkNode
		};
		base.Dependency = IJobExtensions.Schedule(jobData, dependsOn);
		updatedEdges.Dispose(base.Dependency);
	}

	public float GetTemperatureMultiplier(float temperature)
	{
		if (!__query_653552652_2.TryGetSingleton<ElectricityParameterData>(out var value))
		{
			return 1f;
		}
		return value.m_TemperatureConsumptionMultiplier.Evaluate(temperature);
	}

	public static float GetFeeConsumptionMultiplier(float relativeFee, in ServiceFeeParameterData feeParameters)
	{
		return feeParameters.m_ElectricityFeeConsumptionMultiplier.Evaluate(relativeFee);
	}

	public static float GetFeeEfficiencyFactor(float relativeFee, in BuildingEfficiencyParameterData efficiencyParameters)
	{
		return efficiencyParameters.m_ElectricityFeeFactor.Evaluate(relativeFee);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_653552652_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_653552652_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<ElectricityParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_653552652_2 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AdjustElectricityConsumptionSystem()
	{
	}
}
