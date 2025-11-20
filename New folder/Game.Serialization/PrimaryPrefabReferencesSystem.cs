using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Effects;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class PrimaryPrefabReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct FixPrefabRefJob : IJobChunk
	{
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				PrefabRef value = nativeArray[i];
				if (value.m_Prefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_Prefab);
					nativeArray[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixUnderConstructionJob : IJobChunk
	{
		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<UnderConstruction> nativeArray = chunk.GetNativeArray(ref m_UnderConstructionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				UnderConstruction value = nativeArray[i];
				if (value.m_NewPrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_NewPrefab);
					nativeArray[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixCompanyDataJob : IJobChunk
	{
		public ComponentTypeHandle<CompanyData> m_CompanyDataType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CompanyData> nativeArray = chunk.GetNativeArray(ref m_CompanyDataType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CompanyData value = nativeArray[i];
				if (value.m_Brand != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_Brand);
					nativeArray[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixPolicyJob : IJobChunk
	{
		public BufferTypeHandle<Policy> m_PolicyType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Policy> bufferAccessor = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Policy> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Policy value = dynamicBuffer[j];
					if (value.m_Policy != Entity.Null)
					{
						m_PrefabReferences.Check(ref value.m_Policy);
						dynamicBuffer[j] = value;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixServiceBudgetJob : IJobChunk
	{
		public BufferTypeHandle<ServiceBudgetData> m_BudgetType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ServiceBudgetData> bufferAccessor = chunk.GetBufferAccessor(ref m_BudgetType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ServiceBudgetData> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ServiceBudgetData value = dynamicBuffer[j];
					m_PrefabReferences.Check(ref value.m_Service);
					dynamicBuffer[j] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixAtmosphereJob : IJobChunk
	{
		public ComponentTypeHandle<AtmosphereData> m_AtmosphereType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<AtmosphereData> nativeArray = chunk.GetNativeArray(ref m_AtmosphereType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AtmosphereData value = nativeArray[i];
				if (value.m_AtmospherePrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_AtmospherePrefab);
				}
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixBiomeJob : IJobChunk
	{
		public ComponentTypeHandle<BiomeData> m_BiomeType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<BiomeData> nativeArray = chunk.GetNativeArray(ref m_BiomeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				BiomeData value = nativeArray[i];
				if (value.m_BiomePrefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_BiomePrefab);
				}
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixVehicleModelJob : IJobChunk
	{
		public BufferTypeHandle<VehicleModel> m_VehicleModelType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<VehicleModel> bufferAccessor = chunk.GetBufferAccessor(ref m_VehicleModelType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<VehicleModel> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ref VehicleModel reference = ref dynamicBuffer.ElementAt(j);
					if (reference.m_PrimaryPrefab != Entity.Null)
					{
						m_PrefabReferences.Check(ref reference.m_PrimaryPrefab);
					}
					if (reference.m_SecondaryPrefab != Entity.Null)
					{
						m_PrefabReferences.Check(ref reference.m_SecondaryPrefab);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixEditorContainerJob : IJobChunk
	{
		public ComponentTypeHandle<Game.Tools.EditorContainer> m_EditorContainerType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Tools.EditorContainer> nativeArray = chunk.GetNativeArray(ref m_EditorContainerType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Tools.EditorContainer value = nativeArray[i];
				if (value.m_Prefab != Entity.Null)
				{
					m_PrefabReferences.Check(ref value.m_Prefab);
				}
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixChirpJob : IJobChunk
	{
		public ComponentTypeHandle<Game.Triggers.Chirp> m_ChirpType;

		public BufferTypeHandle<ChirpEntity> m_ChirpEntityType;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabDatas;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Triggers.Chirp> nativeArray = chunk.GetNativeArray(ref m_ChirpType);
			BufferAccessor<ChirpEntity> bufferAccessor = chunk.GetBufferAccessor(ref m_ChirpEntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref Game.Triggers.Chirp reference = ref nativeArray.ElementAt(i);
				if (m_PrefabDatas.HasComponent(reference.m_Sender))
				{
					m_PrefabReferences.Check(ref reference.m_Sender);
				}
			}
			for (int j = 0; j < bufferAccessor.Length; j++)
			{
				DynamicBuffer<ChirpEntity> dynamicBuffer = bufferAccessor[j];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					ref ChirpEntity reference2 = ref dynamicBuffer.ElementAt(k);
					if (m_PrefabDatas.HasComponent(reference2.m_Entity))
					{
						m_PrefabReferences.Check(ref reference2.m_Entity);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixSubReplacementJob : IJobChunk
	{
		public BufferTypeHandle<SubReplacement> m_SubReplacementType;

		public PrefabReferences m_PrefabReferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<SubReplacement> bufferAccessor = chunk.GetBufferAccessor(ref m_SubReplacementType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<SubReplacement> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					SubReplacement value = dynamicBuffer[j];
					if (value.m_Prefab != Entity.Null)
					{
						m_PrefabReferences.Check(ref value.m_Prefab);
					}
					dynamicBuffer[j] = value;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentTypeHandle;

		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CompanyData> __Game_Companies_CompanyData_RW_ComponentTypeHandle;

		public BufferTypeHandle<Policy> __Game_Policies_Policy_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceBudgetData> __Game_Simulation_ServiceBudgetData_RW_BufferTypeHandle;

		public ComponentTypeHandle<AtmosphereData> __Game_Simulation_AtmosphereData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BiomeData> __Game_Simulation_BiomeData_RW_ComponentTypeHandle;

		public BufferTypeHandle<VehicleModel> __Game_Routes_VehicleModel_RW_BufferTypeHandle;

		public ComponentTypeHandle<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Triggers.Chirp> __Game_Triggers_Chirp_RW_ComponentTypeHandle;

		public BufferTypeHandle<ChirpEntity> __Game_Triggers_ChirpEntity_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		public BufferTypeHandle<SubReplacement> __Game_Net_SubReplacement_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
			__Game_Objects_UnderConstruction_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnderConstruction>();
			__Game_Companies_CompanyData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyData>();
			__Game_Policies_Policy_RW_BufferTypeHandle = state.GetBufferTypeHandle<Policy>();
			__Game_Simulation_ServiceBudgetData_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceBudgetData>();
			__Game_Simulation_AtmosphereData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AtmosphereData>();
			__Game_Simulation_BiomeData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BiomeData>();
			__Game_Routes_VehicleModel_RW_BufferTypeHandle = state.GetBufferTypeHandle<VehicleModel>();
			__Game_Tools_EditorContainer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Tools.EditorContainer>();
			__Game_Triggers_Chirp_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Triggers.Chirp>();
			__Game_Triggers_ChirpEntity_RW_BufferTypeHandle = state.GetBufferTypeHandle<ChirpEntity>();
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Net_SubReplacement_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubReplacement>();
		}
	}

	private CheckPrefabReferencesSystem m_CheckPrefabReferencesSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ClimateSystem m_ClimateSystem;

	private TerrainMaterialSystem m_TerrainMaterialSystem;

	private TransportUsageTrackSystem m_TransportUsageTrackSystem;

	private EntityQuery m_PrefabRefQuery;

	private EntityQuery m_SetLevelQuery;

	private EntityQuery m_CompanyDataQuery;

	private EntityQuery m_PolicyQuery;

	private EntityQuery m_ActualBudgetQuery;

	private EntityQuery m_ServiceBudgetQuery;

	private EntityQuery m_VehicleModelQuery;

	private EntityQuery m_EditorContainerQuery;

	private EntityQuery m_AtmosphereQuery;

	private EntityQuery m_BiomeQuery;

	private EntityQuery m_ChirpQuery;

	private EntityQuery m_SubReplacementQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CheckPrefabReferencesSystem = base.World.GetOrCreateSystemManaged<CheckPrefabReferencesSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_TerrainMaterialSystem = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		m_TransportUsageTrackSystem = base.World.GetOrCreateSystemManaged<TransportUsageTrackSystem>();
		m_PrefabRefQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<NetCompositionData>(), ComponentType.Exclude<EffectInstance>(), ComponentType.Exclude<LivePath>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SetLevelQuery = GetEntityQuery(ComponentType.ReadOnly<UnderConstruction>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CompanyDataQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_PolicyQuery = GetEntityQuery(ComponentType.ReadOnly<Policy>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ServiceBudgetQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>());
		m_AtmosphereQuery = GetEntityQuery(ComponentType.ReadOnly<AtmosphereData>());
		m_BiomeQuery = GetEntityQuery(ComponentType.ReadOnly<BiomeData>());
		m_VehicleModelQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleModel>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_EditorContainerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Tools.EditorContainer>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Triggers.Chirp>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SubReplacementQuery = GetEntityQuery(ComponentType.ReadOnly<SubReplacement>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		PrefabReferences references = m_CheckPrefabReferencesSystem.GetPrefabReferences(this, out dependencies);
		dependencies = JobHandle.CombineDependencies(base.Dependency, dependencies);
		FixPrefabRefJob jobData = new FixPrefabRefJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixUnderConstructionJob jobData2 = new FixUnderConstructionJob
		{
			m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UnderConstruction_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixCompanyDataJob jobData3 = new FixCompanyDataJob
		{
			m_CompanyDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixPolicyJob jobData4 = new FixPolicyJob
		{
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixServiceBudgetJob jobData5 = new FixServiceBudgetJob
		{
			m_BudgetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceBudgetData_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixAtmosphereJob jobData6 = new FixAtmosphereJob
		{
			m_AtmosphereType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_AtmosphereData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixBiomeJob jobData7 = new FixBiomeJob
		{
			m_BiomeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_BiomeData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixVehicleModelJob jobData8 = new FixVehicleModelJob
		{
			m_VehicleModelType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_VehicleModel_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixEditorContainerJob jobData9 = new FixEditorContainerJob
		{
			m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixChirpJob jobData10 = new FixChirpJob
		{
			m_ChirpType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Triggers_Chirp_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ChirpEntityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Triggers_ChirpEntity_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		FixSubReplacementJob jobData11 = new FixSubReplacementJob
		{
			m_SubReplacementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubReplacement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabReferences = references
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_PrefabRefQuery, dependencies);
		JobHandle job2 = JobChunkExtensions.ScheduleParallel(jobData2, m_SetLevelQuery, dependencies);
		JobHandle job3 = JobChunkExtensions.ScheduleParallel(jobData3, m_CompanyDataQuery, dependencies);
		JobHandle job4 = JobChunkExtensions.ScheduleParallel(jobData4, m_PolicyQuery, dependencies);
		JobHandle job5 = JobChunkExtensions.ScheduleParallel(jobData5, m_ServiceBudgetQuery, dependencies);
		JobHandle job6 = JobChunkExtensions.ScheduleParallel(jobData6, m_AtmosphereQuery, dependencies);
		JobHandle job7 = JobChunkExtensions.ScheduleParallel(jobData7, m_BiomeQuery, dependencies);
		JobHandle job8 = JobChunkExtensions.ScheduleParallel(jobData8, m_VehicleModelQuery, dependencies);
		JobHandle job9 = JobChunkExtensions.ScheduleParallel(jobData9, m_EditorContainerQuery, dependencies);
		JobHandle job10 = JobChunkExtensions.ScheduleParallel(jobData10, m_ChirpQuery, dependencies);
		JobHandle job11 = JobChunkExtensions.ScheduleParallel(jobData11, m_SubReplacementQuery, dependencies);
		dependencies.Complete();
		m_CityConfigurationSystem.PatchReferences(ref references);
		m_ClimateSystem.PatchReferences(ref references);
		m_TerrainMaterialSystem.PatchReferences(ref references);
		m_TransportUsageTrackSystem.PatchReferences(ref references);
		dependencies = JobUtils.CombineDependencies(job9, job8, job, job3, job4, job5, job2, job6, job7, job10, job11);
		m_CheckPrefabReferencesSystem.AddPrefabReferencesUser(dependencies);
		base.Dependency = dependencies;
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
	public PrimaryPrefabReferencesSystem()
	{
	}
}
