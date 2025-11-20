using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Reflection;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class IndustrialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateIndustrialDemandJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ZoneData> m_UnlockedZoneDatas;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_IndustrialPropertyChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_OfficePropertyChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_StorageCompanyChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CityServiceChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<CityServiceUpkeep> m_ServiceUpkeepType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyOnMarket> m_PropertyOnMarketType;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attached;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_ServiceUpkeeps;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_Upkeeps;

		public EconomyParameterData m_EconomyParameters;

		public DemandParameterData m_DemandParameters;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public NativeArray<int> m_EmployableByEducation;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		[ReadOnly]
		public Workplaces m_FreeWorkplaces;

		public Entity m_City;

		public NativeValue<int> m_IndustrialCompanyDemand;

		public NativeValue<int> m_IndustrialBuildingDemand;

		public NativeValue<int> m_StorageCompanyDemand;

		public NativeValue<int> m_StorageBuildingDemand;

		public NativeValue<int> m_OfficeCompanyDemand;

		public NativeValue<int> m_OfficeBuildingDemand;

		public NativeArray<int> m_IndustrialDemandFactors;

		public NativeArray<int> m_OfficeDemandFactors;

		public NativeArray<int> m_IndustrialCompanyDemands;

		public NativeArray<int> m_IndustrialBuildingDemands;

		public NativeArray<int> m_StorageBuildingDemands;

		public NativeArray<int> m_StorageCompanyDemands;

		[ReadOnly]
		public NativeArray<int> m_Productions;

		[ReadOnly]
		public NativeArray<int> m_CompanyResourceDemands;

		[ReadOnly]
		public NativeArray<int> m_HouseholdResourceDemands;

		public NativeArray<int> m_FreeProperties;

		[ReadOnly]
		public NativeArray<int> m_Propertyless;

		public NativeArray<int> m_FreeStorages;

		public NativeArray<int> m_Storages;

		public NativeArray<int> m_StorageCapacities;

		public NativeArray<int> m_ResourceDemands;

		public float m_IndustrialOfficeTaxEffectDemandOffset;

		public bool m_UnlimitedDemand;

		public void Execute()
		{
			bool flag = false;
			for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
			{
				if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Industrial)
				{
					flag = true;
					break;
				}
			}
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
				if (EconomyUtils.IsOfficeResource(iterator.resource))
				{
					m_ResourceDemands[resourceIndex] = (m_HouseholdResourceDemands[resourceIndex] + m_CompanyResourceDemands[resourceIndex]) * 2;
				}
				else
				{
					m_ResourceDemands[resourceIndex] = ((m_CompanyResourceDemands[resourceIndex] == 0 && EconomyUtils.IsIndustrialResource(resourceData, includeMaterial: false, includeOffice: false)) ? 100 : m_CompanyResourceDemands[resourceIndex]);
				}
				m_FreeProperties[resourceIndex] = 0;
				m_Storages[resourceIndex] = 0;
				m_FreeStorages[resourceIndex] = 0;
				m_StorageCapacities[resourceIndex] = 0;
			}
			for (int j = 0; j < m_IndustrialDemandFactors.Length; j++)
			{
				m_IndustrialDemandFactors[j] = 0;
			}
			for (int k = 0; k < m_OfficeDemandFactors.Length; k++)
			{
				m_OfficeDemandFactors[k] = 0;
			}
			for (int l = 0; l < m_CityServiceChunks.Length; l++)
			{
				ArchetypeChunk archetypeChunk = m_CityServiceChunks[l];
				if (!archetypeChunk.Has(ref m_ServiceUpkeepType))
				{
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabType);
				for (int m = 0; m < nativeArray2.Length; m++)
				{
					Entity prefab = nativeArray2[m].m_Prefab;
					Entity entity = nativeArray[m];
					if (m_ServiceUpkeeps.HasBuffer(prefab))
					{
						DynamicBuffer<ServiceUpkeepData> dynamicBuffer = m_ServiceUpkeeps[prefab];
						for (int n = 0; n < dynamicBuffer.Length; n++)
						{
							ServiceUpkeepData serviceUpkeepData = dynamicBuffer[n];
							if (serviceUpkeepData.m_Upkeep.m_Resource != Resource.Money)
							{
								int amount = serviceUpkeepData.m_Upkeep.m_Amount;
								m_ResourceDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData.m_Upkeep.m_Resource)] += amount;
							}
						}
					}
					if (!m_InstalledUpgrades.HasBuffer(entity))
					{
						continue;
					}
					DynamicBuffer<InstalledUpgrade> dynamicBuffer2 = m_InstalledUpgrades[entity];
					for (int num = 0; num < dynamicBuffer2.Length; num++)
					{
						Entity upgrade = dynamicBuffer2[num].m_Upgrade;
						if (BuildingUtils.CheckOption(dynamicBuffer2[num], BuildingOption.Inactive) || !m_Prefabs.HasComponent(upgrade))
						{
							continue;
						}
						Entity prefab2 = m_Prefabs[upgrade].m_Prefab;
						if (m_Upkeeps.HasBuffer(prefab2))
						{
							DynamicBuffer<ServiceUpkeepData> dynamicBuffer3 = m_Upkeeps[prefab2];
							for (int num2 = 0; num2 < dynamicBuffer3.Length; num2++)
							{
								ServiceUpkeepData serviceUpkeepData2 = dynamicBuffer3[num2];
								m_ResourceDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData2.m_Upkeep.m_Resource)] += serviceUpkeepData2.m_Upkeep.m_Amount;
							}
						}
					}
				}
			}
			for (int num3 = 0; num3 < m_StorageCompanyChunks.Length; num3++)
			{
				ArchetypeChunk archetypeChunk2 = m_StorageCompanyChunks[num3];
				NativeArray<Entity> nativeArray3 = archetypeChunk2.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk2.GetNativeArray(ref m_PrefabType);
				for (int num4 = 0; num4 < nativeArray3.Length; num4++)
				{
					Entity entity2 = nativeArray3[num4];
					Entity prefab3 = nativeArray4[num4].m_Prefab;
					if (m_IndustrialProcessDatas.HasComponent(prefab3))
					{
						int resourceIndex2 = EconomyUtils.GetResourceIndex(m_IndustrialProcessDatas[prefab3].m_Output.m_Resource);
						m_Storages[resourceIndex2]++;
						StorageLimitData storageLimitData = m_StorageLimitDatas[prefab3];
						if (!m_PropertyRenters.HasComponent(entity2) || !m_Prefabs.HasComponent(m_PropertyRenters[entity2].m_Property))
						{
							m_FreeStorages[resourceIndex2]--;
							m_StorageCapacities[resourceIndex2] += kStorageCompanyEstimateLimit;
						}
						else
						{
							Entity property = m_PropertyRenters[entity2].m_Property;
							Entity prefab4 = m_Prefabs[property].m_Prefab;
							m_StorageCapacities[resourceIndex2] += storageLimitData.GetAdjustedLimitForWarehouse(m_SpawnableBuildingDatas[prefab4], m_BuildingDatas[prefab4]);
						}
					}
				}
			}
			for (int num5 = 0; num5 < m_IndustrialPropertyChunks.Length; num5++)
			{
				ArchetypeChunk archetypeChunk3 = m_IndustrialPropertyChunks[num5];
				if (!archetypeChunk3.Has(ref m_PropertyOnMarketType))
				{
					continue;
				}
				NativeArray<Entity> nativeArray5 = archetypeChunk3.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray6 = archetypeChunk3.GetNativeArray(ref m_PrefabType);
				for (int num6 = 0; num6 < nativeArray6.Length; num6++)
				{
					Entity prefab5 = nativeArray6[num6].m_Prefab;
					if (!m_BuildingPropertyDatas.HasComponent(prefab5))
					{
						continue;
					}
					BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab5];
					if (m_Attached.TryGetComponent(nativeArray5[num6], out var componentData) && m_Prefabs.TryGetComponent(componentData.m_Parent, out var componentData2) && m_BuildingPropertyDatas.TryGetComponent(componentData2.m_Prefab, out var componentData3))
					{
						buildingPropertyData.m_AllowedManufactured &= componentData3.m_AllowedManufactured;
					}
					ResourceIterator iterator2 = ResourceIterator.GetIterator();
					while (iterator2.Next())
					{
						int resourceIndex3 = EconomyUtils.GetResourceIndex(iterator2.resource);
						if ((buildingPropertyData.m_AllowedManufactured & iterator2.resource) != Resource.NoResource)
						{
							m_FreeProperties[resourceIndex3]++;
						}
						if ((buildingPropertyData.m_AllowedStored & iterator2.resource) != Resource.NoResource)
						{
							m_FreeStorages[resourceIndex3]++;
						}
					}
				}
			}
			_ = m_IndustrialBuildingDemand.value;
			bool flag2 = m_OfficeBuildingDemand.value > 0;
			_ = m_StorageBuildingDemand.value;
			m_IndustrialCompanyDemand.value = 0;
			m_IndustrialBuildingDemand.value = 0;
			m_StorageCompanyDemand.value = 0;
			m_StorageBuildingDemand.value = 0;
			m_OfficeCompanyDemand.value = 0;
			m_OfficeBuildingDemand.value = 0;
			int num7 = 0;
			int num8 = 0;
			iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex4 = EconomyUtils.GetResourceIndex(iterator.resource);
				if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
				{
					continue;
				}
				ResourceData resourceData2 = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
				bool isProduceable = resourceData2.m_IsProduceable;
				bool isMaterial = resourceData2.m_IsMaterial;
				bool isTradable = resourceData2.m_IsTradable;
				bool flag3 = resourceData2.m_Weight == 0f;
				if (isTradable && !flag3)
				{
					int num9 = m_ResourceDemands[resourceIndex4];
					m_StorageCompanyDemands[resourceIndex4] = 0;
					m_StorageBuildingDemands[resourceIndex4] = 0;
					if (num9 > kStorageProductionDemand && m_StorageCapacities[resourceIndex4] < num9)
					{
						m_StorageCompanyDemands[resourceIndex4] = 1;
					}
					if (m_FreeStorages[resourceIndex4] < 0)
					{
						m_StorageBuildingDemands[resourceIndex4] = 1;
					}
					m_StorageCompanyDemand.value += m_StorageCompanyDemands[resourceIndex4];
					m_StorageBuildingDemand.value += m_StorageBuildingDemands[resourceIndex4];
					m_IndustrialDemandFactors[17] += math.max(0, m_StorageBuildingDemands[resourceIndex4]);
				}
				if (!isProduceable)
				{
					continue;
				}
				float value = (isMaterial ? m_DemandParameters.m_ExtractorBaseDemand : m_DemandParameters.m_IndustrialBaseDemand);
				float num10 = (1f + (float)m_ResourceDemands[resourceIndex4] - (float)m_Productions[resourceIndex4]) / ((float)m_ResourceDemands[resourceIndex4] + 1f);
				if (iterator.resource == Resource.Electronics)
				{
					CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.IndustrialElectronicsDemand);
				}
				else if (iterator.resource == Resource.Software)
				{
					CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.OfficeSoftwareDemand);
				}
				int num11 = (flag3 ? TaxSystem.GetOfficeTaxRate(iterator.resource, m_TaxRates) : TaxSystem.GetIndustrialTaxRate(iterator.resource, m_TaxRates));
				float num12 = m_DemandParameters.m_TaxEffect.z * -0.05f * ((float)num11 - 10f);
				num12 += m_IndustrialOfficeTaxEffectDemandOffset;
				float num13 = 100f * num12;
				int num14 = 0;
				int num15 = 0;
				float num16 = m_DemandParameters.m_NeutralUnemployment / 100f;
				for (int num17 = 0; num17 < 5; num17++)
				{
					if (num17 < 2)
					{
						num15 += (int)((float)m_EmployableByEducation[num17] * (1f - num16)) - m_FreeWorkplaces[num17];
					}
					else
					{
						num14 += (int)((float)m_EmployableByEducation[num17] * (1f - num16)) - m_FreeWorkplaces[num17];
					}
				}
				float num18 = 50f * math.max(0f, value * num10);
				if (num13 > 0f)
				{
					num14 = (int)MapAndClaimWorkforceEffect(num14, 0f - math.max(10f + num13, 10f), 10f);
					num15 = (int)MapAndClaimWorkforceEffect(num15, 0f - math.max(10f + num13, 10f), 15f);
				}
				else
				{
					num14 = math.clamp(num14, -10, 10);
					num15 = math.clamp(num15, -10, 15);
				}
				if (flag3)
				{
					m_IndustrialCompanyDemands[resourceIndex4] = ((num18 > 0f) ? Mathf.RoundToInt(num18 + num13 + (float)num14) : 0);
					m_IndustrialCompanyDemands[resourceIndex4] = math.min(100, math.max(0, m_IndustrialCompanyDemands[resourceIndex4]));
					m_OfficeCompanyDemand.value += Mathf.RoundToInt(m_IndustrialCompanyDemands[resourceIndex4]);
					num7++;
				}
				else
				{
					m_IndustrialCompanyDemands[resourceIndex4] = Mathf.RoundToInt(num18 + num13 + (float)num14 + (float)num15);
					m_IndustrialCompanyDemands[resourceIndex4] = math.min(100, math.max(0, m_IndustrialCompanyDemands[resourceIndex4]));
					m_IndustrialCompanyDemand.value += Mathf.RoundToInt(m_IndustrialCompanyDemands[resourceIndex4]);
					if (!isMaterial)
					{
						num8++;
					}
				}
				if (m_ResourceDemands[resourceIndex4] > 0)
				{
					if (!isMaterial && m_IndustrialCompanyDemands[resourceIndex4] > 0)
					{
						m_IndustrialBuildingDemands[resourceIndex4] = ((m_FreeProperties[resourceIndex4] - m_Propertyless[resourceIndex4] <= 0) ? 50 : 0);
					}
					else if (m_IndustrialCompanyDemands[resourceIndex4] > 0)
					{
						m_IndustrialBuildingDemands[resourceIndex4] = 1;
					}
					else
					{
						m_IndustrialBuildingDemands[resourceIndex4] = 0;
					}
					if (m_IndustrialBuildingDemands[resourceIndex4] > 0)
					{
						if (flag3)
						{
							m_OfficeBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialCompanyDemands[resourceIndex4] : 0);
						}
						else if (!isMaterial)
						{
							m_IndustrialBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialCompanyDemands[resourceIndex4] : 0);
						}
					}
				}
				else
				{
					m_IndustrialBuildingDemands[resourceIndex4] = 0;
				}
				if (isMaterial)
				{
					continue;
				}
				if (flag3)
				{
					if (!flag2 || (m_IndustrialBuildingDemands[resourceIndex4] > 0 && m_IndustrialCompanyDemands[resourceIndex4] > 0))
					{
						m_OfficeDemandFactors[2] += num14;
						m_OfficeDemandFactors[4] += (int)num18;
						m_OfficeDemandFactors[11] += (int)num13;
						m_OfficeDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex4];
					}
				}
				else
				{
					m_IndustrialDemandFactors[2] += num14;
					m_IndustrialDemandFactors[1] += num15;
					m_IndustrialDemandFactors[4] += (int)num18;
					m_IndustrialDemandFactors[11] += (int)num13;
					m_IndustrialDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex4];
				}
			}
			m_OfficeDemandFactors[4] = ((m_OfficeDemandFactors[4] == 0) ? (-1) : m_OfficeDemandFactors[4]);
			m_IndustrialDemandFactors[4] = ((m_IndustrialDemandFactors[4] == 0) ? (-1) : m_IndustrialDemandFactors[4]);
			m_IndustrialDemandFactors[13] = ((m_IndustrialDemandFactors[13] == 0) ? (-1) : m_IndustrialDemandFactors[13]);
			m_OfficeDemandFactors[13] = ((m_OfficeDemandFactors[13] == 0) ? (-1) : m_OfficeDemandFactors[13]);
			if (m_Populations[m_City].m_Population <= 0)
			{
				m_OfficeDemandFactors[4] = 0;
				m_IndustrialDemandFactors[4] = 0;
			}
			if (m_IndustrialPropertyChunks.Length == 0)
			{
				m_IndustrialDemandFactors[18] = m_IndustrialDemandFactors[13];
				m_IndustrialDemandFactors[13] = 0;
			}
			if (m_OfficePropertyChunks.Length == 0)
			{
				m_OfficeDemandFactors[18] = m_OfficeDemandFactors[13];
				m_OfficeDemandFactors[13] = 0;
			}
			m_StorageBuildingDemand.value = Mathf.CeilToInt(math.pow(20f * (float)m_StorageBuildingDemand.value, 0.75f));
			m_IndustrialBuildingDemand.value = (flag ? (2 * m_IndustrialBuildingDemand.value / num8) : 0);
			m_OfficeCompanyDemand.value *= 2 * m_OfficeCompanyDemand.value / num7;
			m_IndustrialBuildingDemand.value = math.clamp(m_IndustrialBuildingDemand.value, 0, 100);
			m_OfficeBuildingDemand.value = math.clamp(m_OfficeBuildingDemand.value, 0, 100);
			if (m_UnlimitedDemand)
			{
				m_OfficeBuildingDemand.value = 100;
				m_IndustrialBuildingDemand.value = 100;
			}
		}

		private float MapAndClaimWorkforceEffect(float value, float min, float max)
		{
			if (value < 0f)
			{
				float valueToClamp = math.unlerp(-2000f, 0f, value);
				valueToClamp = math.clamp(valueToClamp, 0f, 1f);
				return math.lerp(min, 0f, valueToClamp);
			}
			float valueToClamp2 = math.unlerp(0f, 20f, value);
			valueToClamp2 = math.clamp(valueToClamp2, 0f, 1f);
			return math.lerp(0f, max, valueToClamp2);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CityServiceUpkeep>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyOnMarket>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
		}
	}

	private static readonly int kStorageProductionDemand = 2000;

	private static readonly int kStorageCompanyEstimateLimit = 864000;

	private ResourceSystem m_ResourceSystem;

	private CitySystem m_CitySystem;

	private ClimateSystem m_ClimateSystem;

	private TaxSystem m_TaxSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private CountCompanyDataSystem m_CountCompanyDataSystem;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_DemandParameterQuery;

	private EntityQuery m_IndustrialQuery;

	private EntityQuery m_OfficeQuery;

	private EntityQuery m_StorageCompanyQuery;

	private EntityQuery m_ProcessDataQuery;

	private EntityQuery m_CityServiceQuery;

	private EntityQuery m_UnlockedZoneDataQuery;

	private EntityQuery m_GameModeSettingQuery;

	private NativeValue<int> m_IndustrialCompanyDemand;

	private NativeValue<int> m_IndustrialBuildingDemand;

	private NativeValue<int> m_StorageCompanyDemand;

	private NativeValue<int> m_StorageBuildingDemand;

	private NativeValue<int> m_OfficeCompanyDemand;

	private NativeValue<int> m_OfficeBuildingDemand;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ResourceDemands;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_IndustrialDemandFactors;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_OfficeDemandFactors;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_IndustrialCompanyDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_IndustrialZoningDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_IndustrialBuildingDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_StorageBuildingDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_StorageCompanyDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_FreeProperties;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_FreeStorages;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_Storages;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_StorageCapacities;

	[DebugWatchDeps]
	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private int m_LastIndustrialCompanyDemand;

	private int m_LastIndustrialBuildingDemand;

	private int m_LastStorageCompanyDemand;

	private int m_LastStorageBuildingDemand;

	private int m_LastOfficeCompanyDemand;

	private int m_LastOfficeBuildingDemand;

	private float m_IndustrialOfficeTaxEffectDemandOffset;

	private bool m_UnlimitedDemand;

	private TypeHandle __TypeHandle;

	[DebugWatchValue(color = "#f7dc6f")]
	public int industrialCompanyDemand => m_LastIndustrialCompanyDemand;

	[DebugWatchValue(color = "#b7950b")]
	public int industrialBuildingDemand => m_LastIndustrialBuildingDemand;

	[DebugWatchValue(color = "#cccccc")]
	public int storageCompanyDemand => m_LastStorageCompanyDemand;

	[DebugWatchValue(color = "#999999")]
	public int storageBuildingDemand => m_LastStorageBuildingDemand;

	[DebugWatchValue(color = "#af7ac5")]
	public int officeCompanyDemand => m_LastOfficeCompanyDemand;

	[DebugWatchValue(color = "#6c3483")]
	public int officeBuildingDemand => m_LastOfficeBuildingDemand;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 7;
	}

	public void SetUnlimitedDemand(bool unlimited)
	{
		m_UnlimitedDemand = unlimited;
	}

	public NativeArray<int> GetConsumption(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_ResourceDemands;
	}

	public NativeArray<int> GetIndustrialDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_IndustrialDemandFactors;
	}

	public NativeArray<int> GetOfficeDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_OfficeDemandFactors;
	}

	public NativeArray<int> GetResourceDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_IndustrialCompanyDemands;
	}

	public NativeArray<int> GetBuildingDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_IndustrialBuildingDemands;
	}

	public NativeArray<int> GetStorageCompanyDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_StorageCompanyDemands;
	}

	public NativeArray<int> GetStorageBuildingDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_StorageBuildingDemands;
	}

	public NativeArray<int> GetIndustrialResourceDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_ResourceDemands;
	}

	public void AddReader(JobHandle reader)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_ResourceDemands.Fill(0);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			m_IndustrialOfficeTaxEffectDemandOffset = 0f;
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable)
		{
			m_IndustrialOfficeTaxEffectDemandOffset = singleton.m_IndustrialOfficeTaxEffectDemandOffset;
		}
		else
		{
			m_IndustrialOfficeTaxEffectDemandOffset = 0f;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_IndustrialQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Condemned>());
		m_OfficeQuery = GetEntityQuery(ComponentType.ReadOnly<OfficeProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Condemned>());
		m_StorageCompanyQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.Exclude<ServiceCompanyData>());
		m_CityServiceQuery = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
		m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_IndustrialCompanyDemand = new NativeValue<int>(Allocator.Persistent);
		m_IndustrialBuildingDemand = new NativeValue<int>(Allocator.Persistent);
		m_StorageCompanyDemand = new NativeValue<int>(Allocator.Persistent);
		m_StorageBuildingDemand = new NativeValue<int>(Allocator.Persistent);
		m_OfficeCompanyDemand = new NativeValue<int>(Allocator.Persistent);
		m_OfficeBuildingDemand = new NativeValue<int>(Allocator.Persistent);
		m_IndustrialDemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		m_OfficeDemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		int resourceCount = EconomyUtils.ResourceCount;
		m_IndustrialCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_IndustrialZoningDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_IndustrialBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_StorageBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_StorageCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_FreeStorages = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_Storages = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_StorageCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_IndustrialOfficeTaxEffectDemandOffset = 0f;
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_DemandParameterQuery);
		RequireForUpdate(m_ProcessDataQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_IndustrialCompanyDemand.Dispose();
		m_IndustrialBuildingDemand.Dispose();
		m_StorageCompanyDemand.Dispose();
		m_StorageBuildingDemand.Dispose();
		m_OfficeCompanyDemand.Dispose();
		m_OfficeBuildingDemand.Dispose();
		m_IndustrialDemandFactors.Dispose();
		m_OfficeDemandFactors.Dispose();
		m_IndustrialCompanyDemands.Dispose();
		m_IndustrialZoningDemands.Dispose();
		m_IndustrialBuildingDemands.Dispose();
		m_StorageBuildingDemands.Dispose();
		m_StorageCompanyDemands.Dispose();
		m_ResourceDemands.Dispose();
		m_FreeProperties.Dispose();
		m_Storages.Dispose();
		m_FreeStorages.Dispose();
		m_StorageCapacities.Dispose();
		base.OnDestroy();
	}

	public void SetDefaults(Context context)
	{
		m_IndustrialCompanyDemand.value = 0;
		m_IndustrialBuildingDemand.value = 0;
		m_StorageCompanyDemand.value = 0;
		m_StorageBuildingDemand.value = 0;
		m_OfficeCompanyDemand.value = 0;
		m_OfficeBuildingDemand.value = 0;
		m_IndustrialDemandFactors.Fill(0);
		m_OfficeDemandFactors.Fill(0);
		m_IndustrialCompanyDemands.Fill(0);
		m_IndustrialZoningDemands.Fill(0);
		m_IndustrialBuildingDemands.Fill(0);
		m_StorageBuildingDemands.Fill(0);
		m_StorageCompanyDemands.Fill(0);
		m_FreeProperties.Fill(0);
		m_Storages.Fill(0);
		m_FreeStorages.Fill(0);
		m_LastIndustrialCompanyDemand = 0;
		m_LastIndustrialBuildingDemand = 0;
		m_LastStorageCompanyDemand = 0;
		m_LastStorageBuildingDemand = 0;
		m_LastOfficeCompanyDemand = 0;
		m_LastOfficeBuildingDemand = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_IndustrialCompanyDemand.value;
		writer.Write(value);
		int value2 = m_IndustrialBuildingDemand.value;
		writer.Write(value2);
		int value3 = m_StorageCompanyDemand.value;
		writer.Write(value3);
		int value4 = m_StorageBuildingDemand.value;
		writer.Write(value4);
		int value5 = m_OfficeCompanyDemand.value;
		writer.Write(value5);
		int value6 = m_OfficeBuildingDemand.value;
		writer.Write(value6);
		int length = m_IndustrialDemandFactors.Length;
		writer.Write(length);
		NativeArray<int> value7 = m_IndustrialDemandFactors;
		writer.Write(value7);
		NativeArray<int> value8 = m_OfficeDemandFactors;
		writer.Write(value8);
		NativeArray<int> value9 = m_IndustrialCompanyDemands;
		writer.Write(value9);
		NativeArray<int> value10 = m_IndustrialZoningDemands;
		writer.Write(value10);
		NativeArray<int> value11 = m_IndustrialBuildingDemands;
		writer.Write(value11);
		NativeArray<int> value12 = m_StorageBuildingDemands;
		writer.Write(value12);
		NativeArray<int> value13 = m_StorageCompanyDemands;
		writer.Write(value13);
		NativeArray<int> value14 = m_FreeProperties;
		writer.Write(value14);
		NativeArray<int> value15 = m_Storages;
		writer.Write(value15);
		NativeArray<int> value16 = m_FreeStorages;
		writer.Write(value16);
		int value17 = m_LastIndustrialCompanyDemand;
		writer.Write(value17);
		int value18 = m_LastIndustrialBuildingDemand;
		writer.Write(value18);
		int value19 = m_LastStorageCompanyDemand;
		writer.Write(value19);
		int value20 = m_LastStorageBuildingDemand;
		writer.Write(value20);
		int value21 = m_LastOfficeCompanyDemand;
		writer.Write(value21);
		int value22 = m_LastOfficeBuildingDemand;
		writer.Write(value22);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_IndustrialCompanyDemand.value = value;
		reader.Read(out int value2);
		m_IndustrialBuildingDemand.value = value2;
		reader.Read(out int value3);
		m_StorageCompanyDemand.value = value3;
		reader.Read(out int value4);
		m_StorageBuildingDemand.value = value4;
		reader.Read(out int value5);
		m_OfficeCompanyDemand.value = value5;
		reader.Read(out int value6);
		m_OfficeBuildingDemand.value = value6;
		if (reader.context.version < Version.demandFactorCountSerialization)
		{
			NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
			NativeArray<int> value7 = nativeArray;
			reader.Read(value7);
			CollectionUtils.CopySafe(nativeArray, m_IndustrialDemandFactors);
			NativeArray<int> value8 = nativeArray;
			reader.Read(value8);
			CollectionUtils.CopySafe(nativeArray, m_OfficeDemandFactors);
			nativeArray.Dispose();
		}
		else
		{
			reader.Read(out int value9);
			if (value9 == m_IndustrialDemandFactors.Length)
			{
				NativeArray<int> value10 = m_IndustrialDemandFactors;
				reader.Read(value10);
				NativeArray<int> value11 = m_OfficeDemandFactors;
				reader.Read(value11);
			}
			else
			{
				NativeArray<int> nativeArray2 = new NativeArray<int>(value9, Allocator.Temp);
				NativeArray<int> value12 = nativeArray2;
				reader.Read(value12);
				CollectionUtils.CopySafe(nativeArray2, m_IndustrialDemandFactors);
				NativeArray<int> value13 = nativeArray2;
				reader.Read(value13);
				CollectionUtils.CopySafe(nativeArray2, m_OfficeDemandFactors);
				nativeArray2.Dispose();
			}
		}
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> value14 = m_IndustrialCompanyDemands;
			reader.Read(value14);
			NativeArray<int> value15 = m_IndustrialZoningDemands;
			reader.Read(value15);
			NativeArray<int> value16 = m_IndustrialBuildingDemands;
			reader.Read(value16);
			NativeArray<int> value17 = m_StorageBuildingDemands;
			reader.Read(value17);
			NativeArray<int> value18 = m_StorageCompanyDemands;
			reader.Read(value18);
		}
		else
		{
			NativeArray<int> subArray = m_IndustrialCompanyDemands.GetSubArray(0, 40);
			reader.Read(subArray);
			NativeArray<int> subArray2 = m_IndustrialZoningDemands.GetSubArray(0, 40);
			reader.Read(subArray2);
			NativeArray<int> subArray3 = m_IndustrialBuildingDemands.GetSubArray(0, 40);
			reader.Read(subArray3);
			NativeArray<int> subArray4 = m_StorageBuildingDemands.GetSubArray(0, 40);
			reader.Read(subArray4);
			NativeArray<int> subArray5 = m_StorageCompanyDemands.GetSubArray(0, 40);
			reader.Read(subArray5);
			m_IndustrialCompanyDemands[40] = 0;
			m_IndustrialZoningDemands[40] = 0;
			m_IndustrialBuildingDemands[40] = 0;
			m_StorageBuildingDemands[40] = 0;
			m_StorageCompanyDemands[40] = 0;
		}
		if (reader.context.version <= Version.companyDemandOptimization)
		{
			NativeArray<int> value19 = new NativeArray<int>(40, Allocator.Temp);
			reader.Read(value19);
			reader.Read(value19);
			if (reader.context.version <= Version.demandFactorCountSerialization)
			{
				reader.Read(value19);
				reader.Read(value19);
			}
			reader.Read(value19);
			reader.Read(value19);
		}
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> value20 = m_FreeProperties;
			reader.Read(value20);
			NativeArray<int> value21 = m_Storages;
			reader.Read(value21);
			NativeArray<int> value22 = m_FreeStorages;
			reader.Read(value22);
		}
		else
		{
			NativeArray<int> subArray6 = m_FreeProperties.GetSubArray(0, 40);
			reader.Read(subArray6);
			NativeArray<int> subArray7 = m_Storages.GetSubArray(0, 40);
			reader.Read(subArray7);
			NativeArray<int> subArray8 = m_FreeStorages.GetSubArray(0, 40);
			reader.Read(subArray8);
			m_FreeProperties[40] = 0;
			m_Storages[40] = 0;
			m_FreeStorages[40] = 0;
		}
		ref int value23 = ref m_LastIndustrialCompanyDemand;
		reader.Read(out value23);
		ref int value24 = ref m_LastIndustrialBuildingDemand;
		reader.Read(out value24);
		ref int value25 = ref m_LastStorageCompanyDemand;
		reader.Read(out value25);
		ref int value26 = ref m_LastStorageBuildingDemand;
		reader.Read(out value26);
		ref int value27 = ref m_LastOfficeCompanyDemand;
		reader.Read(out value27);
		ref int value28 = ref m_LastOfficeBuildingDemand;
		reader.Read(out value28);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
		{
			m_LastIndustrialCompanyDemand = m_IndustrialCompanyDemand.value;
			m_LastIndustrialBuildingDemand = m_IndustrialBuildingDemand.value;
			m_LastStorageCompanyDemand = m_StorageCompanyDemand.value;
			m_LastStorageBuildingDemand = m_StorageBuildingDemand.value;
			m_LastOfficeCompanyDemand = m_OfficeCompanyDemand.value;
			m_LastOfficeBuildingDemand = m_OfficeBuildingDemand.value;
			JobHandle deps;
			CountCompanyDataSystem.IndustrialCompanyDatas industrialCompanyDatas = m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out deps);
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			JobHandle outJobHandle3;
			JobHandle outJobHandle4;
			JobHandle deps2;
			UpdateIndustrialDemandJob jobData = new UpdateIndustrialDemandJob
			{
				m_IndustrialPropertyChunks = m_IndustrialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_OfficePropertyChunks = m_OfficeQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle3),
				m_CityServiceChunks = m_CityServiceQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle4),
				m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceUpkeepType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PropertyOnMarketType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageLimitDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Attached = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpkeeps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_Upkeeps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
				m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
				m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables(),
				m_HouseholdResourceDemands = m_CountHouseholdDataSystem.GetResourceNeeds(out deps2),
				m_TaxRates = m_TaxSystem.GetTaxRates(),
				m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces(),
				m_City = m_CitySystem.City,
				m_IndustrialCompanyDemand = m_IndustrialCompanyDemand,
				m_IndustrialBuildingDemand = m_IndustrialBuildingDemand,
				m_StorageCompanyDemand = m_StorageCompanyDemand,
				m_StorageBuildingDemand = m_StorageBuildingDemand,
				m_OfficeCompanyDemand = m_OfficeCompanyDemand,
				m_OfficeBuildingDemand = m_OfficeBuildingDemand,
				m_IndustrialCompanyDemands = m_IndustrialCompanyDemands,
				m_IndustrialBuildingDemands = m_IndustrialBuildingDemands,
				m_StorageBuildingDemands = m_StorageBuildingDemands,
				m_StorageCompanyDemands = m_StorageCompanyDemands,
				m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless,
				m_CompanyResourceDemands = industrialCompanyDatas.m_Demand,
				m_FreeProperties = m_FreeProperties,
				m_Productions = industrialCompanyDatas.m_Production,
				m_Storages = m_Storages,
				m_FreeStorages = m_FreeStorages,
				m_StorageCapacities = m_StorageCapacities,
				m_IndustrialDemandFactors = m_IndustrialDemandFactors,
				m_OfficeDemandFactors = m_OfficeDemandFactors,
				m_ResourceDemands = m_ResourceDemands,
				m_IndustrialOfficeTaxEffectDemandOffset = m_IndustrialOfficeTaxEffectDemandOffset,
				m_UnlimitedDemand = m_UnlimitedDemand
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, outJobHandle2, deps, outJobHandle3, outJobHandle4, deps2));
			m_WriteDependencies = base.Dependency;
			m_CountCompanyDataSystem.AddReader(base.Dependency);
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
			m_TaxSystem.AddReader(base.Dependency);
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
	public IndustrialDemandSystem()
	{
	}
}
