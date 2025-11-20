using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class OverrideSystem : GameSystemBase
{
	private struct TreeAction
	{
		public Entity m_Entity;

		public BoundsMask m_Mask;
	}

	[BurstCompile]
	private struct UpdateOverriddenLayersJob : IJob
	{
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		public NativeQueue<TreeAction> m_Actions;

		public void Execute()
		{
			TreeAction item;
			while (m_Actions.TryDequeue(out item))
			{
				if (m_LaneSearchTree.TryGet(item.m_Entity, out var bounds))
				{
					bounds.m_Mask = (bounds.m_Mask & ~(BoundsMask.AllLayers | BoundsMask.NotOverridden)) | item.m_Mask;
					m_LaneSearchTree.Update(item.m_Entity, bounds);
				}
			}
		}
	}

	[BurstCompile]
	private struct FindUpdatedLanesJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					m_ResultQueue.Enqueue(objectEntity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = MathUtils.Expand(m_Bounds[index], 1.6f),
				m_ResultQueue = m_ResultQueue
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CollectObjectsJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_Queue;

		public NativeList<Entity> m_ResultList;

		public void Execute()
		{
			int count = m_Queue.Count;
			m_ResultList.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_ResultList[i] = m_Queue.Dequeue();
			}
			m_ResultList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_ResultList.Length)
			{
				Entity entity2 = m_ResultList[num++];
				if (entity2 != entity)
				{
					m_ResultList[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_ResultList.Length)
			{
				m_ResultList.RemoveRangeSwapBack(num2, m_ResultList.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct CheckLaneOverrideJob : IJobParallelForDefer
	{
		private struct LaneIteratorSubData
		{
			public Bounds3 m_Bounds1;

			public Bounds3 m_Bounds2;

			public Bezier4x3 m_Curve1;

			public Bezier4x3 m_Curve2;
		}

		private struct LaneIterator : INativeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, LaneIteratorSubData>, IUnsafeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, LaneIteratorSubData>
		{
			public float m_Range;

			public float m_SizeLimit;

			public float m_Priority;

			public Entity m_LaneEntity;

			public Curve m_LaneCurve;

			public NativeList<CutRange> m_CutRangeList;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

			public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

			public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

			public bool m_CutForTraffic;

			public bool m_FullOverride;

			public bool Intersect(QuadTreeBoundsXZ bounds, ref LaneIteratorSubData subData)
			{
				if (m_FullOverride)
				{
					return false;
				}
				bool2 x = default(bool2);
				x.x = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds1);
				x.y = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds2);
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
					x.x = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds1);
					x.y = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds2);
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

			public void Iterate(QuadTreeBoundsXZ bounds, LaneIteratorSubData subData, Entity entity)
			{
				if (m_FullOverride || m_LaneEntity == entity)
				{
					return;
				}
				bool2 x = default(bool2);
				x.x = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds1);
				x.y = MathUtils.Intersect(bounds.m_Bounds, subData.m_Bounds2);
				if (!math.any(x))
				{
					return;
				}
				Bounds1 bounds2 = new Bounds1(1f, 0f);
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					if ((componentData.m_UtilityTypes & UtilityTypes.Fence) == 0 || componentData.m_VisualCapacity < m_Priority)
					{
						return;
					}
					Curve curve = m_CurveData[entity];
					NetLaneGeometryData netLaneGeometryData = m_PrefabLaneGeometryData[prefabRef.m_Prefab];
					float num = m_Range + netLaneGeometryData.m_Size.x * 0.5f;
					Bounds1 bounds3 = new Bounds1(1f, 0f);
					if (MathUtils.Distance(m_LaneCurve.m_Bezier, curve.m_Bezier.a, out var t) < num && IsParallel(MathUtils.Tangent(m_LaneCurve.m_Bezier, t), MathUtils.StartTangent(curve.m_Bezier)))
					{
						bounds2 |= t;
						bounds3 |= 0f;
					}
					if (MathUtils.Distance(m_LaneCurve.m_Bezier, curve.m_Bezier.d, out var t2) < num && IsParallel(MathUtils.Tangent(m_LaneCurve.m_Bezier, t2), MathUtils.EndTangent(curve.m_Bezier)))
					{
						bounds2 |= t2;
						bounds3 |= 1f;
					}
					if (MathUtils.Distance(curve.m_Bezier, m_LaneCurve.m_Bezier.a, out var t3) < num && IsParallel(MathUtils.Tangent(curve.m_Bezier, t3), MathUtils.StartTangent(m_LaneCurve.m_Bezier)))
					{
						bounds2 |= 0f;
						bounds3 |= t3;
					}
					if (MathUtils.Distance(curve.m_Bezier, m_LaneCurve.m_Bezier.d, out var t4) < num && IsParallel(MathUtils.Tangent(curve.m_Bezier, t4), MathUtils.EndTangent(m_LaneCurve.m_Bezier)))
					{
						bounds2 |= 1f;
						bounds3 |= t4;
					}
					float num2 = MathUtils.Size(bounds2);
					float num3 = MathUtils.Size(bounds3);
					if (num2 <= 0f || num3 <= 0f || (m_Priority == componentData.m_VisualCapacity && (num3 > num2 || (num2 == num3 && m_LaneEntity.Index > entity.Index))))
					{
						return;
					}
				}
				else
				{
					if (!m_CutForTraffic || !m_PrefabNetLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) || (componentData2.m_Flags & (LaneFlags.Road | LaneFlags.Pedestrian)) == 0)
					{
						return;
					}
					Curve curve2 = m_CurveData[entity];
					if (MathUtils.Intersect(m_LaneCurve.m_Bezier.xz, curve2.m_Bezier.xz, out var t5, 3))
					{
						float num4 = componentData2.m_Width * 0.5f / math.max(0.001f, m_LaneCurve.m_Length);
						bounds2.min = math.clamp(t5.x - num4, 0f, bounds2.min);
						bounds2.max = math.clamp(t5.x + num4, bounds2.max, 1f);
					}
					if (MathUtils.Size(bounds2) <= 0f)
					{
						return;
					}
				}
				AddCutRange(bounds2);
			}

			public void AddCutRange(Bounds1 range)
			{
				if (range.min * m_LaneCurve.m_Length < m_Range)
				{
					range.min = 0f;
				}
				if ((1f - range.max) * m_LaneCurve.m_Length < m_Range)
				{
					range.max = 1f;
				}
				if (range.min == 0f && range.max == 1f)
				{
					m_FullOverride = true;
					return;
				}
				if (!m_CutRangeList.IsCreated)
				{
					m_CutRangeList = new NativeList<CutRange>(4, Allocator.Temp);
				}
				CutRange value = new CutRange
				{
					m_CurveDelta = range
				};
				for (int i = 0; i < m_CutRangeList.Length; i++)
				{
					CutRange cutRange = m_CutRangeList[i];
					if (ShouldMerge(cutRange, value))
					{
						cutRange.m_CurveDelta |= value.m_CurveDelta;
						int num = 0;
						for (int j = i + 1; j < m_CutRangeList.Length; j++)
						{
							CutRange cutRange2 = m_CutRangeList[j];
							if (!ShouldMerge(cutRange, cutRange2))
							{
								break;
							}
							cutRange.m_CurveDelta |= cutRange2.m_CurveDelta;
							num++;
						}
						if (num != 0)
						{
							m_CutRangeList.RemoveRange(i + 1, num);
						}
						m_CutRangeList[i] = cutRange;
						if (cutRange.m_CurveDelta.min == 0f && cutRange.m_CurveDelta.max == 1f)
						{
							m_FullOverride = true;
							m_CutRangeList.Clear();
						}
						return;
					}
					if (cutRange.m_CurveDelta.min > value.m_CurveDelta.min)
					{
						CollectionUtils.Insert(m_CutRangeList, i, value);
						return;
					}
				}
				m_CutRangeList.Add(in value);
			}

			private bool IsParallel(float3 tangent1, float3 tangent2)
			{
				return math.abs(math.dot(math.normalizesafe(tangent1.xz), math.normalizesafe(tangent2.xz))) > 0.95f;
			}

			private bool ShouldMerge(CutRange cutRange1, CutRange cutRange2)
			{
				if ((cutRange1.m_CurveDelta.min - cutRange2.m_CurveDelta.max) * m_LaneCurve.m_Length < m_Range)
				{
					return (cutRange2.m_CurveDelta.min - cutRange1.m_CurveDelta.max) * m_LaneCurve.m_Length < m_Range;
				}
				return false;
			}
		}

		[ReadOnly]
		public ComponentLookup<Overridden> m_OverriddenData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<UtilityLane> m_UtilityLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CutRange> m_CutRanges;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<Entity> m_LaneArray;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<TreeAction>.ParallelWriter m_TreeActions;

		public void Execute(int index)
		{
			Entity entity = m_LaneArray[index];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (!m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || (componentData.m_UtilityTypes & UtilityTypes.Fence) == 0)
			{
				return;
			}
			Curve curve = m_CurveData[entity];
			UtilityLane utilityLane = m_UtilityLaneData[entity];
			NetLaneGeometryData laneGeometryData = m_PrefabLaneGeometryData[prefabRef.m_Prefab];
			float num = laneGeometryData.m_Size.x * 0.5f + 1.6f;
			LaneIterator iterator = new LaneIterator
			{
				m_Range = num,
				m_SizeLimit = num * 4f,
				m_Priority = componentData.m_VisualCapacity,
				m_LaneEntity = entity,
				m_LaneCurve = curve,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabUtilityLaneData = m_PrefabUtilityLaneData,
				m_PrefabNetLaneData = m_PrefabNetLaneData,
				m_PrefabLaneGeometryData = m_PrefabLaneGeometryData,
				m_CutForTraffic = ((utilityLane.m_Flags & UtilityLaneFlags.CutForTraffic) != 0)
			};
			LaneIteratorSubData subData = default(LaneIteratorSubData);
			MathUtils.Divide(curve.m_Bezier, out subData.m_Curve1, out subData.m_Curve2, 0.5f);
			subData.m_Bounds1 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve1), num);
			subData.m_Bounds2 = MathUtils.Expand(MathUtils.Bounds(subData.m_Curve2), num);
			m_LaneSearchTree.Iterate(ref iterator, subData);
			if (iterator.m_CutForTraffic && !iterator.m_FullOverride && (!iterator.m_CutRangeList.IsCreated || iterator.m_CutRangeList.Length == 0))
			{
				float num2 = math.min(0.25f, 3f / math.max(0.001f, curve.m_Length));
				iterator.AddCutRange(new Bounds1(0.5f - num2, 0.5f + num2));
			}
			m_CurveData = iterator.m_CurveData;
			m_PrefabRefData = iterator.m_PrefabRefData;
			m_PrefabUtilityLaneData = iterator.m_PrefabUtilityLaneData;
			m_PrefabNetLaneData = iterator.m_PrefabNetLaneData;
			m_PrefabLaneGeometryData = iterator.m_PrefabLaneGeometryData;
			if (iterator.m_FullOverride)
			{
				if (!m_OverriddenData.HasComponent(entity))
				{
					if (m_CutRanges.HasBuffer(entity))
					{
						m_CommandBuffer.RemoveComponent<CutRange>(index, entity);
					}
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
					m_CommandBuffer.AddComponent(index, entity, default(Overridden));
					AddTreeAction(entity, curve, utilityLane, laneGeometryData, isOverridden: true);
				}
			}
			else if (iterator.m_CutRangeList.IsCreated && iterator.m_CutRangeList.Length != 0)
			{
				DynamicBuffer<CutRange> bufferData;
				if (m_OverriddenData.HasComponent(entity))
				{
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
					m_CommandBuffer.RemoveComponent<Overridden>(index, entity);
					m_CommandBuffer.AddBuffer<CutRange>(index, entity).AddRange(iterator.m_CutRangeList.AsArray());
					AddTreeAction(entity, curve, utilityLane, laneGeometryData, isOverridden: false);
				}
				else if (m_CutRanges.TryGetBuffer(entity, out bufferData))
				{
					if (!IsEqual(bufferData, iterator.m_CutRangeList))
					{
						m_CommandBuffer.AddComponent(index, entity, default(Updated));
						bufferData.CopyFrom(iterator.m_CutRangeList.AsArray());
					}
				}
				else
				{
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
					m_CommandBuffer.AddBuffer<CutRange>(index, entity).AddRange(iterator.m_CutRangeList.AsArray());
				}
			}
			else if (m_OverriddenData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(index, entity, default(Updated));
				m_CommandBuffer.RemoveComponent<Overridden>(index, entity);
				AddTreeAction(entity, curve, utilityLane, laneGeometryData, isOverridden: false);
			}
			else if (m_CutRanges.HasBuffer(entity))
			{
				m_CommandBuffer.AddComponent(index, entity, default(Updated));
				m_CommandBuffer.RemoveComponent<CutRange>(index, entity);
			}
			if (iterator.m_CutRangeList.IsCreated)
			{
				iterator.m_CutRangeList.Dispose();
			}
		}

		private void AddTreeAction(Entity entity, Curve curve, UtilityLane utilityLane, NetLaneGeometryData laneGeometryData, bool isOverridden)
		{
			TreeAction value = new TreeAction
			{
				m_Entity = entity
			};
			if (!isOverridden)
			{
				value.m_Mask |= BoundsMask.NotOverridden;
				if (curve.m_Length > 0.1f)
				{
					MeshLayer defaultLayers = (m_EditorMode ? laneGeometryData.m_EditorLayers : laneGeometryData.m_GameLayers);
					m_OwnerData.TryGetComponent(entity, out var componentData);
					value.m_Mask |= CommonUtils.GetBoundsMask(SearchSystem.GetLayers(componentData, utilityLane, defaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
				}
			}
			m_TreeActions.Enqueue(value);
		}

		private bool IsEqual(DynamicBuffer<CutRange> cutRanges1, NativeList<CutRange> cutRanges2)
		{
			if (cutRanges1.Length != cutRanges2.Length)
			{
				return false;
			}
			for (int i = 0; i < cutRanges1.Length; i++)
			{
				if (!cutRanges1[i].m_CurveDelta.Equals(cutRanges2[i].m_CurveDelta))
				{
					return false;
				}
			}
			return true;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLane> __Game_Net_UtilityLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		public BufferLookup<CutRange> __Game_Net_CutRange_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentLookup = state.GetComponentLookup<UtilityLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_CutRange_RW_BufferLookup = state.GetBufferLookup<CutRange>();
		}
	}

	private const float MIN_PARALLEL_FENCE_DISTANCE = 1.6f;

	private UpdateCollectSystem m_NetUpdateCollectSystem;

	private SearchSystem m_NetSearchSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private ToolSystem m_ToolSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetUpdateCollectSystem = base.World.GetOrCreateSystemManaged<UpdateCollectSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_NetUpdateCollectSystem.lanesUpdated)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			NativeQueue<TreeAction> actions = new NativeQueue<TreeAction>(Allocator.TempJob);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, CollectUpdatedLanes(nativeList));
			JobHandle dependencies;
			CheckLaneOverrideJob jobData = new CheckLaneOverrideJob
			{
				m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CutRanges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_CutRange_RW_BufferLookup, ref base.CheckedStateRef),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_LaneArray = nativeList.AsDeferredJobArray(),
				m_LaneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_TreeActions = actions.AsParallelWriter()
			};
			JobHandle dependencies2;
			UpdateOverriddenLayersJob jobData2 = new UpdateOverriddenLayersJob
			{
				m_LaneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: false, out dependencies2),
				m_Actions = actions
			};
			JobHandle jobHandle = jobData.Schedule(nativeList, 1, JobHandle.CombineDependencies(base.Dependency, dependencies));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, dependencies2));
			nativeList.Dispose(jobHandle);
			actions.Dispose(jobHandle2);
			m_NetSearchSystem.AddLaneSearchTreeWriter(jobHandle2);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	private JobHandle CollectUpdatedLanes(NativeList<Entity> updateLanesList)
	{
		NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);
		JobHandle dependencies;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies);
		JobHandle jobHandle = default(JobHandle);
		if (m_NetUpdateCollectSystem.lanesUpdated)
		{
			JobHandle dependencies2;
			NativeList<Bounds2> updatedLaneBounds = m_NetUpdateCollectSystem.GetUpdatedLaneBounds(out dependencies2);
			JobHandle jobHandle2 = new FindUpdatedLanesJob
			{
				m_Bounds = updatedLaneBounds.AsDeferredJobArray(),
				m_SearchTree = laneSearchTree,
				m_ResultQueue = queue.AsParallelWriter()
			}.Schedule(updatedLaneBounds, 1, JobHandle.CombineDependencies(dependencies2, dependencies));
			m_NetUpdateCollectSystem.AddLaneBoundsReader(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		JobHandle jobHandle3 = IJobExtensions.Schedule(new CollectObjectsJob
		{
			m_Queue = queue,
			m_ResultList = updateLanesList
		}, jobHandle);
		queue.Dispose(jobHandle3);
		m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
		return jobHandle3;
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
	public OverrideSystem()
	{
	}
}
