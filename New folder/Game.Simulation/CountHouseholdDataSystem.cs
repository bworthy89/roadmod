using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Economy;
using Game.Objects;
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
public class CountHouseholdDataSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct HouseholdNeedData : IAccumulable<HouseholdNeedData>
	{
		public int m_HouseholdNeed;

		public void Accumulate(HouseholdNeedData other)
		{
			m_HouseholdNeed += other.m_HouseholdNeed;
		}
	}

	public struct HouseholdData : IAccumulable<HouseholdData>, ISerializable
	{
		public int m_MovingInHouseholdCount;

		public int m_MovingInCitizenCount;

		public int m_MovingAwayHouseholdCount;

		public int m_CommuterHouseholdCount;

		public int m_TouristCitizenCount;

		public int m_HomelessHouseholdCount;

		public int m_HomelessCitizenCount;

		public int m_MovedInHouseholdCount;

		public int m_MovedInCitizenCount;

		public int m_ChildrenCount;

		public int m_TeenCount;

		public int m_AdultCount;

		public int m_SeniorCount;

		public int m_StudentCount;

		public int m_UneducatedCount;

		public int m_PoorlyEducatedCount;

		public int m_EducatedCount;

		public int m_WellEducatedCount;

		public int m_HighlyEducatedCount;

		public int m_WorkableCitizenCount;

		public int m_CityWorkerCount;

		public int m_DeadCitizenCount;

		public long m_TotalMovedInCitizenHappiness;

		public long m_TotalMovedInCitizenWellbeing;

		public long m_TotalMovedInCitizenHealth;

		public int m_EmployableByEducation0;

		public int m_EmployableByEducation1;

		public int m_EmployableByEducation2;

		public int m_EmployableByEducation3;

		public int m_EmployableByEducation4;

		public void Accumulate(HouseholdData other)
		{
			m_MovingInHouseholdCount += other.m_MovingInHouseholdCount;
			m_MovingInCitizenCount += other.m_MovingInCitizenCount;
			m_MovingAwayHouseholdCount += other.m_MovingAwayHouseholdCount;
			m_CommuterHouseholdCount += other.m_CommuterHouseholdCount;
			m_TouristCitizenCount += other.m_TouristCitizenCount;
			m_HomelessHouseholdCount += other.m_HomelessHouseholdCount;
			m_HomelessCitizenCount += other.m_HomelessCitizenCount;
			m_MovedInHouseholdCount += other.m_MovedInHouseholdCount;
			m_MovedInCitizenCount += other.m_MovedInCitizenCount;
			m_ChildrenCount += other.m_ChildrenCount;
			m_TeenCount += other.m_TeenCount;
			m_AdultCount += other.m_AdultCount;
			m_SeniorCount += other.m_SeniorCount;
			m_StudentCount += other.m_StudentCount;
			m_UneducatedCount += other.m_UneducatedCount;
			m_PoorlyEducatedCount += other.m_PoorlyEducatedCount;
			m_EducatedCount += other.m_EducatedCount;
			m_WellEducatedCount += other.m_WellEducatedCount;
			m_HighlyEducatedCount += other.m_HighlyEducatedCount;
			m_WorkableCitizenCount += other.m_WorkableCitizenCount;
			m_CityWorkerCount += other.m_CityWorkerCount;
			m_DeadCitizenCount += other.m_DeadCitizenCount;
			m_TotalMovedInCitizenHappiness += other.m_TotalMovedInCitizenHappiness;
			m_TotalMovedInCitizenWellbeing += other.m_TotalMovedInCitizenWellbeing;
			m_TotalMovedInCitizenHealth += other.m_TotalMovedInCitizenHealth;
			m_EmployableByEducation0 += other.m_EmployableByEducation0;
			m_EmployableByEducation1 += other.m_EmployableByEducation1;
			m_EmployableByEducation2 += other.m_EmployableByEducation2;
			m_EmployableByEducation3 += other.m_EmployableByEducation3;
			m_EmployableByEducation4 += other.m_EmployableByEducation4;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int value = m_MovingInHouseholdCount;
			writer.Write(value);
			int value2 = m_MovingInCitizenCount;
			writer.Write(value2);
			int value3 = m_MovingAwayHouseholdCount;
			writer.Write(value3);
			int value4 = m_CommuterHouseholdCount;
			writer.Write(value4);
			int value5 = m_TouristCitizenCount;
			writer.Write(value5);
			int value6 = m_HomelessHouseholdCount;
			writer.Write(value6);
			int value7 = m_HomelessCitizenCount;
			writer.Write(value7);
			int value8 = m_MovedInHouseholdCount;
			writer.Write(value8);
			int value9 = m_MovedInCitizenCount;
			writer.Write(value9);
			int value10 = m_ChildrenCount;
			writer.Write(value10);
			int value11 = m_TeenCount;
			writer.Write(value11);
			int value12 = m_AdultCount;
			writer.Write(value12);
			int value13 = m_SeniorCount;
			writer.Write(value13);
			int value14 = m_StudentCount;
			writer.Write(value14);
			int value15 = m_UneducatedCount;
			writer.Write(value15);
			int value16 = m_PoorlyEducatedCount;
			writer.Write(value16);
			int value17 = m_EducatedCount;
			writer.Write(value17);
			int value18 = m_WellEducatedCount;
			writer.Write(value18);
			int value19 = m_HighlyEducatedCount;
			writer.Write(value19);
			int value20 = m_WorkableCitizenCount;
			writer.Write(value20);
			int value21 = m_CityWorkerCount;
			writer.Write(value21);
			int value22 = m_DeadCitizenCount;
			writer.Write(value22);
			long value23 = m_TotalMovedInCitizenHealth;
			writer.Write(value23);
			long value24 = m_TotalMovedInCitizenHappiness;
			writer.Write(value24);
			long value25 = m_TotalMovedInCitizenWellbeing;
			writer.Write(value25);
			int value26 = m_EmployableByEducation0;
			writer.Write(value26);
			int value27 = m_EmployableByEducation1;
			writer.Write(value27);
			int value28 = m_EmployableByEducation2;
			writer.Write(value28);
			int value29 = m_EmployableByEducation3;
			writer.Write(value29);
			int value30 = m_EmployableByEducation4;
			writer.Write(value30);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			if (reader.context.version < Version.statisticUnifying)
			{
				reader.Read(out int value);
				reader.Read(out value);
				reader.Read(out value);
				return;
			}
			ref int value2 = ref m_MovingInHouseholdCount;
			reader.Read(out value2);
			ref int value3 = ref m_MovingInCitizenCount;
			reader.Read(out value3);
			ref int value4 = ref m_MovingAwayHouseholdCount;
			reader.Read(out value4);
			ref int value5 = ref m_CommuterHouseholdCount;
			reader.Read(out value5);
			ref int value6 = ref m_TouristCitizenCount;
			reader.Read(out value6);
			ref int value7 = ref m_HomelessHouseholdCount;
			reader.Read(out value7);
			ref int value8 = ref m_HomelessCitizenCount;
			reader.Read(out value8);
			ref int value9 = ref m_MovedInHouseholdCount;
			reader.Read(out value9);
			ref int value10 = ref m_MovedInCitizenCount;
			reader.Read(out value10);
			ref int value11 = ref m_ChildrenCount;
			reader.Read(out value11);
			ref int value12 = ref m_TeenCount;
			reader.Read(out value12);
			ref int value13 = ref m_AdultCount;
			reader.Read(out value13);
			ref int value14 = ref m_SeniorCount;
			reader.Read(out value14);
			ref int value15 = ref m_StudentCount;
			reader.Read(out value15);
			ref int value16 = ref m_UneducatedCount;
			reader.Read(out value16);
			ref int value17 = ref m_PoorlyEducatedCount;
			reader.Read(out value17);
			ref int value18 = ref m_EducatedCount;
			reader.Read(out value18);
			ref int value19 = ref m_WellEducatedCount;
			reader.Read(out value19);
			ref int value20 = ref m_HighlyEducatedCount;
			reader.Read(out value20);
			ref int value21 = ref m_WorkableCitizenCount;
			reader.Read(out value21);
			ref int value22 = ref m_CityWorkerCount;
			reader.Read(out value22);
			ref int value23 = ref m_DeadCitizenCount;
			reader.Read(out value23);
			ref long value24 = ref m_TotalMovedInCitizenHealth;
			reader.Read(out value24);
			ref long value25 = ref m_TotalMovedInCitizenHappiness;
			reader.Read(out value25);
			ref long value26 = ref m_TotalMovedInCitizenWellbeing;
			reader.Read(out value26);
			ref int value27 = ref m_EmployableByEducation0;
			reader.Read(out value27);
			ref int value28 = ref m_EmployableByEducation1;
			reader.Read(out value28);
			ref int value29 = ref m_EmployableByEducation2;
			reader.Read(out value29);
			ref int value30 = ref m_EmployableByEducation3;
			reader.Read(out value30);
			ref int value31 = ref m_EmployableByEducation4;
			reader.Read(out value31);
		}

		public int Population()
		{
			return m_MovedInCitizenCount;
		}

		public int Unemployed()
		{
			return m_WorkableCitizenCount - m_CityWorkerCount;
		}
	}

	[BurstCompile]
	private struct ResetJob : IJob
	{
		public NativeArray<int> m_ResourceNeed;

		public NativeArray<int> m_EmployableByEducation;

		public void Execute()
		{
			m_ResourceNeed.Fill(0);
			m_EmployableByEducation.Fill(0);
		}
	}

	[BurstCompile]
	private struct CountHouseholdJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdNeed> m_HouseholdNeedType;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Student> m_Students;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		public NativeAccumulator<HouseholdData>.ParallelWriter m_HouseholdCountData;

		public NativeAccumulator<HouseholdNeedData>.ParallelWriter m_HouseholdNeedCountData;

		[ReadOnly]
		public CitizenHappinessParameterData m_CitizenHappinessParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			HouseholdData value = default(HouseholdData);
			bool flag = chunk.Has<TouristHousehold>();
			bool flag2 = chunk.Has<CommuterHousehold>();
			bool flag3 = chunk.Has<HomelessHousehold>();
			if (chunk.Has<MovingAway>() && !flag && !flag2)
			{
				value.m_MovingAwayHouseholdCount += chunk.Count;
				m_HouseholdCountData.Accumulate(value);
				return;
			}
			NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdType);
			NativeArray<HouseholdNeed> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdNeedType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			if (flag2)
			{
				value.m_CommuterHouseholdCount += chunk.Count;
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				bool flag4 = true;
				bool flag5 = !(flag2 || flag);
				if ((nativeArray[i].m_Flags & HouseholdFlags.MovedIn) == 0)
				{
					flag4 = false;
					flag5 = false;
					if (!flag3)
					{
						value.m_MovingInHouseholdCount++;
						value.m_MovingInCitizenCount += bufferAccessor[i].Length;
					}
				}
				if (flag4 && nativeArray2[i].m_Resource != Resource.NoResource)
				{
					int resourceIndex = EconomyUtils.GetResourceIndex(nativeArray2[i].m_Resource);
					m_HouseholdNeedCountData.Accumulate(resourceIndex, new HouseholdNeedData
					{
						m_HouseholdNeed = nativeArray2[i].m_Amount
					});
				}
				if (flag && chunk.Has<Target>())
				{
					value.m_TouristCitizenCount += bufferAccessor[i].Length;
				}
				if (!flag5)
				{
					continue;
				}
				value.m_MovedInHouseholdCount++;
				if (flag3)
				{
					value.m_HomelessHouseholdCount++;
				}
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (CitizenUtils.IsDead(dynamicBuffer[j].m_Citizen, ref m_HealthProblems))
					{
						value.m_DeadCitizenCount++;
						continue;
					}
					Citizen citizen = m_Citizens[dynamicBuffer[j].m_Citizen];
					switch (citizen.GetAge())
					{
					case CitizenAge.Child:
						value.m_ChildrenCount++;
						break;
					case CitizenAge.Teen:
						value.m_TeenCount++;
						break;
					case CitizenAge.Adult:
						value.m_AdultCount++;
						break;
					case CitizenAge.Elderly:
						value.m_SeniorCount++;
						break;
					}
					int educationLevel = citizen.GetEducationLevel();
					switch (educationLevel)
					{
					case 0:
						value.m_UneducatedCount++;
						break;
					case 1:
						value.m_PoorlyEducatedCount++;
						break;
					case 2:
						value.m_EducatedCount++;
						break;
					case 3:
						value.m_WellEducatedCount++;
						break;
					case 4:
						value.m_HighlyEducatedCount++;
						break;
					}
					if (m_Students.HasComponent(dynamicBuffer[j].m_Citizen))
					{
						value.m_StudentCount++;
					}
					bool flag6 = false;
					bool flag7 = false;
					if (m_Workers.HasComponent(dynamicBuffer[j].m_Citizen))
					{
						flag6 = true;
						flag7 = m_OutsideConnections.HasComponent(m_Workers[dynamicBuffer[j].m_Citizen].m_Workplace);
						if (!flag7)
						{
							value.m_CityWorkerCount++;
						}
					}
					if (CitizenUtils.IsWorkableCitizen(dynamicBuffer[j].m_Citizen, ref m_Citizens, ref m_Students, ref m_HealthProblems))
					{
						value.m_WorkableCitizenCount++;
						if (!flag6 || flag7 || m_Workers[dynamicBuffer[j].m_Citizen].m_Level < educationLevel)
						{
							switch (educationLevel)
							{
							case 0:
								value.m_EmployableByEducation0++;
								break;
							case 1:
								value.m_EmployableByEducation1++;
								break;
							case 2:
								value.m_EmployableByEducation2++;
								break;
							case 3:
								value.m_EmployableByEducation3++;
								break;
							case 4:
								value.m_EmployableByEducation4++;
								break;
							}
						}
					}
					value.m_TotalMovedInCitizenHappiness += citizen.Happiness;
					value.m_TotalMovedInCitizenWellbeing += citizen.m_WellBeing;
					value.m_TotalMovedInCitizenHealth += citizen.m_Health;
					value.m_MovedInCitizenCount++;
					if (flag3)
					{
						value.m_HomelessCitizenCount++;
					}
				}
			}
			m_HouseholdCountData.Accumulate(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ResultJob : IJob
	{
		[ReadOnly]
		public NativeAccumulator<HouseholdData> m_HouseholdData;

		[ReadOnly]
		public NativeAccumulator<HouseholdNeedData> m_HouseholdNeedData;

		public NativeArray<int> m_ResourceNeed;

		public NativeArray<int> m_EmployableByEducation;

		public Entity m_City;

		public ComponentLookup<Population> m_Populations;

		public void Execute()
		{
			for (int i = 0; i < EconomyUtils.ResourceCount; i++)
			{
				m_ResourceNeed[i] = m_HouseholdNeedData.GetResult(i).m_HouseholdNeed;
			}
			HouseholdData result = m_HouseholdData.GetResult();
			m_EmployableByEducation[0] = result.m_EmployableByEducation0;
			m_EmployableByEducation[1] = result.m_EmployableByEducation1;
			m_EmployableByEducation[2] = result.m_EmployableByEducation2;
			m_EmployableByEducation[3] = result.m_EmployableByEducation3;
			m_EmployableByEducation[4] = result.m_EmployableByEducation4;
			Population value = m_Populations[m_City];
			value.m_Population = result.Population();
			value.m_PopulationWithMoveIn = result.m_MovedInCitizenCount + result.m_MovingInCitizenCount;
			if (value.m_Population > 0)
			{
				value.m_AverageHappiness = (int)(result.m_TotalMovedInCitizenHappiness / result.m_MovedInCitizenCount);
				value.m_AverageHealth = (int)(result.m_TotalMovedInCitizenHealth / result.m_MovedInCitizenCount);
			}
			else
			{
				value.m_AverageHappiness = 50;
				value.m_AverageHealth = 50;
			}
			m_Populations[m_City] = value;
		}
	}

	[BurstCompile]
	private struct CitizenRequirementJob : IJobChunk
	{
		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CitizenRequirementData> m_CitizenRequirementType;

		public ComponentTypeHandle<UnlockRequirementData> m_UnlockRequirementType;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		public Entity m_City;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CitizenRequirementData> nativeArray2 = chunk.GetNativeArray(ref m_CitizenRequirementType);
			NativeArray<UnlockRequirementData> nativeArray3 = chunk.GetNativeArray(ref m_UnlockRequirementType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				CitizenRequirementData citizenRequirement = nativeArray2[nextIndex];
				UnlockRequirementData unlockRequirement = nativeArray3[nextIndex];
				if (ShouldUnlock(citizenRequirement, ref unlockRequirement))
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_UnlockEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Unlock(nativeArray[nextIndex]));
				}
				nativeArray3[nextIndex] = unlockRequirement;
			}
		}

		private bool ShouldUnlock(CitizenRequirementData citizenRequirement, ref UnlockRequirementData unlockRequirement)
		{
			Population population = m_Populations[m_City];
			if (population.m_Population < citizenRequirement.m_MinimumPopulation || citizenRequirement.m_MinimumHappiness == 0)
			{
				unlockRequirement.m_Progress = math.min(population.m_Population, citizenRequirement.m_MinimumPopulation);
			}
			else
			{
				unlockRequirement.m_Progress = math.min(population.m_AverageHappiness, citizenRequirement.m_MinimumHappiness);
			}
			if (population.m_Population >= citizenRequirement.m_MinimumPopulation)
			{
				return population.m_AverageHappiness >= citizenRequirement.m_MinimumHappiness;
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
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

		public ComponentTypeHandle<HouseholdNeed> __Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle;

		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle;

		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RW_ComponentLookup;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		public ComponentLookup<Student> __Game_Citizens_Student_RW_ComponentLookup;

		public ComponentLookup<Worker> __Game_Citizens_Worker_RW_ComponentLookup;

		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RW_ComponentLookup;

		public ComponentLookup<Population> __Game_City_Population_RW_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CitizenRequirementData> __Game_Prefabs_CitizenRequirementData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
			__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdNeed>();
			__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>();
			__Game_Citizens_HealthProblem_RW_ComponentLookup = state.GetComponentLookup<HealthProblem>();
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
			__Game_Citizens_Student_RW_ComponentLookup = state.GetComponentLookup<Student>();
			__Game_Citizens_Worker_RW_ComponentLookup = state.GetComponentLookup<Worker>();
			__Game_Objects_OutsideConnection_RW_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>();
			__Game_City_Population_RW_ComponentLookup = state.GetComponentLookup<Population>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_CitizenRequirementData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CitizenRequirementData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
		}
	}

	private ResourceSystem m_ResourceSystem;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_RequirementQuery;

	private EntityArchetype m_UnlockEventArchetype;

	[DebugWatchDeps]
	private JobHandle m_HouseholdDataWriteDependencies;

	private JobHandle m_HouseholdDataReadDependencies;

	private NativeAccumulator<HouseholdData> m_HouseholdCountData;

	private NativeAccumulator<HouseholdNeedData> m_HouseholdNeedCountData;

	private HouseholdData m_LastHouseholdCountData;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ResourceNeed;

	private bool m_NeedForceCountData;

	[DebugWatchValue]
	private NativeArray<int> m_EmployableByEducation;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	[DebugWatchValue]
	public int MovingInHouseholdCount => m_LastHouseholdCountData.m_MovingInHouseholdCount;

	[DebugWatchValue]
	public int MovingInCitizenCount => m_LastHouseholdCountData.m_MovingInCitizenCount;

	[DebugWatchValue]
	public int MovingAwayHouseholdCount => m_LastHouseholdCountData.m_MovingAwayHouseholdCount;

	[DebugWatchValue]
	public int CommuterHouseholdCount => m_LastHouseholdCountData.m_CommuterHouseholdCount;

	[DebugWatchValue]
	public int TouristCitizenCount => m_LastHouseholdCountData.m_TouristCitizenCount;

	[DebugWatchValue]
	public int HomelessHouseholdCount => m_LastHouseholdCountData.m_HomelessHouseholdCount;

	[DebugWatchValue]
	public int HomelessCitizenCount => m_LastHouseholdCountData.m_HomelessCitizenCount;

	[DebugWatchValue]
	public int MovedInHouseholdCount => m_LastHouseholdCountData.m_MovedInHouseholdCount;

	[DebugWatchValue]
	public int MovedInCitizenCount => m_LastHouseholdCountData.m_MovedInCitizenCount;

	[DebugWatchValue]
	public int ChildrenCount => m_LastHouseholdCountData.m_ChildrenCount;

	[DebugWatchValue]
	public int AdultCount => m_LastHouseholdCountData.m_AdultCount;

	[DebugWatchValue]
	public int TeenCount => m_LastHouseholdCountData.m_TeenCount;

	[DebugWatchValue]
	public int SeniorCount => m_LastHouseholdCountData.m_SeniorCount;

	[DebugWatchValue]
	public int StudentCount => m_LastHouseholdCountData.m_StudentCount;

	[DebugWatchValue]
	public int UneducatedCount => m_LastHouseholdCountData.m_UneducatedCount;

	[DebugWatchValue]
	public int PoorlyEducatedCount => m_LastHouseholdCountData.m_PoorlyEducatedCount;

	[DebugWatchValue]
	public int EducatedCount => m_LastHouseholdCountData.m_EducatedCount;

	[DebugWatchValue]
	public int WellEducatedCount => m_LastHouseholdCountData.m_WellEducatedCount;

	[DebugWatchValue]
	public int HighlyEducatedCount => m_LastHouseholdCountData.m_HighlyEducatedCount;

	[DebugWatchValue]
	public int WorkableCitizenCount => m_LastHouseholdCountData.m_WorkableCitizenCount;

	[DebugWatchValue]
	public int CityWorkerCount => m_LastHouseholdCountData.m_CityWorkerCount;

	[DebugWatchValue]
	public int DeadCitizenCount => m_LastHouseholdCountData.m_DeadCitizenCount;

	[DebugWatchValue]
	public int AverageCitizenHappiness
	{
		get
		{
			if (m_LastHouseholdCountData.m_MovedInCitizenCount == 0)
			{
				return 0;
			}
			return (int)(m_LastHouseholdCountData.m_TotalMovedInCitizenHappiness / m_LastHouseholdCountData.m_MovedInCitizenCount);
		}
	}

	[DebugWatchValue]
	public int AverageCitizenHealth
	{
		get
		{
			if (m_LastHouseholdCountData.m_MovedInCitizenCount == 0)
			{
				return 0;
			}
			return (int)(m_LastHouseholdCountData.m_TotalMovedInCitizenHealth / m_LastHouseholdCountData.m_MovedInCitizenCount);
		}
	}

	[DebugWatchValue]
	public float UnemploymentRate
	{
		get
		{
			if (m_LastHouseholdCountData.m_WorkableCitizenCount == 0)
			{
				return 0f;
			}
			return 100f * (float)math.max(0, m_LastHouseholdCountData.m_WorkableCitizenCount - m_LastHouseholdCountData.m_CityWorkerCount) / (float)m_LastHouseholdCountData.m_WorkableCitizenCount;
		}
	}

	[DebugWatchValue]
	public float HomelessnessRate
	{
		get
		{
			if (m_LastHouseholdCountData.m_MovedInCitizenCount == 0)
			{
				return 0f;
			}
			return 100f * (float)m_LastHouseholdCountData.m_HomelessCitizenCount / (float)m_LastHouseholdCountData.m_MovedInCitizenCount;
		}
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public HouseholdData GetHouseholdCountData()
	{
		return m_LastHouseholdCountData;
	}

	public NativeArray<int> GetResourceNeeds(out JobHandle deps)
	{
		deps = m_HouseholdDataWriteDependencies;
		return m_ResourceNeed;
	}

	public bool IsCountDataNotReady()
	{
		return m_NeedForceCountData;
	}

	public NativeArray<int> GetEmployables()
	{
		return m_EmployableByEducation;
	}

	public void AddHouseholdDataReader(JobHandle reader)
	{
		m_HouseholdDataReadDependencies = JobHandle.CombineDependencies(m_HouseholdDataReadDependencies, reader);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_HouseholdQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Household>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenRequirementData>(), ComponentType.ReadWrite<UnlockRequirementData>(), ComponentType.ReadOnly<Locked>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_HouseholdCountData = new NativeAccumulator<HouseholdData>(Allocator.Persistent);
		m_HouseholdNeedCountData = new NativeAccumulator<HouseholdNeedData>(EconomyUtils.ResourceCount, Allocator.Persistent);
		m_ResourceNeed = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
		m_EmployableByEducation = new NativeArray<int>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_HouseholdCountData.Dispose();
		m_HouseholdNeedCountData.Dispose();
		m_ResourceNeed.Dispose();
		m_EmployableByEducation.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		HouseholdData value = m_LastHouseholdCountData;
		writer.Write(value);
		NativeArray<int> value2 = m_ResourceNeed;
		writer.Write(value2);
		NativeArray<int> value3 = m_EmployableByEducation;
		writer.Write(value3);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.economyFix)
		{
			ref HouseholdData value = ref m_LastHouseholdCountData;
			reader.Read(out value);
		}
		if (reader.context.version >= Version.countHouseholdDataFix)
		{
			if (reader.context.format.Has(FormatTags.FishResource))
			{
				NativeArray<int> value2 = m_ResourceNeed;
				reader.Read(value2);
			}
			else
			{
				NativeArray<int> subArray = m_ResourceNeed.GetSubArray(0, 40);
				reader.Read(subArray);
				m_ResourceNeed[40] = 0;
			}
			NativeArray<int> value3 = m_EmployableByEducation;
			reader.Read(value3);
		}
		else
		{
			m_NeedForceCountData = true;
		}
	}

	public void SetDefaults(Context context)
	{
		m_LastHouseholdCountData = default(HouseholdData);
		m_HouseholdCountData.Clear();
		m_HouseholdNeedCountData.Clear();
		m_ResourceNeed.Fill(0);
		m_EmployableByEducation.Fill(0);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_HouseholdQuery.IsEmptyIgnoreFilter)
		{
			if (!m_WasReset)
			{
				m_LastHouseholdCountData = default(HouseholdData);
				ResetJob jobData = new ResetJob
				{
					m_ResourceNeed = m_ResourceNeed,
					m_EmployableByEducation = m_EmployableByEducation
				};
				m_HouseholdDataWriteDependencies = IJobExtensions.Schedule(jobData, base.Dependency);
				m_WasReset = true;
			}
			return;
		}
		m_WasReset = false;
		m_LastHouseholdCountData = m_HouseholdCountData.GetResult();
		m_HouseholdCountData.Clear();
		m_HouseholdNeedCountData.Clear();
		CountHouseholdJob jobData2 = new CountHouseholdJob
		{
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdNeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Students = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCountData = m_HouseholdCountData.AsParallelWriter(),
			m_HouseholdNeedCountData = m_HouseholdNeedCountData.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_HouseholdQuery, base.Dependency);
		ResultJob jobData3 = new ResultJob
		{
			m_HouseholdData = m_HouseholdCountData,
			m_HouseholdNeedData = m_HouseholdNeedCountData,
			m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RW_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_ResourceNeed = m_ResourceNeed,
			m_EmployableByEducation = m_EmployableByEducation
		};
		base.Dependency = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(base.Dependency, m_HouseholdDataReadDependencies));
		m_HouseholdDataWriteDependencies = base.Dependency;
		if (m_NeedForceCountData)
		{
			base.Dependency.Complete();
			m_NeedForceCountData = false;
		}
		CitizenRequirementJob jobData4 = new CitizenRequirementJob
		{
			m_UnlockEventArchetype = m_UnlockEventArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CitizenRequirementData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnlockRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData4, m_RequirementQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CountHouseholdDataSystem()
	{
	}
}
