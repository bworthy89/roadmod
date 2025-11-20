#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BatteryAISystem : GameSystemBase
{
	[BurstCompile]
	private struct BatteryTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_BuildingConnectionType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Game.Buildings.Battery> m_BatteryType;

		public ComponentTypeHandle<Game.Buildings.EmergencyGenerator> m_EmergencyGeneratorType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BatteryData> m_BatteryDatas;

		[ReadOnly]
		public ComponentLookup<EmergencyGeneratorData> m_EmergencyGeneratorDatas;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ResourceConsumer> m_ResourceConsumers;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<ServiceUsage> m_ServiceUsages;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PollutionEmitModifier> m_EmitModifiers;

		public IconCommandBuffer m_IconCommandBuffer;

		public ElectricityParameterData m_ElectricityParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Game.Buildings.Battery> nativeArray3 = chunk.GetNativeArray(ref m_BatteryType);
			NativeArray<Game.Buildings.EmergencyGenerator> nativeArray4 = chunk.GetNativeArray(ref m_EmergencyGeneratorType);
			NativeArray<ElectricityBuildingConnection> nativeArray5 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity owner = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				ref Game.Buildings.Battery reference = ref nativeArray3.ElementAt(i);
				ElectricityBuildingConnection electricityBuildingConnection = nativeArray5[i];
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				if (electricityBuildingConnection.m_ChargeEdge == Entity.Null || electricityBuildingConnection.m_DischargeEdge == Entity.Null)
				{
					UnityEngine.Debug.LogError("Battery is missing charge or discharge edge!");
					continue;
				}
				m_BatteryDatas.TryGetComponent(prefab, out var componentData);
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor2[i], ref m_Prefabs, ref m_BatteryDatas);
				}
				bool flag = reference.m_StoredEnergy == 0;
				ElectricityFlowEdge value = m_FlowEdges[electricityBuildingConnection.m_DischargeEdge];
				ElectricityFlowEdge value2 = m_FlowEdges[electricityBuildingConnection.m_ChargeEdge];
				int num = value2.m_Flow - value.m_Flow;
				reference.m_StoredEnergy = math.clamp(reference.m_StoredEnergy + num, 0L, componentData.capacityTicks);
				reference.m_Capacity = componentData.m_Capacity;
				reference.m_LastFlow = num;
				bool flag2 = reference.m_StoredEnergy == 0;
				if (flag2 && !flag)
				{
					m_IconCommandBuffer.Add(owner, m_ElectricityParameterData.m_BatteryEmptyNotificationPrefab, IconPriority.Problem);
				}
				else if (!flag2 && flag)
				{
					m_IconCommandBuffer.Remove(owner, m_ElectricityParameterData.m_BatteryEmptyNotificationPrefab);
				}
				if (nativeArray4.Length != 0)
				{
					Bounds1 bounds = default(Bounds1);
					int num2 = 0;
					int num3 = 0;
					if (bufferAccessor2.Length != 0)
					{
						foreach (InstalledUpgrade item in bufferAccessor2[i])
						{
							if (!BuildingUtils.CheckOption(item, BuildingOption.Inactive) && m_EmergencyGeneratorDatas.TryGetComponent(m_Prefabs[item], out var componentData2))
							{
								bounds = new Bounds1(math.max(bounds.min, componentData2.m_ActivationThreshold.min), math.max(bounds.max, componentData2.m_ActivationThreshold.max));
								if (HasResources(item))
								{
									num2 += Mathf.CeilToInt(efficiency * (float)componentData2.m_ElectricityProduction);
									num3 += componentData2.m_ElectricityProduction;
								}
							}
						}
					}
					ref Game.Buildings.EmergencyGenerator reference2 = ref nativeArray4.ElementAt(i);
					float num4 = (float)reference.m_StoredEnergy / (float)math.max(1L, componentData.capacityTicks);
					bool flag3 = reference2.m_Production > 0;
					bool flag4 = efficiency > 0f && (num4 < bounds.min || (flag3 && num4 < bounds.max));
					reference2.m_Production = (flag4 ? math.min(num2, (int)(componentData.capacityTicks - reference.m_StoredEnergy)) : 0);
					float num5 = ((num3 > 0) ? ((float)reference2.m_Production / (float)num3) : 0f);
					if (bufferAccessor2.Length != 0)
					{
						foreach (InstalledUpgrade item2 in bufferAccessor2[i])
						{
							if (m_EmergencyGeneratorDatas.HasComponent(m_Prefabs[item2]) && m_ServiceUsages.HasComponent(item2))
							{
								bool flag5 = !BuildingUtils.CheckOption(item2, BuildingOption.Inactive);
								if (m_EmitModifiers.HasComponent(item2))
								{
									PollutionEmitModifier value3 = m_EmitModifiers[item2];
									value3.m_GroundPollutionModifier = ((!(flag5 && flag4)) ? (-1) : 0);
									value3.m_AirPollutionModifier = ((!(flag5 && flag4)) ? (-1) : 0);
									value3.m_NoisePollutionModifier = ((!(flag5 && flag4)) ? (-1) : 0);
									m_EmitModifiers[item2] = value3;
								}
								if (flag5)
								{
									m_ServiceUsages[item2] = new ServiceUsage
									{
										m_Usage = (HasResources(item2) ? num5 : 0f)
									};
								}
							}
						}
					}
					Assert.IsTrue(reference2.m_Production >= 0);
					reference.m_StoredEnergy += reference2.m_Production;
				}
				value.m_Capacity = (int)((efficiency > 0f) ? math.min(componentData.m_PowerOutput, reference.m_StoredEnergy) : 0);
				m_FlowEdges[electricityBuildingConnection.m_DischargeEdge] = value;
				value2.m_Capacity = (int)math.min(Mathf.RoundToInt(efficiency * (float)componentData.m_PowerOutput), componentData.capacityTicks - reference.m_StoredEnergy);
				m_FlowEdges[electricityBuildingConnection.m_ChargeEdge] = value2;
			}
		}

		private bool HasResources(Entity upgrade)
		{
			if (m_ResourceConsumers.TryGetComponent(upgrade, out var componentData))
			{
				return componentData.m_ResourceAvailability > 0;
			}
			return true;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.Battery> __Game_Buildings_Battery_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.EmergencyGenerator> __Game_Buildings_EmergencyGenerator_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BatteryData> __Game_Prefabs_BatteryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EmergencyGeneratorData> __Game_Prefabs_EmergencyGeneratorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RO_ComponentLookup;

		public ComponentLookup<ServiceUsage> __Game_Buildings_ServiceUsage_RW_ComponentLookup;

		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup;

		public ComponentLookup<PollutionEmitModifier> __Game_Buildings_PollutionEmitModifier_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Battery_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Battery>();
			__Game_Buildings_EmergencyGenerator_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.EmergencyGenerator>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BatteryData_RO_ComponentLookup = state.GetComponentLookup<BatteryData>(isReadOnly: true);
			__Game_Prefabs_EmergencyGeneratorData_RO_ComponentLookup = state.GetComponentLookup<EmergencyGeneratorData>(isReadOnly: true);
			__Game_Buildings_ResourceConsumer_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ResourceConsumer>(isReadOnly: true);
			__Game_Buildings_ServiceUsage_RW_ComponentLookup = state.GetComponentLookup<ServiceUsage>();
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>();
			__Game_Buildings_PollutionEmitModifier_RW_ComponentLookup = state.GetComponentLookup<PollutionEmitModifier>();
		}
	}

	private EntityQuery m_BatteryQuery;

	private EntityQuery m_SettingsQuery;

	private IconCommandSystem m_IconCommandSystem;

	private TypeHandle __TypeHandle;

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
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_BatteryQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Battery>(), ComponentType.ReadOnly<ElectricityBuildingConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>());
		RequireForUpdate(m_BatteryQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		BatteryTickJob jobData = new BatteryTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Battery_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmergencyGeneratorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_EmergencyGenerator_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BatteryDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BatteryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EmergencyGeneratorDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EmergencyGeneratorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUsages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUsage_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_EmitModifiers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PollutionEmitModifier_RW_ComponentLookup, ref base.CheckedStateRef),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_ElectricityParameterData = m_SettingsQuery.GetSingleton<ElectricityParameterData>()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BatteryQuery, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public BatteryAISystem()
	{
	}
}
