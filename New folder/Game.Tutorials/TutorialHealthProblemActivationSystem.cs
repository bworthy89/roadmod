using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialHealthProblemActivationSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckProblemsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblemActivationData> m_ActivationType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_HealthProblemChunks;

		public bool m_NoHospital;

		public bool m_NoCemetery;

		public EntityCommandBuffer.ParallelWriter m_Writer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<HealthProblemActivationData> nativeArray = chunk.GetNativeArray(ref m_ActivationType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityTypeHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (Execute(nativeArray[i]))
				{
					m_Writer.AddComponent<TutorialActivated>(unfilteredChunkIndex, nativeArray2[i]);
				}
			}
		}

		private bool Execute(HealthProblemActivationData data)
		{
			if ((data.m_Require & HealthProblemFlags.Dead) != HealthProblemFlags.None)
			{
				if (!m_NoCemetery)
				{
					return false;
				}
			}
			else if (!m_NoHospital)
			{
				return false;
			}
			int num = 0;
			for (int i = 0; i < m_HealthProblemChunks.Length; i++)
			{
				NativeArray<HealthProblem> nativeArray = m_HealthProblemChunks[i].GetNativeArray(ref m_HealthProblemType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					if ((nativeArray[j].m_Flags & data.m_Require) != HealthProblemFlags.None)
					{
						num++;
					}
					if (num >= data.m_RequiredCount)
					{
						return true;
					}
				}
			}
			return false;
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
		public ComponentTypeHandle<HealthProblemActivationData> __Game_Tutorials_HealthProblemActivationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tutorials_HealthProblemActivationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblemActivationData>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
		}
	}

	protected EntityCommandBufferSystem m_BarrierSystem;

	private EntityQuery m_TutorialQuery;

	private EntityQuery m_HealthProblemQuery;

	private EntityQuery m_MedicalClinicQuery;

	private EntityQuery m_MedicalClinicUnlockedQuery;

	private EntityQuery m_CemeteryQuery;

	private EntityQuery m_CemeteryUnlockedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_TutorialQuery = GetEntityQuery(ComponentType.ReadOnly<HealthProblemActivationData>(), ComponentType.Exclude<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>());
		m_HealthProblemQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<HealthProblem>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_MedicalClinicQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.Hospital>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_MedicalClinicUnlockedQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<HospitalData>(), ComponentType.ReadOnly<BuildingData>(), ComponentType.Exclude<Locked>());
		m_CemeteryQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CemeteryUnlockedQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<DeathcareFacilityData>(), ComponentType.ReadOnly<BuildingData>(), ComponentType.Exclude<Locked>());
		RequireForUpdate(m_HealthProblemQuery);
		RequireForUpdate(m_TutorialQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = m_MedicalClinicQuery.IsEmptyIgnoreFilter && !m_MedicalClinicUnlockedQuery.IsEmpty;
		bool flag2 = m_CemeteryQuery.IsEmptyIgnoreFilter && !m_CemeteryUnlockedQuery.IsEmpty;
		if (flag || flag2)
		{
			JobHandle outJobHandle;
			CheckProblemsJob jobData = new CheckProblemsJob
			{
				m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ActivationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_HealthProblemActivationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HealthProblemChunks = m_HealthProblemQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_NoHospital = flag,
				m_NoCemetery = flag2,
				m_Writer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TutorialQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			jobData.m_HealthProblemChunks.Dispose(base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
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
	public TutorialHealthProblemActivationSystem()
	{
	}
}
