using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

[CompilerGenerated]
public class ResidentPseudoRandomSystem : GameSystemBase
{
	[BurstCompile]
	private struct ResidentPseudoRandomJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Resident> m_ResidentType;

		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Resident> nativeArray = chunk.GetNativeArray(ref m_ResidentType);
			NativeArray<PseudoRandomSeed> nativeArray2 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Resident resident = nativeArray[i];
				ref PseudoRandomSeed reference = ref nativeArray2.ElementAt(i);
				if (m_CitizenData.TryGetComponent(resident.m_Citizen, out var componentData))
				{
					Random random = componentData.GetPseudoRandom(CitizenPseudoRandom.SpawnResident);
					reference = new PseudoRandomSeed(ref random);
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
		public ComponentTypeHandle<Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Resident>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<Resident>(), ComponentType.ReadWrite<PseudoRandomSeed>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!(m_LoadGameSystem.context.version >= Version.residentPseudoRandomFix) && !m_Query.IsEmptyIgnoreFilter)
		{
			ResidentPseudoRandomJob jobData = new ResidentPseudoRandomJob
			{
				m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CitizenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_Query, base.Dependency);
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
	public ResidentPseudoRandomSystem()
	{
	}
}
