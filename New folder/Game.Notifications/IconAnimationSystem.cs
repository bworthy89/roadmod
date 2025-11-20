using System.Runtime.CompilerServices;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Notifications;

[CompilerGenerated]
public class IconAnimationSystem : GameSystemBase
{
	[BurstCompile]
	private struct IconAnimationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Animation> m_AnimationType;

		[ReadOnly]
		public float m_DeltaTime;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Animation> nativeArray2 = chunk.GetNativeArray(ref m_AnimationType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Animation value = nativeArray2[i];
				value.m_Timer += m_DeltaTime;
				switch (value.m_Type)
				{
				case AnimationType.MarkerAppear:
				case AnimationType.WarningAppear:
					if (value.m_Timer >= value.m_Duration)
					{
						Entity e2 = nativeArray[i];
						m_CommandBuffer.RemoveComponent<Animation>(unfilteredChunkIndex, e2);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, default(Updated));
					}
					break;
				case AnimationType.MarkerDisappear:
				case AnimationType.WarningResolve:
				case AnimationType.Transaction:
					if (value.m_Timer >= value.m_Duration)
					{
						Entity e = nativeArray[i];
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Deleted));
					}
					break;
				}
				nativeArray2[i] = value;
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

		public ComponentTypeHandle<Animation> __Game_Notifications_Animation_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Notifications_Animation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animation>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_AnimationQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_AnimationQuery = GetEntityQuery(ComponentType.ReadWrite<Animation>(), ComponentType.ReadOnly<Icon>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_AnimationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new IconAnimationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Animation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeltaTime = UnityEngine.Time.deltaTime,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_AnimationQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public IconAnimationSystem()
	{
	}
}
