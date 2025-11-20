using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
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
public class CourseSplitSystem : GameSystemBase
{
	private struct IntersectPos : IComparable<IntersectPos>
	{
		public CoursePos m_CoursePos;

		public Bounds1 m_CourseIntersection;

		public Bounds1 m_IntersectionHeightMin;

		public Bounds1 m_IntersectionHeightMax;

		public Bounds1 m_EdgeIntersection;

		public Bounds1 m_EdgeHeightRangeMin;

		public Bounds1 m_EdgeHeightRangeMax;

		public Bounds1 m_CanMove;

		public float m_Priority;

		public int m_CourseIndex;

		public int m_AuxIndex;

		public bool m_IsNode;

		public bool m_IsOptional;

		public bool m_IsStartEnd;

		public bool m_IsTunnel;

		public bool m_IsWaterway;

		public int CompareTo(IntersectPos other)
		{
			return (int)math.sign(m_CourseIntersection.min - other.m_CourseIntersection.min);
		}

		public override int GetHashCode()
		{
			return m_CourseIndex;
		}
	}

	private struct Course
	{
		public CreationDefinition m_CreationDefinition;

		public OwnerDefinition m_OwnerDefinition;

		public NetCourse m_CourseData;

		public Upgraded m_UpgradedData;

		public Entity m_CourseEntity;
	}

	private struct Overlap
	{
		public Entity m_OverlapEntity;

		public int m_CourseIndex;
	}

	[BurstCompile]
	private struct CheckCoursesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> m_UpgradedType;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		public NativeHashMap<Entity, bool> m_DeletedEntities;

