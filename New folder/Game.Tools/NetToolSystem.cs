using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Effects;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Settings;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class NetToolSystem : ToolBaseSystem
{
	public enum Mode
	{
		Straight,
		SimpleCurve,
		ComplexCurve,
		Continuous,
		Grid,
		Replace,
		Point
	}

	private class NetToolPreferences
	{
		public Mode m_Mode;

		public Snap m_Snap;

		public float m_Elevation;

		public float m_ElevationStep;

		public int m_ParallelCount;

		public float m_ParallelOffset;

		public bool m_Underground;

		public void Save(NetToolSystem netTool)
		{
			m_Mode = netTool.mode;
			m_Snap = netTool.selectedSnap;
			m_Elevation = netTool.elevation;
			m_ElevationStep = netTool.elevationStep;
			m_ParallelCount = netTool.parallelCount;
			m_ParallelOffset = netTool.parallelOffset;
			m_Underground = netTool.underground;
		}

		public void Load(NetToolSystem netTool)
		{
			netTool.mode = m_Mode;
			netTool.selectedSnap = m_Snap;
			netTool.elevation = m_Elevation;
			netTool.elevationStep = m_ElevationStep;
			netTool.parallelCount = m_ParallelCount;
			netTool.parallelOffset = m_ParallelOffset;
			netTool.underground = m_Underground;
		}
	}

	private enum State
	{
		Default,
		Applying,
		Cancelling
	}

	public struct UpgradeState
	{
		public bool m_IsUpgrading;

		public bool m_SkipFlags;

		public SubReplacementSide m_SubReplacementSide;

		public SubReplacementType m_SubReplacementType;

		public CompositionFlags m_OldFlags;

		public CompositionFlags m_AddFlags;

		public CompositionFlags m_RemoveFlags;

		public Entity m_SubReplacementPrefab;
	}

	public struct PathEdge
	{
		public Entity m_Entity;

		public bool m_Invert;

		public bool m_Upgrade;
	}

	public struct PathItem : ILessThan<PathItem>
	{
		public Entity m_Node;

		public Entity m_Edge;

		public float m_Cost;

		public bool LessThan(PathItem other)
		{
			return m_Cost < other.m_Cost;
		}
	}

	[BurstCompile]
	private struct UpdateStartEntityJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		public NativeReference<Entity> m_StartEntity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetCourse> nativeArray = chunk.GetNativeArray(ref m_NetCourseType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NetCourse netCourse = nativeArray[i];
				if ((netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsParallel)) == CoursePosFlags.IsFirst)
				{
					m_StartEntity.Value = netCourse.m_StartPosition.m_Entity;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct AppliedUpgrade
	{
		public Entity m_Entity;

		public Entity m_SubReplacementPrefab;

		public CompositionFlags m_Flags;

		public SubReplacementType m_SubReplacementType;

		public SubReplacementSide m_SubReplacementSide;
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		private struct ParentObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public ControlPoint m_BestSnapPosition;

			public Line3.Segment m_Line;

			public Bounds3 m_Bounds;

			public float m_Radius;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

			public ComponentLookup<AssetStampData> m_AssetStampData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || m_OwnerData.HasComponent(item))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[item];
				if (!m_BuildingData.HasComponent(prefabRef.m_Prefab) && !m_BuildingExtensionData.HasComponent(prefabRef.m_Prefab) && !m_AssetStampData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				Game.Objects.Transform transform = m_TransformData[item];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				float3 @float = MathUtils.Center(bounds.m_Bounds);
				Line3.Segment segment = m_Line - @float;
				int2 @int = default(int2);
				@int.x = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.x);
				@int.y = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.z);
				float2 size = (float2)@int * 8f;
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					Circle2 circle = new Circle2(size.x * 0.5f, (transform.m_Position - @float).xz);
					if (MathUtils.Intersect(circle, new Circle2(m_Radius, segment.a.xz)))
					{
						m_BestSnapPosition.m_OriginalEntity = item;
						return;
					}
					if (MathUtils.Intersect(circle, new Circle2(m_Radius, segment.b.xz)))
					{
						m_BestSnapPosition.m_OriginalEntity = item;
						return;
					}
					float num = MathUtils.Length(segment.xz);
					if (num > m_Radius)
					{
						float2 float2 = MathUtils.Right((segment.b.xz - segment.a.xz) * (m_Radius / num));
						if (MathUtils.Intersect(new Quad2(segment.a.xz + float2, segment.b.xz + float2, segment.b.xz - float2, segment.a.xz - float2), circle))
						{
							m_BestSnapPosition.m_OriginalEntity = item;
						}
					}
					return;
				}
				Quad2 xz = ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, size).xz;
				if (MathUtils.Intersect(xz, new Circle2(m_Radius, segment.a.xz)))
				{
					m_BestSnapPosition.m_OriginalEntity = item;
					return;
				}
				if (MathUtils.Intersect(xz, new Circle2(m_Radius, segment.b.xz)))
				{
					m_BestSnapPosition.m_OriginalEntity = item;
					return;
				}
				float num2 = MathUtils.Length(segment.xz);
				if (num2 > m_Radius)
				{
					float2 float3 = MathUtils.Right((segment.b.xz - segment.a.xz) * (m_Radius / num2));
					Quad2 quad = new Quad2(segment.a.xz + float3, segment.b.xz + float3, segment.b.xz - float3, segment.a.xz - float3);
					if (MathUtils.Intersect(xz, quad))
					{
						m_BestSnapPosition.m_OriginalEntity = item;
					}
				}
			}
		}

		private struct LotIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public float m_Radius;

			public float m_EdgeOffset;

			public float m_MaxDistance;

			public int m_CellWidth;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Game.Net.Node> m_NodeData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

			public ComponentLookup<AssetStampData> m_AssetStampData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || m_OwnerData.HasComponent(item))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[item];
				if (!m_BuildingData.HasComponent(prefabRef.m_Prefab) && !m_BuildingExtensionData.HasComponent(prefabRef.m_Prefab) && !m_AssetStampData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				Game.Objects.Transform transform = m_TransformData[item];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				float2 @float = math.normalizesafe(math.forward(transform.m_Rotation).xz, new float2(0f, 1f));
				float2 float2 = MathUtils.Right(@float);
				float2 x = m_ControlPoint.m_HitPosition.xz - transform.m_Position.xz;
				int2 @int = default(int2);
				@int.x = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.x);
				@int.y = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.z);
				float2 float3 = (float2)@int * 8f;
				float2 offset = math.select(0f, 4f, ((m_CellWidth + @int) & 1) != 0);
				float2 float4 = new float2(math.dot(x, float2), math.dot(x, @float));
				float2 float5 = MathUtils.Snap(float4, 8f, offset);
				if (m_EdgeOffset != 0f && (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0)
				{
					float5 = math.select(float5, float5 + math.select(m_EdgeOffset, 0f - m_EdgeOffset, float5 > 0f), math.abs(math.abs(float5) - float3 * 0.5f) < 4f);
				}
				bool2 @bool = math.abs(float4 - float5) < m_MaxDistance;
				if (!math.any(@bool))
				{
					return;
				}
				float5 = math.select(float4, float5, @bool);
				float2 float6 = transform.m_Position.xz + float2 * float5.x + @float * float5.y;
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					if (math.distance(float6, transform.m_Position.xz) > float3.x * 0.5f + m_Radius + 4f)
					{
						return;
					}
				}
				else if (math.any(math.abs(float5) > float3 * 0.5f + m_Radius + 4f))
				{
					return;
				}
				ControlPoint controlPoint = m_ControlPoint;
				if (!m_EdgeData.HasComponent(m_ControlPoint.m_OriginalEntity) && !m_NodeData.HasComponent(m_ControlPoint.m_OriginalEntity))
				{
					controlPoint.m_OriginalEntity = Entity.Null;
				}
				controlPoint.m_Direction = float2;
				controlPoint.m_Position.xz = float6;
				if (m_ControlPoint.m_OriginalEntity != item || m_ControlPoint.m_ElementIndex.x != -1)
				{
					controlPoint.m_Position.y = m_ControlPoint.m_HitPosition.y;
				}
				controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
				Line3 line = new Line3(controlPoint.m_Position, controlPoint.m_Position);
				Line3 line2 = new Line3(controlPoint.m_Position, controlPoint.m_Position);
				line.a.xz -= controlPoint.m_Direction * 8f;
				line.b.xz += controlPoint.m_Direction * 8f;
				line2.a.xz -= MathUtils.Right(controlPoint.m_Direction) * 8f;
				line2.b.xz += MathUtils.Right(controlPoint.m_Direction) * 8f;
				ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
				if (@bool.y)
				{
					ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden, 0f));
				}
				controlPoint.m_Direction = MathUtils.Right(controlPoint.m_Direction);
				if (@bool.x)
				{
					ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden, 0f));
				}
			}
		}

		private struct ZoneIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public float2 m_HitPosition;

			public float3 m_BestPosition;

			public float2 m_BestDirection;

			public float m_BestDistance;

			public ComponentLookup<Block> m_ZoneBlockData;

			public BufferLookup<Cell> m_ZoneCells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds))
				{
					return;
				}
				Block block = m_ZoneBlockData[entity];
				DynamicBuffer<Cell> dynamicBuffer = m_ZoneCells[entity];
				int2 cellIndex = math.clamp(ZoneUtils.GetCellIndex(block, m_HitPosition), 0, block.m_Size - 1);
				float3 cellPosition = ZoneUtils.GetCellPosition(block, cellIndex);
				float num = math.distance(cellPosition.xz, m_HitPosition);
				if (num >= m_BestDistance)
				{
					return;
				}
				if ((dynamicBuffer[cellIndex.x + cellIndex.y * block.m_Size.x].m_State & CellFlags.Visible) != CellFlags.None)
				{
					m_BestPosition = cellPosition;
					m_BestDirection = block.m_Direction;
					m_BestDistance = num;
					return;
				}
				cellIndex.y = 0;
				while (cellIndex.y < block.m_Size.y)
				{
					cellIndex.x = 0;
					while (cellIndex.x < block.m_Size.x)
					{
						if ((dynamicBuffer[cellIndex.x + cellIndex.y * block.m_Size.x].m_State & CellFlags.Visible) != CellFlags.None)
						{
							cellPosition = ZoneUtils.GetCellPosition(block, cellIndex);
							num = math.distance(cellPosition.xz, m_HitPosition);
							if (num < m_BestDistance)
							{
								m_BestPosition = cellPosition;
								m_BestDirection = block.m_Direction;
								m_BestDistance = num;
							}
						}
						cellIndex.x++;
					}
					cellIndex.y++;
				}
			}
		}

		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Snap m_Snap;

			public float m_MaxDistance;

			public float m_NetSnapOffset;

			public float m_ObjectSnapOffset;

			public bool m_SnapCellLength;

			public NetData m_NetData;

			public NetGeometryData m_NetGeometryData;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Game.Net.Node> m_NodeData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

			public ComponentLookup<NetData> m_PrefabNetData;

			public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

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
				if ((m_Snap & (Snap.ExistingGeometry | Snap.NearbyGeometry)) != Snap.None && m_OwnerData.HasComponent(entity))
				{
					Owner owner = m_OwnerData[entity];
					if (m_NodeData.HasComponent(owner.m_Owner))
					{
						SnapToNode(owner.m_Owner);
					}
				}
				if ((m_Snap & Snap.ObjectSide) != Snap.None)
				{
					SnapObjectSide(entity);
				}
			}

			private void SnapToNode(Entity entity)
			{
				if ((entity == m_ControlPoint.m_OriginalEntity && (m_Snap & Snap.ExistingGeometry) != Snap.None) || (m_ConnectedEdges.HasBuffer(entity) && m_ConnectedEdges[entity].Length > 0))
				{
					return;
				}
				Game.Net.Node node = m_NodeData[entity];
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (!m_PrefabNetData.HasComponent(prefabRef.m_Prefab) || !NetUtils.CanConnect(m_PrefabNetData[prefabRef.m_Prefab], m_NetData))
				{
					return;
				}
				ControlPoint snapPosition = m_ControlPoint;
				snapPosition.m_OriginalEntity = entity;
				snapPosition.m_Position = node.m_Position;
				snapPosition.m_Direction = math.mul(node.m_Rotation, new float3(0f, 0f, 1f)).xz;
				MathUtils.TryNormalize(ref snapPosition.m_Direction);
				float level = 1f;
				float num = math.distance(node.m_Position.xz, m_ControlPoint.m_HitPosition.xz);
				float num2 = m_NetGeometryData.m_DefaultWidth * 0.5f;
				if (m_PrefabGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					num2 += componentData.m_DefaultWidth * 0.5f;
				}
				if (!(num >= num2 + m_NetSnapOffset))
				{
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0 && num <= num2 && num <= num2)
					{
						level = 2f;
					}
					snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, 1f, m_ControlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, snapPosition);
				}
			}

			private void SnapObjectSide(Entity entity)
			{
				if (!m_TransformData.HasComponent(entity))
				{
					return;
				}
				Game.Objects.Transform transform = m_TransformData[entity];
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (!m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0)
				{
					bool flag = false;
					if (m_BuildingData.HasComponent(prefabRef.m_Prefab))
					{
						float2 @float = m_BuildingData[prefabRef.m_Prefab].m_LotSize;
						objectGeometryData.m_Bounds.min.xz = @float * -4f;
						objectGeometryData.m_Bounds.max.xz = @float * 4f;
						flag = m_SnapCellLength;
						objectGeometryData.m_Bounds.min.xz -= m_ObjectSnapOffset;
						objectGeometryData.m_Bounds.max.xz += m_ObjectSnapOffset;
						Quad3 quad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds);
						CheckLine(quad.ab, flag);
						CheckLine(quad.bc, flag);
						CheckLine(quad.cd, flag);
						CheckLine(quad.da, flag);
					}
				}
			}

			private void CheckLine(Line3.Segment line, bool snapCellLength)
			{
				if (MathUtils.Distance(line.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_MaxDistance)
				{
					if (snapCellLength)
					{
						t = MathUtils.Snap(t, 8f / MathUtils.Length(line.xz));
					}
					ControlPoint controlPoint = m_ControlPoint;
					controlPoint.m_Direction = math.normalizesafe(MathUtils.Tangent(line.xz));
					controlPoint.m_Position = MathUtils.Position(line, t);
					controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
					if (m_CurveData.HasComponent(m_ControlPoint.m_OriginalEntity))
					{
						MathUtils.Distance(m_CurveData[m_ControlPoint.m_OriginalEntity].m_Bezier.xz, controlPoint.m_Position.xz, out controlPoint.m_CurvePosition);
					}
					else if (!m_NodeData.HasComponent(m_ControlPoint.m_OriginalEntity))
					{
						controlPoint.m_OriginalEntity = Entity.Null;
					}
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
					ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Secondary, 1f));
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public bool m_EditorMode;

			public Bounds3 m_TotalBounds;

			public Bounds3 m_Bounds;

			public Snap m_Snap;

			public Entity m_ServiceUpgradeOwner;

			public float m_SnapOffset;

			public float m_SnapDistance;

			public float m_Elevation;

			public float m_GuideLength;

			public float m_LegSnapWidth;

			public Bounds1 m_HeightRange;

			public NetData m_NetData;

			public RoadData m_PrefabRoadData;

			public NetGeometryData m_NetGeometryData;

			public LocalConnectData m_LocalConnectData;

			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public NativeList<SnapLine> m_SnapLines;

			public TerrainHeightData m_TerrainHeightData;

			public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Net.Node> m_NodeData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<Road> m_RoadData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetData> m_PrefabNetData;

			public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public ComponentLookup<RoadComposition> m_RoadCompositionData;

			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			public BufferLookup<Game.Net.SubNet> m_SubNets;

			public BufferLookup<NetCompositionArea> m_PrefabCompositionAreas;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_TotalBounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_TotalBounds) && (!(entity == m_ControlPoint.m_OriginalEntity) || (m_Snap & Snap.ExistingGeometry) == 0) && (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || (m_Snap & (Snap.ExistingGeometry | Snap.NearbyGeometry)) == 0 || !HandleGeometry(entity)) && (m_Snap & Snap.GuideLines) != Snap.None)
				{
					HandleGuideLines(entity);
				}
			}

			public void HandleGuideLines(Entity entity)
			{
				if (!m_CurveData.HasComponent(entity))
				{
					return;
				}
				bool flag = false;
				bool flag2 = (m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0 && (m_PrefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0 && (m_Snap & Snap.CellLength) != 0;
				float defaultWidth = m_NetGeometryData.m_DefaultWidth;
				float num = defaultWidth;
				float num2 = m_NetGeometryData.m_DefaultWidth * 0.5f;
				bool flag3 = false;
				bool flag4 = false;
				PrefabRef prefabRef = m_PrefabRefData[entity];
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				NetGeometryData netGeometryData = default(NetGeometryData);
				if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				}
				if (!NetUtils.CanConnect(m_NetData, netData) || (!m_EditorMode && (netGeometryData.m_Flags & Game.Net.GeometryFlags.Marker) != 0))
				{
					return;
				}
				if (m_CompositionData.HasComponent(entity))
				{
					Composition composition = m_CompositionData[entity];
					num2 += m_PrefabCompositionData[composition.m_Edge].m_Width * 0.5f;
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0)
					{
						num = netGeometryData.m_DefaultWidth;
						if (m_RoadCompositionData.HasComponent(composition.m_Edge))
						{
							flag = (m_RoadCompositionData[composition.m_Edge].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0 && (m_Snap & Snap.CellLength) != 0;
							if (flag && m_RoadData.HasComponent(entity))
							{
								Road road = m_RoadData[entity];
								flag3 = (road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0;
								flag4 = (road.m_Flags & Game.Net.RoadFlags.EndHalfAligned) != 0;
							}
						}
					}
				}
				int cellWidth = ZoneUtils.GetCellWidth(defaultWidth);
				int cellWidth2 = ZoneUtils.GetCellWidth(num);
				int num3;
				float num4;
				float num5;
				if (flag2)
				{
					num3 = 1 + math.abs(cellWidth2 - cellWidth);
					num4 = (float)(num3 - 1) * -4f;
					num5 = 8f;
				}
				else
				{
					float num6 = math.abs(num - defaultWidth);
					if (num6 > 1.6f)
					{
						num3 = 3;
						num4 = num6 * -0.5f;
						num5 = num6 * 0.5f;
					}
					else
					{
						num3 = 1;
						num4 = 0f;
						num5 = 0f;
					}
				}
				float num7;
				float num8;
				float num9;
				float num10;
				if (flag)
				{
					num7 = math.select(0f, 4f, (((cellWidth ^ cellWidth2) & 1) != 0) ^ flag3);
					num8 = math.select(0f, 4f, (((cellWidth ^ cellWidth2) & 1) != 0) ^ flag4);
					num9 = math.select(num7, 0f - num7, cellWidth > cellWidth2);
					num10 = math.select(num8, 0f - num8, cellWidth > cellWidth2);
					num9 += 8f * (float)((math.max(2, cellWidth2) - math.max(2, cellWidth)) / 2);
					num10 += 8f * (float)((math.max(2, cellWidth2) - math.max(2, cellWidth)) / 2);
				}
				else
				{
					num7 = 0f;
					num8 = 0f;
					num9 = 0f;
					num10 = 0f;
				}
				Curve curve = m_CurveData[entity];
				Edge edge = m_EdgeData[entity];
				float2 value = -MathUtils.StartTangent(curve.m_Bezier).xz;
				float2 value2 = MathUtils.EndTangent(curve.m_Bezier).xz;
				bool flag5 = MathUtils.TryNormalize(ref value);
				bool flag6 = MathUtils.TryNormalize(ref value2);
				bool flag7 = flag5;
				if (flag5)
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[edge.m_Start];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity edge2 = dynamicBuffer[i].m_Edge;
						if (!(edge2 == entity))
						{
							Edge edge3 = m_EdgeData[edge2];
							if (edge3.m_Start == edge.m_Start || edge3.m_End == edge.m_Start)
							{
								flag7 = false;
								break;
							}
						}
					}
				}
				bool flag8 = flag6;
				if (flag6)
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer2 = m_ConnectedEdges[edge.m_End];
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Entity edge4 = dynamicBuffer2[j].m_Edge;
						if (!(edge4 == entity))
						{
							Edge edge5 = m_EdgeData[edge4];
							if (edge5.m_Start == edge.m_End || edge5.m_End == edge.m_End)
							{
								flag8 = false;
								break;
							}
						}
					}
				}
				if (!(flag5 || flag6))
				{
					return;
				}
				for (int k = 0; k < num3; k++)
				{
					if (flag5)
					{
						float3 a = curve.m_Bezier.a;
						a.xz += MathUtils.Left(value) * num4;
						Line3.Segment line = new Line3.Segment(a, a);
						line.b.xz += value * m_GuideLength;
						if (MathUtils.Distance(line.xz, m_ControlPoint.m_HitPosition.xz, out var t) < m_SnapDistance)
						{
							ControlPoint controlPoint = m_ControlPoint;
							controlPoint.m_OriginalEntity = Entity.Null;
							if ((m_Snap & Snap.CellLength) != Snap.None)
							{
								t = MathUtils.Snap(m_GuideLength * t, m_SnapDistance, num7) / m_GuideLength;
							}
							controlPoint.m_Position = MathUtils.Position(line, t);
							controlPoint.m_Direction = value;
							controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint.m_Position, controlPoint.m_Direction);
							ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint);
							ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.GuideLine, 0.1f));
						}
						if (k == 0 && flag7)
						{
							float3 @float = a;
							@float.xz += value * num9;
							Line3.Segment line2 = new Line3.Segment(@float, @float);
							line2.b.xz += MathUtils.Right(value) * m_GuideLength;
							if (MathUtils.Distance(line2.xz, m_ControlPoint.m_HitPosition.xz, out var t2) < m_SnapDistance)
							{
								ControlPoint controlPoint2 = m_ControlPoint;
								controlPoint2.m_OriginalEntity = Entity.Null;
								if ((m_Snap & Snap.CellLength) != Snap.None)
								{
									t2 = MathUtils.Snap(m_GuideLength * t2, m_SnapDistance) / m_GuideLength;
								}
								controlPoint2.m_Position = MathUtils.Position(line2, t2);
								controlPoint2.m_Direction = MathUtils.Right(value);
								controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
								ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint2);
								ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.GuideLine, 0.1f));
							}
						}
						if (k == num3 - 1 && flag7)
						{
							float3 float2 = a;
							float2.xz += value * num9;
							Line3.Segment line3 = new Line3.Segment(float2, float2);
							line3.b.xz += MathUtils.Left(value) * m_GuideLength;
							if (MathUtils.Distance(line3.xz, m_ControlPoint.m_HitPosition.xz, out var t3) < m_SnapDistance)
							{
								ControlPoint controlPoint3 = m_ControlPoint;
								controlPoint3.m_OriginalEntity = Entity.Null;
								if ((m_Snap & Snap.CellLength) != Snap.None)
								{
									t3 = MathUtils.Snap(m_GuideLength * t3, m_SnapDistance) / m_GuideLength;
								}
								controlPoint3.m_Position = MathUtils.Position(line3, t3);
								controlPoint3.m_Direction = MathUtils.Left(value);
								controlPoint3.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint3.m_Position, controlPoint3.m_Direction);
								ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint3);
								ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint3, NetUtils.StraightCurve(line3.a, line3.b), SnapLineFlags.GuideLine, 0.1f));
							}
						}
					}
					if (flag6)
					{
						float3 d = curve.m_Bezier.d;
						d.xz += MathUtils.Left(value2) * num4;
						Line3.Segment line4 = new Line3.Segment(d, d);
						line4.b.xz += value2 * m_GuideLength;
						if (MathUtils.Distance(line4.xz, m_ControlPoint.m_HitPosition.xz, out var t4) < m_SnapDistance)
						{
							ControlPoint controlPoint4 = m_ControlPoint;
							controlPoint4.m_OriginalEntity = Entity.Null;
							if ((m_Snap & Snap.CellLength) != Snap.None)
							{
								t4 = MathUtils.Snap(m_GuideLength * t4, m_SnapDistance, num8) / m_GuideLength;
							}
							controlPoint4.m_Position = MathUtils.Position(line4, t4);
							controlPoint4.m_Direction = value2;
							controlPoint4.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint4.m_Position, controlPoint4.m_Direction);
							ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint4);
							ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint4, NetUtils.StraightCurve(line4.a, line4.b), SnapLineFlags.GuideLine, 0.1f));
						}
						if (k == 0 && flag8)
						{
							float3 float3 = d;
							float3.xz += value2 * num10;
							Line3.Segment line5 = new Line3.Segment(float3, float3);
							line5.b.xz += MathUtils.Right(value2) * m_GuideLength;
							if (MathUtils.Distance(line5.xz, m_ControlPoint.m_HitPosition.xz, out var t5) < m_SnapDistance)
							{
								ControlPoint controlPoint5 = m_ControlPoint;
								controlPoint5.m_OriginalEntity = Entity.Null;
								if ((m_Snap & Snap.CellLength) != Snap.None)
								{
									t5 = MathUtils.Snap(m_GuideLength * t5, m_SnapDistance) / m_GuideLength;
								}
								controlPoint5.m_Position = MathUtils.Position(line5, t5);
								controlPoint5.m_Direction = MathUtils.Right(value2);
								controlPoint5.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint5.m_Position, controlPoint5.m_Direction);
								ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint5);
								ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint5, NetUtils.StraightCurve(line5.a, line5.b), SnapLineFlags.GuideLine, 0.1f));
							}
						}
						if (k == num3 - 1 && flag8)
						{
							float3 float4 = d;
							float4.xz += value2 * num10;
							Line3.Segment line6 = new Line3.Segment(float4, float4);
							line6.b.xz += MathUtils.Left(value2) * m_GuideLength;
							if (MathUtils.Distance(line6.xz, m_ControlPoint.m_HitPosition.xz, out var t6) < m_SnapDistance)
							{
								ControlPoint controlPoint6 = m_ControlPoint;
								controlPoint6.m_OriginalEntity = Entity.Null;
								if ((m_Snap & Snap.CellLength) != Snap.None)
								{
									t6 = MathUtils.Snap(m_GuideLength * t6, m_SnapDistance) / m_GuideLength;
								}
								controlPoint6.m_Position = MathUtils.Position(line6, t6);
								controlPoint6.m_Direction = MathUtils.Left(value2);
								controlPoint6.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0.1f, m_ControlPoint.m_HitPosition, controlPoint6.m_Position, controlPoint6.m_Direction);
								ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint6);
								ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint6, NetUtils.StraightCurve(line6.a, line6.b), SnapLineFlags.GuideLine, 0.1f));
							}
						}
					}
					num4 += num5;
				}
			}

			public bool HandleGeometry(Entity entity)
			{
				PrefabRef prefabRef = m_PrefabRefData[entity];
				ControlPoint controlPoint = m_ControlPoint;
				controlPoint.m_OriginalEntity = entity;
				float num = m_NetGeometryData.m_DefaultWidth * 0.5f + m_SnapOffset;
				if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					if ((m_NetGeometryData.m_Flags & ~netGeometryData.m_Flags & Game.Net.GeometryFlags.StandingNodes) != 0)
					{
						num = m_LegSnapWidth * 0.5f + m_SnapOffset;
					}
				}
				if (m_ConnectedEdges.HasBuffer(entity))
				{
					Game.Net.Node node = m_NodeData[entity];
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[entity];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Edge edge = m_EdgeData[dynamicBuffer[i].m_Edge];
						if (edge.m_Start == entity || edge.m_End == entity)
						{
							return false;
						}
					}
					if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						num += m_PrefabGeometryData[prefabRef.m_Prefab].m_DefaultWidth * 0.5f;
					}
					if (math.distance(node.m_Position.xz, m_ControlPoint.m_HitPosition.xz) >= num)
					{
						return false;
					}
					float y = node.m_Position.y;
					return HandleGeometry(controlPoint, y, prefabRef, ignoreHeightDistance: false);
				}
				if (m_CurveData.HasComponent(entity))
				{
					Curve curve = m_CurveData[entity];
					if (m_CompositionData.HasComponent(entity))
					{
						Composition composition = m_CompositionData[entity];
						num += m_PrefabCompositionData[composition.m_Edge].m_Width * 0.5f;
					}
					if (MathUtils.Distance(curve.m_Bezier.xz, m_ControlPoint.m_HitPosition.xz, out controlPoint.m_CurvePosition) >= num)
					{
						return false;
					}
					float y2 = MathUtils.Position(curve.m_Bezier, controlPoint.m_CurvePosition).y;
					return HandleGeometry(controlPoint, y2, prefabRef, ignoreHeightDistance: false);
				}
				return false;
			}

			public bool HandleGeometry(ControlPoint controlPoint, float snapHeight, PrefabRef prefabRef, bool ignoreHeightDistance)
			{
				if (!m_PrefabNetData.HasComponent(prefabRef.m_Prefab))
				{
					return false;
				}
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				bool snapAdded = false;
				bool flag = true;
				bool allowEdgeSnap = true;
				float num = ((!(m_Elevation < 0f)) ? (WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, controlPoint.m_HitPosition) + m_Elevation) : (TerrainUtils.SampleHeight(ref m_TerrainHeightData, controlPoint.m_HitPosition) + m_Elevation));
				if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					Bounds1 bounds = m_NetGeometryData.m_DefaultHeightRange + num;
					Bounds1 bounds2 = netGeometryData.m_DefaultHeightRange + snapHeight;
					if (!MathUtils.Intersect(bounds, bounds2))
					{
						flag = false;
						allowEdgeSnap = (netGeometryData.m_Flags & Game.Net.GeometryFlags.NoEdgeConnection) == 0;
					}
				}
				if (flag && !NetUtils.CanConnect(netData, m_NetData))
				{
					return snapAdded;
				}
				if ((m_NetData.m_ConnectLayers & ~netData.m_RequiredLayers & Layer.LaneEditor) != Layer.None)
				{
					return snapAdded;
				}
				float position = snapHeight - num;
				if (!ignoreHeightDistance && !MathUtils.Intersect(m_HeightRange, position))
				{
					return snapAdded;
				}
				if (m_NodeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					if (m_ConnectedEdges.HasBuffer(controlPoint.m_OriginalEntity))
					{
						DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[controlPoint.m_OriginalEntity];
						if (dynamicBuffer.Length != 0)
						{
							for (int i = 0; i < dynamicBuffer.Length; i++)
							{
								Entity edge = dynamicBuffer[i].m_Edge;
								Edge edge2 = m_EdgeData[edge];
								if (!(edge2.m_Start != controlPoint.m_OriginalEntity) || !(edge2.m_End != controlPoint.m_OriginalEntity))
								{
									HandleCurve(controlPoint, edge, allowEdgeSnap, ref snapAdded);
								}
							}
							return snapAdded;
						}
					}
					ControlPoint snapPosition = controlPoint;
					Game.Net.Node node = m_NodeData[controlPoint.m_OriginalEntity];
					snapPosition.m_Position = node.m_Position;
					snapPosition.m_Direction = math.mul(node.m_Rotation, new float3(0f, 0f, 1f)).xz;
					MathUtils.TryNormalize(ref snapPosition.m_Direction);
					float level = 1f;
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
					{
						float num2 = m_NetGeometryData.m_DefaultWidth * 0.5f;
						if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
						{
							num2 += m_PrefabGeometryData[prefabRef.m_Prefab].m_DefaultWidth * 0.5f;
						}
						if (math.distance(node.m_Position.xz, controlPoint.m_HitPosition.xz) <= num2)
						{
							level = 2f;
						}
					}
					snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, snapPosition);
					snapAdded = true;
				}
				else if (m_CurveData.HasComponent(controlPoint.m_OriginalEntity))
				{
					HandleCurve(controlPoint, controlPoint.m_OriginalEntity, allowEdgeSnap, ref snapAdded);
				}
				return snapAdded;
			}

			private bool SnapSegmentAreas(ControlPoint controlPoint, NetCompositionData prefabCompositionData, DynamicBuffer<NetCompositionArea> areas, Segment segment, ref bool snapAdded)
			{
				bool result = false;
				for (int i = 0; i < areas.Length; i++)
				{
					NetCompositionArea netCompositionArea = areas[i];
					if ((netCompositionArea.m_Flags & NetAreaFlags.Buildable) == 0)
					{
						continue;
					}
					float num = netCompositionArea.m_Width * 0.51f;
					if (!(m_LegSnapWidth * 0.5f >= num))
					{
						result = true;
						Bezier4x3 curve = MathUtils.Lerp(segment.m_Left, segment.m_Right, netCompositionArea.m_Position.x / prefabCompositionData.m_Width + 0.5f);
						float t;
						float num2 = MathUtils.Distance(curve.xz, controlPoint.m_HitPosition.xz, out t);
						ControlPoint controlPoint2 = controlPoint;
						controlPoint2.m_Position = MathUtils.Position(curve, t);
						controlPoint2.m_Direction = math.normalizesafe(MathUtils.Tangent(curve, t).xz);
						if ((netCompositionArea.m_Flags & NetAreaFlags.Invert) != 0)
						{
							controlPoint2.m_Direction = -controlPoint2.m_Direction;
						}
						float3 @float = MathUtils.Position(MathUtils.Lerp(segment.m_Left, segment.m_Right, netCompositionArea.m_SnapPosition.x / prefabCompositionData.m_Width + 0.5f), t);
						float maxLength = math.max(0f, math.min(netCompositionArea.m_Width * 0.5f, math.abs(netCompositionArea.m_SnapPosition.x - netCompositionArea.m_Position.x) + netCompositionArea.m_SnapWidth * 0.5f) - m_LegSnapWidth * 0.5f);
						controlPoint2.m_Position.xz += MathUtils.ClampLength(@float.xz - controlPoint2.m_Position.xz, maxLength);
						controlPoint2.m_Position.y += netCompositionArea.m_Position.y;
						float level = 1f;
						if (num2 <= prefabCompositionData.m_Width * 0.5f - math.abs(netCompositionArea.m_Position.x) + m_LegSnapWidth * 0.5f)
						{
							level = 2f;
						}
						controlPoint2.m_Rotation = ToolUtils.CalculateRotation(controlPoint2.m_Direction);
						controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, 1f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
						ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint2);
						ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, curve, GetSnapLineFlags(m_NetGeometryData.m_Flags), 1f));
						snapAdded = true;
					}
				}
				return result;
			}

			private void HandleCurve(ControlPoint controlPoint, Entity curveEntity, bool allowEdgeSnap, ref bool snapAdded)
			{
				bool flag = false;
				bool flag2 = (m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0 && (m_PrefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0 && (m_Snap & Snap.CellLength) != 0;
				float defaultWidth = m_NetGeometryData.m_DefaultWidth;
				float num = defaultWidth;
				float num2 = m_NetGeometryData.m_DefaultWidth * 0.5f;
				bool2 @bool = false;
				PrefabRef prefabRef = m_PrefabRefData[curveEntity];
				NetGeometryData netGeometryData = default(NetGeometryData);
				if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				}
				if (m_CompositionData.HasComponent(curveEntity))
				{
					Composition composition = m_CompositionData[curveEntity];
					NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
					num2 += prefabCompositionData.m_Width * 0.5f;
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0)
					{
						num = netGeometryData.m_DefaultWidth;
						if (m_RoadCompositionData.HasComponent(composition.m_Edge))
						{
							flag = (m_RoadCompositionData[composition.m_Edge].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0 && (m_Snap & Snap.CellLength) != 0;
							if (flag && m_RoadData.HasComponent(curveEntity))
							{
								Road road = m_RoadData[curveEntity];
								@bool.x = (road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0;
								@bool.y = (road.m_Flags & Game.Net.RoadFlags.EndHalfAligned) != 0;
							}
						}
					}
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.SnapToNetAreas) != 0)
					{
						DynamicBuffer<NetCompositionArea> areas = m_PrefabCompositionAreas[composition.m_Edge];
						EdgeGeometry edgeGeometry = m_EdgeGeometryData[curveEntity];
						if (SnapSegmentAreas(controlPoint, prefabCompositionData, areas, edgeGeometry.m_Start, ref snapAdded) | SnapSegmentAreas(controlPoint, prefabCompositionData, areas, edgeGeometry.m_End, ref snapAdded))
						{
							return;
						}
					}
				}
				int num3;
				float num4;
				float num5;
				if (flag2)
				{
					int cellWidth = ZoneUtils.GetCellWidth(defaultWidth);
					int cellWidth2 = ZoneUtils.GetCellWidth(num);
					num3 = 1 + math.abs(cellWidth2 - cellWidth);
					num4 = (float)(num3 - 1) * -4f;
					num5 = 8f;
				}
				else
				{
					float num6 = math.abs(num - defaultWidth);
					if (num6 > 1.6f)
					{
						num3 = 3;
						num4 = num6 * -0.5f;
						num5 = num6 * 0.5f;
					}
					else
					{
						num3 = 1;
						num4 = 0f;
						num5 = 0f;
					}
				}
				float num7;
				if (flag)
				{
					int cellWidth3 = ZoneUtils.GetCellWidth(defaultWidth);
					int cellWidth4 = ZoneUtils.GetCellWidth(num);
					num7 = math.select(0f, 4f, (((cellWidth3 ^ cellWidth4) & 1) != 0) ^ @bool.x);
				}
				else
				{
					num7 = 0f;
				}
				Curve curve = m_CurveData[curveEntity];
				if (!m_EditorMode && m_OwnerData.TryGetComponent(curveEntity, out var componentData) && componentData.m_Owner != m_ServiceUpgradeOwner && (!m_EdgeData.HasComponent(componentData.m_Owner) || (m_OwnerData.TryGetComponent(componentData.m_Owner, out var componentData2) && componentData2.m_Owner != m_ServiceUpgradeOwner)))
				{
					allowEdgeSnap = false;
				}
				float2 @float = math.normalizesafe(MathUtils.Left(MathUtils.StartTangent(curve.m_Bezier).xz));
				float2 float2 = math.normalizesafe(MathUtils.Left(curve.m_Bezier.c.xz - curve.m_Bezier.b.xz));
				float2 float3 = math.normalizesafe(MathUtils.Left(MathUtils.EndTangent(curve.m_Bezier).xz));
				bool flag3 = math.dot(@float, float2) > 0.9998477f && math.dot(float2, float3) > 0.9998477f;
				for (int i = 0; i < num3; i++)
				{
					Bezier4x3 curve2;
					if (math.abs(num4) < 0.08f)
					{
						curve2 = curve.m_Bezier;
					}
					else if (flag3)
					{
						curve2 = curve.m_Bezier;
						curve2.a.xz += @float * num4;
						curve2.b.xz += math.lerp(@float, float3, 1f / 3f) * num4;
						curve2.c.xz += math.lerp(@float, float3, 2f / 3f) * num4;
						curve2.d.xz += float3 * num4;
					}
					else
					{
						curve2 = NetUtils.OffsetCurveLeftSmooth(curve.m_Bezier, num4);
					}
					float t;
					float num8 = (((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0) ? MathUtils.Distance(curve2.xz, controlPoint.m_HitPosition.xz, out t) : NetUtils.ExtendedDistance(curve2.xz, controlPoint.m_HitPosition.xz, out t));
					ControlPoint controlPoint2 = controlPoint;
					if ((m_Snap & Snap.CellLength) != Snap.None)
					{
						float num9 = MathUtils.Length(curve2.xz);
						num9 += math.select(0f, 4f, @bool.x != @bool.y);
						num9 = math.fmod(num9 + 0.1f, 8f) * 0.5f;
						float value = NetUtils.ExtendedLength(curve2.xz, t);
						value = MathUtils.Snap(value, m_SnapDistance, num7 + num9);
						t = NetUtils.ExtendedClampLength(curve2.xz, value);
						if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
						{
							t = math.saturate(t);
						}
						controlPoint2.m_CurvePosition = t;
					}
					else
					{
						t = math.saturate(t);
						if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
						{
							float value2 = NetUtils.ExtendedLength(curve2.xz, t);
							value2 = MathUtils.Snap(value2, 4f);
							controlPoint2.m_CurvePosition = NetUtils.ExtendedClampLength(curve2.xz, value2);
						}
						else
						{
							if (t >= 0.5f)
							{
								if (math.distance(curve2.d.xz, controlPoint.m_HitPosition.xz) < m_SnapOffset)
								{
									t = 1f;
								}
							}
							else if (math.distance(curve2.a.xz, controlPoint.m_HitPosition.xz) < m_SnapOffset)
							{
								t = 0f;
							}
							controlPoint2.m_CurvePosition = t;
						}
					}
					if (!allowEdgeSnap && t > 0f && t < 1f)
					{
						if (t >= 0.5f)
						{
							if (math.distance(curve2.d.xz, controlPoint.m_HitPosition.xz) >= num2 + m_SnapOffset)
							{
								continue;
							}
							t = 1f;
							controlPoint2.m_CurvePosition = 1f;
						}
						else
						{
							if (math.distance(curve2.a.xz, controlPoint.m_HitPosition.xz) >= num2 + m_SnapOffset)
							{
								continue;
							}
							t = 0f;
							controlPoint2.m_CurvePosition = 0f;
						}
					}
					NetUtils.ExtendedPositionAndTangent(curve2, t, out controlPoint2.m_Position, out var tangent);
					controlPoint2.m_Direction = tangent.xz;
					MathUtils.TryNormalize(ref controlPoint2.m_Direction);
					float level = 1f;
					if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0 && num8 <= num2)
					{
						level = 2f;
					}
					controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, 1f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
					ToolUtils.AddSnapPosition(ref m_BestSnapPosition, controlPoint2);
					ToolUtils.AddSnapLine(ref m_BestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, curve2, GetSnapLineFlags(m_NetGeometryData.m_Flags), 1f));
					snapAdded = true;
					num4 += num5;
				}
			}
		}

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public float m_Elevation;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_LanePrefab;

		[ReadOnly]
		public Entity m_ServiceUpgradeOwner;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_RemoveUpgrade;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Block> m_ZoneBlockData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_PrefabRoadData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<RoadComposition> m_RoadCompositionData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_AssetStampData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Cell> m_ZoneCells;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> m_PrefabCompositionAreas;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> m_AuxiliaryNets;

		[ReadOnly]
		public BufferLookup<FixedNetElement> m_FixedNetElements;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

		public NativeList<ControlPoint> m_ControlPoints;

		public NativeList<SnapLine> m_SnapLines;

		public NativeList<UpgradeState> m_UpgradeStates;

		public NativeReference<Entity> m_StartEntity;

		public NativeReference<Entity> m_LastSnappedEntity;

		public NativeReference<int> m_LastControlPointsAngle;

		public NativeReference<AppliedUpgrade> m_AppliedUpgrade;

		public SourceUpdateData m_SourceUpdateData;

		public void Execute()
		{
			RoadData prefabRoadData = default(RoadData);
			NetGeometryData netGeometryData = default(NetGeometryData);
			LocalConnectData localConnectData = default(LocalConnectData);
			PlaceableNetData placeableNetData = default(PlaceableNetData);
			NetData prefabNetData = m_PrefabNetData[m_Prefab];
			if (m_PrefabRoadData.HasComponent(m_Prefab))
			{
				prefabRoadData = m_PrefabRoadData[m_Prefab];
			}
			if (m_PrefabGeometryData.HasComponent(m_Prefab))
			{
				netGeometryData = m_PrefabGeometryData[m_Prefab];
			}
			if (m_LocalConnectData.HasComponent(m_Prefab))
			{
				localConnectData = m_LocalConnectData[m_Prefab];
			}
			if (m_PlaceableData.HasComponent(m_Prefab))
			{
				placeableNetData = m_PlaceableData[m_Prefab];
			}
			placeableNetData.m_SnapDistance = math.max(placeableNetData.m_SnapDistance, 1f);
			if (m_LanePrefab != Entity.Null)
			{
				netGeometryData.m_Flags |= Game.Net.GeometryFlags.StrictNodes;
			}
			m_SnapLines.Clear();
			m_UpgradeStates.Clear();
			if (m_Mode == Mode.Replace || m_ControlPoints.Length <= 1)
			{
				m_StartEntity.Value = default(Entity);
			}
			if (m_Mode == Mode.Replace)
			{
				ControlPoint startPoint = m_ControlPoints[0];
				ControlPoint endPoint = m_ControlPoints[m_ControlPoints.Length - 1];
				m_ControlPoints.Clear();
				SubReplacement subReplacement = default(SubReplacement);
				if ((placeableNetData.m_SetUpgradeFlags.m_General & CompositionFlags.General.SecondaryMiddleBeautification) != 0 || (placeableNetData.m_SetUpgradeFlags.m_Left & CompositionFlags.Side.SecondaryBeautification) != 0 || (placeableNetData.m_SetUpgradeFlags.m_Right & CompositionFlags.Side.SecondaryBeautification) != 0)
				{
					subReplacement.m_Type = SubReplacementType.Tree;
				}
				NativeList<PathEdge> path = new NativeList<PathEdge>(Allocator.Temp);
				CreatePath(startPoint, endPoint, path, prefabNetData, placeableNetData, ref m_EdgeData, ref m_NodeData, ref m_CurveData, ref m_PrefabRefData, ref m_PrefabNetData, ref m_ConnectedEdges);
				AddControlPoints(m_ControlPoints, m_UpgradeStates, m_AppliedUpgrade, startPoint, endPoint, path, m_Snap, m_RemoveUpgrade, m_LeftHandTraffic, m_EditorMode, netGeometryData, prefabRoadData, placeableNetData, subReplacement, ref m_OwnerData, ref m_BorderDistrictData, ref m_DistrictData, ref m_EdgeData, ref m_NodeData, ref m_CurveData, ref m_CompositionData, ref m_UpgradedData, ref m_EdgeGeometryData, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabGeometryData, ref m_PrefabCompositionData, ref m_RoadCompositionData, ref m_ConnectedEdges, ref m_SubReplacements, ref m_AuxiliaryNets, ref m_FixedNetElements);
				return;
			}
			ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 1];
			ControlPoint bestSnapPosition = controlPoint;
			bestSnapPosition.m_Position = bestSnapPosition.m_HitPosition;
			bestSnapPosition.m_OriginalEntity = Entity.Null;
			HandleWorldSize(ref bestSnapPosition, controlPoint, netGeometryData);
			if ((m_Snap & Snap.ObjectSurface) != Snap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity) && m_SubNets.HasBuffer(controlPoint.m_OriginalEntity))
			{
				bestSnapPosition.m_OriginalEntity = controlPoint.m_OriginalEntity;
			}
			float waterSurfaceHeight = float.MinValue;
			if ((m_Snap & Snap.Shoreline) != Snap.None)
			{
				SnapShoreline(ref bestSnapPosition, controlPoint, netGeometryData, ref waterSurfaceHeight);
			}
			if ((m_Snap & (Snap.CellLength | Snap.StraightDirection)) != Snap.None && m_ControlPoints.Length >= 2)
			{
				HandleControlPoints(ref bestSnapPosition, controlPoint, netGeometryData, placeableNetData);
			}
			if ((m_Snap & (Snap.ExistingGeometry | Snap.NearbyGeometry | Snap.GuideLines)) != Snap.None)
			{
				HandleExistingGeometry(ref bestSnapPosition, controlPoint, prefabRoadData, netGeometryData, prefabNetData, localConnectData, placeableNetData);
			}
			if ((m_Snap & (Snap.ExistingGeometry | Snap.ObjectSide | Snap.NearbyGeometry)) != Snap.None)
			{
				HandleExistingObjects(ref bestSnapPosition, controlPoint, prefabRoadData, netGeometryData, prefabNetData, placeableNetData);
			}
			if ((m_Snap & Snap.LotGrid) != Snap.None)
			{
				HandleLotGrid(ref bestSnapPosition, controlPoint, prefabRoadData, netGeometryData, prefabNetData, placeableNetData);
			}
			if ((m_Snap & Snap.ZoneGrid) != Snap.None)
			{
				HandleZoneGrid(ref bestSnapPosition, controlPoint, prefabRoadData, netGeometryData, prefabNetData);
			}
			ControlPoint snapTargetControlPoint = bestSnapPosition;
			if (m_Mode == Mode.Grid)
			{
				AdjustMiddlePoint(ref bestSnapPosition, netGeometryData);
				AdjustControlPointHeight(ref bestSnapPosition, controlPoint, netGeometryData, placeableNetData, waterSurfaceHeight);
			}
			else
			{
				AdjustControlPointHeight(ref bestSnapPosition, controlPoint, netGeometryData, placeableNetData, waterSurfaceHeight);
				if (m_Mode == Mode.Continuous)
				{
					AdjustMiddlePoint(ref bestSnapPosition, netGeometryData);
				}
			}
			if (m_EditorMode)
			{
				if ((m_Snap & Snap.AutoParent) == 0)
				{
					bestSnapPosition.m_OriginalEntity = Entity.Null;
				}
				else if (bestSnapPosition.m_OriginalEntity == Entity.Null)
				{
					FindParent(ref bestSnapPosition, netGeometryData);
				}
			}
			if ((m_Snap & Snap.ObjectSurface) != Snap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity) && m_SubNets.HasBuffer(controlPoint.m_OriginalEntity) && bestSnapPosition.m_OriginalEntity == controlPoint.m_OriginalEntity)
			{
				bestSnapPosition.m_ElementIndex = controlPoint.m_ElementIndex;
			}
			else
			{
				bestSnapPosition.m_ElementIndex = -1;
			}
			if (CanPlaySnapSound(ref snapTargetControlPoint, ref controlPoint))
			{
				m_SourceUpdateData.AddSnap();
			}
			m_ControlPoints[m_ControlPoints.Length - 1] = bestSnapPosition;
			m_LastSnappedEntity.Value = snapTargetControlPoint.m_OriginalEntity;
		}

		private bool CanPlaySnapSound(ref ControlPoint snapTargetControlPoint, ref ControlPoint controlPoint)
		{
			Layer layer = Layer.None;
			if (m_PrefabNetData.HasComponent(m_Prefab))
			{
				layer = m_PrefabNetData[m_Prefab].m_RequiredLayers;
				int num = 0;
				int value = m_LastControlPointsAngle.Value;
				if ((m_Snap & Snap.StraightDirection) != Snap.None && m_ControlPoints.Length >= 2 && snapTargetControlPoint.m_OriginalEntity == Entity.Null)
				{
					ControlPoint controlPoint2;
					if (m_Mode == Mode.Continuous && m_ControlPoints.Length == 3)
					{
						controlPoint2 = m_ControlPoints[0];
						controlPoint2.m_Direction = m_ControlPoints[1].m_Direction;
					}
					else
					{
						controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 2];
					}
					Line3.Segment segment = new Line3.Segment(controlPoint2.m_Position, snapTargetControlPoint.m_Position);
					float num2 = MathUtils.Length(segment.xz);
					if (num2 > 1f)
					{
						float2 direction = controlPoint2.m_Direction;
						float2 x = (segment.b.xz - segment.a.xz) / num2;
						float x2 = math.dot(x, direction);
						num = (int)math.round(math.degrees(math.atan2(x.x * direction.y - direction.x * x.y, x2)) + 180f);
						if (num % 180 == 0 && m_StartEntity.Value == Entity.Null)
						{
							num = 0;
						}
					}
				}
				m_LastControlPointsAngle.Value = num;
				if (snapTargetControlPoint.m_OriginalEntity != Entity.Null)
				{
					if (m_LastSnappedEntity.Value == Entity.Null && snapTargetControlPoint.m_OriginalEntity != controlPoint.m_OriginalEntity)
					{
						if (m_RoadData.HasComponent(snapTargetControlPoint.m_OriginalEntity))
						{
							return true;
						}
						Layer layer2 = Layer.None;
						if (!m_PrefabRefData.TryGetComponent(snapTargetControlPoint.m_OriginalEntity, out var componentData) || !m_LocalConnectData.TryGetComponent(componentData, out var componentData2))
						{
							return false;
						}
						layer2 = componentData2.m_Layers;
						layer2 = (Layer)((uint)layer2 & 0xFFFFFFFEu);
						if ((layer2 & layer) != Layer.None)
						{
							return true;
						}
						Layer layer3 = Layer.WaterPipe | Layer.SewagePipe;
						if ((layer2 & layer3) != Layer.None && (layer & layer3) != Layer.None)
						{
							return true;
						}
					}
				}
				else if (value % 360 != 0 && value != num && num % 360 != 0 && (num % 90 == 0 || (num % 45 == 0 && m_Mode == Mode.Continuous)))
				{
					return true;
				}
				return false;
			}
			return false;
		}

		private void HandleWorldSize(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, NetGeometryData prefabGeometryData)
		{
			Bounds3 bounds = TerrainUtils.GetBounds(ref m_TerrainHeightData);
			float num = prefabGeometryData.m_DefaultWidth * 0.5f;
			bool2 @bool = false;
			float2 trueValue = 0f;
			if (controlPoint.m_HitPosition.x < bounds.min.x + num)
			{
				@bool.x = true;
				trueValue.x = bounds.min.x - num;
			}
			else if (controlPoint.m_HitPosition.x > bounds.max.x - num)
			{
				@bool.x = true;
				trueValue.x = bounds.max.x + num;
			}
			if (controlPoint.m_HitPosition.z < bounds.min.z + num)
			{
				@bool.y = true;
				trueValue.y = bounds.min.z - num;
			}
			else if (controlPoint.m_HitPosition.z > bounds.max.z - num)
			{
				@bool.y = true;
				trueValue.y = bounds.max.z + num;
			}
			if (math.any(@bool))
			{
				ControlPoint controlPoint2 = controlPoint;
				controlPoint2.m_OriginalEntity = Entity.Null;
				controlPoint2.m_Direction = new float2(0f, 1f);
				controlPoint2.m_Position.xz = math.select(controlPoint.m_HitPosition.xz, trueValue, @bool);
				controlPoint2.m_Position.y = controlPoint.m_HitPosition.y;
				controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(2f, 1f, 0f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
				ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
				if (@bool.x)
				{
					Line3 line = new Line3(controlPoint2.m_Position, controlPoint2.m_Position)
					{
						a = 
						{
							z = bounds.min.z
						},
						b = 
						{
							z = bounds.max.z
						}
					};
					ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden, 0f));
				}
				if (@bool.y)
				{
					controlPoint2.m_Direction = new float2(1f, 0f);
					Line3 line2 = new Line3(controlPoint2.m_Position, controlPoint2.m_Position)
					{
						a = 
						{
							x = bounds.min.x
						},
						b = 
						{
							x = bounds.max.x
						}
					};
					ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden, 0f));
				}
			}
		}

		private void FindParent(ref ControlPoint bestSnapPosition, NetGeometryData prefabGeometryData)
		{
			Line3.Segment line = ((m_ControlPoints.Length < 2) ? new Line3.Segment(bestSnapPosition.m_Position, bestSnapPosition.m_Position) : new Line3.Segment(m_ControlPoints[m_ControlPoints.Length - 2].m_Position, bestSnapPosition.m_Position));
			float num = math.max(0.01f, prefabGeometryData.m_DefaultWidth * 0.5f - 0.5f);
			ParentObjectIterator iterator = new ParentObjectIterator
			{
				m_BestSnapPosition = bestSnapPosition,
				m_Line = line,
				m_Bounds = MathUtils.Expand(MathUtils.Bounds(line), num + 0.4f),
				m_Radius = num,
				m_OwnerData = m_OwnerData,
				m_TransformData = m_TransformData,
				m_BuildingData = m_BuildingData,
				m_BuildingExtensionData = m_BuildingExtensionData,
				m_AssetStampData = m_AssetStampData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabObjectGeometryData = m_ObjectGeometryData
			};
			m_ObjectSearchTree.Iterate(ref iterator);
			bestSnapPosition = iterator.m_BestSnapPosition;
		}

		private void HandleLotGrid(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, RoadData prefabRoadData, NetGeometryData prefabGeometryData, NetData prefabNetData, PlaceableNetData placeableNetData)
		{
			int cellWidth = ZoneUtils.GetCellWidth(prefabGeometryData.m_DefaultWidth);
			float num = (float)(cellWidth + 1) * (placeableNetData.m_SnapDistance * 0.5f) * 1.4142135f;
			float edgeOffset = 0f;
			float maxDistance = placeableNetData.m_SnapDistance * 0.5f + 0.1f;
			if (m_PrefabLaneData.HasComponent(m_LanePrefab))
			{
				edgeOffset = m_PrefabLaneData[m_LanePrefab].m_Width * 0.5f;
			}
			LotIterator iterator = new LotIterator
			{
				m_Bounds = new Bounds2(controlPoint.m_HitPosition.xz - num, controlPoint.m_HitPosition.xz + num),
				m_Radius = prefabGeometryData.m_DefaultWidth * 0.5f,
				m_EdgeOffset = edgeOffset,
				m_MaxDistance = maxDistance,
				m_CellWidth = cellWidth,
				m_ControlPoint = controlPoint,
				m_BestSnapPosition = bestSnapPosition,
				m_SnapLines = m_SnapLines,
				m_OwnerData = m_OwnerData,
				m_EdgeData = m_EdgeData,
				m_NodeData = m_NodeData,
				m_TransformData = m_TransformData,
				m_PrefabRefData = m_PrefabRefData,
				m_BuildingData = m_BuildingData,
				m_BuildingExtensionData = m_BuildingExtensionData,
				m_AssetStampData = m_AssetStampData,
				m_PrefabObjectGeometryData = m_ObjectGeometryData
			};
			m_ObjectSearchTree.Iterate(ref iterator);
			bestSnapPosition = iterator.m_BestSnapPosition;
		}

		private void SnapShoreline(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, NetGeometryData prefabGeometryData, ref float waterSurfaceHeight)
		{
			float num = prefabGeometryData.m_DefaultWidth * 2f + 10f;
			int2 x = (int2)math.floor(WaterUtils.ToSurfaceSpace(ref m_WaterSurfaceData, controlPoint.m_HitPosition - num).xz);
			int2 x2 = (int2)math.ceil(WaterUtils.ToSurfaceSpace(ref m_WaterSurfaceData, controlPoint.m_HitPosition + num).xz);
			x = math.max(x, default(int2));
			x2 = math.min(x2, m_WaterSurfaceData.resolution.xz - 1);
			float3 @float = default(float3);
			float3 float2 = default(float3);
			float2 float3 = default(float2);
			for (int i = x.y; i <= x2.y; i++)
			{
				for (int j = x.x; j <= x2.x; j++)
				{
					float3 worldPosition = WaterUtils.GetWorldPosition(ref m_WaterSurfaceData, new int2(j, i));
					if (worldPosition.y > 0.2f)
					{
						float num2 = TerrainUtils.SampleHeight(ref m_TerrainHeightData, worldPosition) + worldPosition.y;
						float num3 = math.max(0f, num * num - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
						worldPosition.y = (worldPosition.y - 0.2f) * num3;
						worldPosition.xz *= worldPosition.y;
						float2 += worldPosition;
						num2 *= num3;
						float3 += new float2(num2, num3);
					}
					else if (worldPosition.y < 0.2f)
					{
						float num4 = math.max(0f, num * num - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
						worldPosition.y = (0.2f - worldPosition.y) * num4;
						worldPosition.xz *= worldPosition.y;
						@float += worldPosition;
					}
				}
			}
			if (@float.y != 0f && float2.y != 0f && float3.y != 0f)
			{
				@float /= @float.y;
				float2 /= float2.y;
				float3 value = new float3
				{
					xz = @float.xz - float2.xz
				};
				if (MathUtils.TryNormalize(ref value))
				{
					float num5 = 8f / num;
					waterSurfaceHeight = float3.x / float3.y;
					ControlPoint controlPoint2 = controlPoint;
					controlPoint2.m_Position.xz = math.lerp(float2.xz, @float.xz, 0.5f);
					controlPoint2.m_Position.y = waterSurfaceHeight;
					controlPoint2.m_Position += value;
					controlPoint2.m_Direction = value.xz;
					controlPoint2.m_Rotation = ToolUtils.CalculateRotation(controlPoint2.m_Direction);
					controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition * num5, controlPoint2.m_Position * num5, controlPoint2.m_Direction);
					controlPoint2.m_OriginalEntity = Entity.Null;
					float3 startPos = controlPoint2.m_Position + value * num;
					float3 endPos = controlPoint2.m_Position - value * num;
					ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
					ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(startPos, endPos), SnapLineFlags.Hidden, 0f));
				}
			}
		}

		private void AdjustControlPointHeight(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData, float waterSurfaceHeight)
		{
			float y = bestSnapPosition.m_Position.y;
			if ((m_Snap & Snap.ObjectSurface) == 0 || !m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
			{
				if (m_Elevation < 0f)
				{
					bestSnapPosition.m_Position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, bestSnapPosition.m_Position) + m_Elevation;
				}
				else
				{
					bestSnapPosition.m_Position.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, bestSnapPosition.m_Position, out float waterDepth);
					float num = m_Elevation;
					if (waterDepth >= 0.2f && (placeableNetData.m_PlacementFlags & (Game.Net.PlacementFlags.OnGround | Game.Net.PlacementFlags.Floating)) == Game.Net.PlacementFlags.OnGround)
					{
						num = math.max(m_Elevation, placeableNetData.m_MinWaterElevation);
						bestSnapPosition.m_Elevation = math.max(bestSnapPosition.m_Elevation, placeableNetData.m_MinWaterElevation);
					}
					else if ((m_Snap & Snap.Shoreline) != Snap.None)
					{
						float num2 = math.max(m_Elevation, placeableNetData.m_MinWaterElevation);
						if (waterSurfaceHeight + num2 > bestSnapPosition.m_Position.y)
						{
							num = num2;
							bestSnapPosition.m_Elevation = math.max(bestSnapPosition.m_Elevation, placeableNetData.m_MinWaterElevation);
							bestSnapPosition.m_Position.y = waterSurfaceHeight;
						}
					}
					bestSnapPosition.m_Position.y += num;
				}
			}
			else
			{
				bestSnapPosition.m_Position.y = controlPoint.m_HitPosition.y;
			}
			Bounds1 bounds = prefabGeometryData.m_DefaultHeightRange + bestSnapPosition.m_Position.y;
			if (m_PrefabRefData.HasComponent(controlPoint.m_OriginalEntity))
			{
				PrefabRef prefabRef = m_PrefabRefData[controlPoint.m_OriginalEntity];
				if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					Bounds1 bounds2 = m_PrefabGeometryData[prefabRef.m_Prefab].m_DefaultHeightRange + controlPoint.m_Position.y;
					if (bounds2.max > bounds.min)
					{
						bounds.max = math.max(bounds.max, bounds2.max);
						if (bestSnapPosition.m_OriginalEntity == Entity.Null)
						{
							bestSnapPosition.m_OriginalEntity = controlPoint.m_OriginalEntity;
						}
					}
				}
			}
			if (!m_PrefabRefData.HasComponent(bestSnapPosition.m_OriginalEntity))
			{
				return;
			}
			PrefabRef prefabRef2 = m_PrefabRefData[bestSnapPosition.m_OriginalEntity];
			if (!m_PrefabGeometryData.HasComponent(prefabRef2.m_Prefab))
			{
				return;
			}
			NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef2.m_Prefab];
			Bounds1 bounds3 = netGeometryData.m_DefaultHeightRange + y;
			if (MathUtils.Intersect(bounds, bounds3))
			{
				if (((prefabGeometryData.m_MergeLayers ^ netGeometryData.m_MergeLayers) & Layer.Waterway) == 0)
				{
					bestSnapPosition.m_Elevation += y - bestSnapPosition.m_Position.y;
					bestSnapPosition.m_Position.y = y;
					bestSnapPosition.m_Elevation = MathUtils.Clamp(bestSnapPosition.m_Elevation, placeableNetData.m_ElevationRange);
				}
			}
			else
			{
				bestSnapPosition.m_OriginalEntity = Entity.Null;
			}
		}

		private void AdjustMiddlePoint(ref ControlPoint bestSnapPosition, NetGeometryData netGeometryData)
		{
			float2 @float = (((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) == 0) ? (netGeometryData.m_DefaultWidth * new float2(16f, 8f)) : ((float)ZoneUtils.GetCellWidth(netGeometryData.m_DefaultWidth) * 8f + new float2(192f, 96f)));
			float2 float2 = @float * 11f;
			if (m_ControlPoints.Length == 2)
			{
				ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 2];
				float2 value = bestSnapPosition.m_Position.xz - controlPoint.m_Position.xz;
				if (MathUtils.TryNormalize(ref value))
				{
					bestSnapPosition.m_Direction = value;
				}
				if (m_Mode == Mode.Grid && math.distance(controlPoint.m_Position.xz, bestSnapPosition.m_Position.xz) > float2.x)
				{
					bestSnapPosition.m_Position.xz = controlPoint.m_Position.xz + value * float2.x;
					bestSnapPosition.m_OriginalEntity = Entity.Null;
				}
			}
			else
			{
				if (m_ControlPoints.Length != 3)
				{
					return;
				}
				ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 3];
				ControlPoint value2 = m_ControlPoints[m_ControlPoints.Length - 2];
				if (m_Mode == Mode.Grid)
				{
					float2 x = bestSnapPosition.m_Position.xz - controlPoint2.m_Position.xz;
					float2 float3 = new float2(math.dot(x, value2.m_Direction), math.dot(x, MathUtils.Right(value2.m_Direction)));
					bool2 @bool = math.abs(float3) > float2;
					float3 = math.select(float3, math.select(float2, -float2, float3 < 0f), @bool);
					value2.m_Position = controlPoint2.m_Position;
					value2.m_Position.xz += value2.m_Direction * float3.x;
					if (math.any(@bool))
					{
						bestSnapPosition.m_Position.xz = value2.m_Position.xz + MathUtils.Right(value2.m_Direction) * float3.y;
						bestSnapPosition.m_OriginalEntity = Entity.Null;
					}
				}
				else
				{
					value2.m_Elevation = (controlPoint2.m_Elevation + bestSnapPosition.m_Elevation) * 0.5f;
					float2 float4 = bestSnapPosition.m_Position.xz - controlPoint2.m_Position.xz;
					float2 direction = value2.m_Direction;
					float2 value3 = float4;
					if (MathUtils.TryNormalize(ref value3))
					{
						float num = math.dot(direction, value3);
						if (num >= 0.70710677f)
						{
							float2 float5 = math.lerp(controlPoint2.m_Position.xz, bestSnapPosition.m_Position.xz, 0.5f);
							Line2 line = new Line2(controlPoint2.m_Position.xz, controlPoint2.m_Position.xz + direction);
							Line2 line2 = new Line2(float5, float5 + MathUtils.Right(value3));
							if (MathUtils.Intersect(line, line2, out var t))
							{
								value2.m_Position = controlPoint2.m_Position;
								value2.m_Position.xz += direction * t.x;
								float2 value4 = bestSnapPosition.m_Position.xz - value2.m_Position.xz;
								if (MathUtils.TryNormalize(ref value4))
								{
									bestSnapPosition.m_Direction = value4;
								}
							}
						}
						else if (num >= 0f)
						{
							float2 float6 = math.lerp(controlPoint2.m_Position.xz, bestSnapPosition.m_Position.xz, 0.5f);
							Line2 line3 = new Line2(controlPoint2.m_Position.xz, controlPoint2.m_Position.xz + MathUtils.Right(direction));
							Line2 line4 = new Line2(float6, float6 + MathUtils.Right(value3));
							if (MathUtils.Intersect(line3, line4, out var t2))
							{
								value2.m_Position = controlPoint2.m_Position;
								value2.m_Position.xz += direction * math.abs(t2.x);
								float2 value5 = bestSnapPosition.m_Position.xz - MathUtils.Position(line3, t2.x);
								if (MathUtils.TryNormalize(ref value5))
								{
									bestSnapPosition.m_Direction = math.select(MathUtils.Right(value5), MathUtils.Left(value5), math.dot(MathUtils.Right(direction), value3) < 0f);
								}
							}
						}
						else
						{
							value2.m_Position = controlPoint2.m_Position;
							value2.m_Position.xz += direction * math.abs(math.dot(float4, MathUtils.Right(direction)) * 0.5f);
							bestSnapPosition.m_Direction = -value2.m_Direction;
						}
					}
					else
					{
						value2.m_Position = controlPoint2.m_Position;
					}
				}
				if (value2.m_Elevation < 0f)
				{
					value2.m_Position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, value2.m_Position) + value2.m_Elevation;
				}
				else
				{
					value2.m_Position.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, value2.m_Position) + value2.m_Elevation;
				}
				m_ControlPoints[m_ControlPoints.Length - 2] = value2;
			}
		}

		private void HandleControlPoints(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData)
		{
			ControlPoint controlPoint2 = controlPoint;
			controlPoint2.m_OriginalEntity = Entity.Null;
			controlPoint2.m_Position = controlPoint.m_HitPosition;
			float num = placeableNetData.m_SnapDistance;
			if (m_Mode == Mode.Grid && m_ControlPoints.Length == 3)
			{
				if ((m_Snap & Snap.CellLength) != Snap.None)
				{
					float2 xz = m_ControlPoints[0].m_Position.xz;
					float2 direction = m_ControlPoints[1].m_Direction;
					float2 @float = MathUtils.Right(direction);
					float2 x = controlPoint.m_HitPosition.xz - xz;
					x = new float2(math.dot(x, direction), math.dot(x, @float));
					x = MathUtils.Snap(x, num);
					xz += x.x * direction + x.y * @float;
					controlPoint2.m_Direction = direction;
					controlPoint2.m_Position.xz = xz;
					controlPoint2.m_Position.y = controlPoint.m_HitPosition.y;
					controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
					Line3 line = new Line3(controlPoint2.m_Position, controlPoint2.m_Position);
					Line3 line2 = new Line3(controlPoint2.m_Position, controlPoint2.m_Position);
					line.a.xz -= controlPoint2.m_Direction * 8f;
					line.b.xz += controlPoint2.m_Direction * 8f;
					line2.a.xz -= MathUtils.Right(controlPoint2.m_Direction) * 8f;
					line2.b.xz += MathUtils.Right(controlPoint2.m_Direction) * 8f;
					ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
					ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden, 0f));
					controlPoint2.m_Direction = MathUtils.Right(controlPoint2.m_Direction);
					ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden, 0f));
				}
				return;
			}
			ControlPoint prev;
			if (m_Mode == Mode.Continuous && m_ControlPoints.Length == 3)
			{
				prev = m_ControlPoints[0];
				prev.m_OriginalEntity = Entity.Null;
				prev.m_Direction = m_ControlPoints[1].m_Direction;
			}
			else
			{
				prev = m_ControlPoints[m_ControlPoints.Length - 2];
				if (prev.m_Direction.Equals(default(float2)) && m_ControlPoints.Length >= 3)
				{
					prev.m_Direction = math.normalizesafe(prev.m_Position.xz - m_ControlPoints[m_ControlPoints.Length - 3].m_Position.xz);
				}
			}
			float3 value = controlPoint.m_HitPosition - prev.m_Position;
			value = MathUtils.Normalize(value, value.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			bool flag = false;
			bool flag2 = false;
			if ((m_Snap & Snap.StraightDirection) != Snap.None)
			{
				float bestDirectionDistance = float.MaxValue;
				if (prev.m_OriginalEntity != Entity.Null)
				{
					HandleStartDirection(prev.m_OriginalEntity, prev, controlPoint, placeableNetData, ref bestDirectionDistance, ref controlPoint2.m_Position, ref value);
				}
				if (m_StartEntity.Value != Entity.Null && m_StartEntity.Value != prev.m_OriginalEntity && m_ControlPoints.Length == 2)
				{
					HandleStartDirection(m_StartEntity.Value, prev, controlPoint, placeableNetData, ref bestDirectionDistance, ref controlPoint2.m_Position, ref value);
				}
				if (!prev.m_Direction.Equals(default(float2)) && bestDirectionDistance == float.MaxValue)
				{
					ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref value, controlPoint.m_HitPosition, prev.m_Position, new float3(prev.m_Direction.x, 0f, prev.m_Direction.y), placeableNetData.m_SnapDistance);
					if (bestDirectionDistance >= placeableNetData.m_SnapDistance && m_Mode == Mode.Continuous && m_ControlPoints.Length == 3)
					{
						float2 float2 = MathUtils.RotateLeft(prev.m_Direction, MathF.PI / 4f);
						ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref value, controlPoint.m_HitPosition, prev.m_Position, new float3(float2.x, 0f, float2.y), placeableNetData.m_SnapDistance);
						float2 = MathUtils.RotateRight(prev.m_Direction, MathF.PI / 4f);
						ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref value, controlPoint.m_HitPosition, prev.m_Position, new float3(float2.x, 0f, float2.y), placeableNetData.m_SnapDistance);
						num *= 1.4142135f;
					}
				}
				flag = bestDirectionDistance < placeableNetData.m_SnapDistance;
				flag2 = bestDirectionDistance < placeableNetData.m_SnapDistance;
			}
			if ((m_Snap & Snap.CellLength) != Snap.None && (m_Mode != Mode.Continuous || (m_ControlPoints.Length == 3 && flag2)))
			{
				float value2 = math.distance(prev.m_Position, controlPoint2.m_Position);
				controlPoint2.m_Position = prev.m_Position + value * MathUtils.Snap(value2, num);
				flag = true;
			}
			controlPoint2.m_Direction = value.xz;
			controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
			if (flag)
			{
				ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
			}
			if (flag2)
			{
				float3 position = controlPoint2.m_Position;
				float3 endPos = position;
				endPos.xz += controlPoint2.m_Direction;
				ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(position, endPos), GetSnapLineFlags(prefabGeometryData.m_Flags) | SnapLineFlags.Hidden, 0f));
			}
		}

		private void HandleStartDirection(Entity startEntity, ControlPoint prev, ControlPoint controlPoint, PlaceableNetData placeableNetData, ref float bestDirectionDistance, ref float3 snapPosition, ref float3 snapDirection)
		{
			if (m_ConnectedEdges.HasBuffer(startEntity))
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[startEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (!(edge2.m_Start != startEntity) || !(edge2.m_End != startEntity))
					{
						Curve curve = m_CurveData[edge];
						float3 value = ((edge2.m_Start == startEntity) ? MathUtils.StartTangent(curve.m_Bezier) : MathUtils.EndTangent(curve.m_Bezier));
						value = MathUtils.Normalize(value, value.xz);
						value.y = math.clamp(value.y, -1f, 1f);
						ToolUtils.DirectionSnap(ref bestDirectionDistance, ref snapPosition, ref snapDirection, controlPoint.m_HitPosition, prev.m_Position, value, placeableNetData.m_SnapDistance);
					}
				}
			}
			else if (m_CurveData.HasComponent(startEntity))
			{
				float3 value2 = MathUtils.Tangent(m_CurveData[startEntity].m_Bezier, prev.m_CurvePosition);
				value2 = MathUtils.Normalize(value2, value2.xz);
				value2.y = math.clamp(value2.y, -1f, 1f);
				ToolUtils.DirectionSnap(ref bestDirectionDistance, ref snapPosition, ref snapDirection, controlPoint.m_HitPosition, prev.m_Position, value2, placeableNetData.m_SnapDistance);
			}
			else if (m_TransformData.HasComponent(startEntity))
			{
				float3 value3 = math.forward(m_TransformData[startEntity].m_Rotation);
				value3 = MathUtils.Normalize(value3, value3.xz);
				value3.y = math.clamp(value3.y, -1f, 1f);
				ToolUtils.DirectionSnap(ref bestDirectionDistance, ref snapPosition, ref snapDirection, controlPoint.m_HitPosition, prev.m_Position, value3, placeableNetData.m_SnapDistance);
			}
		}

		private void HandleZoneGrid(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, RoadData prefabRoadData, NetGeometryData prefabGeometryData, NetData prefabNetData)
		{
			int cellWidth = ZoneUtils.GetCellWidth(prefabGeometryData.m_DefaultWidth);
			float num = (float)(cellWidth + 1) * 4f * 1.4142135f;
			float offset = math.select(0f, 4f, (cellWidth & 1) == 0);
			ZoneIterator iterator = new ZoneIterator
			{
				m_Bounds = new Bounds2(controlPoint.m_HitPosition.xz - num, controlPoint.m_HitPosition.xz + num),
				m_HitPosition = controlPoint.m_HitPosition.xz,
				m_BestDistance = num,
				m_ZoneBlockData = m_ZoneBlockData,
				m_ZoneCells = m_ZoneCells
			};
			m_ZoneSearchTree.Iterate(ref iterator);
			if (iterator.m_BestDistance < num)
			{
				float2 x = controlPoint.m_HitPosition.xz - iterator.m_BestPosition.xz;
				float2 @float = MathUtils.Right(iterator.m_BestDirection);
				float num2 = MathUtils.Snap(math.dot(x, iterator.m_BestDirection), 8f, offset);
				float num3 = MathUtils.Snap(math.dot(x, @float), 8f, offset);
				ControlPoint controlPoint2 = controlPoint;
				if (!m_EdgeData.HasComponent(controlPoint.m_OriginalEntity) && !m_NodeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					controlPoint2.m_OriginalEntity = Entity.Null;
				}
				controlPoint2.m_Direction = iterator.m_BestDirection;
				controlPoint2.m_Position.xz = iterator.m_BestPosition.xz + iterator.m_BestDirection * num2 + @float * num3;
				controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, controlPoint.m_HitPosition, controlPoint2.m_Position, controlPoint2.m_Direction);
				Line3 line = new Line3(controlPoint2.m_Position, controlPoint2.m_Position);
				Line3 line2 = new Line3(controlPoint2.m_Position, controlPoint2.m_Position);
				line.a.xz -= controlPoint2.m_Direction * 8f;
				line.b.xz += controlPoint2.m_Direction * 8f;
				line2.a.xz -= MathUtils.Right(controlPoint2.m_Direction) * 8f;
				line2.b.xz += MathUtils.Right(controlPoint2.m_Direction) * 8f;
				ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
				ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden, 1f));
				controlPoint2.m_Direction = MathUtils.Right(controlPoint2.m_Direction);
				ToolUtils.AddSnapLine(ref bestSnapPosition, m_SnapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden, 1f));
			}
		}

		private void HandleExistingObjects(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, RoadData prefabRoadData, NetGeometryData prefabGeometryData, NetData prefabNetData, PlaceableNetData placeableNetData)
		{
			float num = (((m_Snap & Snap.NearbyGeometry) != Snap.None) ? placeableNetData.m_SnapDistance : 0f);
			float num2 = (((prefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) == 0 || (m_Snap & Snap.CellLength) == 0) ? (prefabGeometryData.m_DefaultWidth * 0.5f) : ((float)ZoneUtils.GetCellWidth(prefabGeometryData.m_DefaultWidth) * 4f));
			float num3 = 0f;
			if ((m_Snap & (Snap.ExistingGeometry | Snap.NearbyGeometry)) != Snap.None)
			{
				num3 = math.max(num3, prefabGeometryData.m_DefaultWidth + num);
			}
			if ((m_Snap & Snap.ObjectSide) != Snap.None)
			{
				num3 = math.max(num3, num2 + placeableNetData.m_SnapDistance);
			}
			ObjectIterator iterator = new ObjectIterator
			{
				m_Bounds = new Bounds3(controlPoint.m_HitPosition - num3, controlPoint.m_HitPosition + num3),
				m_Snap = m_Snap,
				m_MaxDistance = placeableNetData.m_SnapDistance,
				m_NetSnapOffset = num,
				m_ObjectSnapOffset = num2,
				m_SnapCellLength = ((prefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0 && (m_Snap & Snap.CellLength) != 0),
				m_NetData = prefabNetData,
				m_NetGeometryData = prefabGeometryData,
				m_ControlPoint = controlPoint,
				m_BestSnapPosition = bestSnapPosition,
				m_SnapLines = m_SnapLines,
				m_OwnerData = m_OwnerData,
				m_CurveData = m_CurveData,
				m_NodeData = m_NodeData,
				m_TransformData = m_TransformData,
				m_PrefabRefData = m_PrefabRefData,
				m_BuildingData = m_BuildingData,
				m_ObjectGeometryData = m_ObjectGeometryData,
				m_PrefabNetData = m_PrefabNetData,
				m_PrefabGeometryData = m_PrefabGeometryData,
				m_ConnectedEdges = m_ConnectedEdges
			};
			m_ObjectSearchTree.Iterate(ref iterator);
			bestSnapPosition = iterator.m_BestSnapPosition;
		}

		private static SnapLineFlags GetSnapLineFlags(Game.Net.GeometryFlags geometryFlags)
		{
			SnapLineFlags snapLineFlags = (SnapLineFlags)0;
			if ((geometryFlags & Game.Net.GeometryFlags.StrictNodes) == 0)
			{
				snapLineFlags |= SnapLineFlags.ExtendedCurve;
			}
			return snapLineFlags;
		}

		private void HandleExistingGeometry(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, RoadData prefabRoadData, NetGeometryData prefabGeometryData, NetData prefabNetData, LocalConnectData localConnectData, PlaceableNetData placeableNetData)
		{
			float num = (((m_Snap & Snap.NearbyGeometry) != Snap.None) ? placeableNetData.m_SnapDistance : 0f);
			float num2 = prefabGeometryData.m_DefaultWidth + num;
			float num3 = placeableNetData.m_SnapDistance * 64f;
			Bounds1 bounds = new Bounds1(-50f, 50f) | localConnectData.m_HeightRange;
			Bounds3 bounds2 = new Bounds3
			{
				xz = new Bounds2(controlPoint.m_HitPosition.xz - num2, controlPoint.m_HitPosition.xz + num2),
				y = controlPoint.m_HitPosition.y + bounds
			};
			Bounds3 totalBounds = bounds2;
			if ((m_Snap & Snap.GuideLines) != Snap.None)
			{
				totalBounds.min -= num3;
				totalBounds.max += num3;
			}
			float num4 = -1f;
			if ((prefabGeometryData.m_Flags & (Game.Net.GeometryFlags.SnapToNetAreas | Game.Net.GeometryFlags.StandingNodes)) != 0 && m_SubObjects.HasBuffer(m_Prefab))
			{
				DynamicBuffer<Game.Prefabs.SubObject> dynamicBuffer = m_SubObjects[m_Prefab];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Game.Prefabs.SubObject subObject = dynamicBuffer[i];
					if (m_ObjectGeometryData.HasComponent(subObject.m_Prefab))
					{
						ObjectGeometryData objectGeometryData = m_ObjectGeometryData[subObject.m_Prefab];
						if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							num4 = math.max(num4, objectGeometryData.m_LegSize.x + objectGeometryData.m_LegOffset.x * 2f);
						}
					}
				}
			}
			num4 = math.select(num4, prefabGeometryData.m_DefaultWidth, num4 <= 0f);
			NetIterator iterator = new NetIterator
			{
				m_TotalBounds = totalBounds,
				m_Bounds = bounds2,
				m_Snap = m_Snap,
				m_ServiceUpgradeOwner = m_ServiceUpgradeOwner,
				m_SnapOffset = num,
				m_SnapDistance = placeableNetData.m_SnapDistance,
				m_Elevation = m_Elevation,
				m_GuideLength = num3,
				m_LegSnapWidth = num4,
				m_HeightRange = bounds,
				m_NetData = prefabNetData,
				m_PrefabRoadData = prefabRoadData,
				m_NetGeometryData = prefabGeometryData,
				m_LocalConnectData = localConnectData,
				m_ControlPoint = controlPoint,
				m_BestSnapPosition = bestSnapPosition,
				m_SnapLines = m_SnapLines,
				m_TerrainHeightData = m_TerrainHeightData,
				m_WaterSurfaceData = m_WaterSurfaceData,
				m_OwnerData = m_OwnerData,
				m_EditorMode = m_EditorMode,
				m_NodeData = m_NodeData,
				m_EdgeData = m_EdgeData,
				m_CurveData = m_CurveData,
				m_CompositionData = m_CompositionData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_RoadData = m_RoadData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabNetData = m_PrefabNetData,
				m_PrefabGeometryData = m_PrefabGeometryData,
				m_PrefabCompositionData = m_PrefabCompositionData,
				m_RoadCompositionData = m_RoadCompositionData,
				m_ConnectedEdges = m_ConnectedEdges,
				m_SubNets = m_SubNets,
				m_PrefabCompositionAreas = m_PrefabCompositionAreas
			};
			if ((m_Snap & Snap.ExistingGeometry) != Snap.None && m_PrefabRefData.HasComponent(controlPoint.m_OriginalEntity))
			{
				PrefabRef prefabRef = m_PrefabRefData[controlPoint.m_OriginalEntity];
				if (!iterator.HandleGeometry(controlPoint, controlPoint.m_HitPosition.y, prefabRef, ignoreHeightDistance: true) && (m_Snap & Snap.GuideLines) != Snap.None)
				{
					iterator.HandleGuideLines(controlPoint.m_OriginalEntity);
				}
			}
			m_NetSearchTree.Iterate(ref iterator);
			bestSnapPosition = iterator.m_BestSnapPosition;
		}
	}

	[BurstCompile]
	public struct FixControlPointsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		public NativeList<ControlPoint> m_ControlPoints;

		public void Execute()
		{
			Entity entity = Entity.Null;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TempType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Temp temp = nativeArray2[j];
					Entity entity2 = nativeArray[j];
					Edge componentData;
					Edge componentData2;
					Temp componentData3;
					Temp componentData4;
					if ((temp.m_Flags & TempFlags.Delete) != 0)
					{
						if (temp.m_Original != Entity.Null)
						{
							FixControlPoints(temp.m_Original, Entity.Null);
						}
					}
					else if ((temp.m_Flags & (TempFlags.Replace | TempFlags.Combine)) != 0)
					{
						if (temp.m_Original != Entity.Null)
						{
							FixControlPoints(temp.m_Original, entity2);
						}
					}
					else if ((temp.m_Flags & TempFlags.Modify) != 0 && m_EdgeData.TryGetComponent(entity2, out componentData) && m_EdgeData.TryGetComponent(temp.m_Original, out componentData2) && ((m_TempData.TryGetComponent(componentData.m_Start, out componentData3) && componentData3.m_Original == componentData2.m_End) || (m_TempData.TryGetComponent(componentData.m_End, out componentData4) && componentData4.m_Original == componentData2.m_Start)))
					{
						InverseCurvePositions(temp.m_Original);
					}
					if ((temp.m_Flags & TempFlags.IsLast) != 0)
					{
						entity = (((temp.m_Flags & (TempFlags.Create | TempFlags.Replace)) == 0) ? temp.m_Original : entity2);
					}
				}
			}
			if (entity != Entity.Null && m_Mode != Mode.Replace)
			{
				for (int k = 0; k < m_ControlPoints.Length; k++)
				{
					ControlPoint value = m_ControlPoints[k];
					value.m_OriginalEntity = entity;
					m_ControlPoints[k] = value;
				}
			}
		}

		private void FixControlPoints(Entity entity, Entity replace)
		{
			if (!(entity != Entity.Null))
			{
				return;
			}
			for (int i = 0; i < m_ControlPoints.Length; i++)
			{
				ControlPoint value = m_ControlPoints[i];
				if (value.m_OriginalEntity == entity)
				{
					value.m_OriginalEntity = replace;
					m_ControlPoints[i] = value;
				}
			}
		}

		private void InverseCurvePositions(Entity entity)
		{
			if (!(entity != Entity.Null))
			{
				return;
			}
			for (int i = 0; i < m_ControlPoints.Length; i++)
			{
				ControlPoint value = m_ControlPoints[i];
				if (value.m_OriginalEntity == entity)
				{
					value.m_CurvePosition = 1f - value.m_CurvePosition;
					m_ControlPoints[i] = value;
				}
			}
		}
	}

	[BurstCompile]
	public struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_RemoveUpgrade;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public int2 m_ParallelCount;

		[ReadOnly]
		public float m_ParallelOffset;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public AgeMask m_AgeMask;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public NativeList<UpgradeState> m_UpgradeStates;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Extension> m_ExtensionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_CachedNodes;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public Entity m_NetPrefab;

		[ReadOnly]
		public Entity m_LanePrefab;

		[ReadOnly]
		public Entity m_ServiceUpgradeOwner;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions = default(NativeParallelHashMap<Entity, OwnerDefinition>);
			if (m_Mode == Mode.Replace)
			{
				CreateReplacement(ref ownerDefinitions);
			}
			else
			{
				int length = m_ControlPoints.Length;
				if (length == 1)
				{
					CreateSinglePoint(ref ownerDefinitions);
				}
				else
				{
					switch (m_Mode)
					{
					case Mode.Straight:
						CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
						break;
					case Mode.SimpleCurve:
						if (length == 2)
						{
							CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
						}
						else
						{
							CreateSimpleCurve(ref ownerDefinitions, 1);
						}
						break;
					case Mode.ComplexCurve:
						switch (length)
						{
						case 2:
							CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
							break;
						case 3:
							CreateSimpleCurve(ref ownerDefinitions, 1);
							break;
						default:
							CreateComplexCurve(ref ownerDefinitions);
							break;
						}
						break;
					case Mode.Grid:
						if (length == 2)
						{
							CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
						}
						else
						{
							CreateGrid(ref ownerDefinitions);
						}
						break;
					case Mode.Continuous:
						if (length == 2)
						{
							CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
						}
						else
						{
							CreateContinuousCurve(ref ownerDefinitions);
						}
						break;
					}
				}
			}
			if (ownerDefinitions.IsCreated)
			{
				ownerDefinitions.Dispose();
			}
		}

		private bool GetLocalCurve(NetCourse course, OwnerDefinition ownerDefinition, out LocalCurveCache localCurveCache)
		{
			Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(new Game.Objects.Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation));
			localCurveCache = default(LocalCurveCache);
			localCurveCache.m_Curve.a = ObjectUtils.WorldToLocal(inverseParentTransform, course.m_Curve.a);
			localCurveCache.m_Curve.b = ObjectUtils.WorldToLocal(inverseParentTransform, course.m_Curve.b);
			localCurveCache.m_Curve.c = ObjectUtils.WorldToLocal(inverseParentTransform, course.m_Curve.c);
			localCurveCache.m_Curve.d = ObjectUtils.WorldToLocal(inverseParentTransform, course.m_Curve.d);
			return true;
		}

		private bool GetOwnerDefinition(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, Entity original, bool checkControlPoints, CoursePos startPos, CoursePos endPos, out OwnerDefinition ownerDefinition)
		{
			Entity entity = Entity.Null;
			ownerDefinition = default(OwnerDefinition);
			if (m_OwnerData.TryGetComponent(original, out var componentData))
			{
				entity = componentData.m_Owner;
			}
			else if (m_EditorMode)
			{
				if (checkControlPoints)
				{
					for (int i = 0; i < m_ControlPoints.Length; i++)
					{
						Entity entity2 = m_ControlPoints[i].m_OriginalEntity;
						if (m_NodeData.HasComponent(entity2))
						{
							entity2 = Entity.Null;
						}
						while (m_OwnerData.HasComponent(entity2) && !m_BuildingData.HasComponent(entity2))
						{
							entity2 = m_OwnerData[entity2].m_Owner;
							if (m_TempData.HasComponent(entity2))
							{
								Temp temp = m_TempData[entity2];
								if (temp.m_Original != Entity.Null)
								{
									entity2 = temp.m_Original;
								}
							}
						}
						if (m_InstalledUpgrades.TryGetBuffer(entity2, out var bufferData) && bufferData.Length != 0)
						{
							entity2 = bufferData[0].m_Upgrade;
						}
						if (m_TransformData.HasComponent(entity2) && m_SubNets.HasBuffer(entity2))
						{
							entity = entity2;
							break;
						}
					}
				}
			}
			else
			{
				entity = m_ServiceUpgradeOwner;
			}
			Game.Objects.Transform componentData2;
			Curve componentData5;
			if (ownerDefinitions.IsCreated && ownerDefinitions.TryGetValue(entity, out var item))
			{
				ownerDefinition = item;
			}
			else if (m_TransformData.TryGetComponent(entity, out componentData2))
			{
				Entity owner = Entity.Null;
				if (m_OwnerData.TryGetComponent(entity, out componentData))
				{
					owner = componentData.m_Owner;
				}
				UpdateOwnerObject(owner, entity, Entity.Null, componentData2);
				ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				ownerDefinition.m_Position = componentData2.m_Position;
				ownerDefinition.m_Rotation = componentData2.m_Rotation;
				if (m_AttachmentData.TryGetComponent(entity, out var componentData3) && m_TransformData.TryGetComponent(componentData3.m_Attached, out var componentData4))
				{
					UpdateOwnerObject(Entity.Null, componentData3.m_Attached, entity, componentData4);
				}
				if (!ownerDefinitions.IsCreated)
				{
					ownerDefinitions = new NativeParallelHashMap<Entity, OwnerDefinition>(8, Allocator.Temp);
				}
				ownerDefinitions.Add(entity, ownerDefinition);
			}
			else if (m_CurveData.TryGetComponent(entity, out componentData5))
			{
				ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				ownerDefinition.m_Position = componentData5.m_Bezier.a;
				ownerDefinition.m_Rotation = new float4(componentData5.m_Bezier.d, 0f);
			}
			if ((startPos.m_Flags & endPos.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != (CoursePosFlags.IsFirst | CoursePosFlags.IsLast) && m_PrefabSubObjects.HasBuffer(m_NetPrefab))
			{
				DynamicBuffer<Game.Prefabs.SubObject> dynamicBuffer = m_PrefabSubObjects[m_NetPrefab];
				NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Game.Prefabs.SubObject subObject = dynamicBuffer[j];
					if ((subObject.m_Flags & SubObjectFlags.MakeOwner) != 0)
					{
						Game.Objects.Transform courseObjectTransform = GetCourseObjectTransform(subObject, startPos, endPos);
						CreateCourseObject(subObject.m_Prefab, courseObjectTransform, ownerDefinition, ref selectedSpawnables);
						ownerDefinition.m_Prefab = subObject.m_Prefab;
						ownerDefinition.m_Position = courseObjectTransform.m_Position;
						ownerDefinition.m_Rotation = courseObjectTransform.m_Rotation;
						break;
					}
				}
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Game.Prefabs.SubObject subObject2 = dynamicBuffer[k];
					if ((subObject2.m_Flags & (SubObjectFlags.CoursePlacement | SubObjectFlags.MakeOwner)) == SubObjectFlags.CoursePlacement)
					{
						Game.Objects.Transform courseObjectTransform2 = GetCourseObjectTransform(subObject2, startPos, endPos);
						CreateCourseObject(subObject2.m_Prefab, courseObjectTransform2, ownerDefinition, ref selectedSpawnables);
					}
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Dispose();
				}
			}
			return ownerDefinition.m_Prefab != Entity.Null;
		}

		private void UpdateOwnerObject(Entity owner, Entity original, Entity attachedParent, Game.Objects.Transform transform)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			Entity prefab = m_PrefabRefData[original].m_Prefab;
			CreationDefinition component = new CreationDefinition
			{
				m_Owner = owner,
				m_Original = original
			};
			component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_ParentMesh = -1,
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation
			};
			if (m_TransformData.HasComponent(owner))
			{
				Game.Objects.Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
				component2.m_LocalPosition = transform2.m_Position;
				component2.m_LocalRotation = transform2.m_Rotation;
			}
			else
			{
				component2.m_LocalPosition = transform.m_Position;
				component2.m_LocalRotation = transform.m_Rotation;
			}
			if (m_PrefabRefData.TryGetComponent(attachedParent, out var componentData))
			{
				component.m_Attached = componentData.m_Prefab;
				component.m_Flags |= CreationFlags.Attach;
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
			UpdateSubNets(transform, prefab, original);
			UpdateSubAreas(transform, prefab, original);
		}

		private Game.Objects.Transform GetCourseObjectTransform(Game.Prefabs.SubObject subObject, CoursePos startPos, CoursePos endPos)
		{
			CoursePos coursePos = (((subObject.m_Flags & SubObjectFlags.StartPlacement) != 0) ? startPos : endPos);
			Game.Objects.Transform result = default(Game.Objects.Transform);
			result.m_Position = ObjectUtils.LocalToWorld(coursePos.m_Position, coursePos.m_Rotation, subObject.m_Position);
			result.m_Rotation = math.mul(coursePos.m_Rotation, subObject.m_Rotation);
			return result;
		}

		private void CreateCourseObject(Entity prefab, Game.Objects.Transform transform, OwnerDefinition ownerDefinition, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = prefab
			};
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_ParentMesh = -1,
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation
			};
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				Game.Objects.Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Game.Objects.Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
				component2.m_LocalPosition = transform2.m_Position;
				component2.m_LocalRotation = transform2.m_Rotation;
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
			else
			{
				component2.m_LocalPosition = transform.m_Position;
				component2.m_LocalRotation = transform.m_Rotation;
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
			CreateSubNets(transform, prefab);
			CreateSubAreas(transform, prefab, ref selectedSpawnables);
		}

		private void CreateSubAreas(Game.Objects.Transform transform, Entity prefab, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			if (!m_PrefabSubAreas.HasBuffer(prefab))
			{
				return;
			}
			DynamicBuffer<Game.Prefabs.SubArea> dynamicBuffer = m_PrefabSubAreas[prefab];
			DynamicBuffer<SubAreaNode> nodes = m_PrefabSubAreaNodes[prefab];
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(10000);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Game.Prefabs.SubArea subArea = dynamicBuffer[i];
				int seed;
				if (!m_EditorMode && m_PrefabPlaceholderElements.HasBuffer(subArea.m_Prefab))
				{
					DynamicBuffer<PlaceholderObjectElement> placeholderElements = m_PrefabPlaceholderElements[subArea.m_Prefab];
					if (!selectedSpawnables.IsCreated)
					{
						selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
					}
					if (!AreaUtils.SelectAreaPrefab(placeholderElements, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
					{
						continue;
					}
				}
				else
				{
					seed = random.NextInt();
				}
				AreaGeometryData areaGeometryData = m_PrefabAreaGeometryData[subArea.m_Prefab];
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = subArea.m_Prefab,
					m_RandomSeed = seed
				};
				if (areaGeometryData.m_Type != Game.Areas.AreaType.Lot)
				{
					component.m_Flags |= CreationFlags.Hidden;
				}
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				OwnerDefinition component2 = new OwnerDefinition
				{
					m_Prefab = prefab,
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				m_CommandBuffer.AddComponent(e, component2);
				DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
				dynamicBuffer2.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
				DynamicBuffer<LocalNodeCache> dynamicBuffer3 = default(DynamicBuffer<LocalNodeCache>);
				if (m_EditorMode)
				{
					dynamicBuffer3 = m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
					dynamicBuffer3.ResizeUninitialized(dynamicBuffer2.Length);
				}
				int num = ObjectToolBaseSystem.GetFirstNodeIndex(nodes, subArea.m_NodeRange);
				int num2 = 0;
				for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
				{
					float3 position = nodes[num].m_Position;
					float3 position2 = ObjectUtils.LocalToWorld(transform, position);
					int parentMesh = nodes[num].m_ParentMesh;
					float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
					dynamicBuffer2[num2] = new Game.Areas.Node(position2, elevation);
					if (m_EditorMode)
					{
						dynamicBuffer3[num2] = new LocalNodeCache
						{
							m_Position = position,
							m_ParentMesh = parentMesh
						};
					}
					num2++;
					if (++num == subArea.m_NodeRange.y)
					{
						num = subArea.m_NodeRange.x;
					}
				}
			}
		}

		private void CreateSubNets(Game.Objects.Transform transform, Entity prefab)
		{
			if (!m_PrefabSubNets.HasBuffer(prefab))
			{
				return;
			}
			DynamicBuffer<Game.Prefabs.SubNet> subNets = m_PrefabSubNets[prefab];
			NativeList<float4> nativeList = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
			for (int i = 0; i < subNets.Length; i++)
			{
				Game.Prefabs.SubNet subNet = subNets[i];
				if (subNet.m_NodeIndex.x >= 0)
				{
					while (nativeList.Length <= subNet.m_NodeIndex.x)
					{
						nativeList.Add(default(float4));
					}
					nativeList[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
				}
				if (subNet.m_NodeIndex.y >= 0)
				{
					while (nativeList.Length <= subNet.m_NodeIndex.y)
					{
						nativeList.Add(default(float4));
					}
					nativeList[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
				}
			}
			for (int j = 0; j < nativeList.Length; j++)
			{
				nativeList[j] /= math.max(1f, nativeList[j].w);
			}
			for (int k = 0; k < subNets.Length; k++)
			{
				Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_NetGeometryData);
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = subNet2.m_Prefab
				};
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				OwnerDefinition component2 = new OwnerDefinition
				{
					m_Prefab = prefab,
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				m_CommandBuffer.AddComponent(e, component2);
				NetCourse component3 = default(NetCourse);
				component3.m_Curve = TransformCurve(subNet2.m_Curve, transform.m_Position, transform.m_Rotation);
				component3.m_StartPosition.m_Position = component3.m_Curve.a;
				component3.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component3.m_Curve), transform.m_Rotation);
				component3.m_StartPosition.m_CourseDelta = 0f;
				component3.m_StartPosition.m_Elevation = subNet2.m_Curve.a.y;
				component3.m_StartPosition.m_ParentMesh = subNet2.m_ParentMesh.x;
				if (subNet2.m_NodeIndex.x >= 0)
				{
					component3.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(transform, nativeList[subNet2.m_NodeIndex.x].xyz);
				}
				component3.m_EndPosition.m_Position = component3.m_Curve.d;
				component3.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component3.m_Curve), transform.m_Rotation);
				component3.m_EndPosition.m_CourseDelta = 1f;
				component3.m_EndPosition.m_Elevation = subNet2.m_Curve.d.y;
				component3.m_EndPosition.m_ParentMesh = subNet2.m_ParentMesh.y;
				if (subNet2.m_NodeIndex.y >= 0)
				{
					component3.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(transform, nativeList[subNet2.m_NodeIndex.y].xyz);
				}
				component3.m_Length = MathUtils.Length(component3.m_Curve);
				component3.m_FixedIndex = -1;
				component3.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
				component3.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
				if (component3.m_StartPosition.m_Position.Equals(component3.m_EndPosition.m_Position))
				{
					component3.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
					component3.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
				}
				m_CommandBuffer.AddComponent(e, component3);
				if (subNet2.m_Upgrades != default(CompositionFlags))
				{
					Upgraded component4 = new Upgraded
					{
						m_Flags = subNet2.m_Upgrades
					};
					m_CommandBuffer.AddComponent(e, component4);
				}
				if (m_EditorMode)
				{
					LocalCurveCache component5 = new LocalCurveCache
					{
						m_Curve = subNet2.m_Curve
					};
					m_CommandBuffer.AddComponent(e, component5);
				}
			}
			nativeList.Dispose();
		}

		private Bezier4x3 TransformCurve(Bezier4x3 curve, float3 position, quaternion rotation)
		{
			curve.a = ObjectUtils.LocalToWorld(position, rotation, curve.a);
			curve.b = ObjectUtils.LocalToWorld(position, rotation, curve.b);
			curve.c = ObjectUtils.LocalToWorld(position, rotation, curve.c);
			curve.d = ObjectUtils.LocalToWorld(position, rotation, curve.d);
			return curve;
		}

		private void UpdateSubNets(Game.Objects.Transform transform, Entity prefab, Entity original)
		{
			NativeParallelHashSet<Entity> nativeParallelHashSet = default(NativeParallelHashSet<Entity>);
			if (m_Mode == Mode.Replace && m_UpgradeStates.Length != 0)
			{
				nativeParallelHashSet = new NativeParallelHashSet<Entity>(m_UpgradeStates.Length, Allocator.Temp);
				for (int i = 0; i < m_UpgradeStates.Length; i++)
				{
					ControlPoint controlPoint = m_ControlPoints[i * 2 + 1];
					ControlPoint controlPoint2 = m_ControlPoints[i * 2 + 2];
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[controlPoint.m_OriginalEntity];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity edge = dynamicBuffer[j].m_Edge;
						Edge edge2 = m_EdgeData[edge];
						if (edge2.m_Start == controlPoint.m_OriginalEntity && edge2.m_End == controlPoint2.m_OriginalEntity)
						{
							nativeParallelHashSet.Add(edge);
						}
						else if (edge2.m_End == controlPoint.m_OriginalEntity && edge2.m_Start == controlPoint2.m_OriginalEntity)
						{
							nativeParallelHashSet.Add(edge);
						}
					}
				}
			}
			if (m_SubNets.HasBuffer(original))
			{
				DynamicBuffer<Game.Net.SubNet> dynamicBuffer2 = m_SubNets[original];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					Entity subNet = dynamicBuffer2[k].m_SubNet;
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
					else if (m_EdgeData.HasComponent(subNet) && (!nativeParallelHashSet.IsCreated || !nativeParallelHashSet.Contains(subNet)))
					{
						Edge edge3 = m_EdgeData[subNet];
						Entity e2 = m_CommandBuffer.CreateEntity();
						CreationDefinition component4 = new CreationDefinition
						{
							m_Original = subNet
						};
						if (m_EditorContainerData.HasComponent(subNet))
						{
							component4.m_SubPrefab = m_EditorContainerData[subNet].m_Prefab;
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
						component6.m_Curve = m_CurveData[subNet].m_Bezier;
						component6.m_Length = MathUtils.Length(component6.m_Curve);
						component6.m_FixedIndex = -1;
						component6.m_StartPosition.m_Entity = edge3.m_Start;
						component6.m_StartPosition.m_Position = component6.m_Curve.a;
						component6.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component6.m_Curve));
						component6.m_StartPosition.m_CourseDelta = 0f;
						component6.m_EndPosition.m_Entity = edge3.m_End;
						component6.m_EndPosition.m_Position = component6.m_Curve.d;
						component6.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component6.m_Curve));
						component6.m_EndPosition.m_CourseDelta = 1f;
						m_CommandBuffer.AddComponent(e2, component6);
					}
				}
			}
			if (nativeParallelHashSet.IsCreated)
			{
				nativeParallelHashSet.Dispose();
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

		private void UpdateSubAreas(Game.Objects.Transform transform, Entity prefab, Entity original)
		{
			if (!m_SubAreas.HasBuffer(original))
			{
				return;
			}
			DynamicBuffer<Game.Areas.SubArea> dynamicBuffer = m_SubAreas[original];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity area = dynamicBuffer[i].m_Area;
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
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_AreaNodes[area];
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(dynamicBuffer2.AsNativeArray());
				if (m_CachedNodes.HasBuffer(area))
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer3 = m_CachedNodes[area];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(e).CopyFrom(dynamicBuffer3.AsNativeArray());
				}
			}
		}

		private void CreateReplacement(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions)
		{
			for (int i = 0; i < m_UpgradeStates.Length; i++)
			{
				ControlPoint controlPoint = m_ControlPoints[i * 2 + 1];
				ControlPoint endPoint = m_ControlPoints[i * 2 + 2];
				UpgradeState upgradeState = m_UpgradeStates[i];
				if (controlPoint.m_OriginalEntity == Entity.Null || endPoint.m_OriginalEntity == Entity.Null)
				{
					continue;
				}
				if (controlPoint.m_OriginalEntity == endPoint.m_OriginalEntity)
				{
					if (upgradeState.m_IsUpgrading || m_RemoveUpgrade)
					{
						CreateUpgrade(ref ownerDefinitions, upgradeState, controlPoint, i == 0, i == m_UpgradeStates.Length - 1);
					}
					else
					{
						CreateReplacement(ref ownerDefinitions, controlPoint, i == 0, i == m_UpgradeStates.Length - 1);
					}
					continue;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[controlPoint.m_OriginalEntity];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity edge = dynamicBuffer[j].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == controlPoint.m_OriginalEntity && edge2.m_End == endPoint.m_OriginalEntity)
					{
						if (upgradeState.m_IsUpgrading || m_RemoveUpgrade)
						{
							CreateUpgrade(ref ownerDefinitions, edge, upgradeState, invert: false, i == 0, i == m_UpgradeStates.Length - 1);
						}
						else
						{
							CreateReplacement(ref ownerDefinitions, controlPoint, endPoint, edge, invert: false, i == 0, i == m_UpgradeStates.Length - 1);
						}
					}
					else if (edge2.m_End == controlPoint.m_OriginalEntity && edge2.m_Start == endPoint.m_OriginalEntity)
					{
						if (upgradeState.m_IsUpgrading || m_RemoveUpgrade)
						{
							CreateUpgrade(ref ownerDefinitions, edge, upgradeState, invert: true, i == 0, i == m_UpgradeStates.Length - 1);
						}
						else
						{
							CreateReplacement(ref ownerDefinitions, controlPoint, endPoint, edge, invert: true, i == 0, i == m_UpgradeStates.Length - 1);
						}
					}
				}
			}
		}

		private void CreateReplacement(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, ControlPoint point, bool isStart, bool isEnd)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = point.m_OriginalEntity,
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab
			};
			component.m_Flags |= CreationFlags.SubElevation;
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			NetCourse netCourse = default(NetCourse);
			netCourse.m_Curve = new Bezier4x3(point.m_Position, point.m_Position, point.m_Position, point.m_Position);
			netCourse.m_StartPosition = GetCoursePos(netCourse.m_Curve, point, 0f);
			netCourse.m_EndPosition = GetCoursePos(netCourse.m_Curve, point, 1f);
			netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
			netCourse.m_FixedIndex = -1;
			if (isStart)
			{
				netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			if (isEnd)
			{
				netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			}
			if (GetOwnerDefinition(ref ownerDefinitions, point.m_OriginalEntity, checkControlPoints: false, netCourse.m_StartPosition, netCourse.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				netCourse.m_StartPosition.m_ParentMesh = -1;
				netCourse.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, netCourse);
		}

		private void CreateReplacement(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, ControlPoint startPoint, ControlPoint endPoint, Entity edge, bool invert, bool isStart, bool isEnd)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = edge,
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab
			};
			component.m_Flags |= CreationFlags.Align | CreationFlags.SubElevation;
			Curve curve = m_CurveData[edge];
			if (invert)
			{
				curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
				component.m_Flags |= CreationFlags.Invert;
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			NetCourse netCourse = default(NetCourse);
			if (startPoint.m_Position.Equals(curve.m_Bezier.a) && endPoint.m_Position.Equals(curve.m_Bezier.d))
			{
				netCourse.m_Curve = curve.m_Bezier;
				netCourse.m_Length = curve.m_Length;
				netCourse.m_FixedIndex = -1;
			}
			else
			{
				float3 value = MathUtils.StartTangent(curve.m_Bezier);
				float3 value2 = MathUtils.EndTangent(curve.m_Bezier);
				value = MathUtils.Normalize(value, value.xz);
				value2 = MathUtils.Normalize(value2, value2.xz);
				netCourse.m_Curve = NetUtils.FitCurve(startPoint.m_Position, value, value2, endPoint.m_Position);
				netCourse.m_Curve.b.y = curve.m_Bezier.b.y;
				netCourse.m_Curve.c.y = curve.m_Bezier.c.y;
				netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
				netCourse.m_FixedIndex = -1;
			}
			if (m_FixedData.TryGetComponent(edge, out var componentData))
			{
				netCourse.m_FixedIndex = componentData.m_Index;
			}
			netCourse.m_StartPosition.m_Entity = startPoint.m_OriginalEntity;
			netCourse.m_StartPosition.m_Position = startPoint.m_Position;
			netCourse.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(netCourse.m_Curve));
			netCourse.m_StartPosition.m_CourseDelta = 0f;
			netCourse.m_EndPosition.m_Entity = endPoint.m_OriginalEntity;
			netCourse.m_EndPosition.m_Position = endPoint.m_Position;
			netCourse.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(netCourse.m_Curve));
			netCourse.m_EndPosition.m_CourseDelta = 1f;
			if (isStart)
			{
				netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			if (isEnd)
			{
				netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			}
			if (GetOwnerDefinition(ref ownerDefinitions, edge, checkControlPoints: false, netCourse.m_StartPosition, netCourse.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			m_CommandBuffer.AddComponent(e, netCourse);
		}

		private void CreateUpgrade(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, UpgradeState upgradeState, ControlPoint point, bool isStart, bool isEnd)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = point.m_OriginalEntity,
				m_Prefab = m_PrefabRefData[point.m_OriginalEntity].m_Prefab
			};
			component.m_Flags |= CreationFlags.Align | CreationFlags.SubElevation;
			if (!upgradeState.m_SkipFlags)
			{
				m_UpgradedData.TryGetComponent(point.m_OriginalEntity, out var componentData);
				componentData.m_Flags = (componentData.m_Flags & ~upgradeState.m_RemoveFlags) | (upgradeState.m_AddFlags & ~upgradeState.m_OldFlags);
				m_CommandBuffer.AddComponent(e, componentData);
				if (((upgradeState.m_OldFlags & ~upgradeState.m_RemoveFlags) | upgradeState.m_AddFlags) != upgradeState.m_OldFlags)
				{
					component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
				}
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			NetCourse netCourse = default(NetCourse);
			netCourse.m_Curve = new Bezier4x3(point.m_Position, point.m_Position, point.m_Position, point.m_Position);
			netCourse.m_StartPosition = GetCoursePos(netCourse.m_Curve, point, 0f);
			netCourse.m_EndPosition = GetCoursePos(netCourse.m_Curve, point, 1f);
			netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
			netCourse.m_FixedIndex = -1;
			if (isStart)
			{
				netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			if (isEnd)
			{
				netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			}
			if (GetOwnerDefinition(ref ownerDefinitions, point.m_OriginalEntity, checkControlPoints: false, netCourse.m_StartPosition, netCourse.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				netCourse.m_StartPosition.m_ParentMesh = -1;
				netCourse.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, netCourse);
		}

		private void CreateUpgrade(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, Entity edge, UpgradeState upgradeState, bool invert, bool isStart, bool isEnd)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = edge,
				m_Prefab = m_PrefabRefData[edge].m_Prefab
			};
			component.m_Flags |= CreationFlags.Align | CreationFlags.SubElevation;
			if (!upgradeState.m_SkipFlags)
			{
				m_UpgradedData.TryGetComponent(edge, out var componentData);
				componentData.m_Flags = (componentData.m_Flags & ~upgradeState.m_RemoveFlags) | (upgradeState.m_AddFlags & ~upgradeState.m_OldFlags);
				m_CommandBuffer.AddComponent(e, componentData);
				DynamicBuffer<SubReplacement> dynamicBuffer = m_CommandBuffer.AddBuffer<SubReplacement>(e);
				if (m_SubReplacements.TryGetBuffer(edge, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						SubReplacement elem = bufferData[i];
						if (elem.m_Side != upgradeState.m_SubReplacementSide || elem.m_Type != upgradeState.m_SubReplacementType)
						{
							dynamicBuffer.Add(elem);
						}
					}
				}
				if (upgradeState.m_SubReplacementType != SubReplacementType.None && upgradeState.m_SubReplacementPrefab != Entity.Null)
				{
					dynamicBuffer.Add(new SubReplacement
					{
						m_Prefab = upgradeState.m_SubReplacementPrefab,
						m_Type = upgradeState.m_SubReplacementType,
						m_Side = upgradeState.m_SubReplacementSide,
						m_AgeMask = m_AgeMask
					});
				}
				if (((upgradeState.m_OldFlags & ~upgradeState.m_RemoveFlags) | upgradeState.m_AddFlags) != upgradeState.m_OldFlags || upgradeState.m_SubReplacementType != SubReplacementType.None)
				{
					component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
				}
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			Edge edge2 = m_EdgeData[edge];
			NetCourse netCourse = default(NetCourse);
			netCourse.m_Curve = m_CurveData[edge].m_Bezier;
			netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
			netCourse.m_FixedIndex = -1;
			if (m_FixedData.TryGetComponent(edge, out var componentData2))
			{
				netCourse.m_FixedIndex = componentData2.m_Index;
			}
			netCourse.m_StartPosition.m_Entity = edge2.m_Start;
			netCourse.m_StartPosition.m_Position = netCourse.m_Curve.a;
			netCourse.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(netCourse.m_Curve));
			netCourse.m_StartPosition.m_CourseDelta = 0f;
			netCourse.m_EndPosition.m_Entity = edge2.m_End;
			netCourse.m_EndPosition.m_Position = netCourse.m_Curve.d;
			netCourse.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(netCourse.m_Curve));
			netCourse.m_EndPosition.m_CourseDelta = 1f;
			if (invert)
			{
				if (isStart)
				{
					netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
				}
				if (isEnd)
				{
					netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				}
			}
			else
			{
				if (isStart)
				{
					netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
				}
				if (isEnd)
				{
					netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
				}
			}
			if (GetOwnerDefinition(ref ownerDefinitions, edge, checkControlPoints: false, netCourse.m_StartPosition, netCourse.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			m_CommandBuffer.AddComponent(e, netCourse);
		}

		private void CreateSinglePoint(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions)
		{
			ControlPoint controlPoint = m_ControlPoints[0];
			Entity e = m_CommandBuffer.CreateEntity();
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab,
				m_RandomSeed = random.NextInt()
			};
			component.m_Flags |= CreationFlags.SubElevation;
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			NetCourse netCourse = default(NetCourse);
			netCourse.m_Curve = new Bezier4x3(controlPoint.m_Position, controlPoint.m_Position, controlPoint.m_Position, controlPoint.m_Position);
			netCourse.m_StartPosition = GetCoursePos(netCourse.m_Curve, controlPoint, 0f);
			netCourse.m_EndPosition = GetCoursePos(netCourse.m_Curve, controlPoint, 1f);
			netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.IsLast | CoursePosFlags.IsRight | CoursePosFlags.IsLeft;
			netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.IsLast | CoursePosFlags.IsRight | CoursePosFlags.IsLeft;
			netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
			netCourse.m_FixedIndex = -1;
			if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, netCourse.m_StartPosition, netCourse.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				netCourse.m_StartPosition.m_ParentMesh = -1;
				netCourse.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, netCourse);
		}

		private void CreateStraightLine(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, int2 index)
		{
			ControlPoint controlPoint = m_ControlPoints[index.x];
			ControlPoint controlPoint2 = m_ControlPoints[index.y];
			FixElevation(ref controlPoint);
			if (m_NetGeometryData.HasComponent(m_NetPrefab) && m_NetGeometryData[m_NetPrefab].m_MaxSlopeSteepness == 0f)
			{
				SetHeight(controlPoint, ref controlPoint2);
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			CreationDefinition creationDefinition = new CreationDefinition
			{
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab,
				m_RandomSeed = random.NextInt()
			};
			creationDefinition.m_Flags |= CreationFlags.SubElevation;
			NetCourse course = default(NetCourse);
			course.m_Curve = NetUtils.StraightCurve(controlPoint.m_Position, controlPoint2.m_Position);
			course.m_StartPosition = GetCoursePos(course.m_Curve, controlPoint, 0f);
			course.m_EndPosition = GetCoursePos(course.m_Curve, controlPoint2, 1f);
			course.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			course.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			if (course.m_StartPosition.m_Position.Equals(course.m_EndPosition.m_Position) && course.m_StartPosition.m_Entity.Equals(course.m_EndPosition.m_Entity))
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			bool2 x = m_ParallelCount > 0;
			if (!x.x)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
			if (!x.y)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			course.m_Length = MathUtils.Length(course.m_Curve);
			course.m_FixedIndex = -1;
			if (m_PlaceableData.HasComponent(m_NetPrefab))
			{
				PlaceableNetData placeableNetData = m_PlaceableData[m_NetPrefab];
				if (CalculatedInverseWeight(course, placeableNetData.m_PlacementFlags) < 0f)
				{
					InvertCourse(ref course);
				}
			}
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, creationDefinition);
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, course.m_StartPosition, course.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(course, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				course.m_StartPosition.m_ParentMesh = -1;
				course.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, course);
			if (math.any(x))
			{
				NativeParallelHashMap<float4, float3> nodeMap = new NativeParallelHashMap<float4, float3>(100, Allocator.Temp);
				CreateParallelCourses(creationDefinition, ownerDefinition, course, nodeMap);
				nodeMap.Dispose();
			}
		}

		private void InvertCourse(ref NetCourse course)
		{
			course.m_Curve = MathUtils.Invert(course.m_Curve);
			CommonUtils.Swap(ref course.m_StartPosition.m_Position, ref course.m_EndPosition.m_Position);
			CommonUtils.Swap(ref course.m_StartPosition.m_Rotation, ref course.m_EndPosition.m_Rotation);
			CommonUtils.Swap(ref course.m_StartPosition.m_Elevation, ref course.m_EndPosition.m_Elevation);
			CommonUtils.Swap(ref course.m_StartPosition.m_Flags, ref course.m_EndPosition.m_Flags);
			CommonUtils.Swap(ref course.m_StartPosition.m_ParentMesh, ref course.m_EndPosition.m_ParentMesh);
			quaternion a = quaternion.RotateY(MathF.PI);
			course.m_StartPosition.m_Rotation = math.mul(a, course.m_StartPosition.m_Rotation);
			course.m_EndPosition.m_Rotation = math.mul(a, course.m_EndPosition.m_Rotation);
			if ((course.m_StartPosition.m_Flags & (CoursePosFlags.IsRight | CoursePosFlags.IsLeft)) == CoursePosFlags.IsLeft)
			{
				course.m_StartPosition.m_Flags &= ~CoursePosFlags.IsLeft;
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			else if ((course.m_StartPosition.m_Flags & (CoursePosFlags.IsRight | CoursePosFlags.IsLeft)) == CoursePosFlags.IsRight)
			{
				course.m_StartPosition.m_Flags &= ~CoursePosFlags.IsRight;
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
			if ((course.m_EndPosition.m_Flags & (CoursePosFlags.IsRight | CoursePosFlags.IsLeft)) == CoursePosFlags.IsLeft)
			{
				course.m_EndPosition.m_Flags &= ~CoursePosFlags.IsLeft;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			else if ((course.m_EndPosition.m_Flags & (CoursePosFlags.IsRight | CoursePosFlags.IsLeft)) == CoursePosFlags.IsRight)
			{
				course.m_EndPosition.m_Flags &= ~CoursePosFlags.IsRight;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
		}

		private float CalculatedInverseWeight(NetCourse course, Game.Net.PlacementFlags placementFlags)
		{
			float num = 0f;
			if ((placementFlags & (Game.Net.PlacementFlags.FlowLeft | Game.Net.PlacementFlags.FlowRight)) != Game.Net.PlacementFlags.None)
			{
				int num2 = math.max(1, Mathf.RoundToInt(course.m_Length * m_WaterSurfaceData.scale.x));
				for (int i = 0; i < num2; i++)
				{
					float t = ((float)i + 0.5f) / (float)num2;
					float3 worldPosition = MathUtils.Position(course.m_Curve, t);
					float3 @float = MathUtils.Tangent(course.m_Curve, t);
					float2 x = WaterUtils.SampleVelocity(ref m_WaterSurfaceData, worldPosition);
					float2 y = math.normalizesafe(MathUtils.Right(@float.xz));
					float num3 = math.dot(x, y);
					num += math.select(num3, 0f - num3, (placementFlags & Game.Net.PlacementFlags.FlowLeft) != 0);
				}
			}
			return num;
		}

		private void FixElevation(ref ControlPoint controlPoint)
		{
			if (m_PlaceableData.HasComponent(m_NetPrefab))
			{
				PlaceableNetData placeableNetData = m_PlaceableData[m_NetPrefab];
				if (controlPoint.m_Elevation < placeableNetData.m_ElevationRange.min)
				{
					controlPoint.m_Position.y += placeableNetData.m_ElevationRange.min - controlPoint.m_Elevation;
					controlPoint.m_Elevation = placeableNetData.m_ElevationRange.min;
					controlPoint.m_OriginalEntity = Entity.Null;
				}
				else if (controlPoint.m_Elevation > placeableNetData.m_ElevationRange.max)
				{
					controlPoint.m_Position.y += placeableNetData.m_ElevationRange.max - controlPoint.m_Elevation;
					controlPoint.m_Elevation = placeableNetData.m_ElevationRange.max;
					controlPoint.m_OriginalEntity = Entity.Null;
				}
			}
		}

		private void SetHeight(ControlPoint startPoint, ref ControlPoint controlPoint)
		{
			float y = startPoint.m_Position.y;
			controlPoint.m_Position.y = y;
		}

		private void CreateParallelCourses(CreationDefinition definitionData, OwnerDefinition ownerDefinition, NetCourse courseData, NativeParallelHashMap<float4, float3> nodeMap)
		{
			if (courseData.m_StartPosition.m_Position.Equals(courseData.m_EndPosition.m_Position))
			{
				return;
			}
			float num = m_ParallelOffset;
			float elevationLimit = 1f;
			if (m_NetGeometryData.HasComponent(m_NetPrefab))
			{
				NetGeometryData netGeometryData = m_NetGeometryData[m_NetPrefab];
				num += netGeometryData.m_DefaultWidth;
				elevationLimit = netGeometryData.m_ElevationLimit;
			}
			NetCourse netCourse = courseData;
			NetCourse netCourse2 = courseData;
			for (int i = 1; i <= m_ParallelCount.x; i++)
			{
				NetCourse netCourse3 = netCourse;
				netCourse3.m_Curve = NetUtils.OffsetCurveLeftSmooth(netCourse.m_Curve, num);
				float4 key = new float4(netCourse.m_Curve.a, -i);
				if (!nodeMap.TryAdd(key, netCourse3.m_Curve.a))
				{
					netCourse3.m_Curve.a = nodeMap[key];
				}
				key = new float4(netCourse.m_Curve.d, -i);
				if (!nodeMap.TryAdd(key, netCourse3.m_Curve.d))
				{
					netCourse3.m_Curve.d = nodeMap[key];
				}
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(-i);
				CreateParallelCourse(definitionData, ownerDefinition, netCourse, netCourse3, num, elevationLimit, (i & 1) != 0, i == m_ParallelCount.x, isRight: false, 0, ref random);
				netCourse = netCourse3;
			}
			for (int j = 1; j <= m_ParallelCount.y; j++)
			{
				NetCourse netCourse4 = netCourse2;
				netCourse4.m_Curve = NetUtils.OffsetCurveLeftSmooth(netCourse2.m_Curve, 0f - num);
				float4 key2 = new float4(netCourse2.m_Curve.a, j);
				if (!nodeMap.TryAdd(key2, netCourse4.m_Curve.a))
				{
					netCourse4.m_Curve.a = nodeMap[key2];
				}
				key2 = new float4(netCourse2.m_Curve.d, j);
				if (!nodeMap.TryAdd(key2, netCourse4.m_Curve.d))
				{
					netCourse4.m_Curve.d = nodeMap[key2];
				}
				Unity.Mathematics.Random random2 = m_RandomSeed.GetRandom(j);
				CreateParallelCourse(definitionData, ownerDefinition, netCourse2, netCourse4, 0f - num, elevationLimit, (j & 1) != 0, isLeft: false, j == m_ParallelCount.y, 0, ref random2);
				netCourse2 = netCourse4;
			}
		}

		private void CreateParallelCourse(CreationDefinition definitionData, OwnerDefinition ownerDefinition, NetCourse courseData, NetCourse courseData2, float parallelOffset, float elevationLimit, bool invert, bool isLeft, bool isRight, int level, ref Unity.Mathematics.Random random)
		{
			float num = math.abs(parallelOffset);
			if (++level >= 10 || math.distance(courseData2.m_Curve.a.xz, courseData2.m_Curve.d.xz) < num * 2f)
			{
				CreateParallelCourse(definitionData, ownerDefinition, courseData2, elevationLimit, invert, isLeft, isRight, ref random);
				return;
			}
			float3 @float = MathUtils.Position(courseData2.m_Curve, 0.5f);
			float t;
			float num2 = MathUtils.Distance(courseData.m_Curve.xz, @float.xz, out t);
			float3 float2 = MathUtils.Position(courseData.m_Curve, t);
			float3 value = MathUtils.Tangent(courseData.m_Curve, t);
			value = MathUtils.Normalize(value, value.xz);
			float2 float3 = value.zx * new float2(0f - parallelOffset, parallelOffset);
			if (math.abs(num2 - num) > num * 0.02f || math.dot(float3, @float.xz - float2.xz) < 0f)
			{
				float2.xz += float3;
				float3 value2 = MathUtils.StartTangent(courseData2.m_Curve);
				float3 value3 = MathUtils.EndTangent(courseData2.m_Curve);
				value2 = MathUtils.Normalize(value2, value2.xz);
				value3 = MathUtils.Normalize(value3, value3.xz);
				NetCourse courseData3 = courseData2;
				NetCourse courseData4 = courseData2;
				courseData3.m_Curve = NetUtils.FitCurve(courseData2.m_Curve.a, value2, value, float2);
				courseData3.m_EndPosition.m_Flags &= ~(CoursePosFlags.IsFirst | CoursePosFlags.IsLast);
				courseData3.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(value);
				courseData4.m_Curve = NetUtils.FitCurve(float2, value, value3, courseData2.m_Curve.d);
				courseData4.m_StartPosition.m_Flags &= ~(CoursePosFlags.IsFirst | CoursePosFlags.IsLast);
				courseData4.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(value);
				float num3 = MathUtils.Length(courseData3.m_Curve);
				float num4 = MathUtils.Length(courseData4.m_Curve);
				t = math.saturate(num3 / (num3 + num4));
				courseData3.m_EndPosition.m_Elevation = math.lerp(courseData2.m_StartPosition.m_Elevation, courseData2.m_EndPosition.m_Elevation, t);
				courseData3.m_EndPosition.m_ParentMesh = math.select(courseData2.m_StartPosition.m_ParentMesh, -1, courseData2.m_StartPosition.m_ParentMesh != courseData2.m_EndPosition.m_ParentMesh);
				courseData4.m_StartPosition.m_Elevation = math.lerp(courseData2.m_StartPosition.m_Elevation, courseData2.m_EndPosition.m_Elevation, t);
				courseData4.m_StartPosition.m_ParentMesh = math.select(courseData2.m_StartPosition.m_ParentMesh, -1, courseData2.m_StartPosition.m_ParentMesh != courseData2.m_EndPosition.m_ParentMesh);
				CreateParallelCourse(definitionData, ownerDefinition, courseData, courseData3, parallelOffset, elevationLimit, invert, isLeft, isRight, level, ref random);
				CreateParallelCourse(definitionData, ownerDefinition, courseData, courseData4, parallelOffset, elevationLimit, invert, isLeft, isRight, level, ref random);
			}
			else
			{
				CreateParallelCourse(definitionData, ownerDefinition, courseData2, elevationLimit, invert, isLeft, isRight, ref random);
			}
		}

		private void CreateParallelCourse(CreationDefinition definitionData, OwnerDefinition ownerDefinition, NetCourse courseData, float elevationLimit, bool invert, bool isLeft, bool isRight, ref Unity.Mathematics.Random random)
		{
			LinearizeElevation(ref courseData.m_Curve);
			courseData.m_StartPosition.m_Position = courseData.m_Curve.a;
			courseData.m_StartPosition.m_Entity = Entity.Null;
			courseData.m_StartPosition.m_SplitPosition = 0f;
			courseData.m_StartPosition.m_Flags &= ~(CoursePosFlags.IsRight | CoursePosFlags.IsLeft);
			courseData.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
			courseData.m_EndPosition.m_Position = courseData.m_Curve.d;
			courseData.m_EndPosition.m_Entity = Entity.Null;
			courseData.m_EndPosition.m_SplitPosition = 0f;
			courseData.m_EndPosition.m_Flags &= ~(CoursePosFlags.IsRight | CoursePosFlags.IsLeft);
			courseData.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
			courseData.m_Length = MathUtils.Length(courseData.m_Curve);
			courseData.m_FixedIndex = -1;
			if (courseData.m_StartPosition.m_Elevation.x > 0f - elevationLimit && courseData.m_StartPosition.m_Elevation.x < elevationLimit)
			{
				courseData.m_StartPosition.m_Flags |= CoursePosFlags.FreeHeight;
			}
			if (courseData.m_EndPosition.m_Elevation.x > 0f - elevationLimit && courseData.m_EndPosition.m_Elevation.x < elevationLimit)
			{
				courseData.m_EndPosition.m_Flags |= CoursePosFlags.FreeHeight;
			}
			if (invert)
			{
				courseData.m_Curve = MathUtils.Invert(courseData.m_Curve);
				CommonUtils.Swap(ref courseData.m_StartPosition.m_Position, ref courseData.m_EndPosition.m_Position);
				CommonUtils.Swap(ref courseData.m_StartPosition.m_Rotation, ref courseData.m_EndPosition.m_Rotation);
				CommonUtils.Swap(ref courseData.m_StartPosition.m_Elevation, ref courseData.m_EndPosition.m_Elevation);
				CommonUtils.Swap(ref courseData.m_StartPosition.m_Flags, ref courseData.m_EndPosition.m_Flags);
				CommonUtils.Swap(ref courseData.m_StartPosition.m_ParentMesh, ref courseData.m_EndPosition.m_ParentMesh);
				quaternion a = quaternion.RotateY(MathF.PI);
				courseData.m_StartPosition.m_Rotation = math.mul(a, courseData.m_StartPosition.m_Rotation);
				courseData.m_EndPosition.m_Rotation = math.mul(a, courseData.m_EndPosition.m_Rotation);
			}
			if (isLeft || isRight)
			{
				if (invert == isLeft)
				{
					courseData.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
					courseData.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
				}
				else
				{
					courseData.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
					courseData.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
				}
			}
			definitionData.m_RandomSeed ^= random.NextInt();
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, definitionData);
			m_CommandBuffer.AddComponent(e, default(Updated));
			m_CommandBuffer.AddComponent(e, courseData);
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
		}

		private void CreateSimpleCurve(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions, int middleIndex)
		{
			ControlPoint controlPoint = m_ControlPoints[0];
			ControlPoint controlPoint2 = m_ControlPoints[middleIndex];
			ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];
			FixElevation(ref controlPoint);
			FixElevation(ref controlPoint2);
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(m_NetPrefab))
			{
				netGeometryData = m_NetGeometryData[m_NetPrefab];
				if (netGeometryData.m_MaxSlopeSteepness == 0f)
				{
					SetHeight(controlPoint, ref controlPoint2);
					SetHeight(controlPoint, ref controlPoint3);
				}
			}
			else
			{
				netGeometryData.m_DefaultWidth = 0.02f;
			}
			float t;
			float num = MathUtils.Distance(new Line2.Segment(controlPoint.m_Position.xz, controlPoint2.m_Position.xz), controlPoint3.m_Position.xz, out t);
			float t2;
			float num2 = MathUtils.Distance(new Line2.Segment(controlPoint3.m_Position.xz, controlPoint2.m_Position.xz), controlPoint.m_Position.xz, out t2);
			if (num <= netGeometryData.m_DefaultWidth * 0.75f && num <= num2)
			{
				t *= 0.5f + num / netGeometryData.m_DefaultWidth * (2f / 3f);
				controlPoint2.m_Position = math.lerp(controlPoint.m_Position, controlPoint2.m_Position, t);
			}
			else if (num2 <= netGeometryData.m_DefaultWidth * 0.75f)
			{
				t2 *= 0.5f + num2 / netGeometryData.m_DefaultWidth * (2f / 3f);
				controlPoint2.m_Position = math.lerp(controlPoint3.m_Position, controlPoint2.m_Position, t2);
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			CreationDefinition creationDefinition = new CreationDefinition
			{
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab,
				m_RandomSeed = random.NextInt()
			};
			creationDefinition.m_Flags |= CreationFlags.SubElevation;
			NetCourse course = new NetCourse
			{
				m_Curve = NetUtils.FitCurve(new Line3.Segment(controlPoint.m_Position, controlPoint2.m_Position), new Line3.Segment(controlPoint3.m_Position, controlPoint2.m_Position))
			};
			LinearizeElevation(ref course.m_Curve);
			course.m_StartPosition = GetCoursePos(course.m_Curve, controlPoint, 0f);
			course.m_EndPosition = GetCoursePos(course.m_Curve, controlPoint3, 1f);
			course.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			course.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			if (course.m_StartPosition.m_Position.Equals(course.m_EndPosition.m_Position) && course.m_StartPosition.m_Entity.Equals(course.m_EndPosition.m_Entity))
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			bool2 x = m_ParallelCount > 0;
			if (!x.x)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
			if (!x.y)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			course.m_Length = MathUtils.Length(course.m_Curve);
			course.m_FixedIndex = -1;
			if (m_PlaceableData.HasComponent(m_NetPrefab))
			{
				PlaceableNetData placeableNetData = m_PlaceableData[m_NetPrefab];
				if (CalculatedInverseWeight(course, placeableNetData.m_PlacementFlags) < 0f)
				{
					InvertCourse(ref course);
				}
			}
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, creationDefinition);
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, course.m_StartPosition, course.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(course, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				course.m_StartPosition.m_ParentMesh = -1;
				course.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, course);
			if (math.any(x))
			{
				NativeParallelHashMap<float4, float3> nodeMap = new NativeParallelHashMap<float4, float3>(100, Allocator.Temp);
				CreateParallelCourses(creationDefinition, ownerDefinition, course, nodeMap);
				nodeMap.Dispose();
			}
		}

		private float GetCutPosition(NetGeometryData netGeometryData, float length, float t)
		{
			if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
			{
				return math.saturate(MathUtils.Snap(length * t + 0.16f, 8f) / length);
			}
			return t;
		}

		private void CreateGrid(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions)
		{
			ControlPoint controlPoint = m_ControlPoints[0];
			ControlPoint controlPoint2 = m_ControlPoints[1];
			ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];
			FixElevation(ref controlPoint);
			FixElevation(ref controlPoint2);
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(m_NetPrefab))
			{
				netGeometryData = m_NetGeometryData[m_NetPrefab];
				if (netGeometryData.m_MaxSlopeSteepness == 0f)
				{
					SetHeight(controlPoint, ref controlPoint2);
					SetHeight(controlPoint, ref controlPoint3);
				}
			}
			bool flag = math.dot(controlPoint3.m_Position.xz - controlPoint2.m_Position.xz, MathUtils.Right(controlPoint2.m_Direction)) > 0f;
			flag ^= math.dot(controlPoint3.m_Position.xz - controlPoint.m_Position.xz, controlPoint2.m_Direction) < 0f;
			float3 @float = new float3(controlPoint2.m_Direction.x, 0f, controlPoint2.m_Direction.y);
			controlPoint2.m_Position = controlPoint.m_Position + @float * math.dot(controlPoint3.m_Position - controlPoint.m_Position, @float);
			float2 float2 = new float2(math.distance(controlPoint.m_Position.xz, controlPoint2.m_Position.xz), math.distance(controlPoint2.m_Position.xz, controlPoint3.m_Position.xz));
			float2 float3 = (((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) == 0) ? (netGeometryData.m_DefaultWidth * new float2(16f, 8f)) : ((float)ZoneUtils.GetCellWidth(netGeometryData.m_DefaultWidth) * 8f + new float2(192f, 96f)));
			float2 float4 = math.max(1f, math.ceil((float2 - 0.16f) / float3));
			float3 = float2 / float4;
			float4 -= math.select(0f, 1f, float3 < netGeometryData.m_DefaultWidth + 3f);
			int2 @int = new int2(Mathf.RoundToInt(float4.x), Mathf.RoundToInt(float4.y));
			if (@int.y == 0)
			{
				CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
				return;
			}
			if (@int.x == 0)
			{
				CreateStraightLine(ref ownerDefinitions, new int2(1, m_ControlPoints.Length - 1));
				return;
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			CoursePos coursePos = GetCoursePos(new Bezier4x3(controlPoint.m_Position, controlPoint.m_Position, controlPoint.m_Position, controlPoint.m_Position), controlPoint, 0f);
			CoursePos coursePos2 = GetCoursePos(new Bezier4x3(controlPoint3.m_Position, controlPoint3.m_Position, controlPoint3.m_Position, controlPoint3.m_Position), controlPoint3, 1f);
			coursePos.m_Flags |= CoursePosFlags.IsFirst;
			coursePos2.m_Flags |= CoursePosFlags.IsLast;
			OwnerDefinition ownerDefinition2;
			bool ownerDefinition = GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, coursePos, coursePos2, out ownerDefinition2);
			float length = math.distance(controlPoint.m_Position.xz, controlPoint2.m_Position.xz);
			float length2 = math.distance(controlPoint2.m_Position.xz, controlPoint3.m_Position.xz);
			Line3.Segment line = new Line3.Segment(controlPoint.m_Position, controlPoint.m_Position + controlPoint3.m_Position - controlPoint2.m_Position);
			Line3.Segment line2 = new Line3.Segment(controlPoint2.m_Position, controlPoint3.m_Position);
			Line1.Segment line3 = new Line1.Segment(controlPoint.m_Elevation, controlPoint2.m_Elevation);
			Line1.Segment line4 = new Line1.Segment(controlPoint2.m_Elevation, controlPoint3.m_Elevation);
			int2 int2 = default(int2);
			int2.y = 0;
			Line3.Segment line5 = default(Line3.Segment);
			Line3.Segment line6 = default(Line3.Segment);
			Line1.Segment line7 = default(Line1.Segment);
			Line1.Segment line8 = default(Line1.Segment);
			while (int2.y <= @int.y)
			{
				float cutPosition = GetCutPosition(netGeometryData, length2, (float)int2.y / (float)@int.y);
				float cutPosition2 = GetCutPosition(netGeometryData, length2, (float)(int2.y + 1) / (float)@int.y);
				line5.a = MathUtils.Position(line, cutPosition);
				line5.b = MathUtils.Position(line2, cutPosition);
				line6.a = MathUtils.Position(line, cutPosition2);
				line6.b = MathUtils.Position(line2, cutPosition2);
				line7.a = MathUtils.Position(line3, cutPosition);
				line7.b = MathUtils.Position(line4, cutPosition);
				line8.a = MathUtils.Position(line3, cutPosition2);
				line8.b = MathUtils.Position(line4, cutPosition2);
				int2.x = 0;
				while (int2.x < @int.x)
				{
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Prefab = m_NetPrefab,
						m_SubPrefab = m_LanePrefab,
						m_RandomSeed = random.NextInt()
					};
					component.m_Flags |= CreationFlags.SubElevation;
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, default(Updated));
					bool num = math.all(int2 == 0);
					bool flag2 = math.all(new int2(int2.x + 1, int2.y) == @int);
					bool flag3 = (int2.y & 1) == 1 || int2.y == @int.y;
					ControlPoint controlPoint4;
					if (num)
					{
						controlPoint4 = controlPoint;
					}
					else
					{
						float cutPosition3 = GetCutPosition(netGeometryData, length, (float)int2.x / (float)@int.x);
						controlPoint4 = new ControlPoint
						{
							m_Rotation = controlPoint.m_Rotation,
							m_Position = MathUtils.Position(line5, cutPosition3),
							m_Elevation = MathUtils.Position(line7, cutPosition3)
						};
					}
					ControlPoint controlPoint5;
					if (flag2)
					{
						controlPoint5 = controlPoint3;
					}
					else
					{
						float cutPosition4 = GetCutPosition(netGeometryData, length, (float)(int2.x + 1) / (float)@int.x);
						controlPoint5 = new ControlPoint
						{
							m_Rotation = controlPoint.m_Rotation,
							m_Position = MathUtils.Position(line5, cutPosition4),
							m_Elevation = MathUtils.Position(line7, cutPosition4)
						};
					}
					NetCourse netCourse = default(NetCourse);
					netCourse.m_Curve = NetUtils.StraightCurve(controlPoint4.m_Position, controlPoint5.m_Position);
					netCourse.m_StartPosition = GetCoursePos(netCourse.m_Curve, controlPoint4, 0f);
					netCourse.m_EndPosition = GetCoursePos(netCourse.m_Curve, controlPoint5, 1f);
					if (!ownerDefinition)
					{
						netCourse.m_StartPosition.m_ParentMesh = -1;
						netCourse.m_EndPosition.m_ParentMesh = -1;
					}
					netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsGrid;
					netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsGrid;
					if (int2.y != 0)
					{
						netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
						netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
					}
					if (!num)
					{
						netCourse.m_StartPosition.m_Flags |= CoursePosFlags.FreeHeight;
					}
					if (!flag2)
					{
						netCourse.m_EndPosition.m_Flags |= CoursePosFlags.FreeHeight;
					}
					if (int2.y == 0 || int2.y == @int.y)
					{
						if (int2.x == 0)
						{
							netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
						}
						if (int2.x + 1 == @int.x)
						{
							netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
						}
						netCourse.m_StartPosition.m_Flags |= (CoursePosFlags)(flag ? 32 : 16);
						netCourse.m_EndPosition.m_Flags |= (CoursePosFlags)(flag ? 32 : 16);
					}
					if (flag3)
					{
						netCourse.m_Curve = MathUtils.Invert(netCourse.m_Curve);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_Entity, ref netCourse.m_EndPosition.m_Entity);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_SplitPosition, ref netCourse.m_EndPosition.m_SplitPosition);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_Position, ref netCourse.m_EndPosition.m_Position);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_Rotation, ref netCourse.m_EndPosition.m_Rotation);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_Elevation, ref netCourse.m_EndPosition.m_Elevation);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_Flags, ref netCourse.m_EndPosition.m_Flags);
						CommonUtils.Swap(ref netCourse.m_StartPosition.m_ParentMesh, ref netCourse.m_EndPosition.m_ParentMesh);
						quaternion a = quaternion.RotateY(MathF.PI);
						netCourse.m_StartPosition.m_Rotation = math.mul(a, netCourse.m_StartPosition.m_Rotation);
						netCourse.m_EndPosition.m_Rotation = math.mul(a, netCourse.m_EndPosition.m_Rotation);
					}
					netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
					netCourse.m_FixedIndex = -1;
					m_CommandBuffer.AddComponent(e, netCourse);
					if (ownerDefinition2.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(e, ownerDefinition2);
						if (m_EditorMode && GetLocalCurve(netCourse, ownerDefinition2, out var localCurveCache))
						{
							m_CommandBuffer.AddComponent(e, localCurveCache);
						}
					}
					int2.x++;
				}
				if (int2.y != @int.y)
				{
					int2.x = 0;
					while (int2.x <= @int.x)
					{
						Entity e2 = m_CommandBuffer.CreateEntity();
						CreationDefinition component2 = new CreationDefinition
						{
							m_Prefab = m_NetPrefab,
							m_SubPrefab = m_LanePrefab,
							m_RandomSeed = random.NextInt()
						};
						component2.m_Flags |= CreationFlags.SubElevation;
						m_CommandBuffer.AddComponent(e2, component2);
						m_CommandBuffer.AddComponent(e2, default(Updated));
						bool num2 = math.all(int2 == 0);
						bool flag4 = math.all(new int2(int2.x, int2.y + 1) == @int);
						bool flag5 = ((@int.x - int2.x) & 1) == 1 || int2.x == 0;
						float cutPosition5 = GetCutPosition(netGeometryData, length, (float)int2.x / (float)@int.x);
						ControlPoint controlPoint6 = ((!num2) ? new ControlPoint
						{
							m_Rotation = controlPoint.m_Rotation,
							m_Position = MathUtils.Position(line5, cutPosition5),
							m_Elevation = MathUtils.Position(line7, cutPosition5)
						} : controlPoint);
						ControlPoint controlPoint7 = ((!flag4) ? new ControlPoint
						{
							m_Rotation = controlPoint.m_Rotation,
							m_Position = MathUtils.Position(line6, cutPosition5),
							m_Elevation = MathUtils.Position(line8, cutPosition5)
						} : controlPoint3);
						NetCourse netCourse2 = default(NetCourse);
						netCourse2.m_Curve = NetUtils.StraightCurve(controlPoint6.m_Position, controlPoint7.m_Position);
						netCourse2.m_StartPosition = GetCoursePos(netCourse2.m_Curve, controlPoint6, 0f);
						netCourse2.m_EndPosition = GetCoursePos(netCourse2.m_Curve, controlPoint7, 1f);
						if (!ownerDefinition)
						{
							netCourse2.m_StartPosition.m_ParentMesh = -1;
							netCourse2.m_EndPosition.m_ParentMesh = -1;
						}
						netCourse2.m_StartPosition.m_Flags |= CoursePosFlags.IsGrid;
						netCourse2.m_EndPosition.m_Flags |= CoursePosFlags.IsGrid;
						if (int2.x != @int.x)
						{
							netCourse2.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
							netCourse2.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
						}
						if (!num2)
						{
							netCourse2.m_StartPosition.m_Flags |= CoursePosFlags.FreeHeight;
						}
						if (!flag4)
						{
							netCourse2.m_EndPosition.m_Flags |= CoursePosFlags.FreeHeight;
						}
						if (int2.x == 0 || int2.x == @int.x)
						{
							if (int2.y == 0)
							{
								netCourse2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
							}
							if (int2.y + 1 == @int.y)
							{
								netCourse2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
							}
							netCourse2.m_StartPosition.m_Flags |= (CoursePosFlags)(flag ? 32 : 16);
							netCourse2.m_EndPosition.m_Flags |= (CoursePosFlags)(flag ? 32 : 16);
						}
						if (flag5)
						{
							netCourse2.m_Curve = MathUtils.Invert(netCourse2.m_Curve);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_Entity, ref netCourse2.m_EndPosition.m_Entity);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_SplitPosition, ref netCourse2.m_EndPosition.m_SplitPosition);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_Position, ref netCourse2.m_EndPosition.m_Position);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_Rotation, ref netCourse2.m_EndPosition.m_Rotation);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_Elevation, ref netCourse2.m_EndPosition.m_Elevation);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_Flags, ref netCourse2.m_EndPosition.m_Flags);
							CommonUtils.Swap(ref netCourse2.m_StartPosition.m_ParentMesh, ref netCourse2.m_EndPosition.m_ParentMesh);
							quaternion a2 = quaternion.RotateY(MathF.PI);
							netCourse2.m_StartPosition.m_Rotation = math.mul(a2, netCourse2.m_StartPosition.m_Rotation);
							netCourse2.m_EndPosition.m_Rotation = math.mul(a2, netCourse2.m_EndPosition.m_Rotation);
						}
						netCourse2.m_Length = MathUtils.Length(netCourse2.m_Curve);
						netCourse2.m_FixedIndex = -1;
						m_CommandBuffer.AddComponent(e2, netCourse2);
						if (ownerDefinition2.m_Prefab != Entity.Null)
						{
							m_CommandBuffer.AddComponent(e2, ownerDefinition2);
							if (m_EditorMode && GetLocalCurve(netCourse2, ownerDefinition2, out var localCurveCache2))
							{
								m_CommandBuffer.AddComponent(e2, localCurveCache2);
							}
						}
						int2.x++;
					}
				}
				int2.y++;
			}
		}

		private void CreateComplexCurve(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions)
		{
			ControlPoint controlPoint = m_ControlPoints[0];
			ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 3];
			ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 2];
			ControlPoint controlPoint4 = m_ControlPoints[m_ControlPoints.Length - 1];
			FixElevation(ref controlPoint);
			FixElevation(ref controlPoint2);
			FixElevation(ref controlPoint3);
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(m_NetPrefab))
			{
				netGeometryData = m_NetGeometryData[m_NetPrefab];
				if (netGeometryData.m_MaxSlopeSteepness == 0f)
				{
					SetHeight(controlPoint, ref controlPoint2);
					SetHeight(controlPoint, ref controlPoint3);
					SetHeight(controlPoint, ref controlPoint4);
				}
			}
			else
			{
				netGeometryData.m_DefaultWidth = 0.02f;
			}
			float t;
			float num = MathUtils.Distance(new Line2.Segment(controlPoint.m_Position.xz, controlPoint2.m_Position.xz), controlPoint3.m_Position.xz, out t);
			float t2;
			float num2 = MathUtils.Distance(new Line2.Segment(controlPoint3.m_Position.xz, controlPoint2.m_Position.xz), controlPoint.m_Position.xz, out t2);
			if (num <= netGeometryData.m_DefaultWidth * 0.75f && num <= num2)
			{
				t *= 0.5f + num / netGeometryData.m_DefaultWidth * (2f / 3f);
				controlPoint2.m_Position = math.lerp(controlPoint.m_Position, controlPoint2.m_Position, t);
			}
			else if (num2 <= netGeometryData.m_DefaultWidth * 0.75f)
			{
				t2 *= 0.5f + num2 / netGeometryData.m_DefaultWidth * (2f / 3f);
				controlPoint2.m_Position = math.lerp(controlPoint3.m_Position, controlPoint2.m_Position, t2);
			}
			float2 @float = controlPoint2.m_Position.xz - controlPoint.m_Position.xz;
			float2 float2 = controlPoint3.m_Position.xz - controlPoint4.m_Position.xz;
			float2 value = @float;
			float2 value2 = float2;
			if (!MathUtils.TryNormalize(ref value))
			{
				CreateSimpleCurve(ref ownerDefinitions, 2);
				return;
			}
			if (!MathUtils.TryNormalize(ref value2))
			{
				CreateSimpleCurve(ref ownerDefinitions, 1);
				return;
			}
			float2 float3 = math.lerp(controlPoint2.m_Position.xz, controlPoint3.m_Position.xz, 0.5f);
			num = MathUtils.Distance(new Line2.Segment(controlPoint2.m_Position.xz, controlPoint3.m_Position.xz), controlPoint4.m_Position.xz, out t);
			if (num <= netGeometryData.m_DefaultWidth * 0.75f)
			{
				t *= 0.5f + num / netGeometryData.m_DefaultWidth * (2f / 3f);
				controlPoint3.m_Position = math.lerp(controlPoint2.m_Position, controlPoint3.m_Position, t);
				float2 = controlPoint3.m_Position.xz - controlPoint4.m_Position.xz;
				value2 = float2;
				if (!MathUtils.TryNormalize(ref value2))
				{
					CreateSimpleCurve(ref ownerDefinitions, 1);
					return;
				}
			}
			Bezier4x3 curve = new Bezier4x3(controlPoint.m_Position, controlPoint2.m_Position, controlPoint3.m_Position, controlPoint4.m_Position);
			float2 xz = MathUtils.Position(curve, 0.5f).xz;
			float2 float4 = float3 - xz;
			float num3 = math.dot(value, value2);
			float2 float8;
			if (math.abs(num3) < 0.999f)
			{
				float2 float5 = value2.yx * value;
				float2 float6 = float4.yx * value;
				float2 float7 = value2.yx * float4;
				float num4 = (float5.x - float5.y) * 0.375f;
				float8 = new float2(float7.x - float7.y, float6.x - float6.y) / num4;
				float8 *= math.abs(num3);
			}
			else
			{
				float2 float9 = new float2(math.length(@float), math.length(float2));
				float8 = ((!(num3 > 0f)) ? (float9 / 3f) : (new float2(math.dot(float4, value), math.dot(float4, value2)) * (float9 / (math.csum(float9) * 0.375f))));
			}
			curve.b.xz += value * float8.x;
			curve.c.xz += value2 * float8.y;
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			CreationDefinition creationDefinition = new CreationDefinition
			{
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab,
				m_RandomSeed = random.NextInt()
			};
			creationDefinition.m_Flags |= CreationFlags.SubElevation;
			NetCourse course = new NetCourse
			{
				m_Curve = curve
			};
			LinearizeElevation(ref course.m_Curve);
			course.m_StartPosition = GetCoursePos(course.m_Curve, controlPoint, 0f);
			course.m_EndPosition = GetCoursePos(course.m_Curve, controlPoint4, 1f);
			course.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			course.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			if (course.m_StartPosition.m_Position.Equals(course.m_EndPosition.m_Position) && course.m_StartPosition.m_Entity.Equals(course.m_EndPosition.m_Entity))
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			bool2 x = m_ParallelCount > 0;
			if (!x.x)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
			if (!x.y)
			{
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
				course.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			course.m_Length = MathUtils.Length(course.m_Curve);
			course.m_FixedIndex = -1;
			if (m_PlaceableData.HasComponent(m_NetPrefab))
			{
				PlaceableNetData placeableNetData = m_PlaceableData[m_NetPrefab];
				if (CalculatedInverseWeight(course, placeableNetData.m_PlacementFlags) < 0f)
				{
					InvertCourse(ref course);
				}
			}
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, creationDefinition);
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, course.m_StartPosition, course.m_EndPosition, out var ownerDefinition))
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
				if (m_EditorMode && GetLocalCurve(course, ownerDefinition, out var localCurveCache))
				{
					m_CommandBuffer.AddComponent(e, localCurveCache);
				}
			}
			else
			{
				course.m_StartPosition.m_ParentMesh = -1;
				course.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e, course);
			if (math.any(x))
			{
				NativeParallelHashMap<float4, float3> nodeMap = new NativeParallelHashMap<float4, float3>(100, Allocator.Temp);
				CreateParallelCourses(creationDefinition, ownerDefinition, course, nodeMap);
				nodeMap.Dispose();
			}
		}

		private void CreateContinuousCurve(ref NativeParallelHashMap<Entity, OwnerDefinition> ownerDefinitions)
		{
			ControlPoint controlPoint = m_ControlPoints[0];
			ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 2];
			ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];
			FixElevation(ref controlPoint);
			FixElevation(ref controlPoint2);
			bool flag = false;
			if (m_NetGeometryData.HasComponent(m_NetPrefab))
			{
				NetGeometryData netGeometryData = m_NetGeometryData[m_NetPrefab];
				flag = (netGeometryData.m_Flags & Game.Net.GeometryFlags.NoCurveSplit) != 0;
				if (netGeometryData.m_MaxSlopeSteepness == 0f)
				{
					SetHeight(controlPoint, ref controlPoint2);
					SetHeight(controlPoint, ref controlPoint3);
				}
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			float3 startTangent = new float3(controlPoint2.m_Direction.x, 0f, controlPoint2.m_Direction.y);
			float3 @float = new float3(controlPoint3.m_Direction.x, 0f, controlPoint3.m_Direction.y);
			float num = math.dot(math.normalizesafe(controlPoint3.m_Position.xz - controlPoint.m_Position.xz), controlPoint2.m_Direction);
			if (math.abs(num) < 0.01f && !flag)
			{
				int2 @int = random.NextInt2();
				CreationDefinition creationDefinition = new CreationDefinition
				{
					m_Prefab = m_NetPrefab,
					m_SubPrefab = m_LanePrefab
				};
				creationDefinition.m_Flags |= CreationFlags.SubElevation;
				NetCourse course = default(NetCourse);
				NetCourse course2 = default(NetCourse);
				float num2 = math.distance(controlPoint.m_Position.xz, controlPoint2.m_Position.xz);
				controlPoint2.m_Direction = MathUtils.Right(controlPoint2.m_Direction);
				if (math.dot(controlPoint3.m_Position.xz - controlPoint.m_Position.xz, controlPoint2.m_Direction) < 0f)
				{
					controlPoint2.m_Direction = -controlPoint2.m_Direction;
				}
				float3 float2 = new float3(controlPoint2.m_Direction.x, 0f, controlPoint2.m_Direction.y);
				controlPoint2.m_OriginalEntity = Entity.Null;
				controlPoint2.m_Position += float2 * num2;
				course.m_Curve = NetUtils.FitCurve(controlPoint.m_Position, startTangent, float2, controlPoint2.m_Position);
				course2.m_Curve = NetUtils.FitCurve(controlPoint2.m_Position, float2, @float, controlPoint3.m_Position);
				MathUtils.Divide(NetUtils.FitCurve(controlPoint.m_Position, startTangent, @float, controlPoint3.m_Position), out var output, out var output2, 0.5f);
				float t = math.abs(num) * 100f;
				course.m_Curve = MathUtils.Lerp(course.m_Curve, output, t);
				course2.m_Curve = MathUtils.Lerp(course2.m_Curve, output2, t);
				LinearizeElevation(ref course.m_Curve, ref course2.m_Curve);
				controlPoint2.m_Position = course.m_Curve.d;
				controlPoint2.m_Elevation = math.lerp(controlPoint.m_Elevation, controlPoint3.m_Elevation, 0.5f);
				course.m_StartPosition = GetCoursePos(course.m_Curve, controlPoint, 0f);
				course.m_EndPosition = GetCoursePos(course.m_Curve, controlPoint2, 1f);
				course2.m_StartPosition = GetCoursePos(course2.m_Curve, controlPoint2, 0f);
				course2.m_EndPosition = GetCoursePos(course2.m_Curve, controlPoint3, 1f);
				course.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
				course.m_EndPosition.m_Flags |= CoursePosFlags.FreeHeight;
				course2.m_StartPosition.m_Flags |= CoursePosFlags.FreeHeight;
				course2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
				bool2 x = m_ParallelCount > 0;
				if (!x.x)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
					course.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
					course2.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
					course2.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
				}
				if (!x.y)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
					course.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
					course2.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
					course2.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
				}
				course.m_Length = MathUtils.Length(course.m_Curve);
				course2.m_Length = MathUtils.Length(course2.m_Curve);
				course.m_FixedIndex = -1;
				course2.m_FixedIndex = -1;
				if (m_PlaceableData.HasComponent(m_NetPrefab))
				{
					PlaceableNetData placeableNetData = m_PlaceableData[m_NetPrefab];
					if (CalculatedInverseWeight(course, placeableNetData.m_PlacementFlags) + CalculatedInverseWeight(course2, placeableNetData.m_PlacementFlags) < 0f)
					{
						InvertCourse(ref course);
						InvertCourse(ref course2);
					}
				}
				Entity e = m_CommandBuffer.CreateEntity();
				Entity e2 = m_CommandBuffer.CreateEntity();
				creationDefinition.m_RandomSeed = @int.x;
				m_CommandBuffer.AddComponent(e, creationDefinition);
				creationDefinition.m_RandomSeed = @int.y;
				m_CommandBuffer.AddComponent(e2, creationDefinition);
				m_CommandBuffer.AddComponent(e, default(Updated));
				m_CommandBuffer.AddComponent(e2, default(Updated));
				if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, course.m_StartPosition, course2.m_EndPosition, out var ownerDefinition))
				{
					m_CommandBuffer.AddComponent(e, ownerDefinition);
					m_CommandBuffer.AddComponent(e2, ownerDefinition);
					if (m_EditorMode && GetLocalCurve(course, ownerDefinition, out var localCurveCache))
					{
						m_CommandBuffer.AddComponent(e, localCurveCache);
					}
					if (m_EditorMode && GetLocalCurve(course2, ownerDefinition, out var localCurveCache2))
					{
						m_CommandBuffer.AddComponent(e2, localCurveCache2);
					}
				}
				else
				{
					course.m_StartPosition.m_ParentMesh = -1;
					course.m_EndPosition.m_ParentMesh = -1;
					course2.m_StartPosition.m_ParentMesh = -1;
					course2.m_EndPosition.m_ParentMesh = -1;
				}
				m_CommandBuffer.AddComponent(e, course);
				m_CommandBuffer.AddComponent(e2, course2);
				if (math.any(x))
				{
					NativeParallelHashMap<float4, float3> nodeMap = new NativeParallelHashMap<float4, float3>(100, Allocator.Temp);
					CreateParallelCourses(creationDefinition, ownerDefinition, course, nodeMap);
					CreateParallelCourses(creationDefinition, ownerDefinition, course2, nodeMap);
					nodeMap.Dispose();
				}
				return;
			}
			CreationDefinition creationDefinition2 = new CreationDefinition
			{
				m_Prefab = m_NetPrefab,
				m_SubPrefab = m_LanePrefab,
				m_RandomSeed = random.NextInt()
			};
			creationDefinition2.m_Flags |= CreationFlags.SubElevation;
			NetCourse course3 = default(NetCourse);
			if (num < 0f)
			{
				float3 endPos = controlPoint3.m_Position + @float * num;
				course3.m_Curve = NetUtils.FitCurve(controlPoint.m_Position, startTangent, @float, endPos);
				course3.m_Curve.d = controlPoint3.m_Position;
			}
			else
			{
				course3.m_Curve = NetUtils.FitCurve(controlPoint.m_Position, startTangent, @float, controlPoint3.m_Position);
			}
			LinearizeElevation(ref course3.m_Curve);
			course3.m_StartPosition = GetCoursePos(course3.m_Curve, controlPoint, 0f);
			course3.m_EndPosition = GetCoursePos(course3.m_Curve, controlPoint3, 1f);
			course3.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			course3.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			if (course3.m_StartPosition.m_Position.Equals(course3.m_EndPosition.m_Position) && course3.m_StartPosition.m_Entity.Equals(course3.m_EndPosition.m_Entity))
			{
				course3.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				course3.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			bool2 x2 = m_ParallelCount > 0;
			if (!x2.x)
			{
				course3.m_StartPosition.m_Flags |= CoursePosFlags.IsLeft;
				course3.m_EndPosition.m_Flags |= CoursePosFlags.IsLeft;
			}
			if (!x2.y)
			{
				course3.m_StartPosition.m_Flags |= CoursePosFlags.IsRight;
				course3.m_EndPosition.m_Flags |= CoursePosFlags.IsRight;
			}
			course3.m_Length = MathUtils.Length(course3.m_Curve);
			course3.m_FixedIndex = -1;
			if (m_PlaceableData.HasComponent(m_NetPrefab))
			{
				PlaceableNetData placeableNetData2 = m_PlaceableData[m_NetPrefab];
				if (CalculatedInverseWeight(course3, placeableNetData2.m_PlacementFlags) < 0f)
				{
					InvertCourse(ref course3);
				}
			}
			Entity e3 = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e3, creationDefinition2);
			m_CommandBuffer.AddComponent(e3, default(Updated));
			if (GetOwnerDefinition(ref ownerDefinitions, Entity.Null, checkControlPoints: true, course3.m_StartPosition, course3.m_EndPosition, out var ownerDefinition2))
			{
				m_CommandBuffer.AddComponent(e3, ownerDefinition2);
				if (m_EditorMode && GetLocalCurve(course3, ownerDefinition2, out var localCurveCache3))
				{
					m_CommandBuffer.AddComponent(e3, localCurveCache3);
				}
			}
			else
			{
				course3.m_StartPosition.m_ParentMesh = -1;
				course3.m_EndPosition.m_ParentMesh = -1;
			}
			m_CommandBuffer.AddComponent(e3, course3);
			if (math.any(x2))
			{
				NativeParallelHashMap<float4, float3> nodeMap2 = new NativeParallelHashMap<float4, float3>(100, Allocator.Temp);
				CreateParallelCourses(creationDefinition2, ownerDefinition2, course3, nodeMap2);
				nodeMap2.Dispose();
			}
		}

		private void LinearizeElevation(ref Bezier4x3 curve)
		{
			float2 @float = math.lerp(curve.a.y, curve.d.y, new float2(1f / 3f, 2f / 3f));
			curve.b.y = @float.x;
			curve.c.y = @float.y;
		}

		private void LinearizeElevation(ref Bezier4x3 curve1, ref Bezier4x3 curve2)
		{
			curve1.d.y = (curve2.a.y = math.lerp(curve1.a.y, curve2.d.y, 0.5f));
			LinearizeElevation(ref curve1);
			LinearizeElevation(ref curve2);
		}

		private CoursePos GetCoursePos(Bezier4x3 curve, ControlPoint controlPoint, float courseDelta)
		{
			CoursePos result = default(CoursePos);
			if (controlPoint.m_OriginalEntity != Entity.Null)
			{
				if (m_EdgeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					if (controlPoint.m_CurvePosition <= 0f)
					{
						result.m_Entity = m_EdgeData[controlPoint.m_OriginalEntity].m_Start;
						result.m_SplitPosition = 0f;
					}
					else if (controlPoint.m_CurvePosition >= 1f)
					{
						result.m_Entity = m_EdgeData[controlPoint.m_OriginalEntity].m_End;
						result.m_SplitPosition = 1f;
					}
					else
					{
						result.m_Entity = controlPoint.m_OriginalEntity;
						result.m_SplitPosition = controlPoint.m_CurvePosition;
					}
				}
				else if (m_NodeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					result.m_Entity = controlPoint.m_OriginalEntity;
					result.m_SplitPosition = controlPoint.m_CurvePosition;
				}
			}
			result.m_Position = controlPoint.m_Position;
			result.m_Elevation = controlPoint.m_Elevation;
			result.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(curve, courseDelta));
			result.m_CourseDelta = courseDelta;
			result.m_ParentMesh = controlPoint.m_ElementIndex.x;
			Entity entity = controlPoint.m_OriginalEntity;
			while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(entity) && !m_ExtensionData.HasComponent(entity))
			{
				Edge componentData;
				LocalTransformCache componentData2;
				LocalTransformCache componentData3;
				if (m_LocalTransformCacheData.HasComponent(entity))
				{
					result.m_ParentMesh = m_LocalTransformCacheData[entity].m_ParentMesh;
				}
				else if (m_EdgeData.TryGetComponent(entity, out componentData) && m_LocalTransformCacheData.TryGetComponent(componentData.m_Start, out componentData2) && m_LocalTransformCacheData.TryGetComponent(componentData.m_End, out componentData3))
				{
					result.m_ParentMesh = math.select(componentData2.m_ParentMesh, -1, componentData2.m_ParentMesh != componentData3.m_ParentMesh);
				}
				entity = m_OwnerData[entity].m_Owner;
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadComposition> __Game_Prefabs_RoadComposition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> __Game_Prefabs_NetCompositionArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> __Game_Prefabs_AuxiliaryNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<FixedNetElement> __Game_Prefabs_FixedNetElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_RoadComposition_RO_ComponentLookup = state.GetComponentLookup<RoadComposition>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Prefabs_NetCompositionArea_RO_BufferLookup = state.GetBufferLookup<NetCompositionArea>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_AuxiliaryNet_RO_BufferLookup = state.GetBufferLookup<AuxiliaryNet>(isReadOnly: true);
			__Game_Prefabs_FixedNetElement_RO_BufferLookup = state.GetBufferLookup<FixedNetElement>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	public const string kToolID = "Net Tool";

	private bool m_LoadingPreferences;

	private Mode m_Mode;

	private float m_Elevation;

	private float m_LastElevation;

	private float m_ElevationStep;

	private int m_ParallelCount;

	private float m_ParallelOffset;

	private bool m_Underground;

	private Snap m_SelectedSnap;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private AudioManager m_AudioManager;

	private NetInitializeSystem m_NetInitializeSystem;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_NodeQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_ContainerQuery;

	private IProxyAction m_DowngradeNetEdge;

	private IProxyAction m_PlaceNetControlPoint;

	private IProxyAction m_PlaceNetEdge;

	private IProxyAction m_PlaceNetNode;

	private IProxyAction m_ReplaceNetEdge;

	private IProxyAction m_UndoNetControlPoint;

	private IProxyAction m_UpgradeNetEdge;

	private IProxyAction m_DiscardUpgrade;

	private IProxyAction m_DiscardDowngrade;

	private IProxyAction m_DiscardReplace;

	private IProxyAction m_IncorrectApply;

	private bool m_ApplyBlocked;

	private NativeList<ControlPoint> m_ControlPoints;

	private NativeList<SnapLine> m_SnapLines;

	private NativeList<UpgradeState> m_UpgradeStates;

	private NativeReference<Entity> m_StartEntity;

	private NativeReference<Entity> m_LastSnappedEntity;

	private NativeReference<int> m_LastControlPointsAngle;

	private NativeReference<AppliedUpgrade> m_AppliedUpgrade;

	private ControlPoint m_LastRaycastPoint;

	private ControlPoint m_ApplyStartPoint;

	private State m_State;

	private Bounds1 m_LastElevationRange;

	private Mode m_LastActualMode;

	private float m_ApplyTimer;

	private NetPrefab m_Prefab;

	private NetPrefab m_SelectedPrefab;

	private NetLanePrefab m_LanePrefab;

	private bool m_AllowUndergroundReplace;

	private bool m_ForceCancel;

	private RandomSeed m_RandomSeed;

	private NetToolPreferences m_DefaultToolPreferences;

	private Dictionary<Entity, NetToolPreferences> m_ToolPreferences;

	private TypeHandle __TypeHandle;

	public override string toolID => "Net Tool";

	public override int uiModeIndex => (int)actualMode;

	public Mode mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			if (value != m_Mode)
			{
				m_Mode = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public Mode actualMode
	{
		get
		{
			if (upgradeOnly)
			{
				return Mode.Replace;
			}
			switch (mode)
			{
			case Mode.Grid:
				if (!allowGrid)
				{
					return Mode.Straight;
				}
				return mode;
			case Mode.Replace:
				if (!allowReplace)
				{
					return Mode.Straight;
				}
				return mode;
			case Mode.Point:
				if (!m_ToolSystem.actionMode.IsEditor())
				{
					return Mode.Straight;
				}
				return mode;
			default:
				return mode;
			}
		}
	}

	public float elevation
	{
		get
		{
			return m_Elevation;
		}
		set
		{
			if (value != m_Elevation)
			{
				m_Elevation = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public float elevationStep
	{
		get
		{
			return m_ElevationStep;
		}
		set
		{
			if (value != m_ElevationStep)
			{
				m_ElevationStep = value;
				SaveToolPreferences();
			}
		}
	}

	public int parallelCount
	{
		get
		{
			return m_ParallelCount;
		}
		set
		{
			if (value != m_ParallelCount)
			{
				m_ParallelCount = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public int actualParallelCount
	{
		get
		{
			if (!allowParallel || (allowGrid && mode == Mode.Grid))
			{
				return 0;
			}
			return parallelCount;
		}
	}

	public float parallelOffset
	{
		get
		{
			return m_ParallelOffset;
		}
		set
		{
			if (value != m_ParallelOffset)
			{
				m_ParallelOffset = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public bool underground
	{
		get
		{
			return m_Underground;
		}
		set
		{
			if (value != m_Underground)
			{
				m_Underground = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public override bool allowUnderground
	{
		get
		{
			if (actualMode == Mode.Replace)
			{
				return m_AllowUndergroundReplace;
			}
			return false;
		}
	}

	public override Snap selectedSnap
	{
		get
		{
			return m_SelectedSnap;
		}
		set
		{
			if (value != m_SelectedSnap)
			{
				m_SelectedSnap = value;
				m_ForceUpdate = true;
				SaveToolPreferences();
			}
		}
	}

	public NetPrefab prefab
	{
		get
		{
			return m_SelectedPrefab;
		}
		set
		{
			if (value != m_SelectedPrefab)
			{
				m_SelectedPrefab = value;
				m_ForceUpdate = true;
				if (value != null)
				{
					m_LanePrefab = null;
				}
				if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_SelectedPrefab, out var component))
				{
					upgradeOnly = (component.m_PlacementFlags & Game.Net.PlacementFlags.UpgradeOnly) != 0;
					allowParallel = (component.m_PlacementFlags & Game.Net.PlacementFlags.AllowParallel) != 0;
					allowGrid = allowParallel && (!m_PrefabSystem.TryGetComponentData<NetGeometryData>(m_SelectedPrefab, out var component2) || component2.m_EdgeLengthRange.min == 0f);
					allowReplace = m_PrefabSystem.TryGetComponentData<NetData>(m_SelectedPrefab, out var component3) && m_NetInitializeSystem.CanReplace(component3, m_ToolSystem.actionMode.IsGame()) && (!m_PrefabSystem.TryGetBuffer((PrefabBase)m_SelectedPrefab, isReadOnly: true, out DynamicBuffer<AuxiliaryNet> buffer) || buffer.Length == 0) && !m_PrefabSystem.HasComponent<FixedNetElement>(m_SelectedPrefab);
					serviceUpgrade = m_PrefabSystem.HasComponent<ServiceUpgradeData>(m_SelectedPrefab);
					m_AllowUndergroundReplace = component.m_ElevationRange.min < 0f || (component.m_PlacementFlags & Game.Net.PlacementFlags.UndergroundUpgrade) != 0;
				}
				else
				{
					upgradeOnly = false;
					allowParallel = false;
					allowGrid = false;
					allowReplace = false;
					serviceUpgrade = false;
					m_AllowUndergroundReplace = false;
				}
				LoadToolPreferences();
				m_ToolSystem.EventPrefabChanged?.Invoke(value);
			}
		}
	}

	public NetLanePrefab lane
	{
		get
		{
			return m_LanePrefab;
		}
		set
		{
			if (value != m_LanePrefab)
			{
				m_LanePrefab = value;
				m_ForceUpdate = true;
				if (value != null)
				{
					m_SelectedPrefab = null;
					upgradeOnly = false;
					allowParallel = true;
					allowGrid = false;
					allowReplace = false;
					serviceUpgrade = false;
				}
				LoadToolPreferences();
				m_ToolSystem.EventPrefabChanged?.Invoke(value);
			}
		}
	}

	public bool upgradeOnly { get; private set; }

	public bool allowParallel { get; private set; }

	public bool allowGrid { get; private set; }

	public bool allowReplace { get; private set; }

	public bool serviceUpgrade { get; private set; }

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_DowngradeNetEdge;
			yield return m_PlaceNetControlPoint;
			yield return m_PlaceNetEdge;
			yield return m_PlaceNetNode;
			yield return m_ReplaceNetEdge;
			yield return m_UndoNetControlPoint;
			yield return m_UpgradeNetEdge;
			yield return m_DiscardUpgrade;
			yield return m_DiscardDowngrade;
			yield return m_DiscardReplace;
			yield return m_IncorrectApply;
		}
	}

	public override void GetUIModes(List<ToolMode> modes)
	{
		if (upgradeOnly)
		{
			modes.Add(new ToolMode(Mode.Replace.ToString(), 5));
			return;
		}
		modes.Add(new ToolMode(Mode.Straight.ToString(), 0));
		modes.Add(new ToolMode(Mode.SimpleCurve.ToString(), 1));
		modes.Add(new ToolMode(Mode.ComplexCurve.ToString(), 2));
		modes.Add(new ToolMode(Mode.Continuous.ToString(), 3));
		if (allowGrid)
		{
			modes.Add(new ToolMode(Mode.Grid.ToString(), 4));
		}
		if (allowReplace)
		{
			modes.Add(new ToolMode(Mode.Replace.ToString(), 5));
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			modes.Add(new ToolMode(Mode.Point.ToString(), 6));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_NetInitializeSystem = base.World.GetOrCreateSystemManaged<NetInitializeSystem>();
		m_ControlPoints = new NativeList<ControlPoint>(4, Allocator.Persistent);
		m_SnapLines = new NativeList<SnapLine>(10, Allocator.Persistent);
		m_UpgradeStates = new NativeList<UpgradeState>(4, Allocator.Persistent);
		m_StartEntity = new NativeReference<Entity>(Allocator.Persistent);
		m_LastSnappedEntity = new NativeReference<Entity>(Allocator.Persistent);
		m_LastControlPointsAngle = new NativeReference<int>(Allocator.Persistent);
		m_AppliedUpgrade = new NativeReference<AppliedUpgrade>(Allocator.Persistent);
		m_DefinitionQuery = GetDefinitionQuery();
		m_ContainerQuery = GetContainerQuery();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Lane>());
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Edge>());
		m_NodeQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Game.Net.Node>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_DowngradeNetEdge = InputManager.instance.toolActionCollection.GetActionState("Downgrade Net Edge", "NetToolSystem");
		m_PlaceNetControlPoint = InputManager.instance.toolActionCollection.GetActionState("Place Net Control Point", "NetToolSystem");
		m_PlaceNetEdge = InputManager.instance.toolActionCollection.GetActionState("Place Net Edge", "NetToolSystem");
		m_PlaceNetNode = InputManager.instance.toolActionCollection.GetActionState("Place Net Node", "NetToolSystem");
		m_ReplaceNetEdge = InputManager.instance.toolActionCollection.GetActionState("Replace Net Edge", "NetToolSystem");
		m_UndoNetControlPoint = InputManager.instance.toolActionCollection.GetActionState("Undo Net Control Point", "NetToolSystem");
		m_UpgradeNetEdge = InputManager.instance.toolActionCollection.GetActionState("Upgrade Net Edge", "NetToolSystem");
		m_DiscardUpgrade = InputManager.instance.toolActionCollection.GetActionState("Discard Upgrade", "NetToolSystem");
		m_DiscardDowngrade = InputManager.instance.toolActionCollection.GetActionState("Discard Downgrade", "NetToolSystem");
		m_DiscardReplace = InputManager.instance.toolActionCollection.GetActionState("Discard Replace", "NetToolSystem");
		m_IncorrectApply = InputManager.instance.toolActionCollection.GetActionState("Incorrect Apply", "NetToolSystem");
		elevationStep = 10f;
		parallelOffset = 8f;
		selectedSnap &= ~(Snap.AutoParent | Snap.ContourLines);
		m_DefaultToolPreferences = new NetToolPreferences();
		m_DefaultToolPreferences.Save(this);
		m_ToolPreferences = new Dictionary<Entity, NetToolPreferences>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		m_SnapLines.Dispose();
		m_UpgradeStates.Dispose();
		m_StartEntity.Dispose();
		m_LastSnappedEntity.Dispose();
		m_LastControlPointsAngle.Dispose();
		m_AppliedUpgrade.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_SnapLines.Clear();
		m_UpgradeStates.Clear();
		m_StartEntity.Value = default(Entity);
		m_LastSnappedEntity.Value = default(Entity);
		m_LastControlPointsAngle.Value = 0;
		m_AppliedUpgrade.Value = default(AppliedUpgrade);
		m_LastRaycastPoint = default(ControlPoint);
		m_ApplyStartPoint = default(ControlPoint);
		m_State = State.Default;
		m_ApplyTimer = 0f;
		m_RandomSeed = RandomSeed.Next();
		m_ForceCancel = false;
		m_ApplyBlocked = false;
		base.requireZones = false;
		base.requireUnderground = false;
		base.requirePipelines = false;
		base.requireNetArrows = false;
		base.requireAreas = AreaTypeMask.None;
		base.requireNet = Layer.None;
	}

	private protected override void ResetActions()
	{
		base.ResetActions();
		m_IncorrectApply.shouldBeEnabled = false;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			switch (actualMode)
			{
			case Mode.Straight:
			case Mode.SimpleCurve:
			case Mode.ComplexCurve:
			case Mode.Continuous:
			case Mode.Grid:
			{
				int maxControlPointCount = GetMaxControlPointCount(actualMode);
				base.applyAction.shouldBeEnabled = base.actionsEnabled && ((m_ControlPoints.Length > 1 && m_ControlPoints.Length < maxControlPointCount) || GetAllowApply());
				base.applyActionOverride = ((m_ControlPoints.Length < maxControlPointCount) ? m_PlaceNetControlPoint : m_PlaceNetEdge);
				base.secondaryApplyAction.shouldBeEnabled = false;
				base.secondaryApplyActionOverride = null;
				base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_ControlPoints.Length >= 2;
				base.cancelActionOverride = m_UndoNetControlPoint;
				break;
			}
			case Mode.Replace:
				if (prefab.Has<NetUpgrade>())
				{
					if (m_State == State.Default || m_UpgradeStates.Length == 0)
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowUpgrade();
						base.applyActionOverride = m_UpgradeNetEdge;
						base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowDowngrade();
						base.secondaryApplyActionOverride = m_DowngradeNetEdge;
						base.cancelAction.shouldBeEnabled = false;
						base.cancelActionOverride = null;
					}
					else if (m_State == State.Applying)
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled;
						base.applyActionOverride = m_UpgradeNetEdge;
						base.secondaryApplyAction.shouldBeEnabled = false;
						base.secondaryApplyActionOverride = null;
						base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_UpgradeStates.Length >= 2;
						base.cancelActionOverride = m_DiscardUpgrade;
					}
					else if (m_State == State.Cancelling)
					{
						base.applyAction.shouldBeEnabled = false;
						base.applyActionOverride = null;
						base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
						base.secondaryApplyActionOverride = m_DowngradeNetEdge;
						base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_UpgradeStates.Length >= 2;
						base.cancelActionOverride = m_DiscardDowngrade;
					}
				}
				else
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
					base.applyActionOverride = m_ReplaceNetEdge;
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					if (m_ControlPoints.Length > 4)
					{
						base.cancelAction.shouldBeEnabled = base.actionsEnabled;
						base.cancelActionOverride = m_DiscardReplace;
					}
					else
					{
						base.cancelAction.shouldBeEnabled = false;
						base.cancelActionOverride = null;
					}
				}
				break;
			case Mode.Point:
				base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
				base.applyActionOverride = m_PlaceNetNode;
				base.secondaryApplyAction.shouldBeEnabled = false;
				base.secondaryApplyActionOverride = null;
				base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_ControlPoints.Length >= 2;
				base.cancelActionOverride = m_UndoNetControlPoint;
				break;
			}
			m_IncorrectApply.shouldBeEnabled = m_State != State.Cancelling && base.actionsEnabled && !base.applyAction.shouldBeEnabled;
		}
	}

	public override PrefabBase GetPrefab()
	{
		if (!(prefab != null))
		{
			return lane;
		}
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is NetPrefab netPrefab)
		{
			this.prefab = netPrefab;
			return true;
		}
		if (prefab is NetLanePrefab netLanePrefab)
		{
			lane = netLanePrefab;
			return true;
		}
		return false;
	}

	private void LoadToolPreferences()
	{
		PrefabBase prefabBase = GetPrefab();
		if (!(prefabBase == null))
		{
			m_LoadingPreferences = true;
			Entity entity = m_PrefabSystem.GetEntity(prefabBase);
			base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component);
			if (m_ToolPreferences.TryGetValue(component.m_Group, out var value))
			{
				value.Load(this);
			}
			else
			{
				m_DefaultToolPreferences.Load(this);
			}
			m_LoadingPreferences = false;
		}
	}

	private void SaveToolPreferences()
	{
		if (m_LoadingPreferences)
		{
			return;
		}
		PrefabBase prefabBase = GetPrefab();
		if (!(prefabBase == null))
		{
			Entity entity = m_PrefabSystem.GetEntity(prefabBase);
			base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component);
			if (!m_ToolPreferences.ContainsKey(component.m_Group))
			{
				m_ToolPreferences[component.m_Group] = new NetToolPreferences();
			}
			m_ToolPreferences[component.m_Group].Save(this);
		}
	}

	public void ResetToolPreferences()
	{
		m_ToolPreferences.Clear();
		m_DefaultToolPreferences.Load(this);
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		ResetToolPreferences();
	}

	public NativeList<ControlPoint> GetControlPoints(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_ControlPoints;
	}

	public NativeList<SnapLine> GetSnapLines(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_SnapLines;
	}

	private NetPrefab GetNetPrefab()
	{
		if (m_ToolSystem.actionMode.IsEditor() && m_LanePrefab != null && GetContainers(m_ContainerQuery, out var laneContainer, out var _))
		{
			return m_PrefabSystem.GetPrefab<NetPrefab>(laneContainer);
		}
		return m_SelectedPrefab;
	}

	public override void SetUnderground(bool underground)
	{
		if (actualMode == Mode.Replace)
		{
			this.underground = underground;
		}
	}

	public override void ElevationUp()
	{
		NetPrefab netPrefab = GetNetPrefab();
		if (!(netPrefab != null))
		{
			return;
		}
		if (actualMode == Mode.Replace)
		{
			underground = false;
			return;
		}
		m_Prefab = netPrefab;
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out var component) && component.m_UndergroundPrefab != Entity.Null && elevation < 0f)
		{
			m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab);
		}
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component))
		{
			CheckElevationRange(component);
			elevation = math.floor(elevation / elevationStep + 1.00001f) * elevationStep;
			if (elevation > component.m_ElevationRange.max + elevationStep * 0.5f && m_Prefab != netPrefab)
			{
				m_Prefab = netPrefab;
				m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component);
				CheckElevationRange(component);
			}
		}
	}

	public override void ElevationDown()
	{
		NetPrefab netPrefab = GetNetPrefab();
		if (!(netPrefab != null))
		{
			return;
		}
		if (actualMode == Mode.Replace)
		{
			underground = true;
			return;
		}
		m_Prefab = netPrefab;
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out var component) && component.m_UndergroundPrefab != Entity.Null && elevation < 0f)
		{
			m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab);
		}
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component))
		{
			CheckElevationRange(component);
			elevation = math.ceil(elevation / elevationStep - 1.00001f) * elevationStep;
			if (elevation < component.m_ElevationRange.min - elevationStep * 0.5f && m_Prefab == netPrefab && component.m_UndergroundPrefab != Entity.Null)
			{
				m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab);
				m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component);
				CheckElevationRange(component);
			}
		}
	}

	public override void ElevationScroll()
	{
		NetPrefab netPrefab = GetNetPrefab();
		if (!(netPrefab != null))
		{
			return;
		}
		if (actualMode == Mode.Replace)
		{
			underground = !underground;
			return;
		}
		m_Prefab = netPrefab;
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out var component) && component.m_UndergroundPrefab != Entity.Null && elevation < 0f)
		{
			m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab);
		}
		if (!m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component))
		{
			return;
		}
		elevation = math.floor(elevation / elevationStep + 1.00001f) * elevationStep;
		if (!(elevation > component.m_ElevationRange.max + elevationStep * 0.5f))
		{
			return;
		}
		if (m_Prefab != netPrefab)
		{
			m_Prefab = netPrefab;
			m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component);
			CheckElevationRange(component);
			return;
		}
		if (component.m_UndergroundPrefab != Entity.Null)
		{
			m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab);
			m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component);
		}
		elevation = math.ceil(component.m_ElevationRange.min / elevationStep) * elevationStep;
		CheckElevationRange(component);
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		NetPrefab netPrefab = GetNetPrefab();
		m_Prefab = null;
		if (actualMode == Mode.Replace)
		{
			if (netPrefab != null)
			{
				if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(netPrefab, out var component))
				{
					if ((component.m_PlacementFlags & Game.Net.PlacementFlags.UndergroundUpgrade) == 0)
					{
						if (component.m_ElevationRange.min >= 0f && component.m_UndergroundPrefab == Entity.Null)
						{
							underground = false;
						}
						else if (component.m_ElevationRange.max < 0f && component.m_UndergroundPrefab == Entity.Null)
						{
							underground = true;
						}
					}
				}
				else
				{
					underground = false;
				}
				m_Prefab = ((underground && component.m_UndergroundPrefab != Entity.Null) ? m_PrefabSystem.GetPrefab<NetPrefab>(component.m_UndergroundPrefab) : netPrefab);
				NetData componentData = m_PrefabSystem.GetComponentData<NetData>(m_Prefab);
				m_PrefabSystem.TryGetComponentData<NetGeometryData>(m_Prefab, out var component2);
				if (underground)
				{
					m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
				}
				else
				{
					m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
				}
				if ((component2.m_Flags & Game.Net.GeometryFlags.Marker) != 0)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers;
				}
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.IgnoreSecondary;
				m_ToolRaycastSystem.typeMask = TypeMask.Net;
				m_ToolRaycastSystem.netLayerMask = componentData.m_RequiredLayers;
			}
			else
			{
				m_ToolRaycastSystem.collisionMask = (CollisionMask)0;
				m_ToolRaycastSystem.typeMask = TypeMask.Net;
				m_ToolRaycastSystem.netLayerMask = Layer.None;
			}
		}
		else if (netPrefab != null)
		{
			if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse && m_State == State.Applying && SharedSettings.instance.input.elevationDraggingEnabled)
			{
				Camera main = Camera.main;
				if (main != null && InputManager.instance.mouseOnScreen)
				{
					Line3 line = ToolRaycastSystem.CalculateRaycastLine(main);
					float3 hitPosition = m_ApplyStartPoint.m_HitPosition;
					hitPosition.y += m_ApplyStartPoint.m_Elevation;
					if (TryIntersectLineWithPlane(plane: new Triangle3(hitPosition, hitPosition + math.up(), hitPosition + (float3)main.transform.right), line: line, minDot: 0.05f, d: out var d) && d >= 0f && (double)d <= 1.0)
					{
						float3 y = MathUtils.Position(line, d);
						float num = y.y - hitPosition.y;
						float num2 = math.distance(line.a, y);
						float y2 = 2f * math.tan(math.radians(math.min(89f, main.fieldOfView * 0.5f))) * num2;
						float num3 = math.abs(num) / math.max(1f, y2);
						float num4 = 0.5f / (1f + num3 * 20f);
						if (m_ApplyTimer >= num4)
						{
							GetSurfaceHeights(netPrefab, out var overground, out var num5);
							bool flag = m_ApplyStartPoint.m_Elevation < 0f;
							float num6 = math.select(overground, num5, flag);
							elevation = m_ApplyStartPoint.m_Elevation + num - num6;
							elevation = math.round(elevation / elevationStep) * elevationStep;
							bool flag2 = elevation < 0f;
							if (overground != num5 && flag2 != flag)
							{
								num6 = math.select(overground, num5, flag2);
								elevation = m_ApplyStartPoint.m_Elevation + num - num6;
								elevation = math.round(elevation / elevationStep) * elevationStep;
								bool flag3 = elevation < 0f;
								if (flag3 != flag2)
								{
									elevation = math.select(0f, 0f - elevationStep, flag3);
								}
							}
						}
					}
				}
				m_ApplyTimer += UnityEngine.Time.deltaTime;
			}
			if (elevation > m_LastElevation)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetElevationUpSound);
			}
			else if (elevation < m_LastElevation)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetElevationDownSound);
			}
			m_LastElevation = elevation;
			m_Prefab = netPrefab;
			if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(netPrefab, out var component3) && component3.m_UndergroundPrefab != Entity.Null && elevation < 0f)
			{
				m_Prefab = m_PrefabSystem.GetPrefab<NetPrefab>(component3.m_UndergroundPrefab);
			}
			if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out component3))
			{
				CheckElevationRange(component3);
				elevation = MathUtils.Clamp(elevation, component3.m_ElevationRange);
			}
			else
			{
				m_LastElevationRange = default(Bounds1);
				elevation = 0f;
			}
			NetData componentData2 = m_PrefabSystem.GetComponentData<NetData>(m_Prefab);
			m_PrefabSystem.TryGetComponentData<NetGeometryData>(m_Prefab, out var component4);
			if (elevation < 0f)
			{
				m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
				m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
			}
			else
			{
				m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
				m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Water;
			}
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.ElevateOffset | RaycastFlags.SubElements | RaycastFlags.Outside | RaycastFlags.IgnoreSecondary;
			m_ToolRaycastSystem.netLayerMask = componentData2.m_ConnectLayers;
			m_ToolRaycastSystem.rayOffset = new float3(0f, 0f - component4.m_DefaultSurfaceHeight.max - elevation, 0f);
			GetAvailableSnapMask(out var onMask, out var offMask);
			Snap actualSnap = ToolBaseSystem.GetActualSnap(selectedSnap, onMask, offMask);
			if ((actualSnap & (Snap.ExistingGeometry | Snap.NearbyGeometry)) != Snap.None)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Net;
				if ((component4.m_Flags & Game.Net.GeometryFlags.Marker) != 0)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers;
				}
			}
			if ((actualSnap & Snap.ObjectSurface) != Snap.None)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
				if (m_ToolSystem.actionMode.IsEditor())
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
				}
			}
		}
		else
		{
			m_ToolRaycastSystem.collisionMask = (CollisionMask)0;
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.ElevateOffset | RaycastFlags.SubElements | RaycastFlags.Outside;
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Net | TypeMask.Water;
			m_ToolRaycastSystem.netLayerMask = Layer.None;
			m_ToolRaycastSystem.rayOffset = default(float3);
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.UpgradeIsMain;
		}
	}

	private static bool TryIntersectLineWithPlane(Line3 line, Triangle3 plane, float minDot, out float d)
	{
		float3 x = math.normalize(MathUtils.NormalCW(plane));
		if (math.abs(math.dot(x, math.normalize(line.ab))) > minDot)
		{
			float3 y = line.a - plane.a;
			d = (0f - math.dot(x, y)) / math.dot(x, line.ab);
			return true;
		}
		d = 0f;
		return false;
	}

	private void GetSurfaceHeights(NetPrefab prefab, out float overground, out float underground)
	{
		overground = 0f;
		underground = 0f;
		if (m_PrefabSystem.TryGetComponentData<NetGeometryData>(prefab, out var component))
		{
			overground = component.m_DefaultSurfaceHeight.max;
			underground = component.m_DefaultSurfaceHeight.max;
		}
		if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(prefab, out var component2) && component2.m_UndergroundPrefab != Entity.Null)
		{
			NetPrefab netPrefab = m_PrefabSystem.GetPrefab<NetPrefab>(component2.m_UndergroundPrefab);
			if (m_PrefabSystem.TryGetComponentData<NetGeometryData>(netPrefab, out var component3))
			{
				underground = component3.m_DefaultSurfaceHeight.max;
			}
		}
	}

	private void CheckElevationRange(PlaceableNetData placeableNetData)
	{
		if (!placeableNetData.m_ElevationRange.Equals(m_LastElevationRange))
		{
			float position = MathUtils.Clamp(0f, placeableNetData.m_ElevationRange);
			if (!MathUtils.Intersect(m_LastElevationRange, position) || !MathUtils.Intersect(placeableNetData.m_ElevationRange, elevation))
			{
				elevation = position;
			}
			m_LastElevationRange = placeableNetData.m_ElevationRange;
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		UpdateActions();
		if (m_FocusChanged)
		{
			return inputDeps;
		}
		Mode mode = actualMode;
		if (mode != m_LastActualMode)
		{
			if (m_LastActualMode == Mode.Replace || mode == Mode.Replace)
			{
				m_ControlPoints.Clear();
				m_State = State.Default;
			}
			else
			{
				int maxControlPointCount = GetMaxControlPointCount(mode);
				if (maxControlPointCount < m_ControlPoints.Length)
				{
					m_ControlPoints.RemoveRange(maxControlPointCount, m_ControlPoints.Length - maxControlPointCount);
				}
			}
			m_LastActualMode = mode;
		}
		bool flag = m_ForceCancel;
		m_ForceCancel = false;
		if (mode != Mode.Replace)
		{
			inputDeps = UpdateStartEntity(inputDeps);
		}
		if (m_Prefab != null)
		{
			NetData componentData = m_PrefabSystem.GetComponentData<NetData>(m_Prefab);
			m_PrefabSystem.TryGetComponentData<NetGeometryData>(m_Prefab, out var component);
			bool laneContainer = m_PrefabSystem.HasComponent<EditorContainerData>(m_Prefab);
			base.requireZones = false;
			base.requireUnderground = underground;
			base.requirePipelines = false;
			base.requireNetArrows = (component.m_Flags & Game.Net.GeometryFlags.Directional) != 0;
			base.requireAreas = AreaTypeMask.None;
			base.requireStops = TransportType.None;
			base.requireNet = componentData.m_ConnectLayers | componentData.m_RequiredLayers | component.m_MergeLayers | component.m_IntersectLayers;
			if (actualMode != Mode.Replace)
			{
				base.requireUnderground = elevation < 0f && (elevation <= component.m_ElevationLimit * -3f || (component.m_Flags & Game.Net.GeometryFlags.LoweredIsTunnel) != 0);
				base.requirePipelines = elevation < 0f;
			}
			if (m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out var component2))
			{
				if ((component2.m_PlacementFlags & Game.Net.PlacementFlags.OnGround) != Game.Net.PlacementFlags.None && !base.requireUnderground)
				{
					base.requireZones = true;
					base.requireAreas |= AreaTypeMask.Lots;
					if (m_ToolSystem.actionMode.IsEditor())
					{
						base.requireAreas |= AreaTypeMask.Spaces;
					}
				}
				if (mode != Mode.Replace && (component2.m_ElevationRange.max > 0f || (component.m_Flags & Game.Net.GeometryFlags.RequireElevated) != 0) && !base.requireUnderground)
				{
					base.requireNet |= Layer.Waterway;
				}
			}
			if (m_PrefabSystem.TryGetBuffer((PrefabBase)m_Prefab, isReadOnly: true, out DynamicBuffer<Game.Prefabs.SubObject> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					Game.Prefabs.SubObject subObject = buffer[i];
					if (base.EntityManager.TryGetComponent<TransportStopData>(subObject.m_Prefab, out var component3) && !base.EntityManager.HasComponent<OutsideConnectionData>(subObject.m_Prefab))
					{
						base.requireStops = component3.m_TransportType;
						break;
					}
				}
			}
			UpdateInfoview(m_ToolSystem.actionMode.IsEditor() ? Entity.Null : m_PrefabSystem.GetEntity(m_Prefab));
			GetAvailableSnapMask(component, component2, mode, m_ToolSystem.actionMode.IsEditor(), laneContainer, base.requireUnderground, out m_SnapOnMask, out m_SnapOffMask);
			if (m_State != State.Default && !base.applyAction.enabled && !base.secondaryApplyAction.enabled)
			{
				m_State = State.Default;
			}
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				if (m_IncorrectApply.WasPressedThisFrame())
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
					return Update(inputDeps, fullUpdate: false);
				}
				if (actualMode == Mode.Replace)
				{
					switch (m_State)
					{
					case State.Default:
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
						return Update(inputDeps, fullUpdate: false);
					case State.Applying:
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
						return Update(inputDeps, fullUpdate: false);
					case State.Cancelling:
						if (base.cancelAction.WasPressedThisFrame())
						{
							m_ApplyBlocked = true;
							m_State = State.Default;
							return Update(inputDeps, fullUpdate: true);
						}
						if (base.secondaryApplyAction.WasReleasedThisFrame())
						{
							return Cancel(inputDeps);
						}
						return Update(inputDeps, fullUpdate: false);
					default:
						return Update(inputDeps, fullUpdate: false);
					}
				}
				if (m_State != State.Cancelling && base.cancelAction.WasPressedThisFrame())
				{
					return Cancel(inputDeps, base.cancelAction.WasReleasedThisFrame());
				}
				if (m_State == State.Cancelling && (flag || base.cancelAction.WasReleasedThisFrame()))
				{
					return Cancel(inputDeps);
				}
				if (m_State != State.Applying && base.applyAction.WasPressedThisFrame())
				{
					return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
				}
				if (m_State == State.Applying && base.applyAction.WasReleasedThisFrame())
				{
					return Apply(inputDeps);
				}
				return Update(inputDeps, fullUpdate: false);
			}
		}
		else
		{
			base.requireZones = false;
			base.requireUnderground = false;
			base.requirePipelines = false;
			base.requireNetArrows = false;
			base.requireAreas = AreaTypeMask.None;
			base.requireStops = TransportType.None;
			base.requireNet = Layer.None;
			UpdateInfoview(Entity.Null);
		}
		if (m_State == State.Applying && (base.applyAction.WasReleasedThisFrame() || base.cancelAction.WasPressedThisFrame()))
		{
			m_State = State.Default;
		}
		else if (m_State == State.Cancelling && (base.secondaryApplyAction.WasReleasedThisFrame() || base.cancelAction.WasPressedThisFrame()))
		{
			m_State = State.Default;
		}
		return Clear(inputDeps);
	}

	private static int GetMaxControlPointCount(Mode mode)
	{
		switch (mode)
		{
		case Mode.Straight:
			return 2;
		case Mode.SimpleCurve:
		case Mode.Continuous:
		case Mode.Grid:
			return 3;
		case Mode.ComplexCurve:
			return 4;
		default:
			return 1;
		}
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		if (m_Prefab != null)
		{
			m_PrefabSystem.TryGetComponentData<NetGeometryData>(m_Prefab, out var component);
			m_PrefabSystem.TryGetComponentData<PlaceableNetData>(m_Prefab, out var component2);
			bool laneContainer = m_PrefabSystem.HasComponent<EditorContainerData>(m_Prefab);
			bool flag = underground;
			if (actualMode != Mode.Replace)
			{
				flag = elevation < 0f && (elevation <= component.m_ElevationLimit * -3f || (component.m_Flags & Game.Net.GeometryFlags.LoweredIsTunnel) != 0);
			}
			GetAvailableSnapMask(component, component2, actualMode, m_ToolSystem.actionMode.IsEditor(), laneContainer, flag, out onMask, out offMask);
		}
		else
		{
			base.GetAvailableSnapMask(out onMask, out offMask);
		}
	}

	private static void GetAvailableSnapMask(NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData, Mode mode, bool editorMode, bool laneContainer, bool underground, out Snap onMask, out Snap offMask)
	{
		if (mode == Mode.Replace)
		{
			onMask = Snap.ExistingGeometry;
			offMask = onMask;
			if ((placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.UpgradeOnly) == 0)
			{
				onMask |= Snap.ContourLines;
				offMask |= Snap.ContourLines;
			}
			if (laneContainer)
			{
				onMask &= ~Snap.ExistingGeometry;
				offMask &= ~Snap.ExistingGeometry;
				onMask |= Snap.NearbyGeometry;
				return;
			}
			if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
			{
				offMask &= ~Snap.ExistingGeometry;
			}
			if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
			{
				onMask |= Snap.CellLength;
				offMask |= Snap.CellLength;
			}
			return;
		}
		onMask = Snap.ExistingGeometry | Snap.CellLength | Snap.StraightDirection | Snap.ObjectSide | Snap.GuideLines | Snap.ZoneGrid | Snap.ContourLines;
		offMask = onMask;
		if (underground)
		{
			onMask &= ~(Snap.ObjectSide | Snap.ZoneGrid);
		}
		else if ((placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.ShoreLine) != Game.Net.PlacementFlags.None)
		{
			onMask |= Snap.Shoreline;
			offMask |= Snap.Shoreline;
		}
		if (laneContainer)
		{
			onMask &= ~(Snap.CellLength | Snap.ObjectSide);
			offMask &= ~(Snap.CellLength | Snap.ObjectSide);
		}
		else if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.Marker) != 0)
		{
			onMask &= ~Snap.ObjectSide;
			offMask &= ~Snap.ObjectSide;
		}
		if (laneContainer)
		{
			onMask &= ~Snap.ExistingGeometry;
			offMask &= ~Snap.ExistingGeometry;
			onMask |= Snap.NearbyGeometry;
			offMask |= Snap.NearbyGeometry;
		}
		else if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
		{
			offMask &= ~Snap.ExistingGeometry;
			onMask |= Snap.NearbyGeometry;
			offMask |= Snap.NearbyGeometry;
		}
		if (editorMode)
		{
			onMask |= Snap.ObjectSurface | Snap.LotGrid | Snap.AutoParent;
			offMask |= Snap.ObjectSurface | Snap.LotGrid | Snap.AutoParent;
		}
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (actualMode == Mode.Replace)
		{
			if (m_State != State.Cancelling && m_ControlPoints.Length >= 1)
			{
				m_State = State.Cancelling;
				m_ForceCancel = singleFrameOnly;
				m_AppliedUpgrade.Value = default(AppliedUpgrade);
				return Update(inputDeps, fullUpdate: true);
			}
			m_State = State.Default;
			if (GetAllowApply() && !m_EdgeQuery.IsEmptyIgnoreFilter)
			{
				SetAppliedUpgrade(removing: true);
				base.applyMode = ApplyMode.Apply;
				m_RandomSeed = RandomSeed.Next();
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint))
				{
					controlPoint.m_Elevation = elevation;
					m_ControlPoints.Add(in controlPoint);
					inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
					inputDeps = FixControlPoints(inputDeps);
					inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint2))
				{
					controlPoint2.m_Elevation = elevation;
					m_ControlPoints.Add(in controlPoint2);
					inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
					inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			return inputDeps;
		}
		m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetCancelSound);
		m_State = State.Default;
		base.applyMode = ApplyMode.Clear;
		m_UpgradeStates.Clear();
		if (m_ControlPoints.Length > 0)
		{
			m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
		}
		if (GetRaycastResult(out var controlPoint3))
		{
			controlPoint3.m_HitPosition.y += elevation;
			controlPoint3.m_Elevation = elevation;
			if (m_ControlPoints.Length > 0)
			{
				m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint3;
			}
			else
			{
				m_ControlPoints.Add(in controlPoint3);
			}
			inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
			inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
		}
		else
		{
			inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		return inputDeps;
	}

	private void SetAppliedUpgrade(bool removing)
	{
		m_AppliedUpgrade.Value = default(AppliedUpgrade);
		if (m_UpgradeStates.Length < 1 || m_ControlPoints.Length < 4)
		{
			return;
		}
		Entity originalEntity = m_ControlPoints[m_ControlPoints.Length - 3].m_OriginalEntity;
		Entity originalEntity2 = m_ControlPoints[m_ControlPoints.Length - 2].m_OriginalEntity;
		UpgradeState upgradeState = m_UpgradeStates[m_UpgradeStates.Length - 1];
		AppliedUpgrade value = new AppliedUpgrade
		{
			m_SubReplacementPrefab = upgradeState.m_SubReplacementPrefab,
			m_Flags = (removing ? upgradeState.m_RemoveFlags : upgradeState.m_AddFlags),
			m_SubReplacementType = upgradeState.m_SubReplacementType,
			m_SubReplacementSide = upgradeState.m_SubReplacementSide
		};
		if (originalEntity == originalEntity2)
		{
			value.m_Entity = originalEntity;
			m_AppliedUpgrade.Value = value;
		}
		else
		{
			if (!base.EntityManager.TryGetBuffer(originalEntity, isReadOnly: true, out DynamicBuffer<ConnectedEdge> buffer))
			{
				return;
			}
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity edge = buffer[i].m_Edge;
				if (base.EntityManager.TryGetComponent<Edge>(edge, out var component) && ((component.m_Start == originalEntity && component.m_End == originalEntity2) || (component.m_End == originalEntity && component.m_Start == originalEntity2)))
				{
					value.m_Entity = edge;
					m_AppliedUpgrade.Value = value;
				}
			}
		}
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (GetRaycastResult(out Entity entity, out RaycastHit hit))
		{
			controlPoint = FilterRaycastResult(entity, hit);
			return controlPoint.m_OriginalEntity != Entity.Null;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
	{
		if (GetRaycastResult(out var entity, out var hit, out forceUpdate))
		{
			controlPoint = FilterRaycastResult(entity, hit);
			return controlPoint.m_OriginalEntity != Entity.Null;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	private ControlPoint FilterRaycastResult(Entity entity, RaycastHit hit)
	{
		if (actualMode == Mode.Replace)
		{
			if (base.EntityManager.HasComponent<Game.Net.Node>(entity) && base.EntityManager.HasComponent<Edge>(hit.m_HitEntity) && m_PrefabSystem.TryGetComponentData<PlaceableNetData>(prefab, out var component) && (component.m_PlacementFlags & Game.Net.PlacementFlags.NodeUpgrade) == 0)
			{
				entity = hit.m_HitEntity;
			}
			if (!AllowUpgrade(entity))
			{
				return default(ControlPoint);
			}
		}
		return new ControlPoint(entity, hit);
	}

	private bool AllowUpgradeForEntity(Entity entity)
	{
		bool flag = false;
		Entity entity2 = entity;
		Owner component;
		while (base.EntityManager.TryGetComponent<Owner>(entity2, out component))
		{
			if (base.EntityManager.HasComponent<Edge>(component.m_Owner))
			{
				flag = true;
			}
			else if (!m_ToolSystem.actionMode.IsEditor() || flag)
			{
				return false;
			}
			entity2 = component.m_Owner;
		}
		return true;
	}

	private bool AllowUpgrade(Entity entity)
	{
		if (AllowUpgradeForEntity(entity))
		{
			return true;
		}
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ConnectedEdge> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity edge = buffer[i].m_Edge;
				Edge componentData = base.EntityManager.GetComponentData<Edge>(edge);
				if ((componentData.m_Start == entity || componentData.m_End == entity) && AllowUpgradeForEntity(edge))
				{
					return true;
				}
			}
		}
		return false;
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		return inputDeps;
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		Mode mode = actualMode;
		if (mode == Mode.Replace)
		{
			if (m_State != State.Applying && m_ControlPoints.Length >= 1 && !singleFrameOnly)
			{
				m_State = State.Applying;
				m_AppliedUpgrade.Value = default(AppliedUpgrade);
				return Update(inputDeps, fullUpdate: true);
			}
			m_State = State.Default;
			if (GetAllowApply() && !m_EdgeQuery.IsEmptyIgnoreFilter)
			{
				SetAppliedUpgrade(removing: false);
				base.applyMode = ApplyMode.Apply;
				m_RandomSeed = RandomSeed.Next();
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetBuildSound);
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint))
				{
					controlPoint.m_Elevation = elevation;
					m_ControlPoints.Add(in controlPoint);
					inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
					inputDeps = FixControlPoints(inputDeps);
					inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			else
			{
				inputDeps = Update(inputDeps, fullUpdate: true);
			}
			return inputDeps;
		}
		if (m_State != State.Applying && m_ControlPoints.Length >= 1 && !singleFrameOnly)
		{
			m_State = State.Applying;
			m_ApplyStartPoint = m_LastRaycastPoint;
			m_ApplyStartPoint.m_HitPosition.y -= m_ApplyStartPoint.m_Elevation;
			m_ApplyTimer = 0f;
			return Update(inputDeps, fullUpdate: true);
		}
		m_State = State.Default;
		if (m_ControlPoints.Length < mode switch
		{
			Mode.Straight => 2, 
			Mode.SimpleCurve => 3, 
			Mode.ComplexCurve => 4, 
			Mode.Continuous => 3, 
			Mode.Grid => 3, 
			_ => 1, 
		})
		{
			base.applyMode = ApplyMode.Clear;
			if (GetRaycastResult(out var controlPoint2))
			{
				if (m_ControlPoints.Length <= 1)
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetStartSound);
				}
				else
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetNodeSound);
				}
				controlPoint2.m_HitPosition.y += elevation;
				controlPoint2.m_Elevation = elevation;
				m_ControlPoints.Add(in controlPoint2);
				inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
				inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
			}
			else
			{
				inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
			}
		}
		else if (!((mode == Mode.Point) ? m_NodeQuery : m_EdgeQuery).IsEmptyIgnoreFilter)
		{
			if (!GetAllowApply())
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
			}
			else
			{
				base.applyMode = ApplyMode.Apply;
				m_RandomSeed = RandomSeed.Next();
				int num = 0;
				switch (mode)
				{
				case Mode.Continuous:
				{
					ControlPoint value2 = m_ControlPoints[m_ControlPoints.Length - 2];
					ControlPoint value3 = m_ControlPoints[m_ControlPoints.Length - 1];
					m_ControlPoints.Clear();
					float num2 = math.distance(value2.m_Position.xz, value3.m_Position.xz);
					value2.m_OriginalEntity = Entity.Null;
					value2.m_Direction = value3.m_Direction;
					value2.m_Position = value3.m_Position;
					value2.m_Position.xz += value2.m_Direction * num2;
					m_ControlPoints.Add(in value3);
					num++;
					m_ControlPoints.Add(in value2);
					num++;
					break;
				}
				case Mode.Point:
					m_ControlPoints.Clear();
					break;
				default:
				{
					ControlPoint value = m_ControlPoints[m_ControlPoints.Length - 1];
					m_ControlPoints.Clear();
					m_ControlPoints.Add(in value);
					num++;
					break;
				}
				}
				if (GetRaycastResult(out var controlPoint3))
				{
					controlPoint3.m_HitPosition.y += elevation;
					controlPoint3.m_Elevation = elevation;
					m_ControlPoints.Add(in controlPoint3);
					inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
					num++;
				}
				if (num >= 1)
				{
					inputDeps = FixControlPoints(inputDeps);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetBuildSound);
					inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
		}
		else
		{
			inputDeps = Update(inputDeps, fullUpdate: true);
		}
		return inputDeps;
	}

	protected override bool GetAllowApply()
	{
		Mode mode = actualMode;
		if (mode != Mode.Replace)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_EdgeQuery.ToArchetypeChunkArray(Allocator.Temp);
			NativeArray<ArchetypeChunk> nativeArray2 = m_NodeQuery.ToArchetypeChunkArray(Allocator.Temp);
			NativeHashSet<Entity> nativeHashSet = new NativeHashSet<Entity>(100, Allocator.Temp);
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Edge> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Owner> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Temp> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Edge> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<Temp> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle3);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray4[j].m_Flags & TempFlags.Create) != 0)
					{
						Edge edge = nativeArray3[j];
						nativeHashSet.Add(edge.m_Start);
						nativeHashSet.Add(edge.m_End);
					}
				}
			}
			if (mode != Mode.Point && m_ControlPoints.Length > 1 && nativeHashSet.IsEmpty)
			{
				return false;
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				ArchetypeChunk archetypeChunk2 = nativeArray2[k];
				NativeArray<Entity> nativeArray5 = archetypeChunk2.GetNativeArray(entityTypeHandle);
				NativeArray<Owner> nativeArray6 = archetypeChunk2.GetNativeArray(ref typeHandle2);
				NativeArray<Temp> nativeArray7 = archetypeChunk2.GetNativeArray(ref typeHandle3);
				for (int l = 0; l < nativeArray5.Length; l++)
				{
					Entity item = nativeArray5[l];
					Temp temp = nativeArray7[l];
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Replace)) != 0 && (temp.m_Flags & TempFlags.Essential) != 0 && !nativeHashSet.Contains(item) && ((mode != Mode.Point && m_ControlPoints.Length > 1) || (CollectionUtils.TryGet(nativeArray6, l, out var value) && value.m_Owner == Entity.Null && (mode == Mode.Point || m_ControlPoints.Length > 1))))
					{
						return false;
					}
				}
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeHashSet.Dispose();
			return base.GetAllowApply();
		}
		if (m_ControlPoints.Length >= 1)
		{
			return base.GetAllowApply();
		}
		return false;
	}

	private bool GetAllowUpgrade()
	{
		if (m_UpgradeStates.Length == 0 || m_ControlPoints.Length < 4)
		{
			return false;
		}
		return true;
	}

	private bool GetAllowDowngrade()
	{
		if (m_UpgradeStates.Length == 0 || m_ControlPoints.Length < 4)
		{
			return false;
		}
		ref NativeList<ControlPoint> reference = ref m_ControlPoints;
		ControlPoint controlPoint = reference[reference.Length - 4];
		ref NativeList<ControlPoint> reference2 = ref m_ControlPoints;
		ControlPoint controlPoint2 = reference2[reference2.Length - 3];
		ref NativeList<ControlPoint> reference3 = ref m_ControlPoints;
		ControlPoint controlPoint3 = reference3[reference3.Length - 2];
		ref NativeList<ControlPoint> reference4 = ref m_ControlPoints;
		_ = reference4[reference4.Length - 1];
		Entity entity = m_PrefabSystem.GetEntity(m_Prefab);
		if (entity == Entity.Null)
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<PlaceableNetData>(entity, out var component))
		{
			return false;
		}
		if ((component.m_PlacementFlags & Game.Net.PlacementFlags.NodeUpgrade) != Game.Net.PlacementFlags.None)
		{
			if (base.EntityManager.TryGetComponent<Upgraded>(controlPoint2.m_OriginalEntity, out var component2) && CheckFlags(component2.m_Flags, m_UpgradeStates[0].m_AddFlags, m_UpgradeStates[0].m_SubReplacementSide))
			{
				return true;
			}
			if (!base.EntityManager.TryGetBuffer(controlPoint2.m_OriginalEntity, isReadOnly: true, out DynamicBuffer<ConnectedEdge> buffer))
			{
				return false;
			}
			CompositionFlags currentFlags = default(CompositionFlags);
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity edge = buffer[i].m_Edge;
				if (!base.EntityManager.TryGetComponent<Edge>(edge, out var component3) || !base.EntityManager.TryGetComponent<Composition>(edge, out var component4))
				{
					continue;
				}
				Entity entity2 = Entity.Null;
				if (component3.m_Start.Equals(controlPoint2.m_OriginalEntity))
				{
					entity2 = component4.m_StartNode;
				}
				else
				{
					if (!component3.m_End.Equals(controlPoint2.m_OriginalEntity))
					{
						continue;
					}
					entity2 = component4.m_EndNode;
				}
				if (base.EntityManager.TryGetComponent<NetCompositionData>(entity2, out var component5))
				{
					currentFlags |= component5.m_Flags;
				}
			}
			return CheckFlags(currentFlags, m_UpgradeStates[0].m_AddFlags, m_UpgradeStates[0].m_SubReplacementSide);
		}
		if (!base.EntityManager.TryGetComponent<Edge>(controlPoint.m_OriginalEntity, out var component6))
		{
			return false;
		}
		if ((component6.m_Start != controlPoint2.m_OriginalEntity || component6.m_End != controlPoint3.m_OriginalEntity) && (component6.m_End != controlPoint2.m_OriginalEntity || component6.m_Start != controlPoint3.m_OriginalEntity))
		{
			return false;
		}
		if (base.EntityManager.TryGetComponent<Upgraded>(controlPoint.m_OriginalEntity, out var component7) && CheckFlags(component7.m_Flags, m_UpgradeStates[0].m_AddFlags, m_UpgradeStates[0].m_SubReplacementSide))
		{
			return true;
		}
		if (!base.EntityManager.TryGetComponent<Composition>(controlPoint.m_OriginalEntity, out var component8))
		{
			return false;
		}
		Entity entity3 = (((double)controlPoint.m_CurvePosition < 0.5) ? component8.m_StartNode : component8.m_EndNode);
		if (!base.EntityManager.TryGetComponent<NetCompositionData>(entity3, out var component9))
		{
			return false;
		}
		CompositionFlags flags = component9.m_Flags;
		if ((flags.m_General & CompositionFlags.General.Crosswalk) != 0)
		{
			flags.m_Left |= CompositionFlags.Side.AddCrosswalk;
			flags.m_Right |= CompositionFlags.Side.AddCrosswalk;
		}
		return CheckFlags(flags, m_UpgradeStates[0].m_AddFlags, m_UpgradeStates[0].m_SubReplacementSide);
		static bool CheckFlags(CompositionFlags compositionFlags, CompositionFlags upgradeFlags, SubReplacementSide side)
		{
			if (((upgradeFlags.m_Left | upgradeFlags.m_Right) & (CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk | CompositionFlags.Side.ForbidStraight)) != 0)
			{
				switch (side)
				{
				case SubReplacementSide.Left:
				case SubReplacementSide.Right:
					return ((compositionFlags.m_Left | compositionFlags.m_Right) & (upgradeFlags.m_Left | upgradeFlags.m_Right)) != 0;
				case SubReplacementSide.Middle:
					return (compositionFlags.m_General & upgradeFlags.m_General) != 0;
				}
			}
			else
			{
				switch (side)
				{
				case SubReplacementSide.Left:
					return (compositionFlags.m_Left & upgradeFlags.m_Left) != 0;
				case SubReplacementSide.Middle:
					return (compositionFlags.m_General & upgradeFlags.m_General) != 0;
				case SubReplacementSide.Right:
					return (compositionFlags.m_Right & upgradeFlags.m_Right) != 0;
				}
			}
			return false;
		}
	}

	private JobHandle Update(JobHandle inputDeps, bool fullUpdate)
	{
		if (actualMode == Mode.Replace)
		{
			if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
			{
				controlPoint.m_Elevation = elevation;
				fullUpdate = fullUpdate || forceUpdate;
				if (m_ControlPoints.Length == 0)
				{
					base.applyMode = ApplyMode.Clear;
					m_ControlPoints.Add(in controlPoint);
					inputDeps = SnapControlPoints(inputDeps, m_State == State.Cancelling);
					inputDeps = UpdateCourse(inputDeps, m_State == State.Cancelling);
				}
				else
				{
					base.applyMode = ApplyMode.None;
					if (fullUpdate || !m_LastRaycastPoint.Equals(controlPoint))
					{
						m_LastRaycastPoint = controlPoint;
						ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 1];
						if (m_State == State.Applying || m_State == State.Cancelling)
						{
							if (m_ControlPoints.Length == 1)
							{
								m_ControlPoints.Add(in controlPoint);
							}
							else
							{
								m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint;
							}
						}
						else
						{
							m_ControlPoints.Clear();
							m_UpgradeStates.Clear();
							m_ControlPoints.Add(in controlPoint);
						}
						inputDeps = SnapControlPoints(inputDeps, m_State == State.Cancelling);
						JobHandle.ScheduleBatchedJobs();
						if (!fullUpdate)
						{
							inputDeps.Complete();
							ControlPoint other = m_ControlPoints[m_ControlPoints.Length - 1];
							fullUpdate = !controlPoint2.EqualsIgnoreHit(other);
						}
						if (fullUpdate)
						{
							base.applyMode = ApplyMode.Clear;
							inputDeps = UpdateCourse(inputDeps, m_State == State.Cancelling);
						}
					}
				}
			}
			else
			{
				if (m_State == State.Default)
				{
					m_ControlPoints.Clear();
					m_UpgradeStates.Clear();
					m_AppliedUpgrade.Value = default(AppliedUpgrade);
				}
				base.applyMode = ApplyMode.Clear;
				inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
			}
			return inputDeps;
		}
		if (GetRaycastResult(out ControlPoint controlPoint3, out bool forceUpdate2))
		{
			if (m_State == State.Applying)
			{
				controlPoint3 = m_ApplyStartPoint;
			}
			controlPoint3.m_HitPosition.y += elevation;
			controlPoint3.m_Elevation = elevation;
			fullUpdate = fullUpdate || forceUpdate2;
			if (m_ControlPoints.Length == 0)
			{
				base.applyMode = ApplyMode.Clear;
				m_ControlPoints.Add(in controlPoint3);
				inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
				inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
			}
			else
			{
				base.applyMode = ApplyMode.None;
				if (fullUpdate || !m_LastRaycastPoint.Equals(controlPoint3))
				{
					if (m_ControlPoints.Length >= 2 && math.distance(m_LastRaycastPoint.m_Position, controlPoint3.m_Position) > 0.01f)
					{
						m_AudioManager.PlayUISoundIfNotPlaying(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetExpandSound);
					}
					m_LastRaycastPoint = controlPoint3;
					ControlPoint controlPoint4 = m_ControlPoints[m_ControlPoints.Length - 1];
					m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint3;
					inputDeps = SnapControlPoints(inputDeps, removeUpgrade: false);
					JobHandle.ScheduleBatchedJobs();
					if (!fullUpdate)
					{
						inputDeps.Complete();
						ControlPoint other2 = m_ControlPoints[m_ControlPoints.Length - 1];
						fullUpdate = !controlPoint4.EqualsIgnoreHit(other2);
					}
					if (fullUpdate)
					{
						base.applyMode = ApplyMode.Clear;
						inputDeps = UpdateCourse(inputDeps, removeUpgrade: false);
					}
				}
			}
		}
		else
		{
			base.applyMode = ApplyMode.Clear;
			inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		return inputDeps;
	}

	private JobHandle UpdateStartEntity(JobHandle inputDeps)
	{
		if (!m_DefinitionQuery.IsEmptyIgnoreFilter)
		{
			return JobChunkExtensions.Schedule(new UpdateStartEntityJob
			{
				m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StartEntity = m_StartEntity
			}, m_DefinitionQuery, inputDeps);
		}
		return inputDeps;
	}

	private JobHandle SnapControlPoints(JobHandle inputDeps, bool removeUpgrade)
	{
		Entity lanePrefab = Entity.Null;
		Entity serviceUpgradeOwner = Entity.Null;
		if (m_LanePrefab != null)
		{
			lanePrefab = m_PrefabSystem.GetEntity(m_LanePrefab);
		}
		if (serviceUpgrade)
		{
			serviceUpgradeOwner = GetUpgradable(m_ToolSystem.selected);
		}
		JobHandle deps;
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle deps2;
		SnapJob jobData = new SnapJob
		{
			m_Mode = actualMode,
			m_Snap = GetActualSnap(),
			m_Elevation = elevation,
			m_Prefab = m_PrefabSystem.GetEntity(m_Prefab),
			m_LanePrefab = lanePrefab,
			m_ServiceUpgradeOwner = serviceUpgradeOwner,
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_RemoveUpgrade = removeUpgrade,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneBlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ZoneCells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabCompositionAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_AuxiliaryNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_FixedNetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_FixedNetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
			m_ZoneSearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
			m_ControlPoints = m_ControlPoints,
			m_SnapLines = m_SnapLines,
			m_UpgradeStates = m_UpgradeStates,
			m_StartEntity = m_StartEntity,
			m_AppliedUpgrade = m_AppliedUpgrade,
			m_LastSnappedEntity = m_LastSnappedEntity,
			m_LastControlPointsAngle = m_LastControlPointsAngle,
			m_SourceUpdateData = m_AudioManager.GetSourceUpdateData(out deps2)
		};
		inputDeps = JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2);
		inputDeps = JobHandle.CombineDependencies(inputDeps, dependencies3, deps);
		inputDeps = JobHandle.CombineDependencies(inputDeps, deps2);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, inputDeps);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		return jobHandle;
	}

	private JobHandle FixControlPoints(JobHandle inputDeps)
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new FixControlPointsJob
		{
			m_Chunks = chunks,
			m_Mode = mode,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControlPoints = m_ControlPoints
		}, JobHandle.CombineDependencies(inputDeps, outJobHandle));
		chunks.Dispose(jobHandle);
		return jobHandle;
	}

	private Entity GetUpgradable(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Attached>(entity, out var component))
		{
			return component.m_Parent;
		}
		return entity;
	}

	private JobHandle UpdateCourse(JobHandle inputDeps, bool removeUpgrade)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (m_Prefab != null)
		{
			JobHandle deps;
			CreateDefinitionsJob jobData = new CreateDefinitionsJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_RemoveUpgrade = removeUpgrade,
				m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_Mode = actualMode,
				m_ParallelCount = math.select(new int2(actualParallelCount, 0), new int2(0, actualParallelCount), m_CityConfigurationSystem.leftHandTraffic),
				m_ParallelOffset = parallelOffset,
				m_RandomSeed = m_RandomSeed,
				m_ControlPoints = m_ControlPoints,
				m_UpgradeStates = m_UpgradeStates,
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_NetPrefab = m_PrefabSystem.GetEntity(m_Prefab),
				m_WaterSurfaceData = m_WaterSystem.GetVelocitiesSurfaceData(out deps),
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			};
			if (m_LanePrefab != null)
			{
				jobData.m_LanePrefab = m_PrefabSystem.GetEntity(m_LanePrefab);
			}
			if (serviceUpgrade)
			{
				jobData.m_ServiceUpgradeOwner = GetUpgradable(m_ToolSystem.selected);
			}
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, deps));
			m_WaterSystem.AddVelocitySurfaceReader(jobHandle2);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		return jobHandle;
	}

	public static void CreatePath(ControlPoint startPoint, ControlPoint endPoint, NativeList<PathEdge> path, NetData prefabNetData, PlaceableNetData placeableNetData, ref ComponentLookup<Edge> edgeData, ref ComponentLookup<Game.Net.Node> nodeData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<NetData> prefabNetDatas, ref BufferLookup<ConnectedEdge> connectedEdgeData)
	{
		if (math.distance(startPoint.m_Position, endPoint.m_Position) < placeableNetData.m_SnapDistance * 0.5f)
		{
			endPoint = startPoint;
		}
		CompositionFlags.General general = placeableNetData.m_SetUpgradeFlags.m_General;
		CompositionFlags.Side side = placeableNetData.m_SetUpgradeFlags.m_Left | placeableNetData.m_SetUpgradeFlags.m_Right;
		if (general == (CompositionFlags.General)0u && side == (CompositionFlags.Side)0u)
		{
			general = placeableNetData.m_UnsetUpgradeFlags.m_General;
			side = placeableNetData.m_UnsetUpgradeFlags.m_Left | placeableNetData.m_UnsetUpgradeFlags.m_Right;
		}
		if (startPoint.m_OriginalEntity == endPoint.m_OriginalEntity)
		{
			if (edgeData.HasComponent(endPoint.m_OriginalEntity))
			{
				NetData netData = prefabNetDatas[prefabRefData[endPoint.m_OriginalEntity].m_Prefab];
				bool num = (prefabNetData.m_RequiredLayers & netData.m_RequiredLayers) != 0;
				bool flag = !num && (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.IsUpgrade) != Game.Net.PlacementFlags.None && (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.NodeUpgrade) == 0 && ((netData.m_GeneralFlagMask & general) != 0 || (netData.m_SideFlagMask & side) != 0);
				if (num || flag)
				{
					PathEdge value = new PathEdge
					{
						m_Entity = endPoint.m_OriginalEntity,
						m_Invert = (endPoint.m_CurvePosition < startPoint.m_CurvePosition),
						m_Upgrade = flag
					};
					path.Add(in value);
				}
			}
			else
			{
				if (!nodeData.HasComponent(endPoint.m_OriginalEntity))
				{
					return;
				}
				NetData netData2 = prefabNetDatas[prefabRefData[endPoint.m_OriginalEntity].m_Prefab];
				bool flag2 = (prefabNetData.m_RequiredLayers & netData2.m_RequiredLayers) != 0;
				if (flag2)
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = connectedEdgeData[endPoint.m_OriginalEntity];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity edge = dynamicBuffer[i].m_Edge;
						Edge edge2 = edgeData[edge];
						if (edge2.m_Start == endPoint.m_OriginalEntity || edge2.m_End == endPoint.m_OriginalEntity)
						{
							flag2 = false;
							break;
						}
					}
				}
				bool flag3 = !flag2 && (placeableNetData.m_PlacementFlags & (Game.Net.PlacementFlags.IsUpgrade | Game.Net.PlacementFlags.NodeUpgrade)) == (Game.Net.PlacementFlags.IsUpgrade | Game.Net.PlacementFlags.NodeUpgrade) && ((netData2.m_GeneralFlagMask & general) != 0 || (netData2.m_SideFlagMask & side) != 0);
				if (flag2 || flag3)
				{
					PathEdge value = new PathEdge
					{
						m_Entity = endPoint.m_OriginalEntity,
						m_Upgrade = flag3
					};
					path.Add(in value);
				}
			}
			return;
		}
		NativeMinHeap<PathItem> nativeMinHeap = new NativeMinHeap<PathItem>(100, Allocator.Temp);
		NativeParallelHashMap<Entity, Entity> nativeParallelHashMap = new NativeParallelHashMap<Entity, Entity>(100, Allocator.Temp);
		if (edgeData.TryGetComponent(endPoint.m_OriginalEntity, out var componentData))
		{
			NetData netData3 = prefabNetDatas[prefabRefData[endPoint.m_OriginalEntity].m_Prefab];
			bool num2 = (prefabNetData.m_RequiredLayers & netData3.m_RequiredLayers) != 0;
			bool flag4 = !num2 && (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.IsUpgrade) != Game.Net.PlacementFlags.None && ((netData3.m_GeneralFlagMask & general) != 0 || (netData3.m_SideFlagMask & side) != 0);
			if (num2 || flag4)
			{
				nativeMinHeap.Insert(new PathItem
				{
					m_Node = componentData.m_Start,
					m_Edge = endPoint.m_OriginalEntity,
					m_Cost = 0f
				});
				nativeMinHeap.Insert(new PathItem
				{
					m_Node = componentData.m_End,
					m_Edge = endPoint.m_OriginalEntity,
					m_Cost = 0f
				});
			}
		}
		else if (nodeData.HasComponent(endPoint.m_OriginalEntity))
		{
			nativeMinHeap.Insert(new PathItem
			{
				m_Node = endPoint.m_OriginalEntity,
				m_Edge = Entity.Null,
				m_Cost = 0f
			});
		}
		Entity entity = Entity.Null;
		while (nativeMinHeap.Length != 0)
		{
			PathItem pathItem = nativeMinHeap.Extract();
			if (pathItem.m_Edge == startPoint.m_OriginalEntity)
			{
				nativeParallelHashMap[pathItem.m_Node] = pathItem.m_Edge;
				entity = pathItem.m_Node;
				break;
			}
			if (!nativeParallelHashMap.TryAdd(pathItem.m_Node, pathItem.m_Edge))
			{
				continue;
			}
			if (pathItem.m_Node == startPoint.m_OriginalEntity)
			{
				entity = pathItem.m_Node;
				break;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer2 = connectedEdgeData[pathItem.m_Node];
			PrefabRef prefabRef = default(PrefabRef);
			if (pathItem.m_Edge != Entity.Null)
			{
				prefabRef = prefabRefData[pathItem.m_Edge];
			}
			for (int j = 0; j < dynamicBuffer2.Length; j++)
			{
				Entity edge3 = dynamicBuffer2[j].m_Edge;
				if (edge3 == pathItem.m_Edge)
				{
					continue;
				}
				componentData = edgeData[edge3];
				Entity entity2;
				if (componentData.m_Start == pathItem.m_Node)
				{
					entity2 = componentData.m_End;
				}
				else
				{
					if (!(componentData.m_End == pathItem.m_Node))
					{
						continue;
					}
					entity2 = componentData.m_Start;
				}
				if (!nativeParallelHashMap.ContainsKey(entity2) || !(edge3 != startPoint.m_OriginalEntity))
				{
					PrefabRef prefabRef2 = prefabRefData[edge3];
					NetData netData4 = prefabNetDatas[prefabRef2.m_Prefab];
					bool num3 = (prefabNetData.m_RequiredLayers & netData4.m_RequiredLayers) != 0;
					bool flag5 = !num3 && (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.IsUpgrade) != Game.Net.PlacementFlags.None && ((netData4.m_GeneralFlagMask & general) != 0 || (netData4.m_SideFlagMask & side) != 0);
					if (num3 || flag5)
					{
						Curve curve = curveData[edge3];
						float num4 = pathItem.m_Cost + curve.m_Length;
						num4 += math.select(0f, 9.9f, prefabRef2.m_Prefab != prefabRef.m_Prefab);
						num4 += math.select(0f, 10f, dynamicBuffer2.Length > 2);
						nativeMinHeap.Insert(new PathItem
						{
							m_Node = entity2,
							m_Edge = edge3,
							m_Cost = num4
						});
					}
				}
			}
		}
		Entity item;
		while (nativeParallelHashMap.TryGetValue(entity, out item) && !(item == Entity.Null))
		{
			componentData = edgeData[item];
			NetData netData5 = prefabNetDatas[prefabRefData[item].m_Prefab];
			bool flag6 = componentData.m_End == entity;
			bool flag7 = (prefabNetData.m_RequiredLayers & netData5.m_RequiredLayers) != 0;
			Entity entity3 = (flag6 ? componentData.m_Start : componentData.m_End);
			if (flag7 || (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.NodeUpgrade) == 0)
			{
				PathEdge value = new PathEdge
				{
					m_Entity = item,
					m_Invert = flag6,
					m_Upgrade = !flag7
				};
				path.Add(in value);
			}
			else
			{
				if (entity == startPoint.m_OriginalEntity)
				{
					PathEdge value = new PathEdge
					{
						m_Entity = entity,
						m_Upgrade = true
					};
					path.Add(in value);
				}
				if (item != endPoint.m_OriginalEntity)
				{
					PathEdge value = new PathEdge
					{
						m_Entity = entity3,
						m_Upgrade = true
					};
					path.Add(in value);
				}
			}
			if (!(item == endPoint.m_OriginalEntity))
			{
				entity = entity3;
				continue;
			}
			break;
		}
	}

	private static bool IsNearEnd(Entity edge, Curve curve, float3 position, bool invert, ref ComponentLookup<EdgeGeometry> edgeGeometryData)
	{
		if (edgeGeometryData.TryGetComponent(edge, out var componentData))
		{
			Bezier4x3 bezier4x = MathUtils.Lerp(componentData.m_Start.m_Left, componentData.m_Start.m_Right, 0.5f);
			Bezier4x3 bezier4x2 = MathUtils.Lerp(componentData.m_End.m_Left, componentData.m_End.m_Right, 0.5f);
			float t;
			float num = MathUtils.Distance(bezier4x.xz, position.xz, out t);
			float t2;
			float num2 = MathUtils.Distance(bezier4x2.xz, position.xz, out t2);
			float middleLength = componentData.m_Start.middleLength;
			float middleLength2 = componentData.m_End.middleLength;
			return math.select(t * middleLength, middleLength + t2 * middleLength2, num2 < num) > (middleLength + middleLength2) * 0.5f != invert;
		}
		MathUtils.Distance(curve.m_Bezier.xz, position.xz, out var t3);
		return t3 > 0.5f;
	}

	private static bool AllowUpgrade(Entity entity, bool isUpgrade, bool editorMode, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Edge> edgeData)
	{
		bool flag = false;
		Entity entity2 = entity;
		Owner componentData;
		while (ownerData.TryGetComponent(entity2, out componentData))
		{
			if (edgeData.HasComponent(componentData.m_Owner))
			{
				flag = true;
				if (!isUpgrade)
				{
					return false;
				}
			}
			else if (!editorMode || flag)
			{
				return false;
			}
			entity2 = componentData.m_Owner;
		}
		return true;
	}

	private static bool AllowUpgrade(Entity entity, bool isUpgrade, bool editorMode, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Edge> edgeData, ref ComponentLookup<PrefabRef> prefabRefData, ref BufferLookup<ConnectedEdge> connectedEdgeData, ref BufferLookup<AuxiliaryNet> auxiliaryNetData, ref BufferLookup<FixedNetElement> fixedNetElementData)
	{
		if (!isUpgrade && prefabRefData.TryGetComponent(entity, out var componentData) && ((auxiliaryNetData.TryGetBuffer(componentData.m_Prefab, out var bufferData) && bufferData.Length != 0) || fixedNetElementData.HasBuffer(componentData.m_Prefab)))
		{
			return false;
		}
		if (AllowUpgrade(entity, isUpgrade, editorMode, ref ownerData, ref edgeData))
		{
			return true;
		}
		if (connectedEdgeData.TryGetBuffer(entity, out var bufferData2))
		{
			for (int i = 0; i < bufferData2.Length; i++)
			{
				Entity edge = bufferData2[i].m_Edge;
				Edge edge2 = edgeData[edge];
				if ((edge2.m_Start == entity || edge2.m_End == entity) && AllowUpgrade(edge, isUpgrade, editorMode, ref ownerData, ref edgeData))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void AddControlPoints(NativeList<ControlPoint> controlPoints, NativeList<UpgradeState> upgradeStates, NativeReference<AppliedUpgrade> appliedUpgrade, ControlPoint startPoint, ControlPoint endPoint, NativeList<PathEdge> path, Snap snap, bool removeUpgrade, bool leftHandTraffic, bool editorMode, NetGeometryData prefabGeometryData, RoadData prefabRoadData, PlaceableNetData placeableNetData, SubReplacement subReplacement, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<BorderDistrict> borderDistrictData, ref ComponentLookup<District> districtData, ref ComponentLookup<Edge> edgeData, ref ComponentLookup<Game.Net.Node> nodeData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Composition> compositionData, ref ComponentLookup<Upgraded> upgradedData, ref ComponentLookup<EdgeGeometry> edgeGeometryData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<NetData> prefabNetData, ref ComponentLookup<NetGeometryData> prefabNetGeometryData, ref ComponentLookup<NetCompositionData> prefabCompositionData, ref ComponentLookup<RoadComposition> prefabRoadCompositionData, ref BufferLookup<ConnectedEdge> connectedEdgeData, ref BufferLookup<SubReplacement> subReplacementData, ref BufferLookup<AuxiliaryNet> auxiliaryNetData, ref BufferLookup<FixedNetElement> fixedNetElementData)
	{
		controlPoints.Add(in startPoint);
		float num = 0f;
		float num2 = 0f;
		bool flag = false;
		CompositionFlags.General general = placeableNetData.m_SetUpgradeFlags.m_General | placeableNetData.m_UnsetUpgradeFlags.m_General;
		CompositionFlags.Side side = placeableNetData.m_SetUpgradeFlags.m_Left | placeableNetData.m_SetUpgradeFlags.m_Right | placeableNetData.m_UnsetUpgradeFlags.m_Left | placeableNetData.m_UnsetUpgradeFlags.m_Right;
		if (path.Length != 0)
		{
			PathEdge pathEdge = path[path.Length - 1];
			if (edgeData.HasComponent(pathEdge.m_Entity))
			{
				NetData netData = prefabNetData[prefabRefData[pathEdge.m_Entity].m_Prefab];
				bool flag2 = (netData.m_GeneralFlagMask & general) != 0;
				bool flag3 = (netData.m_SideFlagMask & side) != 0;
				if (pathEdge.m_Upgrade && !flag3)
				{
					flag = true;
				}
				else
				{
					Composition composition = compositionData[pathEdge.m_Entity];
					Curve curve = curveData[pathEdge.m_Entity];
					NetCompositionData netCompositionData = prefabCompositionData[composition.m_Edge];
					num2 = netCompositionData.m_Width * 0.5f;
					MathUtils.Distance(curve.m_Bezier.xz, endPoint.m_HitPosition.xz, out var t);
					float3 @float = MathUtils.Position(curve.m_Bezier, t);
					float3 value = MathUtils.Tangent(curve.m_Bezier, t);
					value = MathUtils.Normalize(value, value.xz);
					num = math.dot(endPoint.m_HitPosition.xz - @float.xz, MathUtils.Right(value.xz));
					num = math.select(num, 0f - num, pathEdge.m_Invert);
					flag = flag2 && math.abs(num) <= netCompositionData.m_Width * (1f / 6f);
				}
			}
		}
		for (int i = 0; i < path.Length; i++)
		{
			PathEdge pathEdge2 = path[i];
			if (!AllowUpgrade(pathEdge2.m_Entity, pathEdge2.m_Upgrade, editorMode, ref ownerData, ref edgeData, ref prefabRefData, ref connectedEdgeData, ref auxiliaryNetData, ref fixedNetElementData))
			{
				continue;
			}
			if (edgeData.TryGetComponent(pathEdge2.m_Entity, out var componentData))
			{
				Curve curve2 = curveData[pathEdge2.m_Entity];
				if (pathEdge2.m_Invert)
				{
					CommonUtils.Swap(ref componentData.m_Start, ref componentData.m_End);
					curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
				}
				float num3 = 0f;
				if (pathEdge2.m_Upgrade)
				{
					NetData netData2 = prefabNetData[prefabRefData[pathEdge2.m_Entity].m_Prefab];
					if ((netData2.m_RequiredLayers & Layer.Road) != Layer.None && (placeableNetData.m_SetUpgradeFlags.m_Left | placeableNetData.m_SetUpgradeFlags.m_Right) == CompositionFlags.Side.ForbidSecondary && borderDistrictData.TryGetComponent(pathEdge2.m_Entity, out var componentData2) && AreaUtils.CheckOption(componentData2, DistrictOption.ForbidBicycles, ref districtData))
					{
						continue;
					}
					UpgradeState value2 = new UpgradeState
					{
						m_IsUpgrading = true
					};
					if (upgradedData.TryGetComponent(pathEdge2.m_Entity, out var componentData3))
					{
						value2.m_OldFlags = componentData3.m_Flags;
					}
					if (compositionData.TryGetComponent(pathEdge2.m_Entity, out var componentData4))
					{
						if (prefabCompositionData.TryGetComponent(componentData4.m_StartNode, out var componentData5))
						{
							if ((componentData5.m_Flags.m_General & CompositionFlags.General.Crosswalk) != 0)
							{
								if ((componentData5.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
								{
									value2.m_OldFlags.m_Left |= CompositionFlags.Side.AddCrosswalk;
								}
								else
								{
									value2.m_OldFlags.m_Right |= CompositionFlags.Side.AddCrosswalk;
								}
							}
							else if ((componentData5.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
							{
								value2.m_OldFlags.m_Left |= CompositionFlags.Side.RemoveCrosswalk;
							}
							else
							{
								value2.m_OldFlags.m_Right |= CompositionFlags.Side.RemoveCrosswalk;
							}
						}
						if (prefabCompositionData.TryGetComponent(componentData4.m_EndNode, out var componentData6))
						{
							if ((componentData6.m_Flags.m_General & CompositionFlags.General.Crosswalk) != 0)
							{
								if ((componentData6.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
								{
									value2.m_OldFlags.m_Left |= CompositionFlags.Side.AddCrosswalk;
								}
								else
								{
									value2.m_OldFlags.m_Right |= CompositionFlags.Side.AddCrosswalk;
								}
							}
							else if ((componentData6.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
							{
								value2.m_OldFlags.m_Left |= CompositionFlags.Side.RemoveCrosswalk;
							}
							else
							{
								value2.m_OldFlags.m_Right |= CompositionFlags.Side.RemoveCrosswalk;
							}
						}
					}
					CompositionFlags compositionFlags;
					CompositionFlags compositionFlags2;
					if (num < 0f != pathEdge2.m_Invert)
					{
						compositionFlags = NetCompositionHelpers.InvertCompositionFlags(placeableNetData.m_SetUpgradeFlags);
						compositionFlags2 = NetCompositionHelpers.InvertCompositionFlags(placeableNetData.m_UnsetUpgradeFlags);
						value2.m_SubReplacementSide = SubReplacementSide.Left;
					}
					else
					{
						compositionFlags = placeableNetData.m_SetUpgradeFlags;
						compositionFlags2 = placeableNetData.m_UnsetUpgradeFlags;
						value2.m_SubReplacementSide = SubReplacementSide.Right;
					}
					CompositionFlags.Side side2 = CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk | CompositionFlags.Side.ForbidStraight;
					CompositionFlags.Side side3 = (compositionFlags.m_Left | compositionFlags.m_Right) & side2;
					CompositionFlags.Side side4 = (compositionFlags2.m_Left | compositionFlags2.m_Right) & side2;
					if ((side3 | side4) != 0)
					{
						bool2 @bool = false;
						if ((i > 0) & (i < path.Length - 1))
						{
							@bool = true;
						}
						else
						{
							if (i == 0)
							{
								bool flag4 = IsNearEnd(pathEdge2.m_Entity, curve2, startPoint.m_HitPosition, pathEdge2.m_Invert, ref edgeGeometryData);
								@bool |= new bool2(!flag4, flag4);
								if (i + 1 < path.Length && edgeData.TryGetComponent(path[i + 1].m_Entity, out var componentData7))
								{
									@bool |= new bool2((componentData.m_Start == componentData7.m_Start) | (componentData.m_Start == componentData7.m_End), (componentData.m_End == componentData7.m_Start) | (componentData.m_End == componentData7.m_End));
								}
							}
							if (i == path.Length - 1)
							{
								bool flag5 = IsNearEnd(pathEdge2.m_Entity, curve2, endPoint.m_HitPosition, pathEdge2.m_Invert, ref edgeGeometryData);
								@bool |= new bool2(!flag5, flag5);
								if (i - 1 >= 0 && edgeData.TryGetComponent(path[i - 1].m_Entity, out var componentData8))
								{
									@bool |= new bool2((componentData.m_Start == componentData8.m_Start) | (componentData.m_Start == componentData8.m_End), (componentData.m_End == componentData8.m_Start) | (componentData.m_End == componentData8.m_End));
								}
							}
						}
						if (pathEdge2.m_Invert != leftHandTraffic)
						{
							@bool = @bool.yx;
						}
						if (@bool.x)
						{
							compositionFlags.m_Left |= side3;
							compositionFlags2.m_Left |= side4;
						}
						else
						{
							compositionFlags.m_Left &= ~side3;
							compositionFlags2.m_Left &= ~side4;
						}
						if (@bool.y)
						{
							compositionFlags.m_Right |= side3;
							compositionFlags2.m_Right |= side4;
						}
						else
						{
							compositionFlags.m_Right &= ~side3;
							compositionFlags2.m_Right &= ~side4;
						}
					}
					bool flag6 = (netData2.m_GeneralFlagMask & general) != 0;
					bool flag7 = (netData2.m_SideFlagMask & side) != 0;
					if (flag || !flag7)
					{
						CompositionFlags.Side side5 = ~(CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.SecondaryBeautification | CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.SecondaryLane);
						compositionFlags.m_Left &= side5;
						compositionFlags.m_Right &= side5;
						compositionFlags2.m_Left &= side5;
						compositionFlags2.m_Right &= side5;
					}
					if (!flag || !flag6)
					{
						CompositionFlags.General general2 = ~(CompositionFlags.General.WideMedian | CompositionFlags.General.PrimaryMiddleBeautification | CompositionFlags.General.SecondaryMiddleBeautification);
						compositionFlags.m_General &= general2;
						compositionFlags2.m_General &= general2;
					}
					if (flag && flag6)
					{
						value2.m_SubReplacementSide = SubReplacementSide.Middle;
						value2.m_SubReplacementType = subReplacement.m_Type;
					}
					else if (!flag && flag7)
					{
						value2.m_SubReplacementType = subReplacement.m_Type;
					}
					if (value2.m_SubReplacementType != SubReplacementType.None)
					{
						if (!removeUpgrade)
						{
							value2.m_SubReplacementPrefab = subReplacement.m_Prefab;
						}
						bool flag8 = false;
						bool flag9 = subReplacement.m_Prefab != Entity.Null;
						if (subReplacementData.TryGetBuffer(pathEdge2.m_Entity, out var bufferData))
						{
							for (int j = 0; j < bufferData.Length; j++)
							{
								SubReplacement subReplacement2 = bufferData[j];
								if (subReplacement2.m_Side == value2.m_SubReplacementSide && subReplacement2.m_Type == value2.m_SubReplacementType)
								{
									flag8 = true;
									flag9 = subReplacement2.m_Prefab != subReplacement.m_Prefab;
									break;
								}
							}
						}
						if (!(removeUpgrade ? flag8 : flag9))
						{
							value2.m_SubReplacementType = SubReplacementType.None;
						}
					}
					if (removeUpgrade)
					{
						compositionFlags2.m_General = (CompositionFlags.General)0u;
						compositionFlags2.m_Left &= CompositionFlags.Side.RemoveCrosswalk;
						compositionFlags2.m_Right &= CompositionFlags.Side.RemoveCrosswalk;
						value2.m_AddFlags = compositionFlags2;
						value2.m_RemoveFlags = compositionFlags;
					}
					else
					{
						value2.m_AddFlags = compositionFlags;
						value2.m_RemoveFlags = compositionFlags2;
					}
					upgradeStates.Add(in value2);
				}
				else
				{
					upgradeStates.Add(default(UpgradeState));
					if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0)
					{
						num3 = num;
						if ((snap & Snap.ExistingGeometry) != Snap.None)
						{
							Composition composition2 = compositionData[pathEdge2.m_Entity];
							NetGeometryData netGeometryData = prefabNetGeometryData[prefabRefData[pathEdge2.m_Entity].m_Prefab];
							prefabRoadCompositionData.TryGetComponent(composition2.m_Edge, out var componentData9);
							float num4 = math.abs(netGeometryData.m_DefaultWidth - prefabGeometryData.m_DefaultWidth);
							if ((snap & Snap.CellLength) != Snap.None && (componentData9.m_Flags & prefabRoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0)
							{
								int cellWidth = ZoneUtils.GetCellWidth(netGeometryData.m_DefaultWidth);
								int cellWidth2 = ZoneUtils.GetCellWidth(prefabGeometryData.m_DefaultWidth);
								float offset = math.select(0f, 4f, ((cellWidth ^ cellWidth2) & 1) != 0);
								num4 = (float)math.abs(cellWidth - cellWidth2) * 8f;
								num3 *= (num4 * 0.5f + 3.92f) / num2;
								num3 = MathUtils.Snap(num3, 8f, offset);
								num3 = math.clamp(num3, num4 * -0.5f, num4 * 0.5f);
							}
							else if (num4 > 1.6f)
							{
								num3 *= num4 * 0.74f / num2;
								num3 = MathUtils.Snap(num3, num4 * 0.5f);
								num3 = math.clamp(num3, num4 * -0.5f, num4 * 0.5f);
							}
							else
							{
								num3 = 0f;
							}
						}
					}
				}
				ControlPoint value3 = endPoint;
				value3.m_OriginalEntity = componentData.m_Start;
				value3.m_Position = curve2.m_Bezier.a;
				ControlPoint value4 = endPoint;
				value4.m_OriginalEntity = componentData.m_End;
				value4.m_Position = curve2.m_Bezier.d;
				if (math.abs(num3) >= 0.01f)
				{
					float3 value5 = MathUtils.StartTangent(curve2.m_Bezier);
					float3 value6 = MathUtils.EndTangent(curve2.m_Bezier);
					value5 = MathUtils.Normalize(value5, value5.xz);
					value6 = MathUtils.Normalize(value6, value6.xz);
					value3.m_Position.xz += MathUtils.Right(value5.xz) * num3;
					value4.m_Position.xz += MathUtils.Right(value6.xz) * num3;
				}
				controlPoints.Add(in value3);
				controlPoints.Add(in value4);
			}
			else
			{
				if (!nodeData.TryGetComponent(pathEdge2.m_Entity, out var componentData10))
				{
					continue;
				}
				if (pathEdge2.m_Upgrade)
				{
					UpgradeState value7 = new UpgradeState
					{
						m_IsUpgrading = true
					};
					if (upgradedData.TryGetComponent(pathEdge2.m_Entity, out var componentData11))
					{
						value7.m_OldFlags = componentData11.m_Flags;
					}
					if (connectedEdgeData.TryGetBuffer(pathEdge2.m_Entity, out var bufferData2))
					{
						CompositionFlags compositionFlags3 = default(CompositionFlags);
						for (int k = 0; k < bufferData2.Length; k++)
						{
							Entity edge = bufferData2[k].m_Edge;
							componentData = edgeData[edge];
							Composition componentData14;
							NetCompositionData componentData15;
							if (componentData.m_Start == pathEdge2.m_Entity)
							{
								if (compositionData.TryGetComponent(edge, out var componentData12) && prefabCompositionData.TryGetComponent(componentData12.m_StartNode, out var componentData13))
								{
									compositionFlags3 |= componentData13.m_Flags;
								}
							}
							else if (componentData.m_End == pathEdge2.m_Entity && compositionData.TryGetComponent(edge, out componentData14) && prefabCompositionData.TryGetComponent(componentData14.m_EndNode, out componentData15))
							{
								compositionFlags3 |= componentData15.m_Flags;
							}
						}
						if ((compositionFlags3.m_General & CompositionFlags.General.TrafficLights) != 0)
						{
							value7.m_OldFlags.m_General |= CompositionFlags.General.TrafficLights;
						}
						else
						{
							value7.m_OldFlags.m_General |= CompositionFlags.General.RemoveTrafficLights;
						}
					}
					CompositionFlags setUpgradeFlags = placeableNetData.m_SetUpgradeFlags;
					CompositionFlags unsetUpgradeFlags = placeableNetData.m_UnsetUpgradeFlags;
					if (removeUpgrade)
					{
						unsetUpgradeFlags.m_General &= CompositionFlags.General.RemoveTrafficLights;
						unsetUpgradeFlags.m_Left = (CompositionFlags.Side)0u;
						unsetUpgradeFlags.m_Right = (CompositionFlags.Side)0u;
						value7.m_AddFlags = unsetUpgradeFlags;
						value7.m_RemoveFlags = setUpgradeFlags;
					}
					else
					{
						value7.m_AddFlags = setUpgradeFlags;
						value7.m_RemoveFlags = unsetUpgradeFlags;
					}
					upgradeStates.Add(in value7);
				}
				else
				{
					upgradeStates.Add(default(UpgradeState));
				}
				ControlPoint value8 = endPoint;
				value8.m_OriginalEntity = pathEdge2.m_Entity;
				value8.m_Position = componentData10.m_Position;
				controlPoints.Add(in value8);
				controlPoints.Add(in value8);
			}
		}
		controlPoints.Add(in endPoint);
		AppliedUpgrade value9 = appliedUpgrade.Value;
		if (value9.m_Entity != Entity.Null)
		{
			if (upgradeStates.Length != 1 || path[path.Length - 1].m_Entity != value9.m_Entity || upgradeStates[0].m_AddFlags != value9.m_Flags || upgradeStates[0].m_SubReplacementSide != value9.m_SubReplacementSide || (subReplacement.m_Type != value9.m_SubReplacementType && value9.m_SubReplacementType != SubReplacementType.None) || (subReplacement.m_Prefab != value9.m_SubReplacementPrefab && value9.m_SubReplacementPrefab != Entity.Null))
			{
				appliedUpgrade.Value = default(AppliedUpgrade);
				return;
			}
			UpgradeState value10 = upgradeStates[0];
			value10.m_SkipFlags = true;
			upgradeStates[0] = value10;
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
	public NetToolSystem()
	{
	}
}
