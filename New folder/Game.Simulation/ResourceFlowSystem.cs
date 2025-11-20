#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResourceFlowSystem : GameSystemBase
{
	private struct SourceNodeData
	{
		public int m_Flow;

		public Entity m_Node;
	}

	public struct TargetDirectionData
	{
		public Entity m_Node;

		public Entity m_Edge;

		public int2 m_Direction;
	}

	private struct ResourceNodeItem : ILessThan<ResourceNodeItem>
	{
		public float m_Distance;

		public Entity m_Node;

		public TargetDirectionData m_Target;

		public bool LessThan(ResourceNodeItem other)
		{
			return m_Distance < other.m_Distance;
		}
	}

	[BurstCompile]
	private struct ResourceFlowJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<ResourceConnection> m_ResourceConnectionType;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<ResourceConnection> m_ResourceConnectionData;

		public void Execute()
		{
			NativeList<SourceNodeData> nativeList = new NativeList<SourceNodeData>(10, Allocator.Temp);
			NativeHashMap<Entity, TargetDirectionData> nativeHashMap = new NativeHashMap<Entity, TargetDirectionData>(100, Allocator.Temp);
			NativeMinHeap<ResourceNodeItem> nativeMinHeap = new NativeMinHeap<ResourceNodeItem>(10, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Node> nativeArray = archetypeChunk.GetNativeArray(ref m_NodeType);
				NativeArray<ResourceConnection> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ResourceConnectionType);
				if (nativeArray.Length != 0)
				{
					NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_EntityType);
					NativeArray<Owner> nativeArray4 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					bool flag = archetypeChunk.Has(ref m_ServiceUpgradeType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						ref ResourceConnection reference = ref nativeArray2.ElementAt(j);
						Owner value2;
						Transform componentData;
						if (reference.m_Flow.y >> 1 != 0)
						{
							SourceNodeData value = new SourceNodeData
							{
								m_Flow = reference.m_Flow.y >> 1,
								m_Node = nativeArray3[j]
							};
							nativeList.Add(in value);
						}
						else if (!flag && CollectionUtils.TryGet(nativeArray4, j, out value2) && !m_ServiceUpgradeData.HasComponent(value2.m_Owner) && m_TransformData.TryGetComponent(value2.m_Owner, out componentData))
						{
							nativeMinHeap.Insert(new ResourceNodeItem
							{
								m_Node = nativeArray3[j],
								m_Distance = math.distancesq(componentData.m_Position, nativeArray[j].m_Position)
							});
						}
						reference.m_Flow = default(int2);
					}
				}
				else if (archetypeChunk.Has(ref m_ObjectType))
				{
					NativeArray<Entity> nativeArray5 = archetypeChunk.GetNativeArray(m_EntityType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						ref ResourceConnection reference2 = ref nativeArray2.ElementAt(k);
						if (reference2.m_Flow.y >> 1 != 0)
						{
							SourceNodeData value = new SourceNodeData
							{
								m_Flow = reference2.m_Flow.y >> 1,
								m_Node = nativeArray5[k]
							};
							nativeList.Add(in value);
						}
						reference2.m_Flow = default(int2);
					}
				}
				else
				{
					for (int l = 0; l < nativeArray2.Length; l++)
					{
						nativeArray2.ElementAt(l).m_Flow = default(int2);
					}
				}
			}
			RefRW<ResourceConnection> refRW;
			while (nativeMinHeap.Length != 0)
			{
				ResourceNodeItem resourceNodeItem = nativeMinHeap.Extract();
				if (!nativeHashMap.TryAdd(resourceNodeItem.m_Node, resourceNodeItem.m_Target))
				{
					continue;
				}
				refRW = m_ResourceConnectionData.GetRefRW(resourceNodeItem.m_Node);
				refRW.ValueRW.m_Flow.y = 1;
				if (!m_ConnectedEdges.TryGetBuffer(resourceNodeItem.m_Node, out var bufferData))
				{
					continue;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					ConnectedEdge connectedEdge = bufferData[m];
					if (!m_ResourceConnectionData.HasComponent(connectedEdge.m_Edge))
					{
						continue;
					}
					Edge edge = m_EdgeData[connectedEdge.m_Edge];
					Curve curve = m_CurveData[connectedEdge.m_Edge];
					DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[connectedEdge.m_Edge];
					float num = 0.5f;
					bool3 @bool = new bool3(x: false, y: true, z: false);
					if (edge.m_Start == resourceNodeItem.m_Node)
					{
						num = 0f;
						@bool = new bool3(x: true, y: false, z: false);
					}
					else if (edge.m_End == resourceNodeItem.m_Node)
					{
						num = 1f;
						@bool = new bool3(x: false, y: false, z: true);
					}
					else
					{
						for (int n = 0; n < dynamicBuffer.Length; n++)
						{
							ConnectedNode connectedNode = dynamicBuffer[n];
							if (connectedNode.m_Node == resourceNodeItem.m_Node)
							{
								num = connectedNode.m_CurvePosition;
								break;
							}
						}
					}
					if (!nativeHashMap.ContainsKey(edge.m_Start) && m_ResourceConnectionData.HasComponent(edge.m_Start))
					{
						nativeMinHeap.Insert(new ResourceNodeItem
						{
							m_Distance = resourceNodeItem.m_Distance + curve.m_Length * num,
							m_Node = edge.m_Start,
							m_Target = new TargetDirectionData
							{
								m_Node = resourceNodeItem.m_Node,
								m_Edge = connectedEdge.m_Edge,
								m_Direction = new int2(-1, math.select(-1, 0, @bool.y))
							}
						});
					}
					if (!nativeHashMap.ContainsKey(edge.m_End) && m_ResourceConnectionData.HasComponent(edge.m_End))
					{
						nativeMinHeap.Insert(new ResourceNodeItem
						{
							m_Distance = resourceNodeItem.m_Distance + curve.m_Length * (1f - num),
							m_Node = edge.m_End,
							m_Target = new TargetDirectionData
							{
								m_Node = resourceNodeItem.m_Node,
								m_Edge = connectedEdge.m_Edge,
								m_Direction = new int2(math.select(1, 0, @bool.y), 1)
							}
						});
					}
					for (int num2 = 0; num2 < dynamicBuffer.Length; num2++)
					{
						ConnectedNode connectedNode2 = dynamicBuffer[num2];
						if (!nativeHashMap.ContainsKey(connectedNode2.m_Node) && m_ResourceConnectionData.HasComponent(connectedNode2.m_Node))
						{
							nativeMinHeap.Insert(new ResourceNodeItem
							{
								m_Distance = resourceNodeItem.m_Distance + curve.m_Length * math.abs(connectedNode2.m_CurvePosition - num),
								m_Node = connectedNode2.m_Node,
								m_Target = new TargetDirectionData
								{
									m_Node = resourceNodeItem.m_Node,
									m_Edge = connectedEdge.m_Edge,
									m_Direction = math.select(new int2(0, 0), new int2(1, -1), @bool.xz)
								}
							});
						}
					}
					if (!m_SubObjects.TryGetBuffer(connectedEdge.m_Edge, out var bufferData2))
					{
						continue;
					}
					for (int num3 = 0; num3 < bufferData2.Length; num3++)
					{
						SubObject subObject = bufferData2[num3];
						if (m_ResourceConnectionData.HasComponent(subObject.m_SubObject) && !nativeHashMap.ContainsKey(subObject.m_SubObject) && m_TransformData.TryGetComponent(subObject.m_SubObject, out var componentData2))
						{
							MathUtils.Distance(new Line3.Segment(curve.m_Bezier.a, curve.m_Bezier.d), componentData2.m_Position, out var t);
							nativeMinHeap.Insert(new ResourceNodeItem
							{
								m_Distance = resourceNodeItem.m_Distance + curve.m_Length * math.abs(t - num),
								m_Node = subObject.m_SubObject,
								m_Target = new TargetDirectionData
								{
									m_Node = resourceNodeItem.m_Node,
									m_Edge = connectedEdge.m_Edge,
									m_Direction = math.select(new int2(0, 0), new int2(1, -1), @bool.xz)
								}
							});
						}
					}
				}
			}
			for (int num4 = 0; num4 < nativeList.Length; num4++)
			{
				SourceNodeData sourceNodeData = nativeList[num4];
				if (!nativeHashMap.TryGetValue(sourceNodeData.m_Node, out var item) || item.m_Edge == Entity.Null)
				{
					continue;
				}
				refRW = m_ResourceConnectionData.GetRefRW(sourceNodeData.m_Node);
				refRW.ValueRW.m_Flow.x += sourceNodeData.m_Flow;
				for (int num5 = 0; num5 < 10000; num5++)
				{
					refRW = m_ResourceConnectionData.GetRefRW(item.m_Edge);
					refRW.ValueRW.m_Flow += item.m_Direction * sourceNodeData.m_Flow;
					sourceNodeData.m_Node = item.m_Node;
					if (!nativeHashMap.TryGetValue(sourceNodeData.m_Node, out item) || item.m_Edge == Entity.Null)
					{
						refRW = m_ResourceConnectionData.GetRefRW(sourceNodeData.m_Node);
						refRW.ValueRW.m_Flow.x -= sourceNodeData.m_Flow;
						break;
					}
				}
			}
			nativeList.Dispose();
			nativeHashMap.Dispose();
			nativeMinHeap.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		public ComponentTypeHandle<ResourceConnection> __Game_Net_ResourceConnection_RW_ComponentTypeHandle;

		public ComponentLookup<ResourceConnection> __Game_Net_ResourceConnection_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Object>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgrade>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgrade>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Net_ResourceConnection_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceConnection>();
			__Game_Net_ResourceConnection_RW_ComponentLookup = state.GetComponentLookup<ResourceConnection>();
		}
	}

	private ExtractorCompanySystem m_ExtractorCompanySystem;

	private EntityQuery m_NetQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / EconomyUtils.kCompanyUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ExtractorCompanySystem = base.World.GetExistingSystemManaged<ExtractorCompanySystem>();
		m_NetQuery = GetEntityQuery(ComponentType.ReadWrite<ResourceConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_NetQuery);
		Assert.AreEqual(GetUpdateInterval(SystemUpdatePhase.GameSimulation), m_ExtractorCompanySystem.GetUpdateInterval(SystemUpdatePhase.GameSimulation) * 16);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		ResourceFlowJob jobData = new ResourceFlowJob
		{
			m_Chunks = m_NetQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ResourceConnection_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		jobData.m_Chunks.Dispose(jobHandle);
		base.Dependency = jobHandle;
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
	public ResourceFlowSystem()
	{
	}
}
