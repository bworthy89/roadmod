using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class AreaToolSystem : ToolBaseSystem
{
	public enum Mode
	{
		Edit,
		Generate
	}

	public enum State
	{
		Default,
		Create,
		Modify,
		Remove
	}

	public enum Tooltip
	{
		None,
		CreateArea,
		ModifyNode,
		ModifyEdge,
		CreateAreaOrModifyNode,
		CreateAreaOrModifyEdge,
		AddNode,
		InsertNode,
		MoveNode,
		MergeNodes,
		CompleteArea,
		DeleteArea,
		RemoveNode,
		GenerateAreas
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		private struct ParentObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Line3.Segment m_Line;

			public float m_BoundsOffset;

			public float m_MaxDistance;

			public Entity m_Parent;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float2 t;
				return MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, m_BoundsOffset), m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, m_BoundsOffset), m_Line, out var t) || !m_TransformData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				Transform transform = m_TransformData[entity];
				if (!m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
				if (m_BuildingData.HasComponent(prefabRef.m_Prefab))
				{
					float2 @float = m_BuildingData[prefabRef.m_Prefab].m_LotSize;
					objectGeometryData.m_Bounds.min.xz = @float * -4f - m_MaxDistance;
					objectGeometryData.m_Bounds.max.xz = @float * 4f + m_MaxDistance;
				}
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					float num = math.max(math.cmax(objectGeometryData.m_Bounds.max.xz), 0f - math.cmin(objectGeometryData.m_Bounds.max.xz));
					if (MathUtils.Distance(m_Line.xz, transform.m_Position.xz, out var _) < num + m_MaxDistance)
					{
						m_Parent = entity;
					}
				}
				else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds).xz, m_Line.xz, out t))
				{
					m_Parent = entity;
				}
			}
		}

		private struct AreaIterator2 : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public bool m_EditorMode;

			public Game.Areas.AreaType m_AreaType;

			public Bounds3 m_Bounds;

			public float m_MaxDistance1;

			public float m_MaxDistance2;

			public ControlPoint m_ControlPoint1;

			public ControlPoint m_ControlPoint2;

			public NativeParallelHashSet<Entity> m_IgnoreAreas;

			public NativeList<ControlPoint> m_ControlPoints;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

			public ComponentLookup<Game.Areas.Lot> m_LotData;

			public ComponentLookup<Owner> m_OwnerData;

			public BufferLookup<Game.Areas.Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || (m_IgnoreAreas.IsCreated && m_IgnoreAreas.Contains(areaItem.m_Area)) || (m_OwnerData.TryGetComponent(areaItem.m_Area, out var componentData) && (m_Nodes.HasBuffer(componentData.m_Owner) || (m_EditorMode && m_InstalledUpgrades.TryGetBuffer(componentData.m_Owner, out var bufferData) && bufferData.Length != 0))))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[areaItem.m_Area];
				AreaGeometryData areaGeometryData = m_PrefabAreaData[prefabRef.m_Prefab];
				if (areaGeometryData.m_Type != m_AreaType || (!m_EditorMode && (areaGeometryData.m_Flags & Game.Areas.GeometryFlags.HiddenIngame) != 0))
				{
					return;
				}
				DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
				Triangle triangle = m_Triangles[areaItem.m_Area][areaItem.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				int3 @int = math.abs(triangle.m_Indices - triangle.m_Indices.yzx);
				bool3 x = (@int == 1) | (@int == nodes.Length - 1);
				if (math.any(x))
				{
					bool lockFirstEdge = !m_EditorMode && m_LotData.HasComponent(areaItem.m_Area);
					if (x.x)
					{
						CheckLine(triangle2.ab, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.xy, lockFirstEdge);
					}
					if (x.y)
					{
						CheckLine(triangle2.bc, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.yz, lockFirstEdge);
					}
					if (x.z)
					{
						CheckLine(triangle2.ca, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.zx, lockFirstEdge);
					}
				}
			}

			public void CheckLine(Line3.Segment line, float snapDistance, Entity area, int2 nodeIndex, bool lockFirstEdge)
			{
				if (lockFirstEdge && math.cmin(nodeIndex) == 0 && math.cmax(nodeIndex) == 1)
				{
					return;
				}
				float t;
				float num = MathUtils.Distance(line.xz, m_ControlPoint1.m_Position.xz, out t);
				float t2;
				float num2 = MathUtils.Distance(line.xz, m_ControlPoint2.m_HitPosition.xz, out t2);
				if (!(num < m_MaxDistance1) || !(num2 < m_MaxDistance2))
				{
					return;
				}
				float num3 = math.distance(line.a.xz, m_ControlPoint2.m_HitPosition.xz);
				float num4 = math.distance(line.b.xz, m_ControlPoint2.m_HitPosition.xz);
				ControlPoint value = m_ControlPoint1;
				value.m_OriginalEntity = area;
				if (num3 <= snapDistance && num3 <= num4 && (!lockFirstEdge || nodeIndex.x >= 2))
				{
					value.m_ElementIndex = new int2(nodeIndex.x, -1);
				}
				else if (num4 <= snapDistance && (!lockFirstEdge || nodeIndex.y >= 2))
				{
					value.m_ElementIndex = new int2(nodeIndex.y, -1);
				}
				else
				{
					value.m_ElementIndex = new int2(-1, math.select(math.cmax(nodeIndex), math.cmin(nodeIndex), math.abs(nodeIndex.y - nodeIndex.x) == 1));
				}
				for (int i = 0; i < m_ControlPoints.Length; i++)
				{
					if (m_ControlPoints[i].m_OriginalEntity == area)
					{
						return;
					}
				}
				m_ControlPoints.Add(in value);
			}
		}

		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public bool m_EditorMode;

			public bool m_IgnoreStartPositions;

			public Snap m_Snap;

			public Game.Areas.AreaType m_AreaType;

			public Bounds3 m_Bounds;

			public float m_MaxDistance;

			public NativeParallelHashSet<Entity> m_IgnoreAreas;

			public Entity m_PreferArea;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public NativeList<ControlPoint> m_MoveStartPositions;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

			public ComponentLookup<Game.Areas.Lot> m_LotData;

			public ComponentLookup<Owner> m_OwnerData;

			public BufferLookup<Game.Areas.Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || (m_IgnoreAreas.IsCreated && m_IgnoreAreas.Contains(areaItem.m_Area)))
				{
					return;
				}
				Entity area = areaItem.m_Area;
				if (areaItem.m_Area != m_PreferArea)
				{
					if ((m_Snap & Snap.ExistingGeometry) == 0)
					{
						bool flag = false;
						if (m_IgnoreStartPositions)
						{
							for (int i = 0; i < m_MoveStartPositions.Length; i++)
							{
								flag |= m_MoveStartPositions[i].m_OriginalEntity == areaItem.m_Area;
							}
						}
						if (!flag)
						{
							return;
						}
					}
					if (m_OwnerData.TryGetComponent(areaItem.m_Area, out var componentData))
					{
						if (m_Nodes.HasBuffer(componentData.m_Owner))
						{
							return;
						}
						if (m_EditorMode && m_InstalledUpgrades.TryGetBuffer(componentData.m_Owner, out var bufferData) && bufferData.Length != 0)
						{
							area = Entity.Null;
						}
					}
				}
				PrefabRef prefabRef = m_PrefabRefData[areaItem.m_Area];
				AreaGeometryData areaGeometryData = m_PrefabAreaData[prefabRef.m_Prefab];
				if (areaGeometryData.m_Type != m_AreaType || (!m_EditorMode && (areaGeometryData.m_Flags & Game.Areas.GeometryFlags.HiddenIngame) != 0))
				{
					return;
				}
				DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
				Triangle triangle = m_Triangles[areaItem.m_Area][areaItem.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				int3 @int = math.abs(triangle.m_Indices - triangle.m_Indices.yzx);
				bool3 @bool = @int == nodes.Length - 1;
				bool3 x = (@int == 1) | @bool;
				if (!math.any(x))
				{
					return;
				}
				if (m_IgnoreStartPositions)
				{
					bool3 test = triangle.m_Indices.yzx < triangle.m_Indices != @bool;
					int3 int2 = math.select(triangle.m_Indices, triangle.m_Indices.yzx, test);
					int3 int3 = math.select(triangle.m_Indices.yzx, triangle.m_Indices, test);
					for (int j = 0; j < m_MoveStartPositions.Length; j++)
					{
						ControlPoint controlPoint = m_MoveStartPositions[j];
						if (!(controlPoint.m_OriginalEntity != areaItem.m_Area))
						{
							x &= controlPoint.m_ElementIndex.x != int2;
							x &= controlPoint.m_ElementIndex.x != int3;
							x &= controlPoint.m_ElementIndex.y != int2;
						}
					}
				}
				bool lockFirstEdge = !m_EditorMode && m_LotData.HasComponent(areaItem.m_Area);
				float snapDistance = math.select(areaGeometryData.m_SnapDistance, areaGeometryData.m_SnapDistance * 0.5f, (m_Snap & Snap.ExistingGeometry) == 0);
				if (x.x)
				{
					CheckLine(triangle2.ab, snapDistance, area, triangle.m_Indices.xy, lockFirstEdge);
				}
				if (x.y)
				{
					CheckLine(triangle2.bc, snapDistance, area, triangle.m_Indices.yz, lockFirstEdge);
				}
				if (x.z)
				{
					CheckLine(triangle2.ca, snapDistance, area, triangle.m_Indices.zx, lockFirstEdge);
				}
			}

			public void CheckLine(Line3.Segment line, float snapDistance, Entity area, int2 nodeIndex, bool lockFirstEdge)
			{
				if ((!lockFirstEdge || math.cmin(nodeIndex) != 0 || math.cmax(nodeIndex) != 1) && MathUtils.Distance(line.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_MaxDistance)
				{
					float heightWeight = math.select(0f, 1f, m_AreaType == Game.Areas.AreaType.Space);
					float level = math.select(2f, 3f, area == m_PreferArea);
					float num = math.distance(line.a.xz, m_ControlPoint.m_HitPosition.xz);
					float num2 = math.distance(line.b.xz, m_ControlPoint.m_HitPosition.xz);
					ControlPoint controlPoint = m_ControlPoint;
					controlPoint.m_OriginalEntity = area;
					controlPoint.m_Direction = line.b.xz - line.a.xz;
					MathUtils.TryNormalize(ref controlPoint.m_Direction);
					if (num <= snapDistance && num <= num2 && (!lockFirstEdge || nodeIndex.x >= 2))
					{
						controlPoint.m_Position = line.a;
						controlPoint.m_ElementIndex = new int2(nodeIndex.x, -1);
						controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0, heightWeight));
					}
					else if (num2 <= snapDistance && (!lockFirstEdge || nodeIndex.y >= 2))
					{
						controlPoint.m_Position = line.b;
						controlPoint.m_ElementIndex = new int2(nodeIndex.y, -1);
						controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0, heightWeight));
					}
					else
					{
						controlPoint.m_Position = MathUtils.Position(line, t);
						controlPoint.m_ElementIndex = new int2(-1, math.select(math.cmax(nodeIndex), math.cmin(nodeIndex), math.abs(nodeIndex.y - nodeIndex.x) == 1));
						controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0, heightWeight));
					}
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Snap m_Snap;

			public Bounds3 m_Bounds;

			public float m_MaxDistance;

			public Game.Areas.AreaType m_AreaType;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndGeometryData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
				{
					return;
				}
				Composition composition = default(Composition);
				if (m_CompositionData.HasComponent(entity))
				{
					composition = m_CompositionData[entity];
				}
				if ((m_Snap & Snap.NetSide) != Snap.None)
				{
					if (m_EdgeGeometryData.HasComponent(entity) && CheckComposition(composition.m_Edge))
					{
						EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
						SnapEdgeCurve(edgeGeometry.m_Start.m_Left);
						SnapEdgeCurve(edgeGeometry.m_Start.m_Right);
						SnapEdgeCurve(edgeGeometry.m_End.m_Left);
						SnapEdgeCurve(edgeGeometry.m_End.m_Right);
					}
					if (m_StartGeometryData.HasComponent(entity) && CheckComposition(composition.m_StartNode))
					{
						StartNodeGeometry startNodeGeometry = m_StartGeometryData[entity];
						if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
						{
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Left);
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Right);
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Left);
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Right);
						}
						else
						{
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Left);
							SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Right);
						}
					}
					if (m_EndGeometryData.HasComponent(entity) && CheckComposition(composition.m_EndNode))
					{
						EndNodeGeometry endNodeGeometry = m_EndGeometryData[entity];
						if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
						{
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Left);
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Right);
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Left);
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Right);
						}
						else
						{
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Left);
							SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Right);
						}
					}
				}
				if ((m_Snap & Snap.NetMiddle) != Snap.None && m_CurveData.HasComponent(entity) && CheckComposition(composition.m_Edge))
				{
					SnapEdgeCurve(m_CurveData[entity].m_Bezier);
				}
			}

			private bool CheckComposition(Entity composition)
			{
				if (m_PrefabCompositionData.TryGetComponent(composition, out var componentData) && (componentData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					return false;
				}
				return true;
			}

			private void SnapEdgeCurve(Bezier4x3 curve)
			{
				if (MathUtils.Intersect(m_Bounds, MathUtils.Bounds(curve)))
				{
					float heightWeight = math.select(0f, 1f, m_AreaType == Game.Areas.AreaType.Space);
					if (MathUtils.Distance(curve.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_MaxDistance)
					{
						ControlPoint controlPoint = m_ControlPoint;
						controlPoint.m_OriginalEntity = Entity.Null;
						controlPoint.m_Position = MathUtils.Position(curve, t);
						controlPoint.m_Direction = MathUtils.Tangent(curve, t).xz;
						MathUtils.TryNormalize(ref controlPoint.m_Direction);
						controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, curve, (SnapLineFlags)0, heightWeight));
					}
				}
			}

			private void SnapNodeCurve(Bezier4x3 curve)
			{
				float3 value = MathUtils.StartTangent(curve);
				value = MathUtils.Normalize(value, value.xz);
				value.y = math.clamp(value.y, -1f, 1f);
				Line3.Segment line = new Line3.Segment(curve.a, curve.a + value * math.dot(curve.d - curve.a, value));
				if (MathUtils.Intersect(m_Bounds, MathUtils.Bounds(line)))
				{
					float heightWeight = math.select(0f, 1f, m_AreaType == Game.Areas.AreaType.Space);
					if (MathUtils.Distance(line.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_MaxDistance)
					{
						ControlPoint controlPoint = m_ControlPoint;
						controlPoint.m_OriginalEntity = Entity.Null;
						controlPoint.m_Direction = value.xz;
						controlPoint.m_Position = MathUtils.Position(line, t);
						controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0, heightWeight));
					}
				}
			}
		}

		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float m_MaxDistance;

			public Game.Areas.AreaType m_AreaType;

			public Snap m_Snap;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

			public ComponentLookup<AssetStampData> m_AssetStampData;

			public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_TransformData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				Transform transform = m_TransformData[entity];
				if (!m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
				if ((m_Snap & Snap.LotGrid) != Snap.None && (m_BuildingData.HasComponent(prefabRef.m_Prefab) || m_BuildingExtensionData.HasComponent(prefabRef.m_Prefab) || m_AssetStampData.HasComponent(prefabRef.m_Prefab)))
				{
					float2 @float = math.normalizesafe(math.forward(transform.m_Rotation).xz, new float2(0f, 1f));
					float2 float2 = MathUtils.Right(@float);
					float2 x = m_ControlPoint.m_HitPosition.xz - transform.m_Position.xz;
					float heightWeight = math.select(0f, 1f, m_AreaType == Game.Areas.AreaType.Space);
					int2 @int = default(int2);
					@int.x = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.x);
					@int.y = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.z);
					float2 float3 = (float2)@int * 8f;
					float2 offset = math.select(0f, 4f, (@int & 1) != 0);
					float2 float4 = new float2(math.dot(x, float2), math.dot(x, @float));
					float2 float5 = MathUtils.Snap(float4, 8f, offset);
					bool2 @bool = math.abs(float4 - float5) < m_MaxDistance;
					if (!math.any(@bool))
					{
						return;
					}
					float5 = math.select(float4, float5, @bool);
					float2 float6 = transform.m_Position.xz + float2 * float5.x + @float * float5.y;
					if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
					{
						if (math.distance(float6, transform.m_Position.xz) > float3.x * 0.5f + 4f)
						{
							return;
						}
					}
					else if (math.any(math.abs(float5) > float3 * 0.5f + 4f))
					{
						return;
					}
					ControlPoint controlPoint = m_ControlPoint;
					controlPoint.m_OriginalEntity = Entity.Null;
					controlPoint.m_Direction = float2;
					controlPoint.m_Position.xz = float6;
					controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
					Line3 line = new Line3(controlPoint.m_Position, controlPoint.m_Position);
					Line3 line2 = new Line3(controlPoint.m_Position, controlPoint.m_Position);
					line.a.xz -= controlPoint.m_Direction * 8f;
					line.b.xz += controlPoint.m_Direction * 8f;
					line2.a.xz -= MathUtils.Right(controlPoint.m_Direction) * 8f;
					line2.b.xz += MathUtils.Right(controlPoint.m_Direction) * 8f;
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
					if (@bool.y)
					{
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden, heightWeight));
					}
					controlPoint.m_Direction = MathUtils.Right(controlPoint.m_Direction);
					if (@bool.x)
					{
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden, heightWeight));
					}
				}
				else if ((m_Snap & Snap.ObjectSide) != Snap.None && (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0)
				{
					if (m_BuildingData.HasComponent(prefabRef.m_Prefab))
					{
						float2 float7 = m_BuildingData[prefabRef.m_Prefab].m_LotSize;
						objectGeometryData.m_Bounds.min.xz = float7 * -4f;
						objectGeometryData.m_Bounds.max.xz = float7 * 4f;
					}
					Quad3 quad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds);
					CheckLine(quad.ab);
					CheckLine(quad.bc);
					CheckLine(quad.cd);
					CheckLine(quad.da);
				}
			}

			private void CheckLine(Line3 line)
			{
				float heightWeight = math.select(0f, 1f, m_AreaType == Game.Areas.AreaType.Space);
				if (MathUtils.Distance(line.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_MaxDistance)
				{
					ControlPoint controlPoint = m_ControlPoint;
					controlPoint.m_OriginalEntity = Entity.Null;
					controlPoint.m_Direction = math.normalizesafe(MathUtils.Tangent(line.xz));
					controlPoint.m_Position = MathUtils.Position(line, t);
					controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, heightWeight, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
					ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0, heightWeight));
				}
			}
		}

		[ReadOnly]
		public bool m_AllowCreateArea;

		[ReadOnly]
		public bool m_ControlPointsMoved;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<Entity> m_ApplyTempAreas;

		[ReadOnly]
		public NativeList<ControlPoint> m_MoveStartPositions;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_AssetStampData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_CachedNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		public NativeList<ControlPoint> m_ControlPoints;

		public void Execute()
		{
			AreaGeometryData areaGeometryData = m_PrefabAreaData[m_Prefab];
			int index = math.select(0, m_ControlPoints.Length - 1, m_State == State.Create);
			ControlPoint controlPoint = m_ControlPoints[index];
			controlPoint.m_Position = controlPoint.m_HitPosition;
			ControlPoint bestSnapPosition = controlPoint;
			switch (m_State)
			{
			case State.Default:
				if (FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, controlPoint.m_OriginalEntity, ignoreStartPositions: false, 0))
				{
					FixControlPointPosition(ref bestSnapPosition);
				}
				else if (!m_AllowCreateArea)
				{
					bestSnapPosition = default(ControlPoint);
				}
				else if (m_EditorMode)
				{
					FindParent(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance);
				}
				else
				{
					bestSnapPosition.m_ElementIndex = -1;
				}
				break;
			case State.Create:
				FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, Entity.Null, ignoreStartPositions: false, m_ControlPoints.Length - 3);
				if (m_ControlPoints.Length >= 4)
				{
					ControlPoint controlPoint3 = m_ControlPoints[0];
					if (math.distance(controlPoint3.m_Position, bestSnapPosition.m_Position) < areaGeometryData.m_SnapDistance * 0.5f)
					{
						bestSnapPosition.m_Position = controlPoint3.m_Position;
					}
				}
				if (m_EditorMode)
				{
					FindParent(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance);
					if (m_ControlPoints.Length >= 2 && m_Nodes.HasBuffer(m_ControlPoints[0].m_OriginalEntity))
					{
						ControlPoint value = m_ControlPoints[0];
						value.m_ElementIndex = new int2(FindParentMesh(m_ControlPoints[0]), -1);
						m_ControlPoints[0] = value;
					}
				}
				else
				{
					bestSnapPosition.m_ElementIndex = -1;
				}
				break;
			case State.Modify:
				if (m_ControlPointsMoved)
				{
					FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, Entity.Null, ignoreStartPositions: true, 0);
					float num = areaGeometryData.m_SnapDistance * 0.5f;
					for (int i = 0; i < m_MoveStartPositions.Length; i++)
					{
						ControlPoint controlPoint2 = m_MoveStartPositions[i];
						if (m_Nodes.HasBuffer(controlPoint2.m_OriginalEntity) && controlPoint2.m_ElementIndex.x >= 0)
						{
							DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_Nodes[controlPoint2.m_OriginalEntity];
							int index2 = math.select(controlPoint2.m_ElementIndex.x - 1, dynamicBuffer.Length - 1, controlPoint2.m_ElementIndex.x == 0);
							int index3 = math.select(controlPoint2.m_ElementIndex.x + 1, 0, controlPoint2.m_ElementIndex.x == dynamicBuffer.Length - 1);
							float3 position = dynamicBuffer[index2].m_Position;
							float3 position2 = dynamicBuffer[index3].m_Position;
							float num2 = math.distance(bestSnapPosition.m_Position, position);
							float num3 = math.distance(bestSnapPosition.m_Position, position2);
							if (num2 < num)
							{
								bestSnapPosition.m_Position = position;
								num = num2;
							}
							if (num3 < num)
							{
								bestSnapPosition.m_Position = position2;
								num = num3;
							}
						}
					}
					if (m_EditorMode)
					{
						bestSnapPosition.m_ElementIndex = new int2(FindParentMesh(controlPoint), -1);
					}
					else
					{
						bestSnapPosition.m_ElementIndex = -1;
					}
				}
				else
				{
					FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, controlPoint.m_OriginalEntity, ignoreStartPositions: false, 0);
					FixControlPointPosition(ref bestSnapPosition);
				}
				break;
			case State.Remove:
				bestSnapPosition = m_MoveStartPositions[0];
				break;
			}
			if (m_State == State.Default)
			{
				m_ControlPoints.Clear();
				m_ControlPoints.Add(in bestSnapPosition);
				if (m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity) && math.any(bestSnapPosition.m_ElementIndex >= 0))
				{
					AddControlPoints(bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance * 0.5f);
				}
			}
			else
			{
				m_ControlPoints[index] = bestSnapPosition;
			}
		}

		private void FindParent(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance)
		{
			if ((m_Snap & Snap.AutoParent) != Snap.None)
			{
				ParentObjectIterator iterator = new ParentObjectIterator
				{
					m_BoundsOffset = snapDistance * 0.125f + 0.4f,
					m_MaxDistance = snapDistance * 0.125f,
					m_TransformData = m_TransformData,
					m_PrefabRefData = m_PrefabRefData,
					m_BuildingData = m_PrefabBuildingData,
					m_ObjectGeometryData = m_ObjectGeometryData
				};
				Entity entity = controlPoint.m_OriginalEntity;
				if (m_EditorMode)
				{
					Owner componentData;
					while (m_OwnerData.TryGetComponent(entity, out componentData) && !m_BuildingData.HasComponent(entity))
					{
						entity = componentData.m_Owner;
					}
					if (m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
					{
						entity = bufferData[0].m_Upgrade;
					}
				}
				int num = math.max(1, m_ControlPoints.Length - 1);
				for (int i = 0; i < num; i++)
				{
					if (i == m_ControlPoints.Length - 1)
					{
						iterator.m_Line.a = bestSnapPosition.m_Position;
					}
					else
					{
						iterator.m_Line.a = m_ControlPoints[i].m_Position;
					}
					if (i + 1 >= m_ControlPoints.Length - 1)
					{
						iterator.m_Line.b = bestSnapPosition.m_Position;
					}
					else
					{
						iterator.m_Line.b = m_ControlPoints[i + 1].m_Position;
					}
					m_ObjectSearchTree.Iterate(ref iterator);
					if (!(iterator.m_Parent != Entity.Null))
					{
						continue;
					}
					Entity entity2 = iterator.m_Parent;
					if (m_EditorMode)
					{
						Owner componentData2;
						while (m_OwnerData.TryGetComponent(entity2, out componentData2) && !m_BuildingData.HasComponent(entity2))
						{
							entity2 = componentData2.m_Owner;
						}
						if (m_InstalledUpgrades.TryGetBuffer(entity2, out var bufferData2) && bufferData2.Length != 0)
						{
							entity2 = bufferData2[0].m_Upgrade;
						}
					}
					if (entity2 != entity)
					{
						bestSnapPosition.m_ElementIndex = -1;
					}
					else
					{
						bestSnapPosition.m_ElementIndex = new int2(FindParentMesh(controlPoint), -1);
					}
					bestSnapPosition.m_OriginalEntity = iterator.m_Parent;
					return;
				}
			}
			bestSnapPosition.m_OriginalEntity = Entity.Null;
			bestSnapPosition.m_ElementIndex = -1;
		}

		private int FindParentMesh(ControlPoint controlPoint)
		{
			if (m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
			{
				return controlPoint.m_ElementIndex.x;
			}
			if (!m_Nodes.TryGetBuffer(controlPoint.m_OriginalEntity, out var bufferData) || bufferData.Length < 2)
			{
				return -1;
			}
			int result = 0;
			float num = float.MaxValue;
			Game.Areas.Node node = bufferData[bufferData.Length - 1];
			LocalNodeCache localNodeCache = default(LocalNodeCache);
			if (m_CachedNodes.TryGetBuffer(controlPoint.m_OriginalEntity, out var bufferData2))
			{
				localNodeCache = bufferData2[bufferData.Length - 1];
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Areas.Node node2 = bufferData[i];
				LocalNodeCache localNodeCache2 = default(LocalNodeCache);
				if (bufferData2.IsCreated)
				{
					localNodeCache2 = bufferData2[i];
				}
				float t;
				float num2 = MathUtils.DistanceSquared(new Line3.Segment(node.m_Position, node2.m_Position), controlPoint.m_HitPosition, out t);
				if (num2 < num)
				{
					num = num2;
					result = ((!bufferData2.IsCreated) ? math.select(0, -1, ((t >= 0.5f) ? node2.m_Elevation : node.m_Elevation) == float.MinValue) : ((t >= 0.5f) ? localNodeCache2.m_ParentMesh : localNodeCache.m_ParentMesh));
				}
				node = node2;
				localNodeCache = localNodeCache2;
			}
			return result;
		}

		private void FixControlPointPosition(ref ControlPoint bestSnapPosition)
		{
			if (!m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity) || bestSnapPosition.m_ElementIndex.x < 0)
			{
				return;
			}
			Entity entity = bestSnapPosition.m_OriginalEntity;
			if (m_ApplyTempAreas.IsCreated)
			{
				for (int i = 0; i < m_ApplyTempAreas.Length; i++)
				{
					Entity entity2 = m_ApplyTempAreas[i];
					if (m_TempData[entity2].m_Original == entity)
					{
						entity = entity2;
						break;
					}
				}
			}
			bestSnapPosition.m_Position = m_Nodes[entity][bestSnapPosition.m_ElementIndex.x].m_Position;
		}

		private void AddControlPoints(ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance)
		{
			AreaIterator2 iterator = new AreaIterator2
			{
				m_EditorMode = m_EditorMode,
				m_AreaType = type,
				m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance),
				m_MaxDistance1 = snapDistance * 0.1f,
				m_MaxDistance2 = snapDistance,
				m_ControlPoint1 = bestSnapPosition,
				m_ControlPoint2 = controlPoint,
				m_ControlPoints = m_ControlPoints,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabAreaData = m_PrefabAreaData,
				m_LotData = m_LotData,
				m_OwnerData = m_OwnerData,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles,
				m_InstalledUpgrades = m_InstalledUpgrades
			};
			if (m_ApplyTempAreas.IsCreated && m_ApplyTempAreas.Length != 0)
			{
				iterator.m_IgnoreAreas = new NativeParallelHashSet<Entity>(m_ApplyTempAreas.Length, Allocator.Temp);
				for (int i = 0; i < m_ApplyTempAreas.Length; i++)
				{
					Entity entity = m_ApplyTempAreas[i];
					Temp temp = m_TempData[entity];
					iterator.m_IgnoreAreas.Add(temp.m_Original);
					if ((!m_OwnerData.TryGetComponent(entity, out var componentData) || !m_Nodes.HasBuffer(componentData.m_Owner)) && (temp.m_Flags & TempFlags.Delete) == 0)
					{
						Entity area = (((temp.m_Flags & TempFlags.Create) != 0) ? entity : temp.m_Original);
						DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_Nodes[entity];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							int2 nodeIndex = new int2(j, math.select(j + 1, 0, j == dynamicBuffer.Length - 1));
							Line3.Segment line = new Line3.Segment(dynamicBuffer[nodeIndex.x].m_Position, dynamicBuffer[nodeIndex.y].m_Position);
							iterator.CheckLine(line, snapDistance, area, nodeIndex, m_LotData.HasComponent(entity));
						}
					}
				}
			}
			m_AreaSearchTree.Iterate(ref iterator);
			if (iterator.m_IgnoreAreas.IsCreated)
			{
				iterator.m_IgnoreAreas.Dispose();
			}
		}

		private bool FindControlPoint(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance, Entity preferredArea, bool ignoreStartPositions, int selfSnap)
		{
			bestSnapPosition.m_OriginalEntity = Entity.Null;
			NativeList<SnapLine> snapLines = new NativeList<SnapLine>(10, Allocator.Temp);
			if ((m_Snap & Snap.StraightDirection) != Snap.None)
			{
				if (m_State == State.Create)
				{
					ControlPoint controlPoint2 = controlPoint;
					controlPoint2.m_OriginalEntity = Entity.Null;
					controlPoint2.m_Position = controlPoint.m_HitPosition;
					float3 resultDir = default(float3);
					float bestDirectionDistance = float.MaxValue;
					if (m_ControlPoints.Length >= 2)
					{
						ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 2];
						if (!controlPoint3.m_Direction.Equals(default(float2)))
						{
							ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref resultDir, controlPoint.m_HitPosition, controlPoint3.m_Position, new float3(controlPoint3.m_Direction.x, 0f, controlPoint3.m_Direction.y), snapDistance);
						}
					}
					if (m_ControlPoints.Length >= 3)
					{
						ControlPoint controlPoint4 = m_ControlPoints[m_ControlPoints.Length - 3];
						ControlPoint controlPoint5 = m_ControlPoints[m_ControlPoints.Length - 2];
						float2 @float = math.normalizesafe(controlPoint4.m_Position.xz - controlPoint5.m_Position.xz);
						if (!@float.Equals(default(float2)))
						{
							ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref resultDir, controlPoint.m_HitPosition, controlPoint5.m_Position, new float3(@float.x, 0f, @float.y), snapDistance);
						}
					}
					if (!resultDir.Equals(default(float3)))
					{
						controlPoint2.m_Direction = resultDir.xz;
						controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
						ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
						float3 position = controlPoint2.m_Position;
						float3 endPos = position;
						endPos.xz += controlPoint2.m_Direction;
						ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(position, endPos), SnapLineFlags.Hidden, 0f));
					}
				}
				else if (m_State == State.Modify)
				{
					for (int i = 0; i < m_MoveStartPositions.Length; i++)
					{
						ControlPoint controlPoint6 = m_MoveStartPositions[i];
						if (!m_Nodes.HasBuffer(controlPoint6.m_OriginalEntity) || !math.any(controlPoint6.m_ElementIndex >= 0))
						{
							continue;
						}
						DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_Nodes[controlPoint6.m_OriginalEntity];
						if (dynamicBuffer.Length < 3)
						{
							continue;
						}
						int4 @int = math.select(controlPoint6.m_ElementIndex.x + new int4(-2, -1, 1, 2), controlPoint6.m_ElementIndex.y + new int4(-1, 0, 1, 2), controlPoint6.m_ElementIndex.y >= 0);
						@int = math.select(@int, @int + new int2(dynamicBuffer.Length, -dynamicBuffer.Length).xxyy, new bool4(@int.xy < 0, @int.zw >= dynamicBuffer.Length));
						float3 position2 = dynamicBuffer[@int.x].m_Position;
						float3 position3 = dynamicBuffer[@int.y].m_Position;
						float3 position4 = dynamicBuffer[@int.z].m_Position;
						float3 position5 = dynamicBuffer[@int.w].m_Position;
						float2 float2 = math.normalizesafe(position2.xz - position3.xz);
						float2 float3 = math.normalizesafe(position5.xz - position4.xz);
						if (!float2.Equals(default(float2)))
						{
							ControlPoint controlPoint7 = controlPoint;
							controlPoint7.m_OriginalEntity = Entity.Null;
							controlPoint7.m_Position = controlPoint.m_HitPosition;
							float3 resultDir2 = default(float3);
							float bestDirectionDistance2 = float.MaxValue;
							ToolUtils.DirectionSnap(ref bestDirectionDistance2, ref controlPoint7.m_Position, ref resultDir2, controlPoint.m_HitPosition, position3, new float3(float2.x, 0f, float2.y), snapDistance);
							if (!resultDir2.Equals(default(float3)))
							{
								controlPoint7.m_Direction = resultDir2.xz;
								controlPoint7.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, controlPoint7.m_Position, controlPoint7.m_Direction);
								ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint7);
								float3 position6 = controlPoint7.m_Position;
								float3 endPos2 = position6;
								endPos2.xz += controlPoint7.m_Direction;
								ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint7, NetUtils.StraightCurve(position6, endPos2), SnapLineFlags.Hidden, 0f));
							}
						}
						if (!float3.Equals(default(float2)))
						{
							ControlPoint controlPoint8 = controlPoint;
							controlPoint8.m_OriginalEntity = Entity.Null;
							controlPoint8.m_Position = controlPoint.m_HitPosition;
							float3 resultDir3 = default(float3);
							float bestDirectionDistance3 = float.MaxValue;
							ToolUtils.DirectionSnap(ref bestDirectionDistance3, ref controlPoint8.m_Position, ref resultDir3, controlPoint.m_HitPosition, position4, new float3(float3.x, 0f, float3.y), snapDistance);
							if (!resultDir3.Equals(default(float3)))
							{
								controlPoint8.m_Direction = resultDir3.xz;
								controlPoint8.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, controlPoint8.m_Position, controlPoint8.m_Direction);
								ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint8);
								float3 position7 = controlPoint8.m_Position;
								float3 endPos3 = position7;
								endPos3.xz += controlPoint8.m_Direction;
								ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint8, NetUtils.StraightCurve(position7, endPos3), SnapLineFlags.Hidden, 0f));
							}
						}
					}
				}
			}
			if ((m_Snap & Snap.ExistingGeometry) != Snap.None || preferredArea != Entity.Null || ignoreStartPositions || selfSnap >= 1)
			{
				float num = math.select(snapDistance, snapDistance * 0.5f, (m_Snap & Snap.ExistingGeometry) == 0);
				AreaIterator iterator = new AreaIterator
				{
					m_EditorMode = m_EditorMode,
					m_IgnoreStartPositions = ignoreStartPositions,
					m_Snap = m_Snap,
					m_AreaType = type,
					m_Bounds = new Bounds3(controlPoint.m_HitPosition - num, controlPoint.m_HitPosition + num),
					m_MaxDistance = num,
					m_PreferArea = preferredArea,
					m_ControlPoint = controlPoint,
					m_BestSnapPosition = bestSnapPosition,
					m_SnapLines = snapLines,
					m_MoveStartPositions = m_MoveStartPositions,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabAreaData = m_PrefabAreaData,
					m_LotData = m_LotData,
					m_OwnerData = m_OwnerData,
					m_Nodes = m_Nodes,
					m_Triangles = m_Triangles,
					m_InstalledUpgrades = m_InstalledUpgrades
				};
				if (m_ApplyTempAreas.IsCreated && m_ApplyTempAreas.Length != 0)
				{
					iterator.m_IgnoreAreas = new NativeParallelHashSet<Entity>(m_ApplyTempAreas.Length, Allocator.Temp);
					for (int j = 0; j < m_ApplyTempAreas.Length; j++)
					{
						Entity entity = m_ApplyTempAreas[j];
						Temp temp = m_TempData[entity];
						iterator.m_IgnoreAreas.Add(temp.m_Original);
						if ((m_OwnerData.TryGetComponent(entity, out var componentData) && m_Nodes.HasBuffer(componentData.m_Owner)) || (temp.m_Flags & TempFlags.Delete) != 0)
						{
							continue;
						}
						Entity entity2 = (((temp.m_Flags & TempFlags.Create) != 0) ? entity : temp.m_Original);
						if ((m_Snap & Snap.ExistingGeometry) != Snap.None || entity2 == preferredArea)
						{
							DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_Nodes[entity];
							for (int k = 0; k < dynamicBuffer2.Length; k++)
							{
								int2 nodeIndex = new int2(k, math.select(k + 1, 0, k == dynamicBuffer2.Length - 1));
								Line3.Segment line = new Line3.Segment(dynamicBuffer2[nodeIndex.x].m_Position, dynamicBuffer2[nodeIndex.y].m_Position);
								iterator.CheckLine(line, num, entity2, nodeIndex, !m_EditorMode && m_LotData.HasComponent(entity));
							}
						}
					}
				}
				if ((m_Snap & Snap.ExistingGeometry) != Snap.None || preferredArea != Entity.Null || ignoreStartPositions)
				{
					m_AreaSearchTree.Iterate(ref iterator);
				}
				for (int l = 0; l < selfSnap; l++)
				{
					Line3.Segment line2 = new Line3.Segment(m_ControlPoints[l].m_Position, m_ControlPoints[l + 1].m_Position);
					iterator.CheckLine(line2, num, Entity.Null, new int2(l, l + 1), lockFirstEdge: false);
				}
				bestSnapPosition = iterator.m_BestSnapPosition;
				if (iterator.m_IgnoreAreas.IsCreated)
				{
					iterator.m_IgnoreAreas.Dispose();
				}
			}
			if ((m_Snap & (Snap.NetSide | Snap.NetMiddle)) != Snap.None && (m_State != State.Default || m_AllowCreateArea))
			{
				NetIterator iterator2 = new NetIterator
				{
					m_Snap = m_Snap,
					m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance),
					m_MaxDistance = snapDistance,
					m_AreaType = type,
					m_ControlPoint = controlPoint,
					m_BestSnapPosition = bestSnapPosition,
					m_SnapLines = snapLines,
					m_CurveData = m_CurveData,
					m_EdgeGeometryData = m_EdgeGeometryData,
					m_StartGeometryData = m_StartGeometryData,
					m_EndGeometryData = m_EndGeometryData,
					m_CompositionData = m_CompositionData,
					m_PrefabCompositionData = m_PrefabCompositionData
				};
				m_NetSearchTree.Iterate(ref iterator2);
				bestSnapPosition = iterator2.m_BestSnapPosition;
			}
			if ((m_Snap & (Snap.ObjectSide | Snap.LotGrid)) != Snap.None && (m_State != State.Default || m_AllowCreateArea))
			{
				ObjectIterator iterator3 = new ObjectIterator
				{
					m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance),
					m_MaxDistance = snapDistance,
					m_AreaType = type,
					m_Snap = m_Snap,
					m_ControlPoint = controlPoint,
					m_BestSnapPosition = bestSnapPosition,
					m_SnapLines = snapLines,
					m_TransformData = m_TransformData,
					m_PrefabRefData = m_PrefabRefData,
					m_BuildingData = m_PrefabBuildingData,
					m_BuildingExtensionData = m_BuildingExtensionData,
					m_AssetStampData = m_AssetStampData,
					m_ObjectGeometryData = m_ObjectGeometryData
				};
				m_ObjectSearchTree.Iterate(ref iterator3);
				bestSnapPosition = iterator3.m_BestSnapPosition;
			}
			snapLines.Dispose();
			return m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity);
		}
	}

	[BurstCompile]
	private struct RemoveMapTilesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> m_CacheType;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (m_ControlPoints.Length == 1 && m_ControlPoints[0].Equals(default(ControlPoint)))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Game.Areas.Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<LocalNodeCache> bufferAccessor2 = chunk.GetBufferAccessor(ref m_CacheType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity original = nativeArray[i];
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor[i];
				Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
				CreationDefinition component = new CreationDefinition
				{
					m_Original = original
				};
				component.m_Flags |= CreationFlags.Delete;
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component);
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Updated));
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(unfilteredChunkIndex, e).CopyFrom(dynamicBuffer.AsNativeArray());
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer2 = bufferAccessor2[i];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(unfilteredChunkIndex, e).CopyFrom(dynamicBuffer2.AsNativeArray());
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public bool m_AllowCreateArea;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_Recreate;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<Entity> m_ApplyTempAreas;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<Entity> m_ApplyTempBuildings;

		[ReadOnly]
		public NativeList<ControlPoint> m_MoveStartPositions;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Clear> m_ClearData;

		[ReadOnly]
		public ComponentLookup<Space> m_SpaceData;

		[ReadOnly]
		public ComponentLookup<Area> m_AreaData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_CachedNodes;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		public NativeValue<Tooltip> m_Tooltip;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (m_ControlPoints.Length != 1 || !m_ControlPoints[0].Equals(default(ControlPoint)))
			{
				switch (m_Mode)
				{
				case Mode.Edit:
					Edit();
					break;
				case Mode.Generate:
					Generate();
					break;
				}
			}
		}

		private void Generate()
		{
			int2 @int = default(int2);
			@int.y = 0;
			Bounds2 bounds = default(Bounds2);
			while (@int.y < 23)
			{
				@int.x = 0;
				while (@int.x < 23)
				{
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Prefab = m_Prefab
					};
					float2 @float = new float2(23f, 23f) * 311.65216f;
					bounds.min = (float2)@int * 623.3043f - @float;
					bounds.max = (float2)(@int + 1) * 623.3043f - @float;
					DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
					dynamicBuffer.ResizeUninitialized(5);
					dynamicBuffer[0] = new Game.Areas.Node(new float3(bounds.min.x, 0f, bounds.min.y), float.MinValue);
					dynamicBuffer[1] = new Game.Areas.Node(new float3(bounds.min.x, 0f, bounds.max.y), float.MinValue);
					dynamicBuffer[2] = new Game.Areas.Node(new float3(bounds.max.x, 0f, bounds.max.y), float.MinValue);
					dynamicBuffer[3] = new Game.Areas.Node(new float3(bounds.max.x, 0f, bounds.min.y), float.MinValue);
					dynamicBuffer[4] = dynamicBuffer[0];
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, default(Updated));
					@int.x++;
				}
				@int.y++;
			}
			m_Tooltip.value = Tooltip.GenerateAreas;
		}

		private void GetControlPoints(int index, out ControlPoint firstPoint, out ControlPoint lastPoint)
		{
			switch (m_State)
			{
			case State.Default:
				firstPoint = m_ControlPoints[index];
				lastPoint = m_ControlPoints[index];
				break;
			case State.Create:
				firstPoint = default(ControlPoint);
				lastPoint = m_ControlPoints[m_ControlPoints.Length - 1];
				break;
			case State.Modify:
				firstPoint = m_MoveStartPositions[index];
				lastPoint = m_ControlPoints[0];
				break;
			case State.Remove:
				firstPoint = m_MoveStartPositions[index];
				lastPoint = m_ControlPoints[0];
				break;
			default:
				firstPoint = default(ControlPoint);
				lastPoint = default(ControlPoint);
				break;
			}
		}

		private void Edit()
		{
			AreaGeometryData areaData = m_PrefabAreaData[m_Prefab];
			int num = m_State switch
			{
				State.Default => m_ControlPoints.Length, 
				State.Create => 1, 
				State.Modify => m_MoveStartPositions.Length, 
				State.Remove => m_MoveStartPositions.Length, 
				_ => 0, 
			};
			m_Tooltip.value = Tooltip.None;
			bool flag = false;
			NativeParallelHashSet<Entity> createdEntities = new NativeParallelHashSet<Entity>(num * 2, Allocator.Temp);
			for (int i = 0; i < num; i++)
			{
				GetControlPoints(i, out var firstPoint, out var _);
				if (m_Nodes.HasBuffer(firstPoint.m_OriginalEntity) && math.any(firstPoint.m_ElementIndex >= 0))
				{
					createdEntities.Add(firstPoint.m_OriginalEntity);
				}
			}
			NativeList<ClearAreaData> clearAreas = default(NativeList<ClearAreaData>);
			for (int j = 0; j < num; j++)
			{
				GetControlPoints(j, out var firstPoint2, out var lastPoint2);
				if (j == 0 && m_State == State.Modify)
				{
					flag = !firstPoint2.Equals(lastPoint2);
				}
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = m_Prefab
				};
				if (m_Nodes.HasBuffer(firstPoint2.m_OriginalEntity) && math.any(firstPoint2.m_ElementIndex >= 0))
				{
					component.m_Original = firstPoint2.m_OriginalEntity;
				}
				else if (m_Recreate != Entity.Null)
				{
					component.m_Original = m_Recreate;
				}
				float minNodeDistance = AreaUtils.GetMinNodeDistance(areaData);
				int2 @int = default(int2);
				DynamicBuffer<Game.Areas.Node> nodes = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
				DynamicBuffer<LocalNodeCache> dynamicBuffer = default(DynamicBuffer<LocalNodeCache>);
				bool isComplete = false;
				if (m_Nodes.HasBuffer(firstPoint2.m_OriginalEntity) && math.any(firstPoint2.m_ElementIndex >= 0))
				{
					component.m_Flags |= CreationFlags.Relocate;
					isComplete = true;
					Entity sourceArea = GetSourceArea(firstPoint2.m_OriginalEntity);
					DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_Nodes[sourceArea];
					DynamicBuffer<LocalNodeCache> dynamicBuffer3 = default(DynamicBuffer<LocalNodeCache>);
					if (m_CachedNodes.HasBuffer(sourceArea))
					{
						dynamicBuffer3 = m_CachedNodes[sourceArea];
					}
					float num2 = float.MinValue;
					int num3 = -1;
					if (lastPoint2.m_ElementIndex.x >= 0)
					{
						num3 = lastPoint2.m_ElementIndex.x;
						if (m_OwnerData.TryGetComponent(firstPoint2.m_OriginalEntity, out var componentData))
						{
							Entity owner = componentData.m_Owner;
							while (m_OwnerData.HasComponent(owner) && !m_BuildingData.HasComponent(owner))
							{
								if (m_LocalTransformCacheData.HasComponent(owner))
								{
									num3 = m_LocalTransformCacheData[owner].m_ParentMesh;
								}
								owner = m_OwnerData[owner].m_Owner;
							}
							if (m_TransformData.TryGetComponent(owner, out var componentData2))
							{
								num2 = lastPoint2.m_Position.y - componentData2.m_Position.y;
							}
						}
						if (num3 != -1)
						{
							if (num2 == float.MinValue)
							{
								num2 = 0f;
							}
						}
						else
						{
							num2 = float.MinValue;
						}
					}
					if (firstPoint2.m_ElementIndex.y >= 0)
					{
						int y = firstPoint2.m_ElementIndex.y;
						int index = math.select(firstPoint2.m_ElementIndex.y + 1, 0, firstPoint2.m_ElementIndex.y == dynamicBuffer2.Length - 1);
						float2 @float = new float2(math.distance(lastPoint2.m_Position, dynamicBuffer2[y].m_Position), math.distance(lastPoint2.m_Position, dynamicBuffer2[index].m_Position));
						bool flag2 = flag && math.any(@float < minNodeDistance);
						int num4 = math.select(1, 0, flag2 || !flag);
						int length = dynamicBuffer2.Length + num4;
						nodes.ResizeUninitialized(length);
						int num5 = 0;
						if (dynamicBuffer3.IsCreated)
						{
							dynamicBuffer = m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
							dynamicBuffer.ResizeUninitialized(length);
							for (int k = 0; k <= firstPoint2.m_ElementIndex.y; k++)
							{
								nodes[num5] = dynamicBuffer2[k];
								dynamicBuffer[num5] = dynamicBuffer3[k];
								num5++;
							}
							@int.x = num5;
							for (int l = 0; l < num4; l++)
							{
								nodes[num5] = new Game.Areas.Node(lastPoint2.m_Position, num2);
								dynamicBuffer[num5] = new LocalNodeCache
								{
									m_Position = lastPoint2.m_Position,
									m_ParentMesh = num3
								};
								num5++;
							}
							@int.y = num5;
							for (int m = firstPoint2.m_ElementIndex.y + 1; m < dynamicBuffer2.Length; m++)
							{
								nodes[num5] = dynamicBuffer2[m];
								dynamicBuffer[num5] = dynamicBuffer3[m];
								num5++;
							}
						}
						else
						{
							for (int n = 0; n <= firstPoint2.m_ElementIndex.y; n++)
							{
								nodes[num5++] = dynamicBuffer2[n];
							}
							for (int num6 = 0; num6 < num4; num6++)
							{
								nodes[num5++] = new Game.Areas.Node(lastPoint2.m_Position, num2);
							}
							for (int num7 = firstPoint2.m_ElementIndex.y + 1; num7 < dynamicBuffer2.Length; num7++)
							{
								nodes[num5++] = dynamicBuffer2[num7];
							}
						}
						switch (m_State)
						{
						case State.Default:
							if (m_AllowCreateArea)
							{
								m_Tooltip.value = Tooltip.CreateAreaOrModifyEdge;
							}
							else
							{
								m_Tooltip.value = Tooltip.ModifyEdge;
							}
							break;
						case State.Modify:
							if (!flag2 && flag)
							{
								m_Tooltip.value = Tooltip.InsertNode;
							}
							break;
						}
					}
					else
					{
						bool flag3 = false;
						if (!m_OwnerData.HasComponent(component.m_Original) || dynamicBuffer2.Length >= 4)
						{
							if (m_State == State.Remove)
							{
								flag3 = true;
							}
							else
							{
								int index2 = math.select(firstPoint2.m_ElementIndex.x - 1, dynamicBuffer2.Length - 1, firstPoint2.m_ElementIndex.x == 0);
								int index3 = math.select(firstPoint2.m_ElementIndex.x + 1, 0, firstPoint2.m_ElementIndex.x == dynamicBuffer2.Length - 1);
								float2 float2 = new float2(math.distance(lastPoint2.m_Position, dynamicBuffer2[index2].m_Position), math.distance(lastPoint2.m_Position, dynamicBuffer2[index3].m_Position));
								flag3 = flag && math.any(float2 < minNodeDistance);
							}
						}
						int num8 = math.select(0, 1, flag || flag3);
						int num9 = math.select(1, 0, flag3 || !flag);
						int num10 = dynamicBuffer2.Length + num9 - num8;
						nodes.ResizeUninitialized(num10);
						int num11 = 0;
						if (dynamicBuffer3.IsCreated)
						{
							dynamicBuffer = m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
							dynamicBuffer.ResizeUninitialized(num10);
							for (int num12 = 0; num12 <= firstPoint2.m_ElementIndex.x - num8; num12++)
							{
								nodes[num11] = dynamicBuffer2[num12];
								dynamicBuffer[num11] = dynamicBuffer3[num12];
								num11++;
							}
							@int.x = num11;
							for (int num13 = 0; num13 < num9; num13++)
							{
								nodes[num11] = new Game.Areas.Node(lastPoint2.m_Position, num2);
								dynamicBuffer[num11] = new LocalNodeCache
								{
									m_Position = lastPoint2.m_Position,
									m_ParentMesh = num3
								};
								num11++;
							}
							@int.y = num11;
							for (int num14 = firstPoint2.m_ElementIndex.x + 1; num14 < dynamicBuffer2.Length; num14++)
							{
								nodes[num11] = dynamicBuffer2[num14];
								dynamicBuffer[num11] = dynamicBuffer3[num14];
								num11++;
							}
						}
						else
						{
							for (int num15 = 0; num15 <= firstPoint2.m_ElementIndex.x - num8; num15++)
							{
								nodes[num11++] = dynamicBuffer2[num15];
							}
							for (int num16 = 0; num16 < num9; num16++)
							{
								nodes[num11++] = new Game.Areas.Node(lastPoint2.m_Position, num2);
							}
							for (int num17 = firstPoint2.m_ElementIndex.x + 1; num17 < dynamicBuffer2.Length; num17++)
							{
								nodes[num11++] = dynamicBuffer2[num17];
							}
						}
						if (num10 < 3)
						{
							component.m_Flags |= CreationFlags.Delete;
						}
						switch (m_State)
						{
						case State.Default:
							if (m_AllowCreateArea)
							{
								m_Tooltip.value = Tooltip.CreateAreaOrModifyNode;
							}
							else
							{
								m_Tooltip.value = Tooltip.ModifyNode;
							}
							break;
						case State.Modify:
							if (num10 < 3)
							{
								m_Tooltip.value = Tooltip.DeleteArea;
							}
							else if (flag3)
							{
								m_Tooltip.value = Tooltip.MergeNodes;
							}
							else if (flag)
							{
								m_Tooltip.value = Tooltip.MoveNode;
							}
							break;
						case State.Remove:
							if (num10 < 3)
							{
								m_Tooltip.value = Tooltip.DeleteArea;
							}
							else if (flag3)
							{
								m_Tooltip.value = Tooltip.RemoveNode;
							}
							break;
						}
					}
				}
				else
				{
					if (m_Recreate != Entity.Null)
					{
						component.m_Flags |= CreationFlags.Recreate;
					}
					bool flag4 = false;
					if (m_ControlPoints.Length >= 2)
					{
						flag4 = math.distance(m_ControlPoints[m_ControlPoints.Length - 2].m_Position, m_ControlPoints[m_ControlPoints.Length - 1].m_Position) < minNodeDistance;
					}
					int num18 = math.select(m_ControlPoints.Length, m_ControlPoints.Length - 1, flag4);
					nodes.ResizeUninitialized(num18);
					if (m_EditorMode)
					{
						dynamicBuffer = m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
						dynamicBuffer.ResizeUninitialized(num18);
						@int = new int2(0, num18);
						float num19 = float.MinValue;
						int num20 = lastPoint2.m_ElementIndex.x;
						if (m_TransformData.HasComponent(lastPoint2.m_OriginalEntity))
						{
							Entity entity = lastPoint2.m_OriginalEntity;
							while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(entity))
							{
								if (m_LocalTransformCacheData.HasComponent(entity))
								{
									num20 = m_LocalTransformCacheData[entity].m_ParentMesh;
								}
								entity = m_OwnerData[entity].m_Owner;
							}
							if (m_TransformData.TryGetComponent(entity, out var componentData3))
							{
								num19 = componentData3.m_Position.y;
							}
						}
						for (int num21 = 0; num21 < num18; num21++)
						{
							int num22 = -1;
							float num23 = float.MinValue;
							if (m_ControlPoints[num21].m_ElementIndex.x >= 0)
							{
								num22 = math.select(m_ControlPoints[num21].m_ElementIndex.x, num20, num20 != -1);
								num23 = math.select(num23, m_ControlPoints[num21].m_Position.y - num19, num19 != float.MinValue);
							}
							if (num22 != -1)
							{
								if (num23 == float.MinValue)
								{
									num23 = 0f;
								}
							}
							else
							{
								num23 = float.MinValue;
							}
							nodes[num21] = new Game.Areas.Node(m_ControlPoints[num21].m_Position, num23);
							dynamicBuffer[num21] = new LocalNodeCache
							{
								m_Position = m_ControlPoints[num21].m_Position,
								m_ParentMesh = num22
							};
						}
					}
					else
					{
						for (int num24 = 0; num24 < num18; num24++)
						{
							nodes[num24] = new Game.Areas.Node(m_ControlPoints[num24].m_Position, float.MinValue);
						}
					}
					switch (m_State)
					{
					case State.Default:
						if (m_ControlPoints.Length == 1 && m_AllowCreateArea)
						{
							m_Tooltip.value = Tooltip.CreateArea;
						}
						break;
					case State.Create:
						if (!flag4)
						{
							if (m_ControlPoints.Length >= 4 && m_ControlPoints[0].m_Position.Equals(m_ControlPoints[m_ControlPoints.Length - 1].m_Position))
							{
								m_Tooltip.value = Tooltip.CompleteArea;
							}
							else
							{
								m_Tooltip.value = Tooltip.AddNode;
							}
						}
						break;
					}
				}
				bool flag5 = false;
				Transform inverseParentTransform = default(Transform);
				if (m_TransformData.HasComponent(lastPoint2.m_OriginalEntity))
				{
					if ((areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0)
					{
						ClearAreaHelpers.FillClearAreas(m_PrefabRefData[lastPoint2.m_OriginalEntity].m_Prefab, m_TransformData[lastPoint2.m_OriginalEntity], nodes, isComplete, m_PrefabObjectGeometryData, ref clearAreas);
					}
					OwnerDefinition ownerDefinition = GetOwnerDefinition(lastPoint2.m_OriginalEntity, component.m_Original, createdEntities, upgrade: true, (areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0, clearAreas);
					if (ownerDefinition.m_Prefab != Entity.Null)
					{
						inverseParentTransform.m_Position = -ownerDefinition.m_Position;
						inverseParentTransform.m_Rotation = math.inverse(ownerDefinition.m_Rotation);
						flag5 = true;
						m_CommandBuffer.AddComponent(e, ownerDefinition);
					}
				}
				else if (m_OwnerData.HasComponent(component.m_Original))
				{
					Entity owner2 = m_OwnerData[component.m_Original].m_Owner;
					if (m_TransformData.HasComponent(owner2))
					{
						if ((areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0)
						{
							ClearAreaHelpers.FillClearAreas(m_PrefabRefData[owner2].m_Prefab, m_TransformData[owner2], nodes, isComplete, m_PrefabObjectGeometryData, ref clearAreas);
						}
						OwnerDefinition ownerDefinition2 = GetOwnerDefinition(owner2, component.m_Original, createdEntities, upgrade: true, (areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0, clearAreas);
						if (ownerDefinition2.m_Prefab != Entity.Null)
						{
							inverseParentTransform.m_Position = -ownerDefinition2.m_Position;
							inverseParentTransform.m_Rotation = math.inverse(ownerDefinition2.m_Rotation);
							flag5 = true;
							m_CommandBuffer.AddComponent(e, ownerDefinition2);
						}
						else
						{
							Transform transform = m_TransformData[owner2];
							inverseParentTransform.m_Position = -transform.m_Position;
							inverseParentTransform.m_Rotation = math.inverse(transform.m_Rotation);
							flag5 = true;
							component.m_Owner = owner2;
						}
					}
					else
					{
						component.m_Owner = owner2;
					}
				}
				if (flag5)
				{
					for (int num25 = @int.x; num25 < @int.y; num25++)
					{
						LocalNodeCache localNodeCache = dynamicBuffer[num25];
						localNodeCache.m_Position = ObjectUtils.WorldToLocal(inverseParentTransform, localNodeCache.m_Position);
					}
				}
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				if (m_AreaData.TryGetComponent(component.m_Original, out var componentData4) && m_SubObjects.TryGetBuffer(component.m_Original, out var bufferData) && (componentData4.m_Flags & AreaFlags.Complete) != 0)
				{
					CheckSubObjects(bufferData, nodes, createdEntities, minNodeDistance, (componentData4.m_Flags & AreaFlags.CounterClockwise) != 0);
				}
				if (clearAreas.IsCreated)
				{
					clearAreas.Clear();
				}
			}
			if (clearAreas.IsCreated)
			{
				clearAreas.Dispose();
			}
			createdEntities.Dispose();
		}

		private Entity GetSourceArea(Entity originalArea)
		{
			if (m_ApplyTempAreas.IsCreated)
			{
				for (int i = 0; i < m_ApplyTempAreas.Length; i++)
				{
					Entity entity = m_ApplyTempAreas[i];
					if (originalArea == m_TempData[entity].m_Original)
					{
						return entity;
					}
				}
			}
			return originalArea;
		}

		private void CheckSubObjects(DynamicBuffer<Game.Objects.SubObject> subObjects, DynamicBuffer<Game.Areas.Node> nodes, NativeParallelHashSet<Entity> createdEntities, float minNodeDistance, bool isCounterClockwise)
		{
			Line2.Segment line = default(Line2.Segment);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Game.Objects.SubObject subObject = subObjects[i];
				if (!m_BuildingData.HasComponent(subObject.m_SubObject))
				{
					continue;
				}
				if (m_ApplyTempBuildings.IsCreated)
				{
					bool flag = false;
					for (int j = 0; j < m_ApplyTempBuildings.Length; j++)
					{
						if (m_ApplyTempBuildings[j] == subObject.m_SubObject)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
				}
				Transform transform = m_TransformData[subObject.m_SubObject];
				PrefabRef prefabRef = m_PrefabRefData[subObject.m_SubObject];
				if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					continue;
				}
				float num;
				if ((componentData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					num = componentData.m_Size.x * 0.5f;
				}
				else
				{
					num = math.length(MathUtils.Size(componentData.m_Bounds.xz)) * 0.5f;
					transform.m_Position.xz -= math.rotate(transform.m_Rotation, MathUtils.Center(componentData.m_Bounds)).xz;
				}
				float num2 = 0f;
				int num3 = -1;
				bool flag2 = nodes.Length <= 2;
				if (!flag2)
				{
					float num4 = float.MaxValue;
					float num5 = num + minNodeDistance;
					num5 *= num5;
					line.a = nodes[nodes.Length - 1].m_Position.xz;
					for (int k = 0; k < nodes.Length; k++)
					{
						line.b = nodes[k].m_Position.xz;
						float t;
						float num6 = MathUtils.DistanceSquared(line, transform.m_Position.xz, out t);
						if (num6 < num5)
						{
							flag2 = true;
							break;
						}
						if (num6 < num4)
						{
							num4 = num6;
							num2 = t;
							num3 = k;
						}
						line.a = line.b;
					}
				}
				if (!flag2 && num3 >= 0)
				{
					int2 @int = math.select(new int2(num3 - 1, num3), new int2(num3 - 2, num3 + 1), new bool2(num2 == 0f, num2 == 1f));
					@int = math.select(@int, @int + new int2(nodes.Length, -nodes.Length), new bool2(@int.x < 0, @int.y >= nodes.Length));
					@int = math.select(@int, @int.yx, isCounterClockwise);
					float2 xz = nodes[@int.x].m_Position.xz;
					float2 xz2 = nodes[@int.y].m_Position.xz;
					flag2 = math.dot(transform.m_Position.xz - xz, MathUtils.Right(xz2 - xz)) <= 0f;
				}
				if (flag2)
				{
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Original = subObject.m_SubObject
					};
					component.m_Flags |= CreationFlags.Delete;
					ObjectDefinition component2 = new ObjectDefinition
					{
						m_ParentMesh = -1,
						m_Position = transform.m_Position,
						m_Rotation = transform.m_Rotation,
						m_LocalPosition = transform.m_Position,
						m_LocalRotation = transform.m_Rotation
					};
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, component2);
					m_CommandBuffer.AddComponent(e, default(Updated));
					UpdateSubNets(transform, prefabRef.m_Prefab, subObject.m_SubObject, default(NativeList<ClearAreaData>), removeAll: true);
					UpdateSubAreas(transform, prefabRef.m_Prefab, subObject.m_SubObject, createdEntities, default(NativeList<ClearAreaData>), removeAll: true);
				}
			}
		}

		private OwnerDefinition GetOwnerDefinition(Entity parent, Entity area, NativeParallelHashSet<Entity> createdEntities, bool upgrade, bool fullUpdate, NativeList<ClearAreaData> clearAreas)
		{
			OwnerDefinition result = default(OwnerDefinition);
			if (!m_EditorMode)
			{
				return result;
			}
			Entity entity = parent;
			while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(entity))
			{
				entity = m_OwnerData[entity].m_Owner;
			}
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			if (m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
			{
				if (fullUpdate && m_TransformData.HasComponent(entity))
				{
					Transform transform = m_TransformData[entity];
					ClearAreaHelpers.FillClearAreas(bufferData, area, m_TransformData, m_ClearData, m_PrefabRefData, m_PrefabObjectGeometryData, m_SubAreas, m_Nodes, m_Triangles, ref clearAreas);
					ClearAreaHelpers.InitClearAreas(clearAreas, transform);
					if (createdEntities.Add(entity))
					{
						Entity owner = Entity.Null;
						if (m_OwnerData.HasComponent(entity))
						{
							owner = m_OwnerData[entity].m_Owner;
						}
						UpdateOwnerObject(owner, entity, createdEntities, transform, default(OwnerDefinition), upgrade: false, clearAreas);
					}
					ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
					ownerDefinition.m_Position = transform.m_Position;
					ownerDefinition.m_Rotation = transform.m_Rotation;
				}
				entity = bufferData[0].m_Upgrade;
			}
			if (m_TransformData.HasComponent(entity))
			{
				Transform transform2 = m_TransformData[entity];
				if (createdEntities.Add(entity))
				{
					Entity owner2 = Entity.Null;
					if (ownerDefinition.m_Prefab == Entity.Null && m_OwnerData.HasComponent(entity))
					{
						owner2 = m_OwnerData[entity].m_Owner;
					}
					UpdateOwnerObject(owner2, entity, createdEntities, transform2, ownerDefinition, upgrade, default(NativeList<ClearAreaData>));
				}
				result.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				result.m_Position = transform2.m_Position;
				result.m_Rotation = transform2.m_Rotation;
			}
			return result;
		}

		private void UpdateOwnerObject(Entity owner, Entity original, NativeParallelHashSet<Entity> createdEntities, Transform transform, OwnerDefinition ownerDefinition, bool upgrade, NativeList<ClearAreaData> clearAreas)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			Entity prefab = m_PrefabRefData[original].m_Prefab;
			CreationDefinition component = new CreationDefinition
			{
				m_Owner = owner,
				m_Original = original
			};
			if (upgrade)
			{
				component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
			}
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_ParentMesh = -1,
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation
			};
			if (m_TransformData.HasComponent(owner))
			{
				Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
				component2.m_LocalPosition = transform2.m_Position;
				component2.m_LocalRotation = transform2.m_Rotation;
			}
			else
			{
				component2.m_LocalPosition = transform.m_Position;
				component2.m_LocalRotation = transform.m_Rotation;
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
			UpdateSubNets(transform, prefab, original, clearAreas, removeAll: false);
			UpdateSubAreas(transform, prefab, original, createdEntities, clearAreas, removeAll: false);
		}

		private void UpdateSubNets(Transform transform, Entity prefab, Entity original, NativeList<ClearAreaData> clearAreas, bool removeAll)
		{
			if (!m_SubNets.HasBuffer(original))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_SubNets[original];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subNet = dynamicBuffer[i].m_SubNet;
				if (m_NodeData.HasComponent(subNet))
				{
					if (!HasEdgeStartOrEnd(subNet, original))
					{
						Game.Net.Node node = m_NodeData[subNet];
						Entity e = m_CommandBuffer.CreateEntity();
						CreationDefinition component = new CreationDefinition
						{
							m_Original = subNet
						};
						if (m_EditorContainerData.HasComponent(subNet))
						{
							component.m_SubPrefab = m_EditorContainerData[subNet].m_Prefab;
						}
						Game.Net.Elevation componentData;
						bool onGround = !m_NetElevationData.TryGetComponent(subNet, out componentData) || math.cmin(math.abs(componentData.m_Elevation)) < 2f;
						if (removeAll)
						{
							component.m_Flags |= CreationFlags.Delete;
						}
						else if (ClearAreaHelpers.ShouldClear(clearAreas, node.m_Position, onGround))
						{
							component.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
						}
						OwnerDefinition component2 = new OwnerDefinition
						{
							m_Prefab = prefab,
							m_Position = transform.m_Position,
							m_Rotation = transform.m_Rotation
						};
						m_CommandBuffer.AddComponent(e, component2);
						m_CommandBuffer.AddComponent(e, component);
						m_CommandBuffer.AddComponent(e, default(Updated));
						NetCourse component3 = new NetCourse
						{
							m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
							m_Length = 0f,
							m_FixedIndex = -1,
							m_StartPosition = 
							{
								m_Entity = subNet,
								m_Position = node.m_Position,
								m_Rotation = node.m_Rotation,
								m_CourseDelta = 0f
							},
							m_EndPosition = 
							{
								m_Entity = subNet,
								m_Position = node.m_Position,
								m_Rotation = node.m_Rotation,
								m_CourseDelta = 1f
							}
						};
						m_CommandBuffer.AddComponent(e, component3);
					}
				}
				else if (m_EdgeData.HasComponent(subNet))
				{
					Edge edge = m_EdgeData[subNet];
					Entity e2 = m_CommandBuffer.CreateEntity();
					CreationDefinition component4 = new CreationDefinition
					{
						m_Original = subNet
					};
					if (m_EditorContainerData.HasComponent(subNet))
					{
						component4.m_SubPrefab = m_EditorContainerData[subNet].m_Prefab;
					}
					Curve curve = m_CurveData[subNet];
					Game.Net.Elevation componentData2;
					bool onGround2 = !m_NetElevationData.TryGetComponent(subNet, out componentData2) || math.cmin(math.abs(componentData2.m_Elevation)) < 2f;
					if (removeAll)
					{
						component4.m_Flags |= CreationFlags.Delete;
					}
					else if (ClearAreaHelpers.ShouldClear(clearAreas, curve.m_Bezier, onGround2))
					{
						component4.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
					}
					OwnerDefinition component5 = new OwnerDefinition
					{
						m_Prefab = prefab,
						m_Position = transform.m_Position,
						m_Rotation = transform.m_Rotation
					};
					m_CommandBuffer.AddComponent(e2, component5);
					m_CommandBuffer.AddComponent(e2, component4);
					m_CommandBuffer.AddComponent(e2, default(Updated));
					NetCourse component6 = default(NetCourse);
					component6.m_Curve = curve.m_Bezier;
					component6.m_Length = MathUtils.Length(component6.m_Curve);
					component6.m_FixedIndex = -1;
					component6.m_StartPosition.m_Entity = edge.m_Start;
					component6.m_StartPosition.m_Position = component6.m_Curve.a;
					component6.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component6.m_Curve));
					component6.m_StartPosition.m_CourseDelta = 0f;
					component6.m_EndPosition.m_Entity = edge.m_End;
					component6.m_EndPosition.m_Position = component6.m_Curve.d;
					component6.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component6.m_Curve));
					component6.m_EndPosition.m_CourseDelta = 1f;
					m_CommandBuffer.AddComponent(e2, component6);
				}
			}
		}

		private bool HasEdgeStartOrEnd(Entity node, Entity owner)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if ((edge2.m_Start == node || edge2.m_End == node) && m_OwnerData.HasComponent(edge) && m_OwnerData[edge].m_Owner == owner)
				{
					return true;
				}
			}
			return false;
		}

		private void UpdateSubAreas(Transform transform, Entity prefab, Entity original, NativeParallelHashSet<Entity> createdEntities, NativeList<ClearAreaData> clearAreas, bool removeAll)
		{
			if (!m_SubAreas.HasBuffer(original))
			{
				return;
			}
			DynamicBuffer<Game.Areas.SubArea> dynamicBuffer = m_SubAreas[original];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity area = dynamicBuffer[i].m_Area;
				if (!createdEntities.Add(area))
				{
					continue;
				}
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Original = area
				};
				OwnerDefinition component2 = new OwnerDefinition
				{
					m_Prefab = prefab,
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				m_CommandBuffer.AddComponent(e, component2);
				DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[area];
				if (removeAll)
				{
					component.m_Flags |= CreationFlags.Delete;
				}
				else if (m_SpaceData.HasComponent(area))
				{
					DynamicBuffer<Triangle> triangles = m_Triangles[area];
					if (ClearAreaHelpers.ShouldClear(clearAreas, nodes, triangles, transform))
					{
						component.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
					}
				}
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(nodes.AsNativeArray());
				if (m_CachedNodes.HasBuffer(area))
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer2 = m_CachedNodes[area];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(e).CopyFrom(dynamicBuffer2.AsNativeArray());
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Clear> __Game_Areas_Clear_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferTypeHandle = state.GetBufferTypeHandle<LocalNodeCache>(isReadOnly: true);
			__Game_Areas_Clear_RO_ComponentLookup = state.GetComponentLookup<Clear>(isReadOnly: true);
			__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Space>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	public const string kToolID = "Area Tool";

	private ObjectToolSystem m_ObjectToolSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private AudioManager m_AudioManager;

	private IProxyAction m_AddAreaNode;

	private IProxyAction m_InsertAreaNode;

	private IProxyAction m_MergeAreaNode;

	private IProxyAction m_MoveAreaNode;

	private IProxyAction m_DeleteAreaNode;

	private IProxyAction m_UndoAreaNode;

	private IProxyAction m_CompleteArea;

	private IProxyAction m_CreateArea;

	private IProxyAction m_DeleteArea;

	private IProxyAction m_DiscardInsertAreaNode;

	private IProxyAction m_DiscardMoveAreaNode;

	private IProxyAction m_DiscardMergeAreaNode;

	private IProxyAction m_CreateAreaOrMoveAreaNode;

	private IProxyAction m_CreateAreaOrInsertAreaNode;

	private bool m_ApplyBlocked;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_TempAreaQuery;

	private EntityQuery m_TempBuildingQuery;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_SoundQuery;

	private ControlPoint m_LastRaycastPoint;

	private NativeList<ControlPoint> m_ControlPoints;

	private NativeList<ControlPoint> m_MoveStartPositions;

	private NativeValue<Tooltip> m_Tooltip;

	private Mode m_LastMode;

	private State m_State;

	private AreaPrefab m_Prefab;

	private bool m_ControlPointsMoved;

	private bool m_AllowCreateArea;

	private bool m_ForceCancel;

	private TypeHandle __TypeHandle;

	public override string toolID => "Area Tool";

	public override int uiModeIndex => (int)actualMode;

	public Mode mode { get; set; }

	public Mode actualMode
	{
		get
		{
			if (!allowGenerate)
			{
				return Mode.Edit;
			}
			return mode;
		}
	}

	public Entity recreate { get; set; }

	public bool underground { get; set; }

	public bool allowGenerate { get; private set; }

	public State state => m_State;

	public Tooltip tooltip => m_Tooltip.value;

	public AreaPrefab prefab
	{
		get
		{
			return m_Prefab;
		}
		set
		{
			if (value != m_Prefab)
			{
				m_Prefab = value;
				allowGenerate = m_ToolSystem.actionMode.IsEditor() && value is MapTilePrefab;
				m_ToolSystem.EventPrefabChanged?.Invoke(value);
			}
		}
	}

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_AddAreaNode;
			yield return m_InsertAreaNode;
			yield return m_MergeAreaNode;
			yield return m_MoveAreaNode;
			yield return m_DeleteAreaNode;
			yield return m_UndoAreaNode;
			yield return m_CompleteArea;
			yield return m_CreateArea;
			yield return m_DeleteArea;
			yield return m_DiscardInsertAreaNode;
			yield return m_DiscardMoveAreaNode;
			yield return m_DiscardMergeAreaNode;
			yield return m_CreateAreaOrMoveAreaNode;
			yield return m_CreateAreaOrInsertAreaNode;
		}
	}

	public override void GetUIModes(List<ToolMode> modes)
	{
		modes.Add(new ToolMode(Mode.Edit.ToString(), 0));
		if (allowGenerate)
		{
			modes.Add(new ToolMode(Mode.Generate.ToString(), 1));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_TempAreaQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Temp>());
		m_TempBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Temp>());
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_AddAreaNode = InputManager.instance.toolActionCollection.GetActionState("Add Area Node", "AreaToolSystem");
		m_InsertAreaNode = InputManager.instance.toolActionCollection.GetActionState("Insert Area Node", "AreaToolSystem");
		m_MergeAreaNode = InputManager.instance.toolActionCollection.GetActionState("Merge Area Node", "AreaToolSystem");
		m_MoveAreaNode = InputManager.instance.toolActionCollection.GetActionState("Move Area Node", "AreaToolSystem");
		m_DeleteAreaNode = InputManager.instance.toolActionCollection.GetActionState("Delete Area Node", "AreaToolSystem");
		m_UndoAreaNode = InputManager.instance.toolActionCollection.GetActionState("Undo Area Node", "AreaToolSystem");
		m_CompleteArea = InputManager.instance.toolActionCollection.GetActionState("Complete Area", "AreaToolSystem");
		m_CreateArea = InputManager.instance.toolActionCollection.GetActionState("Create Area", "AreaToolSystem");
		m_DeleteArea = InputManager.instance.toolActionCollection.GetActionState("Delete Area", "AreaToolSystem");
		m_DiscardInsertAreaNode = InputManager.instance.toolActionCollection.GetActionState("Discard Insert Area Node", "AreaToolSystem");
		m_DiscardMoveAreaNode = InputManager.instance.toolActionCollection.GetActionState("Discard Move Area Node", "AreaToolSystem");
		m_DiscardMergeAreaNode = InputManager.instance.toolActionCollection.GetActionState("Discard Merge Area Node", "AreaToolSystem");
		m_CreateAreaOrMoveAreaNode = InputManager.instance.toolActionCollection.GetActionState("Create Area Or Move Area Node", "AreaToolSystem");
		m_CreateAreaOrInsertAreaNode = InputManager.instance.toolActionCollection.GetActionState("Create Area Or Insert Area Node", "AreaToolSystem");
		selectedSnap &= ~Snap.AutoParent;
		m_ControlPoints = new NativeList<ControlPoint>(20, Allocator.Persistent);
		m_MoveStartPositions = new NativeList<ControlPoint>(10, Allocator.Persistent);
		m_Tooltip = new NativeValue<Tooltip>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		m_MoveStartPositions.Dispose();
		m_Tooltip.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_MoveStartPositions.Clear();
		m_LastRaycastPoint = default(ControlPoint);
		m_LastMode = actualMode;
		m_State = State.Default;
		m_Tooltip.value = Tooltip.None;
		m_AllowCreateArea = false;
		m_ForceCancel = false;
		m_ApplyBlocked = false;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		recreate = Entity.Null;
		base.OnStopRunning();
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			UpdateApplyAction();
			UpdateSecondaryApplyAction();
			UpdateCancelAction();
		}
	}

	private void UpdateApplyAction()
	{
		switch (state)
		{
		case State.Default:
		{
			if (m_ControlPoints.Length < 1 || m_ControlPoints[0].Equals(default(ControlPoint)))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = null;
				break;
			}
			for (int k = 0; k < m_ControlPoints.Length; k++)
			{
				if (!base.EntityManager.TryGetComponent<Area>(m_ControlPoints[k].m_OriginalEntity, out var component2) || (component2.m_Flags & AreaFlags.Complete) == 0 || !base.EntityManager.TryGetBuffer(m_ControlPoints[k].m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer2))
				{
					continue;
				}
				for (int l = 0; l < buffer2.Length; l++)
				{
					if (buffer2[l].m_Position.Equals(m_ControlPoints[k].m_Position))
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled;
						base.applyActionOverride = (base.EntityManager.HasComponent<Game.Areas.Lot>(m_ControlPoints[k].m_OriginalEntity) ? m_MoveAreaNode : m_CreateAreaOrMoveAreaNode);
						return;
					}
				}
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = (base.EntityManager.HasComponent<Game.Areas.Lot>(m_ControlPoints[k].m_OriginalEntity) ? m_InsertAreaNode : m_CreateAreaOrInsertAreaNode);
				return;
			}
			if (!base.EntityManager.HasComponent<Game.Areas.Node>(m_ControlPoints[0].m_OriginalEntity) || !math.any(m_ControlPoints[0].m_ElementIndex >= 0))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_CreateArea;
			}
			else
			{
				base.applyAction.shouldBeEnabled = false;
				base.applyActionOverride = null;
			}
			break;
		}
		case State.Create:
		{
			ref NativeList<ControlPoint> reference = ref m_ControlPoints;
			if (reference[reference.Length - 1].Equals(default(ControlPoint)))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = null;
				break;
			}
			ref NativeList<ControlPoint> reference2 = ref m_ControlPoints;
			ControlPoint controlPoint = reference2[reference2.Length - 1];
			ref float3 position = ref controlPoint.m_Position;
			ref NativeList<ControlPoint> reference3 = ref m_ControlPoints;
			if (!position.Equals(reference3[reference3.Length - 2].m_Position))
			{
				ref NativeList<ControlPoint> reference4 = ref m_ControlPoints;
				if (!reference4[reference4.Length - 1].m_Position.Equals(m_ControlPoints[0].m_Position))
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = (GetAllowApply() ? m_AddAreaNode : null);
					break;
				}
			}
			if (m_ControlPoints.Length >= 3)
			{
				ref NativeList<ControlPoint> reference5 = ref m_ControlPoints;
				if (reference5[reference5.Length - 1].m_Position.Equals(m_ControlPoints[0].m_Position))
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = (GetAllowApply() ? m_CompleteArea : null);
					break;
				}
			}
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.applyActionOverride = null;
			break;
		}
		case State.Modify:
		{
			for (int i = 0; i < m_MoveStartPositions.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<Area>(m_MoveStartPositions[i].m_OriginalEntity, out var component) || (component.m_Flags & AreaFlags.Complete) == 0 || !base.EntityManager.TryGetBuffer(m_MoveStartPositions[i].m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer))
				{
					continue;
				}
				for (int j = 0; j < buffer.Length; j++)
				{
					if (!buffer[j].m_Position.Equals(m_MoveStartPositions[i].m_Position) && buffer[j].m_Position.Equals(m_ControlPoints[0].m_Position))
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled;
						base.applyActionOverride = m_MergeAreaNode;
						return;
					}
					if (buffer[j].m_Position.Equals(m_MoveStartPositions[i].m_Position))
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled;
						base.applyActionOverride = m_MoveAreaNode;
						return;
					}
				}
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_InsertAreaNode;
				return;
			}
			base.applyAction.shouldBeEnabled = false;
			base.applyActionOverride = null;
			break;
		}
		case State.Remove:
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.applyActionOverride = null;
			break;
		default:
			base.applyAction.shouldBeEnabled = false;
			base.applyActionOverride = null;
			break;
		}
	}

	private void UpdateSecondaryApplyAction()
	{
		switch (state)
		{
		case State.Default:
		{
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < m_ControlPoints.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<Area>(m_ControlPoints[i].m_OriginalEntity, out var component))
				{
					continue;
				}
				if ((component.m_Flags & AreaFlags.Complete) == 0)
				{
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					return;
				}
				if (!base.EntityManager.TryGetBuffer(m_ControlPoints[i].m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer))
				{
					continue;
				}
				for (int j = 0; j < buffer.Length; j++)
				{
					if (buffer[j].m_Position.Equals(m_ControlPoints[i].m_Position))
					{
						if (buffer.Length > 3)
						{
							flag = true;
						}
						else if (!base.EntityManager.HasComponent<Owner>(m_ControlPoints[i].m_OriginalEntity))
						{
							flag2 = true;
						}
						break;
					}
				}
			}
			if (flag2)
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = m_DeleteArea;
			}
			else if (flag)
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = m_DeleteAreaNode;
			}
			else
			{
				base.secondaryApplyAction.shouldBeEnabled = false;
				base.secondaryApplyActionOverride = null;
			}
			break;
		}
		case State.Create:
			if (m_ControlPoints.Length > 1)
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = m_UndoAreaNode;
			}
			else
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = null;
			}
			break;
		case State.Remove:
			base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
			base.secondaryApplyActionOverride = m_DeleteAreaNode;
			break;
		default:
			base.secondaryApplyAction.shouldBeEnabled = false;
			base.secondaryApplyActionOverride = null;
			break;
		}
	}

	private void UpdateCancelAction()
	{
		if (state == State.Modify)
		{
			for (int i = 0; i < m_MoveStartPositions.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<Area>(m_MoveStartPositions[i].m_OriginalEntity, out var component) || (component.m_Flags & AreaFlags.Complete) == 0 || !base.EntityManager.TryGetBuffer(m_MoveStartPositions[i].m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer))
				{
					continue;
				}
				for (int j = 0; j < buffer.Length; j++)
				{
					if (!buffer[j].m_Position.Equals(m_MoveStartPositions[i].m_Position) && buffer[j].m_Position.Equals(m_ControlPoints[0].m_Position))
					{
						base.cancelAction.shouldBeEnabled = base.actionsEnabled;
						base.cancelActionOverride = m_DiscardMergeAreaNode;
						return;
					}
					if (buffer[j].m_Position.Equals(m_MoveStartPositions[i].m_Position))
					{
						base.cancelAction.shouldBeEnabled = base.actionsEnabled;
						base.cancelActionOverride = m_DiscardMoveAreaNode;
						return;
					}
				}
				base.cancelAction.shouldBeEnabled = base.actionsEnabled;
				base.cancelActionOverride = m_DiscardInsertAreaNode;
				return;
			}
			base.cancelAction.shouldBeEnabled = false;
			base.cancelActionOverride = null;
		}
		else
		{
			base.cancelAction.shouldBeEnabled = false;
			base.cancelActionOverride = null;
		}
	}

	public NativeList<ControlPoint> GetControlPoints(out NativeList<ControlPoint> moveStartPositions, out JobHandle dependencies)
	{
		moveStartPositions = m_MoveStartPositions;
		dependencies = base.Dependency;
		return m_ControlPoints;
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is AreaPrefab areaPrefab)
		{
			this.prefab = areaPrefab;
			return true;
		}
		return false;
	}

	public override void SetUnderground(bool underground)
	{
		this.underground = underground;
	}

	public override void ElevationUp()
	{
		underground = false;
	}

	public override void ElevationDown()
	{
		underground = true;
	}

	public override void ElevationScroll()
	{
		underground = !underground;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		if (prefab != null)
		{
			AreaGeometryData componentData = m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab);
			GetAvailableSnapMask(out var onMask, out var offMask);
			Snap actualSnap = ToolBaseSystem.GetActualSnap(selectedSnap, onMask, offMask);
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Areas;
			m_ToolRaycastSystem.areaTypeMask = AreaUtils.GetTypeMask(componentData.m_Type);
			if ((componentData.m_Flags & Game.Areas.GeometryFlags.OnWaterSurface) != 0)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Water;
			}
			if ((actualSnap & Snap.ObjectSurface) != Snap.None)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
				if (m_ToolSystem.actionMode.IsEditor())
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
				}
				if (underground)
				{
					m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
					m_ToolRaycastSystem.typeMask &= ~(TypeMask.Terrain | TypeMask.Water);
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.PartialSurface;
				}
			}
			if ((actualSnap & Snap.ExistingGeometry) == 0 && m_State != State.Default)
			{
				m_ToolRaycastSystem.typeMask &= ~TypeMask.Areas;
			}
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Areas;
			m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.UpgradeIsMain;
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (m_FocusChanged)
		{
			return inputDeps;
		}
		if (actualMode != m_LastMode)
		{
			m_ControlPoints.Clear();
			m_MoveStartPositions.Clear();
			m_LastRaycastPoint = default(ControlPoint);
			m_LastMode = actualMode;
			m_State = State.Default;
			m_Tooltip.value = Tooltip.None;
			m_AllowCreateArea = false;
		}
		bool flag = m_ForceCancel;
		m_ForceCancel = false;
		if (base.EntityManager.TryGetBuffer(recreate, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer))
		{
			m_State = State.Create;
			if (m_ControlPoints.Length < 3 && buffer.Length >= 2)
			{
				ref NativeList<ControlPoint> reference = ref m_ControlPoints;
				ControlPoint value = new ControlPoint
				{
					m_OriginalEntity = recreate,
					m_ElementIndex = new int2(0, -1),
					m_Position = buffer[0].m_Position,
					m_HitPosition = buffer[0].m_Position
				};
				reference.Add(in value);
				ref NativeList<ControlPoint> reference2 = ref m_ControlPoints;
				value = new ControlPoint
				{
					m_OriginalEntity = recreate,
					m_ElementIndex = new int2(1, -1),
					m_Position = buffer[1].m_Position,
					m_HitPosition = buffer[1].m_Position
				};
				reference2.Add(in value);
				ref NativeList<ControlPoint> reference3 = ref m_ControlPoints;
				value = new ControlPoint
				{
					m_ElementIndex = new int2(-1, -1),
					m_Position = math.lerp(buffer[0].m_Position, buffer[1].m_Position, 0.5f),
					m_HitPosition = math.lerp(buffer[0].m_Position, buffer[1].m_Position, 0.5f)
				};
				reference3.Add(in value);
			}
		}
		UpdateActions();
		if (prefab != null)
		{
			AreaGeometryData componentData = m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab);
			base.requireAreas = AreaUtils.GetTypeMask(componentData.m_Type);
			base.requireZones = componentData.m_Type == Game.Areas.AreaType.Lot;
			base.requireNet = Layer.None;
			if ((componentData.m_Flags & Game.Areas.GeometryFlags.PhysicalGeometry) != 0 && (componentData.m_Flags & Game.Areas.GeometryFlags.OnWaterSurface) != 0)
			{
				base.requireNet |= Layer.Waterway;
			}
			m_AllowCreateArea = (m_ToolSystem.actionMode.IsEditor() || componentData.m_Type != Game.Areas.AreaType.Lot) && (componentData.m_Type != Game.Areas.AreaType.Surface || (componentData.m_Flags & Game.Areas.GeometryFlags.ClipTerrain) != 0 || m_PrefabSystem.HasComponent<RenderedAreaData>(prefab));
			Entity entity = Entity.Null;
			if (base.EntityManager.TryGetComponent<Owner>(recreate, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Owner, out var component2))
			{
				entity = component2.m_Prefab;
			}
			else if (!m_ToolSystem.actionMode.IsEditor())
			{
				entity = m_PrefabSystem.GetEntity(prefab);
			}
			UpdateInfoview(entity);
			GetAvailableSnapMask(componentData, m_ToolSystem.actionMode.IsEditor(), out m_SnapOnMask, out m_SnapOffMask);
			allowUnderground = (ToolBaseSystem.GetActualSnap(selectedSnap, m_SnapOnMask, m_SnapOffMask) & Snap.ObjectSurface) != 0;
			base.requireUnderground = allowUnderground && underground;
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				switch (m_State)
				{
				case State.Default:
				case State.Create:
					if (m_ApplyBlocked)
					{
						if (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
						{
							m_ApplyBlocked = false;
						}
						return Update(inputDeps, fullUpdate: false);
					}
					if (base.applyAction.WasPressedThisFrame())
					{
						return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
					}
					if (base.secondaryApplyAction.WasPressedThisFrame())
					{
						return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
					}
					break;
				case State.Modify:
					if (base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = true;
						m_State = State.Default;
						return Update(inputDeps, fullUpdate: true);
					}
					if (base.applyAction.WasReleasedThisFrame())
					{
						return Apply(inputDeps);
					}
					break;
				case State.Remove:
					if (flag || base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = true;
						m_State = State.Default;
						return Update(inputDeps, fullUpdate: true);
					}
					if (base.secondaryApplyAction.WasReleasedThisFrame())
					{
						return Cancel(inputDeps);
					}
					break;
				}
				return Update(inputDeps, fullUpdate: false);
			}
		}
		else
		{
			base.requireAreas = AreaTypeMask.None;
			base.requireZones = false;
			base.requireNet = Layer.None;
			base.requireUnderground = false;
			m_AllowCreateArea = false;
			allowUnderground = false;
			UpdateInfoview(Entity.Null);
		}
		if (m_State == State.Modify && (!base.applyAction.enabled || base.applyAction.WasReleasedThisFrame()))
		{
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				return Cancel(inputDeps);
			}
			m_ControlPoints.Clear();
			m_State = State.Default;
		}
		else if (m_State == State.Remove && (!base.secondaryApplyAction.enabled || base.secondaryApplyAction.WasReleasedThisFrame()))
		{
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				return Apply(inputDeps);
			}
			m_ControlPoints.Clear();
			m_State = State.Default;
		}
		if (m_State != State.Default)
		{
			return inputDeps;
		}
		return Clear(inputDeps);
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		if (prefab != null)
		{
			GetAvailableSnapMask(m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab), m_ToolSystem.actionMode.IsEditor(), out onMask, out offMask);
		}
		else
		{
			base.GetAvailableSnapMask(out onMask, out offMask);
		}
	}

	private static void GetAvailableSnapMask(AreaGeometryData prefabAreaData, bool editorMode, out Snap onMask, out Snap offMask)
	{
		onMask = Snap.ExistingGeometry | Snap.StraightDirection;
		offMask = onMask;
		switch (prefabAreaData.m_Type)
		{
		case Game.Areas.AreaType.Lot:
			onMask |= Snap.NetSide | Snap.ObjectSide;
			offMask |= Snap.NetSide | Snap.ObjectSide;
			if (editorMode)
			{
				onMask |= Snap.LotGrid | Snap.AutoParent;
				offMask |= Snap.LotGrid | Snap.AutoParent;
			}
			break;
		case Game.Areas.AreaType.District:
			onMask |= Snap.NetMiddle;
			offMask |= Snap.NetMiddle;
			break;
		case Game.Areas.AreaType.Space:
			onMask |= Snap.NetSide | Snap.ObjectSide | Snap.ObjectSurface;
			offMask |= Snap.NetSide | Snap.ObjectSide | Snap.ObjectSurface;
			if (editorMode)
			{
				onMask |= Snap.LotGrid | Snap.AutoParent;
				offMask |= Snap.LotGrid | Snap.AutoParent;
			}
			break;
		case Game.Areas.AreaType.Surface:
			onMask |= Snap.NetSide | Snap.ObjectSide;
			offMask |= Snap.NetSide | Snap.ObjectSide;
			if (editorMode)
			{
				onMask |= Snap.LotGrid | Snap.AutoParent;
				offMask |= Snap.LotGrid | Snap.AutoParent;
			}
			break;
		case Game.Areas.AreaType.MapTile:
			break;
		}
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Default:
			if (actualMode == Mode.Generate)
			{
				return Update(inputDeps, fullUpdate: false);
			}
			if (GetAllowApply() && m_ControlPoints.Length > 0)
			{
				base.applyMode = ApplyMode.Clear;
				ControlPoint value = m_ControlPoints[0];
				if (base.EntityManager.HasComponent<Area>(value.m_OriginalEntity) && value.m_ElementIndex.x >= 0)
				{
					if (base.EntityManager.TryGetBuffer(value.m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer) && buffer.Length <= 3)
					{
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDeleteAreaSound);
					}
					else
					{
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
					}
					m_State = State.Remove;
					m_ControlPointsMoved = false;
					m_ForceCancel = singleFrameOnly;
					m_MoveStartPositions.Clear();
					m_MoveStartPositions.AddRange(m_ControlPoints.AsArray());
					m_ControlPoints.Clear();
					if (GetRaycastResult(out var controlPoint2))
					{
						m_LastRaycastPoint = controlPoint2;
						m_ControlPoints.Add(in controlPoint2);
						inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
						inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
					}
					else
					{
						m_ControlPoints.Add(in value);
					}
					return inputDeps;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Create:
		{
			m_ControlPoints.RemoveAtSwapBack(m_ControlPoints.Length - 1);
			base.applyMode = ApplyMode.Clear;
			if (m_ControlPoints.Length <= 1)
			{
				m_State = State.Default;
			}
			if (recreate != Entity.Null && m_ControlPoints.Length <= 2)
			{
				m_ToolSystem.activeTool = m_ObjectToolSystem;
				return inputDeps;
			}
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
			if (GetRaycastResult(out var controlPoint4))
			{
				m_LastRaycastPoint = controlPoint4;
				m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint4;
				inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
				inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			else if (m_ControlPoints.Length >= 2)
			{
				m_ControlPoints[m_ControlPoints.Length - 1] = m_ControlPoints[m_ControlPoints.Length - 2];
				inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			return inputDeps;
		}
		case State.Modify:
		{
			m_ControlPoints.Clear();
			base.applyMode = ApplyMode.Clear;
			m_State = State.Default;
			if (GetRaycastResult(out var controlPoint3))
			{
				m_LastRaycastPoint = controlPoint3;
				m_ControlPoints.Add(in controlPoint3);
				inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
				inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			return inputDeps;
		}
		case State.Remove:
		{
			NativeArray<Entity> applyTempAreas = default(NativeArray<Entity>);
			NativeArray<Entity> applyTempBuildings = default(NativeArray<Entity>);
			if (GetAllowApply() && !m_TempAreaQuery.IsEmptyIgnoreFilter)
			{
				base.applyMode = ApplyMode.Apply;
				applyTempAreas = m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
				applyTempBuildings = m_TempBuildingQuery.ToEntityArray(Allocator.TempJob);
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
			}
			m_State = State.Default;
			m_ControlPoints.Clear();
			if (GetRaycastResult(out var controlPoint))
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps, applyTempAreas);
				inputDeps = UpdateDefinitions(inputDeps, applyTempAreas, applyTempBuildings);
			}
			if (applyTempAreas.IsCreated)
			{
				applyTempAreas.Dispose(inputDeps);
			}
			if (applyTempBuildings.IsCreated)
			{
				applyTempBuildings.Dispose(inputDeps);
			}
			return inputDeps;
		}
		default:
			return Update(inputDeps, fullUpdate: false);
		}
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Default:
			if (actualMode == Mode.Generate)
			{
				if (GetAllowApply() && !m_TempAreaQuery.IsEmptyIgnoreFilter)
				{
					NativeArray<Entity> applyTempAreas = m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
					base.applyMode = ApplyMode.Apply;
					m_ControlPoints.Clear();
					if (GetRaycastResult(out var controlPoint2))
					{
						m_LastRaycastPoint = controlPoint2;
						m_ControlPoints.Add(in controlPoint2);
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
						inputDeps = SnapControlPoints(inputDeps, applyTempAreas);
						inputDeps = UpdateDefinitions(inputDeps, applyTempAreas, default(NativeArray<Entity>));
					}
					if (applyTempAreas.IsCreated)
					{
						applyTempAreas.Dispose(inputDeps);
					}
					return inputDeps;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			if (m_ControlPoints.Length > 0)
			{
				base.applyMode = ApplyMode.Clear;
				ControlPoint value = m_ControlPoints[0];
				if (base.EntityManager.HasComponent<Area>(value.m_OriginalEntity) && math.any(value.m_ElementIndex >= 0) && !singleFrameOnly)
				{
					m_State = State.Modify;
					m_ControlPointsMoved = false;
					m_MoveStartPositions.Clear();
					m_MoveStartPositions.AddRange(m_ControlPoints.AsArray());
					m_ControlPoints.Clear();
					if (GetRaycastResult(out var controlPoint3))
					{
						m_LastRaycastPoint = controlPoint3;
						m_ControlPoints.Add(in controlPoint3);
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolSelectPointSound);
						inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
						JobHandle.ScheduleBatchedJobs();
						inputDeps.Complete();
						ControlPoint other = m_ControlPoints[0];
						if (!m_MoveStartPositions[0].Equals(other))
						{
							float minNodeDistance = AreaUtils.GetMinNodeDistance(m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab));
							if (math.distance(m_MoveStartPositions[0].m_Position, other.m_Position) < minNodeDistance * 0.5f)
							{
								m_ControlPoints[0] = m_MoveStartPositions[0];
							}
							else
							{
								m_ControlPointsMoved = true;
							}
						}
						inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
					}
					else
					{
						m_ControlPoints.Add(in value);
					}
					return inputDeps;
				}
				if (GetAllowApply() && !value.Equals(default(ControlPoint)) && m_AllowCreateArea)
				{
					m_State = State.Create;
					m_MoveStartPositions.Clear();
					m_ControlPoints.Clear();
					m_ControlPoints.Add(in value);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
					if (GetRaycastResult(out var controlPoint4))
					{
						m_LastRaycastPoint = controlPoint4;
						m_ControlPoints.Add(in controlPoint4);
						inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
						inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
					}
					else
					{
						m_ControlPoints.Add(in value);
					}
					return inputDeps;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Create:
			if (!m_TempAreaQuery.IsEmptyIgnoreFilter)
			{
				if (!GetAllowApply())
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
				}
				else
				{
					AreaGeometryData componentData = m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab);
					float num = math.distance(m_ControlPoints[m_ControlPoints.Length - 2].m_Position, m_ControlPoints[m_ControlPoints.Length - 1].m_Position);
					float minNodeDistance2 = AreaUtils.GetMinNodeDistance(componentData);
					if (num >= minNodeDistance2)
					{
						bool flag = true;
						NativeArray<Area> nativeArray = m_TempAreaQuery.ToComponentDataArray<Area>(Allocator.TempJob);
						for (int i = 0; i < nativeArray.Length; i++)
						{
							flag &= (nativeArray[i].m_Flags & AreaFlags.Complete) != 0;
						}
						nativeArray.Dispose();
						NativeArray<Entity> applyTempAreas2 = default(NativeArray<Entity>);
						if (flag)
						{
							m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolFinishAreaSound);
							base.applyMode = ApplyMode.Apply;
							m_State = State.Default;
							m_ControlPoints.Clear();
							if (recreate != Entity.Null)
							{
								if (m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Move)
								{
									m_ToolSystem.activeTool = m_DefaultToolSystem;
								}
								else
								{
									m_ToolSystem.activeTool = m_ObjectToolSystem;
								}
								return inputDeps;
							}
							applyTempAreas2 = m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
						}
						else
						{
							m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
							base.applyMode = ApplyMode.Clear;
						}
						if (GetRaycastResult(out var controlPoint5))
						{
							m_LastRaycastPoint = controlPoint5;
							m_ControlPoints.Add(in controlPoint5);
							inputDeps = SnapControlPoints(inputDeps, applyTempAreas2);
							inputDeps = UpdateDefinitions(inputDeps, applyTempAreas2, default(NativeArray<Entity>));
						}
						if (applyTempAreas2.IsCreated)
						{
							applyTempAreas2.Dispose(inputDeps);
						}
						return inputDeps;
					}
				}
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Modify:
		{
			if (!m_ControlPointsMoved && GetAllowApply() && m_ControlPoints.Length > 0)
			{
				if (m_AllowCreateArea)
				{
					ControlPoint value2 = m_ControlPoints[0];
					base.applyMode = ApplyMode.Clear;
					m_State = State.Create;
					m_MoveStartPositions.Clear();
					m_ControlPoints.Clear();
					m_ControlPoints.Add(in value2);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
					if (GetRaycastResult(out var controlPoint6))
					{
						m_LastRaycastPoint = controlPoint6;
						m_ControlPoints.Add(in controlPoint6);
						inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
						inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
					}
					else
					{
						m_ControlPoints.Add(in value2);
					}
					return inputDeps;
				}
				base.applyMode = ApplyMode.Clear;
				m_State = State.Default;
				m_ControlPoints.Clear();
				if (GetRaycastResult(out var controlPoint7))
				{
					m_LastRaycastPoint = controlPoint7;
					m_ControlPoints.Add(in controlPoint7);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
					inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
					inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
				}
				return inputDeps;
			}
			NativeArray<Entity> applyTempAreas3 = default(NativeArray<Entity>);
			NativeArray<Entity> applyTempBuildings = default(NativeArray<Entity>);
			if (GetAllowApply() && !m_TempAreaQuery.IsEmptyIgnoreFilter)
			{
				base.applyMode = ApplyMode.Apply;
				applyTempAreas3 = m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
				applyTempBuildings = m_TempBuildingQuery.ToEntityArray(Allocator.TempJob);
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
			}
			m_State = State.Default;
			m_ControlPoints.Clear();
			if (GetRaycastResult(out var controlPoint8))
			{
				m_LastRaycastPoint = controlPoint8;
				m_ControlPoints.Add(in controlPoint8);
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
				inputDeps = SnapControlPoints(inputDeps, applyTempAreas3);
				inputDeps = UpdateDefinitions(inputDeps, applyTempAreas3, applyTempBuildings);
			}
			if (applyTempAreas3.IsCreated)
			{
				applyTempAreas3.Dispose(inputDeps);
			}
			if (applyTempBuildings.IsCreated)
			{
				applyTempBuildings.Dispose(inputDeps);
			}
			return inputDeps;
		}
		case State.Remove:
		{
			m_ControlPoints.Clear();
			base.applyMode = ApplyMode.Clear;
			m_State = State.Default;
			if (GetRaycastResult(out var controlPoint))
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
				inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
				inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			return inputDeps;
		}
		default:
			return Update(inputDeps, fullUpdate: false);
		}
	}

	private JobHandle Update(JobHandle inputDeps, bool fullUpdate)
	{
		if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
		{
			forceUpdate = forceUpdate || fullUpdate;
			if (m_ControlPoints.Length == 0)
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
				base.applyMode = ApplyMode.Clear;
				return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			if (m_LastRaycastPoint.Equals(controlPoint) && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			m_LastRaycastPoint = controlPoint;
			int index = math.select(0, m_ControlPoints.Length - 1, m_State == State.Create);
			ControlPoint value = m_ControlPoints[index];
			m_ControlPoints[index] = controlPoint;
			inputDeps = SnapControlPoints(inputDeps, default(NativeArray<Entity>));
			JobHandle.ScheduleBatchedJobs();
			inputDeps.Complete();
			ControlPoint other = m_ControlPoints[index];
			if (value.EqualsIgnoreHit(other))
			{
				base.applyMode = ApplyMode.None;
			}
			else
			{
				float minNodeDistance = AreaUtils.GetMinNodeDistance(m_PrefabSystem.GetComponentData<AreaGeometryData>(prefab));
				if (m_State == State.Modify && !m_ControlPointsMoved && math.distance(value.m_Position, other.m_Position) < minNodeDistance * 0.5f)
				{
					m_ControlPoints[index] = value;
					base.applyMode = ApplyMode.None;
				}
				else
				{
					m_ControlPointsMoved = true;
					base.applyMode = ApplyMode.Clear;
					inputDeps = UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
				}
			}
			return inputDeps;
		}
		if (m_LastRaycastPoint.Equals(controlPoint))
		{
			if (forceUpdate || fullUpdate)
			{
				base.applyMode = ApplyMode.Clear;
				return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		m_LastRaycastPoint = controlPoint;
		if (m_State == State.Default && m_ControlPoints.Length >= 1)
		{
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints.Clear();
			m_ControlPoints.Add(default(ControlPoint));
			return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
		}
		if (m_State == State.Modify && m_ControlPoints.Length >= 1)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[0] = m_MoveStartPositions[0];
			return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
		}
		if (m_State == State.Remove && m_ControlPoints.Length >= 1)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[0] = m_MoveStartPositions[0];
			return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
		}
		if (m_ControlPoints.Length >= 2)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[m_ControlPoints.Length - 1] = m_ControlPoints[m_ControlPoints.Length - 2];
			return UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
		}
		return inputDeps;
	}

	private JobHandle SnapControlPoints(JobHandle inputDeps, NativeArray<Entity> applyTempAreas)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		SnapJob jobData = new SnapJob
		{
			m_AllowCreateArea = m_AllowCreateArea,
			m_ControlPointsMoved = m_ControlPointsMoved,
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_Snap = GetActualSnap(),
			m_State = m_State,
			m_Prefab = m_PrefabSystem.GetEntity(prefab),
			m_ApplyTempAreas = applyTempAreas,
			m_MoveStartPositions = m_MoveStartPositions,
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
			m_ControlPoints = m_ControlPoints
		};
		inputDeps = JobHandle.CombineDependencies(inputDeps, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3));
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, inputDeps);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		return jobHandle;
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps, NativeArray<Entity> applyTempAreas, NativeArray<Entity> applyTempBuildings)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (prefab != null)
		{
			if (mode == Mode.Generate)
			{
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new RemoveMapTilesJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_CacheType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_ControlPoints = m_ControlPoints,
					m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
				}, m_MapTileQuery, inputDeps);
				m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			JobHandle jobHandle3 = IJobExtensions.Schedule(new CreateDefinitionsJob
			{
				m_AllowCreateArea = m_AllowCreateArea,
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_Mode = actualMode,
				m_State = m_State,
				m_Prefab = m_PrefabSystem.GetEntity(prefab),
				m_Recreate = recreate,
				m_ApplyTempAreas = applyTempAreas,
				m_ApplyTempBuildings = applyTempBuildings,
				m_MoveStartPositions = m_MoveStartPositions,
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ClearData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Clear_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_ControlPoints = m_ControlPoints,
				m_Tooltip = m_Tooltip,
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			}, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle3);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
		}
		return jobHandle;
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
	public AreaToolSystem()
	{
	}
}
