using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResearchFacilityAISystem : GameSystemBase
{
	[BurstCompile]
	private struct ResearchFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		public ComponentTypeHandle<PointOfInterest> m_PointOfInterestType;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PointOfInterest> nativeArray = chunk.GetNativeArray(ref m_PointOfInterestType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (CollectionUtils.TryGet(nativeArray, i, out var value) && BuildingUtils.GetEfficiency(bufferAccessor, i) > 0f && (!value.m_IsValid || random.NextInt(10) == 0))
				{
					float x = random.NextFloat(MathF.PI * 2f);
					value.m_Position.x = math.sin(x) * 100000f;
					value.m_Position.y = random.NextFloat();
					value.m_Position.y = value.m_Position.y * value.m_Position.y * 900000f + 100000f;
					value.m_Position.z = math.cos(x) * 100000f;
					value.m_IsValid = true;
					nativeArray[i] = value;
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
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		public ComponentTypeHandle<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Common_PointOfInterest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PointOfInterest>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 96;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ResearchFacility>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new ResearchFacilityTickJob
		{
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PointOfInterestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next()
		}, m_BuildingQuery, base.Dependency);
		base.Dependency = dependency;
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
	public ResearchFacilityAISystem()
	{
	}
}
