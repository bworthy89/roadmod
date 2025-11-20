using System.Runtime.CompilerServices;
using Game.Common;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyZonesSystem : GameSystemBase
{
	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<Cell> m_ZoneCellType;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<Cell> bufferAccessor = chunk.GetBufferAccessor(ref m_ZoneCellType);
			if (bufferAccessor.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity, temp);
					}
					else if (temp.m_Original != Entity.Null)
					{
						Update(unfilteredChunkIndex, entity, temp, bufferAccessor[i]);
					}
					else
					{
						Create(unfilteredChunkIndex, entity);
					}
				}
				return;
			}
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				Temp temp2 = nativeArray2[j];
				if ((temp2.m_Flags & TempFlags.Delete) != 0)
				{
					Delete(unfilteredChunkIndex, entity2, temp2);
				}
				else if (temp2.m_Original != Entity.Null)
				{
					Update(unfilteredChunkIndex, entity2, temp2);
				}
				else
				{
					Create(unfilteredChunkIndex, entity2);
				}
			}
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_Cells.HasBuffer(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, bool updateOriginal = true)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
				if (!updateOriginal)
				{
					m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(BatchesUpdated));
				}
			}
			if (updateOriginal && m_Cells.HasBuffer(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Updated));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, DynamicBuffer<Cell> cells)
		{
			if (m_Cells.HasBuffer(temp.m_Original))
			{
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[temp.m_Original];
				DynamicBuffer<Cell> dynamicBuffer2 = m_CommandBuffer.SetBuffer<Cell>(chunkIndex, temp.m_Original);
				dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Cell cell = cells[i];
					Cell value = dynamicBuffer[i];
					if ((cell.m_State & CellFlags.Selected) != CellFlags.None)
					{
						if ((value.m_State & CellFlags.Overridden) != CellFlags.None)
						{
							if (value.m_Zone.Equals(cell.m_Zone))
							{
								value.m_State &= ~CellFlags.Overridden;
							}
						}
						else
						{
							value.m_Zone = cell.m_Zone;
						}
					}
					dynamicBuffer2[i] = value;
				}
			}
			Update(chunkIndex, entity, temp);
		}

		private void Create(int chunkIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Temp>(chunkIndex, entity);
			m_CommandBuffer.AddComponent(chunkIndex, entity, in m_AppliedTypes);
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Cell> __Game_Zones_Cell_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferTypeHandle = state.GetBufferTypeHandle<Cell>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_TempQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Block>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneCellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_TempQuery, base.Dependency);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public ApplyZonesSystem()
	{
	}
}
