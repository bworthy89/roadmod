using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
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
public class WaypointConnectionSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateWaypointReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Connected> m_ConnectedType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<AccessLane> m_AccessLaneType;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> m_RouteLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public BufferLookup<ConnectedRoute> m_ConnectedRoutes;

		public NativeList<Entity> m_UpdatedList;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Connected> nativeArray2 = chunk.GetNativeArray(ref m_ConnectedType);
			if (chunk.Has(ref m_DeletedType))
			{
				if (chunk.Has(ref m_TempType))
				{
					return;
				}
				if (nativeArray2.Length != 0)
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Entity waypoint = nativeArray[i];
						Connected connected = nativeArray2[i];
						if (m_ConnectedRoutes.HasBuffer(connected.m_Connected))
						{
							CollectionUtils.RemoveValue(m_ConnectedRoutes[connected.m_Connected], new ConnectedRoute(waypoint));
						}
					}
					return;
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					if (!m_ConnectedRoutes.HasBuffer(entity))
					{
						continue;
					}
					DynamicBuffer<ConnectedRoute> dynamicBuffer = m_ConnectedRoutes[entity];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity value = dynamicBuffer[k].m_Waypoint;
						if (m_ConnectedData.HasComponent(value) && !m_DeletedData.HasComponent(value))
						{
							m_UpdatedList.Add(in value);
						}
					}
				}
				return;
			}
			if (chunk.Has(ref m_CreatedType) && !chunk.Has(ref m_TempType))
			{
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Entity waypoint2 = nativeArray[l];
					Connected connected2 = nativeArray2[l];
					if (m_ConnectedRoutes.HasBuffer(connected2.m_Connected))
					{
						m_ConnectedRoutes[connected2.m_Connected].Add(new ConnectedRoute(waypoint2));
					}
				}
			}
			bool flag = chunk.Has(ref m_AccessLaneType) || chunk.Has(ref m_RouteLaneType);
			for (int m = 0; m < nativeArray.Length; m++)
			{
				Entity entity2 = nativeArray[m];
				if (m_ConnectedRoutes.HasBuffer(entity2))
				{
					DynamicBuffer<ConnectedRoute> dynamicBuffer2 = m_ConnectedRoutes[entity2];
					for (int n = 0; n < dynamicBuffer2.Length; n++)
					{
						Entity value2 = dynamicBuffer2[n].m_Waypoint;
						if (m_ConnectedData.HasComponent(value2) && !m_DeletedData.HasComponent(value2))
						{
							m_UpdatedList.Add(in value2);
						}
					}
				}
				if (flag)
				{
					m_UpdatedList.Add(nativeArray[m]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindUpdatedWaypointsJob : IJobParallelForDefer
	{
		private struct RouteIterator : INativeQuadTreeIterator<RouteSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<RouteSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Waypoint> m_WaypointData;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, RouteSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_WaypointData.HasComponent(item.m_Entity))
				{
					m_ResultQueue.Enqueue(item.m_Entity);
				}
			}
		}

		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<AccessLane> m_AccessLaneData;

			public ComponentLookup<RouteLane> m_RouteLaneData;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && (m_AccessLaneData.HasComponent(item) || m_RouteLaneData.HasComponent(item)))
				{
					m_ResultQueue.Enqueue(item);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> m_RouteSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_WaypointData;

		[ReadOnly]
		public ComponentLookup<AccessLane> m_AccessLaneData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Bounds2 bounds = MathUtils.Expand(m_Bounds[index], 10f);
			RouteIterator iterator = new RouteIterator
			{
				m_Bounds = bounds,
				m_WaypointData = m_WaypointData,
				m_ResultQueue = m_ResultQueue
			};
			m_RouteSearchTree.Iterate(ref iterator);
			ObjectIterator iterator2 = new ObjectIterator
			{
				m_Bounds = bounds,
				m_AccessLaneData = m_AccessLaneData,
				m_RouteLaneData = m_RouteLaneData,
				m_ResultQueue = m_ResultQueue
			};
			m_ObjectSearchTree.Iterate(ref iterator2);
		}
	}

	[BurstCompile]
	private struct DequeUpdatedWaypointsJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_UpdatedQueue;

		public NativeList<Entity> m_UpdatedList;

		public void Execute()
		{
			Entity item;
			while (m_UpdatedQueue.TryDequeue(out item))
			{
				m_UpdatedList.Add(in item);
			}
			m_UpdatedList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_UpdatedList.Length)
			{
				item = m_UpdatedList[num++];
				if (item != entity)
				{
					m_UpdatedList[num2++] = item;
					entity = item;
				}
			}
			if (num2 < m_UpdatedList.Length)
			{
				m_UpdatedList.RemoveRange(num2, m_UpdatedList.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct RemoveDuplicatedWaypointsJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeList<Entity> m_UpdatedList;

		public void Execute()
		{
			if (m_UpdatedList.Length < 2)
			{
				return;
			}
			m_UpdatedList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_UpdatedList.Length)
			{
				Entity entity2 = m_UpdatedList[num++];
				if (entity2 != entity)
				{
					m_UpdatedList[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_UpdatedList.Length)
			{
				m_UpdatedList.RemoveRange(num2, m_UpdatedList.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct FindWaypointConnectionsJob : IJobParallelForDefer
	{
		private struct SurfaceIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float3 m_Position;

			public int m_JobIndex;

			public ComponentLookup<Game.Areas.Surface> m_SurfaceData;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz) && m_SurfaceData.HasComponent(item.m_Area))
				{
					DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[item.m_Area];
					Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
					if (MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, triangle), m_Position.xz))
					{
						m_CommandBuffer.AddComponent(m_JobIndex, item.m_Area, default(Updated));
					}
				}
			}
		}

		private struct LaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float3 m_Position;

			public RouteConnectionType m_ConnectionType;

			public TrackTypes m_TrackType;

			public RoadTypes m_CarType;

			public bool m_OnGround;

			public float m_CmpDistance;

			public float m_MaxDistance;

			public float m_Elevation;

			public Quad2 m_LotQuad;

			public bool4 m_CheckLot;

			public Entity m_IgnoreOwner;

			public Entity m_IgnoreConnected;

			public Entity m_MasterLot;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

			public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

			public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

			public ComponentLookup<MasterLane> m_MasterLaneData;

			public ComponentLookup<SlaveLane> m_SlaveLaneData;

			public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Game.Net.Edge> m_EdgeData;

			public ComponentLookup<Game.Net.Elevation> m_ElevationData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<Game.Areas.Lot> m_LotData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

			public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

			public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

			public ComponentLookup<NetData> m_PrefabNetData;

			public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

			public BufferLookup<Game.Net.SubLane> m_Lanes;

			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public Entity m_ResultLane;

			public Entity m_ResultOwner;

			public float m_ResultCurvePos;

			public bool m_IntersectLot;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || !m_Lanes.HasBuffer(item.m_Area) || item.m_Area == m_IgnoreOwner)
				{
					return;
				}
				DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[item.m_Area];
				Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				float3 elevations = AreaUtils.GetElevations(nodes, triangle);
				bool flag = m_LotData.HasComponent(item.m_Area);
				float2 t;
				float num = ((!flag && (!m_OnGround || !math.any(elevations == float.MinValue))) ? MathUtils.Distance(triangle2, m_Position, out t) : MathUtils.Distance(triangle2.xz, m_Position.xz, out t));
				if (num >= m_CmpDistance)
				{
					return;
				}
				float3 @float = MathUtils.Position(triangle2, t);
				bool flag2 = false;
				if (math.any(m_CheckLot))
				{
					Line3.Segment segment = new Line3.Segment(m_Position, @float);
					flag2 |= m_CheckLot.x && MathUtils.Intersect(m_LotQuad.ab, segment.xz, out t);
					flag2 |= m_CheckLot.y && MathUtils.Intersect(m_LotQuad.bc, segment.xz, out t);
					flag2 |= m_CheckLot.z && MathUtils.Intersect(m_LotQuad.cd, segment.xz, out t);
					flag2 |= m_CheckLot.w && MathUtils.Intersect(m_LotQuad.da, segment.xz, out t);
				}
				if (flag && (m_MasterLot == Entity.Null || (item.m_Area != m_MasterLot && (!m_OwnerData.TryGetComponent(item.m_Area, out var componentData) || componentData.m_Owner != m_MasterLot))))
				{
					bool3 @bool = AreaUtils.IsEdge(nodes, triangle);
					@bool &= (math.cmin(triangle.m_Indices.xy) != 0) | (math.cmax(triangle.m_Indices.xy) != 1);
					@bool &= (math.cmin(triangle.m_Indices.yz) != 0) | (math.cmax(triangle.m_Indices.yz) != 1);
					@bool &= (math.cmin(triangle.m_Indices.zx) != 0) | (math.cmax(triangle.m_Indices.zx) != 1);
					if ((@bool.x && MathUtils.Distance(triangle2.ab, @float, out var t2) < 0.1f) || (@bool.y && MathUtils.Distance(triangle2.bc, @float, out t2) < 0.1f) || (@bool.z && MathUtils.Distance(triangle2.ca, @float, out t2) < 0.1f))
					{
						return;
					}
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[item.m_Area];
				Entity entity = Entity.Null;
				float num2 = float.MaxValue;
				float resultCurvePos = 0f;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (!m_ConnectionLaneData.TryGetComponent(subLane, out var componentData2) || !CheckLaneType(componentData2))
					{
						continue;
					}
					Curve curve = m_CurveData[subLane];
					float2 t3;
					bool2 x = new bool2(MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out t3), MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t3));
					if (math.any(x))
					{
						float t4;
						float num3 = MathUtils.Distance(curve.m_Bezier, @float, out t4);
						if (num3 < num2)
						{
							float2 float2 = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
							num2 = num3;
							entity = subLane;
							resultCurvePos = math.clamp(t4, float2.x, float2.y);
						}
					}
				}
				if (entity != Entity.Null)
				{
					m_CmpDistance = num;
					m_MaxDistance = num;
					m_ResultLane = entity;
					m_ResultOwner = item.m_Area;
					m_ResultCurvePos = resultCurvePos;
					m_IntersectLot = flag2;
					m_Bounds = new Bounds3(m_Position - num, m_Position + num);
				}
			}

			private bool CheckLaneType(Game.Net.ConnectionLane connectionLane)
			{
				switch (m_ConnectionType)
				{
				case RouteConnectionType.Pedestrian:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Road:
				case RouteConnectionType.Cargo:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane.m_RoadTypes & m_CarType) != RoadTypes.None)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Track:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Track) != 0 && (connectionLane.m_TrackTypes & m_TrackType) != TrackTypes.None)
					{
						return true;
					}
					return false;
				default:
					return false;
				}
			}

			private void CheckDistance(EdgeGeometry edgeGeometry, ref float maxDistance, ref int crossedSide, ref float3 maxDistancePos)
			{
				if (MathUtils.DistanceSquared(edgeGeometry.m_Bounds.xz, m_Position.xz) < maxDistance * maxDistance)
				{
					CheckDistance(edgeGeometry.m_Start.m_Left, -1, ref maxDistance, ref crossedSide, ref maxDistancePos);
					CheckDistance(edgeGeometry.m_Start.m_Right, 1, ref maxDistance, ref crossedSide, ref maxDistancePos);
					CheckDistance(edgeGeometry.m_End.m_Left, -1, ref maxDistance, ref crossedSide, ref maxDistancePos);
					CheckDistance(edgeGeometry.m_End.m_Right, 1, ref maxDistance, ref crossedSide, ref maxDistancePos);
				}
			}

			private void CheckDistance(EdgeNodeGeometry nodeGeometry, ref float maxDistance, ref int crossedSide, ref float3 maxDistancePos)
			{
				if (MathUtils.DistanceSquared(nodeGeometry.m_Bounds.xz, m_Position.xz) < maxDistance * maxDistance)
				{
					if (nodeGeometry.m_MiddleRadius > 0f)
					{
						CheckDistance(nodeGeometry.m_Left.m_Left, -1, ref maxDistance, ref crossedSide, ref maxDistancePos);
						CheckDistance(nodeGeometry.m_Left.m_Right, 1, ref maxDistance, ref crossedSide, ref maxDistancePos);
						CheckDistance(nodeGeometry.m_Right.m_Left, -2, ref maxDistance, ref crossedSide, ref maxDistancePos);
						CheckDistance(nodeGeometry.m_Right.m_Right, 2, ref maxDistance, ref crossedSide, ref maxDistancePos);
					}
					else
					{
						CheckDistance(nodeGeometry.m_Left.m_Left, -1, ref maxDistance, ref crossedSide, ref maxDistancePos);
						CheckDistance(nodeGeometry.m_Right.m_Right, 1, ref maxDistance, ref crossedSide, ref maxDistancePos);
					}
				}
			}

			private void CheckDistance(Bezier4x3 curve, int side, ref float maxDistance, ref int crossedSide, ref float3 maxDistancePos)
			{
				if (MathUtils.DistanceSquared(MathUtils.Bounds(curve.xz), m_Position.xz) < maxDistance * maxDistance)
				{
					float t;
					float num = MathUtils.Distance(curve.xz, m_Position.xz, out t);
					if (num < maxDistance)
					{
						maxDistance = num;
						maxDistancePos = MathUtils.Position(curve, t);
						float2 forward = MathUtils.Tangent(curve.xz, t);
						forward = math.select(MathUtils.Left(forward), MathUtils.Right(forward), side > 0);
						float2 y = m_Position.xz - maxDistancePos.xz;
						crossedSide = math.select(0, side, math.dot(forward, y) > 0f);
					}
				}
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity netEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || !m_Lanes.TryGetBuffer(netEntity, out var bufferData) || netEntity == m_IgnoreOwner)
				{
					return;
				}
				float maxDistance = 10f;
				float3 maxDistancePos = default(float3);
				int crossedSide = 0;
				int num = -1;
				bool flag = false;
				if (m_ConnectionType == RouteConnectionType.Pedestrian)
				{
					DynamicBuffer<ConnectedEdge> bufferData2;
					if (m_EdgeGeometryData.TryGetComponent(netEntity, out var componentData))
					{
						CheckDistance(componentData, ref maxDistance, ref crossedSide, ref maxDistancePos);
						flag = true;
					}
					else if (m_ConnectedEdges.TryGetBuffer(netEntity, out bufferData2))
					{
						for (int i = 0; i < bufferData2.Length; i++)
						{
							ConnectedEdge connectedEdge = bufferData2[i];
							Game.Net.Edge edge = m_EdgeData[connectedEdge.m_Edge];
							float num2 = maxDistance;
							EndNodeGeometry componentData3;
							if (edge.m_Start == netEntity)
							{
								if (m_StartNodeGeometryData.TryGetComponent(connectedEdge.m_Edge, out var componentData2))
								{
									CheckDistance(componentData2.m_Geometry, ref maxDistance, ref crossedSide, ref maxDistancePos);
								}
							}
							else if (edge.m_End == netEntity && m_EndNodeGeometryData.TryGetComponent(connectedEdge.m_Edge, out componentData3))
							{
								CheckDistance(componentData3.m_Geometry, ref maxDistance, ref crossedSide, ref maxDistancePos);
							}
							num = math.select(num, i, maxDistance != num2);
						}
					}
				}
				float num3 = 0f;
				bool flag2 = false;
				if (m_ElevationData.TryGetComponent(netEntity, out var componentData4))
				{
					flag2 = true;
					num3 = ((!flag || crossedSide == 0) ? (math.csum(componentData4.m_Elevation) * 0.5f) : math.select(componentData4.m_Elevation.x, componentData4.m_Elevation.y, crossedSide > 0));
					if (crossedSide != 0)
					{
						PrefabRef prefabRef = m_PrefabRefData[netEntity];
						if ((m_PrefabNetData[prefabRef.m_Prefab].m_RequiredLayers & (Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad)) != Layer.None)
						{
							Entity entity = Entity.Null;
							DynamicBuffer<ConnectedEdge> bufferData3;
							if (flag)
							{
								if (m_CompositionData.TryGetComponent(netEntity, out var componentData5))
								{
									entity = componentData5.m_Edge;
								}
							}
							else if (num != -1 && m_ConnectedEdges.TryGetBuffer(netEntity, out bufferData3))
							{
								ConnectedEdge connectedEdge2 = bufferData3[num];
								if (m_CompositionData.TryGetComponent(netEntity, out var componentData6))
								{
									Game.Net.Edge edge2 = m_EdgeData[connectedEdge2.m_Edge];
									if (edge2.m_Start == netEntity)
									{
										entity = componentData6.m_StartNode;
									}
									else if (edge2.m_End == netEntity)
									{
										entity = componentData6.m_EndNode;
									}
								}
							}
							if (m_PrefabNetCompositionData.TryGetComponent(entity, out var componentData7))
							{
								CompositionFlags.Side side = ((crossedSide > 0) ? componentData7.m_Flags.m_Right : componentData7.m_Flags.m_Left);
								if ((componentData7.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0)
								{
									if (math.abs(crossedSide) == 1 || (side & CompositionFlags.Side.HighTransition) == 0)
									{
										return;
									}
								}
								else if ((side & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0 && (math.abs(crossedSide) == 1 || (side & CompositionFlags.Side.LowTransition) == 0))
								{
									return;
								}
							}
						}
					}
				}
				Entity entity2 = Entity.Null;
				float4 @float = float.MaxValue;
				float4 float2 = float.MaxValue;
				float resultCurvePos = 0f;
				Bounds3 bounds2 = default(Bounds3);
				bool flag3 = false;
				float2 t2 = default(float2);
				for (int j = 0; j < bufferData.Length; j++)
				{
					Entity subLane = bufferData[j].m_SubLane;
					switch (m_ConnectionType)
					{
					case RouteConnectionType.Pedestrian:
					{
						if (!m_PedestrianLaneData.TryGetComponent(subLane, out var componentData8) || (componentData8.m_Flags & (PedestrianLaneFlags.AllowMiddle | PedestrianLaneFlags.OnWater)) != PedestrianLaneFlags.AllowMiddle)
						{
							if (m_CarType != RoadTypes.Bicycle || !m_CarLaneData.TryGetComponent(subLane, out var componentData9) || m_MasterLaneData.HasComponent(subLane) || (componentData9.m_Flags & CarLaneFlags.Unsafe) != 0)
							{
								continue;
							}
							PrefabRef prefabRef3 = m_PrefabRefData[subLane];
							if (m_PrefabCarLaneData[prefabRef3.m_Prefab].m_RoadTypes != RoadTypes.Bicycle)
							{
								continue;
							}
						}
						break;
					}
					case RouteConnectionType.Road:
					{
						if (!m_CarLaneData.TryGetComponent(subLane, out var componentData10) || m_MasterLaneData.HasComponent(subLane) || (componentData10.m_Flags & CarLaneFlags.Unsafe) != 0)
						{
							continue;
						}
						PrefabRef prefabRef4 = m_PrefabRefData[subLane];
						if ((m_PrefabCarLaneData[prefabRef4.m_Prefab].m_RoadTypes & m_CarType) == 0)
						{
							continue;
						}
						break;
					}
					case RouteConnectionType.Track:
					{
						if (!m_TrackLaneData.HasComponent(subLane))
						{
							continue;
						}
						PrefabRef prefabRef2 = m_PrefabRefData[subLane];
						if ((m_PrefabTrackLaneData[prefabRef2.m_Prefab].m_TrackTypes & m_TrackType) == 0)
						{
							continue;
						}
						break;
					}
					default:
						continue;
					}
					Curve curve = m_CurveData[subLane];
					PrefabRef prefabRef5 = m_PrefabRefData[subLane];
					NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef5.m_Prefab];
					float num4 = netLaneData.m_Width * 0.5f;
					if (maxDistance == 10f)
					{
						if (!MathUtils.Intersect(MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), num4).xz, m_Bounds.xz))
						{
							continue;
						}
					}
					else if (entity2 != Entity.Null && !MathUtils.Intersect(MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), num4).xz, bounds2.xz))
					{
						continue;
					}
					bool flag4 = (netLaneData.m_Flags & LaneFlags.OnWater) != 0;
					float t;
					float4 float3 = MathUtils.Distance(curve.m_Bezier.xz, m_Position.xz, out t);
					float num5 = MathUtils.Position(curve.m_Bezier.y, t);
					float2 falseValue = new float2(m_Position.y - num5, m_Elevation - num3);
					falseValue = math.select(falseValue, 0f, (m_OnGround && !flag2) || flag4);
					float num6 = math.max(0f, num4 - float3.x) * 0.1f / math.max(0.01f, num4);
					float3.x = math.max(0f, float3.x - num4) - num6;
					float3.y = math.cmin(math.abs(falseValue));
					float3.z = math.length(math.max(0f, float3.xy));
					float3.w = float3.z + math.min(0f, float3.x);
					bool flag5 = false;
					float4 float4 = float3;
					if (math.any(m_CheckLot) && (float3.x < 10f || maxDistance < 10f))
					{
						Line2.Segment segment = new Line2.Segment(m_Position.xz, MathUtils.Position(curve.m_Bezier, t).xz);
						Line2.Segment line = default(Line2.Segment);
						float num7 = 1f;
						bool flag6 = false;
						if (m_CheckLot.x && MathUtils.Intersect(m_LotQuad.ab, segment, out t2) && t2.y < num7)
						{
							line = m_LotQuad.ab;
							num7 = t2.y;
							flag6 = false;
						}
						if (m_CheckLot.y && MathUtils.Intersect(m_LotQuad.bc, segment, out t2) && t2.y < num7)
						{
							line = m_LotQuad.bc;
							num7 = t2.y;
							flag6 = true;
						}
						if (m_CheckLot.z && MathUtils.Intersect(m_LotQuad.cd, segment, out t2) && t2.y < num7)
						{
							line = m_LotQuad.cd;
							num7 = t2.y;
							flag6 = false;
						}
						if (m_CheckLot.w && MathUtils.Intersect(m_LotQuad.da, segment, out t2) && t2.y < num7)
						{
							line = m_LotQuad.da;
							num7 = t2.y;
							flag6 = true;
						}
						if (num7 != 1f)
						{
							float num8 = MathUtils.Distance(line, m_Position.xz, out t2.x);
							flag5 = true;
							if (num8 < float3.x)
							{
								float2 float5 = MathUtils.Position(line, t2.x);
								float4.x = MathUtils.Distance(curve.m_Bezier.xz, float5, out t);
								float3 float6 = MathUtils.Position(curve.m_Bezier, t);
								float2 x = math.normalizesafe(float6.xz - m_Position.xz);
								float2 x2 = maxDistancePos.xz - m_Position.xz;
								float2 t3 = default(float2);
								float t4 = math.saturate(math.dot(x, math.normalizesafe(x2, t3)));
								segment = new Line2.Segment(float5, maxDistancePos.xz + MathUtils.ClampLength(maxDistancePos.xz - float5, 1f));
								if (flag6)
								{
									if (MathUtils.Intersect(segment, (Line2)m_LotQuad.ab, out t3) || MathUtils.Intersect(segment, (Line2)m_LotQuad.cd, out t3))
									{
										continue;
									}
								}
								else if (MathUtils.Intersect(segment, (Line2)m_LotQuad.bc, out t3) || MathUtils.Intersect(segment, (Line2)m_LotQuad.da, out t3))
								{
									continue;
								}
								num5 = float6.y;
								falseValue = new float2(m_Position.y - num5, m_Elevation - num3);
								falseValue = math.select(falseValue, 0f, (m_OnGround && !flag2) || flag4);
								num6 = math.max(0f, num4 - float4.x) * 0.1f / math.max(0.01f, num4);
								float4.x = math.max(0f, float4.x - num4) - num6;
								float4.x = num8 + float4.x * math.lerp(1f, 0.01f, t4);
								float4.y = math.cmin(math.abs(falseValue));
								float4.z = math.length(math.max(0f, float4.xy));
								float4.w = float4.z + math.min(0f, float4.x);
							}
						}
					}
					if (float4.w < float2.w)
					{
						entity2 = subLane;
						@float = float3;
						float2 = float4;
						resultCurvePos = t;
						bounds2 = new Bounds3(m_Position - float3.z, m_Position + float3.z);
						flag3 = flag5;
					}
				}
				if (!(entity2 != Entity.Null))
				{
					return;
				}
				if (maxDistance < @float.x && maxDistance < 10f)
				{
					@float.x = maxDistance;
					@float.z = math.length(math.max(0f, @float.xy));
					@float.w = @float.z + math.min(0f, @float.x);
					if (!flag3)
					{
						float2 = @float;
					}
				}
				if (float2.w < m_CmpDistance && !DirectlyConnected(netEntity, m_IgnoreOwner) && (!(m_IgnoreConnected != Entity.Null) || !(netEntity != m_IgnoreConnected) || !DirectlyConnected(netEntity, m_IgnoreConnected)))
				{
					if (m_ConnectionType == RouteConnectionType.Road && m_SlaveLaneData.TryGetComponent(entity2, out var componentData11))
					{
						entity2 = bufferData[componentData11.m_MasterIndex].m_SubLane;
					}
					m_CmpDistance = float2.w;
					m_MaxDistance = @float.x;
					m_ResultLane = entity2;
					m_ResultOwner = netEntity;
					m_ResultCurvePos = resultCurvePos;
					m_IntersectLot = flag3;
					if (!math.any(m_CheckLot))
					{
						m_Bounds = new Bounds3(m_Position - @float.z, m_Position + @float.z);
					}
				}
			}

			private bool DirectlyConnected(Entity netEntity1, Entity netEntity2)
			{
				if (m_EdgeData.HasComponent(netEntity1))
				{
					if (m_EdgeData.HasComponent(netEntity2))
					{
						Game.Net.Edge edge = m_EdgeData[netEntity1];
						Game.Net.Edge edge2 = m_EdgeData[netEntity2];
						if (edge.m_Start == edge2.m_Start || edge.m_Start == edge2.m_End || edge.m_End == edge2.m_Start || edge.m_End == edge2.m_End)
						{
							return true;
						}
					}
					else
					{
						Game.Net.Edge edge3 = m_EdgeData[netEntity1];
						if (edge3.m_Start == netEntity2 || edge3.m_End == netEntity2)
						{
							return true;
						}
					}
				}
				else if (m_EdgeData.HasComponent(netEntity2))
				{
					Game.Net.Edge edge4 = m_EdgeData[netEntity2];
					if (edge4.m_Start == netEntity1 || edge4.m_End == netEntity1)
					{
						return true;
					}
				}
				else if (m_ConnectedEdges.HasBuffer(netEntity1))
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[netEntity1];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity edge5 = dynamicBuffer[i].m_Edge;
						Game.Net.Edge edge6 = m_EdgeData[edge5];
						if ((!(edge6.m_Start != netEntity1) || !(edge6.m_End != netEntity1)) && (edge6.m_Start == netEntity2 || edge6.m_End == netEntity2))
						{
							return false;
						}
					}
				}
				return false;
			}
		}

		[ReadOnly]
		public ComponentLookup<Waypoint> m_WaypointData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabConnectionData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> m_NetOutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_ObjectOutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Surface> m_SurfaceData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_Segments;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<AccessLane> m_AccessLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Connected> m_ConnectedData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public NativeArray<Entity> m_UpdatedList;

		[ReadOnly]
		public AirwayHelpers.AirwayData m_AirwayData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public EntityArchetype m_PathTargetEventArchetype;

		public NativeQueue<PathTargetInfo>.ParallelWriter m_PathTargetInfo;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_UpdatedList[index];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			bool flag = false;
			bool onGround = false;
			float num = 0f;
			float3 @float;
			if (m_PositionData.HasComponent(entity))
			{
				@float = m_PositionData[entity].m_Position;
			}
			else
			{
				if (!m_TransformData.HasComponent(entity))
				{
					throw new Exception("FindWaypointConnectionsJob: Position not found!");
				}
				@float = m_TransformData[entity].m_Position;
				flag = true;
				if (m_ElevationData.HasComponent(entity))
				{
					num = m_ElevationData[entity].m_Elevation;
				}
				else
				{
					onGround = true;
				}
			}
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			Connected value = default(Connected);
			Entity entity2 = Entity.Null;
			Entity entity3 = Entity.Null;
			Entity preferOwner = Entity.Null;
			Entity entity4 = Entity.Null;
			Entity laneOwner = Entity.Null;
			AccessLane other = default(AccessLane);
			bool intersectLot = false;
			if (m_ConnectedData.HasComponent(entity))
			{
				value = m_ConnectedData[entity];
				if (value.m_Connected != Entity.Null && (!m_PrefabRefData.HasComponent(value.m_Connected) || m_DeletedData.HasComponent(value.m_Connected)))
				{
					value.m_Connected = Entity.Null;
					m_ConnectedData[entity] = value;
				}
				entity2 = GetLaneContainer(value.m_Connected);
				entity3 = GetMasterLot(value.m_Connected);
				if (m_AttachedData.TryGetComponent(value.m_Connected, out var componentData))
				{
					preferOwner = componentData.m_Parent;
				}
			}
			else
			{
				entity2 = GetLaneContainer(entity);
				entity3 = GetMasterLot(entity);
				if (m_AttachedData.TryGetComponent(entity, out var componentData2))
				{
					preferOwner = componentData2.m_Parent;
				}
			}
			if (m_TransformData.HasComponent(value.m_Connected))
			{
				@float = m_TransformData[value.m_Connected].m_Position;
				flag = true;
				if (m_ElevationData.HasComponent(value.m_Connected))
				{
					num = m_ElevationData[value.m_Connected].m_Elevation;
				}
				else
				{
					onGround = true;
				}
			}
			m_PrefabConnectionData.TryGetComponent(prefabRef.m_Prefab, out var componentData3);
			if (m_PrefabRefData.HasComponent(value.m_Connected))
			{
				PrefabRef prefabRef2 = m_PrefabRefData[value.m_Connected];
				if (m_PrefabConnectionData.HasComponent(prefabRef2.m_Prefab))
				{
					RouteConnectionData routeConnectionData = m_PrefabConnectionData[prefabRef2.m_Prefab];
					componentData3.m_StartLaneOffset = math.max(componentData3.m_StartLaneOffset, routeConnectionData.m_StartLaneOffset);
					componentData3.m_EndMargin = math.max(componentData3.m_EndMargin, routeConnectionData.m_EndMargin);
				}
			}
			if (m_AccessLaneData.HasComponent(entity))
			{
				AccessLane accessLane = m_AccessLaneData[entity];
				other = accessLane;
				bool flag5 = true;
				if (m_PrefabRefData.HasComponent(value.m_Connected))
				{
					PrefabRef prefabRef3 = m_PrefabRefData[value.m_Connected];
					if (m_PrefabSpawnLocationData.HasComponent(prefabRef3.m_Prefab) && m_PrefabConnectionData.HasComponent(prefabRef3.m_Prefab))
					{
						SpawnLocationData spawnLocationData = m_PrefabSpawnLocationData[prefabRef3.m_Prefab];
						RouteConnectionData routeConnectionData2 = m_PrefabConnectionData[prefabRef3.m_Prefab];
						if (spawnLocationData.m_ConnectionType != RouteConnectionType.None && spawnLocationData.m_ConnectionType == routeConnectionData2.m_AccessConnectionType && spawnLocationData.m_ActivityMask.m_Mask == 0)
						{
							accessLane.m_Lane = value.m_Connected;
							accessLane.m_CurvePos = 0f;
							flag5 = false;
						}
					}
				}
				if (flag5)
				{
					FindLane(entity2, @float, num, componentData3.m_AccessConnectionType, componentData3.m_AccessTrackType, componentData3.m_AccessRoadType, Entity.Null, preferOwner, entity3, onGround, checkEdges: false, 0, out laneOwner, out accessLane.m_Lane, out accessLane.m_CurvePos, out intersectLot);
				}
				m_AccessLaneData[entity] = accessLane;
				flag2 = !accessLane.Equals(other);
				flag3 = flag2 || m_UpdatedData.HasComponent(accessLane.m_Lane);
				entity4 = accessLane.m_Lane;
			}
			if (m_RouteLaneData.HasComponent(entity))
			{
				RouteLane routeLane = m_RouteLaneData[entity];
				RouteLane other2 = routeLane;
				if (m_PrefabRefData.HasComponent(value.m_Connected))
				{
					PrefabRef prefabRef4 = m_PrefabRefData[value.m_Connected];
					if (m_PrefabConnectionData.HasComponent(prefabRef4.m_Prefab))
					{
						RouteConnectionData routeConnectionData3 = m_PrefabConnectionData[prefabRef4.m_Prefab];
						componentData3.m_StartLaneOffset = math.max(componentData3.m_StartLaneOffset, routeConnectionData3.m_StartLaneOffset);
						componentData3.m_EndMargin = math.max(componentData3.m_EndMargin, routeConnectionData3.m_EndMargin);
					}
				}
				float3 float2 = @float;
				float elevation = num;
				if (componentData3.m_RouteConnectionType == RouteConnectionType.Air && componentData3.m_RouteRoadType == RoadTypes.Airplane && entity4 != Entity.Null && !m_ConnectionLaneData.HasComponent(entity4))
				{
					Curve curve = m_CurveData[entity4];
					float2 value2 = (curve.m_Bezier.a + curve.m_Bezier.d - float2 * 2f).xz;
					if (MathUtils.TryNormalize(ref value2, 1500f))
					{
						float2.xz -= value2;
						elevation = 1000f;
					}
				}
				Entity entity5 = Entity.Null;
				int shouldIntersectLot = 0;
				if (componentData3.m_RouteConnectionType == componentData3.m_AccessConnectionType)
				{
					switch (componentData3.m_RouteConnectionType)
					{
					case RouteConnectionType.Road:
					case RouteConnectionType.Cargo:
					case RouteConnectionType.Air:
						if (componentData3.m_AccessRoadType == componentData3.m_RouteRoadType)
						{
							entity5 = laneOwner;
						}
						break;
					case RouteConnectionType.Track:
						if (componentData3.m_AccessTrackType == componentData3.m_RouteTrackType)
						{
							entity5 = laneOwner;
						}
						break;
					default:
						entity5 = laneOwner;
						shouldIntersectLot = math.select(1, -1, intersectLot);
						break;
					}
				}
				int num2;
				if (componentData3.m_RouteConnectionType == RouteConnectionType.Pedestrian)
				{
					num2 = ((componentData3.m_AccessConnectionType == RouteConnectionType.Pedestrian) ? 1 : 0);
					if (num2 != 0)
					{
						componentData3.m_RouteRoadType = RoadTypes.Bicycle;
					}
				}
				else
				{
					num2 = 0;
				}
				FindLane(entity2, float2, elevation, componentData3.m_RouteConnectionType, componentData3.m_RouteTrackType, componentData3.m_RouteRoadType, entity5, preferOwner, entity3, onGround, checkEdges: false, shouldIntersectLot, out var laneOwner2, out routeLane.m_EndLane, out routeLane.m_EndCurvePos, out var intersectLot2);
				routeLane.m_StartLane = Entity.Null;
				if (num2 != 0)
				{
					if (m_CarLaneData.HasComponent(routeLane.m_EndLane))
					{
						routeLane.m_StartLane = routeLane.m_EndLane;
						routeLane.m_StartCurvePos = routeLane.m_EndCurvePos;
						routeLane.m_EndLane = Entity.Null;
						routeLane.m_EndCurvePos = 0f;
					}
					else if (m_PedestrianLaneData.HasComponent(entity4) && !m_PedestrianLaneData.HasComponent(routeLane.m_EndLane))
					{
						AccessLane value3 = m_AccessLaneData[entity];
						CommonUtils.Swap(ref value3.m_Lane, ref routeLane.m_EndLane);
						CommonUtils.Swap(ref value3.m_CurvePos, ref routeLane.m_EndCurvePos);
						CommonUtils.Swap(ref laneOwner, ref laneOwner2);
						entity4 = value3.m_Lane;
						m_AccessLaneData[entity] = value3;
						flag2 = !value3.Equals(other);
						flag3 = flag2;
						if (entity5 != Entity.Null)
						{
							entity5 = laneOwner;
						}
					}
					Entity laneOwner3;
					if (m_PedestrianLaneData.HasComponent(routeLane.m_EndLane))
					{
						FindLane(laneOwner2, float2, elevation, RouteConnectionType.Road, TrackTypes.None, RoadTypes.Bicycle, entity5, preferOwner, entity3, onGround, checkEdges: true, shouldIntersectLot, out laneOwner3, out routeLane.m_StartLane, out routeLane.m_StartCurvePos, out intersectLot2);
					}
					else if (m_CarLaneData.HasComponent(routeLane.m_StartLane))
					{
						FindLane(laneOwner2, float2, elevation, RouteConnectionType.Pedestrian, TrackTypes.None, RoadTypes.None, entity5, preferOwner, entity3, onGround, checkEdges: true, shouldIntersectLot, out laneOwner3, out routeLane.m_EndLane, out routeLane.m_EndCurvePos, out intersectLot2);
					}
				}
				if (routeLane.m_StartLane == Entity.Null)
				{
					routeLane.m_StartLane = routeLane.m_EndLane;
					routeLane.m_StartCurvePos = routeLane.m_EndCurvePos;
				}
				if (componentData3.m_StartLaneOffset > 0f && m_CarLaneData.TryGetComponent(routeLane.m_EndLane, out var componentData4) && (componentData4.m_Flags & CarLaneFlags.Twoway) != 0)
				{
					componentData3.m_StartLaneOffset = 0f;
				}
				if (componentData3.m_StartLaneOffset > 0f || componentData3.m_EndMargin > 0f)
				{
					MoveLaneOffsets(ref routeLane.m_StartLane, ref routeLane.m_StartCurvePos, ref routeLane.m_EndLane, ref routeLane.m_EndCurvePos, componentData3.m_StartLaneOffset, componentData3.m_EndMargin);
				}
				if (entity4 != Entity.Null && routeLane.m_EndLane != Entity.Null && !ValidateConnection(entity4, routeLane.m_EndLane, entity5, laneOwner2))
				{
					Entity ownerBuilding = GetOwnerBuilding(entity);
					Entity ownerBuilding2 = GetOwnerBuilding(entity4);
					Entity ownerBuilding3 = GetOwnerBuilding(routeLane.m_EndLane);
					if (ownerBuilding == ownerBuilding2 && ownerBuilding == ownerBuilding3)
					{
						AccessLane value4 = m_AccessLaneData[entity];
						value4.m_Lane = Entity.Null;
						value4.m_CurvePos = 0f;
						m_AccessLaneData[entity] = value4;
						flag2 = !value4.Equals(other);
						flag3 = flag2;
					}
					routeLane.m_StartLane = Entity.Null;
					routeLane.m_EndLane = Entity.Null;
					routeLane.m_StartCurvePos = 0f;
					routeLane.m_EndCurvePos = 0f;
				}
				m_RouteLaneData[entity] = routeLane;
				flag2 |= !routeLane.Equals(other2);
				flag3 |= flag2 || m_UpdatedData.HasComponent(routeLane.m_StartLane) || m_UpdatedData.HasComponent(routeLane.m_EndLane);
				if (!flag && m_CurveData.HasComponent(routeLane.m_EndLane))
				{
					@float = MathUtils.Position(m_CurveData[routeLane.m_EndLane].m_Bezier, routeLane.m_EndCurvePos);
				}
			}
			if (m_PositionData.HasComponent(entity))
			{
				Position value5 = m_PositionData[entity];
				if (math.distance(@float, value5.m_Position) > 0.1f)
				{
					if (!m_TempData.HasComponent(entity))
					{
						Entity e = m_CommandBuffer.CreateEntity(index, m_PathTargetEventArchetype);
						m_CommandBuffer.SetComponent(index, e, new PathTargetMoved(entity, value5.m_Position, @float));
					}
					value5.m_Position = @float;
					m_PositionData[entity] = value5;
					flag2 = true;
					flag4 = true;
				}
			}
			if (flag2)
			{
				if (m_WaypointData.HasComponent(entity))
				{
					Waypoint waypoint = m_WaypointData[entity];
					Owner owner = m_OwnerData[entity];
					DynamicBuffer<RouteSegment> dynamicBuffer = m_Segments[owner.m_Owner];
					int index2 = math.select(waypoint.m_Index - 1, dynamicBuffer.Length - 1, waypoint.m_Index == 0);
					RouteSegment routeSegment = dynamicBuffer[index2];
					RouteSegment routeSegment2 = dynamicBuffer[waypoint.m_Index];
					if (routeSegment.m_Segment != Entity.Null)
					{
						m_CommandBuffer.AddComponent(index, routeSegment.m_Segment, default(Updated));
						if (flag4)
						{
							m_PathTargetInfo.Enqueue(new PathTargetInfo
							{
								m_Segment = routeSegment.m_Segment,
								m_Start = false
							});
						}
					}
					if (routeSegment2.m_Segment != Entity.Null)
					{
						m_CommandBuffer.AddComponent(index, routeSegment2.m_Segment, default(Updated));
						if (flag4)
						{
							m_PathTargetInfo.Enqueue(new PathTargetInfo
							{
								m_Segment = routeSegment2.m_Segment,
								m_Start = true
							});
						}
					}
				}
				m_CommandBuffer.AddComponent(index, entity, default(Updated));
				if (m_TransformData.HasComponent(entity) && m_OwnerData.HasComponent(entity) && !m_TempData.HasComponent(entity))
				{
					UpdateSurfaces(index, m_TransformData[entity].m_Position);
				}
			}
			else if (flag3)
			{
				m_CommandBuffer.AddComponent(index, entity, default(PathfindUpdated));
			}
		}

		private Entity GetOwnerBuilding(Entity entity)
		{
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData) && !m_BuildingData.HasComponent(entity))
			{
				entity = componentData.m_Owner;
			}
			return entity;
		}

		private void UpdateSurfaces(int jobIndex, float3 position)
		{
			SurfaceIterator iterator = new SurfaceIterator
			{
				m_Position = position,
				m_JobIndex = jobIndex,
				m_SurfaceData = m_SurfaceData,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_CommandBuffer = m_CommandBuffer
			};
			m_AreaSearchTree.Iterate(ref iterator);
		}

		private bool ValidateConnection(Entity accessLaneEntity, Entity routeLaneEntity, Entity accessLaneOwner, Entity routeLaneOwner)
		{
			if (m_LaneData.HasComponent(accessLaneEntity) && m_LaneData.HasComponent(routeLaneEntity))
			{
				Lane lane = m_LaneData[accessLaneEntity];
				Lane lane2 = m_LaneData[routeLaneEntity];
				if (lane.m_StartNode.EqualsIgnoreCurvePos(lane2.m_StartNode) || lane.m_StartNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode) || lane.m_StartNode.EqualsIgnoreCurvePos(lane2.m_EndNode) || lane.m_MiddleNode.EqualsIgnoreCurvePos(lane2.m_StartNode) || lane.m_MiddleNode.EqualsIgnoreCurvePos(lane2.m_EndNode) || lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_StartNode) || lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode) || lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_EndNode))
				{
					return false;
				}
			}
			if (m_EdgeData.HasComponent(accessLaneOwner))
			{
				Game.Net.Edge edge = m_EdgeData[accessLaneOwner];
				if (!ValidateConnectedEdges(edge.m_Start, routeLaneOwner))
				{
					return false;
				}
				if (!ValidateConnectedEdges(edge.m_End, routeLaneOwner))
				{
					return false;
				}
			}
			else if (m_ConnectedEdges.HasBuffer(accessLaneOwner) && !ValidateConnectedEdges(accessLaneOwner, routeLaneOwner))
			{
				return false;
			}
			if (m_EdgeData.HasComponent(routeLaneOwner))
			{
				Game.Net.Edge edge2 = m_EdgeData[routeLaneOwner];
				if (!ValidateConnectedEdges(edge2.m_Start, accessLaneOwner))
				{
					return false;
				}
				if (!ValidateConnectedEdges(edge2.m_End, accessLaneOwner))
				{
					return false;
				}
			}
			else if (m_ConnectedEdges.HasBuffer(routeLaneOwner) && !ValidateConnectedEdges(routeLaneOwner, accessLaneOwner))
			{
				return false;
			}
			return true;
		}

		private bool ValidateConnectedEdges(Entity node, Entity other)
		{
			if (node == other)
			{
				return false;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				if (dynamicBuffer[i].m_Edge == other)
				{
					return false;
				}
			}
			return true;
		}

		private Entity GetLaneContainer(Entity entity)
		{
			if (m_OwnerData.HasComponent(entity))
			{
				Owner owner = m_OwnerData[entity];
				if (m_NetOutsideConnectionData.HasComponent(owner.m_Owner) && m_Lanes.HasBuffer(owner.m_Owner))
				{
					return owner.m_Owner;
				}
			}
			if (m_ObjectOutsideConnectionData.HasComponent(entity) && m_Lanes.HasBuffer(entity))
			{
				return entity;
			}
			return Entity.Null;
		}

		private Entity GetMasterLot(Entity entity)
		{
			Entity result = Entity.Null;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
				if (m_LotData.HasComponent(entity))
				{
					result = entity;
				}
			}
			return result;
		}

		private void MoveLaneOffsets(ref Entity startLane, ref float startCurvePos, ref Entity endLane, ref float endCurvePos, float startOffset, float endMargin)
		{
			if (!m_CurveData.TryGetComponent(endLane, out var curve))
			{
				return;
			}
			Entity prevLane = Entity.Null;
			Entity nextLane = Entity.Null;
			Curve prevCurve = default(Curve);
			Curve nextCurve = default(Curve);
			if (m_OwnerData.TryGetComponent(endLane, out var componentData) && m_LaneData.TryGetComponent(endLane, out var componentData2) && m_Lanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subLane = bufferData[i].m_SubLane;
					Lane lane = m_LaneData[subLane];
					if (lane.m_EndNode.Equals(componentData2.m_StartNode))
					{
						prevLane = subLane;
						prevCurve = m_CurveData[subLane];
						if (nextLane != Entity.Null)
						{
							break;
						}
					}
					else if (lane.m_StartNode.Equals(componentData2.m_EndNode))
					{
						nextLane = subLane;
						nextCurve = m_CurveData[subLane];
						if (prevLane != Entity.Null)
						{
							break;
						}
					}
				}
			}
			float prevDistance = MathUtils.Length(curve.m_Bezier.xz, new Bounds1(0f, endCurvePos));
			float totalPrevDistance = prevDistance;
			if (prevLane != Entity.Null)
			{
				totalPrevDistance += MathUtils.Length(prevCurve.m_Bezier.xz);
			}
			float nextDistance = MathUtils.Length(curve.m_Bezier.xz, new Bounds1(endCurvePos, 1f));
			float totalNextDistance = nextDistance;
			if (nextLane != Entity.Null)
			{
				totalNextDistance += MathUtils.Length(nextCurve.m_Bezier.xz);
			}
			float num = math.max(startOffset, endMargin);
			float num2 = 0f;
			if (num + endMargin > totalPrevDistance + totalNextDistance)
			{
				num2 = totalNextDistance - endMargin * (totalPrevDistance + totalNextDistance) / (num + endMargin);
			}
			else if (num > totalPrevDistance)
			{
				num2 = num - totalPrevDistance;
			}
			else if (endMargin > totalNextDistance)
			{
				num2 = totalNextDistance - endMargin;
			}
			MoveLaneOffset(num2, ref endLane, ref endCurvePos);
			if (startOffset == 0f)
			{
				startLane = endLane;
				startCurvePos = endCurvePos;
			}
			else
			{
				startOffset = num2 - startOffset;
				MoveLaneOffset(startOffset, ref startLane, ref startCurvePos);
			}
			void MoveLaneOffset(float offset, ref Entity reference, ref float curvePos)
			{
				if (offset < 0f)
				{
					if (offset <= 0f - totalPrevDistance)
					{
						curvePos = 0f;
						if (prevLane != Entity.Null)
						{
							reference = prevLane;
						}
					}
					else if (offset <= 0f - prevDistance)
					{
						curvePos = 0f;
						offset += prevDistance;
						if (offset < 0f && prevLane != Entity.Null)
						{
							reference = prevLane;
							Bounds1 t = new Bounds1(0f, 1f);
							if (MathUtils.ClampLengthInverse(prevCurve.m_Bezier.xz, ref t, 0f - offset))
							{
								curvePos = t.min;
							}
						}
					}
					else
					{
						Bounds1 t2 = new Bounds1(0f, curvePos);
						if (MathUtils.ClampLengthInverse(curve.m_Bezier.xz, ref t2, 0f - offset))
						{
							curvePos = t2.min;
						}
						else
						{
							curvePos = 0f;
						}
					}
				}
				else if (offset > 0f)
				{
					if (offset >= totalNextDistance)
					{
						curvePos = 1f;
						if (nextLane != Entity.Null)
						{
							reference = nextLane;
						}
					}
					else if (offset >= nextDistance)
					{
						curvePos = 1f;
						offset -= nextDistance;
						if (offset > 0f && nextLane != Entity.Null)
						{
							reference = nextLane;
							Bounds1 t3 = new Bounds1(0f, 1f);
							if (MathUtils.ClampLength(nextCurve.m_Bezier.xz, ref t3, offset))
							{
								curvePos = t3.max;
							}
						}
					}
					else
					{
						Bounds1 t4 = new Bounds1(curvePos, 1f);
						if (MathUtils.ClampLength(curve.m_Bezier.xz, ref t4, offset))
						{
							curvePos = t4.max;
						}
						else
						{
							curvePos = 1f;
						}
					}
				}
			}
		}

		private void FindLane(Entity laneContainer, float3 position, float elevation, RouteConnectionType connectionType, TrackTypes trackTypes, RoadTypes roadTypes, Entity ignoreOwner, Entity preferOwner, Entity masterLot, bool onGround, bool checkEdges, int shouldIntersectLot, out Entity laneOwner, out Entity lane, out float curvePos, out bool intersectLot)
		{
			if (connectionType == RouteConnectionType.Air)
			{
				laneOwner = Entity.Null;
				lane = Entity.Null;
				curvePos = 0f;
				intersectLot = false;
				float distance = float.MaxValue;
				if ((roadTypes & RoadTypes.Helicopter) != RoadTypes.None)
				{
					m_AirwayData.helicopterMap.FindClosestLane(position, m_CurveData, ref lane, ref curvePos, ref distance);
				}
				if ((roadTypes & RoadTypes.Airplane) != RoadTypes.None)
				{
					m_AirwayData.airplaneMap.FindClosestLane(position, m_CurveData, ref lane, ref curvePos, ref distance);
				}
				return;
			}
			if (laneContainer != Entity.Null && laneContainer != ignoreOwner)
			{
				DynamicBuffer<ConnectedEdge> bufferData = default(DynamicBuffer<ConnectedEdge>);
				float num = float.MaxValue;
				laneOwner = laneContainer;
				lane = Entity.Null;
				curvePos = 0f;
				intersectLot = false;
				int num2 = 1;
				if (checkEdges && m_ConnectedEdges.TryGetBuffer(laneContainer, out bufferData))
				{
					num2 += bufferData.Length;
				}
				for (int i = 0; i < num2; i++)
				{
					Entity entity = laneContainer;
					bool flag = false;
					if (i > 0)
					{
						entity = bufferData[i - 1].m_Edge;
						if (!m_EdgeData.TryGetComponent(entity, out var componentData) || (componentData.m_Start != laneContainer && componentData.m_End != laneContainer))
						{
							continue;
						}
					}
					if (!m_Lanes.TryGetBuffer(entity, out var bufferData2))
					{
						continue;
					}
					for (int j = 0; j < bufferData2.Length; j++)
					{
						Entity subLane = bufferData2[j].m_SubLane;
						Game.Net.CarLane componentData2;
						Game.Net.PedestrianLane componentData4;
						if (m_ConnectionLaneData.HasComponent(subLane))
						{
							Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[subLane];
							switch (connectionType)
							{
							case RouteConnectionType.Pedestrian:
								if ((connectionLane.m_Flags & (ConnectionLaneFlags.Pedestrian | ConnectionLaneFlags.AllowMiddle)) != (ConnectionLaneFlags.Pedestrian | ConnectionLaneFlags.AllowMiddle))
								{
									continue;
								}
								break;
							case RouteConnectionType.Road:
								if ((connectionLane.m_Flags & (ConnectionLaneFlags.Road | ConnectionLaneFlags.AllowMiddle)) != (ConnectionLaneFlags.Road | ConnectionLaneFlags.AllowMiddle) || (connectionLane.m_RoadTypes & roadTypes) == 0)
								{
									continue;
								}
								break;
							case RouteConnectionType.Track:
								if ((connectionLane.m_Flags & (ConnectionLaneFlags.Track | ConnectionLaneFlags.AllowMiddle)) != (ConnectionLaneFlags.Track | ConnectionLaneFlags.AllowMiddle) || (connectionLane.m_TrackTypes & trackTypes) == 0)
								{
									continue;
								}
								break;
							case RouteConnectionType.Cargo:
								if ((connectionLane.m_Flags & (ConnectionLaneFlags.AllowMiddle | ConnectionLaneFlags.AllowCargo)) != (ConnectionLaneFlags.AllowMiddle | ConnectionLaneFlags.AllowCargo))
								{
									continue;
								}
								break;
							default:
								continue;
							}
							float t;
							float num3 = MathUtils.Distance(m_CurveData[subLane].m_Bezier, position, out t);
							t = math.select(t, 1f, (connectionLane.m_Flags & ConnectionLaneFlags.Start) != 0);
							if (num3 < num)
							{
								num = num3;
								lane = subLane;
								curvePos = t;
								flag = true;
							}
						}
						else if (connectionType == RouteConnectionType.Road && m_CarLaneData.TryGetComponent(subLane, out componentData2) && !m_MasterLaneData.HasComponent(subLane) && (componentData2.m_Flags & CarLaneFlags.Unsafe) == 0)
						{
							PrefabRef prefabRef = m_PrefabRefData[subLane];
							if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3) && (componentData3.m_RoadTypes & roadTypes) != RoadTypes.None)
							{
								float t2;
								float num4 = MathUtils.Distance(m_CurveData[subLane].m_Bezier, position, out t2);
								if (num4 < num)
								{
									num = num4;
									lane = subLane;
									curvePos = t2;
									flag = true;
								}
							}
						}
						else if (connectionType == RouteConnectionType.Pedestrian && m_PedestrianLaneData.TryGetComponent(subLane, out componentData4) && (componentData4.m_Flags & (PedestrianLaneFlags.AllowMiddle | PedestrianLaneFlags.OnWater)) == PedestrianLaneFlags.AllowMiddle)
						{
							float t3;
							float num5 = MathUtils.Distance(m_CurveData[subLane].m_Bezier, position, out t3);
							if (num5 < num)
							{
								num = num5;
								lane = subLane;
								curvePos = t3;
								flag = true;
							}
						}
					}
					if (flag && m_SlaveLaneData.TryGetComponent(lane, out var componentData5))
					{
						lane = bufferData2[componentData5.m_MasterIndex].m_SubLane;
					}
				}
				return;
			}
			float num6 = 10f;
			LaneIterator iterator = new LaneIterator
			{
				m_Bounds = new Bounds3(position - num6, position + num6),
				m_Position = position,
				m_ConnectionType = connectionType,
				m_TrackType = trackTypes,
				m_CarType = roadTypes,
				m_OnGround = onGround,
				m_CmpDistance = num6,
				m_MaxDistance = num6,
				m_Elevation = elevation,
				m_IgnoreOwner = ignoreOwner,
				m_IgnoreConnected = preferOwner,
				m_MasterLot = masterLot,
				m_OwnerData = m_OwnerData,
				m_PedestrianLaneData = m_PedestrianLaneData,
				m_CarLaneData = m_CarLaneData,
				m_TrackLaneData = m_TrackLaneData,
				m_MasterLaneData = m_MasterLaneData,
				m_SlaveLaneData = m_SlaveLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_EdgeData = m_EdgeData,
				m_ElevationData = m_NetElevationData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_StartNodeGeometryData = m_StartNodeGeometryData,
				m_EndNodeGeometryData = m_EndNodeGeometryData,
				m_CompositionData = m_CompositionData,
				m_LotData = m_LotData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabTrackLaneData = m_PrefabTrackLaneData,
				m_PrefabCarLaneData = m_PrefabCarLaneData,
				m_PrefabNetLaneData = m_PrefabNetLaneData,
				m_PrefabNetData = m_PrefabNetData,
				m_PrefabNetCompositionData = m_PrefabNetCompositionData,
				m_Lanes = m_Lanes,
				m_ConnectedEdges = m_ConnectedEdges,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles
			};
			Entity entity2 = ignoreOwner;
			if (entity2 != Entity.Null)
			{
				Owner componentData6;
				while (m_OwnerData.TryGetComponent(entity2, out componentData6) && !m_BuildingData.HasComponent(entity2))
				{
					entity2 = componentData6.m_Owner;
				}
			}
			float num7 = float.MaxValue;
			if (entity2 != Entity.Null && m_PrefabRefData.TryGetComponent(entity2, out var componentData7) && m_TransformData.TryGetComponent(entity2, out var componentData8) && m_PrefabBuildingData.TryGetComponent(componentData7.m_Prefab, out var componentData9))
			{
				float2 size = (float2)componentData9.m_LotSize * 8f;
				iterator.m_LotQuad = ObjectUtils.CalculateBaseCorners(componentData8.m_Position, componentData8.m_Rotation, size).xz;
				iterator.m_CheckLot = new bool4(x: true, (componentData9.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0, (componentData9.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) != 0, (componentData9.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0);
				float4 @float = float.MaxValue;
				float t4;
				if (iterator.m_CheckLot.x)
				{
					@float.x = MathUtils.Distance(iterator.m_LotQuad.ab, position.xz, out t4);
				}
				if (iterator.m_CheckLot.y)
				{
					@float.y = MathUtils.Distance(iterator.m_LotQuad.bc, position.xz, out t4);
				}
				if (iterator.m_CheckLot.z)
				{
					@float.z = MathUtils.Distance(iterator.m_LotQuad.cd, position.xz, out t4);
				}
				if (iterator.m_CheckLot.w)
				{
					@float.w = MathUtils.Distance(iterator.m_LotQuad.da, position.xz, out t4);
				}
				num7 = math.cmin(@float);
				iterator.m_CheckLot &= @float <= num7 * 2f;
			}
			m_NetSearchTree.Iterate(ref iterator);
			if (math.any(iterator.m_CheckLot))
			{
				iterator.m_Bounds = new Bounds3(position - iterator.m_CmpDistance, position + iterator.m_CmpDistance);
			}
			m_AreaSearchTree.Iterate(ref iterator);
			if (shouldIntersectLot != 0 && math.any(iterator.m_CheckLot))
			{
				if (shouldIntersectLot == -1)
				{
					if (iterator.m_IntersectLot)
					{
						iterator = default(LaneIterator);
					}
				}
				else if (!iterator.m_IntersectLot && iterator.m_MaxDistance > num7)
				{
					iterator = default(LaneIterator);
				}
			}
			laneOwner = iterator.m_ResultOwner;
			lane = iterator.m_ResultLane;
			curvePos = iterator.m_ResultCurvePos;
			intersectLot = iterator.m_IntersectLot;
		}
	}

	private struct PathTargetInfo
	{
		public Entity m_Segment;

		public bool m_Start;
	}

	[BurstCompile]
	private struct ClearPathTargetsJob : IJob
	{
		public NativeQueue<PathTargetInfo> m_PathTargetInfo;

		public ComponentLookup<PathTargets> m_PathTargetsData;

		public void Execute()
		{
			int count = m_PathTargetInfo.Count;
			for (int i = 0; i < count; i++)
			{
				PathTargetInfo pathTargetInfo = m_PathTargetInfo.Dequeue();
				if (m_PathTargetsData.HasComponent(pathTargetInfo.m_Segment))
				{
					PathTargets value = m_PathTargetsData[pathTargetInfo.m_Segment];
					if (pathTargetInfo.m_Start)
					{
						value.m_StartLane = Entity.Null;
						value.m_CurvePositions.x = 0f;
					}
					else
					{
						value.m_EndLane = Entity.Null;
						value.m_CurvePositions.y = 0f;
					}
					m_PathTargetsData[pathTargetInfo.m_Segment] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Connected> __Game_Routes_Connected_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AccessLane> __Game_Routes_AccessLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> __Game_Routes_RouteLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccessLane> __Game_Routes_AccessLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Surface> __Game_Areas_Surface_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public ComponentLookup<AccessLane> __Game_Routes_AccessLane_RW_ComponentLookup;

		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RW_ComponentLookup;

		public ComponentLookup<Connected> __Game_Routes_Connected_RW_ComponentLookup;

		public ComponentLookup<Position> __Game_Routes_Position_RW_ComponentLookup;

		public ComponentLookup<PathTargets> __Game_Routes_PathTargets_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_Connected_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Connected>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RouteLane>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RW_BufferLookup = state.GetBufferLookup<ConnectedRoute>();
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentLookup = state.GetComponentLookup<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.OutsideConnection>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Areas_Surface_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Surface>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Routes_AccessLane_RW_ComponentLookup = state.GetComponentLookup<AccessLane>();
			__Game_Routes_RouteLane_RW_ComponentLookup = state.GetComponentLookup<RouteLane>();
			__Game_Routes_Connected_RW_ComponentLookup = state.GetComponentLookup<Connected>();
			__Game_Routes_Position_RW_ComponentLookup = state.GetComponentLookup<Position>();
			__Game_Routes_PathTargets_RW_ComponentLookup = state.GetComponentLookup<PathTargets>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private Game.Net.UpdateCollectSystem m_NetUpdateCollectSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private AirwaySystem m_AirwaySystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Areas.UpdateCollectSystem m_AreaUpdateCollectSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private SearchSystem m_RouteSearchSystem;

	private EntityQuery m_WaypointQuery;

	private EntityArchetype m_PathTargetEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_NetUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Net.UpdateCollectSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_AreaUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Areas.UpdateCollectSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_RouteSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_WaypointQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<AccessLane>(),
				ComponentType.ReadOnly<RouteLane>(),
				ComponentType.ReadOnly<ConnectedRoute>()
			},
			None = new ComponentType[0]
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<AccessLane>(),
				ComponentType.ReadOnly<RouteLane>(),
				ComponentType.ReadOnly<ConnectedRoute>()
			},
			None = new ComponentType[0]
		});
		m_PathTargetEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<PathTargetMoved>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_WaypointQuery.IsEmptyIgnoreFilter || m_NetUpdateCollectSystem.netsUpdated || m_AreaUpdateCollectSystem.lotsUpdated)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			JobHandle jobHandle = default(JobHandle);
			if (!m_WaypointQuery.IsEmptyIgnoreFilter)
			{
				jobHandle = JobChunkExtensions.Schedule(new UpdateWaypointReferencesJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_ConnectedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_AccessLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_RouteLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ConnectedRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RW_BufferLookup, ref base.CheckedStateRef),
					m_UpdatedList = nativeList
				}, m_WaypointQuery, base.Dependency);
			}
			JobHandle jobHandle2 = jobHandle;
			if (m_NetUpdateCollectSystem.netsUpdated)
			{
				NativeQueue<Entity> updatedQueue = new NativeQueue<Entity>(Allocator.TempJob);
				JobHandle dependencies;
				NativeList<Bounds2> updatedNetBounds = m_NetUpdateCollectSystem.GetUpdatedNetBounds(out dependencies);
				JobHandle dependencies2;
				JobHandle dependencies3;
				FindUpdatedWaypointsJob jobData = new FindUpdatedWaypointsJob
				{
					m_Bounds = updatedNetBounds.AsDeferredJobArray(),
					m_RouteSearchTree = m_RouteSearchSystem.GetSearchTree(readOnly: true, out dependencies2),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
					m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
					m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue.AsParallelWriter()
				};
				DequeUpdatedWaypointsJob jobData2 = new DequeUpdatedWaypointsJob
				{
					m_UpdatedQueue = updatedQueue,
					m_UpdatedList = nativeList
				};
				JobHandle job = JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3);
				JobHandle jobHandle3 = jobData.Schedule(updatedNetBounds, 1, JobHandle.CombineDependencies(base.Dependency, job));
				jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle2, jobHandle3));
				updatedQueue.Dispose(jobHandle2);
				m_NetUpdateCollectSystem.AddNetBoundsReader(jobHandle3);
				m_RouteSearchSystem.AddSearchTreeReader(jobHandle3);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle3);
			}
			if (m_AreaUpdateCollectSystem.lotsUpdated)
			{
				NativeQueue<Entity> updatedQueue2 = new NativeQueue<Entity>(Allocator.TempJob);
				JobHandle dependencies4;
				NativeList<Bounds2> updatedLotBounds = m_AreaUpdateCollectSystem.GetUpdatedLotBounds(out dependencies4);
				JobHandle dependencies5;
				JobHandle dependencies6;
				FindUpdatedWaypointsJob jobData3 = new FindUpdatedWaypointsJob
				{
					m_Bounds = updatedLotBounds.AsDeferredJobArray(),
					m_RouteSearchTree = m_RouteSearchSystem.GetSearchTree(readOnly: true, out dependencies5),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies6),
					m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
					m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue2.AsParallelWriter()
				};
				DequeUpdatedWaypointsJob jobData4 = new DequeUpdatedWaypointsJob
				{
					m_UpdatedQueue = updatedQueue2,
					m_UpdatedList = nativeList
				};
				JobHandle job2 = JobHandle.CombineDependencies(dependencies4, dependencies5, dependencies6);
				JobHandle jobHandle4 = jobData3.Schedule(updatedLotBounds, 1, JobHandle.CombineDependencies(base.Dependency, job2));
				jobHandle2 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(jobHandle2, jobHandle4));
				updatedQueue2.Dispose(jobHandle2);
				m_AreaUpdateCollectSystem.AddLotBoundsReader(jobHandle4);
				m_RouteSearchSystem.AddSearchTreeReader(jobHandle4);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle4);
			}
			NativeQueue<PathTargetInfo> pathTargetInfo = new NativeQueue<PathTargetInfo>(Allocator.TempJob);
			RemoveDuplicatedWaypointsJob jobData5 = new RemoveDuplicatedWaypointsJob
			{
				m_UpdatedList = nativeList
			};
			JobHandle dependencies7;
			JobHandle dependencies8;
			FindWaypointConnectionsJob jobData6 = new FindWaypointConnectionsJob
			{
				m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetOutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectOutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SurfaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Surface_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Segments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
				m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RW_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedList = nativeList.AsDeferredJobArray(),
				m_AirwayData = m_AirwaySystem.GetAirwayData(),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies7),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies8),
				m_PathTargetEventArchetype = m_PathTargetEventArchetype,
				m_PathTargetInfo = pathTargetInfo.AsParallelWriter(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			ClearPathTargetsJob jobData7 = new ClearPathTargetsJob
			{
				m_PathTargetInfo = pathTargetInfo,
				m_PathTargetsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathTargets_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			JobHandle jobHandle5 = IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(IJobExtensions.Schedule(jobData5, jobHandle2), dependencies7, dependencies8), jobData: jobData6, list: nativeList, innerloopBatchCount: 1);
			JobHandle jobHandle6 = IJobExtensions.Schedule(jobData7, jobHandle5);
			nativeList.Dispose(jobHandle5);
			pathTargetInfo.Dispose(jobHandle6);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle5);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle5);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle5);
			base.Dependency = jobHandle6;
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
	public WaypointConnectionSystem()
	{
	}
}
