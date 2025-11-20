using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
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

namespace Game.Areas;

[CompilerGenerated]
public class SurfaceExpandSystem : GameSystemBase
{
	[BurstCompile]
	private struct ExpandAreasJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		public BufferTypeHandle<Expand> m_ExpandType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Extension> m_ExtensionData;

		[ReadOnly]
		public ComponentLookup<AccessLane> m_AccessLaneData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Expand> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ExpandType);
			NativeList<float4> connections = new NativeList<float4>(10, Allocator.Temp);
			for (int i = 0; i < bufferAccessor2.Length; i++)
			{
				DynamicBuffer<Node> nodes = bufferAccessor[i];
				DynamicBuffer<Expand> expands = bufferAccessor2[i];
				expands.ResizeUninitialized(nodes.Length);
				Owner owner = default(Owner);
				if (nativeArray.Length != 0)
				{
					owner = nativeArray[i];
					while (m_OwnerData.HasComponent(owner.m_Owner) && !m_BuildingData.HasComponent(owner.m_Owner))
					{
						owner = m_OwnerData[owner.m_Owner];
					}
				}
				if (m_PrefabRefData.TryGetComponent(owner.m_Owner, out var componentData) && m_TransformData.TryGetComponent(owner.m_Owner, out var componentData2) && m_PrefabBuildingData.TryGetComponent(componentData.m_Prefab, out var componentData3))
				{
					Calculate(expands, nodes, connections, owner.m_Owner, componentData2, componentData3);
				}
				else
				{
					Clear(expands);
				}
			}
			connections.Dispose();
		}

		private void Clear(DynamicBuffer<Expand> expands)
		{
			for (int i = 0; i < expands.Length; i++)
			{
				expands[i] = default(Expand);
			}
		}

		private void Calculate(DynamicBuffer<Expand> expands, DynamicBuffer<Node> nodes, NativeList<float4> connections, Entity building, Transform transform, BuildingData prefabBuildingData)
		{
			if (expands.Length == 0)
			{
				return;
			}
			Quad2 xz = BuildingUtils.CalculateCorners(transform, prefabBuildingData.m_LotSize).xz;
			float2 xz2 = math.mul(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
			float2 xz3 = math.mul(transform.m_Rotation, new float3(-8f, 0f, 0f)).xz;
			float borderDistance = AreaUtils.GetMinNodeDistance(AreaType.Surface) * 0.5f;
			bool flag = false;
			float2 xz4 = nodes[nodes.Length - 1].m_Position.xz;
			float4 @float = CheckBorders(xz, xz4, prefabBuildingData, borderDistance);
			float2 xz5 = nodes[0].m_Position.xz;
			float4 float2 = CheckBorders(xz, xz5, prefabBuildingData, borderDistance);
			bool4 @bool = false;
			if (math.any((@float != -1f) & (float2 != -1f)))
			{
				if (!flag)
				{
					flag = true;
					FillConnections(connections, building, xz, prefabBuildingData);
				}
				@bool = CheckConnections(@float, float2, connections);
			}
			for (int i = 0; i < expands.Length; i++)
			{
				Expand value = default(Expand);
				float2 xz6 = nodes[math.select(i + 1, 0, i == nodes.Length - 1)].m_Position.xz;
				float4 float3 = CheckBorders(xz, xz6, prefabBuildingData, borderDistance);
				bool4 bool2 = false;
				if (math.any((float2 != -1f) & (float3 != -1f)))
				{
					if (!flag)
					{
						flag = true;
						FillConnections(connections, building, xz, prefabBuildingData);
					}
					bool2 = CheckConnections(float2, float3, connections);
				}
				bool4 bool3 = @bool | bool2;
				if (bool3.x)
				{
					value.m_Offset += xz2;
				}
				if (bool3.y)
				{
					value.m_Offset += xz3;
				}
				if (bool3.z)
				{
					value.m_Offset -= xz2;
				}
				if (bool3.w)
				{
					value.m_Offset -= xz3;
				}
				expands[i] = value;
				float2 = float3;
				@bool = bool2;
			}
		}

		private void FillConnections(NativeList<float4> connections, Entity building, Quad2 quad, BuildingData prefabBuildingData)
		{
			connections.Clear();
			AddConnections(connections, building, quad, prefabBuildingData);
		}

		private void AddConnections(NativeList<float4> connections, Entity owner, Quad2 quad, BuildingData prefabBuildingData)
		{
			if (m_SubObjects.TryGetBuffer(owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					AddConnections(connections, bufferData[i], quad, prefabBuildingData);
				}
			}
			if (m_SubNets.TryGetBuffer(owner, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					AddConnections(connections, bufferData2[j], quad, prefabBuildingData);
				}
			}
		}

		private void AddConnections(NativeList<float4> connections, Game.Objects.SubObject subObject, Quad2 quad, BuildingData prefabBuildingData)
		{
			if (m_ExtensionData.HasComponent(subObject.m_SubObject))
			{
				AddConnections(connections, subObject.m_SubObject, quad, prefabBuildingData);
			}
			if (m_AccessLaneData.TryGetComponent(subObject.m_SubObject, out var componentData) && m_RouteLaneData.TryGetComponent(subObject.m_SubObject, out var componentData2) && m_CurveData.TryGetComponent(componentData.m_Lane, out var componentData3) && m_CurveData.TryGetComponent(componentData2.m_EndLane, out var componentData4) && ((m_OwnerData.TryGetComponent(componentData.m_Lane, out var componentData5) && CheckConnectionOwner(componentData5.m_Owner)) || (m_OwnerData.TryGetComponent(componentData2.m_EndLane, out var componentData6) && CheckConnectionOwner(componentData6.m_Owner))))
			{
				Line2.Segment line = default(Line2.Segment);
				line.a = MathUtils.Position(componentData3.m_Bezier, componentData.m_CurvePos).xz;
				line.b = MathUtils.Position(componentData4.m_Bezier, componentData2.m_EndCurvePos).xz;
				AddConnection(connections, line, quad, prefabBuildingData);
			}
		}

		private void AddConnections(NativeList<float4> connections, Game.Net.SubNet subNet, Quad2 quad, BuildingData prefabBuildingData)
		{
			if (!m_ConnectedEdges.TryGetBuffer(subNet.m_SubNet, out var bufferData))
			{
				return;
			}
			Game.Net.Node node = m_NodeData[subNet.m_SubNet];
			for (int i = 0; i < bufferData.Length; i++)
			{
				ConnectedEdge connectedEdge = bufferData[i];
				Edge edge = m_EdgeData[connectedEdge.m_Edge];
				if (edge.m_Start == subNet.m_SubNet || edge.m_End == subNet.m_SubNet || !CheckConnectionOwner(connectedEdge.m_Edge))
				{
					continue;
				}
				DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[connectedEdge.m_Edge];
				Curve curve = m_CurveData[connectedEdge.m_Edge];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ConnectedNode connectedNode = dynamicBuffer[j];
					if (connectedNode.m_Node == subNet.m_SubNet)
					{
						Line2.Segment line = new Line2.Segment(node.m_Position.xz, MathUtils.Position(curve.m_Bezier, connectedNode.m_CurvePosition).xz);
						AddConnection(connections, line, quad, prefabBuildingData);
						break;
					}
				}
			}
		}

		private bool CheckConnectionOwner(Entity owner)
		{
			if (m_PrefabRefData.TryGetComponent(owner, out var componentData) && m_PrefabNetGeometryData.TryGetComponent(componentData.m_Prefab, out var componentData2))
			{
				return (componentData2.m_Flags & Game.Net.GeometryFlags.Marker) == 0;
			}
			return false;
		}

		private void AddConnection(NativeList<float4> connections, Line2.Segment line, Quad2 quad, BuildingData prefabBuildingData)
		{
			float4 value = -1f;
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) != 0 && MathUtils.Intersect(quad.ab, line, out var t))
			{
				value.z = t.x;
			}
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0 && MathUtils.Intersect(quad.bc, line, out var t2))
			{
				value.y = t2.x;
			}
			if (MathUtils.Intersect(quad.cd, line, out var t3))
			{
				value.x = t3.x;
			}
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0 && MathUtils.Intersect(quad.da, line, out var t4))
			{
				value.w = t4.x;
			}
			if (math.any(value != -1f))
			{
				connections.Add(in value);
			}
		}

		private bool4 CheckConnections(float4 border1, float4 border2, NativeList<float4> connections)
		{
			bool4 result = false;
			float4 @float = math.min(math.select(border1, 2f, border1 == -1f), math.select(border2, 2f, border2 == -1f));
			float4 float2 = math.max(border1, border2);
			for (int i = 0; i < connections.Length; i++)
			{
				float4 float3 = connections[i];
				result |= (float3 >= @float) & (float3 <= float2);
			}
			return result;
		}

		private float4 CheckBorders(Quad2 quad, float2 position, BuildingData prefabBuildingData, float borderDistance)
		{
			float4 result = -1f;
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) != 0 && MathUtils.Distance(quad.ab, position, out var t) < borderDistance)
			{
				result.z = t;
			}
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0 && MathUtils.Distance(quad.bc, position, out var t2) < borderDistance)
			{
				result.y = t2;
			}
			if (MathUtils.Distance(quad.cd, position, out var t3) < borderDistance)
			{
				result.x = t3;
			}
			if ((prefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0 && MathUtils.Distance(quad.da, position, out var t4) < borderDistance)
			{
				result.w = t4;
			}
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		public BufferTypeHandle<Expand> __Game_Areas_Expand_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccessLane> __Game_Routes_AccessLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Areas_Expand_RW_BufferTypeHandle = state.GetBufferTypeHandle<Expand>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentLookup = state.GetComponentLookup<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedAreasQuery;

	private EntityQuery m_AllAreasQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedAreasQuery = GetEntityQuery(ComponentType.ReadOnly<Surface>(), ComponentType.ReadOnly<Expand>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_AllAreasQuery = GetEntityQuery(ComponentType.ReadOnly<Surface>(), ComponentType.ReadOnly<Expand>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllAreasQuery : m_UpdatedAreasQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependency = JobChunkExtensions.ScheduleParallel(new ExpandAreasJob
			{
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ExpandType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Expand_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef)
			}, query, base.Dependency);
			base.Dependency = dependency;
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
	public SurfaceExpandSystem()
	{
	}
}
