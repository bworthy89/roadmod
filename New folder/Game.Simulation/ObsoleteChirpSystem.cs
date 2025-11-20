using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ObsoleteChirpSystem : GameSystemBase
{
	[BurstCompile]
	private struct ObsoleteChirpJob : IJob
	{
		[DeallocateOnJobCompletion]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public ComponentLookup<Game.Triggers.Chirp> m_Chirps;

		[ReadOnly]
		public LimitSettingData m_LimitSettingData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			m_Entities.Sort(new ChirpComparer
			{
				m_Chirps = m_Chirps
			});
			for (int i = 0; i < m_Entities.Length - m_LimitSettingData.m_MaxChirpsLimit; i++)
			{
				m_CommandBuffer.AddComponent<Deleted>(m_Entities[i]);
			}
		}
	}

	private struct ChirpComparer : IComparer<Entity>
	{
		[ReadOnly]
		public ComponentLookup<Game.Triggers.Chirp> m_Chirps;

		public int Compare(Entity x, Entity y)
		{
			return m_Chirps[x].m_CreationFrame.CompareTo(m_Chirps[y].m_CreationFrame);
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<Game.Triggers.Chirp> __Game_Triggers_Chirp_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Triggers_Chirp_RW_ComponentLookup = state.GetComponentLookup<Game.Triggers.Chirp>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_ChirpQuery;

	private EntityQuery m_LimitSettingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 65536;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Triggers.Chirp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Game.Triggers.LifePathEvent>(), ComponentType.Exclude<Temp>());
		m_LimitSettingQuery = GetEntityQuery(ComponentType.ReadOnly<LimitSettingData>());
		RequireForUpdate(m_ChirpQuery);
		RequireForUpdate(m_LimitSettingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ChirpQuery.CalculateEntityCount() > m_LimitSettingQuery.GetSingleton<LimitSettingData>().m_MaxChirpsLimit)
		{
			ObsoleteChirpJob jobData = new ObsoleteChirpJob
			{
				m_Entities = m_ChirpQuery.ToEntityArray(Allocator.TempJob),
				m_Chirps = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Triggers_Chirp_RW_ComponentLookup, ref base.CheckedStateRef),
				m_LimitSettingData = m_LimitSettingQuery.GetSingleton<LimitSettingData>(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			};
			base.Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public ObsoleteChirpSystem()
	{
	}
}
