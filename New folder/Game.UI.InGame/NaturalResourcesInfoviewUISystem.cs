using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class NaturalResourcesInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		FertilityAmount,
		ForestAmount,
		OilAmount,
		OreAmount,
		FishAmount,
		FertilityExtraction,
		ForestExtraction,
		OilExtraction,
		OreExtraction,
		FertilityRenewal,
		ForestRenewal,
		FishExtraction,
		FishRenewal,
		Count
	}

	[BurstCompile]
	private struct UpdateWoodJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeLookup;

		[ReadOnly]
		public ComponentLookup<Plant> m_PlantLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> m_TreeDataLookup;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Tree tree = m_TreeLookup[entity];
				Plant plant = m_PlantLookup[entity];
				m_DamagedLookup.TryGetComponent(entity, out var componentData);
				PrefabRef prefabRef = m_PrefabRefLookup[entity];
				TreeData treeData = m_TreeDataLookup[prefabRef.m_Prefab];
				if (treeData.m_WoodAmount >= 1f)
				{
					m_Results[1] += ObjectUtils.CalculateWoodAmount(tree, plant, componentData, treeData);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateResourcesJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<MapFeatureElement> m_MapFeatureElementHandle;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<MapFeatureElement> bufferAccessor = chunk.GetBufferAccessor(ref m_MapFeatureElementHandle);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<MapFeatureElement> dynamicBuffer = bufferAccessor[i];
				num += dynamicBuffer[4].m_Amount;
				num2 += dynamicBuffer[5].m_Amount;
				num7 += dynamicBuffer[8].m_Amount;
				num8 += dynamicBuffer[8].m_RenewalRate;
				num4 += dynamicBuffer[3].m_RenewalRate;
				num5 += dynamicBuffer[2].m_Amount;
				num6 += dynamicBuffer[2].m_RenewalRate;
			}
			m_Results[0] += num5;
			m_Results[9] += num6;
			m_Results[1] += num3;
			m_Results[10] += num4;
			m_Results[2] += num;
			m_Results[3] += num2;
			m_Results[4] += num7;
			m_Results[12] += num8;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateExtractionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedFromEntity;

		[ReadOnly]
		public ComponentLookup<Extractor> m_ExtractorsFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_ExtractorAreaDataFromEntity;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDataFromEntity;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreaBufs;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public EconomyParameterData m_EconomyParameters;

		public NativeArray<float> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_PropertyRenterHandle);
			NativeArray<WorkProvider> nativeArray3 = chunk.GetNativeArray(ref m_WorkProviderHandle);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			float totalOil = 0f;
			float totalOre = 0f;
			float totalForest = 0f;
			float totalFertility = 0f;
			float totalFish = 0f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				PropertyRenter propertyRenter = nativeArray2[i];
				WorkProvider workProvider = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				if (!m_IndustrialProcessDataFromEntity.TryGetComponent(prefabRef.m_Prefab, out var componentData) || !m_WorkplaceDataFromEntity.TryGetComponent(prefabRef.m_Prefab, out var componentData2) || !m_PrefabRefFromEntity.TryGetComponent(propertyRenter.m_Property, out var componentData3) || !m_AttachedFromEntity.TryGetComponent(propertyRenter.m_Property, out var componentData4) || !m_SpawnableBuildingDataFromEntity.TryGetComponent(componentData3.m_Prefab, out var componentData5))
				{
					continue;
				}
				float efficiency = BuildingUtils.GetEfficiency(propertyRenter.m_Property, ref m_BuildingEfficiencyFromEntity);
				float resourcesInArea = ExtractorAISystem.GetResourcesInArea(componentData4.m_Parent, ref m_SubAreaBufs, ref m_InstalledUpgrades, ref m_ExtractorsFromEntity);
				int maxWorkers = workProvider.m_MaxWorkers;
				int level = componentData5.m_Level;
				int dailyProduction = Mathf.FloorToInt(math.min(resourcesInArea, EconomyUtils.GetCompanyProductionPerDay(efficiency, maxWorkers, level, isIndustrial: true, componentData2, componentData, m_ResourcePrefabs, ref m_ResourceDatas, ref m_EconomyParameters)));
				if (m_SubAreaBufs.TryGetBuffer(componentData4.m_Parent, out var bufferData))
				{
					ProcessAreas(bufferData, dailyProduction, ref totalFertility, ref totalForest, ref totalOil, ref totalOre, ref totalFish);
				}
				if (!m_InstalledUpgrades.TryGetBuffer(componentData4.m_Parent, out var bufferData2))
				{
					continue;
				}
				for (int j = 0; j < bufferData2.Length; j++)
				{
					if (!BuildingUtils.CheckOption(bufferData2[j], BuildingOption.Inactive) && m_SubAreaBufs.TryGetBuffer(bufferData2[j].m_Upgrade, out bufferData))
					{
						ProcessAreas(bufferData, dailyProduction, ref totalFertility, ref totalForest, ref totalOil, ref totalOre, ref totalFish);
					}
				}
			}
			m_Result[5] += totalFertility;
			m_Result[6] += totalForest;
			m_Result[7] += totalOil;
			m_Result[8] += totalOre;
			m_Result[11] += totalFish;
		}

		private void ProcessAreas(DynamicBuffer<Game.Areas.SubArea> subAreas, int dailyProduction, ref float totalFertility, ref float totalForest, ref float totalOil, ref float totalOre, ref float totalFish)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			for (int i = 0; i < subAreas.Length; i++)
			{
				Game.Areas.SubArea subArea = subAreas[i];
				if (m_PrefabRefFromEntity.TryGetComponent(subArea.m_Area, out var componentData) && m_ExtractorAreaDataFromEntity.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					switch (componentData2.m_MapFeature)
					{
					case MapFeature.FertileLand:
						num4 += (float)dailyProduction;
						break;
					case MapFeature.Forest:
						num3 += (float)dailyProduction;
						break;
					case MapFeature.Oil:
						num += (float)dailyProduction;
						break;
					case MapFeature.Ore:
						num2 += (float)dailyProduction;
						break;
					case MapFeature.Fish:
						num5 += (float)dailyProduction;
						break;
					}
				}
			}
			totalFertility += num4;
			totalForest += num3;
			totalOil += num;
			totalOre += num2;
			totalFish += num5;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extractor> __Game_Areas_Extractor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_MapFeatureElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<MapFeatureElement>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Areas_Extractor_RO_ComponentLookup = state.GetComponentLookup<Extractor>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private const string kGroup = "naturalResourceInfo";

	private ResourceSystem m_ResourceSystem;

	private ValueBinding<float> m_AvailableOil;

	private ValueBinding<float> m_AvailableOre;

	private ValueBinding<float> m_AvailableForest;

	private ValueBinding<float> m_AvailableFertility;

	private ValueBinding<float> m_ForestRenewalRate;

	private ValueBinding<float> m_FertilityRenewalRate;

	private ValueBinding<float> m_FishRenewalRate;

	private ValueBinding<float> m_AvailableFish;

	private ValueBinding<float> m_OilExtractionRate;

	private ValueBinding<float> m_OreExtractionRate;

	private ValueBinding<float> m_ForestExtractionRate;

	private ValueBinding<float> m_FertilityExtractionRate;

	private ValueBinding<float> m_FishExtractionRate;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_ExtractorQuery;

	private EntityQuery m_WoodQuery;

	private NativeArray<float> m_Results;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1701516008_0;

	public override GameMode gameMode => GameMode.GameOrEditor;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_AvailableFertility.active && !m_AvailableForest.active && !m_AvailableOil.active && !m_AvailableOre.active && !m_FertilityExtractionRate.active && !m_ForestExtractionRate.active && !m_OilExtractionRate.active)
			{
				return m_OreExtractionRate.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		AddBinding(m_AvailableOil = new ValueBinding<float>("naturalResourceInfo", "availableOil", 0f));
		AddBinding(m_AvailableOre = new ValueBinding<float>("naturalResourceInfo", "availableOre", 0f));
		AddBinding(m_AvailableForest = new ValueBinding<float>("naturalResourceInfo", "availableForest", 0f));
		AddBinding(m_AvailableFertility = new ValueBinding<float>("naturalResourceInfo", "availableFertility", 0f));
		AddBinding(m_ForestRenewalRate = new ValueBinding<float>("naturalResourceInfo", "forestRenewalRate", 0f));
		AddBinding(m_FertilityRenewalRate = new ValueBinding<float>("naturalResourceInfo", "fertilityRenewalRate", 0f));
		AddBinding(m_FishRenewalRate = new ValueBinding<float>("naturalResourceInfo", "fishRenewalRate", 0f));
		AddBinding(m_AvailableFish = new ValueBinding<float>("naturalResourceInfo", "availableFish", 0f));
		AddBinding(m_OilExtractionRate = new ValueBinding<float>("naturalResourceInfo", "oilExtractionRate", 0f));
		AddBinding(m_OreExtractionRate = new ValueBinding<float>("naturalResourceInfo", "oreExtractionRate", 0f));
		AddBinding(m_ForestExtractionRate = new ValueBinding<float>("naturalResourceInfo", "forestExtractionRate", 0f));
		AddBinding(m_FertilityExtractionRate = new ValueBinding<float>("naturalResourceInfo", "fertilityExtractionRate", 0f));
		AddBinding(m_FishExtractionRate = new ValueBinding<float>("naturalResourceInfo", "fishExtractionRate", 0f));
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.ReadOnly<MapFeatureElement>(), ComponentType.Exclude<Native>());
		m_WoodQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<Plant>()
			}
		});
		m_ExtractorQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ExtractorCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_Results = new NativeArray<float>(13, Allocator.Persistent);
		RequireForUpdate<ExtractorParameterData>();
		RequireForUpdate<EconomyParameterData>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		ResetResults(m_Results);
		JobHandle dependsOn = JobChunkExtensions.Schedule(new UpdateResourcesJob
		{
			m_MapFeatureElementHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_MapTileQuery, base.Dependency);
		JobHandle dependsOn2 = JobChunkExtensions.Schedule(new UpdateWoodJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TreeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlantLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_WoodQuery, dependsOn);
		JobChunkExtensions.Schedule(new UpdateExtractionJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkProviderHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorsFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorAreaDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkplaceDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcessDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingEfficiencyFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreaBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameters = __query_1701516008_0.GetSingleton<EconomyParameterData>(),
			m_Result = m_Results
		}, m_ExtractorQuery, dependsOn2).Complete();
		m_FertilityExtractionRate.Update(m_Results[5]);
		m_ForestExtractionRate.Update(m_Results[6]);
		m_OreExtractionRate.Update(m_Results[8]);
		m_OilExtractionRate.Update(m_Results[7]);
		m_FishExtractionRate.Update(m_Results[11]);
		m_AvailableFertility.Update(m_Results[0]);
		m_AvailableForest.Update(m_Results[1]);
		m_AvailableOre.Update(m_Results[3]);
		m_AvailableOil.Update(m_Results[2]);
		m_AvailableFish.Update(m_Results[4]);
		m_ForestRenewalRate.Update(m_Results[10]);
		m_FertilityRenewalRate.Update(m_Results[9]);
		m_FishRenewalRate.Update(m_Results[12]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1701516008_0 = entityQueryBuilder2.Build(ref state);
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
	public NaturalResourcesInfoviewUISystem()
	{
	}
}
