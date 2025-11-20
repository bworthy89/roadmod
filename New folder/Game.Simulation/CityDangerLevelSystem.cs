using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.City;
using Game.Common;
using Game.Events;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityDangerLevelSystem : GameSystemBase
{
	[BurstCompile]
	private struct DangerLevelJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Events.DangerLevel> m_DangerLevelType;

		public NativeAccumulator<MaxFloat>.ParallelWriter m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Events.DangerLevel> nativeArray = chunk.GetNativeArray(ref m_DangerLevelType);
			for (int i = 0; i < chunk.Count; i++)
			{
				m_Result.Accumulate(new MaxFloat(nativeArray[i].m_DangerLevel));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateCityJob : IJob
	{
		public ComponentLookup<Game.City.DangerLevel> m_DangerLevel;

		public Entity m_City;

		[ReadOnly]
		public NativeAccumulator<MaxFloat> m_Result;

		public void Execute()
		{
			m_DangerLevel[m_City] = new Game.City.DangerLevel
			{
				m_DangerLevel = m_Result.GetResult().m_Value
			};
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Events.DangerLevel> __Game_Events_DangerLevel_RO_ComponentTypeHandle;

		public ComponentLookup<Game.City.DangerLevel> __Game_City_DangerLevel_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_DangerLevel_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.DangerLevel>(isReadOnly: true);
			__Game_City_DangerLevel_RW_ComponentLookup = state.GetComponentLookup<Game.City.DangerLevel>();
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_DangerLevelQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_DangerLevelQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Events.DangerLevel>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate<Game.City.DangerLevel>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeAccumulator<MaxFloat> result = new NativeAccumulator<MaxFloat>(Allocator.TempJob);
		if (!m_DangerLevelQuery.IsEmptyIgnoreFilter)
		{
			DangerLevelJob jobData = new DangerLevelJob
			{
				m_DangerLevelType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_DangerLevel_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Result = result.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_DangerLevelQuery, base.Dependency);
		}
		UpdateCityJob jobData2 = new UpdateCityJob
		{
			m_DangerLevel = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_DangerLevel_RW_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_Result = result
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		result.Dispose(base.Dependency);
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
	public CityDangerLevelSystem()
	{
	}
}
