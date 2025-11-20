#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class StreetLightSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateStreetLightsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityNodeConnection> m_ElectricityNodeConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> m_ElectricityConsumerType;

		public ComponentTypeHandle<Road> m_RoadType;

		public ComponentTypeHandle<Building> m_BuildingType;

		public ComponentTypeHandle<Watercraft> m_WatercraftType;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_ElectricityFlowEdges;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<StreetLight> m_StreetLightData;

		[ReadOnly]
		public int m_Brightness;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityTypeHandle);
			NativeArray<Road> nativeArray2 = chunk.GetNativeArray(ref m_RoadType);
			NativeArray<ElectricityNodeConnection> nativeArray3 = chunk.GetNativeArray(ref m_ElectricityNodeConnectionType);
			NativeArray<Building> nativeArray4 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<ElectricityConsumer> nativeArray5 = chunk.GetNativeArray(ref m_ElectricityConsumerType);
			NativeArray<Watercraft> nativeArray6 = chunk.GetNativeArray(ref m_WatercraftType);
			NativeArray<PseudoRandomSeed> nativeArray7 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				ref Road reference = ref nativeArray2.ElementAt(i);
				if ((reference.m_Flags & RoadFlags.IsLit) == 0)
				{
					continue;
				}
				Unity.Mathematics.Random random = nativeArray7[i].GetRandom(PseudoRandomSeed.kBrightnessLimit);
				bool flag = IsElectricityConnected(nativeArray3, i) && (m_Brightness < random.NextInt(200, 300) || (reference.m_Flags & RoadFlags.AlwaysLit) != 0);
				if (flag == ((reference.m_Flags & RoadFlags.LightsOff) != 0))
				{
					if (flag)
					{
						reference.m_Flags &= ~RoadFlags.LightsOff;
					}
					else
					{
						reference.m_Flags |= RoadFlags.LightsOff;
					}
					DynamicBuffer<SubObject> subObjects = bufferAccessor[i];
					UpdateStreetLightObjects(unfilteredChunkIndex, subObjects, reference);
				}
			}
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				ref Building reference2 = ref nativeArray4.ElementAt(j);
				Unity.Mathematics.Random random2 = nativeArray7[j].GetRandom(PseudoRandomSeed.kBrightnessLimit);
				bool flag2 = IsElectricityConnected(nativeArray5, j, in reference2);
				bool flag3 = flag2 && m_Brightness < random2.NextInt(200, 300);
				if (flag3 == ((reference2.m_Flags & BuildingFlags.StreetLightsOff) != 0))
				{
					if (flag3)
					{
						reference2.m_Flags &= ~BuildingFlags.StreetLightsOff;
					}
					else
					{
						reference2.m_Flags |= BuildingFlags.StreetLightsOff;
					}
					DynamicBuffer<SubObject> subObjects2 = bufferAccessor[j];
					UpdateStreetLightObjects(unfilteredChunkIndex, subObjects2, reference2);
				}
				if (flag2 != ((reference2.m_Flags & BuildingFlags.Illuminated) != 0))
				{
					if (flag2)
					{
						reference2.m_Flags |= BuildingFlags.Illuminated;
					}
					else
					{
						reference2.m_Flags &= ~BuildingFlags.Illuminated;
					}
					m_CommandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, nativeArray[j]);
				}
			}
			for (int k = 0; k < nativeArray6.Length; k++)
			{
				ref Watercraft reference3 = ref nativeArray6.ElementAt(k);
				Unity.Mathematics.Random random3 = nativeArray7[k].GetRandom(PseudoRandomSeed.kBrightnessLimit);
				bool flag4 = (m_Brightness < random3.NextInt(200, 300)) & ((reference3.m_Flags & WatercraftFlags.DeckLights) != 0);
				if (flag4 == ((reference3.m_Flags & WatercraftFlags.LightsOff) != 0))
				{
					if (flag4)
					{
						reference3.m_Flags &= ~WatercraftFlags.LightsOff;
					}
					else
					{
						reference3.m_Flags |= WatercraftFlags.LightsOff;
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[k], default(EffectsUpdated));
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<SubObject> subObjects3 = bufferAccessor[k];
						UpdateStreetLightObjects(unfilteredChunkIndex, subObjects3, reference3);
					}
				}
			}
		}

		private bool IsElectricityConnected(NativeArray<ElectricityNodeConnection> nodeConnections, int i)
		{
			if (nodeConnections.Length == 0)
			{
				return true;
			}
			return IsElectricityConnected(nodeConnections[i]);
		}

		private bool IsElectricityConnected(in ElectricityNodeConnection nodeConnection)
		{
			Entity electricityNode = nodeConnection.m_ElectricityNode;
			DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = m_ConnectedFlowEdges[electricityNode];
			bool flag = false;
			foreach (ConnectedFlowEdge item in dynamicBuffer)
			{
				flag |= m_ElectricityFlowEdges[item].isDisconnected;
			}
			return !flag;
		}

		private bool IsElectricityConnected(NativeArray<ElectricityConsumer> consumers, int i, in Building building)
		{
			if (consumers.Length != 0)
			{
				return consumers[i].electricityConnected;
			}
			if (m_ElectricityNodeConnections.TryGetComponent(building.m_RoadEdge, out var componentData))
			{
				return IsElectricityConnected(in componentData);
			}
			return true;
		}

		private void UpdateStreetLightObjects(int jobIndex, DynamicBuffer<SubObject> subObjects, Road road)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_StreetLightData.TryGetComponent(subObject, out var componentData))
				{
					UpdateStreetLightState(ref componentData, road);
					m_StreetLightData[subObject] = componentData;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(EffectsUpdated));
				}
			}
		}

		private void UpdateStreetLightObjects(int jobIndex, DynamicBuffer<SubObject> subObjects, Building building)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_StreetLightData.TryGetComponent(subObject, out var componentData))
				{
					UpdateStreetLightState(ref componentData, building);
					m_StreetLightData[subObject] = componentData;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(EffectsUpdated));
				}
				if (m_SubObjects.TryGetBuffer(subObject, out var bufferData))
				{
					UpdateStreetLightObjects(jobIndex, bufferData, building);
				}
			}
		}

		private void UpdateStreetLightObjects(int jobIndex, DynamicBuffer<SubObject> subObjects, Watercraft watercraft)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_StreetLightData.TryGetComponent(subObject, out var componentData))
				{
					UpdateStreetLightState(ref componentData, watercraft);
					m_StreetLightData[subObject] = componentData;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(EffectsUpdated));
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
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Road> __Game_Net_Road_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Building> __Game_Buildings_Building_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Watercraft> __Game_Vehicles_Watercraft_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		public ComponentLookup<StreetLight> __Game_Objects_StreetLight_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>(isReadOnly: true);
			__Game_Net_Road_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Road>();
			__Game_Buildings_Building_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Building>();
			__Game_Vehicles_Watercraft_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Watercraft>();
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Objects_StreetLight_RW_ComponentLookup = state.GetComponentLookup<StreetLight>();
		}
	}

	private const uint UPDATE_INTERVAL = 256u;

	private SimulationSystem m_SimulationSystem;

	private LightingSystem m_LightingSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_StreetLightQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_StreetLightQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<UpdateFrame>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Road>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Watercraft>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_StreetLightQuery);
		Assert.AreEqual(16, 16);
		Assert.AreEqual(16, 16);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_StreetLightQuery.ResetFilter();
		m_StreetLightQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16)));
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateStreetLightsJob
		{
			m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ElectricityNodeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Watercraft_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ElectricityFlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityNodeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_StreetLight_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Brightness = Mathf.RoundToInt(m_LightingSystem.dayLightBrightness * 1000f),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_StreetLightQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	public static void UpdateStreetLightState(ref StreetLight streetLight, Road road)
	{
		if ((road.m_Flags & RoadFlags.LightsOff) != 0)
		{
			streetLight.m_State |= StreetLightState.TurnedOff;
		}
		else
		{
			streetLight.m_State &= ~StreetLightState.TurnedOff;
		}
	}

	public static void UpdateStreetLightState(ref StreetLight streetLight, Building building)
	{
		if ((building.m_Flags & BuildingFlags.StreetLightsOff) != BuildingFlags.None)
		{
			streetLight.m_State |= StreetLightState.TurnedOff;
		}
		else
		{
			streetLight.m_State &= ~StreetLightState.TurnedOff;
		}
	}

	public static void UpdateStreetLightState(ref StreetLight streetLight, Watercraft watercraft)
	{
		if ((watercraft.m_Flags & WatercraftFlags.LightsOff) != 0)
		{
			streetLight.m_State |= StreetLightState.TurnedOff;
		}
		else
		{
			streetLight.m_State &= ~StreetLightState.TurnedOff;
		}
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
	public StreetLightSystem()
	{
	}
}
