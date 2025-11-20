using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PowerPlantAISystem : GameSystemBase
{
	[BurstCompile]
	private struct PowerPlantTickJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> m_ResourceConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPowered> m_WaterPoweredType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> m_SubNetType;

		public ComponentTypeHandle<ElectricityProducer> m_ElectricityProducerType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		public ComponentTypeHandle<ServiceUsage> m_ServiceUsageType;

		public ComponentTypeHandle<PointOfInterest> m_PointOfInterestType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<PowerPlantData> m_PowerPlantDatas;

		[ReadOnly]
		public ComponentLookup<GarbagePoweredData> m_GarbagePoweredData;

		[ReadOnly]
		public ComponentLookup<WindPoweredData> m_WindPoweredData;

		[ReadOnly]
		public ComponentLookup<WaterPoweredData> m_WaterPoweredData;

		[ReadOnly]
		public ComponentLookup<SolarPoweredData> m_SolarPoweredData;

		[ReadOnly]
		public ComponentLookup<GroundWaterPoweredData> m_GroundWaterPoweredData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableNetData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ResourceConsumer> m_ResourceConsumers;

		[ReadOnly]
		public ComponentLookup<Curve> m_Curves;

		[ReadOnly]
		public ComponentLookup<Composition> m_Compositions;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<ServiceUsage> m_ServiceUsages;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		[ReadOnly]
		public NativeArray<Wind> m_WindMap;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		public float m_SunLight;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Game.Buildings.GarbageFacility> nativeArray2 = chunk.GetNativeArray(ref m_GarbageFacilityType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			NativeArray<ElectricityBuildingConnection> nativeArray3 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			NativeArray<ElectricityProducer> nativeArray4 = chunk.GetNativeArray(ref m_ElectricityProducerType);
			NativeArray<Game.Buildings.WaterPowered> nativeArray5 = chunk.GetNativeArray(ref m_WaterPoweredType);
			NativeArray<Game.Objects.Transform> nativeArray6 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<Game.Net.SubNet> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubNetType);
			NativeArray<Game.Buildings.ResourceConsumer> nativeArray7 = chunk.GetNativeArray(ref m_ResourceConsumerType);
			BufferAccessor<Efficiency> bufferAccessor3 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<ServiceUsage> nativeArray8 = chunk.GetNativeArray(ref m_ServiceUsageType);
			NativeArray<PointOfInterest> nativeArray9 = chunk.GetNativeArray(ref m_PointOfInterestType);
			Span<float> factors = stackalloc float[32];
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				ref ElectricityProducer reference = ref nativeArray4.ElementAt(i);
				ElectricityBuildingConnection electricityBuildingConnection = nativeArray3[i];
				byte b = ((nativeArray7.Length != 0) ? nativeArray7[i].m_ResourceAvailability : byte.MaxValue);
				Game.Objects.Transform transform = nativeArray6[i];
				if (bufferAccessor3.Length != 0)
				{
					BuildingUtils.GetEfficiencyFactors(bufferAccessor3[i], factors);
					factors[17] = 1f;
					factors[18] = 1f;
					factors[19] = 1f;
					factors[20] = 1f;
				}
				else
				{
					factors.Fill(1f);
				}
				float efficiency = BuildingUtils.GetEfficiency(factors);
				if (electricityBuildingConnection.m_ProducerEdge == Entity.Null)
				{
					UnityEngine.Debug.LogError("PowerPlant is missing producer edge!");
					continue;
				}
				ElectricityFlowEdge value = m_FlowEdges[electricityBuildingConnection.m_ProducerEdge];
				reference.m_LastProduction = value.m_Flow;
				float num = ((reference.m_Capacity > 0) ? ((float)reference.m_LastProduction / (float)reference.m_Capacity) : 0f);
				if (nativeArray8.Length != 0)
				{
					nativeArray8[i] = new ServiceUsage
					{
						m_Usage = ((b > 0) ? num : 0f)
					};
				}
				if (bufferAccessor.Length != 0)
				{
					foreach (InstalledUpgrade item in bufferAccessor[i])
					{
						if (!BuildingUtils.CheckOption(item, BuildingOption.Inactive) && m_PowerPlantDatas.HasComponent(item) && m_ServiceUsages.HasComponent(item))
						{
							Game.Buildings.ResourceConsumer componentData;
							byte b2 = (m_ResourceConsumers.TryGetComponent(item.m_Upgrade, out componentData) ? componentData.m_ResourceAvailability : b);
							m_ServiceUsages[item] = new ServiceUsage
							{
								m_Usage = ((b2 > 0) ? num : 0f)
							};
						}
					}
				}
				float2 zero = float2.zero;
				if (m_PowerPlantDatas.TryGetComponent(prefab, out var componentData2))
				{
					zero += GetPowerPlantProduction(componentData2, b, efficiency);
				}
				if (bufferAccessor.Length != 0)
				{
					foreach (InstalledUpgrade item2 in bufferAccessor[i])
					{
						if (!BuildingUtils.CheckOption(item2, BuildingOption.Inactive) && m_PowerPlantDatas.TryGetComponent(m_Prefabs[item2.m_Upgrade], out componentData2))
						{
							Game.Buildings.ResourceConsumer componentData3;
							byte resourceAvailability = (m_ResourceConsumers.TryGetComponent(item2.m_Upgrade, out componentData3) ? componentData3.m_ResourceAvailability : b);
							zero += GetPowerPlantProduction(componentData2, resourceAvailability, efficiency);
						}
					}
				}
				m_GarbagePoweredData.TryGetComponent(prefab, out var componentData4);
				m_WindPoweredData.TryGetComponent(prefab, out var componentData5);
				m_WaterPoweredData.TryGetComponent(prefab, out var componentData6);
				m_SolarPoweredData.TryGetComponent(prefab, out var componentData7);
				m_GroundWaterPoweredData.TryGetComponent(prefab, out var componentData8);
				if (bufferAccessor.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData4, bufferAccessor[i], ref m_Prefabs, ref m_GarbagePoweredData);
					UpgradeUtils.CombineStats(ref componentData5, bufferAccessor[i], ref m_Prefabs, ref m_WindPoweredData);
					UpgradeUtils.CombineStats(ref componentData6, bufferAccessor[i], ref m_Prefabs, ref m_WaterPoweredData);
					UpgradeUtils.CombineStats(ref componentData7, bufferAccessor[i], ref m_Prefabs, ref m_SolarPoweredData);
					UpgradeUtils.CombineStats(ref componentData8, bufferAccessor[i], ref m_Prefabs, ref m_GroundWaterPoweredData);
				}
				float2 @float = float2.zero;
				if (componentData4.m_Capacity > 0 && nativeArray2.Length != 0)
				{
					@float = GetGarbageProduction(componentData4, nativeArray2[i]);
				}
				float2 float2 = float2.zero;
				if (componentData5.m_Production > 0)
				{
					Wind wind = WindSystem.GetWind(nativeArray6[i].m_Position, m_WindMap);
					float2 = GetWindProduction(componentData5, wind, efficiency);
					if (float2.x > 0f && nativeArray9.Length != 0 && math.any(wind.m_Wind))
					{
						ref PointOfInterest reference2 = ref nativeArray9.ElementAt(i);
						reference2.m_Position = transform.m_Position;
						reference2.m_Position.xz -= wind.m_Wind;
						reference2.m_IsValid = true;
					}
				}
				float2 zero2 = float2.zero;
				if (nativeArray5.Length != 0 && bufferAccessor2.Length != 0 && componentData6.m_ProductionFactor > 0f)
				{
					zero2 += GetWaterProduction(componentData6, nativeArray5[i], bufferAccessor2[i], efficiency);
				}
				if (componentData8.m_Production > 0 && componentData8.m_MaximumGroundWater > 0)
				{
					zero2 += GetGroundWaterProduction(componentData8, nativeArray6[i].m_Position, efficiency, m_GroundWaterMap);
				}
				float2 float3 = float2.zero;
				if (componentData7.m_Production > 0)
				{
					float3 = GetSolarProduction(componentData7, efficiency);
				}
				float2 float4 = math.round(zero + @float + float2 + zero2 + float3);
				value.m_Capacity = (reference.m_Capacity = (int)float4.x);
				m_FlowEdges[electricityBuildingConnection.m_ProducerEdge] = value;
				if (bufferAccessor3.Length != 0)
				{
					if (float4.y > 0f)
					{
						float targetEfficiency = float4.x / float4.y;
						float4 weights = new float4(zero.y - zero.x, float2.y - float2.x, zero2.y - zero2.x, float3.y - float3.x);
						float4 float5 = BuildingUtils.ApproximateEfficiencyFactors(targetEfficiency, weights);
						factors[17] = float5.x;
						factors[18] = float5.y;
						factors[19] = float5.z;
						factors[20] = float5.w;
					}
					BuildingUtils.SetEfficiencyFactors(bufferAccessor3[i], factors);
				}
			}
		}

		private static float2 GetPowerPlantProduction(PowerPlantData powerPlantData, byte resourceAvailability, float efficiency)
		{
			float num = efficiency * (float)powerPlantData.m_ElectricityProduction;
			return new float2((resourceAvailability > 0) ? num : 0f, num);
		}

		private static float GetGarbageProduction(GarbagePoweredData garbageData, Game.Buildings.GarbageFacility garbageFacility)
		{
			return math.clamp((float)garbageFacility.m_ProcessingRate / garbageData.m_ProductionPerUnit, 0f, garbageData.m_Capacity);
		}

		private float2 GetWaterProduction(WaterPoweredData waterData, Game.Buildings.WaterPowered waterPowered, DynamicBuffer<Game.Net.SubNet> subNets, float efficiency)
		{
			float num = 0f;
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				PrefabRef prefabRef = m_Prefabs[subNet];
				if (m_Curves.TryGetComponent(subNet, out var componentData) && m_Compositions.TryGetComponent(subNet, out var componentData2) && m_PlaceableNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData3) && m_NetCompositionData.TryGetComponent(componentData2.m_Edge, out var componentData4) && (componentData3.m_PlacementFlags & (Game.Net.PlacementFlags.FlowLeft | Game.Net.PlacementFlags.FlowRight)) != Game.Net.PlacementFlags.None && (componentData4.m_Flags.m_General & (CompositionFlags.General.Spillway | CompositionFlags.General.Front | CompositionFlags.General.Back)) == 0)
				{
					num += GetWaterProduction(waterData, componentData, componentData3, componentData4, m_TerrainHeightData, m_WaterSurfaceData);
				}
			}
			float num2 = efficiency * GetWaterCapacity(waterPowered, waterData);
			return new float2(math.clamp(efficiency * num, 0f, num2), num2);
		}

		private float GetWaterProduction(WaterPoweredData waterData, Curve curve, PlaceableNetData placeableData, NetCompositionData compositionData, TerrainHeightData terrainHeightData, WaterSurfaceData<SurfaceWater> waterSurfaceData)
		{
			int num = math.max(1, (int)math.round(curve.m_Length * waterSurfaceData.scale.x));
			bool test = (placeableData.m_PlacementFlags & Game.Net.PlacementFlags.FlowLeft) != 0;
			float num2 = 0f;
			for (int i = 0; i < num; i++)
			{
				float t = ((float)i + 0.5f) / (float)num;
				float3 @float = MathUtils.Position(curve.m_Bezier, t);
				float3 float2 = MathUtils.Tangent(curve.m_Bezier, t);
				float2 float3 = math.normalizesafe(math.select(MathUtils.Right(float2.xz), MathUtils.Left(float2.xz), test));
				float3 worldPosition = @float;
				float3 worldPosition2 = @float;
				worldPosition.xz -= float3 * (compositionData.m_Width * 0.5f);
				worldPosition2.xz += float3 * (compositionData.m_Width * 0.5f);
				float waterDepth;
				float num3 = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, worldPosition, out waterDepth);
				float waterDepth2;
				float num4 = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, worldPosition2, out waterDepth2);
				float2 x = WaterUtils.SampleVelocity(ref waterSurfaceData, worldPosition);
				float2 x2 = WaterUtils.SampleVelocity(ref waterSurfaceData, worldPosition2);
				if (num3 > worldPosition.y)
				{
					waterDepth = math.max(0f, waterDepth - (num3 - worldPosition.y));
					num3 = worldPosition.y;
				}
				num2 += (math.dot(x, float3) * waterDepth + math.dot(x2, float3) * waterDepth2) * 0.5f * math.max(0f, num3 - num4);
			}
			return num2 * waterData.m_ProductionFactor * curve.m_Length / (float)num;
		}

		private float2 GetSolarProduction(SolarPoweredData solarData, float efficiency)
		{
			float num = efficiency * (float)solarData.m_Production;
			return new float2(math.clamp(num * m_SunLight, 0f, num), num);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.WaterPowered> __Game_Buildings_WaterPowered_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		public ComponentTypeHandle<ElectricityProducer> __Game_Buildings_ElectricityProducer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		public ComponentTypeHandle<ServiceUsage> __Game_Buildings_ServiceUsage_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PowerPlantData> __Game_Prefabs_PowerPlantData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbagePoweredData> __Game_Prefabs_GarbagePoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WindPoweredData> __Game_Prefabs_WindPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SolarPoweredData> __Game_Prefabs_SolarPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroundWaterPoweredData> __Game_Prefabs_GroundWaterPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		public ComponentLookup<ServiceUsage> __Game_Buildings_ServiceUsage_RW_ComponentLookup;

		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResourceConsumer>(isReadOnly: true);
			__Game_Buildings_WaterPowered_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.WaterPowered>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Buildings_ElectricityProducer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityProducer>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUsage>();
			__Game_Common_PointOfInterest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PointOfInterest>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PowerPlantData_RO_ComponentLookup = state.GetComponentLookup<PowerPlantData>(isReadOnly: true);
			__Game_Prefabs_GarbagePoweredData_RO_ComponentLookup = state.GetComponentLookup<GarbagePoweredData>(isReadOnly: true);
			__Game_Prefabs_WindPoweredData_RO_ComponentLookup = state.GetComponentLookup<WindPoweredData>(isReadOnly: true);
			__Game_Prefabs_WaterPoweredData_RO_ComponentLookup = state.GetComponentLookup<WaterPoweredData>(isReadOnly: true);
			__Game_Prefabs_SolarPoweredData_RO_ComponentLookup = state.GetComponentLookup<SolarPoweredData>(isReadOnly: true);
			__Game_Prefabs_GroundWaterPoweredData_RO_ComponentLookup = state.GetComponentLookup<GroundWaterPoweredData>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Buildings_ResourceConsumer_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ResourceConsumer>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Buildings_ServiceUsage_RW_ComponentLookup = state.GetComponentLookup<ServiceUsage>();
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>();
		}
	}

	public const int MAX_WATERPOWERED_SIZE = 1000000;

	private PlanetarySystem m_PlanetarySystem;

	private WindSystem m_WindSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private ClimateSystem m_ClimateSystem;

	private EntityQuery m_PowerPlantQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_833752410_0;

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
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_ClimateSystem = base.World.GetExistingSystemManaged<ClimateSystem>();
		m_PowerPlantQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityProducer>(), ComponentType.ReadOnly<ElectricityBuildingConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_PowerPlantQuery);
		RequireForUpdate<ElectricityParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ElectricityParameterData singleton = __query_833752410_0.GetSingleton<ElectricityParameterData>();
		PlanetarySystem.LightData sunLight = m_PlanetarySystem.SunLight;
		float num = 0f;
		if (sunLight.isValid)
		{
			num = math.max(0f, 0f - sunLight.transform.forward.y) * sunLight.additionalData.intensity / 110000f;
		}
		num *= math.lerp(1f, 1f - singleton.m_CloudinessSolarPenalty, m_ClimateSystem.cloudiness.value);
		JobHandle dependencies;
		JobHandle deps;
		JobHandle dependencies2;
		PowerPlantTickJob jobData = new PowerPlantTickJob
		{
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GarbageFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPoweredType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPowered_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ElectricityProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceUsageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PointOfInterestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PowerPlantDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PowerPlantData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbagePoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbagePoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WindPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WindPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SolarPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SolarPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroundWaterPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GroundWaterPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Curves = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Compositions = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUsages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUsage_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WindMap = m_WindSystem.GetMap(readOnly: true, out dependencies),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetVelocitiesSurfaceData(out deps),
			m_GroundWaterMap = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies2),
			m_SunLight = num
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PowerPlantQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, deps, dependencies2));
		m_WindSystem.AddReader(base.Dependency);
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_WaterSystem.AddVelocitySurfaceReader(base.Dependency);
		m_GroundWaterSystem.AddReader(base.Dependency);
	}

	public static float2 GetWindProduction(WindPoweredData windData, Wind wind, float efficiency)
	{
		float num = efficiency * (float)windData.m_Production;
		float x = math.lengthsq(wind.m_Wind) / (windData.m_MaximumWind * windData.m_MaximumWind);
		return new float2(num * math.saturate(math.pow(x, 1.5f)), num);
	}

	public static float GetWaterCapacity(Game.Buildings.WaterPowered waterPowered, WaterPoweredData waterData)
	{
		return math.min(waterPowered.m_Length * waterPowered.m_Height, 1000000f) * waterData.m_CapacityFactor;
	}

	public static float2 GetGroundWaterProduction(GroundWaterPoweredData groundWaterData, float3 position, float efficiency, NativeArray<GroundWater> groundWaterMap)
	{
		float num = (float)GroundWaterSystem.GetGroundWater(position, groundWaterMap).m_Amount / (float)groundWaterData.m_MaximumGroundWater;
		float num2 = efficiency * (float)groundWaterData.m_Production;
		return new float2(math.clamp(num2 * num, 0f, num2), num2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ElectricityParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_833752410_0 = entityQueryBuilder2.Build(ref state);
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
	public PowerPlantAISystem()
	{
	}
}
