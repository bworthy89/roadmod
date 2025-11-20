using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class ChirpLikeCountSystem : GameSystemBase
{
	[BurstCompile]
	private struct LikeCountUpdateJob : IJobChunk
	{
		public ComponentTypeHandle<Chirp> m_ChirpType;

		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrame;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(0);
			NativeArray<Chirp> nativeArray = chunk.GetNativeArray(ref m_ChirpType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref Chirp reference = ref nativeArray.ElementAt(i);
				if (reference.m_InactiveFrame > reference.m_CreationFrame && m_SimulationFrame <= reference.m_InactiveFrame && !(random.NextFloat() < reference.m_ContinuousFactor))
				{
					float num = (1f * (float)m_SimulationFrame - (float)reference.m_CreationFrame) / (float)(reference.m_InactiveFrame - reference.m_CreationFrame);
					reference.m_Likes = math.max(reference.m_Likes, (uint)((float)reference.m_TargetLikes * math.lerp(0f, 1f, 1f - math.pow(1f - num, reference.m_ViralFactor))));
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
		public ComponentTypeHandle<Chirp> __Game_Triggers_Chirp_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Triggers_Chirp_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Chirp>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_ChirpQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ChirpQuery = GetEntityQuery(ComponentType.ReadWrite<Chirp>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_ChirpQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		LikeCountUpdateJob jobData = new LikeCountUpdateJob
		{
			m_ChirpType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Triggers_Chirp_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ChirpQuery, base.Dependency);
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
	public ChirpLikeCountSystem()
	{
	}
}
