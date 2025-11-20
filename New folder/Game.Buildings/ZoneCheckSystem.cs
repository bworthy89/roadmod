using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class ZoneCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindSpawnableBuildingsJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

			public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_BuildingData.HasComponent(objectEntity))
				{
					PrefabRef prefabRef = m_PrefabRefData[objectEntity];
					if (m_PrefabSpawnableBuildingData.HasComponent(prefabRef.m_Prefab) && !m_PrefabSignatureBuildingData.HasComponent(prefabRef.m_Prefab))
					{
						m_ResultQueue.Enqueue(objectEntity);
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = m_Bounds[index],
				m_ResultQueue = m_ResultQueue,
				m_BuildingData = m_BuildingData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabSpawnableBuildingData = m_PrefabSpawnableBuildingData,
				m_PrefabSignatureBuildingData = m_PrefabSignatureBuildingData
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CollectEntitiesJob : IJob
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

		public NativeList<Entity> m_List;

		public void Execute()
		{
			int count = m_Queue.Count;
			if (count == 0)
			{
				return;
			}
			m_List.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_List[i] = m_Queue.Dequeue();
			}
			m_List.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_List.Length)
			{
				Entity entity2 = m_List[num++];
				if (entity2 != entity)
				{
					m_List[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_List.Length)
			{
				m_List.RemoveRangeSwapBack(num2, m_List.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct CheckBuildingZonesJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public int2 m_LotSize;

			public float2 m_StartPosition;

			public float2 m_Right;

			public float2 m_Forward;

			public ZoneType m_ZoneType;

			public CellFlags m_Directions;

			public NativeArray<bool> m_Validated;

			public ComponentLookup<Block> m_BlockData;

			public ComponentLookup<ValidArea> m_ValidAreaData;

			public BufferLookup<Cell> m_Cells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds))
				{
					return;
				}
				ValidArea validArea = m_ValidAreaData[blockEntity];
				if (validArea.m_Area.y <= validArea.m_Area.x)
				{
					return;
				}
				Block target = new Block
				{
					m_Direction = m_Forward
				};
				Block block = m_BlockData[blockEntity];
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
				float2 @float = m_StartPosition;
				int2 @int = default(int2);
				@int.y = 0;
				while (@int.y < m_LotSize.y)
				{
					float2 position = @float;
					@int.x = 0;
					while (@int.x < m_LotSize.x)
					{
						int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
						if (math.all((cellIndex >= validArea.m_Area.xz) & (cellIndex < validArea.m_Area.yw)))
						{
							int index = cellIndex.y * block.m_Size.x + cellIndex.x;
							Cell cell = dynamicBuffer[index];
							if ((cell.m_State & CellFlags.Visible) != CellFlags.None && cell.m_Zone.Equals(m_ZoneType))
							{
								m_Validated[@int.y * m_LotSize.x + @int.x] = true;
								if ((cell.m_State & (CellFlags.Roadside | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) != CellFlags.None)
								{
									CellFlags roadDirection = ZoneUtils.GetRoadDirection(target, block, cell.m_State);
									int4 x = math.select(trueValue: new int4(512, 4, 1024, 2048), falseValue: 0, test: new bool4(@int == 0, @int == m_LotSize - 1));
									m_Directions |= (CellFlags)((uint)roadDirection & (uint)(ushort)math.csum(x));
								}
							}
						}
						position -= m_Right;
						@int.x++;
					}
					@float -= m_Forward;
					@int.y++;
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Condemned> m_CondemnedData;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> m_PrefabPlaceholderBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_PrefabZoneData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public NativeArray<Entity> m_Buildings;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		[ReadOnly]
		public bool m_EditorMode;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_Buildings[index];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			BuildingData prefabBuildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
			SpawnableBuildingData prefabSpawnableBuildingData = m_PrefabSpawnableBuildingData[prefabRef.m_Prefab];
			bool flag = m_EditorMode;
			if (!flag)
			{
				flag = ValidateAttachedParent(entity, prefabBuildingData, prefabSpawnableBuildingData);
			}
			if (!flag)
			{
				flag = ValidateZoneBlocks(entity, prefabBuildingData, prefabSpawnableBuildingData);
			}
			if (flag)
			{
				if (m_CondemnedData.HasComponent(entity))
				{
					m_CommandBuffer.RemoveComponent<Condemned>(index, entity);
					m_IconCommandBuffer.Remove(entity, m_BuildingConfigurationData.m_CondemnedNotification);
				}
			}
			else if (!m_CondemnedData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(index, entity, default(Condemned));
				if (!m_DestroyedData.HasComponent(entity) && !m_AbandonedData.HasComponent(entity))
				{
					m_IconCommandBuffer.Add(entity, m_BuildingConfigurationData.m_CondemnedNotification, IconPriority.FatalProblem);
				}
			}
		}

		private bool ValidateAttachedParent(Entity building, BuildingData prefabBuildingData, SpawnableBuildingData prefabSpawnableBuildingData)
		{
			if (m_AttachedData.HasComponent(building))
			{
				Attached attached = m_AttachedData[building];
				if (m_PrefabRefData.HasComponent(attached.m_Parent))
				{
					PrefabRef prefabRef = m_PrefabRefData[attached.m_Parent];
					if (m_PrefabPlaceholderBuildingData.HasComponent(prefabRef.m_Prefab) && m_PrefabPlaceholderBuildingData[prefabRef.m_Prefab].m_ZonePrefab == prefabSpawnableBuildingData.m_ZonePrefab)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateZoneBlocks(Entity building, BuildingData prefabBuildingData, SpawnableBuildingData prefabSpawnableBuildingData)
		{
			Transform transform = m_TransformData[building];
			if (m_PrefabZoneData.TryGetComponent(prefabSpawnableBuildingData.m_ZonePrefab, out var componentData) && !componentData.m_ZoneType.Equals(ZoneType.None) && !m_PrefabData.IsComponentEnabled(prefabSpawnableBuildingData.m_ZonePrefab))
			{
				return false;
			}
			float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
			float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
			float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
			float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
			float2 float3 = math.abs(float2) + math.abs(@float);
			NativeArray<bool> validated = new NativeArray<bool>(prefabBuildingData.m_LotSize.x * prefabBuildingData.m_LotSize.y, Allocator.Temp);
			Iterator iterator = new Iterator
			{
				m_Bounds = new Bounds2(transform.m_Position.xz - float3, transform.m_Position.xz + float3),
				m_LotSize = prefabBuildingData.m_LotSize,
				m_StartPosition = transform.m_Position.xz + float2 + @float,
				m_Right = xz,
				m_Forward = xz2,
				m_ZoneType = componentData.m_ZoneType,
				m_Validated = validated,
				m_BlockData = m_BlockData,
				m_ValidAreaData = m_ValidAreaData,
				m_Cells = m_Cells
			};
			m_SearchTree.Iterate(ref iterator);
			bool flag = (iterator.m_Directions & CellFlags.Roadside) != 0;
			for (int i = 0; i < validated.Length; i++)
			{
				flag &= validated[i];
			}
			validated.Dispose();
			return flag;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Condemned> __Game_Buildings_Condemned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(isReadOnly: true);
			__Game_Buildings_Condemned_RO_ComponentLookup = state.GetComponentLookup<Condemned>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
		}
	}

	private Game.Zones.UpdateCollectSystem m_ZoneUpdateCollectSystem;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private ToolSystem m_ToolSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_BuildingSettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Zones.UpdateCollectSystem>();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ZoneUpdateCollectSystem.isUpdated && !m_BuildingSettingsQuery.IsEmptyIgnoreFilter)
		{
			NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);
			NativeList<Entity> list = new NativeList<Entity>(Allocator.TempJob);
			JobHandle dependencies;
			NativeList<Bounds2> updatedBounds = m_ZoneUpdateCollectSystem.GetUpdatedBounds(readOnly: true, out dependencies);
			JobHandle dependencies2;
			FindSpawnableBuildingsJob jobData = new FindSpawnableBuildingsJob
			{
				m_Bounds = updatedBounds.AsDeferredJobArray(),
				m_SearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSignatureBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResultQueue = queue.AsParallelWriter()
			};
			CollectEntitiesJob jobData2 = new CollectEntitiesJob
			{
				m_Queue = queue,
				m_List = list
			};
			JobHandle dependencies3;
			CheckBuildingZonesJob jobData3 = new CheckBuildingZonesJob
			{
				m_CondemnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Condemned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ValidAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AbandonedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabPlaceholderBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
				m_BuildingConfigurationData = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
				m_Buildings = list.AsDeferredJobArray(),
				m_SearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_CommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle = jobData.Schedule(updatedBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			JobHandle jobHandle3 = jobData3.Schedule(list, 1, JobHandle.CombineDependencies(jobHandle2, dependencies3));
			queue.Dispose(jobHandle2);
			list.Dispose(jobHandle3);
			m_ZoneUpdateCollectSystem.AddBoundsReader(jobHandle);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
			m_ZoneSearchSystem.AddSearchTreeReader(jobHandle3);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle3);
			m_ModificationEndBarrier.AddJobHandleForProducer(jobHandle3);
			base.Dependency = jobHandle3;
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
	public ZoneCheckSystem()
	{
	}
}