		public NativeList<Course> m_CourseList;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CreationDefinition> nativeArray2 = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<OwnerDefinition> nativeArray3 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
			NativeArray<NetCourse> nativeArray4 = chunk.GetNativeArray(ref m_NetCourseType);
			NativeArray<Upgraded> nativeArray5 = chunk.GetNativeArray(ref m_UpgradedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Course value = new Course
				{
					m_CourseEntity = nativeArray[i],
					m_CreationDefinition = nativeArray2[i],
					m_CourseData = nativeArray4[i]
				};
				if (value.m_CreationDefinition.m_Original != Entity.Null)
				{
					m_DeletedEntities.Add(value.m_CreationDefinition.m_Original, (value.m_CreationDefinition.m_Flags & CreationFlags.Delete) != 0);
				}
				else if (m_NetGeometryData.HasComponent(value.m_CreationDefinition.m_Prefab))
				{
					CollectionUtils.TryGet(nativeArray5, i, out value.m_UpgradedData);
					CollectionUtils.TryGet(nativeArray3, i, out value.m_OwnerDefinition);
					m_CourseList.Add(in value);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindOverlapsJob : IJobParallelForDefer
	{
		private struct OverlapIteratorSubData
		{
			public Bounds2 m_Bounds1;

			public Bounds2 m_Bounds2;

			public Bezier4x2 m_Curve1;

			public Bezier4x2 m_Curve2;
		}

		private struct OverlapIterator : INativeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, OverlapIteratorSubData>, IUnsafeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, OverlapIteratorSubData>
		{
			public float m_Range;

			public float m_SizeLimit;

			public int m_CourseIndex;

			public NativeQueue<Overlap>.ParallelWriter m_OverlapQueue;

			public ComponentLookup<Deleted> m_DeletedData;

			public bool Intersect(QuadTreeBoundsXZ bounds, ref OverlapIteratorSubData subData)
			{
				bool2 x = default(bool2);
				x.x = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds1);
				x.y = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds2);
				if (!math.any(x))
				{
					return false;
				}
				if (math.all(x))
				{
					return true;
				}
				while (math.any(MathUtils.Size(subData.m_Bounds1) > m_SizeLimit))
				{
					if (x.x)
					{
						MathUtils.Divide(subData.m_Curve1, out subData.m_Curve1, out subData.m_Curve2, 0.5f);
					}
					else
					{
						MathUtils.Divide(subData.m_Curve2, out subData.m_Curve1, out subData.m_Curve2, 0.5f);
					}
					subData.m_Bounds1 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve1), m_Range);
					subData.m_Bounds2 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve2), m_Range);
					x.x = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds1);
					x.y = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds2);
					if (!math.any(x))
					{
						return false;
					}
					if (math.all(x))
					{
						return true;
					}
				}
				return true;
			}

			public void Iterate(QuadTreeBoundsXZ bounds, OverlapIteratorSubData subData, Entity overlapEntity)
			{
				bool2 x = default(bool2);
				x.x = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds1);
				x.y = MathUtils.Intersect(bounds.m_Bounds.xz, subData.m_Bounds2);
				if (math.any(x) && !m_DeletedData.HasComponent(overlapEntity))
				{
					m_OverlapQueue.Enqueue(new Overlap
					{
						m_CourseIndex = m_CourseIndex,
						m_OverlapEntity = overlapEntity
					});
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> m_AuxiliaryNets;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public NativeList<Course> m_CourseList;

		public NativeQueue<Overlap>.ParallelWriter m_OverlapQueue;

		public void Execute(int index)
		{
			Course course = m_CourseList[index];
			if ((course.m_CourseData.m_StartPosition.m_Flags & course.m_CourseData.m_EndPosition.m_Flags & CoursePosFlags.DisableMerge) != 0)
			{
				return;
			}
			float num = m_NetData[course.m_CreationDefinition.m_Prefab].m_DefaultWidth * 0.5f;
			if (m_AuxiliaryNets.TryGetBuffer(course.m_CreationDefinition.m_Prefab, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					AuxiliaryNet auxiliaryNet = bufferData[i];
					m_NetData.TryGetComponent(auxiliaryNet.m_Prefab, out var componentData);
					num = math.max(num, math.abs(auxiliaryNet.m_Position.x) + componentData.m_DefaultWidth * 0.5f);
				}
			}
			OverlapIterator iterator = new OverlapIterator
			{
				m_Range = num,
				m_SizeLimit = num * 4f,
				m_CourseIndex = index,
				m_OverlapQueue = m_OverlapQueue,
				m_DeletedData = m_DeletedData
			};
			OverlapIteratorSubData subData = default(OverlapIteratorSubData);
			MathUtils.Divide(course.m_CourseData.m_Curve.xz, out subData.m_Curve1, out subData.m_Curve2, 0.5f);
			subData.m_Bounds1 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve1), num);
			subData.m_Bounds2 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve2), num);
			m_SearchTree.Iterate(ref iterator, subData);
		}
	}

	[BurstCompile]
	private struct DequeueOverlapsJob : IJob
	{
		public NativeQueue<Overlap> m_OverlapQueue;

		public NativeList<Overlap> m_OverlapList;

		public void Execute()
		{
			Overlap item;
			while (m_OverlapQueue.TryDequeue(out item))
			{
				m_OverlapList.Add(in item);
			}
		}
	}

	[BurstCompile]
	private struct CheckCourseIntersectionsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> m_PrefabAuxiliaryNets;

		[ReadOnly]
		public NativeList<Course> m_CourseList;

		[ReadOnly]
		public NativeList<Overlap> m_OverlapList;

		[ReadOnly]
		public NativeHashMap<Entity, bool> m_DeletedEntities;

		public NativeParallelQueue<IntersectPos>.Writer m_Results;

		public void Execute(int index)
		{
			Overlap overlap = m_OverlapList[index];
			if (!m_EdgeGeometryData.HasComponent(overlap.m_OverlapEntity))
			{
				return;
			}
			Entity entity = overlap.m_OverlapEntity;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData) && !m_BuildingData.HasComponent(entity))
			{
				entity = componentData.m_Owner;
				if (m_DeletedData.HasComponent(entity))
				{
					return;
				}
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && (componentData2.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden))
			{
				return;
			}
			Course course = m_CourseList[overlap.m_CourseIndex];
			CheckCourseIntersections(overlap, course, course, -1);
			if (!m_PrefabAuxiliaryNets.TryGetBuffer(course.m_CreationDefinition.m_Prefab, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				AuxiliaryNet auxiliaryNet = bufferData[i];
				Course course2 = course;
				course2.m_CreationDefinition = GetAuxDefinition(course.m_CreationDefinition, auxiliaryNet);
				course2.m_CourseData = course.m_CourseData;
				if (GetAuxCourse(ref course2.m_CourseData, auxiliaryNet, invert: false))
				{
					CheckCourseIntersections(overlap, course, course2, i);
				}
			}
		}

		public void CheckCourseIntersections(Overlap overlap, Course mainCourse, Course course, int auxIndex)
		{
			Edge edgeData = m_EdgeData[overlap.m_OverlapEntity];
			bool3 @bool = true;
			if (m_DeletedEntities.TryGetValue(overlap.m_OverlapEntity, out var item) && item)
			{
				@bool.x = !WillBeOrphan(edgeData.m_Start);
				@bool.y = false;
				@bool.z = !WillBeOrphan(edgeData.m_End);
				if (!math.any(@bool.xz))
				{
					return;
				}
			}
			Curve curve = m_CurveData[overlap.m_OverlapEntity];
			Composition composition = m_CompositionData[overlap.m_OverlapEntity];
			EdgeGeometry geometry = m_EdgeGeometryData[overlap.m_OverlapEntity];
			EdgeNodeGeometry geometry2 = m_StartNodeGeometryData[overlap.m_OverlapEntity].m_Geometry;
			EdgeNodeGeometry geometry3 = m_EndNodeGeometryData[overlap.m_OverlapEntity].m_Geometry;
			NetGeometryData prefabGeometryData = m_PrefabGeometryData[course.m_CreationDefinition.m_Prefab];
			int falseValue = math.max(4, (int)math.ceil(math.log(course.m_CourseData.m_Length * 16f / prefabGeometryData.m_EdgeLengthRange.max) * 1.442695f));
			falseValue = math.select(falseValue, 0, course.m_CourseData.m_Length < prefabGeometryData.m_DefaultWidth * 0.01f);
			bool isWaterway = false;
			float2 courseOffset = new float2(course.m_CourseData.m_StartPosition.m_CourseDelta, course.m_CourseData.m_EndPosition.m_CourseDelta);
			IntersectPos currentIntersectPos = default(IntersectPos);
			IntersectPos result = default(IntersectPos);
			IntersectPos result2 = default(IntersectPos);
			currentIntersectPos.m_Priority = -1f;
			result.m_Priority = -1f;
			result2.m_Priority = -1f;
			PrefabRef prefabRef = m_PrefabRefData[overlap.m_OverlapEntity];
			NetGeometryData prefabGeometryData2 = m_PrefabGeometryData[prefabRef.m_Prefab];
			NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
			NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
			NetCompositionData prefabCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
			if (m_PrefabAuxiliaryNets.HasBuffer(prefabRef.m_Prefab))
			{
				NetData netData = m_PrefabNetData[course.m_CreationDefinition.m_Prefab];
				if ((m_PrefabNetData[prefabRef.m_Prefab].m_ConnectLayers & Layer.Waterway) == 0 || (netData.m_RequiredLayers & Layer.Waterway) == 0)
				{
					prefabCompositionData.m_HeightRange = prefabGeometryData2.m_DefaultHeightRange;
					prefabCompositionData2.m_HeightRange = prefabGeometryData2.m_DefaultHeightRange;
					prefabCompositionData3.m_HeightRange = prefabGeometryData2.m_DefaultHeightRange;
				}
			}
			if ((prefabGeometryData.m_MergeLayers & prefabGeometryData2.m_MergeLayers) == 0)
			{
				NetData netData2 = m_PrefabNetData[course.m_CreationDefinition.m_Prefab];
				NetData netData3 = m_PrefabNetData[prefabRef.m_Prefab];
				if (!NetUtils.CanConnect(netData2, netData3) && (((prefabGeometryData.m_Flags | prefabGeometryData2.m_Flags) & Game.Net.GeometryFlags.Marker) != 0 || ((netData3.m_RequiredLayers & Layer.Waterway) != Layer.None && auxIndex != -1 && (m_PrefabNetData[mainCourse.m_CreationDefinition.m_Prefab].m_ConnectLayers & Layer.Waterway) != Layer.None) || ((netData2.m_RequiredLayers & Layer.Waterway) != Layer.None && m_OwnerData.TryGetComponent(overlap.m_OverlapEntity, out var componentData) && m_PrefabRefData.TryGetComponent(componentData.m_Owner, out var componentData2) && m_PrefabNetData.TryGetComponent(componentData2.m_Prefab, out var componentData3) && (componentData3.m_ConnectLayers & Layer.Waterway) != Layer.None)))
				{
					return;
				}
				isWaterway = (netData2.m_RequiredLayers & Layer.Waterway) == 0 && (netData3.m_RequiredLayers & Layer.Waterway) != 0;
			}
			if (prefabGeometryData2.m_MergeLayers == Layer.None || !@bool.y)
			{
				if (@bool.x)
				{
					CheckNodeGeometry(course.m_CourseData, overlap.m_CourseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, overlap.m_OverlapEntity, edgeData.m_Start, curve.m_Bezier.a, courseOffset, 0f);
				}
				if (@bool.z)
				{
					CheckNodeGeometry(course.m_CourseData, overlap.m_CourseIndex, auxIndex, ref result2, prefabGeometryData, prefabCompositionData3, overlap.m_OverlapEntity, edgeData.m_End, curve.m_Bezier.d, courseOffset, 1f);
				}
			}
			else
			{
				if (@bool.x)
				{
					CheckNodeGeometry(course.m_CourseData, overlap.m_CourseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, overlap.m_OverlapEntity, edgeData.m_Start, geometry2, courseOffset, 0f, falseValue, isWaterway);
				}
				if (@bool.z)
				{
					CheckNodeGeometry(course.m_CourseData, overlap.m_CourseIndex, auxIndex, ref result2, prefabGeometryData, prefabCompositionData3, overlap.m_OverlapEntity, edgeData.m_End, geometry3, courseOffset, 1f, falseValue, isWaterway);
				}
			}
			if (@bool.y)
			{
				CheckEdgeGeometry(course.m_CourseData, overlap.m_CourseIndex, auxIndex, ref result, ref result2, ref currentIntersectPos, prefabGeometryData, prefabGeometryData2, prefabCompositionData, overlap.m_OverlapEntity, edgeData, geometry, curve.m_Bezier, courseOffset, falseValue);
			}
			if (result.m_Priority != -1f)
			{
				m_Results.Enqueue(result);
			}
			if (result2.m_Priority != -1f)
			{
				m_Results.Enqueue(result2);
			}
			if (currentIntersectPos.m_Priority != -1f)
			{
				m_Results.Enqueue(currentIntersectPos);
			}
		}

		private bool WillBeOrphan(Entity node)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if ((edge2.m_Start == node || edge2.m_End == node) && (!m_DeletedEntities.TryGetValue(edge, out var item) || !item))
				{
					return false;
				}
			}
			return true;
		}

		private void CheckEdgeGeometry(NetCourse courseData, int courseIndex, int auxIndex, ref IntersectPos startIntersectPos, ref IntersectPos endIntersectPos, ref IntersectPos currentIntersectPos, NetGeometryData prefabGeometryData, NetGeometryData prefabGeometryData2, NetCompositionData prefabCompositionData2, Entity edge, Edge edgeData, EdgeGeometry geometry, Bezier4x3 curve, float2 courseOffset, int iterations)
		{
			Bounds2 bounds = MathUtils.Bounds(MathUtils.Cut(courseData.m_Curve.xz, courseOffset));
			bounds.min -= prefabGeometryData.m_DefaultWidth * 0.5f;
			bounds.max += prefabGeometryData.m_DefaultWidth * 0.5f;
			if (!MathUtils.Intersect(bounds, geometry.m_Bounds.xz))
			{
				return;
			}
			if (iterations <= 0)
			{
				IntersectPos lastIntersectPos = new IntersectPos
				{
					m_Priority = -1f
				};
				CheckEdgeSegment(courseData, courseIndex, auxIndex, ref startIntersectPos, ref endIntersectPos, ref currentIntersectPos, ref lastIntersectPos, prefabGeometryData, prefabGeometryData2, prefabCompositionData2, edge, edgeData, geometry.m_Start, curve, courseOffset, new float2(0f, 0.5f));
				CheckEdgeSegment(courseData, courseIndex, auxIndex, ref startIntersectPos, ref endIntersectPos, ref currentIntersectPos, ref lastIntersectPos, prefabGeometryData, prefabGeometryData2, prefabCompositionData2, edge, edgeData, geometry.m_End, curve, courseOffset, new float2(0.5f, 1f));
				if (lastIntersectPos.m_Priority != -1f)
				{
					Add(ref currentIntersectPos, lastIntersectPos);
				}
			}
			else
			{
				float3 @float = new float3(courseOffset.x, math.lerp(courseOffset.x, courseOffset.y, 0.5f), courseOffset.y);
				CheckEdgeGeometry(courseData, courseIndex, auxIndex, ref startIntersectPos, ref endIntersectPos, ref currentIntersectPos, prefabGeometryData, prefabGeometryData2, prefabCompositionData2, edge, edgeData, geometry, curve, @float.xy, iterations - 1);
				CheckEdgeGeometry(courseData, courseIndex, auxIndex, ref startIntersectPos, ref endIntersectPos, ref currentIntersectPos, prefabGeometryData, prefabGeometryData2, prefabCompositionData2, edge, edgeData, geometry, curve, @float.yz, iterations - 1);
			}
		}

		private void CheckNodeGeometry(NetCourse courseData, int courseIndex, int auxIndex, ref IntersectPos result, NetGeometryData prefabGeometryData, NetCompositionData prefabCompositionData2, Entity edge, Entity node, EdgeNodeGeometry geometry, float2 courseOffset, float edgeOffset, int iterations, bool isWaterway)
		{
			Bounds2 bounds = MathUtils.Bounds(MathUtils.Cut(courseData.m_Curve.xz, courseOffset));
			bounds.min -= prefabGeometryData.m_DefaultWidth * 0.5f;
			bounds.max += prefabGeometryData.m_DefaultWidth * 0.5f;
			if (!MathUtils.Intersect(bounds, geometry.m_Bounds.xz))
			{
				return;
			}
			if (iterations <= 0)
			{
				if (geometry.m_MiddleRadius > 0f)
				{
					Segment right = geometry.m_Right;
					Segment right2 = geometry.m_Right;
					right.m_Right = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
					right.m_Right.d = geometry.m_Middle.d;
					right2.m_Left = right.m_Right;
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, geometry.m_Left, courseOffset, edgeOffset, 0.5f, isWaterway);
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, right, courseOffset, edgeOffset, 1f, isWaterway);
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, right2, courseOffset, edgeOffset, 0f, isWaterway);
				}
				else
				{
					Segment left = geometry.m_Left;
					Segment right3 = geometry.m_Right;
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, left, courseOffset, edgeOffset, 1f, isWaterway);
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, right3, courseOffset, edgeOffset, 0f, isWaterway);
					left.m_Right = geometry.m_Middle;
					right3.m_Left = geometry.m_Middle;
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, left, courseOffset, edgeOffset, 1f, isWaterway);
					CheckNodeSegment(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, right3, courseOffset, edgeOffset, 0f, isWaterway);
				}
			}
			else
			{
				float3 @float = new float3(courseOffset.x, math.lerp(courseOffset.x, courseOffset.y, 0.5f), courseOffset.y);
				CheckNodeGeometry(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, geometry, @float.xy, edgeOffset, iterations - 1, isWaterway);
				CheckNodeGeometry(courseData, courseIndex, auxIndex, ref result, prefabGeometryData, prefabCompositionData2, edge, node, geometry, @float.yz, edgeOffset, iterations - 1, isWaterway);
			}
		}

		private void CheckNodeGeometry(NetCourse courseData, int courseIndex, int auxIndex, ref IntersectPos result, NetGeometryData prefabGeometryData, NetCompositionData prefabCompositionData2, Entity edge, Entity node, float3 nodePos, float2 courseOffset, float edgeOffset)
		{
			Bezier4x2 curve = MathUtils.Cut(courseData.m_Curve.xz, courseOffset);
			float num = prefabGeometryData.m_DefaultWidth * 0.5f;
			Bounds2 bounds = MathUtils.Expand(MathUtils.Bounds(curve), num);
			Circle2 circle = new Circle2(prefabCompositionData2.m_Width * 0.5f, nodePos.xz);
			Bounds2 bounds2 = MathUtils.Bounds(circle);
			if (!MathUtils.Intersect(bounds, bounds2))
			{
				return;
			}
			float t;
			float num2 = MathUtils.Distance(curve, nodePos.xz, out t);
			if (num2 <= num + circle.radius)
			{
				float num3 = math.lerp(courseOffset.x, courseOffset.y, t);
				float3 position = MathUtils.Position(courseData.m_Curve, num3);
				Bounds1 courseIntersection = default(Bounds1);
				courseIntersection.min = math.lerp(courseOffset.x, courseOffset.y, t - 0.01f);
				courseIntersection.max = math.lerp(courseOffset.x, courseOffset.y, t + 0.01f);
				float num4 = math.max(0f, (num2 - num) / circle.radius);
				num4 = math.sqrt(1f - num4 * num4) * circle.radius;
				if (courseIntersection.max < 1f)
				{
					Bounds1 t2 = new Bounds1(courseIntersection.max, 1f);
					MathUtils.ClampLength(courseData.m_Curve, ref t2, num4);
					courseIntersection.max = t2.max;
				}
				if (courseIntersection.min > 0f)
				{
					Bounds1 t3 = new Bounds1(0f, courseIntersection.min);
					MathUtils.ClampLengthInverse(courseData.m_Curve, ref t3, num4);
					courseIntersection.min = t3.min;
				}
				int parentMesh = -1;
				if (m_LocalTransformCacheData.TryGetComponent(node, out var componentData))
				{
					parentMesh = componentData.m_ParentMesh;
				}
				result = default(IntersectPos);
				result.m_CoursePos.m_Entity = node;
				result.m_CoursePos.m_Position = position;
				result.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, num3));
				result.m_CoursePos.m_CourseDelta = num3;
				result.m_CoursePos.m_SplitPosition = edgeOffset;
				result.m_CoursePos.m_Flags = courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.IsGrid);
				result.m_CoursePos.m_Flags |= CoursePosFlags.FreeHeight;
				result.m_CoursePos.m_ParentMesh = parentMesh;
				result.m_CourseIntersection = courseIntersection;
				result.m_IntersectionHeightMin = new Bounds1(position.y);
				result.m_IntersectionHeightMax = new Bounds1(position.y);
				result.m_EdgeIntersection = new Bounds1(edgeOffset, edgeOffset);
				result.m_EdgeHeightRangeMin = nodePos.y + prefabCompositionData2.m_HeightRange;
				result.m_EdgeHeightRangeMax = nodePos.y + prefabCompositionData2.m_HeightRange;
				result.m_Priority = num2;
				result.m_AuxIndex = auxIndex;
				result.m_CourseIndex = courseIndex;
				result.m_IsNode = true;
				result.m_IsTunnel = (prefabCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 && ((prefabCompositionData2.m_Flags.m_Left | prefabCompositionData2.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0;
			}
		}

		private void CheckEdgeSegment(NetCourse courseData, int courseIndex, int auxIndex, ref IntersectPos startIntersectPos, ref IntersectPos endIntersectPos, ref IntersectPos currentIntersectPos, ref IntersectPos lastIntersectPos, NetGeometryData prefabGeometryData, NetGeometryData prefabGeometryData2, NetCompositionData prefabCompositionData2, Entity edge, Edge edgeData, Segment segment, Bezier4x3 curve, float2 courseOffset, float2 edgeOffset)
		{
			float2 @float = new float2(prefabGeometryData.m_DefaultWidth * 0.5f, prefabGeometryData.m_DefaultWidth * -0.5f);
			float3 float2 = MathUtils.Position(courseData.m_Curve, courseOffset.x);
			float3 float3 = MathUtils.Position(courseData.m_Curve, courseOffset.y);
			float3 value = MathUtils.Tangent(courseData.m_Curve, courseOffset.x);
			float3 value2 = MathUtils.Tangent(courseData.m_Curve, courseOffset.y);
			MathUtils.TryNormalize(ref value);
			MathUtils.TryNormalize(ref value2);
			float2 startLeft = float2.xz - value.zx * @float;
			float2 startRight = float2.xz + value.zx * @float;
			float2 endLeft = float3.xz - value2.zx * @float;
			float2 endRight = float3.xz + value2.zx * @float;
			float start = 0f;
			float2 float4 = segment.m_Left.a.xz;
			float2 float5 = segment.m_Right.a.xz;
			Bounds1 bounds = default(Bounds1);
			for (int i = 1; i <= 8; i++)
			{
				float num = (float)i / 8f;
				float2 xz = MathUtils.Position(segment.m_Left, num).xz;
				float2 xz2 = MathUtils.Position(segment.m_Right, num).xz;
				Bounds1 intersectRange;
				Bounds1 intersectRange2;
				bool flag = QuadIntersect(startLeft, startRight, endLeft, endRight, float4, float5, xz, xz2, out intersectRange, out intersectRange2);
				if (courseOffset.x == 0f)
				{
					Line2.Segment line = new Line2.Segment(math.lerp(float4, float5, 0.5f), math.lerp(xz, xz2, 0.5f));
					if (MathUtils.Distance(line, float2.xz, out var t) <= MathUtils.Distance(line, float3.xz, out t) && CircleIntersect(new Circle2(@float.x, float2.xz), float4, float5, xz, xz2, out var intersectRange3))
					{
						intersectRange |= 0f;
						intersectRange2 |= intersectRange3;
						flag = true;
					}
				}
				if (courseOffset.y == 1f)
				{
					Line2.Segment line2 = new Line2.Segment(math.lerp(float4, float5, 0.5f), math.lerp(xz, xz2, 0.5f));
					if (MathUtils.Distance(line2, float2.xz, out var t2) >= MathUtils.Distance(line2, float3.xz, out t2) && CircleIntersect(new Circle2(@float.x, float3.xz), float4, float5, xz, xz2, out var intersectRange4))
					{
						intersectRange |= 1f;
						intersectRange2 |= intersectRange4;
						flag = true;
					}
				}
				if (flag)
				{
					Line2.Segment line3 = new Line2.Segment(float2.xz, float3.xz);
					Line2.Segment line4 = new Line2.Segment(math.lerp(float4, float5, 0.5f), math.lerp(xz, xz2, 0.5f));
					float2 t3;
					float priority = MathUtils.Distance(line3, line4, out t3);
					float num2 = math.lerp(courseOffset.x, courseOffset.y, t3.x);
					float3 position = MathUtils.Position(courseData.m_Curve, num2);
					MathUtils.Distance(curve.xz, position.xz, out t3.y);
					if ((prefabGeometryData2.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
					{
						float value3 = MathUtils.Length(curve.xz, new Bounds1(0f, t3.y));
						value3 = MathUtils.Snap(value3, 4f);
						Bounds1 t4 = new Bounds1(0f, 1f);
						if (MathUtils.ClampLength(curve.xz, ref t4, value3))
						{
							t3.y = t4.max;
						}
					}
					intersectRange.min = math.lerp(courseOffset.x, courseOffset.y, intersectRange.min - 0.01f);
					intersectRange.max = math.lerp(courseOffset.x, courseOffset.y, intersectRange.max + 0.01f);
					bounds.min = math.lerp(start, num, intersectRange2.min - 0.01f);
					bounds.max = math.lerp(start, num, intersectRange2.max + 0.01f);
					intersectRange2.min = math.lerp(edgeOffset.x, edgeOffset.y, bounds.min);
					intersectRange2.max = math.lerp(edgeOffset.x, edgeOffset.y, bounds.max);
					if (prefabCompositionData2.m_NodeOffset > 0f)
					{
						if (intersectRange2.min > 0f)
						{
							Bounds1 t5 = new Bounds1(0f, intersectRange2.min);
							MathUtils.ClampLengthInverse(curve.xz, ref t5, prefabCompositionData2.m_NodeOffset);
							intersectRange2.min = t5.min;
						}
						if (intersectRange2.max > 0f)
						{
							Bounds1 t6 = new Bounds1(intersectRange2.max, 1f);
							MathUtils.ClampLength(curve.xz, ref t6, prefabCompositionData2.m_NodeOffset);
							intersectRange2.max = t6.max;
						}
					}
					int parentMesh = -1;
					if (m_LocalTransformCacheData.TryGetComponent(edgeData.m_Start, out var componentData) && m_LocalTransformCacheData.TryGetComponent(edgeData.m_End, out var componentData2) && componentData.m_ParentMesh == componentData2.m_ParentMesh)
					{
						parentMesh = componentData.m_ParentMesh;
					}
					MathUtils.Distance(curve.xz, MathUtils.Position(courseData.m_Curve.xz, intersectRange.min), out var t7);
					MathUtils.Distance(curve.xz, MathUtils.Position(courseData.m_Curve.xz, intersectRange.max), out var t8);
					if (t8 < t7)
					{
						CommonUtils.Swap(ref bounds.min, ref bounds.max);
					}
					IntersectPos target = new IntersectPos
					{
						m_CoursePos = 
						{
							m_Entity = edge,
							m_Position = position,
							m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, num2)),
							m_CourseDelta = num2,
							m_SplitPosition = t3.y,
							m_Flags = (courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.IsGrid))
						}
					};
					target.m_CoursePos.m_Flags |= CoursePosFlags.FreeHeight;
					target.m_CoursePos.m_ParentMesh = parentMesh;
					target.m_CourseIntersection = intersectRange;
					target.m_IntersectionHeightMin = new Bounds1(MathUtils.Position(curve, t7).y);
					target.m_IntersectionHeightMax = new Bounds1(MathUtils.Position(curve, t8).y);
					target.m_EdgeIntersection = intersectRange2;
					target.m_EdgeHeightRangeMin = GetHeightRange(segment, bounds.min, prefabCompositionData2.m_HeightRange);
					target.m_EdgeHeightRangeMax = GetHeightRange(segment, bounds.max, prefabCompositionData2.m_HeightRange);
					target.m_Priority = priority;
					target.m_CourseIndex = courseIndex;
					target.m_AuxIndex = auxIndex;
					target.m_IsTunnel = (prefabCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
					if (intersectRange2.min <= 0f)
					{
						target.m_CoursePos.m_Entity = edgeData.m_Start;
						target.m_IsNode = true;
					}
					else if (intersectRange2.max >= 1f)
					{
						target.m_CoursePos.m_Entity = edgeData.m_End;
						target.m_IsNode = true;
					}
					if (startIntersectPos.m_Priority != -1f && (MathUtils.Intersect(startIntersectPos.m_CourseIntersection, target.m_CourseIntersection) || MathUtils.Intersect(startIntersectPos.m_EdgeIntersection, target.m_EdgeIntersection)) && Merge(ref target, startIntersectPos))
					{
						startIntersectPos.m_Priority = -1f;
					}
					if (endIntersectPos.m_Priority != -1f && (MathUtils.Intersect(endIntersectPos.m_CourseIntersection, target.m_CourseIntersection) || MathUtils.Intersect(endIntersectPos.m_EdgeIntersection, target.m_EdgeIntersection)) && Merge(ref target, endIntersectPos))
					{
						endIntersectPos.m_Priority = -1f;
					}
					if (lastIntersectPos.m_Priority != -1f)
					{
						if (!MathUtils.Intersect(lastIntersectPos.m_CourseIntersection, target.m_CourseIntersection) && !MathUtils.Intersect(lastIntersectPos.m_EdgeIntersection, target.m_EdgeIntersection))
						{
							Add(ref currentIntersectPos, lastIntersectPos);
							lastIntersectPos = target;
						}
						else if (!Merge(ref lastIntersectPos, target))
						{
							Add(ref currentIntersectPos, lastIntersectPos);
							lastIntersectPos = target;
						}
					}
					else
					{
						lastIntersectPos = target;
					}
				}
				start = num;
				float4 = xz;
				float5 = xz2;
			}
		}

		private bool Merge(ref IntersectPos target, IntersectPos other)
		{
			if (target.m_IsNode && other.m_IsNode && target.m_CoursePos.m_Entity != other.m_CoursePos.m_Entity)
			{
				return false;
			}
			if (target.m_IsNode && !other.m_IsNode)
			{
				other.m_CoursePos.m_Entity = target.m_CoursePos.m_Entity;
				other.m_IsNode = true;
			}
			else if (!target.m_IsNode && other.m_IsNode)
			{
				target.m_CoursePos.m_Entity = other.m_CoursePos.m_Entity;
				target.m_IsNode = true;
			}
			if (other.m_CourseIntersection.min < target.m_CourseIntersection.min)
			{
				target.m_CourseIntersection.min = other.m_CourseIntersection.min;
				target.m_IntersectionHeightMin = other.m_IntersectionHeightMin;
				target.m_EdgeHeightRangeMin = other.m_EdgeHeightRangeMin;
			}
			else if (other.m_CourseIntersection.min == target.m_CourseIntersection.min)
			{
				target.m_IntersectionHeightMin |= other.m_IntersectionHeightMin;
				target.m_EdgeHeightRangeMin |= other.m_EdgeHeightRangeMin;
			}
			if (other.m_CourseIntersection.max > target.m_CourseIntersection.max)
			{
				target.m_CourseIntersection.max = other.m_CourseIntersection.max;
				target.m_IntersectionHeightMax = other.m_IntersectionHeightMax;
				target.m_EdgeHeightRangeMax = other.m_EdgeHeightRangeMax;
			}
			else if (other.m_CourseIntersection.max == target.m_CourseIntersection.max)
			{
				target.m_IntersectionHeightMax |= other.m_IntersectionHeightMax;
				target.m_EdgeHeightRangeMax |= other.m_EdgeHeightRangeMax;
			}
			target.m_EdgeIntersection |= other.m_EdgeIntersection;
			target.m_IsTunnel &= other.m_IsTunnel;
			target.m_IsWaterway |= other.m_IsWaterway;
			if (other.m_Priority < target.m_Priority)
			{
				target.m_CoursePos = other.m_CoursePos;
				target.m_Priority = other.m_Priority;
			}
			return true;
		}

		private void Add(ref IntersectPos current, IntersectPos other)
		{
			if (current.m_Priority != -1f)
			{
				if (!MathUtils.Intersect(current.m_CourseIntersection, other.m_CourseIntersection) && !MathUtils.Intersect(current.m_EdgeIntersection, other.m_EdgeIntersection))
				{
					m_Results.Enqueue(current);
					current = other;
				}
				else if (!Merge(ref current, other))
				{
					m_Results.Enqueue(current);
					current = other;
				}
			}
			else
			{
				current = other;
			}
		}

		private bool QuadIntersect(float2 startLeft1, float2 startRight1, float2 endLeft1, float2 endRight1, float2 startLeft2, float2 startRight2, float2 endLeft2, float2 endRight2, out Bounds1 intersectRange1, out Bounds1 intersectRange2)
		{
			intersectRange1.min = 1f;
			intersectRange1.max = 0f;
			intersectRange2.min = 1f;
			intersectRange2.max = 0f;
			Bounds2 bounds = default(Bounds2);
			bounds.min = math.min(math.min(startLeft1, startRight1), math.min(endLeft1, endRight1));
			bounds.max = math.max(math.max(startLeft1, startRight1), math.max(endLeft1, endRight1));
			Bounds2 bounds2 = default(Bounds2);
			bounds2.min = math.min(math.min(startLeft2, startRight2), math.min(endLeft2, endRight2));
			bounds2.max = math.max(math.max(startLeft2, startRight2), math.max(endLeft2, endRight2));
			if (!MathUtils.Intersect(bounds, bounds2))
			{
				return false;
			}
			Triangle2 triangle = new Triangle2(startLeft1, endLeft1, endRight1);
			Triangle2 triangle2 = new Triangle2(endRight1, startRight1, startLeft1);
			Triangle2 triangle3 = new Triangle2(startLeft2, endLeft2, endRight2);
			Triangle2 triangle4 = new Triangle2(endRight2, startRight2, startLeft2);
			Line2.Segment line = new Line2.Segment(startLeft1, startRight1);
			Line2.Segment line2 = new Line2.Segment(endLeft1, endRight1);
			Line2.Segment line3 = new Line2.Segment(startLeft1, endLeft1);
			Line2.Segment line4 = new Line2.Segment(startRight1, endRight1);
			Line2.Segment line5 = new Line2.Segment(startLeft2, startRight2);
			Line2.Segment line6 = new Line2.Segment(endLeft2, endRight2);
			Line2.Segment line7 = new Line2.Segment(startLeft2, endLeft2);
			Line2.Segment line8 = new Line2.Segment(startRight2, endRight2);
			if (MathUtils.Intersect(triangle, startLeft2, out var t))
			{
				intersectRange1 |= t.x + t.y;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(triangle, startRight2, out t))
			{
				intersectRange1 |= t.x + t.y;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(triangle, endLeft2, out t))
			{
				intersectRange1 |= t.x + t.y;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(triangle, endRight2, out t))
			{
				intersectRange1 |= t.x + t.y;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(triangle2, startLeft2, out t))
			{
				intersectRange1 |= 1f - t.x - t.y;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(triangle2, startRight2, out t))
			{
				intersectRange1 |= 1f - t.x - t.y;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(triangle2, endLeft2, out t))
			{
				intersectRange1 |= 1f - t.x - t.y;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(triangle2, endRight2, out t))
			{
				intersectRange1 |= 1f - t.x - t.y;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(triangle3, startLeft1, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= t.x + t.y;
			}
			if (MathUtils.Intersect(triangle3, startRight1, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= t.x + t.y;
			}
			if (MathUtils.Intersect(triangle3, endLeft1, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= t.x + t.y;
			}
			if (MathUtils.Intersect(triangle3, endRight1, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= t.x + t.y;
			}
			if (MathUtils.Intersect(triangle4, startLeft1, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= 1f - t.x - t.y;
			}
			if (MathUtils.Intersect(triangle4, startRight1, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= 1f - t.x - t.y;
			}
			if (MathUtils.Intersect(triangle4, endLeft1, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= 1f - t.x - t.y;
			}
			if (MathUtils.Intersect(triangle4, endRight1, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= 1f - t.x - t.y;
			}
			if (MathUtils.Intersect(line, line5, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(line, line6, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(line, line7, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line, line8, out t))
			{
				intersectRange1 |= 0f;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line2, line5, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(line2, line6, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(line2, line7, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line2, line8, out t))
			{
				intersectRange1 |= 1f;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line3, line5, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(line3, line6, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(line3, line7, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line3, line8, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line4, line5, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(line4, line6, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(line4, line7, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= t.y;
			}
			if (MathUtils.Intersect(line4, line8, out t))
			{
				intersectRange1 |= t.x;
				intersectRange2 |= t.y;
			}
			return intersectRange1.min <= intersectRange1.max;
		}

		private bool CircleIntersect(Circle2 circle1, float2 startLeft2, float2 startRight2, float2 endLeft2, float2 endRight2, out Bounds1 intersectRange2)
		{
			intersectRange2.min = 1f;
			intersectRange2.max = 0f;
			Bounds2 bounds = MathUtils.Bounds(circle1);
			Bounds2 bounds2 = default(Bounds2);
			bounds2.min = math.min(math.min(startLeft2, startRight2), math.min(endLeft2, endRight2));
			bounds2.max = math.max(math.max(startLeft2, startRight2), math.max(endLeft2, endRight2));
			if (!MathUtils.Intersect(bounds, bounds2))
			{
				return false;
			}
			Triangle2 triangle = new Triangle2(startLeft2, endLeft2, endRight2);
			Triangle2 triangle2 = new Triangle2(endRight2, startRight2, startLeft2);
			Line2.Segment line = new Line2.Segment(startLeft2, startRight2);
			Line2.Segment line2 = new Line2.Segment(endLeft2, endRight2);
			Line2.Segment line3 = new Line2.Segment(startLeft2, endLeft2);
			Line2.Segment line4 = new Line2.Segment(startRight2, endRight2);
			if (MathUtils.Intersect(triangle, circle1.position, out var t))
			{
				float2 @float = new float2(math.distance(triangle.a, triangle.b), math.distance(triangle.a, triangle.c));
				float2 float2 = circle1.radius * t / (@float * (t.x + t.y));
				float2 float3 = math.max(0f, t - float2);
				float2 float4 = math.min(1f, t + float2);
				intersectRange2 |= float3.x + float4.y;
				intersectRange2 |= float4.x + float4.y;
			}
			if (MathUtils.Intersect(triangle2, circle1.position, out t))
			{
				float2 float5 = new float2(math.distance(triangle2.a, triangle2.b), math.distance(triangle2.a, triangle2.c));
				float2 float6 = circle1.radius * t / (float5 * (t.x + t.y));
				float2 float7 = math.max(0f, t - float6);
				float2 float8 = math.min(1f, t + float6);
				intersectRange2 |= 1f - float7.x - float8.y;
				intersectRange2 |= 1f - float8.x - float8.y;
			}
			if (MathUtils.Intersect(circle1, line, out t))
			{
				intersectRange2 |= 0f;
			}
			if (MathUtils.Intersect(circle1, line2, out t))
			{
				intersectRange2 |= 1f;
			}
			if (MathUtils.Intersect(circle1, line3, out t))
			{
				intersectRange2 |= new Bounds1(t.x, t.y);
			}
			if (MathUtils.Intersect(circle1, line4, out t))
			{
				intersectRange2 |= new Bounds1(t.x, t.y);
			}
			return intersectRange2.min <= intersectRange2.max;
		}

		private Bounds1 GetHeightRange(Bezier4x3 curve, float curvePos, Bounds1 heightRange)
		{
			return MathUtils.Position(curve, curvePos).y + heightRange;
		}

		private Bounds1 GetHeightRange(Segment segment, float curvePos, Bounds1 heightRange)
		{
			return GetHeightRange(segment.m_Left, curvePos, heightRange) | GetHeightRange(segment.m_Right, curvePos, heightRange);
		}

		private void CheckNodeSegment(NetCourse courseData, int courseIndex, int auxIndex, ref IntersectPos currentIntersectPos, NetGeometryData prefabGeometryData, NetCompositionData prefabCompositionData2, Entity edge, Entity node, Segment segment, float2 courseOffset, float edgeOffset, float centerOffset, bool isWaterway)
		{
			float2 @float = new float2(prefabGeometryData.m_DefaultWidth * 0.5f, prefabGeometryData.m_DefaultWidth * -0.5f);
			float3 float2 = MathUtils.Position(courseData.m_Curve, courseOffset.x);
			float3 float3 = MathUtils.Position(courseData.m_Curve, courseOffset.y);
			float3 value = MathUtils.Tangent(courseData.m_Curve, courseOffset.x);
			float3 value2 = MathUtils.Tangent(courseData.m_Curve, courseOffset.y);
			MathUtils.TryNormalize(ref value);
			MathUtils.TryNormalize(ref value2);
			float2 startLeft = float2.xz - value.zx * @float;
			float2 startRight = float2.xz + value.zx * @float;
			float2 endLeft = float3.xz - value2.zx * @float;
			float2 endRight = float3.xz + value2.zx * @float;
			float start = 0f;
			float2 float4 = segment.m_Left.a.xz;
			float2 float5 = segment.m_Right.a.xz;
			IntersectPos target = new IntersectPos
			{
				m_Priority = -1f
			};
			Bounds1 bounds = default(Bounds1);
			for (int i = 1; i <= 8; i++)
			{
				float num = (float)i / 8f;
				float2 xz = MathUtils.Position(segment.m_Left, num).xz;
				float2 xz2 = MathUtils.Position(segment.m_Right, num).xz;
				Bounds1 intersectRange;
				Bounds1 intersectRange2;
				bool flag = QuadIntersect(startLeft, startRight, endLeft, endRight, float4, float5, xz, xz2, out intersectRange, out intersectRange2);
				if (flag)
				{
					if ((prefabCompositionData2.m_Flags.m_General & (CompositionFlags.General.DeadEnd | CompositionFlags.General.Roundabout)) == CompositionFlags.General.DeadEnd)
					{
						MathUtils.Distance(courseData.m_Curve.xz, segment.m_Left.a.xz, out var t);
						MathUtils.Distance(courseData.m_Curve.xz, segment.m_Right.a.xz, out var t2);
						intersectRange = MathUtils.Bounds(t, t2);
						intersectRange.min = math.select(intersectRange.min, 0f, courseOffset.x == 0f && intersectRange.min <= 0.01f);
						intersectRange.max = math.select(intersectRange.max, 1f, courseOffset.y == 1f && intersectRange.max >= 0.99f);
					}
					else
					{
						intersectRange.min = math.lerp(courseOffset.x, courseOffset.y, intersectRange.min - 0.01f);
						intersectRange.max = math.lerp(courseOffset.x, courseOffset.y, intersectRange.max + 0.01f);
					}
				}
				if (courseOffset.x == 0f)
				{
					Line2.Segment line = new Line2.Segment(math.lerp(float4, float5, centerOffset), math.lerp(xz, xz2, centerOffset));
					if (MathUtils.Distance(line, float2.xz, out var t3) <= MathUtils.Distance(line, float3.xz, out t3) && CircleIntersect(new Circle2(@float.x, float2.xz), float4, float5, xz, xz2, out var intersectRange3))
					{
						intersectRange |= 0f;
						intersectRange2 |= intersectRange3;
						flag = true;
					}
				}
				if (courseOffset.y == 1f)
				{
					Line2.Segment line2 = new Line2.Segment(math.lerp(float4, float5, centerOffset), math.lerp(xz, xz2, centerOffset));
					if (MathUtils.Distance(line2, float2.xz, out var t4) >= MathUtils.Distance(line2, float3.xz, out t4) && CircleIntersect(new Circle2(@float.x, float3.xz), float4, float5, xz, xz2, out var intersectRange4))
					{
						intersectRange |= 1f;
						intersectRange2 |= intersectRange4;
						flag = true;
					}
				}
				if (flag)
				{
					Line2.Segment line3 = new Line2.Segment(float2.xz, float3.xz);
					Line2.Segment line4 = new Line2.Segment(math.lerp(float4, float5, centerOffset), math.lerp(xz, xz2, centerOffset));
					float2 t5;
					float priority = MathUtils.Distance(line3, line4, out t5);
					float num2 = math.lerp(courseOffset.x, courseOffset.y, t5.x);
					float3 position = MathUtils.Position(courseData.m_Curve, num2);
					bounds.min = math.lerp(start, num, intersectRange2.min - 0.01f);
					bounds.max = math.lerp(start, num, intersectRange2.max + 0.01f);
					intersectRange2 = new Bounds1(edgeOffset, edgeOffset);
					int parentMesh = -1;
					if (m_LocalTransformCacheData.TryGetComponent(node, out var componentData))
					{
						parentMesh = componentData.m_ParentMesh;
					}
					Bezier4x3 curve = MathUtils.Lerp(segment.m_Left, segment.m_Right, 0.5f);
					MathUtils.Distance(curve.xz, MathUtils.Position(courseData.m_Curve.xz, intersectRange.min), out var t6);
					MathUtils.Distance(curve.xz, MathUtils.Position(courseData.m_Curve.xz, intersectRange.max), out var t7);
					if (t7 < t6)
					{
						CommonUtils.Swap(ref bounds.min, ref bounds.max);
					}
					IntersectPos intersectPos = new IntersectPos
					{
						m_CoursePos = 
						{
							m_Entity = node,
							m_Position = position,
							m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, num2)),
							m_CourseDelta = num2,
							m_SplitPosition = edgeOffset,
							m_Flags = (courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.IsGrid))
						}
					};
					intersectPos.m_CoursePos.m_Flags |= CoursePosFlags.FreeHeight;
					intersectPos.m_CoursePos.m_ParentMesh = parentMesh;
					intersectPos.m_CourseIntersection = intersectRange;
					intersectPos.m_IntersectionHeightMin = new Bounds1(MathUtils.Position(curve, t6).y);
					intersectPos.m_IntersectionHeightMax = new Bounds1(MathUtils.Position(curve, t7).y);
					intersectPos.m_EdgeIntersection = intersectRange2;
					intersectPos.m_EdgeHeightRangeMin = GetHeightRange(segment, bounds.min, prefabCompositionData2.m_HeightRange);
					intersectPos.m_EdgeHeightRangeMax = GetHeightRange(segment, bounds.max, prefabCompositionData2.m_HeightRange);
					intersectPos.m_Priority = priority;
					intersectPos.m_AuxIndex = auxIndex;
					intersectPos.m_CourseIndex = courseIndex;
					intersectPos.m_IsNode = true;
					intersectPos.m_IsTunnel = (prefabCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 && ((prefabCompositionData2.m_Flags.m_Left | prefabCompositionData2.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0;
					intersectPos.m_IsWaterway = isWaterway;
					if (target.m_Priority != -1f)
					{
						if (!MathUtils.Intersect(target.m_CourseIntersection, intersectPos.m_CourseIntersection) && !MathUtils.Intersect(target.m_EdgeIntersection, intersectPos.m_EdgeIntersection))
						{
							Add(ref currentIntersectPos, target);
							target = intersectPos;
						}
						else if (!Merge(ref target, intersectPos))
						{
							Add(ref currentIntersectPos, target);
							target = intersectPos;
						}
					}
					else
					{
						target = intersectPos;
					}
				}
				start = num;
				float4 = xz;
				float5 = xz2;
			}
			if (target.m_Priority != -1f)
			{
				Add(ref currentIntersectPos, target);
			}
		}
	}

	private struct CourseHeightItem
	{
		public float m_TerrainHeight;

		public float m_TerrainBuildHeight;

		public float m_WaterHeight;

		public float m_CourseHeight;

		public float m_DistanceOffset;

		public Bounds1 m_LimitRange;

		public float2 m_LimitDistance;

		public bool m_ForceElevated;
	}

	private struct CourseHeightData
	{
		private NativeArray<CourseHeightItem> m_Buffer;

		private float2 m_SampleRange;

		private float m_SampleFactor;

		public CourseHeightData(Allocator allocator, NetCourse course, NetGeometryData netGeometryData, bool sampleTerrain, ref TerrainHeightData terrainHeightData, ref WaterSurfaceData<SurfaceWater> waterSurfaceData)
		{
			m_SampleRange = new float2(course.m_StartPosition.m_CourseDelta, course.m_EndPosition.m_CourseDelta);
			Bezier4x3 curve = MathUtils.Cut(course.m_Curve, m_SampleRange);
			float num = MathUtils.Length(curve.xz);
			int num2 = 1 + Mathf.CeilToInt(num / 4f);
			if (m_SampleRange.y > m_SampleRange.x)
			{
				m_SampleFactor = (float)(num2 - 1) / (m_SampleRange.y - m_SampleRange.x);
			}
			else
			{
				m_SampleFactor = 0f;
			}
			m_Buffer = new NativeArray<CourseHeightItem>(num2, allocator);
			float3 @float = course.m_StartPosition.m_Position;
			float num3 = 1f / (float)math.max(1, num2 - 1);
			float num4 = 0f;
			float num5 = math.max(course.m_StartPosition.m_Elevation.x, course.m_EndPosition.m_Elevation.x);
			for (int i = 0; i < num2; i++)
			{
				float3 float2;
				float num6;
				if (i == 0)
				{
					float2 = course.m_StartPosition.m_Position;
					num6 = course.m_StartPosition.m_Elevation.x;
				}
				else if (i == num2 - 1)
				{
					float2 = course.m_EndPosition.m_Position;
					num6 = course.m_EndPosition.m_Elevation.x;
				}
				else
				{
					float2 = MathUtils.Position(curve, (float)i * num3);
					num6 = math.lerp(course.m_StartPosition.m_Elevation.x, course.m_EndPosition.m_Elevation.x, (float)i * num3);
				}
				CourseHeightItem value = default(CourseHeightItem);
				if (sampleTerrain)
				{
					WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, float2, out value.m_TerrainHeight, out value.m_WaterHeight, out var waterDepth);
					value.m_TerrainBuildHeight = math.select(value.m_TerrainHeight, value.m_TerrainHeight + num5, num5 < -1f);
					if (waterDepth < 0.2f)
					{
						value.m_WaterHeight = value.m_TerrainHeight;
					}
					else
					{
						value.m_WaterHeight += netGeometryData.m_ElevationLimit * 2f;
					}
				}
				else
				{
					value.m_TerrainHeight = float2.y - num6;
					value.m_TerrainBuildHeight = math.select(value.m_TerrainHeight, value.m_TerrainHeight + num5, num5 < -1f);
					value.m_WaterHeight = -1000000f;
					value.m_CourseHeight = float2.y;
				}
				value.m_DistanceOffset = math.distance(@float.xz, float2.xz);
				value.m_LimitRange = new Bounds1(-1000000f, 1000000f);
				value.m_LimitDistance = 1000000f;
				m_Buffer[i] = value;
				@float = float2;
				num4 += value.m_DistanceOffset;
			}
			if (!sampleTerrain)
			{
				return;
			}
			InitializeCoursePos(ref course.m_StartPosition);
			InitializeCoursePos(ref course.m_EndPosition);
			Bounds1 bounds = new Bounds1(-1000000f, 1000000f);
			Bounds1 bounds2 = new Bounds1(-1000000f, 1000000f);
			if (course.m_StartPosition.m_Elevation.x >= netGeometryData.m_ElevationLimit || course.m_EndPosition.m_Elevation.x >= netGeometryData.m_ElevationLimit || (netGeometryData.m_Flags & Game.Net.GeometryFlags.RequireElevated) != 0)
			{
				bounds.min = course.m_StartPosition.m_Position.y;
				bounds2.min = course.m_EndPosition.m_Position.y;
			}
			else
			{
				if (course.m_StartPosition.m_Elevation.x > 1f)
				{
					bounds.min = course.m_StartPosition.m_Position.y;
					bounds2.min = math.max(bounds2.min, course.m_EndPosition.m_Position.y - num4 * netGeometryData.m_MaxSlopeSteepness * 0.5f);
				}
				if (course.m_EndPosition.m_Elevation.x > 1f)
				{
					bounds.min = math.max(bounds.min, course.m_StartPosition.m_Position.y - num4 * netGeometryData.m_MaxSlopeSteepness * 0.5f);
					bounds2.min = course.m_EndPosition.m_Position.y;
				}
			}
			if (course.m_StartPosition.m_Elevation.x <= 0f - netGeometryData.m_ElevationLimit || course.m_EndPosition.m_Elevation.x <= 0f - netGeometryData.m_ElevationLimit)
			{
				bounds.max = course.m_StartPosition.m_Position.y;
				bounds2.max = course.m_EndPosition.m_Position.y;
			}
			else
			{
				if (course.m_StartPosition.m_Elevation.x < -1f)
				{
					bounds.max = course.m_StartPosition.m_Position.y;
					bounds2.max = math.min(bounds2.max, course.m_EndPosition.m_Position.y + num4 * netGeometryData.m_MaxSlopeSteepness * 0.5f);
				}
				if (course.m_EndPosition.m_Elevation.x < -1f)
				{
					bounds.max = math.min(bounds.max, course.m_StartPosition.m_Position.y + num4 * netGeometryData.m_MaxSlopeSteepness * 0.5f);
					bounds2.max = course.m_EndPosition.m_Position.y;
				}
			}
			float num7 = -1000000f;
			float num8 = 0f;
			for (int j = 0; j < num2; j++)
			{
				CourseHeightItem value2 = m_Buffer[j];
				Bounds1 bounds3;
				if (j == 0)
				{
					bounds3 = bounds;
				}
				else if (j == num2 - 1)
				{
					bounds3 = bounds2;
				}
				else
				{
					num8 += value2.m_DistanceOffset;
					bounds3 = MathUtils.Lerp(bounds, bounds2, num8 / num4);
				}
				num7 -= value2.m_DistanceOffset * netGeometryData.m_MaxSlopeSteepness;
				value2.m_CourseHeight = math.select(math.max(value2.m_TerrainHeight, value2.m_WaterHeight), value2.m_TerrainBuildHeight, num5 < -1f);
				value2.m_CourseHeight = MathUtils.Clamp(value2.m_CourseHeight, bounds3);
				value2.m_CourseHeight = math.max(value2.m_CourseHeight, num7);
				num7 = value2.m_CourseHeight;
				m_Buffer[j] = value2;
			}
			num7 = -1000000f;
			for (int num9 = num2 - 1; num9 >= 0; num9--)
			{
				CourseHeightItem value3 = m_Buffer[num9];
				value3.m_CourseHeight = math.max(value3.m_CourseHeight, num7);
				num7 = value3.m_CourseHeight - value3.m_DistanceOffset * netGeometryData.m_MaxSlopeSteepness;
				m_Buffer[num9] = value3;
			}
		}

		public void InitializeCoursePos(ref CoursePos coursePos)
		{
			if ((coursePos.m_Flags & CoursePosFlags.FreeHeight) != 0)
			{
				float num;
				if (coursePos.m_CourseDelta == m_SampleRange.x)
				{
					CourseHeightItem courseHeightItem = m_Buffer[0];
					num = math.select(math.max(courseHeightItem.m_TerrainHeight, courseHeightItem.m_WaterHeight), courseHeightItem.m_TerrainHeight, coursePos.m_Elevation.x < -1f);
				}
				else if (coursePos.m_CourseDelta == m_SampleRange.y)
				{
					CourseHeightItem courseHeightItem2 = m_Buffer[m_Buffer.Length - 1];
					num = math.select(math.max(courseHeightItem2.m_TerrainHeight, courseHeightItem2.m_WaterHeight), courseHeightItem2.m_TerrainHeight, coursePos.m_Elevation.x < -1f);
				}
				else
				{
					float num2 = (coursePos.m_CourseDelta - m_SampleRange.x) * m_SampleFactor;
					int num3 = math.clamp(Mathf.FloorToInt(num2), 0, m_Buffer.Length - 1);
					CourseHeightItem courseHeightItem3 = m_Buffer[num3];
					CourseHeightItem courseHeightItem4 = m_Buffer[math.min(num3 + 1, m_Buffer.Length - 1)];
					float t = math.saturate(num2 - (float)num3);
					float num4 = math.lerp(courseHeightItem3.m_TerrainHeight, courseHeightItem4.m_TerrainHeight, t);
					float y = math.lerp(courseHeightItem3.m_WaterHeight, courseHeightItem4.m_WaterHeight, t);
					num = math.select(math.max(num4, y), num4, coursePos.m_Elevation.x < -1f);
				}
				coursePos.m_Position.y = num + coursePos.m_Elevation.x;
			}
		}

		public void ApplyLimitRange(IntersectPos intersectPos, NetGeometryData netGeometryData, Bounds1 limitRangeMin, Bounds1 limitRangeMax, bool shrink = false, bool forceElevated = false)
		{
			float2 @float = (new float2(intersectPos.m_CourseIntersection.min, intersectPos.m_CourseIntersection.max) - m_SampleRange.x) * m_SampleFactor;
			int x;
			int x2;
			if (shrink)
			{
				x = math.max(Mathf.RoundToInt(@float.x), 0);
				x2 = math.min(Mathf.RoundToInt(@float.y), m_Buffer.Length - 1);
			}
			else
			{
				x = math.max(Mathf.FloorToInt(@float.x), 0);
				x2 = math.min(Mathf.CeilToInt(@float.y), m_Buffer.Length - 1);
			}
			x = math.min(x, m_Buffer.Length);
			x2 = math.max(x2, -1);
			float num = 1f / (float)math.max(1, x2 - x);
			if (forceElevated)
			{
				for (int i = x; i <= x2; i++)
				{
					CourseHeightItem item = m_Buffer[i];
					Bounds1 limitRange = MathUtils.Lerp(limitRangeMin, limitRangeMax, (float)(i - x) * num);
					if (AddLimit(ref item, limitRange, 0f) || !item.m_ForceElevated)
					{
						item.m_CourseHeight = MathUtils.Clamp(item.m_CourseHeight, item.m_LimitRange);
						item.m_ForceElevated |= forceElevated;
						m_Buffer[i] = item;
					}
				}
			}
			else
			{
				for (int j = x; j <= x2; j++)
				{
					CourseHeightItem item2 = m_Buffer[j];
					Bounds1 limitRange2 = MathUtils.Lerp(limitRangeMin, limitRangeMax, (float)(j - x) * num);
					if (AddLimit(ref item2, limitRange2, 0f))
					{
						item2.m_CourseHeight = MathUtils.Clamp(item2.m_CourseHeight, item2.m_LimitRange);
						m_Buffer[j] = item2;
					}
				}
			}
			if (x > 0)
			{
				float num2 = 0f;
				if (x < m_Buffer.Length)
				{
					num2 += m_Buffer[x].m_DistanceOffset;
				}
				for (int num3 = x - 1; num3 >= 0; num3--)
				{
					CourseHeightItem item3 = m_Buffer[num3];
					Bounds1 limitRange3 = MathUtils.Expand(limitRangeMin, num2 * netGeometryData.m_MaxSlopeSteepness);
					if (!AddLimit(ref item3, limitRange3, num2))
					{
						break;
					}
					item3.m_CourseHeight = MathUtils.Clamp(item3.m_CourseHeight, item3.m_LimitRange);
					num2 += item3.m_DistanceOffset;
					m_Buffer[num3] = item3;
				}
			}
			if (x2 >= m_Buffer.Length - 1)
			{
				return;
			}
			float num4 = 0f;
			for (int k = x2 + 1; k < m_Buffer.Length; k++)
			{
				CourseHeightItem item4 = m_Buffer[k];
				num4 += item4.m_DistanceOffset;
				Bounds1 limitRange4 = MathUtils.Expand(limitRangeMax, num4 * netGeometryData.m_MaxSlopeSteepness);
				if (AddLimit(ref item4, limitRange4, num4))
				{
					item4.m_CourseHeight = MathUtils.Clamp(item4.m_CourseHeight, item4.m_LimitRange);
					m_Buffer[k] = item4;
					continue;
				}
				break;
			}
		}

		private bool AddLimit(ref CourseHeightItem item, Bounds1 limitRange, float distance)
		{
			if (limitRange.min > item.m_LimitRange.min)
			{
				item.m_LimitDistance.x = distance;
				if (limitRange.min > item.m_LimitRange.max)
				{
					float t = math.select(distance / math.csum(item.m_LimitDistance), 0.5f, math.all(item.m_LimitDistance == 0f));
					limitRange = new Bounds1(math.lerp(limitRange.min, item.m_LimitRange.max, t));
				}
			}
			else
			{
				limitRange.min = item.m_LimitRange.min;
			}
			if (limitRange.max < item.m_LimitRange.max)
			{
				item.m_LimitDistance.y = distance;
				if (limitRange.max < item.m_LimitRange.min)
				{
					float t2 = math.select(distance / math.csum(item.m_LimitDistance), 0.5f, math.all(item.m_LimitDistance == 0f));
					limitRange = new Bounds1(math.lerp(limitRange.max, item.m_LimitRange.min, t2));
				}
			}
			else
			{
				limitRange.max = item.m_LimitRange.max;
			}
			bool result = !limitRange.Equals(item.m_LimitRange);
			item.m_LimitRange = limitRange;
			return result;
		}

		public void StraightenElevation(IntersectPos firstPos, IntersectPos lastPos)
		{
			float2 @float = (new float2(firstPos.m_CourseIntersection.max, lastPos.m_CourseIntersection.min) - m_SampleRange.x) * m_SampleFactor;
			int num = math.max(Mathf.FloorToInt(@float.x), 0);
			int num2 = math.min(Mathf.CeilToInt(@float.y), m_Buffer.Length - 1);
			int num3 = num + 1;
			while (num3 < num2)
			{
				CourseHeightItem courseHeightItem = m_Buffer[num3];
				if (courseHeightItem.m_CourseHeight != courseHeightItem.m_TerrainBuildHeight && courseHeightItem.m_LimitRange.min < courseHeightItem.m_LimitRange.max)
				{
					float num4 = courseHeightItem.m_DistanceOffset;
					int i = num3;
					CourseHeightItem courseHeightItem2 = m_Buffer[i];
					for (; i < num2; i++)
					{
						courseHeightItem2 = m_Buffer[i + 1];
						num4 += courseHeightItem2.m_DistanceOffset;
						if (courseHeightItem2.m_CourseHeight == courseHeightItem2.m_TerrainBuildHeight || courseHeightItem2.m_LimitRange.min >= courseHeightItem2.m_LimitRange.max)
						{
							break;
						}
					}
					CourseHeightItem courseHeightItem3 = m_Buffer[num3 - 1];
					float num5 = 0f;
					for (int j = num3; j <= i; j++)
					{
						CourseHeightItem value = m_Buffer[j];
						num5 += value.m_DistanceOffset;
						value.m_CourseHeight = math.lerp(courseHeightItem3.m_CourseHeight, courseHeightItem2.m_CourseHeight, num5 / num4);
						value.m_CourseHeight = MathUtils.Clamp(value.m_CourseHeight, value.m_LimitRange);
						m_Buffer[j] = value;
					}
					num3 = i + 1;
				}
				else
				{
					num3++;
				}
			}
		}

		public void SampleCourseHeight(ref NetCourse course, NetGeometryData netGeometryData)
		{
			if ((course.m_StartPosition.m_Flags & CoursePosFlags.FreeHeight) != 0)
			{
				course.m_StartPosition.m_Position.y = SampleHeight(course.m_StartPosition.m_CourseDelta, out var forceElevated);
				if (forceElevated)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
			}
			else
			{
				SampleHeight(course.m_StartPosition.m_CourseDelta, out var forceElevated2);
				if (forceElevated2)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
			}
			if ((course.m_EndPosition.m_Flags & CoursePosFlags.FreeHeight) != 0)
			{
				course.m_EndPosition.m_Position.y = SampleHeight(course.m_EndPosition.m_CourseDelta, out var forceElevated3);
				if (forceElevated3)
				{
					course.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
			}
			else
			{
				SampleHeight(course.m_EndPosition.m_CourseDelta, out var forceElevated4);
				if (forceElevated4)
				{
					course.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
			}
			if (course.m_StartPosition.m_Position.Equals(course.m_EndPosition.m_Position))
			{
				if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
				{
					course.m_Curve.a = course.m_StartPosition.m_Position;
				}
				else
				{
					course.m_Curve.a.y = course.m_StartPosition.m_Position.y;
				}
				course.m_Curve.b = course.m_Curve.a;
				course.m_Curve.c = course.m_Curve.a;
				course.m_Curve.d = course.m_Curve.a;
			}
			else if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StraightEdges) != 0)
			{
				float2 @float = new float2(course.m_StartPosition.m_CourseDelta, course.m_EndPosition.m_CourseDelta);
				float2 float2 = math.lerp(@float.x, @float.y, new float2(1f / 3f, 2f / 3f));
				SampleHeight(float2.x, out var forceElevated5);
				SampleHeight(float2.y, out var forceElevated6);
				if (forceElevated5 || forceElevated6)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
					course.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
				}
				if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
				{
					course.m_Curve = NetUtils.StraightCurve(course.m_StartPosition.m_Position, course.m_EndPosition.m_Position, netGeometryData.m_Hanging);
				}
				else
				{
					float3 startPos = MathUtils.Position(course.m_Curve, course.m_StartPosition.m_CourseDelta);
					float3 endPos = MathUtils.Position(course.m_Curve, course.m_EndPosition.m_CourseDelta);
					startPos.y = course.m_StartPosition.m_Position.y;
					endPos.y = course.m_EndPosition.m_Position.y;
					course.m_Curve = NetUtils.StraightCurve(startPos, endPos, netGeometryData.m_Hanging);
				}
			}
			else
			{
				float2 t = new float2(course.m_StartPosition.m_CourseDelta, course.m_EndPosition.m_CourseDelta);
				float2 float3 = math.lerp(t.x, t.y, new float2(1f / 3f, 2f / 3f));
				course.m_Curve = MathUtils.Cut(course.m_Curve, t);
				if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0)
				{
					course.m_Curve.a = course.m_StartPosition.m_Position;
					course.m_Curve.d = course.m_EndPosition.m_Position;
				}
				else
				{
					course.m_Curve.a.y = course.m_StartPosition.m_Position.y;
					course.m_Curve.d.y = course.m_EndPosition.m_Position.y;
				}
				course.m_Curve.b.y = SampleHeight(float3.x, out var forceElevated7);
				course.m_Curve.c.y = SampleHeight(float3.y, out var forceElevated8);
				if (forceElevated7 || forceElevated8)
				{
					course.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
					course.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
				}
				float num = course.m_Curve.b.y - MathUtils.Position(course.m_Curve, 1f / 3f).y;
				float num2 = course.m_Curve.c.y - MathUtils.Position(course.m_Curve, 2f / 3f).y;
				course.m_Curve.b.y += num * 3f - num2 * 1.5f;
				course.m_Curve.c.y += num2 * 3f - num * 1.5f;
			}
			course.m_StartPosition.m_CourseDelta = 0f;
			course.m_EndPosition.m_CourseDelta = 1f;
			course.m_Length = MathUtils.Length(course.m_Curve);
		}

		public float SampleHeight(float courseDelta, out bool forceElevated)
		{
			if (courseDelta == m_SampleRange.x)
			{
				CourseHeightItem courseHeightItem = m_Buffer[0];
				forceElevated = courseHeightItem.m_ForceElevated;
				return courseHeightItem.m_CourseHeight;
			}
			if (courseDelta == m_SampleRange.y)
			{
				CourseHeightItem courseHeightItem2 = m_Buffer[m_Buffer.Length - 1];
				forceElevated = courseHeightItem2.m_ForceElevated;
				return courseHeightItem2.m_CourseHeight;
			}
			float num = (courseDelta - m_SampleRange.x) * m_SampleFactor;
			int num2 = math.clamp(Mathf.FloorToInt(num), 0, m_Buffer.Length - 1);
			CourseHeightItem courseHeightItem3 = m_Buffer[num2];
			CourseHeightItem courseHeightItem4 = m_Buffer[math.min(num2 + 1, m_Buffer.Length - 1)];
			forceElevated = courseHeightItem3.m_ForceElevated | courseHeightItem4.m_ForceElevated;
			return math.lerp(courseHeightItem3.m_CourseHeight, courseHeightItem4.m_CourseHeight, math.saturate(num - (float)num2));
		}

		public void GetHeightRange(IntersectPos intersectPos, NetGeometryData netGeometryData, bool canUseElevatedHeight, out Bounds1 minBounds, out Bounds1 maxBounds, out bool elevated)
		{
			float2 @float = (new float2(intersectPos.m_CourseIntersection.min, intersectPos.m_CourseIntersection.max) - m_SampleRange.x) * m_SampleFactor;
			int num = math.max(Mathf.FloorToInt(@float.x), 0);
			int num2 = math.min(Mathf.CeilToInt(@float.y), m_Buffer.Length - 1);
			minBounds = new Bounds1(1000000f, -1000000f);
			maxBounds = new Bounds1(1000000f, -1000000f);
			elevated = false;
			if (num < m_Buffer.Length)
			{
				CourseHeightItem courseHeightItem = m_Buffer[num];
				CourseHeightItem courseHeightItem2 = m_Buffer[math.min(num + 1, m_Buffer.Length - 1)];
				minBounds = new Bounds1(math.lerp(courseHeightItem.m_CourseHeight, courseHeightItem2.m_CourseHeight, math.saturate(@float.x - (float)num)));
				elevated |= (courseHeightItem.m_CourseHeight > courseHeightItem.m_TerrainHeight) | (courseHeightItem2.m_CourseHeight > courseHeightItem2.m_TerrainHeight);
			}
			if (num2 >= 0)
			{
				CourseHeightItem courseHeightItem3 = m_Buffer[math.max(num2 - 1, 0)];
				CourseHeightItem courseHeightItem4 = m_Buffer[num2];
				maxBounds = new Bounds1(math.lerp(courseHeightItem3.m_CourseHeight, courseHeightItem4.m_CourseHeight, math.saturate(@float.y - (float)(num2 - 1))));
				elevated |= (courseHeightItem3.m_CourseHeight > courseHeightItem3.m_TerrainHeight) | (courseHeightItem4.m_CourseHeight > courseHeightItem4.m_TerrainHeight);
			}
			Bounds1 bounds = minBounds | maxBounds;
			for (int i = num + 1; i < num2; i++)
			{
				CourseHeightItem courseHeightItem5 = m_Buffer[i];
				if (courseHeightItem5.m_CourseHeight < bounds.min || courseHeightItem5.m_CourseHeight > bounds.max)
				{
					bounds |= courseHeightItem5.m_CourseHeight;
					float num3 = ((float)i - @float.x) / math.max(1f, @float.y - @float.x);
					float2 float2 = math.saturate(new float2(2f - 2f * num3, 2f * num3));
					float4 float3 = math.max(0f, new float4(minBounds.min - courseHeightItem5.m_CourseHeight, courseHeightItem5.m_CourseHeight - minBounds.max, maxBounds.min - courseHeightItem5.m_CourseHeight, courseHeightItem5.m_CourseHeight - maxBounds.max)) * float2.xxyy;
					minBounds.min -= float3.x;
					minBounds.max += float3.y;
					maxBounds.min -= float3.z;
					maxBounds.max += float3.w;
				}
				elevated |= courseHeightItem5.m_CourseHeight > courseHeightItem5.m_TerrainHeight;
			}
			float num4 = math.select(netGeometryData.m_DefaultHeightRange.min, netGeometryData.m_ElevatedHeightRange.min, canUseElevatedHeight & elevated);
			float num5 = math.select(netGeometryData.m_DefaultHeightRange.max, netGeometryData.m_ElevatedHeightRange.max, canUseElevatedHeight & elevated);
			minBounds.min += num4;
			minBounds.max += num5;
			maxBounds.min += num4;
			maxBounds.max += num5;
		}

		public void Dispose()
		{
			m_Buffer.Dispose();
		}
	}

	[BurstCompile]
	private struct CheckCourseIntersectionResultsJob : IJobParallelForDefer
	{
		private struct AuxIntersectionEntity
		{
			public Entity m_Entity;

			public float m_SplitPosition;
		}

		private struct ElevationSegment
		{
			public Bounds1 m_CourseRange;

			public Bounds1 m_DistanceOffset;

			public int2 m_ElevationType;

			public bool m_CanRemove;
		}

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<LocalCurveCache> m_LocalCurveCacheData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PrefabPlaceableData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_PrefabServiceUpgradeData;

		[ReadOnly]
		public BufferLookup<FixedNetElement> m_PrefabFixedNetElements;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> m_PrefabAuxiliaryNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeList<Course> m_CourseList;

		[ReadOnly]
		public NativeHashMap<Entity, bool> m_DeletedEntities;

		[ReadOnly]
		public NativeParallelQueue<IntersectPos>.Reader m_IntersectionQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Course course = m_CourseList[index];
			NetData prefabNetData = m_PrefabNetData[course.m_CreationDefinition.m_Prefab];
			NetGeometryData netGeometryData = m_PrefabGeometryData[course.m_CreationDefinition.m_Prefab];
			IntersectPos value = default(IntersectPos);
			value.m_CoursePos = course.m_CourseData.m_StartPosition;
			value.m_CourseIntersection = new Bounds1(course.m_CourseData.m_StartPosition.m_CourseDelta, course.m_CourseData.m_StartPosition.m_CourseDelta);
			value.m_IntersectionHeightMin = new Bounds1(float.MaxValue, float.MinValue);
			value.m_IntersectionHeightMax = new Bounds1(float.MaxValue, float.MinValue);
			value.m_EdgeIntersection = new Bounds1(course.m_CourseData.m_StartPosition.m_SplitPosition, course.m_CourseData.m_StartPosition.m_SplitPosition);
			value.m_EdgeHeightRangeMin = new Bounds1(1000000f, -1000000f);
			value.m_EdgeHeightRangeMax = new Bounds1(1000000f, -1000000f);
			value.m_Priority = -1f;
			value.m_AuxIndex = -1;
			value.m_IsNode = m_NodeData.HasComponent(value.m_CoursePos.m_Entity);
			value.m_IsStartEnd = true;
			IntersectPos value2 = default(IntersectPos);
			value2.m_CoursePos = course.m_CourseData.m_EndPosition;
			value2.m_CourseIntersection = new Bounds1(course.m_CourseData.m_EndPosition.m_CourseDelta, course.m_CourseData.m_EndPosition.m_CourseDelta);
			value2.m_IntersectionHeightMin = new Bounds1(float.MaxValue, float.MinValue);
			value2.m_IntersectionHeightMax = new Bounds1(float.MaxValue, float.MinValue);
			value2.m_EdgeIntersection = new Bounds1(course.m_CourseData.m_EndPosition.m_SplitPosition, course.m_CourseData.m_EndPosition.m_SplitPosition);
			value2.m_EdgeHeightRangeMin = new Bounds1(1000000f, -1000000f);
			value2.m_EdgeHeightRangeMax = new Bounds1(1000000f, -1000000f);
			value2.m_Priority = -1f;
			value2.m_AuxIndex = -1;
			value2.m_IsNode = m_NodeData.HasComponent(value2.m_CoursePos.m_Entity);
			value2.m_IsStartEnd = true;
			bool flag = !m_EditorMode && (course.m_CreationDefinition.m_Flags & CreationFlags.SubElevation) != 0 && m_PrefabServiceUpgradeData.HasComponent(course.m_CreationDefinition.m_Prefab);
			bool flag2 = (course.m_CreationDefinition.m_Owner == Entity.Null && course.m_OwnerDefinition.m_Prefab == Entity.Null) || flag;
			CourseHeightData courseHeightData = new CourseHeightData(Allocator.Temp, course.m_CourseData, netGeometryData, flag2, ref m_TerrainHeightData, ref m_WaterSurfaceData);
			if (flag2)
			{
				courseHeightData.InitializeCoursePos(ref value.m_CoursePos);
				courseHeightData.InitializeCoursePos(ref value2.m_CoursePos);
			}
			courseHeightData.ApplyLimitRange(value, netGeometryData, new Bounds1(value.m_CoursePos.m_Position.y), new Bounds1(value.m_CoursePos.m_Position.y));
			courseHeightData.ApplyLimitRange(value2, netGeometryData, new Bounds1(value2.m_CoursePos.m_Position.y), new Bounds1(value2.m_CoursePos.m_Position.y));
			courseHeightData.StraightenElevation(value, value2);
			bool flag3 = m_PrefabBuildingExtensionData.HasComponent(course.m_OwnerDefinition.m_Prefab);
			bool canBeSplitted = flag3 || (course.m_CreationDefinition.m_Owner == Entity.Null && course.m_OwnerDefinition.m_Prefab == Entity.Null) || (course.m_CreationDefinition.m_Flags & CreationFlags.SubElevation) != 0;
			NativeList<IntersectPos> nativeList = new NativeList<IntersectPos>(16, Allocator.Temp);
			NativeList<IntersectPos> nativeList2 = new NativeList<IntersectPos>(16, Allocator.Temp);
			NativeList<IntersectPos> nativeList3 = default(NativeList<IntersectPos>);
			NativeList<IntersectPos> nativeList4 = default(NativeList<IntersectPos>);
			NativeList<AuxIntersectionEntity> auxIntersectionEntities = default(NativeList<AuxIntersectionEntity>);
			int intersectionAuxIndex = 0;
			if (m_PrefabAuxiliaryNets.TryGetBuffer(course.m_CreationDefinition.m_Prefab, out var bufferData))
			{
				auxIntersectionEntities = new NativeList<AuxIntersectionEntity>(16, Allocator.Temp);
				nativeList3 = new NativeList<IntersectPos>(16, Allocator.Temp);
				nativeList4 = new NativeList<IntersectPos>(16, Allocator.Temp);
			}
			nativeList.Add(in value);
			NativeParallelQueue<IntersectPos>.Enumerator enumerator = m_IntersectionQueue.GetEnumerator(index % m_IntersectionQueue.HashRange);
			while (enumerator.MoveNext())
			{
				IntersectPos value3 = enumerator.Current;
				if (value3.m_CourseIndex == index)
				{
					if (value3.m_AuxIndex == -1)
					{
						nativeList.Add(in value3);
					}
					else
					{
						nativeList3.Add(in value3);
					}
				}
			}
			enumerator.Dispose();
			nativeList.Add(in value2);
			if (nativeList.Length >= 4)
			{
				nativeList.AsArray().GetSubArray(1, nativeList.Length - 2).Sort();
			}
			bool canAdjustHeight = flag2 && (prefabNetData.m_RequiredLayers & Layer.Waterway) == 0;
			bool flag4 = (prefabNetData.m_ConnectLayers & Layer.Waterway) != 0;
			if (!flag4 && bufferData.IsCreated)
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					AuxiliaryNet auxiliaryNet = bufferData[i];
					if ((m_PrefabNetData[auxiliaryNet.m_Prefab].m_RequiredLayers & Layer.Waterway) != Layer.None)
					{
						flag4 = true;
						break;
					}
				}
			}
			MergePositions(course.m_CourseData, course.m_CreationDefinition, course.m_OwnerDefinition, prefabNetData, netGeometryData, ref courseHeightData, nativeList, nativeList2, 0f, canAdjustHeight, flag4, canBeSplitted, flag3, bufferData.IsCreated && bufferData.Length != 0);
			if (bufferData.IsCreated)
			{
				value.m_CoursePos.m_Entity = Entity.Null;
				value.m_IsNode = false;
				value2.m_CoursePos.m_Entity = Entity.Null;
				value2.m_IsNode = false;
				for (int j = 0; j < bufferData.Length; j++)
				{
					AuxiliaryNet auxiliaryNet2 = bufferData[j];
					NetData prefabNetData2 = m_PrefabNetData[auxiliaryNet2.m_Prefab];
					NetGeometryData prefabGeometryData = m_PrefabGeometryData[auxiliaryNet2.m_Prefab];
					nativeList.Clear();
					nativeList4.Clear();
					value.m_CoursePos.m_Position.y = course.m_CourseData.m_StartPosition.m_Position.y + auxiliaryNet2.m_Position.y;
					value.m_AuxIndex = j;
					value2.m_CoursePos.m_Position.y = course.m_CourseData.m_EndPosition.m_Position.y + auxiliaryNet2.m_Position.y;
					value2.m_AuxIndex = j;
					nativeList.Add(in value);
					for (int k = 0; k < nativeList3.Length; k++)
					{
						IntersectPos value4 = nativeList3[k];
						if (value4.m_AuxIndex == j)
						{
							nativeList.Add(in value4);
						}
					}
					nativeList.Add(in value2);
					if (nativeList.Length >= 4)
					{
						nativeList.AsArray().GetSubArray(1, nativeList.Length - 2).Sort();
					}
					CreationDefinition creationDefinition = course.m_CreationDefinition;
					creationDefinition.m_Prefab = auxiliaryNet2.m_Prefab;
					canAdjustHeight = flag2 && (prefabNetData2.m_RequiredLayers & Layer.Waterway) == 0;
					MergePositions(course.m_CourseData, creationDefinition, course.m_OwnerDefinition, prefabNetData2, prefabGeometryData, ref courseHeightData, nativeList, nativeList4, auxiliaryNet2.m_Position.y, canAdjustHeight, flag4, canBeSplitted, flag3, hasSubNets: false);
					MergeAuxPositions(course.m_CourseData, nativeList4, nativeList2, bufferData, auxIntersectionEntities, ref intersectionAuxIndex);
				}
			}
			courseHeightData.StraightenElevation(value, value2);
			nativeList.Clear();
			if (m_PrefabFixedNetElements.HasBuffer(course.m_CreationDefinition.m_Prefab))
			{
				nativeList.Add(nativeList2[0]);
				if (nativeList2.Length >= 2)
				{
					nativeList.Add(nativeList2[nativeList2.Length - 1]);
				}
			}
			else
			{
				m_PrefabPlaceableData.TryGetComponent(course.m_CreationDefinition.m_Prefab, out var componentData);
				CheckHeightRange(course.m_CourseData, netGeometryData, componentData, ref courseHeightData, flag2, nativeList2, nativeList);
				if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
				{
					SnapCoursePositions(course.m_CourseData, nativeList);
				}
			}
			if (nativeList.Length >= 3)
			{
				float num = (((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0) ? 36f : 1f);
				if (nativeList[1].m_IsOptional && math.distancesq(nativeList[0].m_CoursePos.m_Position.xz, nativeList[1].m_CoursePos.m_Position.xz) < num)
				{
					nativeList.RemoveAt(1);
				}
			}
			if (nativeList.Length >= 3)
			{
				float num2 = (((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0) ? 36f : 1f);
				if (nativeList[nativeList.Length - 2].m_IsOptional && math.distancesq(nativeList[nativeList.Length - 1].m_CoursePos.m_Position.xz, nativeList[nativeList.Length - 2].m_CoursePos.m_Position.xz) < num2)
				{
					nativeList.RemoveAt(nativeList.Length - 2);
				}
			}
			UpdateCourses(course.m_CourseData, course.m_CreationDefinition, course.m_OwnerDefinition, course.m_UpgradedData, course.m_CourseEntity, index, nativeList, auxIntersectionEntities, ref courseHeightData);
			if (auxIntersectionEntities.IsCreated)
			{
				auxIntersectionEntities.Dispose();
			}
			nativeList.Dispose();
			nativeList2.Dispose();
			courseHeightData.Dispose();
		}

		private void SnapCoursePositions(NetCourse courseData, NativeList<IntersectPos> intersectionList)
		{
			for (int i = 1; i < intersectionList.Length - 1; i++)
			{
				IntersectPos intersectPos = intersectionList[i - 1];
				IntersectPos value = intersectionList[i];
				IntersectPos intersectPos2 = intersectionList[i + 1];
				float value2 = MathUtils.Length(courseData.m_Curve.xz, new Bounds1(intersectPos.m_CoursePos.m_CourseDelta, value.m_CoursePos.m_CourseDelta));
				value2 = MathUtils.Snap(value2, 4f);
				Bounds1 t = new Bounds1(intersectPos.m_CoursePos.m_CourseDelta, intersectPos2.m_CoursePos.m_CourseDelta);
				MathUtils.ClampLength(courseData.m_Curve.xz, ref t, value2);
				value.m_CoursePos.m_CourseDelta = t.max;
				if (((int)math.round(value2 / 4f) & 1) != 0 != ((intersectPos.m_CoursePos.m_Flags & CoursePosFlags.HalfAlign) != 0))
				{
					value.m_CoursePos.m_Flags |= CoursePosFlags.HalfAlign;
				}
				else
				{
					value.m_CoursePos.m_Flags &= ~CoursePosFlags.HalfAlign;
				}
				intersectionList[i] = value;
			}
		}

		private bool IsUnder(NetCourse courseData, Bounds1 heightRangeMin, Bounds1 heightRangeMax, IntersectPos intersectPos, float courseHeightOffset, bool canAdjustHeight)
		{
			if (intersectPos.m_IsStartEnd)
			{
				return false;
			}
			float num = math.max(heightRangeMin.max - intersectPos.m_EdgeHeightRangeMin.min, heightRangeMax.max - intersectPos.m_EdgeHeightRangeMax.min);
			if (num <= 0f)
			{
				return true;
			}
			if (!canAdjustHeight)
			{
				return false;
			}
			float num2 = MathUtils.Position(courseData.m_Curve, intersectPos.m_CourseIntersection.min).y + courseHeightOffset;
			float num3 = MathUtils.Position(courseData.m_Curve, intersectPos.m_CourseIntersection.max).y + courseHeightOffset;
			Bounds1 bounds = intersectPos.m_IntersectionHeightMin;
			Bounds1 bounds2 = intersectPos.m_IntersectionHeightMax;
			if (bounds.max < bounds.min)
			{
				bounds = new Bounds1(intersectPos.m_CoursePos.m_Position.y);
			}
			if (bounds2.max < bounds2.min)
			{
				bounds2 = new Bounds1(intersectPos.m_CoursePos.m_Position.y);
			}
			if (num < bounds.min - num2)
			{
				return num < bounds2.min - num3;
			}
			return false;
		}

		private bool IsOver(NetCourse courseData, Bounds1 heightRangeMin, Bounds1 heightRangeMax, IntersectPos intersectPos, float courseHeightOffset, bool canAdjustHeight)
		{
			if (intersectPos.m_IsStartEnd)
			{
				return false;
			}
			float num = math.max(intersectPos.m_EdgeHeightRangeMin.max - heightRangeMin.min, intersectPos.m_EdgeHeightRangeMax.max - heightRangeMax.min);
			if (num <= 0f)
			{
				return true;
			}
			if (!canAdjustHeight)
			{
				return false;
			}
			float num2 = MathUtils.Position(courseData.m_Curve, intersectPos.m_CourseIntersection.min).y + courseHeightOffset;
			float num3 = MathUtils.Position(courseData.m_Curve, intersectPos.m_CourseIntersection.max).y + courseHeightOffset;
			Bounds1 bounds = intersectPos.m_IntersectionHeightMin;
			Bounds1 bounds2 = intersectPos.m_IntersectionHeightMax;
			if (bounds.max < bounds.min)
			{
				bounds = new Bounds1(intersectPos.m_CoursePos.m_Position.y);
			}
			if (bounds2.max < bounds2.min)
			{
				bounds2 = new Bounds1(intersectPos.m_CoursePos.m_Position.y);
			}
			if (num < num2 - bounds.max)
			{
				return num < num3 - bounds2.max;
			}
			return false;
		}

		private void MergeAuxPositions(NetCourse courseData, NativeList<IntersectPos> source, NativeList<IntersectPos> target, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, ref int intersectionAuxIndex)
		{
			int num = 0;
			if (target.Length == 0)
			{
				return;
			}
			ref IntersectPos reference = ref target.ElementAt(num++);
			for (int i = 0; i < source.Length; i++)
			{
				IntersectPos value = source[i];
				if (value.m_CoursePos.m_Entity == Entity.Null)
				{
					continue;
				}
				bool flag = false;
				bool flag2 = false;
				if (i == 0)
				{
					flag = true;
				}
				else if (i == source.Length - 1)
				{
					reference = ref target.ElementAt(target.Length - 1);
					flag = true;
				}
				else
				{
					while (reference.m_CourseIntersection.max < value.m_CourseIntersection.min && num < target.Length)
					{
						reference = ref target.ElementAt(num++);
					}
					if (MathUtils.Intersect(value.m_CourseIntersection, reference.m_CourseIntersection))
					{
						if (num < target.Length)
						{
							IntersectPos intersectPos = target[num];
							if (!MathUtils.Intersect(value.m_CourseIntersection, intersectPos.m_CourseIntersection))
							{
								flag = true;
							}
						}
						else
						{
							flag = true;
						}
					}
					else if (num > 1)
					{
						flag2 = true;
					}
				}
				if (flag)
				{
					if (reference.m_AuxIndex == -1)
					{
						reference.m_AuxIndex = intersectionAuxIndex++;
					}
					int num2 = reference.m_AuxIndex * auxiliaryNets.Length + value.m_AuxIndex;
					while (auxIntersectionEntities.Length <= num2)
					{
						auxIntersectionEntities.Add(default(AuxIntersectionEntity));
					}
					if (auxIntersectionEntities[num2].m_Entity == Entity.Null)
					{
						reference.m_CourseIntersection |= value.m_CourseIntersection;
						reference.m_CanMove = default(Bounds1);
						reference.m_IsOptional = false;
						auxIntersectionEntities[num2] = new AuxIntersectionEntity
						{
							m_Entity = value.m_CoursePos.m_Entity,
							m_SplitPosition = value.m_CoursePos.m_SplitPosition
						};
					}
				}
				else if (flag2)
				{
					int num3 = intersectionAuxIndex * auxiliaryNets.Length + value.m_AuxIndex;
					while (auxIntersectionEntities.Length <= num3)
					{
						auxIntersectionEntities.Add(default(AuxIntersectionEntity));
					}
					auxIntersectionEntities[num3] = new AuxIntersectionEntity
					{
						m_Entity = value.m_CoursePos.m_Entity,
						m_SplitPosition = value.m_CoursePos.m_SplitPosition
					};
					value.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value.m_CoursePos.m_CourseDelta);
					value.m_CoursePos.m_Entity = Entity.Null;
					value.m_CoursePos.m_SplitPosition = 0f;
					value.m_AuxIndex = intersectionAuxIndex++;
					CollectionUtils.Insert(target, num, value);
					reference = ref target.ElementAt(num++);
				}
			}
		}

		private void MergePositions(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, NetData prefabNetData, NetGeometryData prefabGeometryData, ref CourseHeightData courseHeightData, NativeList<IntersectPos> source, NativeList<IntersectPos> target, float courseHeightOffset, bool canAdjustHeight, bool canConnectWaterway, bool canBeSplitted, bool canSplitOwnedEdges, bool hasSubNets)
		{
			int num = 0;
			int num2 = 0;
			while (num < source.Length)
			{
				IntersectPos value = source[num++];
				bool flag = canAdjustHeight;
				bool flag2 = !hasSubNets;
				if (canConnectWaterway && m_PrefabRefData.TryGetComponent(value.m_CoursePos.m_Entity, out var componentData) && m_PrefabNetData.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					flag &= (componentData2.m_RequiredLayers & Layer.Waterway) == 0;
					flag2 = (componentData2.m_RequiredLayers & Layer.Waterway) != 0;
				}
				courseHeightData.GetHeightRange(value, prefabGeometryData, flag2, out var minBounds, out var maxBounds, out var elevated);
				minBounds += courseHeightOffset;
				maxBounds += courseHeightOffset;
				if (IsUnder(courseData, minBounds, maxBounds, value, courseHeightOffset, flag))
				{
					if (flag)
					{
						float num3 = courseHeightOffset + math.select(prefabGeometryData.m_DefaultHeightRange.max, prefabGeometryData.m_ElevatedHeightRange.max, flag2 && elevated) + 0.5f;
						courseHeightData.ApplyLimitRange(value, prefabGeometryData, new Bounds1(-1000000f, value.m_EdgeHeightRangeMin.min - num3), new Bounds1(-1000000f, value.m_EdgeHeightRangeMax.min - num3));
					}
					continue;
				}
				bool flag3 = !IsOver(courseData, minBounds, maxBounds, value, courseHeightOffset, flag);
				bool ignoreHeight = false;
				bool flag4 = flag3 && CanConnect(creationDefinition, ownerDefinition, prefabNetData, value.m_IsNode, canSplitOwnedEdges, value.m_CoursePos.m_Entity, out ignoreHeight);
				int num4 = num;
				num2 += math.select(0, 1, value.m_IsStartEnd);
				while (num < source.Length)
				{
					IntersectPos intersectPos = source[num];
					bool flag5 = canAdjustHeight;
					bool flag6 = !hasSubNets;
					if (canConnectWaterway && m_PrefabRefData.TryGetComponent(intersectPos.m_CoursePos.m_Entity, out var componentData3) && m_PrefabNetData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
					{
						flag5 &= (componentData4.m_RequiredLayers & Layer.Waterway) == 0;
						flag6 = (componentData4.m_RequiredLayers & Layer.Waterway) != 0;
					}
					courseHeightData.GetHeightRange(intersectPos, prefabGeometryData, flag6, out var minBounds2, out var maxBounds2, out elevated);
					minBounds2 += courseHeightOffset;
					maxBounds2 += courseHeightOffset;
					bool flag7 = flag4;
					if (IsUnder(courseData, minBounds2, maxBounds2, intersectPos, courseHeightOffset, flag5))
					{
						if (flag5)
						{
							float num5 = courseHeightOffset + math.select(prefabGeometryData.m_DefaultHeightRange.max, prefabGeometryData.m_ElevatedHeightRange.max, flag6 && elevated) + 0.5f;
							courseHeightData.ApplyLimitRange(intersectPos, prefabGeometryData, new Bounds1(-1000000f, intersectPos.m_EdgeHeightRangeMin.min - num5), new Bounds1(-1000000f, intersectPos.m_EdgeHeightRangeMax.min - num5));
						}
						if (num > num4)
						{
							source.RemoveAt(num);
							continue;
						}
						num++;
						num4++;
						continue;
					}
					if ((value.m_IsStartEnd && intersectPos.m_IsStartEnd && (creationDefinition.m_Flags & CreationFlags.SubElevation) == 0 && (value.m_CoursePos.m_Flags & intersectPos.m_CoursePos.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) || !MathUtils.Intersect(intersectPos.m_CourseIntersection, value.m_CourseIntersection, out var intersection))
					{
						break;
					}
					if (intersectPos.m_CoursePos.m_Entity != value.m_CoursePos.m_Entity)
					{
						bool flag8 = !IsOver(courseData, minBounds2, maxBounds2, intersectPos, courseHeightOffset, flag5);
						if (flag3 || flag8)
						{
							bool ignoreHeight2;
							bool flag9 = flag8 && CanConnect(creationDefinition, ownerDefinition, prefabNetData, value.m_IsNode | intersectPos.m_IsNode, canSplitOwnedEdges, intersectPos.m_CoursePos.m_Entity, out ignoreHeight2);
							if (!flag4 && flag9 && intersectPos.m_IsNode && !value.m_IsNode)
							{
								flag7 = flag3 && CanConnect(creationDefinition, ownerDefinition, prefabNetData, isNode: true, canSplitOwnedEdges, value.m_CoursePos.m_Entity, out ignoreHeight2);
							}
							if (flag7 != flag9)
							{
								num++;
								continue;
							}
							if (flag7 && intersection.min > 0f && intersection.max < 1f)
							{
								num++;
								continue;
							}
						}
					}
					flag4 = flag7;
					if (value.m_IsNode && !intersectPos.m_IsNode)
					{
						intersectPos.m_CoursePos.m_Entity = value.m_CoursePos.m_Entity;
						intersectPos.m_IsNode = true;
					}
					else if (!value.m_IsNode && intersectPos.m_IsNode)
					{
						value.m_CoursePos.m_Entity = intersectPos.m_CoursePos.m_Entity;
						value.m_IsNode = true;
					}
					if (intersectPos.m_CoursePos.m_Entity == Entity.Null)
					{
						intersectPos.m_CoursePos.m_Entity = value.m_CoursePos.m_Entity;
						intersectPos.m_CoursePos.m_SplitPosition = value.m_CoursePos.m_SplitPosition;
					}
					else if (value.m_CoursePos.m_Entity == Entity.Null)
					{
						value.m_CoursePos.m_Entity = intersectPos.m_CoursePos.m_Entity;
						value.m_CoursePos.m_SplitPosition = intersectPos.m_CoursePos.m_SplitPosition;
					}
					if (intersectPos.m_CourseIntersection.min < value.m_CourseIntersection.min)
					{
						value.m_CourseIntersection.min = intersectPos.m_CourseIntersection.min;
						value.m_IntersectionHeightMin = intersectPos.m_IntersectionHeightMin;
						value.m_EdgeHeightRangeMin = intersectPos.m_EdgeHeightRangeMin;
					}
					else if (intersectPos.m_CourseIntersection.min == value.m_CourseIntersection.min)
					{
						value.m_IntersectionHeightMin |= intersectPos.m_IntersectionHeightMin;
						value.m_EdgeHeightRangeMin |= intersectPos.m_EdgeHeightRangeMin;
					}
					if (intersectPos.m_CourseIntersection.max > value.m_CourseIntersection.max)
					{
						value.m_CourseIntersection.max = intersectPos.m_CourseIntersection.max;
						value.m_IntersectionHeightMax = intersectPos.m_IntersectionHeightMax;
						value.m_EdgeHeightRangeMax = intersectPos.m_EdgeHeightRangeMax;
					}
					else if (intersectPos.m_CourseIntersection.max == value.m_CourseIntersection.max)
					{
						value.m_IntersectionHeightMax |= intersectPos.m_IntersectionHeightMax;
						value.m_EdgeHeightRangeMax |= intersectPos.m_EdgeHeightRangeMax;
					}
					num2 += math.select(0, 1, intersectPos.m_IsStartEnd);
					value.m_IsStartEnd |= intersectPos.m_IsStartEnd;
					value.m_EdgeIntersection |= intersectPos.m_EdgeIntersection;
					value.m_IsTunnel &= intersectPos.m_IsTunnel;
					value.m_IsWaterway |= intersectPos.m_IsWaterway;
					CoursePosFlags flags = value.m_CoursePos.m_Flags;
					CoursePosFlags flags2 = intersectPos.m_CoursePos.m_Flags;
					value.m_CoursePos.m_Flags |= flags2 & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast);
					value.m_CoursePos.m_Flags &= (CoursePosFlags)((uint)flags2 | 0xFFFFFF7Fu);
					intersectPos.m_CoursePos.m_Flags |= flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast);
					intersectPos.m_CoursePos.m_Flags &= (CoursePosFlags)((uint)flags | 0xFFFFFF7Fu);
					if (intersectPos.m_Priority < value.m_Priority)
					{
						value.m_CoursePos = intersectPos.m_CoursePos;
						value.m_Priority = intersectPos.m_Priority;
					}
					if (num > num4)
					{
						source.RemoveAt(num);
						continue;
					}
					num++;
					num4++;
				}
				if (flag4)
				{
					if (!ignoreHeight)
					{
						Bounds1 bounds = value.m_IntersectionHeightMin;
						Bounds1 bounds2 = value.m_IntersectionHeightMax;
						if (bounds.max < bounds.min)
						{
							bounds = new Bounds1(value.m_CoursePos.m_Position.y);
						}
						if (bounds2.max < bounds2.min)
						{
							bounds2 = new Bounds1(value.m_CoursePos.m_Position.y);
						}
						if (value.m_IsWaterway && value.m_CoursePos.m_Entity != Entity.Null && value.m_IsNode)
						{
							value.m_CoursePos.m_Position.y = m_NodeData[value.m_CoursePos.m_Entity].m_Position.y;
							bounds = new Bounds1(value.m_CoursePos.m_Position.y);
							bounds2 = new Bounds1(value.m_CoursePos.m_Position.y);
						}
						courseHeightData.ApplyLimitRange(value, prefabGeometryData, bounds - courseHeightOffset, bounds2 - courseHeightOffset, shrink: true);
					}
				}
				else
				{
					value.m_CoursePos.m_Entity = Entity.Null;
					elevated |= !value.m_IsTunnel;
					if (flag)
					{
						float num6 = courseHeightOffset + math.select(prefabGeometryData.m_DefaultHeightRange.min, prefabGeometryData.m_ElevatedHeightRange.min, flag2 && elevated) - 0.5f;
						bool forceElevated = !value.m_IsTunnel && (prefabGeometryData.m_Flags & Game.Net.GeometryFlags.ExclusiveGround) != 0;
						courseHeightData.ApplyLimitRange(value, prefabGeometryData, new Bounds1(value.m_EdgeHeightRangeMin.max - num6, 1000000f), new Bounds1(value.m_EdgeHeightRangeMax.max - num6, 1000000f), shrink: false, forceElevated);
					}
					if ((value.m_IsTunnel || value.m_AuxIndex != -1) && !value.m_IsStartEnd)
					{
						num = num4;
						continue;
					}
					value.m_IsOptional = !value.m_IsStartEnd;
				}
				if (value.m_IsStartEnd)
				{
					target.Add(in value);
				}
				else if (canBeSplitted)
				{
					if (num2 >= 2)
					{
						if (target.Length >= 2)
						{
							CollectionUtils.Insert(target, target.Length - 1, value);
						}
					}
					else
					{
						target.Add(in value);
					}
				}
				num = num4;
			}
		}

		private bool CanConnect(CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, NetData prefabNetData1, bool isNode, bool canSplitOwnedEdges, Entity entity2, out bool ignoreHeight)
		{
			ignoreHeight = false;
			if (entity2 == Entity.Null)
			{
				return true;
			}
			PrefabRef prefabRef = m_PrefabRefData[entity2];
			NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
			bool flag = !m_EditorMode && m_PrefabServiceUpgradeData.HasComponent(creationDefinition.m_Prefab);
			bool flag2 = !m_EditorMode && m_PrefabServiceUpgradeData.HasComponent(prefabRef.m_Prefab);
			if (((!canSplitOwnedEdges && !isNode) || ((prefabNetData1.m_RequiredLayers & (Layer.MarkerPathway | Layer.MarkerTaxiway)) == 0 && (netData.m_RequiredLayers & (Layer.MarkerPathway | Layer.MarkerTaxiway)) != Layer.None && (creationDefinition.m_Flags & CreationFlags.SubElevation) != 0)) && m_OwnerData.TryGetComponent(entity2, out var componentData))
			{
				if (creationDefinition.m_Owner != componentData.m_Owner && (!m_TransformData.TryGetComponent(componentData.m_Owner, out var componentData2) || !m_PrefabRefData.TryGetComponent(componentData.m_Owner, out var componentData3) || !ownerDefinition.m_Position.Equals(componentData2.m_Position) || !ownerDefinition.m_Rotation.Equals(componentData2.m_Rotation) || ownerDefinition.m_Prefab != componentData3.m_Prefab))
				{
					return false;
				}
				if (!canSplitOwnedEdges && !isNode && flag && !m_ServiceUpgradeData.HasComponent(entity2))
				{
					return false;
				}
			}
			if (!isNode && !m_PrefabData.IsComponentEnabled(prefabRef.m_Prefab))
			{
				return false;
			}
			if (!NetUtils.CanConnect(prefabNetData1, netData))
			{
				return false;
			}
			if (flag || flag2)
			{
				Entity entity3 = entity2;
				while (m_OwnerData.TryGetComponent(entity3, out componentData))
				{
					entity3 = componentData.m_Owner;
				}
				if (creationDefinition.m_Owner != entity3 && (!m_TransformData.TryGetComponent(entity3, out var componentData4) || !m_PrefabRefData.TryGetComponent(entity3, out var componentData5) || !ownerDefinition.m_Position.Equals(componentData4.m_Position) || !ownerDefinition.m_Rotation.Equals(componentData4.m_Rotation) || ownerDefinition.m_Prefab != componentData5.m_Prefab))
				{
					return false;
				}
			}
			Game.Net.Elevation componentData6;
			NetGeometryData componentData7;
			if (((prefabNetData1.m_RequiredLayers ^ netData.m_RequiredLayers) & Layer.Waterway) != Layer.None)
			{
				ignoreHeight = true;
			}
			else if (((prefabNetData1.m_RequiredLayers ^ netData.m_RequiredLayers) & Layer.TrainTrack) != Layer.None && m_ElevationData.TryGetComponent(entity2, out componentData6) && m_PrefabGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData7) && math.any(math.abs(componentData6.m_Elevation) >= componentData7.m_ElevationLimit))
			{
				return false;
			}
			return true;
		}

		private void GetElevationRanges(NetCourse courseData, NetGeometryData prefabGeometryData, ref CourseHeightData courseHeightData, Bounds1 courseDelta, NativeList<ElevationSegment> leftSegments, NativeList<ElevationSegment> rightSegments)
		{
			float num = MathUtils.Length(courseData.m_Curve.xz, courseDelta);
			float num2 = prefabGeometryData.m_DefaultWidth * 0.5f;
			int num3 = Mathf.RoundToInt(num / 4f);
			leftSegments.Clear();
			rightSegments.Clear();
			if (num3 < 1)
			{
				return;
			}
			float3 @float = MathUtils.Position(courseData.m_Curve, courseDelta.min);
			float2 float2 = courseDelta.min;
			float2 float3 = 0f;
			int2 @int = 0;
			bool2 @bool = true;
			float num4 = courseDelta.min;
			float num5 = 0f;
			float num6 = 1f / (float)num3;
			int2 int2 = default(int2);
			ElevationSegment value;
			for (int i = 1; i <= num3; i++)
			{
				float num7 = math.lerp(courseDelta.min, courseDelta.max, (float)i * num6);
				float num8 = math.lerp(courseDelta.min, courseDelta.max, ((float)i - 0.5f) * num6);
				float3 float4 = MathUtils.Position(courseData.m_Curve, num7);
				float3 float5 = MathUtils.Position(courseData.m_Curve, num8);
				float5.y = courseHeightData.SampleHeight(num8, out var forceElevated);
				float3 float6 = new float3
				{
					xz = math.normalizesafe(MathUtils.Right(MathUtils.Tangent(courseData.m_Curve.xz, num8))) * num2
				};
				bool flag;
				if (forceElevated)
				{
					int2 = 2;
					flag = false;
				}
				else
				{
					int2.x = GetElevationType(prefabGeometryData, float5 - float6);
					int2.y = GetElevationType(prefabGeometryData, float5 + float6);
					flag = true;
				}
				if (i != 1)
				{
					if (int2.x != @int.x)
					{
						value = new ElevationSegment
						{
							m_CourseRange = new Bounds1(float2.x, num4),
							m_DistanceOffset = new Bounds1(float3.x, num5),
							m_ElevationType = @int.x,
							m_CanRemove = @bool.x
						};
						leftSegments.Add(in value);
						@bool.x = true;
						float2.x = num4;
						float3.x = num5;
					}
					if (int2.y != @int.y)
					{
						value = new ElevationSegment
						{
							m_CourseRange = new Bounds1(float2.y, num4),
							m_DistanceOffset = new Bounds1(float3.y, num5),
							m_ElevationType = @int.y,
							m_CanRemove = @bool.y
						};
						rightSegments.Add(in value);
						@bool.y = true;
						float2.y = num4;
						float3.y = num5;
					}
				}
				num4 = num7;
				num5 += math.distance(@float.xz, float4.xz);
				@int = int2;
				@bool &= flag;
				@float = float4;
			}
			value = new ElevationSegment
			{
				m_CourseRange = new Bounds1(float2.x, num4),
				m_DistanceOffset = new Bounds1(float3.x, num5),
				m_ElevationType = @int.x,
				m_CanRemove = @bool.x
			};
			leftSegments.Add(in value);
			value = new ElevationSegment
			{
				m_CourseRange = new Bounds1(float2.y, num4),
				m_DistanceOffset = new Bounds1(float3.y, num5),
				m_ElevationType = @int.y,
				m_CanRemove = @bool.y
			};
			rightSegments.Add(in value);
		}

		private int GetElevationType(NetGeometryData prefabGeometryData, float3 position)
		{
			float num = position.y - TerrainUtils.SampleHeight(ref m_TerrainHeightData, position);
			float3 @float = new float3(prefabGeometryData.m_ElevationLimit * 0.5f, prefabGeometryData.m_ElevationLimit, prefabGeometryData.m_ElevationLimit * 3f);
			int2 x = math.select(0, new int2(1), num >= @float.xy);
			int3 x2 = math.select(0, new int3(-1), num <= -@float);
			return math.csum(x) + math.csum(x2);
		}

		private void ExpandMajorElevationSegments(NetCourse courseData, NativeList<ElevationSegment> elevationSegments)
		{
			float x = 8f;
			for (int i = 0; i < elevationSegments.Length; i++)
			{
				ElevationSegment value = elevationSegments[i];
				if (!math.all(value.m_ElevationType == 1) && !math.all(value.m_ElevationType == -1))
				{
					continue;
				}
				float num = math.min(x, MathUtils.Size(value.m_DistanceOffset) * 0.5f);
				int num2 = math.select(2, -2, value.m_ElevationType.x == -1);
				if (i > 0)
				{
					ElevationSegment value2 = elevationSegments[i - 1];
					if (math.all(value2.m_ElevationType == num2))
					{
						Bounds1 t = value.m_CourseRange;
						MathUtils.ClampLength(courseData.m_Curve.xz, ref t, num);
						value.m_CourseRange.min = t.max;
						value.m_DistanceOffset.min += num;
						value2.m_CourseRange.max = t.max;
						value2.m_DistanceOffset.max += num;
						elevationSegments[i - 1] = value2;
					}
				}
				if (i < elevationSegments.Length - 1)
				{
					ElevationSegment value3 = elevationSegments[i + 1];
					if (math.all(value3.m_ElevationType == num2))
					{
						Bounds1 t2 = value.m_CourseRange;
						MathUtils.ClampLengthInverse(courseData.m_Curve.xz, ref t2, num);
						value.m_CourseRange.max = t2.min;
						value.m_DistanceOffset.max -= num;
						value3.m_CourseRange.min = t2.min;
						value3.m_DistanceOffset.min -= num;
						elevationSegments[i + 1] = value3;
					}
				}
				value.m_ElevationType = 0;
				elevationSegments[i] = value;
			}
		}

		private void MergeSimilarElevationSegments(NativeList<ElevationSegment> elevationSegments)
		{
			int i = 0;
			int num = 0;
			while (i < elevationSegments.Length)
			{
				ElevationSegment value = elevationSegments[i++];
				for (; i < elevationSegments.Length; i++)
				{
					ElevationSegment elevationSegment = elevationSegments[i];
					if (math.any(elevationSegment.m_ElevationType != value.m_ElevationType))
					{
						break;
					}
					value.m_CourseRange.max = elevationSegment.m_CourseRange.max;
					value.m_DistanceOffset.max = elevationSegment.m_DistanceOffset.max;
					value.m_CanRemove &= elevationSegment.m_CanRemove;
				}
				elevationSegments[num++] = value;
			}
			if (num < i)
			{
				elevationSegments.RemoveRange(num, i - num);
			}
		}

		private void RemoveShortElevationSegments(NetGeometryData prefabGeometryData, NativeList<ElevationSegment> elevationSegments)
		{
			float defaultWidth = prefabGeometryData.m_DefaultWidth;
			for (int i = 0; i < elevationSegments.Length; i++)
			{
				ElevationSegment elevationSegment = elevationSegments[i];
				float num = MathUtils.Size(elevationSegment.m_DistanceOffset);
				if (!(num < defaultWidth) || !elevationSegment.m_CanRemove)
				{
					continue;
				}
				int num2 = i--;
				for (int j = num2 + 1; j < elevationSegments.Length; j++)
				{
					ElevationSegment elevationSegment2 = elevationSegments[j];
					float num3 = MathUtils.Size(elevationSegment2.m_DistanceOffset);
					if (num3 >= num || !elevationSegment2.m_CanRemove)
					{
						break;
					}
					num = num3;
					num2 = j;
				}
				elevationSegment = elevationSegments[num2];
				ElevationSegment value = new ElevationSegment
				{
					m_ElevationType = -1000000
				};
				if (num2 > 0)
				{
					value = elevationSegments[num2 - 1];
					value.m_CourseRange.max = MathUtils.Center(elevationSegment.m_CourseRange);
					value.m_DistanceOffset.max = MathUtils.Center(elevationSegment.m_DistanceOffset);
					elevationSegments[num2 - 1] = value;
				}
				if (num2 < elevationSegments.Length - 1)
				{
					ElevationSegment value2 = elevationSegments[num2 + 1];
					if (math.all(value2.m_ElevationType == value.m_ElevationType))
					{
						value.m_CourseRange.max = value2.m_CourseRange.max;
						value.m_DistanceOffset.max = value2.m_DistanceOffset.max;
						elevationSegments[num2 - 1] = value;
						elevationSegments.RemoveAt(num2 + 1);
					}
					else
					{
						value2.m_CourseRange.min = MathUtils.Center(elevationSegment.m_CourseRange);
						value2.m_DistanceOffset.min = MathUtils.Center(elevationSegment.m_DistanceOffset);
						elevationSegments[num2 + 1] = value2;
					}
				}
				elevationSegments.RemoveAt(num2);
			}
		}

		private void MergeSideElevationSegments(NativeList<ElevationSegment> leftSegments, NativeList<ElevationSegment> rightSegments)
		{
			int length = rightSegments.Length;
			rightSegments.AddRange(leftSegments.AsArray());
			int length2 = rightSegments.Length;
			int num = length;
			int num2 = 0;
			leftSegments.Clear();
			ElevationSegment value = new ElevationSegment
			{
				m_ElevationType = -1000000
			};
			bool2 @bool = true;
			while (true)
			{
				ElevationSegment elevationSegment = new ElevationSegment
				{
					m_ElevationType = -1000000
				};
				ElevationSegment elevationSegment2 = new ElevationSegment
				{
					m_ElevationType = -1000000
				};
				if (num < length2 && num2 < length)
				{
					ElevationSegment elevationSegment3 = rightSegments[num];
					ElevationSegment elevationSegment4 = rightSegments[num2];
					if (elevationSegment.m_CourseRange.min <= elevationSegment2.m_CourseRange.min)
					{
						elevationSegment = elevationSegment3;
						num++;
					}
					else
					{
						elevationSegment2 = elevationSegment4;
						num2++;
					}
				}
				else if (num < length2)
				{
					elevationSegment = rightSegments[num++];
				}
				else
				{
					if (num2 >= length)
					{
						break;
					}
					elevationSegment2 = rightSegments[num2++];
				}
				if (elevationSegment.m_ElevationType.x != -1000000)
				{
					if (math.all(value.m_ElevationType != -1000000))
					{
						value.m_CourseRange.max = elevationSegment.m_CourseRange.min;
						value.m_DistanceOffset.max = elevationSegment.m_DistanceOffset.min;
						leftSegments.Add(in value);
						@bool.x = true;
					}
					value.m_CourseRange = elevationSegment.m_CourseRange;
					value.m_DistanceOffset = elevationSegment.m_DistanceOffset;
					value.m_ElevationType.x = elevationSegment.m_ElevationType.x;
					@bool &= elevationSegment.m_CanRemove;
				}
				if (elevationSegment2.m_ElevationType.y != -1000000)
				{
					if (math.all(value.m_ElevationType != -1000000))
					{
						value.m_CourseRange.max = elevationSegment2.m_CourseRange.min;
						value.m_DistanceOffset.max = elevationSegment2.m_DistanceOffset.min;
						leftSegments.Add(in value);
						@bool.y = true;
					}
					value.m_CourseRange = elevationSegment2.m_CourseRange;
					value.m_DistanceOffset = elevationSegment2.m_DistanceOffset;
					value.m_ElevationType.y = elevationSegment2.m_ElevationType.y;
					@bool &= elevationSegment2.m_CanRemove;
				}
			}
			if (math.all(value.m_ElevationType != -1000000))
			{
				leftSegments.Add(in value);
			}
		}

		private void CheckHeightRange(NetCourse courseData, NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData, ref CourseHeightData courseHeightData, bool sampleTerrain, NativeList<IntersectPos> source, NativeList<IntersectPos> target)
		{
			bool flag = false;
			NativeList<ElevationSegment> nativeList = new NativeList<ElevationSegment>(32, Allocator.Temp);
			NativeList<ElevationSegment> nativeList2 = new NativeList<ElevationSegment>(32, Allocator.Temp);
			for (int i = 0; i < source.Length; i++)
			{
				IntersectPos value = source[i];
				if (sampleTerrain && i != 0 && (prefabGeometryData.m_Flags & Game.Net.GeometryFlags.RequireElevated) == 0 && (placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.ShoreLine) == 0)
				{
					IntersectPos intersectPos = source[i - 1];
					GetElevationRanges(courseDelta: new Bounds1(intersectPos.m_CourseIntersection.max, value.m_CourseIntersection.min), courseData: courseData, prefabGeometryData: prefabGeometryData, courseHeightData: ref courseHeightData, leftSegments: nativeList, rightSegments: nativeList2);
					ExpandMajorElevationSegments(courseData, nativeList);
					ExpandMajorElevationSegments(courseData, nativeList2);
					MergeSimilarElevationSegments(nativeList);
					MergeSimilarElevationSegments(nativeList2);
					RemoveShortElevationSegments(prefabGeometryData, nativeList);
					RemoveShortElevationSegments(prefabGeometryData, nativeList2);
					MergeSideElevationSegments(nativeList, nativeList2);
					RemoveShortElevationSegments(prefabGeometryData, nativeList);
					for (int j = 1; j < nativeList.Length; j++)
					{
						ElevationSegment elevationSegment = nativeList[j - 1];
						ElevationSegment elevationSegment2 = nativeList[j];
						IntersectPos value2 = new IntersectPos
						{
							m_CoursePos = 
							{
								m_CourseDelta = elevationSegment2.m_CourseRange.min,
								m_Flags = (value.m_CoursePos.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.IsGrid))
							}
						};
						value2.m_CoursePos.m_Flags |= CoursePosFlags.FreeHeight;
						value2.m_CoursePos.m_ParentMesh = math.select(value.m_CoursePos.m_ParentMesh, -1, value.m_CoursePos.m_ParentMesh != intersectPos.m_CoursePos.m_ParentMesh);
						value2.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value2.m_CoursePos.m_CourseDelta);
						value2.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value2.m_CoursePos.m_CourseDelta));
						value2.m_AuxIndex = -1;
						if (elevationSegment2.m_ElevationType.x != elevationSegment.m_ElevationType.x)
						{
							value2.m_CoursePos.m_Flags |= CoursePosFlags.LeftTransition;
						}
						if (elevationSegment2.m_ElevationType.y != elevationSegment.m_ElevationType.y)
						{
							value2.m_CoursePos.m_Flags |= CoursePosFlags.RightTransition;
						}
						if (flag)
						{
							int index = target.Length;
							for (int num = target.Length - 1; num >= 0; num--)
							{
								IntersectPos value3 = target[num];
								if (!value3.m_IsOptional || value3.m_CoursePos.m_CourseDelta <= value2.m_CoursePos.m_CourseDelta)
								{
									break;
								}
								float num2 = MathUtils.Length(t: new Bounds1(value2.m_CoursePos.m_CourseDelta, value3.m_CoursePos.m_CourseDelta), curve: courseData.m_Curve.xz);
								value3.m_CanMove.max = math.max(value3.m_CanMove.max, prefabGeometryData.m_ElevatedLength * 0.95f - num2);
								target[num] = value3;
								index = num;
							}
							CollectionUtils.Insert(target, index, value2);
						}
						else
						{
							target.Add(in value2);
						}
					}
				}
				if (!value.m_IsOptional && !m_FixedData.HasComponent(value.m_CoursePos.m_Entity))
				{
					if (flag && !value.m_IsStartEnd)
					{
						int index2 = target.Length;
						for (int num3 = target.Length - 1; num3 >= 0; num3--)
						{
							IntersectPos value4 = target[num3];
							if (!value4.m_IsOptional || value4.m_CoursePos.m_CourseDelta <= value.m_CoursePos.m_CourseDelta)
							{
								break;
							}
							float num4 = MathUtils.Length(t: new Bounds1(value.m_CoursePos.m_CourseDelta, value4.m_CoursePos.m_CourseDelta), curve: courseData.m_Curve.xz);
							value4.m_CanMove.max = math.max(value4.m_CanMove.max, prefabGeometryData.m_ElevatedLength * 0.95f - num4);
							target[num3] = value4;
							index2 = num3;
						}
						CollectionUtils.Insert(target, index2, value);
					}
					else
					{
						target.Add(in value);
					}
					continue;
				}
				float num5 = prefabGeometryData.m_DefaultWidth * 0.5f;
				if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.MiddlePillars) != 0)
				{
					float num6 = MathUtils.Length(courseData.m_Curve.xz, value.m_CourseIntersection);
					Bounds1 t = new Bounds1(value.m_CourseIntersection.min, value.m_CourseIntersection.max);
					MathUtils.ClampLength(courseData.m_Curve.xz, ref t, num6 * 0.5f);
					num6 += num5 * 2f;
					float y = (prefabGeometryData.m_ElevatedLength * 0.95f - num6) * 0.5f;
					y = math.max(0f, y);
					value.m_CoursePos.m_Entity = Entity.Null;
					value.m_CoursePos.m_CourseDelta = math.min(1f, t.max);
					value.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value.m_CoursePos.m_CourseDelta);
					value.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value.m_CoursePos.m_CourseDelta));
					value.m_CanMove = new Bounds1(0f - y, y);
					value.m_IsOptional = true;
					value.m_AuxIndex = -1;
					float x = MathUtils.Length(courseData.m_Curve.xz, new Bounds1(0f, value.m_CoursePos.m_CourseDelta));
					float y2 = MathUtils.Length(courseData.m_Curve.xz, new Bounds1(value.m_CoursePos.m_CourseDelta, 1f));
					if (!(math.min(x, y2) > y))
					{
						continue;
					}
					int num7 = target.Length;
					for (int num8 = target.Length - 1; num8 >= 0; num8--)
					{
						IntersectPos intersectPos2 = target[num8];
						if (intersectPos2.m_IsOptional || intersectPos2.m_IsStartEnd || intersectPos2.m_CoursePos.m_CourseDelta <= value.m_CoursePos.m_CourseDelta)
						{
							break;
						}
						num7 = num8;
					}
					if (num7 < target.Length)
					{
						float num9 = MathUtils.Length(t: new Bounds1(value.m_CoursePos.m_CourseDelta, target[num7].m_CoursePos.m_CourseDelta), curve: courseData.m_Curve.xz);
						value.m_CanMove.min = math.min(value.m_CanMove.min, num9 - prefabGeometryData.m_ElevatedLength * 0.95f);
					}
					CollectionUtils.Insert(target, num7, value);
					flag = true;
					continue;
				}
				Bounds1 t2 = new Bounds1(0f, value.m_CourseIntersection.min);
				Bounds1 t3 = new Bounds1(value.m_CourseIntersection.max, 1f);
				float num10 = MathUtils.Length(courseData.m_Curve.xz, value.m_CourseIntersection) + num5 * 2f;
				float y3 = (prefabGeometryData.m_ElevatedLength * 0.95f - num10) * 0.5f;
				num5 = math.max(0f, num5 + math.min(0f, y3));
				y3 = math.max(0f, y3);
				MathUtils.ClampLengthInverse(courseData.m_Curve.xz, ref t2, num5);
				MathUtils.ClampLength(courseData.m_Curve.xz, ref t3, num5);
				value.m_CoursePos.m_Entity = Entity.Null;
				value.m_CoursePos.m_CourseDelta = math.max(0f, t2.min);
				value.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value.m_CoursePos.m_CourseDelta);
				value.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value.m_CoursePos.m_CourseDelta));
				value.m_CanMove = new Bounds1(0f - y3, 0f);
				value.m_IsOptional = true;
				value.m_AuxIndex = -1;
				if (MathUtils.Length(courseData.m_Curve.xz, new Bounds1(value.m_CoursePos.m_CourseDelta, 1f)) > y3)
				{
					int num11 = target.Length;
					for (int num12 = target.Length - 1; num12 >= 0; num12--)
					{
						IntersectPos intersectPos3 = target[num12];
						if (intersectPos3.m_IsOptional || intersectPos3.m_IsStartEnd || intersectPos3.m_CoursePos.m_CourseDelta <= value.m_CoursePos.m_CourseDelta)
						{
							break;
						}
						num11 = num12;
					}
					if (num11 < target.Length)
					{
						float num13 = MathUtils.Length(t: new Bounds1(value.m_CoursePos.m_CourseDelta, target[num11].m_CoursePos.m_CourseDelta), curve: courseData.m_Curve.xz);
						value.m_CanMove.min = math.min(value.m_CanMove.min, num13 - prefabGeometryData.m_ElevatedLength * 0.95f);
					}
					CollectionUtils.Insert(target, num11, value);
					flag = true;
				}
				value.m_CoursePos.m_Entity = Entity.Null;
				value.m_CoursePos.m_CourseDelta = math.min(1f, t3.max);
				value.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value.m_CoursePos.m_CourseDelta);
				value.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value.m_CoursePos.m_CourseDelta));
				value.m_CanMove = new Bounds1(0f, y3);
				value.m_IsOptional = true;
				value.m_AuxIndex = -1;
				if (MathUtils.Length(courseData.m_Curve.xz, new Bounds1(0f, value.m_CoursePos.m_CourseDelta)) > y3)
				{
					target.Add(in value);
					flag = true;
				}
			}
			nativeList.Dispose();
			nativeList2.Dispose();
			if (!flag)
			{
				return;
			}
			source.Clear();
			for (int k = 0; k < target.Length; k++)
			{
				IntersectPos value5 = target[k];
				if (value5.m_IsOptional)
				{
					for (; k + 1 < target.Length; k++)
					{
						IntersectPos intersectPos4 = target[k + 1];
						if (!intersectPos4.m_IsOptional)
						{
							break;
						}
						if (intersectPos4.m_CoursePos.m_CourseDelta <= value5.m_CoursePos.m_CourseDelta)
						{
							value5.m_CoursePos.m_CourseDelta = math.lerp(value5.m_CoursePos.m_CourseDelta, intersectPos4.m_CoursePos.m_CourseDelta, 0.5f);
							value5.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value5.m_CoursePos.m_CourseDelta);
							value5.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value5.m_CoursePos.m_CourseDelta));
							value5.m_CanMove = new Bounds1(0f, 0f);
							continue;
						}
						Bounds1 t4 = new Bounds1(value5.m_CoursePos.m_CourseDelta, intersectPos4.m_CoursePos.m_CourseDelta);
						float num14 = MathUtils.Length(courseData.m_Curve.xz, t4);
						if (!(value5.m_CanMove.max - intersectPos4.m_CanMove.min >= num14))
						{
							break;
						}
						float num15 = math.min(value5.m_CanMove.max, math.max(num14 * 0.5f, num14 + intersectPos4.m_CanMove.min));
						MathUtils.ClampLength(courseData.m_Curve.xz, ref t4, num15);
						value5.m_CoursePos.m_CourseDelta = t4.max;
						value5.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value5.m_CoursePos.m_CourseDelta);
						value5.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value5.m_CoursePos.m_CourseDelta));
						value5.m_CanMove.min = math.min(0f, math.max(value5.m_CanMove.min - num15, intersectPos4.m_CanMove.min + num14 - num15));
						value5.m_CanMove.max = math.max(0f, math.min(intersectPos4.m_CanMove.max + num14 - num15, value5.m_CanMove.max - num15));
					}
				}
				source.Add(in value5);
			}
			target.Clear();
			for (int l = 1; l < source.Length; l++)
			{
				IntersectPos value6 = source[l - 1];
				value6.m_Priority = MathUtils.Length(t: new Bounds1(_max: source[l].m_CoursePos.m_CourseDelta, _min: value6.m_CoursePos.m_CourseDelta), curve: courseData.m_Curve.xz);
				source[l - 1] = value6;
			}
			for (int m = 0; m < source.Length; m++)
			{
				if (FindOptionalRange(source, m, out var minIndex, out var maxIndex))
				{
					m = maxIndex;
					int bestIndex;
					while (FindBestIntersectionToRemove(prefabGeometryData, source, minIndex, maxIndex, out bestIndex))
					{
						IntersectPos value7 = source[bestIndex - 1];
						IntersectPos intersectPos5 = source[bestIndex];
						value7.m_Priority += intersectPos5.m_Priority;
						source[bestIndex - 1] = value7;
						if (bestIndex == minIndex)
						{
							value7 = target[target.Length - 1];
							value7.m_Priority += intersectPos5.m_Priority;
							target[target.Length - 1] = value7;
						}
						for (int n = bestIndex; n < maxIndex; n++)
						{
							source[n] = source[n + 1];
						}
						maxIndex--;
					}
					for (int num16 = minIndex; num16 <= maxIndex; num16++)
					{
						target.Add(source[num16]);
					}
				}
				else
				{
					target.Add(source[m]);
				}
			}
			for (int num17 = 1; num17 + 1 < target.Length; num17++)
			{
				IntersectPos value8 = target[num17];
				if (!value8.m_IsOptional)
				{
					continue;
				}
				IntersectPos value9 = target[num17 - 1];
				value8.m_CanMove.min = math.min(0f, math.max(value8.m_CanMove.min, value8.m_Priority - prefabGeometryData.m_ElevatedLength * 0.95f));
				value8.m_CanMove.max = math.max(0f, math.min(value8.m_CanMove.max, prefabGeometryData.m_ElevatedLength * 0.95f - value9.m_Priority));
				float num18 = MathUtils.Clamp((value8.m_Priority - value9.m_Priority) * 0.5f, value8.m_CanMove);
				if (num18 != 0f)
				{
					if (num18 > 0f)
					{
						Bounds1 t5 = new Bounds1(value8.m_CoursePos.m_CourseDelta, 1f);
						MathUtils.ClampLength(courseData.m_Curve.xz, ref t5, num18);
						value8.m_CoursePos.m_CourseDelta = t5.max;
						value8.m_CanMove.max = math.max(0f, value8.m_CanMove.max - num18);
					}
					else
					{
						Bounds1 t6 = new Bounds1(0f, value8.m_CoursePos.m_CourseDelta);
						MathUtils.ClampLengthInverse(courseData.m_Curve.xz, ref t6, 0f - num18);
						value8.m_CoursePos.m_CourseDelta = t6.min;
						value8.m_CanMove.min = math.min(0f, value8.m_CanMove.min - num18);
					}
					value8.m_Priority -= num18;
					value8.m_CoursePos.m_Position = MathUtils.Position(courseData.m_Curve, value8.m_CoursePos.m_CourseDelta);
					value8.m_CoursePos.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, value8.m_CoursePos.m_CourseDelta));
					target[num17] = value8;
					value9.m_Priority += num18;
					target[num17 - 1] = value9;
				}
			}
		}

		private bool FindOptionalRange(NativeList<IntersectPos> source, int index, out int minIndex, out int maxIndex)
		{
			minIndex = index;
			maxIndex = index - 1;
			if (index == 0)
			{
				return false;
			}
			for (int i = minIndex; i + 1 < source.Length; i++)
			{
				if (source[i].m_IsOptional)
				{
					maxIndex = i;
					continue;
				}
				return maxIndex >= minIndex;
			}
			return maxIndex >= minIndex;
		}

		private bool FindBestIntersectionToRemove(NetGeometryData netGeometryData, NativeList<IntersectPos> source, int minIndex, int maxIndex, out int bestIndex)
		{
			bestIndex = minIndex;
			float num = netGeometryData.m_ElevatedLength;
			for (int i = minIndex; i <= maxIndex; i++)
			{
				IntersectPos intersectPos = source[i - 1];
				IntersectPos intersectPos2 = source[i];
				float num2 = intersectPos.m_Priority + intersectPos2.m_Priority;
				if (num2 < num)
				{
					bestIndex = i;
					num = num2;
				}
			}
			return num <= netGeometryData.m_ElevatedLength * 0.95f;
		}

		private void UpdateCourses(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NativeList<IntersectPos> intersectionList, NativeList<AuxIntersectionEntity> auxIntersectionEntities, ref CourseHeightData courseHeightData)
		{
			NetData netData = m_PrefabNetData[creationDefinition.m_Prefab];
			NetGeometryData netGeometryData = m_PrefabGeometryData[creationDefinition.m_Prefab];
			m_PrefabPlaceableData.TryGetComponent(creationDefinition.m_Prefab, out var componentData);
			m_PrefabFixedNetElements.TryGetBuffer(creationDefinition.m_Prefab, out var bufferData);
			m_PrefabAuxiliaryNets.TryGetBuffer(creationDefinition.m_Prefab, out var bufferData2);
			int courseIndex = 0;
			if (intersectionList.Length != 0)
			{
				courseData.m_StartPosition = intersectionList[0].m_CoursePos;
				int2 auxIndex = new int2(intersectionList[0].m_AuxIndex, -1);
				int num = 0;
				for (int i = 1; i < intersectionList.Length; i++)
				{
					courseData.m_EndPosition = intersectionList[i].m_CoursePos;
					auxIndex.y = intersectionList[i].m_AuxIndex;
					if (courseData.m_EndPosition.m_Entity == Entity.Null || courseData.m_EndPosition.m_Entity != courseData.m_StartPosition.m_Entity)
					{
						TryAddCourse(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, componentData, bufferData, bufferData2, auxIntersectionEntities, auxIndex, ref courseHeightData, ref courseIndex);
						num++;
					}
					courseData.m_StartPosition = courseData.m_EndPosition;
					auxIndex.x = auxIndex.y;
				}
				if (num == 0)
				{
					courseData.m_StartPosition = intersectionList[0].m_CoursePos;
					courseData.m_EndPosition = intersectionList[intersectionList.Length - 1].m_CoursePos;
					TryAddCourse(auxIndex: new int2(intersectionList[0].m_AuxIndex, intersectionList[intersectionList.Length - 1].m_AuxIndex), courseData: courseData, creationDefinition: creationDefinition, ownerDefinition: ownerDefinition, upgraded: upgraded, courseEntity: courseEntity, jobIndex: jobIndex, netData: netData, netGeometryData: netGeometryData, placeableNetData: componentData, fixedNetElements: bufferData, auxiliaryNets: bufferData2, auxIntersectionEntities: auxIntersectionEntities, courseHeightData: ref courseHeightData, courseIndex: ref courseIndex);
				}
			}
			if (courseIndex == 0)
			{
				m_CommandBuffer.DestroyEntity(jobIndex, courseEntity);
			}
		}

		private void TryAddCourse(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NetData netData, NetGeometryData netGeometryData, PlaceableNetData placeableNetData, DynamicBuffer<FixedNetElement> fixedNetElements, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, int2 auxIndex, ref CourseHeightData courseHeightData, ref int courseIndex)
		{
			if (creationDefinition.m_Original != Entity.Null || creationDefinition.m_Prefab == Entity.Null)
			{
				courseHeightData.SampleCourseHeight(ref courseData, netGeometryData);
				AddCourse(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, auxiliaryNets, auxIntersectionEntities, auxIndex, ref courseIndex);
				return;
			}
			float2 value = MathUtils.Tangent(courseData.m_Curve, courseData.m_StartPosition.m_CourseDelta).xz;
			float2 value2 = MathUtils.Tangent(courseData.m_Curve, courseData.m_EndPosition.m_CourseDelta).xz;
			if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
			{
				courseHeightData.SampleCourseHeight(ref courseData, netGeometryData);
				AddCourse(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, auxiliaryNets, auxIntersectionEntities, auxIndex, ref courseIndex);
			}
			else if (math.dot(value, value2) < -0.001f && (netGeometryData.m_Flags & Game.Net.GeometryFlags.NoCurveSplit) == 0)
			{
				float num = NetUtils.FindMiddleTangentPos(courseData.m_Curve.xz, new float2(courseData.m_StartPosition.m_CourseDelta, courseData.m_EndPosition.m_CourseDelta));
				CoursePos coursePos = new CoursePos
				{
					m_CourseDelta = num,
					m_Position = MathUtils.Position(courseData.m_Curve, num),
					m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(courseData.m_Curve, num)),
					m_Flags = (courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.IsGrid))
				};
				coursePos.m_Flags |= CoursePosFlags.FreeHeight;
				coursePos.m_ParentMesh = math.select(courseData.m_StartPosition.m_ParentMesh, -1, courseData.m_StartPosition.m_ParentMesh != courseData.m_EndPosition.m_ParentMesh);
				NetCourse courseData2 = courseData;
				NetCourse courseData3 = courseData;
				courseData2.m_EndPosition = coursePos;
				courseData3.m_StartPosition = coursePos;
				courseData2.m_Length = MathUtils.Length(courseData2.m_Curve, new Bounds1(courseData2.m_StartPosition.m_CourseDelta, courseData2.m_EndPosition.m_CourseDelta));
				courseData3.m_Length = MathUtils.Length(courseData3.m_Curve, new Bounds1(courseData3.m_StartPosition.m_CourseDelta, courseData3.m_EndPosition.m_CourseDelta));
				TryAddCoursePhase2(courseData2, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, fixedNetElements, auxiliaryNets, auxIntersectionEntities, new int2(auxIndex.x, -1), ref courseHeightData, ref courseIndex);
				TryAddCoursePhase2(courseData3, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, fixedNetElements, auxiliaryNets, auxIntersectionEntities, new int2(-1, auxIndex.y), ref courseHeightData, ref courseIndex);
			}
			else
			{
				TryAddCoursePhase2(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, fixedNetElements, auxiliaryNets, auxIntersectionEntities, auxIndex, ref courseHeightData, ref courseIndex);
			}
		}

		private void TryAddCoursePhase2(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NetData netData, NetGeometryData netGeometryData, PlaceableNetData placeableNetData, DynamicBuffer<FixedNetElement> fixedNetElements, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, int2 auxIndex, ref CourseHeightData courseHeightData, ref int courseIndex)
		{
			float num = MathUtils.Length(courseData.m_Curve.xz, new Bounds1(courseData.m_StartPosition.m_CourseDelta, courseData.m_EndPosition.m_CourseDelta));
			if (fixedNetElements.IsCreated)
			{
				float2 y = new float2(0f, 0f);
				float num2 = 0f;
				for (int i = 0; i < fixedNetElements.Length; i++)
				{
					FixedNetElement fixedNetElement = fixedNetElements[i];
					float2 @float = new float2(fixedNetElement.m_LengthRange.min, fixedNetElement.m_LengthRange.max);
					int2 @int = math.select(fixedNetElement.m_CountRange, new int2(0, 10000), fixedNetElement.m_CountRange == 0);
					float2 float2 = @float * @int;
					float num3 = math.select(0f, float2.y - float2.x, fixedNetElement.m_LengthRange.max != fixedNetElement.m_LengthRange.min);
					y += float2;
					num2 += num3;
				}
				float2 float3 = (num - 0.16f) / math.max(1f, y);
				int num4;
				if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.NoCurveSplit) != 0)
				{
					num4 = Mathf.CeilToInt(float3.y);
				}
				else
				{
					float2 x = math.normalizesafe(MathUtils.Tangent(courseData.m_Curve, courseData.m_StartPosition.m_CourseDelta).xz);
					float2 y2 = math.normalizesafe(MathUtils.Tangent(courseData.m_Curve, courseData.m_EndPosition.m_CourseDelta).xz);
					float num5 = math.acos(math.clamp(math.dot(x, y2), -1f, 1f));
					float num6 = math.ceil(float3.y);
					float end = math.max(num6, math.floor(float3.x));
					num4 = Mathf.RoundToInt(math.lerp(num6, end, math.saturate(num5 * (2f / MathF.PI))));
				}
				NetCourse netCourse = courseData;
				int2 trueValue = auxIndex;
				for (int j = 1; j <= num4; j++)
				{
					if (j == num4)
					{
						netCourse.m_EndPosition = courseData.m_EndPosition;
						trueValue.y = auxIndex.y;
					}
					else
					{
						netCourse.m_EndPosition = CutCourse(courseData, netGeometryData, num * (float)j / (float)num4);
						trueValue.y = -1;
					}
					NetCourse course = netCourse;
					if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StraightEdges) != 0)
					{
						courseHeightData.SampleCourseHeight(ref course, netGeometryData);
					}
					else
					{
						course.m_Length = MathUtils.Length(course.m_Curve.xz, new Bounds1(course.m_StartPosition.m_CourseDelta, course.m_EndPosition.m_CourseDelta));
					}
					float num7 = course.m_Length - y.x;
					float num8 = math.max(0f, num7);
					float num9 = 0f;
					num7 -= num8;
					NativeArray<NetCourse> nativeArray = new NativeArray<NetCourse>(fixedNetElements.Length, Allocator.Temp);
					NetCourse value = course;
					for (int k = 0; k < fixedNetElements.Length; k++)
					{
						FixedNetElement fixedNetElement2 = fixedNetElements[k];
						float2 float4 = new float2(fixedNetElement2.m_LengthRange.min, fixedNetElement2.m_LengthRange.max);
						int2 int2 = math.select(fixedNetElement2.m_CountRange, new int2(0, 10000), fixedNetElement2.m_CountRange == 0);
						float2 float5 = float4 * int2;
						float num10 = math.select(0f, float5.y - float5.x, fixedNetElement2.m_LengthRange.max != fixedNetElement2.m_LengthRange.min);
						value.m_Length = float5.x + num10 * num8 / math.max(1f, num2);
						value.m_Length += float5.x * num7 / math.max(1f, y.x);
						value.m_FixedIndex = k;
						if (k == fixedNetElements.Length - 1)
						{
							value.m_EndPosition = course.m_EndPosition;
						}
						else
						{
							value.m_EndPosition = CutCourse(course, netGeometryData, num9 + value.m_Length);
							value.m_EndPosition.m_Flags |= CoursePosFlags.IsFixed;
							num9 += value.m_Length;
						}
						nativeArray[k] = value;
						value.m_StartPosition = value.m_EndPosition;
					}
					if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StraightEdges) == 0)
					{
						int num11 = 0;
						int num12 = fixedNetElements.Length - 1;
						float3 position = course.m_StartPosition.m_Position;
						float3 position2 = course.m_EndPosition.m_Position;
						float3 value2 = MathUtils.Tangent(course.m_Curve, course.m_StartPosition.m_CourseDelta);
						float3 value3 = MathUtils.Tangent(course.m_Curve, course.m_EndPosition.m_CourseDelta);
						value2 = MathUtils.Normalize(value2, value2.xz);
						value3 = MathUtils.Normalize(value3, value3.xz);
						for (int l = 0; l < fixedNetElements.Length && (fixedNetElements[l].m_Flags & FixedNetFlags.Straight) != 0; l++)
						{
							value = nativeArray[l];
							value.m_Curve.xz = NetUtils.StraightCurve(position, position + value2 * value.m_Length).xz;
							value.m_StartPosition.m_CourseDelta = 0f;
							value.m_StartPosition.m_Position = value.m_Curve.a;
							value.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(value2);
							value.m_EndPosition.m_CourseDelta = 1f;
							value.m_EndPosition.m_Position = value.m_Curve.d;
							value.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(value2);
							nativeArray[l] = value;
							position = value.m_EndPosition.m_Position;
							num11++;
						}
						int num13 = fixedNetElements.Length - 1;
						while (num13 >= 0 && (fixedNetElements[num13].m_Flags & FixedNetFlags.Straight) != 0)
						{
							value = nativeArray[num13];
							value.m_Curve.xz = NetUtils.StraightCurve(position2 - value3 * value.m_Length, position2 + value2).xz;
							value.m_StartPosition.m_CourseDelta = 0f;
							value.m_StartPosition.m_Position = value.m_Curve.a;
							value.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(-value3);
							value.m_EndPosition.m_CourseDelta = 1f;
							value.m_EndPosition.m_Position = value.m_Curve.d;
							value.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(-value3);
							nativeArray[num13] = value;
							position2 = value.m_StartPosition.m_Position;
							num12--;
							num13--;
						}
						if (num12 >= num11)
						{
							Bezier4x3 curve = NetUtils.FitCurve(position, value2, value3, position2);
							float courseDelta = nativeArray[num11].m_StartPosition.m_CourseDelta;
							float courseDelta2 = nativeArray[num12].m_EndPosition.m_CourseDelta;
							for (int m = num11; m <= num12; m++)
							{
								value = nativeArray[m];
								float2 float6 = new float2(value.m_StartPosition.m_CourseDelta, value.m_EndPosition.m_CourseDelta);
								float6 = (float6 - courseDelta) / math.max(0.001f, courseDelta2 - courseDelta);
								value.m_Curve.xz = MathUtils.Cut(curve, float6).xz;
								value.m_StartPosition.m_CourseDelta = 0f;
								value.m_StartPosition.m_Position = value.m_Curve.a;
								value.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(value.m_Curve));
								value.m_EndPosition.m_CourseDelta = 1f;
								value.m_EndPosition.m_Position = value.m_Curve.d;
								value.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(value.m_Curve));
								nativeArray[m] = value;
							}
						}
					}
					for (int n = 0; n < fixedNetElements.Length; n++)
					{
						value = nativeArray[n];
						FixedNetElement fixedNetElement3 = fixedNetElements[n];
						float2 float7 = new float2(fixedNetElement3.m_LengthRange.min, fixedNetElement3.m_LengthRange.max);
						int2 int3 = math.select(fixedNetElement3.m_CountRange, new int2(0, 10000), fixedNetElement3.m_CountRange == 0);
						int valueToClamp = (int)math.ceil((value.m_Length - 0.16f) / float7.y);
						valueToClamp = math.clamp(valueToClamp, int3.x, int3.y);
						NetCourse netCourse2 = value;
						for (int num14 = 1; num14 <= valueToClamp; num14++)
						{
							if (num14 == valueToClamp)
							{
								netCourse2.m_EndPosition = value.m_EndPosition;
							}
							else
							{
								netCourse2.m_EndPosition = CutCourse(value, netGeometryData, value.m_Length * (float)num14 / (float)valueToClamp);
								netCourse2.m_EndPosition.m_Flags |= CoursePosFlags.IsFixed;
							}
							int2 auxIndex2 = math.select(-1, trueValue, new bool2(n == 0 && num14 == 1, n == fixedNetElements.Length - 1 && num14 == valueToClamp));
							if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.StraightEdges) == 0)
							{
								NetCourse course2 = netCourse2;
								courseHeightData.SampleCourseHeight(ref course2, netGeometryData);
								AddCourse(course2, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, auxiliaryNets, auxIntersectionEntities, auxIndex2, ref courseIndex);
							}
							else
							{
								netCourse2.m_Length = MathUtils.Length(netCourse2.m_Curve.xz, new Bounds1(netCourse2.m_StartPosition.m_CourseDelta, netCourse2.m_EndPosition.m_CourseDelta));
								AddCourse(netCourse2, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, auxiliaryNets, auxIntersectionEntities, auxIndex2, ref courseIndex);
							}
							netCourse2.m_StartPosition = netCourse2.m_EndPosition;
						}
					}
					nativeArray.Dispose();
					netCourse.m_StartPosition = netCourse.m_EndPosition;
					trueValue.x = trueValue.y;
				}
				return;
			}
			NetCourse course3 = courseData;
			courseHeightData.SampleCourseHeight(ref course3, netGeometryData);
			CalculateElevation(creationDefinition, ownerDefinition, ref course3, ref upgraded, netGeometryData, placeableNetData);
			float num15 = netGeometryData.m_EdgeLengthRange.max;
			if ((creationDefinition.m_Flags & CreationFlags.SubElevation) != 0)
			{
				CompositionFlags elevationFlags = NetCompositionHelpers.GetElevationFlags(new Game.Net.Elevation(course3.m_StartPosition.m_Elevation), new Game.Net.Elevation(course3.m_Elevation), new Game.Net.Elevation(course3.m_EndPosition.m_Elevation), netGeometryData);
				num15 = math.select(num15, netGeometryData.m_ElevatedLength, (elevationFlags.m_General & CompositionFlags.General.Elevated) != 0);
			}
			int num16 = (int)math.ceil((num - 0.16f) / num15);
			if (num16 > 1)
			{
				course3 = courseData;
				int2 auxIndex3 = auxIndex;
				for (int num17 = 1; num17 <= num16; num17++)
				{
					if (num17 == num16)
					{
						course3.m_EndPosition = courseData.m_EndPosition;
						auxIndex3.y = auxIndex.y;
					}
					else
					{
						course3.m_EndPosition = CutCourse(courseData, netGeometryData, num * (float)num17 / (float)num16);
						auxIndex3.y = -1;
					}
					NetCourse course4 = course3;
					courseHeightData.SampleCourseHeight(ref course4, netGeometryData);
					AddCourse(course4, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, placeableNetData, auxiliaryNets, auxIntersectionEntities, auxIndex3, ref courseIndex);
					course3.m_StartPosition = course3.m_EndPosition;
					auxIndex3.x = auxIndex3.y;
				}
			}
			else
			{
				AddCourse(course3, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, auxiliaryNets, auxIntersectionEntities, auxIndex, ref courseIndex);
			}
		}

		private CoursePos CutCourse(NetCourse course, NetGeometryData netGeometryData, float cutLength)
		{
			CoursePos result = default(CoursePos);
			if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != 0)
			{
				cutLength = MathUtils.Snap(cutLength + 0.16f, 4f);
				if (((int)math.round(cutLength / 4f) & 1) != 0 != ((course.m_StartPosition.m_Flags & CoursePosFlags.HalfAlign) != 0))
				{
					result.m_Flags |= CoursePosFlags.HalfAlign;
				}
			}
			Bounds1 t = new Bounds1(course.m_StartPosition.m_CourseDelta, 1f);
			MathUtils.ClampLength(course.m_Curve.xz, ref t, cutLength);
			result.m_CourseDelta = t.max;
			result.m_Position = MathUtils.Position(course.m_Curve, result.m_CourseDelta);
			result.m_Rotation = NetUtils.GetNodeRotation(MathUtils.Tangent(course.m_Curve, result.m_CourseDelta));
			result.m_Flags |= course.m_StartPosition.m_Flags & (CoursePosFlags.IsParallel | CoursePosFlags.IsRight | CoursePosFlags.IsLeft | CoursePosFlags.ForceElevatedEdge | CoursePosFlags.IsGrid);
			result.m_Flags |= CoursePosFlags.FreeHeight;
			result.m_ParentMesh = math.select(course.m_StartPosition.m_ParentMesh, -1, course.m_StartPosition.m_ParentMesh != course.m_EndPosition.m_ParentMesh);
			if ((course.m_StartPosition.m_Flags & CoursePosFlags.ForceElevatedEdge) != 0)
			{
				result.m_Flags |= CoursePosFlags.ForceElevatedNode;
			}
			return result;
		}

		private float2 CalculateElevation(CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, NetGeometryData netGeometryData, PlaceableNetData placeableNetData, Bezier4x3 curve, float delta, float offset, bool serviceUpgrade)
		{
			float3 @float = MathUtils.Position(curve, delta);
			bool flag = (netGeometryData.m_Flags & Game.Net.GeometryFlags.SubOwner) == 0 && !serviceUpgrade;
			if ((netGeometryData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
			{
				return 0f;
			}
			if (flag && ownerDefinition.m_Prefab != Entity.Null)
			{
				return @float.y - ownerDefinition.m_Position.y;
			}
			if (flag && creationDefinition.m_Owner != Entity.Null && m_TransformData.TryGetComponent(creationDefinition.m_Owner, out var componentData))
			{
				return @float.y - componentData.m_Position.y;
			}
			float3 float2 = MathUtils.Tangent(curve, delta);
			float3 float3 = new float3
			{
				xz = math.normalizesafe(MathUtils.Right(float2.xz)) * offset
			};
			float2 float4 = default(float2);
			float4.x = TerrainUtils.SampleHeight(ref m_TerrainHeightData, @float - float3);
			float4.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, @float + float3);
			float4 = @float.y - float4;
			if ((placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.ShoreLine) != Game.Net.PlacementFlags.None)
			{
				bool2 x = default(bool2);
				x.x = WaterUtils.SampleDepth(ref m_WaterSurfaceData, @float - float3) >= netGeometryData.m_ElevationLimit;
				x.y = WaterUtils.SampleDepth(ref m_WaterSurfaceData, @float + float3) >= netGeometryData.m_ElevationLimit;
				if (math.all(x))
				{
					float4 = math.max(netGeometryData.m_ElevationLimit, float4);
				}
				else if (x.x || (float4.x - float4.y >= netGeometryData.m_ElevationLimit * 0.1f && !x.y))
				{
					float4.x = math.max(netGeometryData.m_ElevationLimit, float4.x);
					float4.y = math.select(0f, float4.y, float4.y >= netGeometryData.m_ElevationLimit * 3f);
				}
				else if (x.y || (float4.y - float4.x >= netGeometryData.m_ElevationLimit * 0.1f && !x.x))
				{
					float4.x = math.select(0f, float4.x, float4.x >= netGeometryData.m_ElevationLimit * 3f);
					float4.y = math.max(netGeometryData.m_ElevationLimit, float4.y);
				}
			}
			return float4;
		}

		private void CalculateElevation(CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, ref NetCourse courseData, ref Upgraded upgraded, NetGeometryData netGeometryData, PlaceableNetData placeableNetData)
		{
			float delta = math.lerp(courseData.m_StartPosition.m_CourseDelta, courseData.m_EndPosition.m_CourseDelta, 0.5f);
			float offset = netGeometryData.m_DefaultWidth * 0.5f;
			bool serviceUpgrade = !m_EditorMode && (creationDefinition.m_Flags & CreationFlags.SubElevation) != 0 && m_PrefabServiceUpgradeData.HasComponent(creationDefinition.m_Prefab);
			float2 @float = CalculateElevation(creationDefinition, ownerDefinition, netGeometryData, placeableNetData, courseData.m_Curve, courseData.m_StartPosition.m_CourseDelta, offset, serviceUpgrade);
			float2 float2 = CalculateElevation(creationDefinition, ownerDefinition, netGeometryData, placeableNetData, courseData.m_Curve, delta, offset, serviceUpgrade);
			float2 float3 = CalculateElevation(creationDefinition, ownerDefinition, netGeometryData, placeableNetData, courseData.m_Curve, courseData.m_EndPosition.m_CourseDelta, offset, serviceUpgrade);
			bool flag = (upgraded.m_Flags.m_General & CompositionFlags.General.Elevated) != 0;
			if (flag)
			{
				courseData.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
				courseData.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedEdge;
				upgraded.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
				float num = MathUtils.Max(courseData.m_Curve.y);
				if (courseData.m_Curve.a.y > num - 0.1f)
				{
					courseData.m_StartPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
				if (courseData.m_Curve.d.y > num - 0.1f)
				{
					courseData.m_EndPosition.m_Flags |= CoursePosFlags.ForceElevatedNode;
				}
			}
			@float = math.select(@float, netGeometryData.m_ElevationLimit * 2f, ((courseData.m_StartPosition.m_Flags & CoursePosFlags.ForceElevatedNode) != 0) & (@float < netGeometryData.m_ElevationLimit * 2f));
			float2 = math.select(float2, netGeometryData.m_ElevationLimit * 2f, ((courseData.m_StartPosition.m_Flags & courseData.m_EndPosition.m_Flags & CoursePosFlags.ForceElevatedEdge) != 0) & (float2 < netGeometryData.m_ElevationLimit * 2f));
			float3 = math.select(float3, netGeometryData.m_ElevationLimit * 2f, ((courseData.m_EndPosition.m_Flags & CoursePosFlags.ForceElevatedNode) != 0) & (float3 < netGeometryData.m_ElevationLimit * 2f));
			@float = math.select(@float, 0f, new bool2((courseData.m_StartPosition.m_Flags & CoursePosFlags.LeftTransition) != 0, (courseData.m_StartPosition.m_Flags & CoursePosFlags.RightTransition) != 0));
			float3 = math.select(float3, 0f, new bool2((courseData.m_EndPosition.m_Flags & CoursePosFlags.LeftTransition) != 0, (courseData.m_EndPosition.m_Flags & CoursePosFlags.RightTransition) != 0));
			courseData.m_StartPosition.m_Elevation = math.select(default(float2), @float, (@float >= netGeometryData.m_ElevationLimit) | (@float <= 0f - netGeometryData.m_ElevationLimit));
			courseData.m_Elevation = math.select(default(float2), float2, (float2 >= netGeometryData.m_ElevationLimit) | (float2 <= 0f - netGeometryData.m_ElevationLimit));
			courseData.m_EndPosition.m_Elevation = math.select(default(float2), float3, (float3 >= netGeometryData.m_ElevationLimit) | (float3 <= 0f - netGeometryData.m_ElevationLimit));
			if ((creationDefinition.m_Owner != Entity.Null || ownerDefinition.m_Prefab != Entity.Null) && !flag && (creationDefinition.m_Flags & CreationFlags.SubElevation) == 0)
			{
				if (courseData.m_StartPosition.m_ParentMesh < 0)
				{
					courseData.m_StartPosition.m_Elevation = default(float2);
				}
				if (courseData.m_StartPosition.m_ParentMesh < 0 && courseData.m_EndPosition.m_ParentMesh < 0)
				{
					courseData.m_Elevation = default(float2);
				}
				if (courseData.m_EndPosition.m_ParentMesh < 0)
				{
					courseData.m_EndPosition.m_Elevation = default(float2);
				}
			}
			LimitElevation(ref courseData.m_StartPosition.m_Elevation, placeableNetData);
			LimitElevation(ref courseData.m_Elevation, placeableNetData);
			LimitElevation(ref courseData.m_EndPosition.m_Elevation, placeableNetData);
		}

		private void AddCourse(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NetData netData, NetGeometryData netGeometryData, PlaceableNetData placeableNetData, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, int2 auxIndex, ref int courseIndex)
		{
			CalculateElevation(creationDefinition, ownerDefinition, ref courseData, ref upgraded, netGeometryData, placeableNetData);
			AddCourse(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, netGeometryData, auxiliaryNets, auxIntersectionEntities, auxIndex, ref courseIndex);
		}

		private void AddCourse(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NetData netData, NetGeometryData netGeometryData, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, int2 auxIndex, ref int courseIndex)
		{
			AddCourse(courseData, creationDefinition, ownerDefinition, upgraded, courseEntity, jobIndex, netData, ref courseIndex);
			if (!auxiliaryNets.IsCreated)
			{
				return;
			}
			OwnerDefinition ownerDefinition2 = new OwnerDefinition
			{
				m_Prefab = creationDefinition.m_Prefab,
				m_Position = MathUtils.Position(courseData.m_Curve, courseData.m_StartPosition.m_CourseDelta),
				m_Rotation = new float4(MathUtils.Position(courseData.m_Curve, courseData.m_EndPosition.m_CourseDelta), 0f)
			};
			for (int i = 0; i < auxiliaryNets.Length; i++)
			{
				AuxiliaryNet auxiliaryNet = auxiliaryNets[i];
				NetData netData2 = m_PrefabNetData[auxiliaryNet.m_Prefab];
				NetGeometryData netGeometryData2 = m_PrefabGeometryData[auxiliaryNet.m_Prefab];
				m_PrefabPlaceableData.TryGetComponent(auxiliaryNet.m_Prefab, out var componentData);
				bool flag = NetUtils.ShouldInvert(auxiliaryNet.m_InvertMode, m_LefthandTraffic);
				CreationDefinition auxDefinition = GetAuxDefinition(creationDefinition, auxiliaryNet);
				NetCourse courseData2 = courseData;
				if (GetAuxCourse(ref courseData2, auxiliaryNet, flag))
				{
					int2 @int = math.select(auxIndex, auxIndex.yx, flag);
					FixAuxCoursePos(ref courseData2.m_StartPosition, auxiliaryNets, auxIntersectionEntities, @int.x, i);
					FixAuxCoursePos(ref courseData2.m_EndPosition, auxiliaryNets, auxIntersectionEntities, @int.y, i);
					Upgraded upgraded2 = upgraded;
					if (flag)
					{
						upgraded2.m_Flags = NetCompositionHelpers.InvertCompositionFlags(upgraded2.m_Flags);
					}
					CalculateElevation(creationDefinition, ownerDefinition, ref courseData2, ref upgraded2, netGeometryData2, componentData);
					AddCourse(courseData2, auxDefinition, ownerDefinition2, upgraded2, courseEntity, jobIndex, netData2, ref courseIndex);
				}
			}
		}

		private void FixAuxCoursePos(ref CoursePos coursePos, DynamicBuffer<AuxiliaryNet> auxiliaryNets, NativeList<AuxIntersectionEntity> auxIntersectionEntities, int intersectionAuxIndex, int auxNetIndex)
		{
			int num = intersectionAuxIndex * auxiliaryNets.Length + auxNetIndex;
			if (intersectionAuxIndex != -1 && num < auxIntersectionEntities.Length)
			{
				AuxIntersectionEntity auxIntersectionEntity = auxIntersectionEntities[num];
				coursePos.m_Entity = auxIntersectionEntity.m_Entity;
				coursePos.m_SplitPosition = auxIntersectionEntity.m_SplitPosition;
				coursePos.m_Flags &= ~CoursePosFlags.ForceElevatedNode;
			}
			else
			{
				coursePos.m_Entity = Entity.Null;
				coursePos.m_SplitPosition = 0f;
			}
		}

		private bool IgnoreOverlappingEdge(CreationDefinition creationDefinition, NetCourse courseData)
		{
			if (creationDefinition.m_Original != Entity.Null)
			{
				return false;
			}
			if (courseData.m_StartPosition.m_Entity == Entity.Null)
			{
				return false;
			}
			if (courseData.m_EndPosition.m_Entity == Entity.Null)
			{
				return false;
			}
			if (((courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsGrid)) == CoursePosFlags.IsFirst && (courseData.m_EndPosition.m_Flags & (CoursePosFlags.IsLast | CoursePosFlags.IsGrid)) == CoursePosFlags.IsLast) || ((courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsLast | CoursePosFlags.IsGrid)) == CoursePosFlags.IsLast && (courseData.m_EndPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsGrid)) == CoursePosFlags.IsFirst))
			{
				return false;
			}
			if (courseData.m_EndPosition.m_Entity == courseData.m_StartPosition.m_Entity)
			{
				return true;
			}
			if (m_EdgeData.TryGetComponent(courseData.m_StartPosition.m_Entity, out var componentData) && (courseData.m_EndPosition.m_Entity == componentData.m_Start || courseData.m_EndPosition.m_Entity == componentData.m_End))
			{
				return true;
			}
			if (m_EdgeData.TryGetComponent(courseData.m_EndPosition.m_Entity, out var componentData2) && (courseData.m_StartPosition.m_Entity == componentData2.m_Start || courseData.m_StartPosition.m_Entity == componentData2.m_End))
			{
				return true;
			}
			return false;
		}

		private void AddCourse(NetCourse courseData, CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Upgraded upgraded, Entity courseEntity, int jobIndex, NetData netData, ref int courseIndex)
		{
			creationDefinition.m_RandomSeed += courseIndex;
			FindOriginalEdge(ref creationDefinition, ownerDefinition, netData, courseData);
			if (IgnoreOverlappingEdge(creationDefinition, courseData))
			{
				courseData.m_StartPosition.m_Flags |= CoursePosFlags.DontCreate;
				courseData.m_EndPosition.m_Flags |= CoursePosFlags.DontCreate;
			}
			bool flag = m_LocalCurveCacheData.HasComponent(courseEntity);
			LocalCurveCache component = default(LocalCurveCache);
			if (flag)
			{
				Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(new Game.Objects.Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation));
				component.m_Curve.a = ObjectUtils.WorldToLocal(inverseParentTransform, courseData.m_Curve.a);
				component.m_Curve.b = ObjectUtils.WorldToLocal(inverseParentTransform, courseData.m_Curve.b);
				component.m_Curve.c = ObjectUtils.WorldToLocal(inverseParentTransform, courseData.m_Curve.c);
				component.m_Curve.d = ObjectUtils.WorldToLocal(inverseParentTransform, courseData.m_Curve.d);
			}
			if (courseIndex++ == 0)
			{
				m_CommandBuffer.SetComponent(jobIndex, courseEntity, creationDefinition);
				m_CommandBuffer.SetComponent(jobIndex, courseEntity, courseData);
				if (flag)
				{
					m_CommandBuffer.SetComponent(jobIndex, courseEntity, component);
				}
				if (upgraded.m_Flags != default(CompositionFlags))
				{
					m_CommandBuffer.SetComponent(jobIndex, courseEntity, upgraded);
				}
				else
				{
					m_CommandBuffer.RemoveComponent<Upgraded>(jobIndex, courseEntity);
				}
				return;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex);
			m_CommandBuffer.AddComponent(jobIndex, e, creationDefinition);
			m_CommandBuffer.AddComponent(jobIndex, e, courseData);
			m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
			}
			if (flag)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, component);
			}
			if (upgraded.m_Flags != default(CompositionFlags))
			{
				m_CommandBuffer.AddComponent(jobIndex, e, upgraded);
			}
		}

		private void LimitElevation(ref float2 elevation, PlaceableNetData placeableNetData)
		{
			elevation = math.select(elevation, placeableNetData.m_ElevationRange.min, (elevation < placeableNetData.m_ElevationRange.min) & (placeableNetData.m_ElevationRange.min >= 0f));
			elevation = math.select(elevation, placeableNetData.m_ElevationRange.max, (elevation > placeableNetData.m_ElevationRange.max) & (placeableNetData.m_ElevationRange.max < 0f));
		}

		private bool MatchingOwner(CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, Entity entity)
		{
			if (m_OwnerData.TryGetComponent(entity, out var componentData))
			{
				if (creationDefinition.m_Owner != componentData.m_Owner && (!m_TransformData.TryGetComponent(componentData.m_Owner, out var componentData2) || !m_PrefabRefData.TryGetComponent(componentData.m_Owner, out var componentData3) || !ownerDefinition.m_Position.Equals(componentData2.m_Position) || !ownerDefinition.m_Rotation.Equals(componentData2.m_Rotation) || ownerDefinition.m_Prefab != componentData3.m_Prefab))
				{
					return false;
				}
			}
			else if (ownerDefinition.m_Prefab != Entity.Null)
			{
				return false;
			}
			return true;
		}

		private void FindOriginalEdge(ref CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, NetData netData, NetCourse netCourse)
		{
			if (creationDefinition.m_Original != Entity.Null || (creationDefinition.m_Flags & CreationFlags.Permanent) != 0 || !m_ConnectedEdges.HasBuffer(netCourse.m_StartPosition.m_Entity) || !m_ConnectedEdges.HasBuffer(netCourse.m_EndPosition.m_Entity))
			{
				return;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[netCourse.m_StartPosition.m_Entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				if (m_DeletedEntities.ContainsKey(edge))
				{
					continue;
				}
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == netCourse.m_StartPosition.m_Entity && edge2.m_End == netCourse.m_EndPosition.m_Entity)
				{
					if (CanReplace(netData, edge))
					{
						creationDefinition.m_Original = edge;
					}
					break;
				}
				if (edge2.m_Start == netCourse.m_EndPosition.m_Entity && edge2.m_End == netCourse.m_StartPosition.m_Entity)
				{
					if (CanReplace(netData, edge))
					{
						creationDefinition.m_Original = edge;
						creationDefinition.m_Flags |= CreationFlags.Invert;
					}
					break;
				}
			}
			if (creationDefinition.m_Original != Entity.Null && !MatchingOwner(creationDefinition, ownerDefinition, creationDefinition.m_Original))
			{
				creationDefinition.m_Original = Entity.Null;
				creationDefinition.m_Flags &= ~CreationFlags.Invert;
			}
		}

		private bool CanReplace(NetData netData, Entity original)
		{
			PrefabRef prefabRef = m_PrefabRefData[original];
			NetData netData2 = m_PrefabNetData[prefabRef.m_Prefab];
			return (netData.m_RequiredLayers & netData2.m_RequiredLayers) != 0;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> __Game_Prefabs_AuxiliaryNet_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalCurveCache> __Game_Tools_LocalCurveCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<FixedNetElement> __Game_Prefabs_FixedNetElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Prefabs_AuxiliaryNet_RO_BufferLookup = state.GetBufferLookup<AuxiliaryNet>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Tools_LocalCurveCache_RO_ComponentLookup = state.GetComponentLookup<LocalCurveCache>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_FixedNetElement_RO_BufferLookup = state.GetBufferLookup<FixedNetElement>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private ToolReadyBarrier m_ToolReadyBarrier;

	private Game.Net.SearchSystem m_SearchSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_CourseQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ToolReadyBarrier = base.World.GetOrCreateSystemManaged<ToolReadyBarrier>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_CourseQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<NetCourse>(), ComponentType.ReadOnly<Updated>());
		RequireForUpdate(m_CourseQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeHashMap<Entity, bool> deletedEntities = new NativeHashMap<Entity, bool>(100, Allocator.TempJob);
		NativeList<Course> nativeList = new NativeList<Course>(Allocator.TempJob);
		NativeQueue<Overlap> overlapQueue = new NativeQueue<Overlap>(Allocator.TempJob);
		NativeList<Overlap> nativeList2 = new NativeList<Overlap>(Allocator.TempJob);
		NativeParallelQueue<IntersectPos> nativeParallelQueue = new NativeParallelQueue<IntersectPos>(Allocator.TempJob);
		CheckCoursesJob jobData = new CheckCoursesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedEntities = deletedEntities,
			m_CourseList = nativeList
		};
		JobHandle dependencies;
		FindOverlapsJob jobData2 = new FindOverlapsJob
		{
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AuxiliaryNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_SearchTree = m_SearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CourseList = nativeList,
			m_OverlapQueue = overlapQueue.AsParallelWriter()
		};
		DequeueOverlapsJob jobData3 = new DequeueOverlapsJob
		{
			m_OverlapQueue = overlapQueue,
			m_OverlapList = nativeList2
		};
		CheckCourseIntersectionsJob jobData4 = new CheckCourseIntersectionsJob
		{
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabAuxiliaryNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_CourseList = nativeList,
			m_OverlapList = nativeList2,
			m_DeletedEntities = deletedEntities,
			m_Results = nativeParallelQueue.AsWriter()
		};
		JobHandle deps;
		CheckCourseIntersectionResultsJob jobData5 = new CheckCourseIntersectionResultsJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalCurveCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalCurveCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFixedNetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_FixedNetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabAuxiliaryNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CourseList = nativeList,
			m_DeletedEntities = deletedEntities,
			m_IntersectionQueue = nativeParallelQueue.AsReader(),
			m_CommandBuffer = m_ToolReadyBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(JobChunkExtensions.Schedule(jobData, m_CourseQuery, base.Dependency), dependencies), jobData: jobData2, list: nativeList, innerloopBatchCount: 1);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData3, jobHandle);
		JobHandle jobHandle3 = jobData4.Schedule(nativeList2, 1, jobHandle2);
		JobHandle jobHandle4 = jobData5.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle3, deps));
		deletedEntities.Dispose(jobHandle4);
		nativeList.Dispose(jobHandle4);
		overlapQueue.Dispose(jobHandle2);
		nativeList2.Dispose(jobHandle3);
		nativeParallelQueue.Dispose(jobHandle4);
		m_SearchSystem.AddNetSearchTreeReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle4);
		m_WaterSystem.AddSurfaceReader(jobHandle4);
		m_ToolReadyBarrier.AddJobHandleForProducer(jobHandle4);
		base.Dependency = jobHandle4;
	}

	private static CreationDefinition GetAuxDefinition(CreationDefinition creationDefinition, AuxiliaryNet auxiliaryNet)
	{
		return new CreationDefinition
		{
			m_Prefab = auxiliaryNet.m_Prefab,
			m_Flags = creationDefinition.m_Flags,
			m_RandomSeed = creationDefinition.m_RandomSeed
		};
	}

	private static bool GetAuxCourse(ref NetCourse courseData, AuxiliaryNet auxiliaryNet, bool invert)
	{
		courseData.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
		courseData.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
		courseData.m_FixedIndex = -1;
		if (auxiliaryNet.m_Position.x != 0f)
		{
			if ((courseData.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) == (CoursePosFlags.IsFirst | CoursePosFlags.IsLast))
			{
				return false;
			}
			courseData.m_Curve = MathUtils.Cut(courseData.m_Curve, new Bounds1(courseData.m_StartPosition.m_CourseDelta, courseData.m_EndPosition.m_CourseDelta));
			courseData.m_Curve = NetUtils.OffsetCurveLeftSmooth(courseData.m_Curve, 0f - auxiliaryNet.m_Position.x);
			courseData.m_Length = MathUtils.Length(courseData.m_Curve);
			courseData.m_StartPosition.m_CourseDelta = 0f;
			courseData.m_EndPosition.m_CourseDelta = 1f;
			courseData.m_StartPosition.m_Position.x += auxiliaryNet.m_Position.x * 0.01f;
			courseData.m_EndPosition.m_Position.x += auxiliaryNet.m_Position.x * 0.01f;
		}
		if (auxiliaryNet.m_Position.z != 0f)
		{
			Bounds1 t = new Bounds1(courseData.m_StartPosition.m_CourseDelta, courseData.m_EndPosition.m_CourseDelta);
			float num = math.abs(auxiliaryNet.m_Position.z);
			float num2 = courseData.m_Length - num * 2f;
			if (!(num2 > math.max(0.9f, num - 0.5f)) || !MathUtils.ClampLength(courseData.m_Curve.xz, ref t, courseData.m_Length - num) || !MathUtils.ClampLengthInverse(courseData.m_Curve.xz, ref t, num2))
			{
				return false;
			}
			courseData.m_Curve = MathUtils.Cut(courseData.m_Curve, t);
			courseData.m_Length = num2;
			courseData.m_StartPosition.m_CourseDelta = 0f;
			courseData.m_EndPosition.m_CourseDelta = 1f;
		}
		if (auxiliaryNet.m_Position.y != 0f)
		{
			courseData.m_Curve.y += auxiliaryNet.m_Position.y;
			courseData.m_StartPosition.m_Position.y += auxiliaryNet.m_Position.y;
			courseData.m_EndPosition.m_Position.y += auxiliaryNet.m_Position.y;
		}
		if (invert)
		{
			courseData.m_Curve = MathUtils.Invert(courseData.m_Curve);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_Entity, ref courseData.m_EndPosition.m_Entity);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_CourseDelta, ref courseData.m_EndPosition.m_CourseDelta);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_SplitPosition, ref courseData.m_EndPosition.m_SplitPosition);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_Position, ref courseData.m_EndPosition.m_Position);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_Rotation, ref courseData.m_EndPosition.m_Rotation);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_Elevation, ref courseData.m_EndPosition.m_Elevation);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_Flags, ref courseData.m_EndPosition.m_Flags);
			CommonUtils.Swap(ref courseData.m_StartPosition.m_ParentMesh, ref courseData.m_EndPosition.m_ParentMesh);
			quaternion a = quaternion.RotateY(MathF.PI);
			courseData.m_StartPosition.m_Rotation = math.mul(a, courseData.m_StartPosition.m_Rotation);
			courseData.m_EndPosition.m_Rotation = math.mul(a, courseData.m_EndPosition.m_Rotation);
			courseData.m_StartPosition.m_CourseDelta = 1f - courseData.m_StartPosition.m_CourseDelta;
			courseData.m_EndPosition.m_CourseDelta = 1f - courseData.m_EndPosition.m_CourseDelta;
		}
		return true;
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
	public CourseSplitSystem()
	{
	}
}
