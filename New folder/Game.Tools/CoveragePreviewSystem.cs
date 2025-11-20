using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class CoveragePreviewSystem : GameSystemBase
{
	[BurstCompile]
	public struct InitializeCoverageJob : IJobChunk
	{
		[ReadOnly]
		public int m_SourceCoverageIndex;

		[ReadOnly]
		public int m_TargetCoverageIndex;

		public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceCoverageType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[m_TargetCoverageIndex] = dynamicBuffer[m_SourceCoverageIndex];
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CopyServiceCoverageJob : IJob
	{
		[ReadOnly]
		public Entity m_Source;

		[ReadOnly]
		public Entity m_Target;

		public BufferLookup<CoverageElement> m_CoverageElements;

		public void Execute()
		{
			if (m_CoverageElements.HasBuffer(m_Source) && m_CoverageElements.HasBuffer(m_Target))
			{
				DynamicBuffer<CoverageElement> v = m_CoverageElements[m_Source];
				m_CoverageElements[m_Target].CopyFrom(v);
			}
		}
	}

	[BurstCompile]
	public struct SetupCoverageSearchJob : IJob
	{
		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public ComponentLookup<BackSide> m_BackSideData;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_PrefabCoverageData;

		public CoverageAction m_Action;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public void Execute()
		{
			PrefabRef prefabRef = m_TargetSeeker.m_PrefabRef[m_Entity];
			m_PrefabCoverageData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
			if (m_TargetSeeker.m_Building.TryGetComponent(m_Entity, out var componentData2))
			{
				Transform transform = m_TargetSeeker.m_Transform[m_Entity];
				if (componentData2.m_RoadEdge != Entity.Null)
				{
					BuildingData buildingData = m_TargetSeeker.m_BuildingData[prefabRef.m_Prefab];
					float3 comparePosition = transform.m_Position;
					if (!m_TargetSeeker.m_Owner.TryGetComponent(componentData2.m_RoadEdge, out var componentData3) || componentData3.m_Owner != m_Entity)
					{
						comparePosition = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
					}
					Random random = m_TargetSeeker.m_RandomSeed.GetRandom(m_Entity.Index);
					m_TargetSeeker.AddEdgeTargets(ref random, m_Entity, 0f, EdgeFlags.DefaultMask, componentData2.m_RoadEdge, comparePosition, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: false);
				}
			}
			else
			{
				m_TargetSeeker.FindTargets(m_Entity, 0f);
			}
			if (m_BackSideData.TryGetComponent(m_Entity, out var componentData4))
			{
				Transform transform2 = m_TargetSeeker.m_Transform[m_Entity];
				if (componentData4.m_RoadEdge != Entity.Null)
				{
					BuildingData buildingData2 = m_TargetSeeker.m_BuildingData[prefabRef.m_Prefab];
					float3 comparePosition2 = transform2.m_Position;
					if (!m_TargetSeeker.m_Owner.TryGetComponent(componentData4.m_RoadEdge, out var componentData5) || componentData5.m_Owner != m_Entity)
					{
						comparePosition2 = BuildingUtils.CalculateFrontPosition(transform2, -buildingData2.m_LotSize.y);
					}
					Random random2 = m_TargetSeeker.m_RandomSeed.GetRandom(m_Entity.Index);
					m_TargetSeeker.AddEdgeTargets(ref random2, m_Entity, 0f, EdgeFlags.DefaultMask, componentData4.m_RoadEdge, comparePosition2, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: false);
				}
			}
			m_Action.data.m_Parameters = new CoverageParameters
			{
				m_Methods = m_TargetSeeker.m_PathfindParameters.m_Methods,
				m_Range = componentData.m_Range
			};
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> __Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<CoverageServiceType> __Game_Net_CoverageServiceType_SharedComponentTypeHandle;

		public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BackSide> __Game_Buildings_BackSide_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentLookup;

		public BufferLookup<CoverageElement> __Game_Pathfind_CoverageElement_RW_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<CoverageElement> __Game_Pathfind_CoverageElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Density> __Game_Net_Density_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ModifiedServiceCoverage> __Game_Buildings_ModifiedServiceCoverage_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CoverageElement> __Game_Pathfind_CoverageElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewCoverageData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_CoverageServiceType_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<CoverageServiceType>();
			__Game_Net_ServiceCoverage_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Buildings_BackSide_RO_ComponentLookup = state.GetComponentLookup<BackSide>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentLookup = state.GetComponentLookup<CoverageData>(isReadOnly: true);
			__Game_Pathfind_CoverageElement_RW_BufferLookup = state.GetBufferLookup<CoverageElement>();
			__Game_Pathfind_CoverageElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<CoverageElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Density_RO_ComponentLookup = state.GetComponentLookup<Density>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ModifiedServiceCoverage_RO_ComponentLookup = state.GetComponentLookup<ModifiedServiceCoverage>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_CoverageElement_RO_BufferLookup = state.GetBufferLookup<CoverageElement>(isReadOnly: true);
			__Game_Areas_ServiceDistrict_RO_BufferLookup = state.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RW_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>();
		}
	}

	private PathfindQueueSystem m_PathfindQueueSystem;

	private AirwaySystem m_AirwaySystem;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_ModifiedQuery;

	private EntityQuery m_UpdatedBuildingQuery;

	private EntityQuery m_ServiceBuildingQuery;

	private EntityQuery m_InfomodeQuery;

	private EntityQuery m_EventQuery;

	private CoverageService m_LastService;

	private PathfindTargetSeekerData m_TargetSeekerData;

	private HashSet<Entity> m_PendingCoverages;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadWrite<Game.Net.ServiceCoverage>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<Game.Net.Edge>(),
				ComponentType.ReadWrite<District>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<Game.Net.Edge>(),
				ComponentType.ReadWrite<District>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<CoverageServiceType>(), ComponentType.ReadOnly<CoverageElement>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>());
		m_ServiceBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<CoverageServiceType>(), ComponentType.ReadOnly<CoverageElement>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_InfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewCoverageData>());
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<CoverageUpdated>());
		m_LastService = CoverageService.Count;
		m_TargetSeekerData = new PathfindTargetSeekerData(this);
		m_PendingCoverages = new HashSet<Entity>();
		RequireForUpdate(m_InfomodeQuery);
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_LastService = CoverageService.Count;
		base.OnStopRunning();
	}

	private bool GetInfoviewCoverageData(out InfoviewCoverageData coverageData)
	{
		if (m_InfomodeQuery.IsEmptyIgnoreFilter)
		{
			coverageData = default(InfoviewCoverageData);
			return false;
		}
		NativeArray<ArchetypeChunk> nativeArray = m_InfomodeQuery.ToArchetypeChunkArray(Allocator.TempJob);
		ComponentTypeHandle<InfoviewCoverageData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		coverageData = nativeArray[0].GetNativeArray(ref typeHandle)[0];
		nativeArray.Dispose();
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!GetInfoviewCoverageData(out var coverageData))
		{
			m_LastService = CoverageService.Count;
		}
		bool flag = m_LastService != coverageData.m_Service;
		bool flag2 = flag || !m_ModifiedQuery.IsEmptyIgnoreFilter;
		m_LastService = coverageData.m_Service;
		bool flag3 = (flag2 ? (!m_ServiceBuildingQuery.IsEmptyIgnoreFilter) : (!m_UpdatedBuildingQuery.IsEmptyIgnoreFilter));
		bool flag4 = !m_EventQuery.IsEmptyIgnoreFilter;
		if (!flag3 && !flag2 && !flag4)
		{
			return;
		}
		NativeArray<ArchetypeChunk> nativeArray = m_ServiceBuildingQuery.ToArchetypeChunkArray(Allocator.TempJob);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		SharedComponentTypeHandle<CoverageServiceType> sharedComponentTypeHandle = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Net_CoverageServiceType_SharedComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<Game.Net.ServiceCoverage> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RW_BufferTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Created> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Temp> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		JobHandle job = default(JobHandle);
		JobHandle jobHandle = default(JobHandle);
		if (flag2)
		{
			m_PendingCoverages.Clear();
		}
		if (flag3)
		{
			m_TargetSeekerData.Update(this, m_AirwaySystem.GetAirwayData());
			PathfindParameters pathfindParameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_PathfindFlags = (PathfindFlags.Stable | PathfindFlags.IgnoreFlow),
				m_IgnoredRules = (RuleFlags.HasBlockage | RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
			ServiceCoverageSystem.SetupPathfindMethods(coverageData.m_Service, ref pathfindParameters, ref setupQueueTarget);
			NativeArray<ArchetypeChunk> nativeArray2 = ((!flag2) ? m_UpdatedBuildingQuery.ToArchetypeChunkArray(Allocator.TempJob) : nativeArray);
			SetupCoverageSearchJob jobData = new SetupCoverageSearchJob
			{
				m_BackSideData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_BackSide_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef)
			};
			base.EntityManager.CompleteDependencyBeforeRO<Temp>();
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray2[i];
				if (archetypeChunk.GetSharedComponent(sharedComponentTypeHandle).m_Service != coverageData.m_Service)
				{
					continue;
				}
				NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<Temp> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < archetypeChunk.Count; j++)
				{
					Entity entity = nativeArray3[j];
					Temp value;
					Entity entity2 = ((CollectionUtils.TryGet(nativeArray4, j, out value) && value.m_Original != Entity.Null) ? value.m_Original : entity);
					if (base.EntityManager.TryGetBuffer(entity2, isReadOnly: true, out DynamicBuffer<CoverageElement> buffer) && buffer.Length == 0)
					{
						m_PendingCoverages.Add(entity);
					}
				}
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				ArchetypeChunk archetypeChunk2 = nativeArray2[k];
				if (archetypeChunk2.GetSharedComponent(sharedComponentTypeHandle).m_Service != coverageData.m_Service)
				{
					continue;
				}
				NativeArray<Entity> nativeArray5 = archetypeChunk2.GetNativeArray(entityTypeHandle);
				NativeArray<Temp> nativeArray6 = archetypeChunk2.GetNativeArray(ref typeHandle2);
				bool flag5 = archetypeChunk2.Has(ref typeHandle);
				for (int l = 0; l < archetypeChunk2.Count; l++)
				{
					Entity entity3 = nativeArray5[l];
					CollectionUtils.TryGet(nativeArray6, l, out var value2);
					CoverageAction action = new CoverageAction(Allocator.Persistent);
					if (value2.m_Original != Entity.Null && (value2.m_Flags & TempFlags.Modify) == 0)
					{
						jobData.m_Entity = value2.m_Original;
					}
					else
					{
						jobData.m_Entity = entity3;
					}
					jobData.m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, action.data.m_Sources.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true);
					jobData.m_Action = action;
					JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, base.Dependency);
					job = JobHandle.CombineDependencies(job, jobHandle2);
					m_PathfindQueueSystem.Enqueue(action, entity3, jobHandle2, uint.MaxValue, this, default(PathEventData), nativeArray6.Length != 0);
					if (flag5 && value2.m_Original != Entity.Null)
					{
						jobHandle = IJobExtensions.Schedule(new CopyServiceCoverageJob
						{
							m_Source = value2.m_Original,
							m_Target = entity3,
							m_CoverageElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_CoverageElement_RW_BufferLookup, ref base.CheckedStateRef)
						}, jobHandle);
					}
				}
			}
			if (!flag2)
			{
				nativeArray2.Dispose();
			}
		}
		if (flag4)
		{
			NativeArray<CoverageUpdated> nativeArray7 = m_EventQuery.ToComponentDataArray<CoverageUpdated>(Allocator.Temp);
			for (int m = 0; m < nativeArray7.Length; m++)
			{
				m_PendingCoverages.Remove(nativeArray7[m].m_Owner);
			}
			nativeArray7.Dispose();
		}
		if (m_PendingCoverages.Count != 0)
		{
			if (flag)
			{
				JobHandle job2 = JobChunkExtensions.ScheduleParallel(new InitializeCoverageJob
				{
					m_SourceCoverageIndex = (int)coverageData.m_Service,
					m_TargetCoverageIndex = 8,
					m_ServiceCoverageType = bufferTypeHandle
				}, m_EdgeQuery, base.Dependency);
				job = JobHandle.CombineDependencies(job, jobHandle, job2);
			}
			else
			{
				job = JobHandle.CombineDependencies(job, jobHandle);
			}
			nativeArray.Dispose();
			base.Dependency = job;
			return;
		}
		NativeList<ServiceCoverageSystem.BuildingData> nativeList = new NativeList<ServiceCoverageSystem.BuildingData>(Allocator.TempJob);
		NativeList<ServiceCoverageSystem.CoverageElement> elements = new NativeList<ServiceCoverageSystem.CoverageElement>(Allocator.TempJob);
		ServiceCoverageSystem.PrepareCoverageJob jobData2 = new ServiceCoverageSystem.PrepareCoverageJob
		{
			m_Service = coverageData.m_Service,
			m_BuildingChunks = nativeArray,
			m_CoverageServiceType = sharedComponentTypeHandle,
			m_EntityType = entityTypeHandle,
			m_CoverageElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_CoverageElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingData = nativeList,
			m_Elements = elements
		};
		ServiceCoverageSystem.ClearCoverageJob jobData3 = new ServiceCoverageSystem.ClearCoverageJob
		{
			m_CoverageIndex = 8,
			m_ServiceCoverageType = bufferTypeHandle
		};
		ServiceCoverageSystem.ProcessCoverageJob jobData4 = new ServiceCoverageSystem.ProcessCoverageJob
		{
			m_CoverageIndex = 8,
			m_BuildingData = nativeList,
			m_Elements = elements,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DensityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Density_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingComponentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ModifiedServiceCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ModifiedServiceCoverage_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CoverageElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_CoverageElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_ServiceDistrict_RO_BufferLookup, ref base.CheckedStateRef),
			m_Efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_CoverageData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RW_BufferLookup, ref base.CheckedStateRef)
		};
		ServiceCoverageSystem.ApplyCoverageJob jobData5 = new ServiceCoverageSystem.ApplyCoverageJob
		{
			m_BuildingData = nativeList,
			m_Elements = elements
		};
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle dependsOn = IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(jobHandle3, JobChunkExtensions.ScheduleParallel(jobData3, m_EdgeQuery, base.Dependency)), jobData: jobData4, list: nativeList, innerloopBatchCount: 1);
		JobHandle jobHandle4 = IJobExtensions.Schedule(jobData5, dependsOn);
		nativeArray.Dispose(jobHandle3);
		nativeList.Dispose(jobHandle4);
		elements.Dispose(jobHandle4);
		job = JobHandle.CombineDependencies(job, jobHandle4);
		base.Dependency = job;
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
	public CoveragePreviewSystem()
	{
	}
}
