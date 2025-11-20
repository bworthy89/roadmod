#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WorkProviderSystem : GameSystemBase
{
	private enum LayOffReason
	{
		Unknown,
		MovingAway,
		TooMany,
		NoBuilding,
		Count
	}

	[BurstCompile]
	private struct WorkProviderTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposes;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDatas;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> m_StudentBufs;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Efficiency> m_Efficiencies;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<LayOffReason>.ParallelWriter m_LayOffQueue;

		public IconCommandBuffer m_IconCommandBuffer;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public WorkProviderParameterData m_WorkProviderParameterData;

		public BuildingEfficiencyParameterData m_BuildingEfficiencyParameterData;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!chunk.Has<Game.Objects.OutsideConnection>() && chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray(ref m_PropertyRenterType);
			bool isDestroyed = chunk.Has(ref m_DestroyedType);
			bool flag = chunk.Has<CompanyData>();
			bool flag2 = !flag && chunk.Has<Game.Objects.OutsideConnection>();
			bool flag3 = !flag && !flag2 && chunk.Has<Game.Buildings.School>();
			for (int i = 0; i < chunk.Count; i++)
			{
				if (m_WorkplaceDatas.TryGetComponent(m_PrefabRefs[nativeArray[i]], out var componentData))
				{
					ref WorkProvider reference = ref nativeArray2.ElementAt(i);
					Entity entity = Entity.Null;
					if (flag)
					{
						if (nativeArray3.Length <= 0)
						{
							continue;
						}
						entity = nativeArray3[i].m_Property;
						if (entity == Entity.Null)
						{
							Liquidate(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i]);
							continue;
						}
					}
					else
					{
						entity = nativeArray[i];
						if (flag2)
						{
							Workplaces workplaces = new Workplaces
							{
								m_Uneducated = 0,
								m_PoorlyEducated = 0,
								m_Educated = 200,
								m_WellEducated = 200,
								m_HighlyEducated = 200
							};
							reference.m_MaxWorkers = workplaces.TotalCount;
						}
						else if (flag3)
						{
							UpdateSchoolMaxWorkers(ref reference, nativeArray[i]);
						}
						if (!flag2 && m_InstalledUpgrades.TryGetBuffer(nativeArray[i], out var bufferData))
						{
							UpgradeUtils.CombineStats(ref componentData, bufferData, ref m_PrefabRefs, ref m_WorkplaceDatas);
						}
					}
					if (entity != Entity.Null && m_PrefabRefs.HasComponent(entity))
					{
						int buildingLevel = PropertyUtils.GetBuildingLevel(m_PrefabRefs[entity], m_SpawnableBuildingDatas);
						Workplaces workplaces2 = EconomyUtils.CalculateNumberOfWorkplaces(reference.m_MaxWorkers, componentData.m_Complexity, buildingLevel);
						Workplaces freeWorkplaces = workplaces2;
						RefreshFreeWorkplace(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i], ref freeWorkplaces);
						if (!flag2)
						{
							UpdateNotificationAndEfficiency(entity, ref reference, bufferAccessor[i], workplaces2, freeWorkplaces, componentData.m_WorkConditions, isDestroyed);
						}
					}
				}
				else
				{
					Liquidate(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i]);
				}
			}
		}

		private void UpdateSchoolMaxWorkers(ref WorkProvider workProvider, Entity schoolEntity)
		{
			int cityServiceWorkplaceMaxWorkers = CityUtils.GetCityServiceWorkplaceMaxWorkers(schoolEntity, ref m_PrefabRefs, ref m_InstalledUpgrades, ref m_Deleteds, ref m_WorkplaceDatas, ref m_SchoolDatas, ref m_StudentBufs);
			workProvider.m_MaxWorkers = cityServiceWorkplaceMaxWorkers;
		}

		private void RefreshFreeWorkplace(int sortKey, Entity workplaceEntity, DynamicBuffer<Employee> employeeBuf, ref Workplaces freeWorkplaces)
		{
			for (int i = 0; i < employeeBuf.Length; i++)
			{
				Employee employee = employeeBuf[i];
				if (!m_Citizens.HasComponent(employee.m_Worker) || CitizenUtils.IsDead(employee.m_Worker, ref m_HealthProblems) || !m_Workers.TryGetComponent(employee.m_Worker, out var componentData) || componentData.m_Workplace != workplaceEntity || m_MovingAways.HasComponent(m_HouseholdMembers[employee.m_Worker].m_Household))
				{
					employeeBuf.RemoveAtSwapBack(i);
					i--;
					m_LayOffQueue.Enqueue(LayOffReason.MovingAway);
				}
				else if (freeWorkplaces[employee.m_Level] <= 0)
				{
					RemoveWorker(sortKey, employee.m_Worker, workplaceEntity);
					employeeBuf.RemoveAtSwapBack(i);
					i--;
					m_LayOffQueue.Enqueue(LayOffReason.TooMany);
				}
				else
				{
					freeWorkplaces[employee.m_Level]--;
				}
			}
			if (freeWorkplaces.TotalCount > 0)
			{
				m_CommandBuffer.AddComponent(sortKey, workplaceEntity, new FreeWorkplaces(freeWorkplaces));
			}
			else
			{
				m_CommandBuffer.RemoveComponent<FreeWorkplaces>(sortKey, workplaceEntity);
			}
		}

		private void UpdateNotificationAndEfficiency(Entity buildingEntity, ref WorkProvider workProvider, DynamicBuffer<Employee> employees, Workplaces maxWorkplaces, Workplaces freeWorkplaces, int workConditions, bool isDestroyed)
		{
			int num = maxWorkplaces.m_Uneducated + maxWorkplaces.m_PoorlyEducated;
			int num2 = freeWorkplaces.m_Uneducated + freeWorkplaces.m_PoorlyEducated;
			bool enabled = false;
			if (!isDestroyed)
			{
				enabled = num > 0 && (float)num2 / (float)num >= m_WorkProviderParameterData.m_UneducatedNotificationLimit;
			}
			UpdateCooldown(ref workProvider.m_UneducatedCooldown, enabled);
			UpdateNotification(buildingEntity, m_WorkProviderParameterData.m_UneducatedNotificationPrefab, workProvider.m_UneducatedCooldown >= m_WorkProviderParameterData.m_UneducatedNotificationDelay, ref workProvider.m_UneducatedNotificationEntity);
			int num3 = maxWorkplaces.m_Educated + 2 * maxWorkplaces.m_WellEducated + 2 * maxWorkplaces.m_HighlyEducated;
			int num4 = freeWorkplaces.m_Educated + 2 * freeWorkplaces.m_WellEducated + 2 * freeWorkplaces.m_HighlyEducated;
			bool enabled2 = false;
			if (!isDestroyed)
			{
				enabled2 = num3 > 0 && (float)(num4 / num3) >= m_WorkProviderParameterData.m_EducatedNotificationLimit;
			}
			UpdateCooldown(ref workProvider.m_EducatedCooldown, enabled2);
			UpdateNotification(buildingEntity, m_WorkProviderParameterData.m_EducatedNotificationPrefab, workProvider.m_EducatedCooldown >= m_WorkProviderParameterData.m_EducatedNotificationDelay, ref workProvider.m_EducatedNotificationEntity);
			if (m_Efficiencies.TryGetBuffer(buildingEntity, out var bufferData))
			{
				float averageWorkforce = EconomyUtils.GetAverageWorkforce(maxWorkplaces);
				float efficiency;
				float efficiency2;
				float num6;
				if (averageWorkforce > 0f)
				{
					CalculateCurrentWorkforce(employees, maxWorkplaces.TotalCount, out var currentWorkforce, out var averageWorkforce2, out var sickWorkforce);
					float num5 = averageWorkforce - averageWorkforce2 - sickWorkforce;
					UpdateCooldown(ref workProvider.m_EfficiencyCooldown, num5 > 0.001f);
					num5 *= math.saturate((float)workProvider.m_EfficiencyCooldown / m_BuildingEfficiencyParameterData.m_MissingEmployeesEfficiencyDelay);
					num5 *= m_BuildingEfficiencyParameterData.m_MissingEmployeesEfficiencyPenalty;
					sickWorkforce *= m_BuildingEfficiencyParameterData.m_SickEmployeesEfficiencyPenalty;
					float2 @float = BuildingUtils.ApproximateEfficiencyFactors((averageWorkforce - num5 - sickWorkforce) / averageWorkforce, new float2(num5, sickWorkforce));
					efficiency = @float.x;
					efficiency2 = @float.y;
					num6 = ((averageWorkforce2 > 0f) ? (currentWorkforce / averageWorkforce2) : 1f);
					num6 += (float)workConditions * 0.01f;
				}
				else
				{
					workProvider.m_EfficiencyCooldown = 0;
					efficiency = 1f;
					efficiency2 = 1f;
					num6 = 1f;
				}
				BuildingUtils.SetEfficiencyFactor(bufferData, EfficiencyFactor.NotEnoughEmployees, efficiency);
				BuildingUtils.SetEfficiencyFactor(bufferData, EfficiencyFactor.SickEmployees, efficiency2);
				BuildingUtils.SetEfficiencyFactor(bufferData, EfficiencyFactor.EmployeeHappiness, num6);
			}
		}

		private void UpdateCooldown(ref short cooldown, bool enabled)
		{
			if (!enabled)
			{
				if (cooldown > 0)
				{
					cooldown = 0;
				}
			}
			else if (cooldown < short.MaxValue)
			{
				cooldown++;
			}
		}

		private void UpdateNotification(Entity building, Entity notificationPrefab, bool enabled, ref Entity currentTarget)
		{
			if (currentTarget != Entity.Null && (!enabled || currentTarget != building))
			{
				m_IconCommandBuffer.Remove(currentTarget, notificationPrefab);
				currentTarget = Entity.Null;
			}
			if (enabled && currentTarget == Entity.Null)
			{
				m_IconCommandBuffer.Add(building, notificationPrefab, IconPriority.Problem);
				currentTarget = building;
			}
		}

		private void CalculateCurrentWorkforce(DynamicBuffer<Employee> employees, int maxCount, out float currentWorkforce, out float averageWorkforce, out float sickWorkforce)
		{
			currentWorkforce = 0f;
			averageWorkforce = 0f;
			sickWorkforce = 0f;
			int num = math.min(employees.Length, maxCount);
			for (int i = 0; i < num; i++)
			{
				Employee employee = employees[i];
				Citizen citizen = m_Citizens[employee.m_Worker];
				if (!m_HealthProblems.HasComponent(employee.m_Worker))
				{
					currentWorkforce += EconomyUtils.GetWorkerWorkforce(citizen.Happiness, employee.m_Level);
					averageWorkforce += EconomyUtils.GetWorkerWorkforce(50, employee.m_Level);
				}
				else
				{
					sickWorkforce += EconomyUtils.GetWorkerWorkforce(50, employee.m_Level);
				}
			}
		}

		private void Liquidate(int sortKey, Entity provider, DynamicBuffer<Employee> employees)
		{
			for (int i = 0; i < employees.Length; i++)
			{
				Entity worker = employees[i].m_Worker;
				if (m_Workers.HasComponent(worker))
				{
					m_LayOffQueue.Enqueue(LayOffReason.NoBuilding);
					RemoveWorker(sortKey, worker, provider);
				}
			}
			employees.Clear();
			m_CommandBuffer.RemoveComponent<FreeWorkplaces>(sortKey, provider);
		}

		private void RemoveWorker(int sortKey, Entity worker, Entity workplace)
		{
			if (m_TravelPurposes.TryGetComponent(worker, out var componentData))
			{
				Purpose purpose = componentData.m_Purpose;
				if (purpose == Purpose.GoingToWork || purpose == Purpose.Working || purpose == Purpose.GoingToSchool || purpose == Purpose.Studying)
				{
					purpose = componentData.m_Purpose;
					if (purpose == Purpose.GoingToSchool || purpose == Purpose.Studying)
					{
						UnityEngine.Debug.LogWarning($"Worker {worker.Index} had incorrect TravelPurpose {(int)componentData.m_Purpose}!");
					}
					m_CommandBuffer.RemoveComponent<TravelPurpose>(sortKey, worker);
				}
			}
			m_CommandBuffer.RemoveComponent<Worker>(sortKey, worker);
			m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, worker, workplace));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct LayOffCountJob : IJob
	{
		public NativeQueue<LayOffReason> m_LayOffQueue;

		public NativeArray<int> m_LayOffs;

		public void Execute()
		{
			LayOffReason item;
			while (m_LayOffQueue.TryDequeue(out item))
			{
				m_LayOffs[(int)item]++;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		public BufferTypeHandle<Employee> __Game_Companies_Employee_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Companies_WorkProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>();
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Companies_Employee_RW_BufferTypeHandle = state.GetBufferTypeHandle<Employee>();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Buildings_Efficiency_RW_BufferLookup = state.GetBufferLookup<Efficiency>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true);
		}
	}

	private const int kUpdatesPerDay = 512;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private IconCommandSystem m_IconCommandSystem;

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_WorkProviderGroup;

	private NativeQueue<LayOffReason> m_LayOffQueue;

	[DebugWatchValue]
	private NativeArray<int> m_LayOffs;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_543653706_0;

	private EntityQuery __query_543653706_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 32;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_WorkProviderGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<WorkProvider>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadWrite<Employee>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<CompanyData>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LayOffQueue = new NativeQueue<LayOffReason>(Allocator.Persistent);
		m_LayOffs = new NativeArray<int>(4, Allocator.Persistent);
		RequireForUpdate(m_WorkProviderGroup);
		RequireForUpdate<WorkProviderParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LayOffs.Dispose();
		m_LayOffQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 512, 16);
		WorkProviderTickJob jobData = new WorkProviderTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TravelPurposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StudentBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Student_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_LayOffQueue = m_LayOffQueue.AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
			m_WorkProviderParameterData = __query_543653706_0.GetSingleton<WorkProviderParameterData>(),
			m_BuildingEfficiencyParameterData = __query_543653706_1.GetSingleton<BuildingEfficiencyParameterData>(),
			m_UpdateFrameIndex = updateFrame
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_WorkProviderGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		LayOffCountJob jobData2 = new LayOffCountJob
		{
			m_LayOffs = m_LayOffs,
			m_LayOffQueue = m_LayOffQueue
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<WorkProviderParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_543653706_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_543653706_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public WorkProviderSystem()
	{
	}
}
