using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class LaneBlockSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindBlockedLanesJob : IJobChunk
	{
		private struct FindBlockedLanesIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Entity m_Entity;

			public float3 m_Position;

			public float m_Radius;

			public DynamicBuffer<BlockedLane> m_BlockedLanes;

			public LaneObjectCommandBuffer m_LaneObjectBuffer;

			public BufferLookup<Game.Net.SubLane> m_SubLanes;

			public ComponentLookup<MasterLane> m_MasterLaneData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetLaneData> m_PrefabLaneData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_SubLanes.HasBuffer(edgeEntity))
				{
					return;
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[edgeEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_MasterLaneData.HasComponent(subLane))
					{
						continue;
					}
					Entity prefab = m_PrefabRefData[subLane].m_Prefab;
					Bezier4x3 bezier = m_CurveData[subLane].m_Bezier;
					NetLaneData netLaneData = m_PrefabLaneData[prefab];
					float num = m_Radius + netLaneData.m_Width * 0.5f;
					float num2 = MathUtils.Distance(MathUtils.Bounds(bezier), m_Position);
					if (num2 < num)
					{
						num2 = MathUtils.Distance(bezier, m_Position, out var t);
						if (num2 < num)
						{
							num2 = math.max(0f, num2 - netLaneData.m_Width * 0.5f);
							float length = math.sqrt(math.max(0f, m_Radius * m_Radius - num2 * num2));
							Bounds1 t2 = new Bounds1(0f, t);
							Bounds1 t3 = new Bounds1(t, 1f);
							MathUtils.ClampLengthInverse(bezier, ref t2, length);
							MathUtils.ClampLength(bezier, ref t3, length);
							m_BlockedLanes.Add(new BlockedLane(subLane, new float2(t2.min, t3.max)));
							m_LaneObjectBuffer.Add(subLane, m_Entity, new float2(t2.min, t3.max));
						}
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		public BufferTypeHandle<BlockedLane> m_BlockedLaneType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<BlockedLane> bufferAccessor = chunk.GetBufferAccessor(ref m_BlockedLaneType);
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					DynamicBuffer<BlockedLane> blockedLanes = bufferAccessor[i];
					Transform transform = nativeArray2[i];
					AddBlockedLanes(entity, blockedLanes, transform, nativeArray3[i].m_Prefab);
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					DynamicBuffer<BlockedLane> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						m_LaneObjectBuffer.Remove(dynamicBuffer[k].m_Lane, entity2);
					}
				}
				return;
			}
			NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int l = 0; l < nativeArray.Length; l++)
			{
				Entity entity3 = nativeArray[l];
				DynamicBuffer<BlockedLane> blockedLanes2 = bufferAccessor[l];
				Transform transform2 = nativeArray4[l];
				PrefabRef prefabRef = nativeArray5[l];
				for (int m = 0; m < blockedLanes2.Length; m++)
				{
					m_LaneObjectBuffer.Remove(blockedLanes2[m].m_Lane, entity3);
				}
				blockedLanes2.Clear();
				AddBlockedLanes(entity3, blockedLanes2, transform2, prefabRef.m_Prefab);
			}
		}

		private void AddBlockedLanes(Entity entity, DynamicBuffer<BlockedLane> blockedLanes, Transform transform, Entity prefab)
		{
			Bounds3 bounds = ((!m_PrefabGeometryData.HasComponent(prefab)) ? default(Bounds3) : m_PrefabGeometryData[prefab].m_Bounds);
			FindBlockedLanesIterator iterator = new FindBlockedLanesIterator
			{
				m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, bounds),
				m_Entity = entity,
				m_Position = transform.m_Position,
				m_Radius = (bounds.max.x - bounds.min.y) * 0.5f,
				m_BlockedLanes = blockedLanes,
				m_LaneObjectBuffer = m_LaneObjectBuffer,
				m_SubLanes = m_SubLanes,
				m_MasterLaneData = m_MasterLaneData,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabLaneData = m_PrefabLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		public BufferTypeHandle<BlockedLane> __Game_Objects_BlockedLane_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Objects_BlockedLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<BlockedLane>();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_ObjectQuery;

	private LaneObjectUpdater m_LaneObjectUpdater;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<BlockedLane>(),
				ComponentType.ReadOnly<Transform>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LaneObjectUpdater = new LaneObjectUpdater(this);
		RequireForUpdate(m_ObjectQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new FindBlockedLanesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockedLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_BlockedLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_LaneObjectBuffer = m_LaneObjectUpdater.Begin(Allocator.TempJob)
		}, m_ObjectQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		JobHandle dependency = m_LaneObjectUpdater.Apply(this, jobHandle);
		base.Dependency = dependency;
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
	public LaneBlockSystem()
	{
	}
}
