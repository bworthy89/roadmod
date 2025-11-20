using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateZonesSystem : GameSystemBase
{
	private struct CellData
	{
		public int2 m_Location;

		public ZoneType m_ZoneType;
	}

	private struct BaseCell
	{
		public Entity m_Block;

		public int2 m_Location;
	}

	[BurstCompile]
	private struct FillBlocksListJob : IJobChunk
	{
		private struct MarqueeIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public ZoneType m_NewZoneType;

			public bool m_Overwrite;

			public ComponentLookup<Block> m_BlockData;

			public BufferLookup<Cell> m_Cells;

			public NativeParallelMultiHashMap<Entity, CellData> m_ZonedCells;

			public NativeList<Entity> m_ZonedBlocks;

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
				Block block = m_BlockData[blockEntity];
				Quad2 quad = ZoneUtils.CalculateCorners(block);
				if (!MathUtils.Intersect(m_Quad, quad))
				{
					return;
				}
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
				CellData item = default(CellData);
				item.m_ZoneType = m_NewZoneType;
				item.m_Location.y = 0;
				while (item.m_Location.y < block.m_Size.y)
				{
					item.m_Location.x = 0;
					while (item.m_Location.x < block.m_Size.x)
					{
						int index = item.m_Location.y * block.m_Size.x + item.m_Location.x;
						Cell cell = dynamicBuffer[index];
						if ((cell.m_State & CellFlags.Visible) != CellFlags.None)
						{
							float3 cellPosition = ZoneUtils.GetCellPosition(block, item.m_Location);
							if (MathUtils.Intersect(m_Quad, cellPosition.xz) && (m_Overwrite | cell.m_Zone.Equals(ZoneType.None)))
							{
								if (!m_ZonedCells.TryGetFirstValue(blockEntity, out var _, out var _))
								{
									m_ZonedBlocks.Add(in blockEntity);
								}
								m_ZonedCells.Add(blockEntity, item);
							}
						}
						item.m_Location.x++;
					}
					item.m_Location.y++;
				}
			}
		}

		private struct BaseLineIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Line2.Segment m_Line;

			public ComponentLookup<Block> m_BlockData;

			public BufferLookup<Cell> m_Cells;

			public NativeList<BaseCell> m_BaseCells;

			public bool Intersect(Bounds2 bounds)
			{
				float2 t;
				return MathUtils.Intersect(bounds, m_Line, out t);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Line, out var t))
				{
					return;
				}
				Block block = m_BlockData[blockEntity];
				int2 cellIndex = ZoneUtils.GetCellIndex(block, m_Line.a);
				int2 cellIndex2 = ZoneUtils.GetCellIndex(block, m_Line.b);
				int2 @int = math.max(math.min(cellIndex, cellIndex2), 0);
				int2 int2 = math.min(math.max(cellIndex, cellIndex2), block.m_Size - 1);
				if (!math.all(int2 >= @int))
				{
					return;
				}
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
				Quad2 quad = ZoneUtils.CalculateCorners(block);
				float2 @float = new float2(1f) / block.m_Size;
				Quad2 quad2 = new Quad2
				{
					a = math.lerp(quad.a, quad.d, (float)@int.y * @float.y),
					b = math.lerp(quad.b, quad.c, (float)@int.y * @float.y)
				};
				for (int i = @int.y; i <= int2.y; i++)
				{
					quad2.d = math.lerp(quad.a, quad.d, (float)(i + 1) * @float.y);
					quad2.c = math.lerp(quad.b, quad.c, (float)(i + 1) * @float.y);
					Quad2 quad3 = new Quad2
					{
						a = math.lerp(quad2.a, quad2.b, (float)@int.x * @float.x),
						d = math.lerp(quad2.d, quad2.c, (float)@int.x * @float.x)
					};
					for (int j = @int.x; j <= int2.x; j++)
					{
						quad3.b = math.lerp(quad2.a, quad2.b, (float)(j + 1) * @float.x);
						quad3.c = math.lerp(quad2.d, quad2.c, (float)(j + 1) * @float.x);
						if ((dynamicBuffer[i * block.m_Size.x + j].m_State & CellFlags.Visible) != CellFlags.None && MathUtils.Intersect(quad3, m_Line, out t))
						{
							ref NativeList<BaseCell> reference = ref m_BaseCells;
							BaseCell value = new BaseCell
							{
								m_Block = blockEntity,
								m_Location = new int2(j, i)
							};
							reference.Add(in value);
						}
						quad3.a = quad3.b;
						quad3.d = quad3.c;
					}
					quad2.a = quad2.d;
					quad2.b = quad2.c;
				}
			}
		}

		private struct FloodFillIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Block m_BaseBlockData;

			public float2 m_Position;

			public CellFlags m_StateMask;

			public ZoneType m_OldZoneType;

			public ZoneType m_NewZoneType;

			public bool m_Overwrite;

			public ComponentLookup<Block> m_BlockData;

			public BufferLookup<Cell> m_Cells;

			public NativeParallelMultiHashMap<Entity, CellData> m_ZonedCells;

			public NativeList<Entity> m_ZonedBlocks;

			public int m_FoundCells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Position);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Position))
				{
					return;
				}
				Block block = m_BlockData[blockEntity];
				CellData item = default(CellData);
				item.m_Location = ZoneUtils.GetCellIndex(block, m_Position);
				item.m_ZoneType = m_NewZoneType;
				if (!math.all((item.m_Location >= 0) & (item.m_Location < block.m_Size)))
				{
					return;
				}
				Cell cell = m_Cells[blockEntity][item.m_Location.y * block.m_Size.x + item.m_Location.x];
				if ((((cell.m_State & (CellFlags.Visible | CellFlags.Occupied)) == m_StateMask) & cell.m_Zone.Equals(m_OldZoneType)) && ZoneUtils.CanShareCells(m_BaseBlockData, block))
				{
					if (!m_ZonedCells.TryGetFirstValue(blockEntity, out var _, out var _))
					{
						m_ZonedBlocks.Add(in blockEntity);
					}
					if (!m_Overwrite && !cell.m_Zone.Equals(ZoneType.None))
					{
						item.m_ZoneType = cell.m_Zone;
					}
					m_ZonedCells.Add(blockEntity, item);
					m_FoundCells++;
				}
			}
		}

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Zoning> m_ZoningType;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		public NativeParallelMultiHashMap<Entity, CellData> m_ZonedCells;

		public NativeList<Entity> m_ZonedBlocks;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<Zoning> nativeArray2 = chunk.GetNativeArray(ref m_ZoningType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition definitionData = nativeArray[i];
				Zoning zoningData = nativeArray2[i];
				if (definitionData.m_Prefab != Entity.Null)
				{
					ZoneData zoneData = m_ZoneData[definitionData.m_Prefab];
					if ((zoningData.m_Flags & ZoningFlags.FloodFill) != 0)
					{
						FloodFillBlocks(definitionData, zoningData, zoneData);
					}
					if ((zoningData.m_Flags & ZoningFlags.Paint) != 0)
					{
						PaintBlocks(definitionData, zoningData, zoneData);
					}
					if ((zoningData.m_Flags & ZoningFlags.Marquee) != 0)
					{
						MarqueeBlocks(definitionData, zoningData, zoneData);
					}
				}
			}
		}

		private void MarqueeBlocks(CreationDefinition definitionData, Zoning zoningData, ZoneData zoneData)
		{
			ZoneType newZoneType;
			if ((zoningData.m_Flags & ZoningFlags.Zone) != 0)
			{
				newZoneType = zoneData.m_ZoneType;
			}
			else
			{
				if ((zoningData.m_Flags & ZoningFlags.Dezone) == 0)
				{
					return;
				}
				newZoneType = ZoneType.None;
			}
			MarqueeIterator iterator = new MarqueeIterator
			{
				m_Bounds = MathUtils.Bounds(zoningData.m_Position.xz),
				m_Quad = zoningData.m_Position.xz,
				m_NewZoneType = newZoneType,
				m_Overwrite = ((zoningData.m_Flags & ZoningFlags.Overwrite) != 0),
				m_BlockData = m_BlockData,
				m_Cells = m_Cells,
				m_ZonedCells = m_ZonedCells,
				m_ZonedBlocks = m_ZonedBlocks
			};
			m_SearchTree.Iterate(ref iterator);
		}

		private void PaintBlocks(CreationDefinition definitionData, Zoning zoningData, ZoneData zoneData)
		{
			ZoneType zoneType;
			if ((zoningData.m_Flags & ZoningFlags.Zone) != 0)
			{
				zoneType = zoneData.m_ZoneType;
			}
			else
			{
				if ((zoningData.m_Flags & ZoningFlags.Dezone) == 0)
				{
					return;
				}
				zoneType = ZoneType.None;
			}
			NativeList<BaseCell> baseCells = new NativeList<BaseCell>(10, Allocator.Temp);
			AddCells(zoningData.m_Position.xz.bc, baseCells);
			CellData item = default(CellData);
			for (int i = 0; i < baseCells.Length; i++)
			{
				BaseCell baseCell = baseCells[i];
				Block block = m_BlockData[baseCell.m_Block];
				item.m_Location = baseCell.m_Location;
				item.m_ZoneType = zoneType;
				if (!math.all((item.m_Location >= 0) & (item.m_Location < block.m_Size)))
				{
					continue;
				}
				Cell cell = m_Cells[baseCell.m_Block][item.m_Location.y * block.m_Size.x + item.m_Location.x];
				if ((cell.m_State & CellFlags.Visible) != CellFlags.None)
				{
					if (!m_ZonedCells.TryGetFirstValue(baseCell.m_Block, out var _, out var _))
					{
						m_ZonedBlocks.Add(in baseCell.m_Block);
					}
					if ((zoningData.m_Flags & ZoningFlags.Overwrite) == 0 && !cell.m_Zone.Equals(ZoneType.None))
					{
						item.m_ZoneType = cell.m_Zone;
					}
					m_ZonedCells.Add(baseCell.m_Block, item);
				}
			}
		}

		private void FloodFillBlocks(CreationDefinition definitionData, Zoning zoningData, ZoneData zoneData)
		{
			ZoneType newZoneType;
			if ((zoningData.m_Flags & ZoningFlags.Zone) != 0)
			{
				newZoneType = zoneData.m_ZoneType;
			}
			else
			{
				if ((zoningData.m_Flags & ZoningFlags.Dezone) == 0)
				{
					return;
				}
				newZoneType = ZoneType.None;
			}
			NativeParallelHashSet<int> nativeParallelHashSet = new NativeParallelHashSet<int>(1000, Allocator.Temp);
			NativeList<BaseCell> baseCells = new NativeList<BaseCell>(10, Allocator.Temp);
			NativeList<int2> nativeList = new NativeList<int2>(1000, Allocator.Temp);
			AddCells(zoningData.m_Position.xz.bc, baseCells);
			for (int i = 0; i < baseCells.Length; i++)
			{
				BaseCell baseCell = baseCells[i];
				Block block = m_BlockData[baseCell.m_Block];
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[baseCell.m_Block];
				int2 value = baseCell.m_Location;
				Cell cell = dynamicBuffer[value.y * block.m_Size.x + value.x];
				FloodFillIterator iterator = new FloodFillIterator
				{
					m_BaseBlockData = block,
					m_StateMask = (cell.m_State & (CellFlags.Visible | CellFlags.Occupied)),
					m_OldZoneType = cell.m_Zone,
					m_NewZoneType = newZoneType,
					m_Overwrite = ((zoningData.m_Flags & ZoningFlags.Overwrite) != 0),
					m_BlockData = m_BlockData,
					m_Cells = m_Cells,
					m_ZonedCells = m_ZonedCells,
					m_ZonedBlocks = m_ZonedBlocks
				};
				nativeParallelHashSet.Add(PackToInt(value));
				nativeList.Add(in value);
				int num = 0;
				while (num < nativeList.Length)
				{
					value = nativeList[num++];
					iterator.m_Position = ZoneUtils.GetCellPosition(block, value).xz;
					iterator.m_FoundCells = 0;
					m_SearchTree.Iterate(ref iterator);
					if (iterator.m_FoundCells != 0)
					{
						int2 value2 = value;
						int2 value3 = value;
						int2 value4 = value;
						int2 value5 = value;
						value2.x--;
						value3.y--;
						value4.x++;
						value5.y++;
						if (nativeParallelHashSet.Add(PackToInt(value2)))
						{
							nativeList.Add(in value2);
						}
						if (nativeParallelHashSet.Add(PackToInt(value3)))
						{
							nativeList.Add(in value3);
						}
						if (nativeParallelHashSet.Add(PackToInt(value4)))
						{
							nativeList.Add(in value4);
						}
						if (nativeParallelHashSet.Add(PackToInt(value5)))
						{
							nativeList.Add(in value5);
						}
					}
				}
				nativeParallelHashSet.Clear();
				nativeList.Clear();
			}
		}

		private static int PackToInt(int2 cellIndex)
		{
			return (cellIndex.y << 16) | (cellIndex.x & 0xFFFF);
		}

		private void AddCells(Line2.Segment line, NativeList<BaseCell> baseCells)
		{
			BaseLineIterator iterator = new BaseLineIterator
			{
				m_Line = line,
				m_BlockData = m_BlockData,
				m_Cells = m_Cells,
				m_BaseCells = baseCells
			};
			m_SearchTree.Iterate(ref iterator);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CreateBlocksJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ZoneBlockData> m_ZoneBlockDataData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, CellData> m_ZonedCells;

		[ReadOnly]
		public NativeArray<Entity> m_ZonedBlocks;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_ZonedBlocks[index];
			Block component = m_BlockData[entity];
			PrefabRef component2 = m_PrefabRefData[entity];
			DynamicBuffer<Cell> dynamicBuffer = m_Cells[entity];
			ZoneBlockData zoneBlockData = m_ZoneBlockDataData[component2.m_Prefab];
			Entity e = m_CommandBuffer.CreateEntity(index, zoneBlockData.m_Archetype);
			m_CommandBuffer.SetComponent(index, e, component2);
			m_CommandBuffer.SetComponent(index, e, component);
			DynamicBuffer<Cell> dynamicBuffer2 = m_CommandBuffer.SetBuffer<Cell>(index, e);
			Temp component3 = new Temp
			{
				m_Original = entity
			};
			m_CommandBuffer.AddComponent(index, e, component3);
			m_CommandBuffer.AddComponent(index, entity, default(Hidden));
			m_CommandBuffer.AddComponent(index, entity, default(BatchesUpdated));
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				dynamicBuffer2.Add(dynamicBuffer[i]);
			}
			if (m_ZonedCells.TryGetFirstValue(entity, out var item, out var it))
			{
				do
				{
					int index2 = item.m_Location.y * component.m_Size.x + item.m_Location.x;
					Cell value = dynamicBuffer2[index2];
					value.m_State |= CellFlags.Selected;
					value.m_Zone = item.m_ZoneType;
					dynamicBuffer2[index2] = value;
				}
				while (m_ZonedCells.TryGetNextValue(out item, ref it));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Zoning> __Game_Tools_Zoning_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneBlockData> __Game_Prefabs_ZoneBlockData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_Zoning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Zoning>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ZoneBlockData_RO_ComponentLookup = state.GetComponentLookup<ZoneBlockData>(isReadOnly: true);
		}
	}

	private SearchSystem m_ZoneSearchSystem;

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<Zoning>(), ComponentType.ReadOnly<Updated>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelMultiHashMap<Entity, CellData> zonedCells = new NativeParallelMultiHashMap<Entity, CellData>(1000, Allocator.TempJob);
		NativeList<Entity> nativeList = new NativeList<Entity>(20, Allocator.TempJob);
		JobHandle dependencies;
		FillBlocksListJob jobData = new FillBlocksListJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Zoning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_SearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_ZonedCells = zonedCells,
			m_ZonedBlocks = nativeList
		};
		CreateBlocksJob jobData2 = new CreateBlocksJob
		{
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneBlockDataData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneBlockData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_ZonedCells = zonedCells,
			m_ZonedBlocks = nativeList.AsDeferredJobArray(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.Schedule(jobData, m_DefinitionQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = jobData2.Schedule(nativeList, 1, jobHandle);
		zonedCells.Dispose(jobHandle2);
		nativeList.Dispose(jobHandle2);
		m_ZoneSearchSystem.AddSearchTreeReader(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
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
	public GenerateZonesSystem()
	{
	}
}
