using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Zones;

public static class CellBlockJobs
{
	[BurstCompile]
	public struct BlockCellsJob : IJobParallelForDefer
	{
		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_BlockEntity;

			public Block m_BlockData;

			public ValidArea m_ValidAreaData;

			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public Quad2 m_IgnoreQuad;

			public Circle2 m_IgnoreCircle;

			public bool2 m_HasIgnore;

			public DynamicBuffer<Cell> m_Cells;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public ComponentLookup<RoadComposition> m_PrefabRoadCompositionData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || !m_EdgeGeometryData.HasComponent(edgeEntity))
				{
					return;
				}
				m_HasIgnore = false;
				if (m_OwnerData.HasComponent(edgeEntity))
				{
					Owner owner = m_OwnerData[edgeEntity];
					if (m_TransformData.HasComponent(owner.m_Owner))
					{
						PrefabRef prefabRef = m_PrefabRefData[owner.m_Owner];
						if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
						{
							Game.Objects.Transform transform = m_TransformData[owner.m_Owner];
							ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
							if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
							{
								m_IgnoreCircle = new Circle2(math.max(objectGeometryData.m_Size - 0.16f, 0f).x * 0.5f, transform.m_Position.xz);
								m_HasIgnore.y = true;
							}
							else
							{
								Bounds3 bounds2 = MathUtils.Expand(objectGeometryData.m_Bounds, -0.08f);
								float3 trueValue = MathUtils.Center(bounds2);
								bool3 test = bounds2.min > bounds2.max;
								bounds2.min = math.select(bounds2.min, trueValue, test);
								bounds2.max = math.select(bounds2.max, trueValue, test);
								m_IgnoreQuad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, bounds2).xz;
								m_HasIgnore.x = true;
							}
						}
					}
				}
				Composition composition = m_CompositionData[edgeEntity];
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[edgeEntity];
				StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[edgeEntity];
				EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[edgeEntity];
				if (MathUtils.Intersect(m_Bounds, edgeGeometry.m_Bounds.xz))
				{
					NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
					RoadComposition prefabRoadData = default(RoadComposition);
					if (m_PrefabRoadCompositionData.HasComponent(composition.m_Edge))
					{
						prefabRoadData = m_PrefabRoadCompositionData[composition.m_Edge];
					}
					CheckSegment(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, prefabCompositionData, prefabRoadData, new bool2(x: true, y: true));
					CheckSegment(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, prefabCompositionData, prefabRoadData, new bool2(x: true, y: true));
				}
				if (MathUtils.Intersect(m_Bounds, startNodeGeometry.m_Geometry.m_Bounds.xz))
				{
					NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
					RoadComposition prefabRoadData2 = default(RoadComposition);
					if (m_PrefabRoadCompositionData.HasComponent(composition.m_StartNode))
					{
						prefabRoadData2 = m_PrefabRoadCompositionData[composition.m_StartNode];
					}
					if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
					{
						CheckSegment(startNodeGeometry.m_Geometry.m_Left.m_Left, startNodeGeometry.m_Geometry.m_Left.m_Right, prefabCompositionData2, prefabRoadData2, new bool2(x: true, y: true));
						Bezier4x3 bezier4x = MathUtils.Lerp(startNodeGeometry.m_Geometry.m_Right.m_Left, startNodeGeometry.m_Geometry.m_Right.m_Right, 0.5f);
						bezier4x.d = startNodeGeometry.m_Geometry.m_Middle.d;
						CheckSegment(startNodeGeometry.m_Geometry.m_Right.m_Left, bezier4x, prefabCompositionData2, prefabRoadData2, new bool2(x: true, y: false));
						CheckSegment(bezier4x, startNodeGeometry.m_Geometry.m_Right.m_Right, prefabCompositionData2, prefabRoadData2, new bool2(x: false, y: true));
					}
					else
					{
						CheckSegment(startNodeGeometry.m_Geometry.m_Left.m_Left, startNodeGeometry.m_Geometry.m_Middle, prefabCompositionData2, prefabRoadData2, new bool2(x: true, y: false));
						CheckSegment(startNodeGeometry.m_Geometry.m_Middle, startNodeGeometry.m_Geometry.m_Right.m_Right, prefabCompositionData2, prefabRoadData2, new bool2(x: false, y: true));
					}
				}
				if (MathUtils.Intersect(m_Bounds, endNodeGeometry.m_Geometry.m_Bounds.xz))
				{
					NetCompositionData prefabCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
					RoadComposition prefabRoadData3 = default(RoadComposition);
					if (m_PrefabRoadCompositionData.HasComponent(composition.m_EndNode))
					{
						prefabRoadData3 = m_PrefabRoadCompositionData[composition.m_EndNode];
					}
					if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
					{
						CheckSegment(endNodeGeometry.m_Geometry.m_Left.m_Left, endNodeGeometry.m_Geometry.m_Left.m_Right, prefabCompositionData3, prefabRoadData3, new bool2(x: true, y: true));
						Bezier4x3 bezier4x2 = MathUtils.Lerp(endNodeGeometry.m_Geometry.m_Right.m_Left, endNodeGeometry.m_Geometry.m_Right.m_Right, 0.5f);
						bezier4x2.d = endNodeGeometry.m_Geometry.m_Middle.d;
						CheckSegment(endNodeGeometry.m_Geometry.m_Right.m_Left, bezier4x2, prefabCompositionData3, prefabRoadData3, new bool2(x: true, y: false));
						CheckSegment(bezier4x2, endNodeGeometry.m_Geometry.m_Right.m_Right, prefabCompositionData3, prefabRoadData3, new bool2(x: false, y: true));
					}
					else
					{
						CheckSegment(endNodeGeometry.m_Geometry.m_Left.m_Left, endNodeGeometry.m_Geometry.m_Middle, prefabCompositionData3, prefabRoadData3, new bool2(x: true, y: false));
						CheckSegment(endNodeGeometry.m_Geometry.m_Middle, endNodeGeometry.m_Geometry.m_Right.m_Right, prefabCompositionData3, prefabRoadData3, new bool2(x: false, y: true));
					}
				}
			}

			private void CheckSegment(Bezier4x3 left, Bezier4x3 right, NetCompositionData prefabCompositionData, RoadComposition prefabRoadData, bool2 isEdge)
			{
				if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 || (prefabCompositionData.m_State & CompositionState.BlockZone) == 0)
				{
					return;
				}
				bool flag = (prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0;
				flag |= (prefabCompositionData.m_State & CompositionState.ExclusiveGround) == 0;
				if (!MathUtils.Intersect((MathUtils.Bounds(left) | MathUtils.Bounds(right)).xz, m_Bounds))
				{
					return;
				}
				isEdge &= ((prefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0) & ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0);
				isEdge &= new bool2((prefabCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0, (prefabCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0);
				Quad3 quad = default(Quad3);
				quad.a = left.a;
				quad.b = right.a;
				Bounds3 bounds = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData.m_HeightRange);
				for (int i = 1; i <= 8; i++)
				{
					float t = (float)i / 8f;
					quad.d = MathUtils.Position(left, t);
					quad.c = MathUtils.Position(right, t);
					Bounds3 bounds2 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData.m_HeightRange);
					Bounds3 bounds3 = bounds | bounds2;
					if (MathUtils.Intersect(bounds3.xz, m_Bounds) && MathUtils.Intersect(m_Quad, quad.xz))
					{
						CellFlags cellFlags = CellFlags.Blocked;
						if (isEdge.x)
						{
							Block source = new Block
							{
								m_Direction = math.normalizesafe(MathUtils.Right(quad.d.xz - quad.a.xz))
							};
							cellFlags |= ZoneUtils.GetRoadDirection(m_BlockData, source);
						}
						if (isEdge.y)
						{
							Block source2 = new Block
							{
								m_Direction = math.normalizesafe(MathUtils.Left(quad.c.xz - quad.b.xz))
							};
							cellFlags |= ZoneUtils.GetRoadDirection(m_BlockData, source2);
						}
						CheckOverlapX(m_Bounds, bounds3, m_Quad, quad, m_ValidAreaData.m_Area, cellFlags, flag);
					}
					quad.a = quad.d;
					quad.b = quad.c;
					bounds = bounds2;
				}
			}

			private static Bounds3 SetHeightRange(Bounds3 bounds, Bounds1 heightRange)
			{
				bounds.min.y += heightRange.min;
				bounds.max.y += heightRange.max;
				return bounds;
			}

			private void CheckOverlapX(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Quad3 quad2, int4 xxzz1, CellFlags flags, bool isElevated)
			{
				if (xxzz1.y - xxzz1.x >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.y = xxzz1.x + xxzz1.y >> 1;
					xxzz3.x = xxzz2.y;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz2.y - xxzz1.x) / (float)(xxzz1.y - xxzz1.x);
					quad3.b = math.lerp(quad1.a, quad1.b, t);
					quad3.c = math.lerp(quad1.d, quad1.c, t);
					quad4.a = quad3.b;
					quad4.d = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapZ(bounds3, bounds2, quad3, quad2, xxzz2, flags, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapZ(bounds4, bounds2, quad4, quad2, xxzz3, flags, isElevated);
					}
				}
				else
				{
					CheckOverlapZ(bounds1, bounds2, quad1, quad2, xxzz1, flags, isElevated);
				}
			}

			private void CheckOverlapZ(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Quad3 quad2, int4 xxzz1, CellFlags flags, bool isElevated)
			{
				if (xxzz1.w - xxzz1.z >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.w = xxzz1.z + xxzz1.w >> 1;
					xxzz3.z = xxzz2.w;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz2.w - xxzz1.z) / (float)(xxzz1.w - xxzz1.z);
					quad3.d = math.lerp(quad1.a, quad1.d, t);
					quad3.c = math.lerp(quad1.b, quad1.c, t);
					quad4.a = quad3.d;
					quad4.b = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapX(bounds3, bounds2, quad3, quad2, xxzz2, flags, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapX(bounds4, bounds2, quad4, quad2, xxzz3, flags, isElevated);
					}
					return;
				}
				if (xxzz1.y - xxzz1.x >= 2)
				{
					CheckOverlapX(bounds1, bounds2, quad1, quad2, xxzz1, flags, isElevated);
					return;
				}
				int index = xxzz1.z * m_BlockData.m_Size.x + xxzz1.x;
				Cell value = m_Cells[index];
				if ((value.m_State & flags) == flags)
				{
					return;
				}
				quad1 = MathUtils.Expand(quad1, -0.0625f);
				if (MathUtils.Intersect(quad1, quad2.xz) && (!math.any(m_HasIgnore) || ((!m_HasIgnore.x || !MathUtils.Intersect(quad1, m_IgnoreQuad)) && (!m_HasIgnore.y || !MathUtils.Intersect(quad1, m_IgnoreCircle)))))
				{
					if (isElevated)
					{
						value.m_Height = (short)math.clamp(Mathf.FloorToInt(bounds2.min.y), -32768, math.min(value.m_Height, 32767));
					}
					else
					{
						value.m_State |= flags;
					}
					m_Cells[index] = value;
				}
			}
		}

		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Entity m_BlockEntity;

			public Block m_BlockData;

			public ValidArea m_ValidAreaData;

			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public DynamicBuffer<Cell> m_Cells;

			public ComponentLookup<Native> m_NativeData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[areaItem.m_Area];
				AreaGeometryData areaGeometryData = m_PrefabAreaGeometryData[prefabRef.m_Prefab];
				if ((areaGeometryData.m_Flags & (Game.Areas.GeometryFlags.PhysicalGeometry | Game.Areas.GeometryFlags.ProtectedArea)) != 0 && ((areaGeometryData.m_Flags & Game.Areas.GeometryFlags.ProtectedArea) == 0 || m_NativeData.HasComponent(areaItem.m_Area)))
				{
					DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[areaItem.m_Area];
					DynamicBuffer<Triangle> dynamicBuffer = m_AreaTriangles[areaItem.m_Area];
					if (dynamicBuffer.Length > areaItem.m_Triangle)
					{
						Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[areaItem.m_Triangle]);
						CheckOverlapX(m_Bounds, bounds.m_Bounds.xz, m_Quad, triangle.xz, m_ValidAreaData.m_Area);
					}
				}
			}

			private void CheckOverlapX(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Triangle2 triangle2, int4 xxzz1)
			{
				if (xxzz1.y - xxzz1.x >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.y = xxzz1.x + xxzz1.y >> 1;
					xxzz3.x = xxzz2.y;
					Quad2 quad2 = quad1;
					Quad2 quad3 = quad1;
					float t = (float)(xxzz2.y - xxzz1.x) / (float)(xxzz1.y - xxzz1.x);
					quad2.b = math.lerp(quad1.a, quad1.b, t);
					quad2.c = math.lerp(quad1.d, quad1.c, t);
					quad3.a = quad2.b;
					quad3.d = quad2.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad2);
					Bounds2 bounds4 = MathUtils.Bounds(quad3);
					if (MathUtils.Intersect(bounds3, bounds2))
					{
						CheckOverlapZ(bounds3, bounds2, quad2, triangle2, xxzz2);
					}
					if (MathUtils.Intersect(bounds4, bounds2))
					{
						CheckOverlapZ(bounds4, bounds2, quad3, triangle2, xxzz3);
					}
				}
				else
				{
					CheckOverlapZ(bounds1, bounds2, quad1, triangle2, xxzz1);
				}
			}

			private void CheckOverlapZ(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Triangle2 triangle2, int4 xxzz1)
			{
				if (xxzz1.w - xxzz1.z >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.w = xxzz1.z + xxzz1.w >> 1;
					xxzz3.z = xxzz2.w;
					Quad2 quad2 = quad1;
					Quad2 quad3 = quad1;
					float t = (float)(xxzz2.w - xxzz1.z) / (float)(xxzz1.w - xxzz1.z);
					quad2.d = math.lerp(quad1.a, quad1.d, t);
					quad2.c = math.lerp(quad1.b, quad1.c, t);
					quad3.a = quad2.d;
					quad3.b = quad2.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad2);
					Bounds2 bounds4 = MathUtils.Bounds(quad3);
					if (MathUtils.Intersect(bounds3, bounds2))
					{
						CheckOverlapX(bounds3, bounds2, quad2, triangle2, xxzz2);
					}
					if (MathUtils.Intersect(bounds4, bounds2))
					{
						CheckOverlapX(bounds4, bounds2, quad3, triangle2, xxzz3);
					}
					return;
				}
				if (xxzz1.y - xxzz1.x >= 2)
				{
					CheckOverlapX(bounds1, bounds2, quad1, triangle2, xxzz1);
					return;
				}
				int index = xxzz1.z * m_BlockData.m_Size.x + xxzz1.x;
				Cell value = m_Cells[index];
				if ((value.m_State & CellFlags.Blocked) == 0)
				{
					quad1 = MathUtils.Expand(quad1, -0.02f);
					if (MathUtils.Intersect(quad1, triangle2))
					{
						value.m_State |= CellFlags.Blocked;
						m_Cells[index] = value;
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<CellCheckHelpers.SortedEntity> m_Blocks;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<RoadComposition> m_PrefabRoadCompositionData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Cell> m_Cells;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		public void Execute(int index)
		{
			Entity entity = m_Blocks[index].m_Entity;
			Block block = m_BlockData[entity];
			DynamicBuffer<Cell> cells = m_Cells[entity];
			ValidArea validAreaData = new ValidArea
			{
				m_Area = new int4(0, block.m_Size.x, 0, block.m_Size.y)
			};
			Bounds2 bounds = ZoneUtils.CalculateBounds(block);
			Quad2 quad = ZoneUtils.CalculateCorners(block);
			ClearBlockStatus(block, cells);
			NetIterator iterator = new NetIterator
			{
				m_BlockEntity = entity,
				m_BlockData = block,
				m_Bounds = bounds,
				m_Quad = quad,
				m_ValidAreaData = validAreaData,
				m_Cells = cells,
				m_OwnerData = m_OwnerData,
				m_TransformData = m_TransformData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_StartNodeGeometryData = m_StartNodeGeometryData,
				m_EndNodeGeometryData = m_EndNodeGeometryData,
				m_CompositionData = m_CompositionData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabCompositionData = m_PrefabCompositionData,
				m_PrefabRoadCompositionData = m_PrefabRoadCompositionData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData
			};
			m_NetSearchTree.Iterate(ref iterator);
			AreaIterator iterator2 = new AreaIterator
			{
				m_BlockEntity = entity,
				m_BlockData = block,
				m_Bounds = bounds,
				m_Quad = quad,
				m_ValidAreaData = validAreaData,
				m_Cells = cells,
				m_NativeData = m_NativeData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabAreaGeometryData = m_PrefabAreaGeometryData,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles
			};
			m_AreaSearchTree.Iterate(ref iterator2);
			CleanBlockedCells(block, ref validAreaData, cells);
			m_ValidAreaData[entity] = validAreaData;
		}

		private static void ClearBlockStatus(Block blockData, DynamicBuffer<Cell> cells)
		{
			for (int i = 0; i < blockData.m_Size.x; i++)
			{
				Cell value = cells[i];
				if ((value.m_State & (CellFlags.Blocked | CellFlags.Shared | CellFlags.Roadside | CellFlags.Occupied | CellFlags.Updating | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) != (CellFlags.Roadside | CellFlags.Updating))
				{
					value.m_State = (value.m_State & ~(CellFlags.Blocked | CellFlags.Shared | CellFlags.Roadside | CellFlags.Occupied | CellFlags.Updating | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) | (CellFlags.Roadside | CellFlags.Updating);
					value.m_Height = short.MaxValue;
					cells[i] = value;
				}
			}
			for (int j = 1; j < blockData.m_Size.y; j++)
			{
				for (int k = 0; k < blockData.m_Size.x; k++)
				{
					int index = j * blockData.m_Size.x + k;
					Cell value2 = cells[index];
					if ((value2.m_State & (CellFlags.Blocked | CellFlags.Shared | CellFlags.Roadside | CellFlags.Occupied | CellFlags.Updating | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) != CellFlags.Updating)
					{
						value2.m_State = (value2.m_State & ~(CellFlags.Blocked | CellFlags.Shared | CellFlags.Roadside | CellFlags.Occupied | CellFlags.Updating | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) | CellFlags.Updating;
						value2.m_Height = short.MaxValue;
						cells[index] = value2;
					}
				}
			}
		}

		private static void CleanBlockedCells(Block blockData, ref ValidArea validAreaData, DynamicBuffer<Cell> cells)
		{
			ValidArea validArea = new ValidArea
			{
				m_Area = 
				{
					xz = blockData.m_Size
				}
			};
			for (int i = validAreaData.m_Area.x; i < validAreaData.m_Area.y; i++)
			{
				Cell value = cells[i];
				Cell cell = cells[blockData.m_Size.x + i];
				if (((value.m_State & CellFlags.Blocked) == 0) & ((cell.m_State & CellFlags.Blocked) != 0))
				{
					value.m_State |= CellFlags.Blocked;
					cells[i] = value;
				}
				int num = 0;
				for (int j = validAreaData.m_Area.z + 1; j < validAreaData.m_Area.w; j++)
				{
					int index = j * blockData.m_Size.x + i;
					Cell cell2 = cells[index];
					if (((cell2.m_State & CellFlags.Blocked) == 0) & ((value.m_State & CellFlags.Blocked) != 0))
					{
						cell2.m_State |= CellFlags.Blocked;
						cells[index] = cell2;
					}
					if ((cell2.m_State & CellFlags.Blocked) == 0)
					{
						num = j + 1;
					}
					value = cell2;
				}
				if (num > validAreaData.m_Area.z)
				{
					validArea.m_Area.xz = math.min(validArea.m_Area.xz, new int2(i, validAreaData.m_Area.z));
					validArea.m_Area.yw = math.max(validArea.m_Area.yw, new int2(i + 1, num));
				}
			}
			validAreaData = validArea;
			for (int k = validAreaData.m_Area.z; k < validAreaData.m_Area.w; k++)
			{
				for (int l = validAreaData.m_Area.x; l < validAreaData.m_Area.y; l++)
				{
					int num2 = k * blockData.m_Size.x + l;
					Cell value2 = cells[num2];
					if ((value2.m_State & (CellFlags.Blocked | CellFlags.RoadLeft)) == 0 && l > 0 && (cells[num2 - 1].m_State & (CellFlags.Blocked | CellFlags.RoadLeft)) == (CellFlags.Blocked | CellFlags.RoadLeft))
					{
						value2.m_State |= CellFlags.RoadLeft;
						cells[num2] = value2;
					}
					if ((value2.m_State & (CellFlags.Blocked | CellFlags.RoadRight)) == 0 && l < blockData.m_Size.x - 1 && (cells[num2 + 1].m_State & (CellFlags.Blocked | CellFlags.RoadRight)) == (CellFlags.Blocked | CellFlags.RoadRight))
					{
						value2.m_State |= CellFlags.RoadRight;
						cells[num2] = value2;
					}
				}
			}
		}
	}
}
