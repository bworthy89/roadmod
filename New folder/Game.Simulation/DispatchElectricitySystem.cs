using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DispatchElectricitySystem : GameSystemBase
{
	[BurstCompile]
	private struct DispatchElectricityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> m_BuildingConnectionType;

		public ComponentTypeHandle<ElectricityConsumer> m_ConsumerType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_NodeConnections;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

		public IconCommandBuffer m_IconCommandBuffer;

		public ElectricityParameterData m_Parameters;

		public BuildingEfficiencyParameterData m_EfficiencyParameters;

		public Entity m_SinkNode;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<ElectricityBuildingConnection> nativeArray3 = chunk.GetNativeArray(ref m_BuildingConnectionType);
			NativeArray<ElectricityConsumer> nativeArray4 = chunk.GetNativeArray(ref m_ConsumerType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref ElectricityConsumer reference = ref nativeArray4.ElementAt(i);
				ElectricityConsumerFlags flags = reference.m_Flags;
				int x = 0;
				bool beyondBottleneck = false;
				bool flag = false;
				if (nativeArray3.Length != 0)
				{
					if (nativeArray3[i].m_ConsumerEdge != Entity.Null)
					{
						ElectricityFlowEdge electricityFlowEdge = m_FlowEdges[nativeArray3[i].m_ConsumerEdge];
						x = electricityFlowEdge.m_Flow;
						beyondBottleneck = electricityFlowEdge.isBeyondBottleneck;
						flag = electricityFlowEdge.isDisconnected;
					}
					else
					{
						UnityEngine.Debug.LogError("ElectricityBuildingConnection is missing consumer edge!");
					}
				}
				else
				{
					Entity roadEdge = nativeArray2[i].m_RoadEdge;
					if (roadEdge != Entity.Null && m_NodeConnections.TryGetComponent(roadEdge, out var componentData) && ElectricityGraphUtils.TryGetFlowEdge(componentData.m_ElectricityNode, m_SinkNode, ref m_FlowConnections, ref m_FlowEdges, out ElectricityFlowEdge edge))
					{
						if (edge.m_Capacity == edge.m_Flow)
						{
							x = reference.m_WantedConsumption;
						}
						else if (edge.m_Capacity > 0)
						{
							float num = (float)edge.m_Flow / (float)edge.m_Capacity;
							x = Mathf.FloorToInt((float)reference.m_WantedConsumption * num);
						}
						flag = edge.isDisconnected;
					}
				}
				reference.m_FulfilledConsumption = math.min(x, reference.m_WantedConsumption);
				HandleCooldown(nativeArray[i], beyondBottleneck, ref reference, flags);
				if ((reference.m_WantedConsumption > 0) ? (reference.m_FulfilledConsumption >= reference.m_WantedConsumption) : (!flag))
				{
					reference.m_Flags |= ElectricityConsumerFlags.Connected;
				}
				else
				{
					reference.m_Flags &= ~ElectricityConsumerFlags.Connected;
				}
				if (bufferAccessor.Length != 0)
				{
					float efficiency = 1f - m_EfficiencyParameters.m_ElectricityPenalty * math.saturate((float)reference.m_CooldownCounter / m_EfficiencyParameters.m_ElectricityPenaltyDelay);
					BuildingUtils.SetEfficiencyFactor(bufferAccessor[i], EfficiencyFactor.ElectricitySupply, efficiency);
				}
			}
		}

		private void HandleCooldown(Entity building, bool beyondBottleneck, ref ElectricityConsumer consumer, ElectricityConsumerFlags oldFlags)
		{
			consumer.m_Flags &= ~(ElectricityConsumerFlags.NoElectricityWarning | ElectricityConsumerFlags.BottleneckWarning);
			if (consumer.m_FulfilledConsumption < consumer.m_WantedConsumption)
			{
				consumer.m_CooldownCounter = (short)math.min(consumer.m_CooldownCounter + 1, 10000);
				if (consumer.m_CooldownCounter >= kAlertCooldown)
				{
					consumer.m_Flags |= (ElectricityConsumerFlags)(beyondBottleneck ? 4 : 2);
				}
			}
			else
			{
				consumer.m_CooldownCounter = 0;
			}
			SetWarning(building, consumer, oldFlags, ElectricityConsumerFlags.NoElectricityWarning, m_Parameters.m_ElectricityNotificationPrefab);
			SetWarning(building, consumer, oldFlags, ElectricityConsumerFlags.BottleneckWarning, m_Parameters.m_BuildingBottleneckNotificationPrefab);
		}

		private void SetWarning(Entity building, ElectricityConsumer consumer, ElectricityConsumerFlags oldFlags, ElectricityConsumerFlags flag, Entity notificationPrefab)
		{
			if ((oldFlags & flag) != (consumer.m_Flags & flag))
			{
				if ((consumer.m_Flags & flag) != ElectricityConsumerFlags.None)
				{
					m_IconCommandBuffer.Add(building, notificationPrefab, IconPriority.Problem);
				}
				else
				{
					m_IconCommandBuffer.Remove(building, notificationPrefab);
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
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
		}
	}

	public static readonly short kAlertCooldown = 2;

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_ConsumerQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2129007938_0;

	private EntityQuery __query_2129007938_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 126;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_ConsumerQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_ConsumerQuery);
		RequireForUpdate<ElectricityParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ElectricityFlowSystem.ready)
		{
			DispatchElectricityJob jobData = new DispatchElectricityJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_NodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_Parameters = __query_2129007938_0.GetSingleton<ElectricityParameterData>(),
				m_EfficiencyParameters = __query_2129007938_1.GetSingleton<BuildingEfficiencyParameterData>(),
				m_SinkNode = m_ElectricityFlowSystem.sinkNode
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ConsumerQuery, base.Dependency);
			m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ElectricityParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2129007938_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2129007938_1 = entityQueryBuilder2.Build(ref state);
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
	public DispatchElectricitySystem()
	{
	}
}
