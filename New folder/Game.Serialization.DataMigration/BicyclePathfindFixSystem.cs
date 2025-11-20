using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

[CompilerGenerated]
public class BicyclePathfindFixSystem : GameSystemBase
{
	[BurstCompile]
	private struct FixLaneDataJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<LaneFlow> m_LaneFlowType;

		[ReadOnly]
		public ComponentTypeHandle<SecondaryFlow> m_SecondaryFlowType;

		[ReadOnly]
		public ComponentTypeHandle<LaneColor> m_LaneColorType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> m_PrefabNetLaneGeometryData;

		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Net.ConnectionLane> nativeArray2 = chunk.GetNativeArray(ref m_ConnectionLaneType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag = chunk.Has(ref m_CarLaneType);
			bool flag2 = chunk.Has(ref m_LaneFlowType);
			bool flag3 = chunk.Has(ref m_SecondaryFlowType);
			bool flag4 = chunk.Has(ref m_LaneColorType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (CollectionUtils.TryGet(nativeArray2, i, out var value) && (value.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Pedestrian | ConnectionLaneFlags.Area)) == (ConnectionLaneFlags.Pedestrian | ConnectionLaneFlags.Area))
				{
					value.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd;
					nativeArray2[i] = value;
				}
				if (!flag)
				{
					continue;
				}
				Entity e = nativeArray[i];
				PrefabRef prefabRef = nativeArray3[i];
				bool flag5 = false;
				bool flag6 = false;
				if (m_PrefabLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					flag5 = (componentData.m_Flags & LaneFlags.TrackFlow) != 0 && (componentData2.m_RoadTypes & ~RoadTypes.Bicycle) != 0;
					flag6 = (componentData.m_Flags & LaneFlags.TrackFlow) != 0 && (componentData2.m_RoadTypes & RoadTypes.Bicycle) != 0;
					if (!flag4 && componentData2.m_RoadTypes == RoadTypes.Bicycle && m_PrefabNetLaneGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						m_CommandBuffer.AddComponent<LaneColor>(unfilteredChunkIndex, e);
					}
				}
				if (flag2 != flag5)
				{
					if (flag2)
					{
						m_CommandBuffer.RemoveComponent<LaneFlow>(unfilteredChunkIndex, e);
					}
					if (flag5)
					{
						m_CommandBuffer.AddComponent<LaneFlow>(unfilteredChunkIndex, e);
					}
				}
				if (flag3 != flag6)
				{
					if (flag3)
					{
						m_CommandBuffer.RemoveComponent<SecondaryFlow>(unfilteredChunkIndex, e);
					}
					if (flag6)
					{
						m_CommandBuffer.AddComponent<SecondaryFlow>(unfilteredChunkIndex, e);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AddSubLaneJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> m_PrefabSubLanes;

		[ReadOnly]
		public ComponentTypeSet m_SubLaneSet;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity e = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				if (!m_PrefabSubLanes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					continue;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					Game.Prefabs.SubLane subLane = bufferData[j];
					if (subLane.m_NodeIndex.x != subLane.m_NodeIndex.y)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, in m_SubLaneSet);
						break;
					}
				}
			}
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
		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneFlow> __Game_Net_LaneFlow_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SecondaryFlow> __Game_Net_SecondaryFlow_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneColor> __Game_Net_LaneColor_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

		public ComponentTypeHandle<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> __Game_Prefabs_SubLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_LaneFlow_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneFlow>(isReadOnly: true);
			__Game_Net_SecondaryFlow_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SecondaryFlow>(isReadOnly: true);
			__Game_Net_LaneColor_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneColor>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
			__Game_Net_ConnectionLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ConnectionLane>();
			__Game_Prefabs_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubLane>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private DeserializationBarrier m_DeserializationBarrier;

	private EntityQuery m_BicycleOwnerQuery;

	private EntityQuery m_SubLaneQuery;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_TakeoffLocationQuery;

	private EntityQuery m_SubObjectQuery;

	private EntityQuery m_ParkingFacilityQuery;

	private ComponentTypeSet m_SubLaneSet;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_DeserializationBarrier = base.World.GetOrCreateSystemManaged<DeserializationBarrier>();
		m_BicycleOwnerQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.Exclude<BicycleOwner>());
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<AreaLane>(),
				ComponentType.ReadOnly<Game.Net.CarLane>()
			}
		});
		m_SubLaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.SubLane>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>()
			}
		});
		m_TakeoffLocationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Routes.TakeoffLocation>());
		m_SubObjectQuery = GetEntityQuery(ComponentType.ReadOnly<Static>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<Game.Net.SubLane>());
		m_SubLaneSet = new ComponentTypeSet(new ComponentType[2]
		{
			ComponentType.ReadWrite<Game.Net.SubLane>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkingFacilityQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Buildings.ParkingFacility>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<CarParkingFacility>(),
				ComponentType.ReadOnly<BicycleParkingFacility>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LoadGameSystem.context.format.Has(FormatTags.BicycleDataMigration))
		{
			return;
		}
		if (!m_BicycleOwnerQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_BicycleOwnerQuery.ToEntityArray(Allocator.TempJob);
			base.EntityManager.AddComponent<BicycleOwner>(m_BicycleOwnerQuery);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Citizen componentData = base.EntityManager.GetComponentData<Citizen>(nativeArray[i]);
				bool flag = (componentData.m_State & CitizenFlags.Commuter) != 0;
				bool flag2 = (componentData.m_State & CitizenFlags.Tourist) != 0;
				bool flag3 = (componentData.m_State & (CitizenFlags.AgeBit1 | CitizenFlags.AgeBit2)) != 0;
				base.EntityManager.SetComponentEnabled<BicycleOwner>(nativeArray[i], !flag && !flag2 && flag3);
			}
			nativeArray.Dispose();
		}
		if (!m_SubLaneQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Updated>(m_SubLaneQuery);
		}
		if (!m_TakeoffLocationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Updated>(m_TakeoffLocationQuery);
		}
		if (!m_LaneQuery.IsEmptyIgnoreFilter)
		{
			FixLaneDataJob jobData = new FixLaneDataJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LaneFlowType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneFlow_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SecondaryFlowType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SecondaryFlow_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LaneColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneColor_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency);
			m_DeserializationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_SubObjectQuery.IsEmptyIgnoreFilter)
		{
			AddSubLaneJob jobData2 = new AddSubLaneJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLaneSet = m_SubLaneSet,
				m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_SubObjectQuery, base.Dependency);
			m_DeserializationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (m_ParkingFacilityQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Entity> nativeArray2 = m_ParkingFacilityQuery.ToEntityArray(Allocator.TempJob);
		for (int j = 0; j < nativeArray2.Length; j++)
		{
			if (base.EntityManager.TryGetComponent<PrefabRef>(nativeArray2[j], out var component) && base.EntityManager.TryGetComponent<ParkingFacilityData>(component.m_Prefab, out var component2))
			{
				if ((component2.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
				{
					base.EntityManager.AddComponentData(nativeArray2[j], default(CarParkingFacility));
				}
				if ((component2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
				{
					base.EntityManager.AddComponentData(nativeArray2[j], default(BicycleParkingFacility));
				}
			}
		}
		nativeArray2.Dispose();
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
	public BicyclePathfindFixSystem()
	{
	}
}
