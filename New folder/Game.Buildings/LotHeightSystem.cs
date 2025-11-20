using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class LotHeightSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddUpdatedLotsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public NativeList<Entity> m_ResultList;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			m_ResultList.AddRange(nativeArray);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindUpdatedLotsJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Lot> m_LotData;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_LotData.HasComponent(entity))
				{
					m_ResultQueue.Enqueue(entity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public ComponentLookup<Lot> m_LotData;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = MathUtils.Expand(m_Bounds[index], 8f),
				m_LotData = m_LotData,
				m_ResultQueue = m_ResultQueue
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CollectLotsJob : IJob
	{
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeQueue<Entity> m_Queue1;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeQueue<Entity> m_Queue2;

		public NativeList<Entity> m_ResultList;

		public void Execute()
		{
			int num = 0;
			if (m_Queue1.IsCreated)
			{
				num += m_Queue1.Count;
			}
			if (m_Queue2.IsCreated)
			{
				num += m_Queue2.Count;
			}
			m_ResultList.Capacity = m_ResultList.Length + num;
			if (m_Queue1.IsCreated)
			{
				NativeArray<Entity> array = m_Queue1.ToArray(Allocator.Temp);
				m_ResultList.AddRange(array);
				array.Dispose();
			}
			if (m_Queue2.IsCreated)
			{
				NativeArray<Entity> array2 = m_Queue2.ToArray(Allocator.Temp);
				m_ResultList.AddRange(array2);
				array2.Dispose();
			}
			m_ResultList.Sort();
			Entity entity = Entity.Null;
			int num2 = 0;
			int num3 = 0;
			while (num2 < m_ResultList.Length)
			{
				Entity entity2 = m_ResultList[num2++];
				if (entity2 != entity)
				{
					m_ResultList[num3++] = entity2;
					entity = entity2;
				}
			}
			if (num3 < m_ResultList.Length)
			{
				m_ResultList.RemoveRange(num3, m_ResultList.Length - num3);
			}
		}
	}

	private struct Heights
	{
		public Bounds1 m_FlexibleBounds;

		public Bounds1 m_RigidBounds;

		public float m_FlexibleStrength;

		public float m_RigidStrength;

		public void Reset()
		{
			m_FlexibleBounds = default(Bounds1);
			m_RigidBounds = default(Bounds1);
			m_FlexibleStrength = 0f;
			m_RigidStrength = 0f;
		}

		public float Center()
		{
			float num = MathUtils.Center(m_FlexibleBounds);
			if (m_RigidStrength != 0f)
			{
				float num2 = MathUtils.Center(m_RigidBounds);
				float num3 = math.min(m_FlexibleStrength, 1f - m_RigidStrength);
				num = (num * num3 + num2 * m_RigidStrength) / (num3 + m_RigidStrength);
			}
			return num;
		}
	}

	[BurstCompile]
	private struct UpdateLotHeightsJob : IJobParallelForDefer
	{
		private struct LotIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public float m_Radius;

			public float3 m_Position;

			public Quad3 m_Quad;

			public Entity m_Ignore;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Lot> m_LotData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<Orphan> m_OrphanData;

			public ComponentLookup<Node> m_NodeData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Game.Net.Elevation> m_ElevationData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

			public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

			public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

			public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public Heights m_Heights1;

			public Heights m_Heights2;

			public Heights m_Heights3;

			public Heights m_Heights4;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Quad.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Quad.xz) || entity == m_Ignore)
				{
					return;
				}
				if (m_LotData.HasComponent(entity))
				{
					Transform transform = m_TransformData[entity];
					PrefabRef prefabRef = m_PrefabRefData[entity];
					int2 @int = 0;
					if (!m_PrefabBuildingTerraformData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || !componentData.m_DontRaise || !componentData.m_DontLower)
					{
						BuildingExtensionData componentData3;
						if (m_PrefabBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
						{
							@int = componentData2.m_LotSize;
						}
						else if (m_PrefabBuildingExtensionData.TryGetComponent(prefabRef.m_Prefab, out componentData3))
						{
							@int = math.select(0, componentData2.m_LotSize, componentData3.m_External);
						}
					}
					if (math.all(@int > 0))
					{
						bool flag = false;
						if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
						{
							Game.Objects.GeometryFlags geometryFlags = (((componentData4.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? Game.Objects.GeometryFlags.Circular : Game.Objects.GeometryFlags.CircularLeg);
							flag = (componentData4.m_Flags & geometryFlags) != 0;
						}
						if (flag)
						{
							CheckCircle(transform.m_Position, (float)@int.x * 4f, rigid: false, componentData.m_DontRaise, componentData.m_DontLower);
						}
						else
						{
							CheckQuad(BuildingUtils.CalculateCorners(transform, @int), rigid: false, componentData.m_DontRaise, componentData.m_DontLower);
						}
					}
				}
				else if (m_CompositionData.HasComponent(entity))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[entity];
					if ((m_PrefabNetGeometryData[prefabRef2.m_Prefab].m_Flags & Game.Net.GeometryFlags.FlattenTerrain) == 0)
					{
						return;
					}
					if (HasLotOwner(entity, out var ignore))
					{
						if (ignore || m_ElevationData.HasComponent(entity))
						{
							return;
						}
						Edge edge = m_EdgeData[entity];
						if (m_ElevationData.HasComponent(edge.m_Start) || m_ElevationData.HasComponent(edge.m_End))
						{
							return;
						}
					}
					Composition composition = m_CompositionData[entity];
					EdgeGeometry geometry = m_EdgeGeometryData[entity];
					StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[entity];
					EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[entity];
					CheckEdgeGeometry(geometry, composition.m_Edge);
					CheckNodeGeometry(startNodeGeometry.m_Geometry, composition.m_StartNode);
					CheckNodeGeometry(endNodeGeometry.m_Geometry, composition.m_EndNode);
				}
				else
				{
					if (!m_OrphanData.HasComponent(entity))
					{
						return;
					}
					PrefabRef prefabRef3 = m_PrefabRefData[entity];
					if ((m_PrefabNetGeometryData[prefabRef3.m_Prefab].m_Flags & Game.Net.GeometryFlags.FlattenTerrain) == 0 || (HasLotOwner(entity, out var ignore2) && (ignore2 || m_ElevationData.HasComponent(entity))))
					{
						return;
					}
					Orphan orphan = m_OrphanData[entity];
					if (m_PrefabNetCompositionData.HasComponent(orphan.m_Composition))
					{
						NetCompositionData netCompositionData = m_PrefabNetCompositionData[orphan.m_Composition];
						if ((netCompositionData.m_State & CompositionState.ExclusiveGround) != 0 && ((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0)
						{
							CheckCircle(m_NodeData[entity].m_Position, netCompositionData.m_Width * 0.5f, rigid: true, dontRaise: false, dontLower: false);
						}
					}
				}
			}

			private bool HasLotOwner(Entity entity, out bool ignore)
			{
				Entity entity2 = entity;
				bool flag = false;
				while (m_OwnerData.HasComponent(entity2))
				{
					entity2 = m_OwnerData[entity2].m_Owner;
					if (entity2 == m_Ignore)
					{
						ignore = true;
						return true;
					}
					flag |= m_LotData.HasComponent(entity2);
				}
				ignore = false;
				return flag;
			}

			private void CheckEdgeGeometry(EdgeGeometry geometry, Entity composition)
			{
				if (!MathUtils.Intersect(geometry.m_Bounds.xz, m_Quad.xz) || !m_PrefabNetCompositionData.HasComponent(composition))
				{
					return;
				}
				NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition];
				if ((netCompositionData.m_State & CompositionState.ExclusiveGround) == 0)
				{
					return;
				}
				if ((netCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0)
				{
					if (geometry.m_Start.m_Length.x > 0.05f)
					{
						CheckCurve(geometry.m_Start.m_Left, rigid: true);
					}
					if (geometry.m_End.m_Length.x > 0.05f)
					{
						CheckCurve(geometry.m_End.m_Left, rigid: true);
					}
				}
				if ((netCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0)
				{
					if (geometry.m_Start.m_Length.y > 0.05f)
					{
						CheckCurve(geometry.m_Start.m_Right, rigid: true);
					}
					if (geometry.m_End.m_Length.y > 0.05f)
					{
						CheckCurve(geometry.m_End.m_Right, rigid: true);
					}
				}
			}

			private void CheckNodeGeometry(EdgeNodeGeometry geometry, Entity composition)
			{
				if (!MathUtils.Intersect(geometry.m_Bounds.xz, m_Quad.xz) || !m_PrefabNetCompositionData.HasComponent(composition))
				{
					return;
				}
				NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition];
				if ((netCompositionData.m_State & CompositionState.ExclusiveGround) == 0)
				{
					return;
				}
				if ((netCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0)
				{
					if (geometry.m_Left.m_Length.x > 0.05f)
					{
						CheckCurve(geometry.m_Left.m_Left, rigid: true);
					}
					if (geometry.m_Right.m_Length.x > 0.05f && (netCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
					{
						CheckCurve(geometry.m_Right.m_Left, rigid: true);
					}
				}
				if ((netCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0)
				{
					if (geometry.m_Right.m_Length.y > 0.05f)
					{
						CheckCurve(geometry.m_Right.m_Right, rigid: true);
					}
					if (geometry.m_Left.m_Length.y > 0.05f && (netCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
					{
						CheckCurve(geometry.m_Left.m_Right, rigid: true);
					}
				}
			}

			private void CheckCurve(Bezier4x3 curve, bool rigid)
			{
				if (MathUtils.Intersect(MathUtils.Bounds(curve.xz), m_Quad.xz))
				{
					float3 @float = math.lerp(m_Quad.a, m_Quad.b, 1f / 6f);
					float3 float2 = math.lerp(m_Quad.a, m_Quad.b, 0.5f);
					float3 float3 = math.lerp(m_Quad.a, m_Quad.b, 5f / 6f);
					if (m_Radius != 0f)
					{
						@float = m_Position + (@float - m_Position) * 1.2247449f;
						float2 = m_Position + (float2 - m_Position) * 1.4142135f;
						float3 = m_Position + (float3 - m_Position) * 1.2247449f;
					}
					CheckCurve(new Line3.Segment(m_Quad.a, @float), 0f, rigid, curve, ref m_Heights1);
					CheckCurve(new Line3.Segment(@float, float2), 0.5f, rigid, curve, ref m_Heights2);
					CheckCurve(new Line3.Segment(float2, float3), 0.5f, rigid, curve, ref m_Heights3);
					CheckCurve(new Line3.Segment(float3, m_Quad.b), 1f, rigid, curve, ref m_Heights4);
				}
			}

			private void CheckQuad(Quad3 quad, bool rigid, bool dontRaise, bool dontLower)
			{
				if (MathUtils.Intersect(quad.xz, m_Quad.xz))
				{
					float3 @float = math.lerp(m_Quad.a, m_Quad.b, 1f / 6f);
					float3 float2 = math.lerp(m_Quad.a, m_Quad.b, 0.5f);
					float3 float3 = math.lerp(m_Quad.a, m_Quad.b, 5f / 6f);
					if (m_Radius != 0f)
					{
						@float = m_Position + (@float - m_Position) * 1.2247449f;
						float2 = m_Position + (float2 - m_Position) * 1.4142135f;
						float3 = m_Position + (float3 - m_Position) * 1.2247449f;
					}
					CheckQuad(new Line3.Segment(m_Quad.a, @float), 0f, rigid, dontRaise, dontLower, quad, ref m_Heights1);
					CheckQuad(new Line3.Segment(@float, float2), 0.5f, rigid, dontRaise, dontLower, quad, ref m_Heights2);
					CheckQuad(new Line3.Segment(float2, float3), 0.5f, rigid, dontRaise, dontLower, quad, ref m_Heights3);
					CheckQuad(new Line3.Segment(float3, m_Quad.b), 1f, rigid, dontRaise, dontLower, quad, ref m_Heights4);
				}
			}

			private void CheckCircle(float3 position, float radius, bool rigid, bool dontRaise, bool dontLower)
			{
				float3 @float = math.lerp(m_Quad.a, m_Quad.b, 1f / 6f);
				float3 float2 = math.lerp(m_Quad.a, m_Quad.b, 0.5f);
				float3 float3 = math.lerp(m_Quad.a, m_Quad.b, 5f / 6f);
				if (m_Radius != 0f)
				{
					@float = m_Position + (@float - m_Position) * 1.2247449f;
					float2 = m_Position + (float2 - m_Position) * 1.4142135f;
					float3 = m_Position + (float3 - m_Position) * 1.2247449f;
				}
				CheckCircle(new Line3.Segment(m_Quad.a, @float), position, radius, rigid, dontRaise, dontLower, ref m_Heights1);
				CheckCircle(new Line3.Segment(@float, float2), position, radius, rigid, dontRaise, dontLower, ref m_Heights2);
				CheckCircle(new Line3.Segment(float2, float3), position, radius, rigid, dontRaise, dontLower, ref m_Heights3);
				CheckCircle(new Line3.Segment(float3, m_Quad.b), position, radius, rigid, dontRaise, dontLower, ref m_Heights4);
			}

			private void CheckCurve(Line3.Segment line, float pivotT, bool rigid, Bezier4x3 curve, ref Heights heights)
			{
				Line3.Segment other = default(Line3.Segment);
				other.a = curve.a;
				for (int i = 1; i <= 16; i++)
				{
					other.b = MathUtils.Position(curve, (float)i * 0.0625f);
					CheckLine(line, pivotT, rigid, dontRaise: false, dontLower: false, other, ref heights);
					other.a = other.b;
				}
			}

			private void CheckQuad(Line3.Segment line, float pivotT, bool rigid, bool dontRaise, bool dontLower, Quad3 quad, ref Heights heights)
			{
				CheckLine(line, pivotT, rigid, dontRaise, dontLower, quad.ab, ref heights);
				CheckLine(line, pivotT, rigid, dontRaise, dontLower, quad.bc, ref heights);
				CheckLine(line, pivotT, rigid, dontRaise, dontLower, quad.cd, ref heights);
				CheckLine(line, pivotT, rigid, dontRaise, dontLower, quad.da, ref heights);
			}

			private void CheckLine(Line3.Segment line, float pivotT, bool rigid, bool dontRaise, bool dontLower, Line3.Segment other, ref Heights heights)
			{
				float3 @float = MathUtils.Position(line, pivotT);
				float t;
				float x = MathUtils.Distance(line.xz, other.a.xz, out t);
				float y = MathUtils.Distance(line.xz, other.b.xz, out t);
				float x2 = MathUtils.Distance(other.xz, @float.xz, out t);
				x2 = math.min(x2, math.min(x, y));
				float num = MathUtils.Length(other.xz);
				float num2 = math.min(8f, num * 16f);
				if (x2 < num2)
				{
					MathUtils.Distance((Line2)other.xz, @float.xz, out float t2);
					float num3 = math.max(0f, math.max(t2 - 1f, 0f - t2)) * num;
					float offset = MathUtils.Position(other.y, t2) - @float.y;
					float strength = (1f - x2 / num2) / (1f + num3 / num2);
					AddHeight(offset, strength, rigid, dontRaise, dontLower, ref heights);
				}
			}

			private void CheckCircle(Line3.Segment line, float3 position, float radius, bool rigid, bool dontRaise, bool dontLower, ref Heights heights)
			{
				float t;
				float num = math.max(0f, MathUtils.Distance(line.xz, position.xz, out t) - radius);
				if (num < 8f)
				{
					float offset = position.y - MathUtils.Position(line.y, t);
					float strength = 1f - num / 8f;
					AddHeight(offset, strength, rigid, dontRaise, dontLower, ref heights);
				}
			}

			private void AddHeight(float offset, float strength, bool rigid, bool dontRaise, bool dontLower, ref Heights heights)
			{
				if (!((offset > 0f && dontRaise) || (offset < 0f && dontLower)))
				{
					if (rigid)
					{
						heights.m_RigidBounds |= offset * strength * 2f;
						heights.m_RigidStrength = math.max(heights.m_RigidStrength, strength);
					}
					else
					{
						heights.m_FlexibleBounds |= offset * strength;
						heights.m_FlexibleStrength = math.max(heights.m_FlexibleStrength, strength);
					}
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Lot> m_LotData;

		[ReadOnly]
		public bool m_IsLoaded;

		[ReadOnly]
		public NativeList<Entity> m_LotList;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_LotList[index];
			Lot lot = m_LotData[entity];
			Transform transform = m_TransformData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			Lot lot2 = lot;
			int2 @int = 1;
			BuildingExtensionData componentData2;
			if (m_PrefabBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				@int = componentData.m_LotSize;
			}
			else if (m_PrefabBuildingExtensionData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
			{
				if (!componentData2.m_External)
				{
					return;
				}
				@int = componentData2.m_LotSize;
			}
			bool flag = false;
			if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
			{
				Game.Objects.GeometryFlags geometryFlags = (((componentData3.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? Game.Objects.GeometryFlags.Circular : Game.Objects.GeometryFlags.CircularLeg);
				flag = (componentData3.m_Flags & geometryFlags) != 0;
			}
			Quad3 quad = BuildingUtils.CalculateCorners(transform, @int);
			Quad3 quad2 = BuildingUtils.CalculateCorners(transform, @int + 2);
			LotIterator iterator = new LotIterator
			{
				m_Ignore = entity,
				m_OwnerData = m_OwnerData,
				m_LotData = m_LotData,
				m_TransformData = m_TransformData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_StartNodeGeometryData = m_StartNodeGeometryData,
				m_EndNodeGeometryData = m_EndNodeGeometryData,
				m_CompositionData = m_CompositionData,
				m_OrphanData = m_OrphanData,
				m_NodeData = m_NodeData,
				m_EdgeData = m_EdgeData,
				m_ElevationData = m_ElevationData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabBuildingData = m_PrefabBuildingData,
				m_PrefabBuildingExtensionData = m_PrefabBuildingExtensionData,
				m_PrefabNetGeometryData = m_PrefabNetGeometryData,
				m_PrefabNetCompositionData = m_PrefabNetCompositionData,
				m_PrefabBuildingTerraformData = m_PrefabBuildingTerraformData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData
			};
			if (flag)
			{
				quad.a = transform.m_Position + (quad.a - transform.m_Position) * 0.70710677f;
				quad.b = transform.m_Position + (quad.b - transform.m_Position) * 0.70710677f;
				quad.c = transform.m_Position + (quad.c - transform.m_Position) * 0.70710677f;
				quad.d = transform.m_Position + (quad.d - transform.m_Position) * 0.70710677f;
				iterator.m_Radius = (float)@int.x * 4f;
				iterator.m_Position = transform.m_Position;
			}
			iterator.m_Heights1.Reset();
			iterator.m_Heights2.Reset();
			iterator.m_Heights3.Reset();
			iterator.m_Heights4.Reset();
			iterator.m_Quad = new Quad3(quad.a, quad.b, quad2.b, quad2.a);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_NetSearchTree.Iterate(ref iterator);
			Heights heights = iterator.m_Heights1;
			lot.m_FrontHeights.y = iterator.m_Heights2.Center();
			lot.m_FrontHeights.z = iterator.m_Heights3.Center();
			iterator.m_Heights1 = iterator.m_Heights4;
			iterator.m_Heights2.Reset();
			iterator.m_Heights3.Reset();
			iterator.m_Heights4.Reset();
			iterator.m_Quad = new Quad3(quad.b, quad.c, quad2.c, quad2.b);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_NetSearchTree.Iterate(ref iterator);
			lot.m_RightHeights.x = iterator.m_Heights1.Center();
			lot.m_RightHeights.y = iterator.m_Heights2.Center();
			lot.m_RightHeights.z = iterator.m_Heights3.Center();
			iterator.m_Heights1 = iterator.m_Heights4;
			iterator.m_Heights2.Reset();
			iterator.m_Heights3.Reset();
			iterator.m_Heights4.Reset();
			iterator.m_Quad = new Quad3(quad.c, quad.d, quad2.d, quad2.c);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_NetSearchTree.Iterate(ref iterator);
			lot.m_BackHeights.x = iterator.m_Heights1.Center();
			lot.m_BackHeights.y = iterator.m_Heights2.Center();
			lot.m_BackHeights.z = iterator.m_Heights3.Center();
			iterator.m_Heights1 = iterator.m_Heights4;
			iterator.m_Heights2.Reset();
			iterator.m_Heights3.Reset();
			iterator.m_Heights4 = heights;
			iterator.m_Quad = new Quad3(quad.d, quad.a, quad2.a, quad2.d);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_NetSearchTree.Iterate(ref iterator);
			lot.m_LeftHeights.x = iterator.m_Heights1.Center();
			lot.m_LeftHeights.y = iterator.m_Heights2.Center();
			lot.m_LeftHeights.z = iterator.m_Heights3.Center();
			lot.m_FrontHeights.x = iterator.m_Heights4.Center();
			m_OwnerData = iterator.m_OwnerData;
			m_LotData = iterator.m_LotData;
			m_TransformData = iterator.m_TransformData;
			m_EdgeGeometryData = iterator.m_EdgeGeometryData;
			m_StartNodeGeometryData = iterator.m_StartNodeGeometryData;
			m_EndNodeGeometryData = iterator.m_EndNodeGeometryData;
			m_CompositionData = iterator.m_CompositionData;
			m_OrphanData = iterator.m_OrphanData;
			m_NodeData = iterator.m_NodeData;
			m_EdgeData = iterator.m_EdgeData;
			m_ElevationData = iterator.m_ElevationData;
			m_PrefabRefData = iterator.m_PrefabRefData;
			m_PrefabBuildingData = iterator.m_PrefabBuildingData;
			m_PrefabBuildingExtensionData = iterator.m_PrefabBuildingExtensionData;
			m_PrefabNetGeometryData = iterator.m_PrefabNetGeometryData;
			m_PrefabNetCompositionData = iterator.m_PrefabNetCompositionData;
			m_PrefabBuildingTerraformData = iterator.m_PrefabBuildingTerraformData;
			m_PrefabObjectGeometryData = iterator.m_PrefabObjectGeometryData;
			if (flag)
			{
				float3 @float = new float3(1.4142135f, 1.0352762f, 1.0352762f);
				lot.m_FrontHeights *= @float;
				lot.m_RightHeights *= @float;
				lot.m_BackHeights *= @float;
				lot.m_LeftHeights *= @float;
			}
			CalculateMiddleHeights(lot.m_FrontHeights.x, ref lot.m_FrontHeights.y, ref lot.m_FrontHeights.z, lot.m_RightHeights.x);
			CalculateMiddleHeights(lot.m_RightHeights.x, ref lot.m_RightHeights.y, ref lot.m_RightHeights.z, lot.m_BackHeights.x);
			CalculateMiddleHeights(lot.m_BackHeights.x, ref lot.m_BackHeights.y, ref lot.m_BackHeights.z, lot.m_LeftHeights.x);
			CalculateMiddleHeights(lot.m_LeftHeights.x, ref lot.m_LeftHeights.y, ref lot.m_LeftHeights.z, lot.m_FrontHeights.x);
			float3 x = math.abs(lot.m_FrontHeights - lot2.m_FrontHeights);
			float3 y = math.abs(lot.m_RightHeights - lot2.m_RightHeights);
			float3 x2 = math.abs(lot.m_BackHeights - lot2.m_BackHeights);
			if (math.cmax(math.max(y: math.max(x2, math.abs(lot.m_LeftHeights - lot2.m_LeftHeights)), x: math.max(x, y))) >= 0.01f)
			{
				m_LotData[entity] = lot;
				if (!m_IsLoaded)
				{
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
				}
			}
		}

		private void CalculateMiddleHeights(float a, ref float b, ref float c, float d)
		{
			float num = b - MathUtils.Position(new Bezier4x1(a, b, c, d), 1f / 3f);
			float num2 = c - MathUtils.Position(new Bezier4x1(a, b, c, d), 2f / 3f);
			b += num * 3f - num2 * 1.5f;
			c += num2 * 3f - num * 1.5f;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<Lot> __Game_Buildings_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		public ComponentLookup<Lot> __Game_Buildings_Lot_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Lot_RO_ComponentLookup = state.GetComponentLookup<Lot>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Buildings_Lot_RW_ComponentLookup = state.GetComponentLookup<Lot>();
		}
	}

	private Game.Objects.UpdateCollectSystem m_ObjectUpdateCollectSystem;

	private Game.Net.UpdateCollectSystem m_NetUpdateCollectSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_UpdateQuery;

	private EntityQuery m_AllQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Objects.UpdateCollectSystem>();
		m_NetUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Net.UpdateCollectSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_UpdateQuery = GetEntityQuery(ComponentType.ReadWrite<Lot>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>());
		m_AllQuery = GetEntityQuery(ComponentType.ReadWrite<Lot>(), ComponentType.Exclude<Deleted>());
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
		bool loaded = GetLoaded();
		EntityQuery query = (loaded ? m_AllQuery : m_UpdateQuery);
		bool flag = !query.IsEmptyIgnoreFilter;
		if (m_ObjectUpdateCollectSystem.isUpdated || m_NetUpdateCollectSystem.netsUpdated || flag)
		{
			JobHandle dependencies;
			NativeQuadTree<Entity, QuadTreeBoundsXZ> staticSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies);
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			NativeQueue<Entity> queue = default(NativeQueue<Entity>);
			NativeQueue<Entity> queue2 = default(NativeQueue<Entity>);
			JobHandle jobHandle = default(JobHandle);
			if (flag)
			{
				JobHandle job = JobChunkExtensions.Schedule(new AddUpdatedLotsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_ResultList = nativeList
				}, query, base.Dependency);
				jobHandle = JobHandle.CombineDependencies(jobHandle, job);
			}
			if (m_ObjectUpdateCollectSystem.isUpdated)
			{
				JobHandle dependencies2;
				NativeList<Bounds2> updatedBounds = m_ObjectUpdateCollectSystem.GetUpdatedBounds(out dependencies2);
				queue = new NativeQueue<Entity>(Allocator.TempJob);
				JobHandle jobHandle2 = new FindUpdatedLotsJob
				{
					m_Bounds = updatedBounds.AsDeferredJobArray(),
					m_SearchTree = staticSearchTree,
					m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = queue.AsParallelWriter()
				}.Schedule(updatedBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
				m_ObjectUpdateCollectSystem.AddBoundsReader(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			if (m_NetUpdateCollectSystem.netsUpdated)
			{
				JobHandle dependencies3;
				NativeList<Bounds2> updatedNetBounds = m_NetUpdateCollectSystem.GetUpdatedNetBounds(out dependencies3);
				queue2 = new NativeQueue<Entity>(Allocator.TempJob);
				JobHandle jobHandle3 = new FindUpdatedLotsJob
				{
					m_Bounds = updatedNetBounds.AsDeferredJobArray(),
					m_SearchTree = staticSearchTree,
					m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = queue2.AsParallelWriter()
				}.Schedule(updatedNetBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies3, dependencies));
				m_NetUpdateCollectSystem.AddNetBoundsReader(jobHandle3);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
			}
			CollectLotsJob jobData = new CollectLotsJob
			{
				m_Queue1 = queue,
				m_Queue2 = queue2,
				m_ResultList = nativeList
			};
			JobHandle dependencies4;
			UpdateLotHeightsJob jobData2 = new UpdateLotHeightsJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingTerraformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RW_ComponentLookup, ref base.CheckedStateRef),
				m_IsLoaded = loaded,
				m_LotList = nativeList,
				m_StaticObjectSearchTree = staticSearchTree,
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies4),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle4 = IJobExtensions.Schedule(jobData, jobHandle);
			JobHandle jobHandle5 = jobData2.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle4, dependencies, dependencies4));
			if (queue.IsCreated)
			{
				queue.Dispose(jobHandle4);
			}
			if (queue2.IsCreated)
			{
				queue2.Dispose(jobHandle4);
			}
			nativeList.Dispose(jobHandle5);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle5);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle5);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle5);
			base.Dependency = jobHandle5;
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
	public LotHeightSystem()
	{
	}
}
