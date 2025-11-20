using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialObjectPlacementTriggerSystem : TutorialTriggerSystemBase
{
	private struct ClearCountJob : IJobChunk
	{
		public ComponentTypeHandle<ObjectPlacementTriggerCountData> m_CountType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ObjectPlacementTriggerCountData> nativeArray = chunk.GetNativeArray(ref m_CountType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ObjectPlacementTriggerCountData value = nativeArray[i];
				value.m_Count = 0;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckObjectsJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CreatedObjectChunks;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> m_ForcedUnlockDataFromEntity;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> m_UnlockRequirementFromEntity;

		[ReadOnly]
		public BufferTypeHandle<ObjectPlacementTriggerData> m_TriggerType;

		[ReadOnly]
		public ComponentLookup<Native> m_Natives;

		[ReadOnly]
		public ComponentLookup<ElectricityProducer> m_ElectricityProducers;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.SewageOutlet> m_SewageOutlets;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Transformer> m_Transformers;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> m_ExtractorFacility;

		[ReadOnly]
		public ComponentLookup<Edge> m_Edges;

		[ReadOnly]
		public ComponentLookup<Game.Net.ElectricityConnection> m_ElectricityConnections;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> m_ResourceConnection;

		[ReadOnly]
		public ComponentLookup<Road> m_Roads;

		[ReadOnly]
		public ComponentLookup<Game.Net.WaterPipeConnection> m_WaterPipeConnections;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgrade;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleted;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ElectricityConnection> m_ElectricityConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.WaterPipeConnection> m_WaterPipeConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ResourceConnection> m_ResourceConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public bool m_HasElevation;

		public ComponentTypeHandle<ObjectPlacementTriggerCountData> m_CountType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public bool m_FirstTimeCheck;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ObjectPlacementTriggerData> bufferAccessor = chunk.GetBufferAccessor(ref m_TriggerType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ObjectPlacementTriggerCountData> nativeArray2 = chunk.GetNativeArray(ref m_CountType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if (!Check(bufferAccessor[i]))
				{
					continue;
				}
				ObjectPlacementTriggerCountData value = nativeArray2[i];
				value.m_Count++;
				if (value.m_Count >= value.m_RequiredCount)
				{
					if (m_FirstTimeCheck)
					{
						m_CommandBuffer.AddComponent<TriggerPreCompleted>(unfilteredChunkIndex, nativeArray[i]);
					}
					else
					{
						m_CommandBuffer.AddComponent<TriggerCompleted>(unfilteredChunkIndex, nativeArray[i]);
					}
					TutorialSystem.ManualUnlock(nativeArray[i], m_UnlockEventArchetype, ref m_ForcedUnlockDataFromEntity, ref m_UnlockRequirementFromEntity, m_CommandBuffer, unfilteredChunkIndex);
				}
				nativeArray2[i] = value;
			}
		}

		private bool Check(DynamicBuffer<ObjectPlacementTriggerData> triggerDatas)
		{
			for (int i = 0; i < triggerDatas.Length; i++)
			{
				ObjectPlacementTriggerData triggerData = triggerDatas[i];
				for (int j = 0; j < m_CreatedObjectChunks.Length; j++)
				{
					ArchetypeChunk archetypeChunk = m_CreatedObjectChunks[j];
					bool num = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.AllowSubObject);
					bool flag = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireElevation);
					bool flag2 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireServiceUpgrade);
					if ((!num && archetypeChunk.Has(ref m_OwnerType)) || (flag && !m_HasElevation) || (flag2 && !archetypeChunk.Has(ref m_ServiceUpgradeType)))
					{
						continue;
					}
					NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					if (archetypeChunk.Has(ref m_EdgeType))
					{
						NativeArray<Edge> nativeArray2 = archetypeChunk.GetNativeArray(ref m_EdgeType);
						bool flag3 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireOutsideConnection);
						bool flag4 = false;
						if (archetypeChunk.Has(ref m_WaterPipeConnectionType))
						{
							if (flag3)
							{
								flag4 = CheckNodes(triggerData, m_WaterPipeConnections, m_Natives, 1, nativeArray2, nativeArray);
							}
							if (!(!flag3 || flag4))
							{
								continue;
							}
							bool flag5 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireRoadConnection);
							bool flag6 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireSewageOutletConnection);
							if (flag5 || flag6)
							{
								bool flag7 = false;
								bool flag8 = false;
								if (flag5)
								{
									flag7 = CheckEdges(triggerData, m_WaterPipeConnections, m_Roads, 1, nativeArray2, nativeArray);
								}
								if (flag6)
								{
									flag8 = CheckNodes(triggerData, m_WaterPipeConnections, m_SewageOutlets, 1, nativeArray2, nativeArray);
								}
								if ((!flag5 || flag7) && (!flag6 || flag8))
								{
									return true;
								}
								continue;
							}
							if (Check(triggerData, nativeArray))
							{
								return true;
							}
						}
						if (archetypeChunk.Has(ref m_ElectricityConnectionType))
						{
							if (flag3)
							{
								flag4 = CheckEdges(triggerData, m_ElectricityConnections, m_Natives, 1, nativeArray2, nativeArray);
							}
							if (!(!flag3 || flag4))
							{
								continue;
							}
							bool flag9 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireRoadConnection);
							bool flag10 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireTransformerConnection);
							bool flag11 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireElectricityProducerConnection);
							if (flag9 || flag10 || flag11)
							{
								bool flag12 = false;
								bool flag13 = false;
								bool flag14 = false;
								if (flag9)
								{
									flag12 = CheckEdges(triggerData, m_ElectricityConnections, m_Roads, 1, nativeArray2, nativeArray);
								}
								if (flag10)
								{
									flag13 = CheckNodes(triggerData, m_ElectricityConnections, m_Transformers, 1, nativeArray2, nativeArray);
								}
								if (flag11)
								{
									flag14 = CheckNodes(triggerData, m_ElectricityConnections, m_ElectricityProducers, 1, nativeArray2, nativeArray);
								}
								if ((!flag9 || flag12) && (!flag10 || flag13) && (!flag11 || flag14))
								{
									return true;
								}
								continue;
							}
							if (Check(triggerData, nativeArray))
							{
								return true;
							}
						}
						if (archetypeChunk.Has(ref m_ResourceConnectionType))
						{
							bool flag15 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireResourceExtractorConnection);
							bool flag16 = FlagsMatch(triggerData, ObjectPlacementTriggerFlags.RequireResourceConnection);
							if (flag15 || flag16)
							{
								bool flag17 = false;
								bool flag18 = false;
								if (flag15)
								{
									flag17 = CheckNodes(triggerData, m_ResourceConnection, m_ExtractorFacility, 1, nativeArray2, nativeArray);
								}
								if (flag16)
								{
									flag18 = CheckNodes(triggerData, m_ResourceConnection, m_ResourceConnection, m_ServiceUpgrade, (!flag15) ? 1 : 2, nativeArray2, nativeArray);
								}
								if ((!flag15 || flag17) && (!flag16 || flag18))
								{
									return true;
								}
								continue;
							}
							if (Check(triggerData, nativeArray))
							{
								return true;
							}
						}
						if (Check(triggerData, nativeArray))
						{
							return true;
						}
					}
					else if (Check(triggerData, nativeArray))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool FlagsMatch(ObjectPlacementTriggerData triggerData, ObjectPlacementTriggerFlags flags)
		{
			return (triggerData.m_Flags & flags) == flags;
		}

		private bool CheckEdges<T1, T2>(ObjectPlacementTriggerData triggerData, ComponentLookup<T1> matchData, ComponentLookup<T2> searchData, int requiredCount, NativeArray<Edge> edges, NativeArray<PrefabRef> prefabRefs) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
		{
			NativeList<Entity> stack = new NativeList<Entity>(10, Allocator.Temp);
			NativeParallelHashMap<Entity, int> onStack = new NativeParallelHashMap<Entity, int>(100, Allocator.Temp);
			for (int i = 0; i < prefabRefs.Length; i++)
			{
				if (!(prefabRefs[i].m_Prefab != triggerData.m_Object) && CheckEdgesImpl(edges[i].m_Start, matchData, searchData, requiredCount, stack, onStack) >= requiredCount)
				{
					return true;
				}
			}
			onStack.Dispose();
			stack.Dispose();
			return false;
		}

		private int CheckEdgesImpl<T1, T2>(Entity node, ComponentLookup<T1> matchData, ComponentLookup<T2> searchData, int requiredCount, NativeList<Entity> stack, NativeParallelHashMap<Entity, int> onStack) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
		{
			int num = 0;
			Push(node, stack, onStack);
			while (stack.Length > 0)
			{
				Entity entity = Pop(stack);
				if (!m_ConnectedEdges.HasBuffer(entity))
				{
					continue;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					if (searchData.HasComponent(edge) && onStack[entity] == 1)
					{
						num++;
					}
					if (num >= requiredCount)
					{
						return num;
					}
					if (m_Edges.HasComponent(edge) && matchData.HasComponent(edge))
					{
						Edge edge2 = m_Edges[edge];
						if (!onStack.ContainsKey(edge2.m_Start) || onStack[edge2.m_Start] < 2)
						{
							Push(edge2.m_Start, stack, onStack);
						}
						if (!onStack.ContainsKey(edge2.m_End) || onStack[edge2.m_End] < 2)
						{
							Push(edge2.m_End, stack, onStack);
						}
					}
				}
			}
			return num;
		}

		private bool CheckNodes<T1, T2>(ObjectPlacementTriggerData triggerData, ComponentLookup<T1> matchData, ComponentLookup<T2> searchData, int requiredCount, NativeArray<Edge> edges, NativeArray<PrefabRef> prefabRefs) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
		{
			return CheckNodes(triggerData, matchData, searchData, m_Deleted, requiredCount, edges, prefabRefs);
		}

		private bool CheckNodes<T1, T2, T3>(ObjectPlacementTriggerData triggerData, ComponentLookup<T1> matchData, ComponentLookup<T2> searchData, ComponentLookup<T3> forbiddenData, int requiredCount, NativeArray<Edge> edges, NativeArray<PrefabRef> prefabRefs) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData
		{
			NativeList<Entity> stack = new NativeList<Entity>(10, Allocator.Temp);
			NativeParallelHashMap<Entity, int> onStack = new NativeParallelHashMap<Entity, int>(100, Allocator.Temp);
			for (int i = 0; i < prefabRefs.Length; i++)
			{
				if (!(prefabRefs[i].m_Prefab != triggerData.m_Object) && CheckNodesImpl(edges[i].m_Start, matchData, searchData, forbiddenData, requiredCount, stack, onStack) >= requiredCount)
				{
					return true;
				}
			}
			onStack.Dispose();
			stack.Dispose();
			return false;
		}

		private int CheckNodesImpl<T1, T2, T3>(Entity node, ComponentLookup<T1> matchData, ComponentLookup<T2> searchData, ComponentLookup<T3> forbiddenData, int requiredCount, NativeList<Entity> stack, NativeParallelHashMap<Entity, int> onStack) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData
		{
			int num = 0;
			Push(node, stack, onStack);
			while (stack.Length > 0)
			{
				Entity entity = Pop(stack);
				if (onStack[entity] == 1 && searchData.HasComponent(entity) && !forbiddenData.HasComponent(entity))
				{
					num++;
				}
				else if (m_Owners.HasComponent(entity))
				{
					Owner owner = m_Owners[entity];
					if (onStack[entity] == 1 && searchData.HasComponent(owner.m_Owner))
					{
						num++;
					}
				}
				if (num >= requiredCount)
				{
					return num;
				}
				if (!m_ConnectedEdges.HasBuffer(entity))
				{
					continue;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					if (!m_Edges.HasComponent(edge) || !matchData.HasComponent(edge))
					{
						continue;
					}
					Edge edge2 = m_Edges[edge];
					if (m_ConnectedNodes.HasBuffer(edge))
					{
						DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_ConnectedNodes[edge];
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							Entity node2 = dynamicBuffer2[j].m_Node;
							if (!onStack.ContainsKey(node2) || onStack[node2] < 1)
							{
								Push(node2, stack, onStack);
							}
						}
					}
					if (!onStack.ContainsKey(edge2.m_Start) || onStack[edge2.m_Start] < 2)
					{
						Push(edge2.m_Start, stack, onStack);
					}
					if (!onStack.ContainsKey(edge2.m_End) || onStack[edge2.m_End] < 2)
					{
						Push(edge2.m_End, stack, onStack);
					}
				}
			}
			return num;
		}

		private void Push(Entity entity, NativeList<Entity> stack, NativeParallelHashMap<Entity, int> onStack)
		{
			if (!onStack.ContainsKey(entity))
			{
				onStack[entity] = 1;
			}
			else
			{
				onStack[entity]++;
			}
			stack.Add(in entity);
		}

		private Entity Pop(NativeList<Entity> stack)
		{
			Entity result = Entity.Null;
			if (stack.Length > 0)
			{
				result = stack[stack.Length - 1];
				stack.RemoveAtSwapBack(stack.Length - 1);
			}
			return result;
		}

		private bool Check(ObjectPlacementTriggerData triggerData, NativeArray<PrefabRef> prefabRefs)
		{
			for (int i = 0; i < prefabRefs.Length; i++)
			{
				if (!(prefabRefs[i].m_Prefab != triggerData.m_Object))
				{
					return true;
				}
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<ObjectPlacementTriggerCountData> __Game_Tutorials_ObjectPlacementTriggerCountData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityProducer> __Game_Buildings_ElectricityProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ElectricityConnection> __Game_Net_ElectricityConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ElectricityConnection> __Game_Net_ElectricityConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.WaterPipeConnection> __Game_Net_WaterPipeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.WaterPipeConnection> __Game_Net_WaterPipeConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> __Game_Net_ResourceConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ResourceConnection> __Game_Net_ResourceConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.SewageOutlet> __Game_Buildings_SewageOutlet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Transformer> __Game_Buildings_Transformer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> __Game_Buildings_ExtractorFacility_RO_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<ObjectPlacementTriggerData> __Game_Tutorials_ObjectPlacementTriggerData_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> __Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> __Game_Prefabs_UnlockRequirement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_ObjectPlacementTriggerCountData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectPlacementTriggerCountData>();
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Buildings_ElectricityProducer_RO_ComponentLookup = state.GetComponentLookup<ElectricityProducer>(isReadOnly: true);
			__Game_Net_ElectricityConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ElectricityConnection>(isReadOnly: true);
			__Game_Net_ElectricityConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ElectricityConnection>(isReadOnly: true);
			__Game_Net_WaterPipeConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.WaterPipeConnection>(isReadOnly: true);
			__Game_Net_WaterPipeConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.WaterPipeConnection>(isReadOnly: true);
			__Game_Net_ResourceConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ResourceConnection>(isReadOnly: true);
			__Game_Net_ResourceConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ResourceConnection>(isReadOnly: true);
			__Game_Buildings_SewageOutlet_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.SewageOutlet>(isReadOnly: true);
			__Game_Buildings_Transformer_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Transformer>(isReadOnly: true);
			__Game_Buildings_ExtractorFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ExtractorFacility>(isReadOnly: true);
			__Game_Tutorials_ObjectPlacementTriggerData_RO_BufferTypeHandle = state.GetBufferTypeHandle<ObjectPlacementTriggerData>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup = state.GetBufferLookup<ForceUIGroupUnlockData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirement_RO_BufferLookup = state.GetBufferLookup<UnlockRequirement>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private NetToolSystem m_NetToolSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_CreatedObjectQuery;

	private EntityQuery m_ObjectQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectPlacementTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.ReadWrite<ObjectPlacementTriggerCountData>(), ComponentType.Exclude<TriggerCompleted>());
		m_CreatedObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Created>()
			},
			Any = new ComponentType[10]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Game.Net.WaterPipeConnection>(),
				ComponentType.ReadOnly<Game.Net.ElectricityConnection>(),
				ComponentType.ReadOnly<Game.Prefabs.ResourceConnection>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<Waterway>(),
				ComponentType.ReadOnly<ServiceUpgradeBuilding>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Native>()
			}
		});
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabRef>() },
			Any = new ComponentType[10]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Game.Net.WaterPipeConnection>(),
				ComponentType.ReadOnly<Game.Net.ElectricityConnection>(),
				ComponentType.ReadOnly<Game.Net.ResourceConnection>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<Waterway>(),
				ComponentType.ReadOnly<ServiceUpgradeBuilding>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Native>()
			}
		});
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (base.triggersChanged)
		{
			ClearCountJob jobData = new ClearCountJob
			{
				m_CountType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectPlacementTriggerCountData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ActiveTriggerQuery, base.Dependency);
			JobHandle outJobHandle;
			CheckObjectsJob jobData2 = new CheckObjectsJob
			{
				m_CreatedObjectChunks = m_ObjectQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_Natives = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ElectricityConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ElectricityConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WaterPipeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_WaterPipeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterPipeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_WaterPipeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResourceConnection = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SewageOutlets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Transformers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Transformer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtractorFacility = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ExtractorFacility_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectPlacementTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CountType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectPlacementTriggerCountData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Roads = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleted = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgrade = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_HasElevation = HasElevation(),
				m_FirstTimeCheck = true
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			jobData2.m_CreatedObjectChunks.Dispose(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
		}
		else if (!m_CreatedObjectQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle2;
			CheckObjectsJob jobData3 = new CheckObjectsJob
			{
				m_CreatedObjectChunks = m_CreatedObjectQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2),
				m_Natives = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ElectricityConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ElectricityConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WaterPipeConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_WaterPipeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterPipeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_WaterPipeConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResourceConnection = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SewageOutlets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_SewageOutlet_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Transformers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Transformer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtractorFacility = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ExtractorFacility_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectPlacementTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CountType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectPlacementTriggerCountData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter(),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
				m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Roads = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleted = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgrade = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UnlockEventArchetype = m_UnlockEventArchetype,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_HasElevation = HasElevation(),
				m_FirstTimeCheck = false
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData3, m_ActiveTriggerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle2));
			jobData3.m_CreatedObjectChunks.Dispose(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
		}
	}

	private bool HasElevation()
	{
		if (m_ToolSystem.activeTool == m_NetToolSystem)
		{
			return math.abs(m_NetToolSystem.elevation) > 0.0001f;
		}
		return false;
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
	public TutorialObjectPlacementTriggerSystem()
	{
	}
}
