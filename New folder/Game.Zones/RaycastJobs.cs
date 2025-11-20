using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Zones;

public static class RaycastJobs
{
	[BurstCompile]
	public struct FindZoneBlockJob : IJobParallelFor
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public float2 m_Position;

			public Entity m_Block;

			public int2 m_CellIndex;

			public ComponentLookup<Block> m_BlockData;

			public BufferLookup<Cell> m_Cells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Position);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (MathUtils.Intersect(bounds, m_Position))
				{
					Block block = m_BlockData[blockEntity];
					int2 cellIndex = ZoneUtils.GetCellIndex(block, m_Position);
					if (math.all((cellIndex >= 0) & (cellIndex < block.m_Size)) && (m_Cells[blockEntity][cellIndex.y * block.m_Size.x + cellIndex.x].m_State & (CellFlags.Shared | CellFlags.Visible)) == CellFlags.Visible)
					{
						m_Block = blockEntity;
						m_CellIndex = cellIndex;
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		[ReadOnly]
		public NativeArray<RaycastResult> m_TerrainResults;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			int index2 = index % m_Input.Length;
			RaycastInput raycastInput = m_Input[index2];
			RaycastResult value = m_TerrainResults[index];
			if ((raycastInput.m_TypeMask & TypeMask.Zones) != TypeMask.None && !(value.m_Owner == Entity.Null))
			{
				Iterator iterator = new Iterator
				{
					m_Position = value.m_Hit.m_HitPosition.xz,
					m_BlockData = m_BlockData,
					m_Cells = m_Cells
				};
				m_SearchTree.Iterate(ref iterator);
				if (iterator.m_Block != Entity.Null)
				{
					value.m_Owner = iterator.m_Block;
					value.m_Hit.m_CellIndex = iterator.m_CellIndex;
					value.m_Hit.m_NormalizedDistance -= 1f / math.max(1f, MathUtils.Length(raycastInput.m_Line));
					m_Results.Accumulate(index2, value);
				}
			}
		}
	}
}
