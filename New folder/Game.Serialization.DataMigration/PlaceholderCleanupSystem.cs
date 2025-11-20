using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

[CompilerGenerated]
public class PlaceholderCleanupSystem : GameSystemBase
{
	[BurstCompile]
	private struct PlaceholderCleanupJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		[ReadOnly]
		public ComponentTypeSet m_ComponentSet;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<IconElement> bufferAccessor = chunk.GetBufferAccessor(ref m_IconElementType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, nativeArray[i], in m_ComponentSet);
				if (CollectionUtils.TryGet(bufferAccessor, i, out var value))
				{
					for (int j = 0; j < value.Length; j++)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, value[j].m_Icon, default(Deleted));
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
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private DeserializationBarrier m_DeserializationBarrier;

	private EntityQuery m_Query;

	private ComponentTypeSet m_ComponentSet;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_DeserializationBarrier = base.World.GetOrCreateSystemManaged<DeserializationBarrier>();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<Placeholder>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Renter>());
		m_ComponentSet = new ComponentTypeSet(new ComponentType[7]
		{
			ComponentType.ReadWrite<Renter>(),
			ComponentType.ReadWrite<PropertyToBeOnMarket>(),
			ComponentType.ReadWrite<PropertyOnMarket>(),
			ComponentType.ReadWrite<ElectricityConsumer>(),
			ComponentType.ReadWrite<WaterConsumer>(),
			ComponentType.ReadWrite<GarbageProducer>(),
			ComponentType.ReadWrite<TelecomConsumer>()
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!(m_LoadGameSystem.context.version >= Version.placeholderCleanup) && !m_Query.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PlaceholderCleanupJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ComponentSet = m_ComponentSet,
				m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_Query, base.Dependency);
			m_DeserializationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
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
	public PlaceholderCleanupSystem()
	{
	}
}
