using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyWaterSourcesSystem : GameSystemBase
{
	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> m_WaterSourceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Temp temp = nativeArray2[i];
				if ((temp.m_Flags & TempFlags.Cancel) != 0)
				{
					Cancel(unfilteredChunkIndex, entity, temp);
				}
				else if ((temp.m_Flags & TempFlags.Delete) != 0)
				{
					Delete(unfilteredChunkIndex, entity, temp);
				}
				else if (m_WaterSourceData.HasComponent(temp.m_Original))
				{
					UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_WaterSourceData, updateValue: true);
					UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_TransformData, updateValue: true);
					Update(unfilteredChunkIndex, entity, temp);
				}
				else
				{
					Create(unfilteredChunkIndex, entity, temp);
				}
			}
		}

		private void Cancel(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(BatchesUpdated));
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_WaterSourceData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void UpdateComponent<T>(int chunkIndex, Entity entity, Entity original, ComponentLookup<T> data, bool updateValue) where T : unmanaged, IComponentData
		{
			if (data.HasComponent(entity))
			{
				if (data.HasComponent(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetComponent(chunkIndex, original, data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, data[entity]);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, default(T));
				}
			}
			else if (data.HasComponent(original))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
			}
		}

		private void UpdateComponent<T>(int chunkIndex, Entity entity, Entity original, BufferLookup<T> data, bool updateValue) where T : unmanaged, IBufferElementData
		{
			if (data.HasBuffer(entity))
			{
				if (data.HasBuffer(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
				}
				else
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original);
				}
			}
			else if (data.HasBuffer(original))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
			}
		}

		private void Update(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(BatchesUpdated));
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Updated));
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Create(int chunkIndex, Entity entity, Temp temp)
		{
			m_CommandBuffer.RemoveComponent<Temp>(chunkIndex, entity);
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Updated));
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Created));
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
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RO_ComponentLookup = state.GetComponentLookup<Game.Simulation.WaterSourceData>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_TempQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(), ComponentType.Exclude<PrefabRef>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentLookup, ref base.CheckedStateRef),
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
	public ApplyWaterSourcesSystem()
	{
	}
}
