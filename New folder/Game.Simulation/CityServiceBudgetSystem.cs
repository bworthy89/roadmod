using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityServiceBudgetSystem : GameSystemBase, ICityServiceBudgetSystem
{
	private struct UpdateDataJob : IJob
	{
		[ReadOnly]
		public BufferLookup<CityStatistic> m_Stats;

		[ReadOnly]
		public NativeList<Entity> m_CityServiceEntities;

		public Entity m_City;

		[ReadOnly]
		public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_Lookup;

		public NativeList<CollectedCityServiceUpkeepData> m_CityServiceUpkeeps;

		public NativeParallelHashMap<Entity, CollectedCityServiceBudgetData> m_CityServiceBudgets;

		[ReadOnly]
		public ComponentLookup<CollectedCityServiceBudgetData> m_CollectedBudgets;

		[ReadOnly]
		public ComponentLookup<Creditworthiness> m_Creditworthiness;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<CollectedCityServiceUpkeepData> m_CollectedUpkeeps;

		public NativeParallelHashMap<Entity, int2> m_CityServiceUpkeepIndices;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public NativeArray<int> m_Income;

		public NativeArray<int> m_Expenses;

		public Entity m_BudgetEntity;

		public int m_MonthlySubsidy;

		[ReadOnly]
		public BufferLookup<ServiceBudgetData> m_BudgetDatas;

		[ReadOnly]
		public NativeList<CollectedCityServiceFeeData> m_CollectedFees;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_ServiceFees;

		[ReadOnly]
		public ComponentLookup<Loan> m_Loans;

		[ReadOnly]
		public OutsideTradeParameterData m_OutsideTradeParameterData;

		[ReadOnly]
		public EconomyParameterData m_EconomyParametersData;

		[ReadOnly]
		public int4 m_ServiceFacilityBuildingCount;

		[ReadOnly]
		public int m_PoliceStationBuildingCount;

		[ReadOnly]
		public int m_MapTileUpkeepCost;

		public NativeReference<int> m_TotalTaxes;

		public void Execute()
		{
			if (!m_ServiceFees.HasBuffer(m_City))
			{
				return;
			}
			DynamicBuffer<ServiceFee> fees = m_ServiceFees[m_City];
			m_CityServiceUpkeeps.Clear();
			for (int i = 0; i < m_CityServiceEntities.Length; i++)
			{
				Entity entity = m_CityServiceEntities[i];
				m_CityServiceBudgets[entity] = m_CollectedBudgets[entity];
				if (m_CollectedUpkeeps.HasBuffer(entity))
				{
					DynamicBuffer<CollectedCityServiceUpkeepData> dynamicBuffer = m_CollectedUpkeeps[entity];
					m_CityServiceUpkeepIndices[entity] = new int2(m_CityServiceUpkeeps.Length, dynamicBuffer.Length);
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						m_CityServiceUpkeeps.Add(dynamicBuffer[j]);
					}
				}
			}
			for (int k = 0; k < 15; k++)
			{
				ExpenseSource expenseSource = (ExpenseSource)k;
				m_Expenses[k] = 0;
				switch (expenseSource)
				{
				case ExpenseSource.ServiceUpkeep:
				{
					for (int l = 0; l < m_CityServiceEntities.Length; l++)
					{
						int serviceBudget = GetServiceBudget(m_CityServiceEntities[l], m_CityServiceBudgets, m_BudgetEntity, m_BudgetDatas);
						m_Expenses[k] += GetEstimatedServiceUpkeep(m_CityServiceBudgets[m_CityServiceEntities[l]], m_CityServiceUpkeepIndices[m_CityServiceEntities[l]], serviceBudget, m_CityServiceUpkeeps);
					}
					break;
				}
				case ExpenseSource.LoanInterest:
				{
					LoanInfo loanInfo = LoanSystem.CalculateLoan(m_Loans[m_City].m_Amount, m_Creditworthiness[m_City].m_Amount, m_CityModifiers[m_City], m_EconomyParametersData.m_LoanMinMaxInterestRate);
					m_Expenses[k] = loanInfo.m_DailyPayment;
					break;
				}
				case ExpenseSource.ImportElectricity:
					m_Expenses[k] = ServiceFeeSystem.GetServiceFees(PlayerResource.Electricity, m_CollectedFees).z;
					break;
				case ExpenseSource.ImportWater:
					m_Expenses[k] = ServiceFeeSystem.GetServiceFees(PlayerResource.Water, m_CollectedFees).z;
					break;
				case ExpenseSource.ExportSewage:
					m_Expenses[k] = ServiceFeeSystem.GetServiceFees(PlayerResource.Sewage, m_CollectedFees).z;
					break;
				case ExpenseSource.SubsidyCommercial:
					m_Expenses[k] = -TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Commercial, TaxResultType.Expense, m_Lookup, m_Stats, m_TaxRates);
					break;
				case ExpenseSource.SubsidyIndustrial:
					m_Expenses[k] = -TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Industrial, TaxResultType.Expense, m_Lookup, m_Stats, m_TaxRates);
					break;
				case ExpenseSource.SubsidyOffice:
					m_Expenses[k] = -TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Office, TaxResultType.Expense, m_Lookup, m_Stats, m_TaxRates);
					break;
				case ExpenseSource.SubsidyResidential:
					m_Expenses[k] = -TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Residential, TaxResultType.Expense, m_Lookup, m_Stats, m_TaxRates);
					break;
				case ExpenseSource.ImportPoliceService:
					if (CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
					{
						m_Expenses[k] = -GetImportedPoliceServiceFee();
					}
					break;
				case ExpenseSource.ImportAmbulanceService:
					if (CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
					{
						m_Expenses[k] = -GetImportedAmbulanceServiceFee();
					}
					break;
				case ExpenseSource.ImportGarbageService:
					if (CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
					{
						m_Expenses[k] = -GetImportedGarbageServiceFee();
					}
					break;
				case ExpenseSource.ImportHearseService:
					if (CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
					{
						m_Expenses[k] = -GetImportedHearseServiceFee();
					}
					break;
				case ExpenseSource.ImportFireEngineService:
					if (CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
					{
						m_Expenses[k] = -GetImportedFireEngineServiceFee();
					}
					break;
				case ExpenseSource.MapTileUpkeep:
					m_Expenses[k] = m_MapTileUpkeepCost;
					break;
				}
			}
			for (int m = 0; m < 14; m++)
			{
				IncomeSource incomeSource = (IncomeSource)m;
				m_Income[m] = 0;
				switch (incomeSource)
				{
				case IncomeSource.ExportElectricity:
					m_Income[m] = ServiceFeeSystem.GetServiceFees(PlayerResource.Electricity, m_CollectedFees).y;
					break;
				case IncomeSource.ExportWater:
					m_Income[m] = ServiceFeeSystem.GetServiceFees(PlayerResource.Water, m_CollectedFees).y;
					break;
				case IncomeSource.FeeEducation:
					m_Income[m] = ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.BasicEducation, ServiceFeeSystem.GetFee(PlayerResource.BasicEducation, fees), m_CollectedFees) + ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.SecondaryEducation, ServiceFeeSystem.GetFee(PlayerResource.SecondaryEducation, fees), m_CollectedFees) + ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.HigherEducation, ServiceFeeSystem.GetFee(PlayerResource.HigherEducation, fees), m_CollectedFees);
					break;
				case IncomeSource.FeeHealthcare:
					m_Income[m] = ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.Healthcare, ServiceFeeSystem.GetFee(PlayerResource.Healthcare, fees), m_CollectedFees);
					break;
				case IncomeSource.FeeParking:
					m_Income[m] = ServiceFeeSystem.GetServiceFees(PlayerResource.Parking, m_CollectedFees).x;
					break;
				case IncomeSource.FeePublicTransport:
					m_Income[m] = ServiceFeeSystem.GetServiceFees(PlayerResource.PublicTransport, m_CollectedFees).x;
					break;
				case IncomeSource.FeeGarbage:
					m_Income[m] = ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.Garbage, ServiceFeeSystem.GetFee(PlayerResource.Garbage, fees), m_CollectedFees);
					break;
				case IncomeSource.FeeElectricity:
					m_Income[m] = ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.Electricity, ServiceFeeSystem.GetFee(PlayerResource.Electricity, fees), m_CollectedFees);
					break;
				case IncomeSource.TaxCommercial:
					m_Income[m] = TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Commercial, TaxResultType.Income, m_Lookup, m_Stats, m_TaxRates);
					break;
				case IncomeSource.TaxIndustrial:
					m_Income[m] = TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Industrial, TaxResultType.Income, m_Lookup, m_Stats, m_TaxRates);
					break;
				case IncomeSource.TaxOffice:
					m_Income[m] = TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Office, TaxResultType.Income, m_Lookup, m_Stats, m_TaxRates);
					break;
				case IncomeSource.TaxResidential:
					m_Income[m] = TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Residential, TaxResultType.Income, m_Lookup, m_Stats, m_TaxRates);
					break;
				case IncomeSource.FeeWater:
					m_Income[m] = ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.Water, ServiceFeeSystem.GetFee(PlayerResource.Water, fees), m_CollectedFees) + ServiceFeeSystem.GetServiceFeeIncomeEstimate(PlayerResource.Sewage, ServiceFeeSystem.GetFee(PlayerResource.Water, fees), m_CollectedFees);
					break;
				case IncomeSource.GovernmentSubsidy:
					m_Income[m] = m_MonthlySubsidy;
					break;
				}
			}
			m_TotalTaxes.Value = GetTotalTaxIncome(m_Income);
		}

		private int GetImportedAmbulanceServiceFee()
		{
			Population population = m_Populations[m_City];
			float value = m_OutsideTradeParameterData.m_AmbulanceImportServiceFee;
			CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.CityServiceImportCost);
			return -(int)(value * (float)(population.m_Population / m_OutsideTradeParameterData.m_OCServiceTradePopulationRange + 1) * (float)m_OutsideTradeParameterData.m_OCServiceTradePopulationRange);
		}

		private int GetImportedHearseServiceFee()
		{
			Population population = m_Populations[m_City];
			float value = m_OutsideTradeParameterData.m_HearseImportServiceFee;
			CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.CityServiceImportCost);
			return -(int)(value * (float)(population.m_Population / m_OutsideTradeParameterData.m_OCServiceTradePopulationRange + 1) * (float)m_OutsideTradeParameterData.m_OCServiceTradePopulationRange);
		}

		private int GetImportedGarbageServiceFee()
		{
			Population population = m_Populations[m_City];
			float value = m_OutsideTradeParameterData.m_GarbageImportServiceFee;
			CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.CityServiceImportCost);
			return -(int)(value * (float)(population.m_Population / m_OutsideTradeParameterData.m_OCServiceTradePopulationRange + 1) * (float)m_OutsideTradeParameterData.m_OCServiceTradePopulationRange);
		}

		private int GetImportedFireEngineServiceFee()
		{
			Population population = m_Populations[m_City];
			float value = m_OutsideTradeParameterData.m_FireEngineImportServiceFee;
			CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.CityServiceImportCost);
			return -(int)(value * (float)(population.m_Population / m_OutsideTradeParameterData.m_OCServiceTradePopulationRange + 1) * (float)m_OutsideTradeParameterData.m_OCServiceTradePopulationRange);
		}

		private int GetImportedPoliceServiceFee()
		{
			Population population = m_Populations[m_City];
			float value = m_OutsideTradeParameterData.m_PoliceImportServiceFee;
			CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.CityServiceImportCost);
			return -(int)(value * (float)(population.m_Population / m_OutsideTradeParameterData.m_OCServiceTradePopulationRange + 1) * (float)m_OutsideTradeParameterData.m_OCServiceTradePopulationRange);
		}
	}

	[BurstCompile]
	private struct ClearServiceDataJob : IJobChunk
	{
		public ComponentTypeHandle<CollectedCityServiceBudgetData> m_BudgetDataType;

		public BufferTypeHandle<CollectedCityServiceUpkeepData> m_UpkeepDataType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CollectedCityServiceBudgetData> nativeArray = chunk.GetNativeArray(ref m_BudgetDataType);
			BufferAccessor<CollectedCityServiceUpkeepData> bufferAccessor = chunk.GetBufferAccessor(ref m_UpkeepDataType);
			for (int i = 0; i < chunk.Count; i++)
			{
				nativeArray[i] = default(CollectedCityServiceBudgetData);
				bufferAccessor[i].Clear();
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CityServiceBudgetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectDatas;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_ServiceUpkeepDatas;

		[ReadOnly]
		public ComponentLookup<ServiceUsage> m_ServiceUsages;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradeBufs;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeBufs;

		public ComponentLookup<CollectedCityServiceBudgetData> m_BudgetDatas;

		public BufferLookup<CollectedCityServiceUpkeepData> m_UpkeepDatas;

		[ReadOnly]
		public DynamicBuffer<ServiceBudgetData> m_ServiceBudgets;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		[ReadOnly]
		public DynamicBuffer<CityModifier> m_CityModifiers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			bool flag = nativeArray3.Length != 0;
			NativeList<ServiceUpkeepData> totalUpkeepDatas = new NativeList<ServiceUpkeepData>(4, Allocator.Temp);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_ServiceObjectDatas.TryGetComponent(prefab, out var componentData))
				{
					continue;
				}
				Entity service = componentData.m_Service;
				if (!m_BudgetDatas.TryGetComponent(service, out var componentData2) || !m_UpkeepDatas.TryGetBuffer(service, out var bufferData))
				{
					continue;
				}
				componentData2.m_Count++;
				if (m_ServiceUpkeepDatas.HasBuffer(prefab))
				{
					int serviceBudget = GetServiceBudget(service);
					totalUpkeepDatas.Clear();
					bool flag2 = flag && BuildingUtils.CheckOption(nativeArray3[i], BuildingOption.Inactive);
					CityServiceUpkeepSystem.GetUpkeepWithUsageScale(totalUpkeepDatas, m_ServiceUpkeepDatas, m_InstalledUpgradeBufs, m_Prefabs, m_ServiceUsages, entity, prefab, flag2);
					int upkeepOfEmployeeWage = CityServiceUpkeepSystem.GetUpkeepOfEmployeeWage(m_EmployeeBufs, entity, m_EconomyParameterData, flag2);
					for (int j = 0; j < totalUpkeepDatas.Length; j++)
					{
						ServiceUpkeepData serviceUpkeepData = totalUpkeepDatas[j];
						int amount = serviceUpkeepData.m_Upkeep.m_Amount;
						float value = (float)amount * EconomyUtils.GetMarketPrice(serviceUpkeepData.m_Upkeep.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
						float f = value;
						if (serviceUpkeepData.m_Upkeep.m_Resource == Resource.Money)
						{
							CityUtils.ApplyModifier(ref value, m_CityModifiers, CityModifierType.CityServiceBuildingBaseUpkeepCost);
							value += (float)upkeepOfEmployeeWage;
							if (flag2)
							{
								value *= 0.1f;
							}
							f = value * ((float)serviceBudget / 100f);
						}
						if (amount > 0)
						{
							ref CollectedCityServiceUpkeepData orCreateUpkeepData = ref GetOrCreateUpkeepData(bufferData, serviceUpkeepData.m_Upkeep.m_Resource);
							orCreateUpkeepData.m_Amount += amount;
							orCreateUpkeepData.m_Cost += Mathf.RoundToInt(f);
							orCreateUpkeepData.m_FullCost += Mathf.RoundToInt(value);
						}
					}
				}
				m_BudgetDatas[service] = componentData2;
			}
		}

		private int GetServiceBudget(Entity service)
		{
			for (int i = 0; i < m_ServiceBudgets.Length; i++)
			{
				if (m_ServiceBudgets[i].m_Service == service)
				{
					return m_ServiceBudgets[i].m_Budget;
				}
			}
			return 100;
		}

		private ref CollectedCityServiceUpkeepData GetOrCreateUpkeepData(DynamicBuffer<CollectedCityServiceUpkeepData> upkeepDatas, Resource resource)
		{
			for (int i = 0; i < upkeepDatas.Length; i++)
			{
				if (upkeepDatas[i].m_Resource == resource)
				{
					return ref upkeepDatas.ElementAt(i);
				}
			}
			int index = upkeepDatas.Add(new CollectedCityServiceUpkeepData
			{
				m_Resource = resource,
				m_Amount = 0,
				m_Cost = 0,
				m_FullCost = 0
			});
			return ref upkeepDatas.ElementAt(index);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ClearBuildingDataJob : IJobChunk
	{
		public ComponentTypeHandle<CollectedServiceBuildingBudgetData> m_BudgetType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CollectedServiceBuildingBudgetData> nativeArray = chunk.GetNativeArray(ref m_BudgetType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				nativeArray[i] = default(CollectedServiceBuildingBudgetData);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CollectServiceBuildingBudgetDatasJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		public ComponentLookup<CollectedServiceBuildingBudgetData> m_Budgets;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			if (nativeArray2.Length != 0 && bufferAccessor.Length != 0)
			{
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity prefab = nativeArray[i].m_Prefab;
					if (m_Budgets.TryGetComponent(prefab, out var componentData))
					{
						componentData.m_Count++;
						componentData.m_Workers += bufferAccessor[i].Length;
						componentData.m_Workplaces += nativeArray2[i].m_MaxWorkers;
						m_Budgets[prefab] = componentData;
					}
				}
				return;
			}
			for (int j = 0; j < chunk.Count; j++)
			{
				Entity prefab2 = nativeArray[j].m_Prefab;
				if (m_Budgets.TryGetComponent(prefab2, out var componentData2))
				{
					componentData2.m_Count++;
					m_Budgets[prefab2] = componentData2;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct NetServiceBudgetJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjects;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> m_PlaceableNetCompositionData;

		public ComponentLookup<CollectedCityServiceBudgetData> m_BudgetDatas;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Composition> nativeArray2 = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				Composition composition = nativeArray2[i];
				Curve curve = nativeArray3[i];
				if (m_PlaceableNetCompositionData.HasComponent(composition.m_Edge) && m_ServiceObjects.TryGetComponent(prefab, out var componentData) && m_PlaceableNetCompositionData.TryGetComponent(composition.m_Edge, out var componentData2))
				{
					AddUpkeepCost(componentData.m_Service, NetUtils.GetUpkeepCost(curve, componentData2));
				}
			}
		}

		private void AddUpkeepCost(Entity service, int upkeep)
		{
			if (m_BudgetDatas.HasComponent(service))
			{
				CollectedCityServiceBudgetData value = m_BudgetDatas[service];
				value.m_BaseCost += upkeep;
				m_BudgetDatas[service] = value;
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
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceBudgetData> __Game_Simulation_ServiceBudgetData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Loan> __Game_Simulation_Loan_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CollectedCityServiceBudgetData> __Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CollectedCityServiceUpkeepData> __Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Creditworthiness> __Game_Simulation_Creditworthiness_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.City.City> __Game_City_City_RO_ComponentLookup;

		public ComponentTypeHandle<CollectedCityServiceBudgetData> __Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentTypeHandle;

		public BufferTypeHandle<CollectedCityServiceUpkeepData> __Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUsage> __Game_Buildings_ServiceUsage_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		public ComponentLookup<CollectedCityServiceBudgetData> __Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup;

		public BufferLookup<CollectedCityServiceUpkeepData> __Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup;

		public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		public ComponentLookup<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> __Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
			__Game_Simulation_ServiceBudgetData_RO_BufferLookup = state.GetBufferLookup<ServiceBudgetData>(isReadOnly: true);
			__Game_Simulation_Loan_RO_ComponentLookup = state.GetComponentLookup<Loan>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentLookup = state.GetComponentLookup<CollectedCityServiceBudgetData>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<CollectedCityServiceUpkeepData>(isReadOnly: true);
			__Game_Simulation_Creditworthiness_RO_ComponentLookup = state.GetComponentLookup<Creditworthiness>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_City_City_RO_ComponentLookup = state.GetComponentLookup<Game.City.City>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedCityServiceBudgetData>();
			__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferTypeHandle = state.GetBufferTypeHandle<CollectedCityServiceUpkeepData>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
			__Game_Buildings_ServiceUsage_RO_ComponentLookup = state.GetComponentLookup<ServiceUsage>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup = state.GetComponentLookup<CollectedCityServiceBudgetData>();
			__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup = state.GetBufferLookup<CollectedCityServiceUpkeepData>();
			__Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>();
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentLookup = state.GetComponentLookup<CollectedServiceBuildingBudgetData>();
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetComposition>(isReadOnly: true);
		}
	}

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ResourceSystem m_ResourceSystem;

	private ServiceFeeSystem m_ServiceFeeSystem;

	private TaxSystem m_TaxSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private GameModeGovernmentSubsidiesSystem m_GameModeGovernmentSubsidiesSystem;

	private EntityQuery m_BudgetDataQuery;

	private EntityQuery m_ServiceBuildingQuery;

	private EntityQuery m_ServiceQuery;

	private EntityQuery m_UpkeepGroup;

	private EntityQuery m_ServiceObjectQuery;

	private EntityQuery m_NetUpkeepQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_OutsideTradeParameterQuery;

	private EntityQuery m_HealthcareFacilityQuery;

	private EntityQuery m_DeathcareFacilityQuery;

	private EntityQuery m_GarbageFacilityQuery;

	private EntityQuery m_FireStationQuery;

	private EntityQuery m_PoliceStationQuery;

	protected NativeArray<int> m_Income;

	protected NativeArray<int> m_IncomeTemp;

	protected NativeArray<int> m_TotalIncome;

	protected NativeArray<int> m_Expenses;

	protected NativeArray<int> m_ExpensesTemp;

	private int m_TotalTaxIncome;

	private NativeReference<int> m_TotalTaxes;

	private NativeParallelHashMap<Entity, CollectedCityServiceBudgetData> m_CityServiceBudgets;

	private NativeParallelHashMap<Entity, int2> m_CityServiceUpkeepIndices;

	private NativeList<CollectedCityServiceUpkeepData> m_CityServiceUpkeeps;

	private JobHandle m_TempArrayDeps;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_844909884_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_GameModeGovernmentSubsidiesSystem = base.World.GetOrCreateSystemManaged<GameModeGovernmentSubsidiesSystem>();
		m_TotalIncome = new NativeArray<int>(14, Allocator.Persistent);
		m_Income = new NativeArray<int>(14, Allocator.Persistent);
		m_IncomeTemp = new NativeArray<int>(14, Allocator.Persistent);
		m_Expenses = new NativeArray<int>(15, Allocator.Persistent);
		m_ExpensesTemp = new NativeArray<int>(15, Allocator.Persistent);
		m_CityServiceUpkeepIndices = new NativeParallelHashMap<Entity, int2>(4, Allocator.Persistent);
		m_CityServiceUpkeeps = new NativeList<CollectedCityServiceUpkeepData>(4, Allocator.Persistent);
		m_CityServiceBudgets = new NativeParallelHashMap<Entity, CollectedCityServiceBudgetData>(4, Allocator.Persistent);
		m_TotalTaxes = new NativeReference<int>(Allocator.Persistent);
		m_ServiceQuery = GetEntityQuery(ComponentType.ReadWrite<CollectedCityServiceBudgetData>(), ComponentType.ReadWrite<CollectedCityServiceUpkeepData>());
		m_ServiceObjectQuery = GetEntityQuery(ComponentType.ReadWrite<CollectedServiceBuildingBudgetData>(), ComponentType.ReadOnly<ServiceObjectData>());
		m_UpkeepGroup = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_BudgetDataQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>());
		m_ServiceBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_NetUpkeepQuery = GetEntityQuery(ComponentType.ReadOnly<Composition>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_HealthcareFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Hospital>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_DeathcareFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_GarbageFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_FireStationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.FireStation>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PoliceStationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.PoliceStation>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_OutsideTradeParameterQuery = GetEntityQuery(ComponentType.ReadOnly<OutsideTradeParameterData>());
		RequireForUpdate(m_BudgetDataQuery);
		RequireForUpdate(m_ServiceQuery);
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (m_BudgetDataQuery.CalculateEntityCount() == 0)
		{
			base.EntityManager.CreateEntity(ComponentType.ReadWrite<ServiceBudgetData>());
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Income.Dispose();
		m_IncomeTemp.Dispose();
		m_TotalIncome.Dispose();
		m_Expenses.Dispose();
		m_ExpensesTemp.Dispose();
		m_CityServiceUpkeeps.Dispose();
		m_CityServiceBudgets.Dispose();
		m_CityServiceUpkeepIndices.Dispose();
		m_TotalTaxes.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TotalTaxIncome = m_TotalTaxes.Value;
		m_TempArrayDeps.Complete();
		m_IncomeTemp.CopyTo(m_Income);
		m_ExpensesTemp.CopyTo(m_Expenses);
		JobHandle outJobHandle;
		NativeList<Entity> cityServiceEntities = m_ServiceQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
		UpdateDataJob jobData = new UpdateDataJob
		{
			m_CityServiceEntities = cityServiceEntities,
			m_City = m_CitySystem.City,
			m_Lookup = m_CityStatisticsSystem.GetLookup(),
			m_Stats = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef),
			m_CollectedFees = m_ServiceFeeSystem.GetServiceFees(),
			m_BudgetDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceBudgetData_RO_BufferLookup, ref base.CheckedStateRef),
			m_Loans = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Loan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BudgetEntity = m_BudgetDataQuery.GetSingletonEntity(),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityServiceBudgets = m_CityServiceBudgets,
			m_CityServiceUpkeepIndices = m_CityServiceUpkeepIndices,
			m_CityServiceUpkeeps = m_CityServiceUpkeeps,
			m_CollectedBudgets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CollectedUpkeeps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_Creditworthiness = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Creditworthiness_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Expenses = m_ExpensesTemp,
			m_ServiceFees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_Income = m_IncomeTemp,
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_City_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideTradeParameterData = m_OutsideTradeParameterQuery.GetSingleton<OutsideTradeParameterData>(),
			m_EconomyParametersData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_MonthlySubsidy = m_GameModeGovernmentSubsidiesSystem.monthlySubsidy,
			m_TotalTaxes = m_TotalTaxes,
			m_ServiceFacilityBuildingCount = new int4(m_HealthcareFacilityQuery.CalculateEntityCount(), m_DeathcareFacilityQuery.CalculateEntityCount(), m_GarbageFacilityQuery.CalculateEntityCount(), m_FireStationQuery.CalculateEntityCount()),
			m_PoliceStationBuildingCount = m_PoliceStationQuery.CalculateEntityCount(),
			m_MapTileUpkeepCost = ((!m_CityConfigurationSystem.unlockMapTiles) ? m_MapTilePurchaseSystem.CalculateOwnedTilesUpkeep() : 0)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, m_TempArrayDeps, base.Dependency));
		m_TaxSystem.AddReader(base.Dependency);
		m_TempArrayDeps = base.Dependency;
		cityServiceEntities.Dispose(base.Dependency);
		ClearServiceDataJob jobData2 = new ClearServiceDataJob
		{
			m_BudgetDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpkeepDataType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_ServiceQuery, base.Dependency);
		CityServiceBudgetJob jobData3 = new CityServiceBudgetJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceUsages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUsage_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgradeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_EmployeeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_BudgetDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceUpkeepData_RW_BufferLookup, ref base.CheckedStateRef),
			m_ServiceBudgets = m_BudgetDataQuery.GetSingletonBuffer<ServiceBudgetData>(isReadOnly: true),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_CityModifiers = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData3, m_UpkeepGroup, base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new ClearBuildingDataJob
		{
			m_BudgetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_ServiceObjectQuery, base.Dependency);
		JobHandle job = JobChunkExtensions.Schedule(new CollectServiceBuildingBudgetDatasJob
		{
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Budgets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RW_ComponentLookup, ref base.CheckedStateRef)
		}, m_ServiceBuildingQuery, dependsOn);
		JobHandle job2 = JobChunkExtensions.Schedule(new NetServiceBudgetJob
		{
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceObjects = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BudgetDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_CollectedCityServiceBudgetData_RW_ComponentLookup, ref base.CheckedStateRef)
		}, m_NetUpkeepQuery, base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(job, job2);
	}

	public NativeArray<int> GetIncomeArray(out JobHandle deps)
	{
		deps = m_TempArrayDeps;
		return m_IncomeTemp;
	}

	public NativeArray<int> GetExpenseArray(out JobHandle deps)
	{
		deps = m_TempArrayDeps;
		return m_ExpensesTemp;
	}

	public void AddArrayReader(JobHandle deps)
	{
		m_TempArrayDeps = JobHandle.CombineDependencies(m_TempArrayDeps, deps);
	}

	public int GetBalance()
	{
		return GetBalance(m_Income, m_Expenses);
	}

	public static int GetBalance(NativeArray<int> income, NativeArray<int> expenses)
	{
		return GetTotalIncome(income) + GetTotalExpenses(expenses);
	}

	public int GetTotalIncome()
	{
		return GetTotalIncome(m_Income);
	}

	public static int GetTotalIncome(NativeArray<int> income)
	{
		int num = 0;
		for (int i = 0; i < income.Length; i++)
		{
			num += income[i];
		}
		return num;
	}

	public int GetTotalExpenses()
	{
		return GetTotalExpenses(m_Expenses);
	}

	public static int GetTotalExpenses(NativeArray<int> expenses)
	{
		int num = 0;
		for (int i = 0; i < expenses.Length; i++)
		{
			num -= expenses[i];
		}
		return num;
	}

	public int GetTotalTaxIncome()
	{
		return m_TotalTaxIncome;
	}

	public int GetIncome(IncomeSource source)
	{
		return GetIncome(source, m_Income);
	}

	public static int GetIncome(IncomeSource source, NativeArray<int> income)
	{
		if ((int)source < income.Length)
		{
			return income[(int)source];
		}
		return 0;
	}

	public int GetTotalIncome(IncomeSource source)
	{
		if ((int)source < m_TotalIncome.Length)
		{
			return m_TotalIncome[(int)source];
		}
		return 0;
	}

	public int GetExpense(ExpenseSource source)
	{
		return GetExpense(source, m_Expenses);
	}

	public static int GetExpense(ExpenseSource source, NativeArray<int> expenses)
	{
		if ((int)source < expenses.Length)
		{
			return expenses[(int)source];
		}
		return 0;
	}

	public int GetMoneyDelta()
	{
		int num = 0;
		for (int i = 0; i < 15; i++)
		{
			num -= GetExpense((ExpenseSource)i);
		}
		for (int j = 0; j < 14; j++)
		{
			num += GetIncome((IncomeSource)j);
		}
		return num / 24;
	}

	public int GetServiceBudget(Entity servicePrefab)
	{
		m_TempArrayDeps.Complete();
		return GetServiceBudget(servicePrefab, m_CityServiceBudgets, m_BudgetDataQuery.GetSingletonEntity(), InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceBudgetData_RO_BufferLookup, ref base.CheckedStateRef));
	}

	public static int GetServiceBudget(Entity servicePrefab, NativeParallelHashMap<Entity, CollectedCityServiceBudgetData> budgets, Entity budgetEntity, BufferLookup<ServiceBudgetData> budgetDatas)
	{
		if (!budgets.ContainsKey(servicePrefab))
		{
			return 0;
		}
		DynamicBuffer<ServiceBudgetData> dynamicBuffer = budgetDatas[budgetEntity];
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			ServiceBudgetData serviceBudgetData = dynamicBuffer[i];
			if (serviceBudgetData.m_Service == servicePrefab)
			{
				return serviceBudgetData.m_Budget;
			}
		}
		return 100;
	}

	public void SetServiceBudget(Entity servicePrefab, int percentage)
	{
		m_TempArrayDeps.Complete();
		if (!m_CityServiceBudgets.ContainsKey(servicePrefab))
		{
			return;
		}
		Entity singletonEntity = m_BudgetDataQuery.GetSingletonEntity();
		DynamicBuffer<ServiceBudgetData> buffer = base.EntityManager.GetBuffer<ServiceBudgetData>(singletonEntity);
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < buffer.Length; i++)
		{
			ServiceBudgetData value = buffer[i];
			if (value.m_Service == servicePrefab)
			{
				flag = value.m_Budget != percentage;
				value.m_Budget = percentage;
				buffer[i] = value;
				flag2 = true;
				break;
			}
		}
		if (!flag2)
		{
			flag = true;
			buffer.Add(new ServiceBudgetData
			{
				m_Service = servicePrefab,
				m_Budget = percentage
			});
		}
		if (flag)
		{
			m_EndFrameBarrier.CreateCommandBuffer().AddComponent<Updated>(singletonEntity);
		}
	}

	public int GetServiceEfficiency(Entity servicePrefab, int budget)
	{
		return Mathf.RoundToInt(100f * __query_844909884_0.GetSingleton<BuildingEfficiencyParameterData>().m_ServiceBudgetEfficiencyFactor.Evaluate((float)budget / 100f));
	}

	private static int GetEstimatedServiceUpkeep(CollectedCityServiceBudgetData data, int2 indices, int budget, NativeList<CollectedCityServiceUpkeepData> upkeeps)
	{
		int num = data.m_BaseCost;
		for (int i = indices.x; i < indices.x + indices.y; i++)
		{
			CollectedCityServiceUpkeepData collectedCityServiceUpkeepData = upkeeps[i];
			int num2 = Mathf.RoundToInt(collectedCityServiceUpkeepData.m_FullCost);
			if (collectedCityServiceUpkeepData.m_Resource == Resource.Money)
			{
				num2 = Mathf.RoundToInt((float)num2 * ((float)budget / 100f));
			}
			num += num2;
		}
		return num;
	}

	public void GetEstimatedServiceBudget(Entity servicePrefab, out int upkeep)
	{
		m_TempArrayDeps.Complete();
		GetEstimatedServiceBudget(servicePrefab, out upkeep, m_CityServiceBudgets, m_CityServiceUpkeeps, m_CityServiceUpkeepIndices, m_BudgetDataQuery.GetSingletonEntity(), GetBufferLookup<ServiceBudgetData>(isReadOnly: true));
	}

	public static void GetEstimatedServiceBudget(Entity servicePrefab, out int upkeep, NativeParallelHashMap<Entity, CollectedCityServiceBudgetData> budgets, NativeList<CollectedCityServiceUpkeepData> upkeeps, NativeParallelHashMap<Entity, int2> upkeepIndices, Entity budgetEntity, BufferLookup<ServiceBudgetData> budgetDatas)
	{
		if (!budgets.ContainsKey(servicePrefab))
		{
			upkeep = 0;
			return;
		}
		int serviceBudget = GetServiceBudget(servicePrefab, budgets, budgetEntity, budgetDatas);
		CollectedCityServiceBudgetData data = budgets[servicePrefab];
		int2 indices = upkeepIndices[servicePrefab];
		upkeep = GetEstimatedServiceUpkeep(data, indices, serviceBudget, upkeeps);
	}

	public int GetNumberOfServiceBuildings(Entity serviceBuildingPrefab)
	{
		if (base.EntityManager.HasComponent<CollectedServiceBuildingBudgetData>(serviceBuildingPrefab))
		{
			return base.EntityManager.GetComponentData<CollectedServiceBuildingBudgetData>(serviceBuildingPrefab).m_Count;
		}
		return 0;
	}

	public int2 GetWorkersAndWorkplaces(Entity serviceBuildingPrefab)
	{
		if (base.EntityManager.HasComponent<CollectedServiceBuildingBudgetData>(serviceBuildingPrefab))
		{
			CollectedServiceBuildingBudgetData componentData = base.EntityManager.GetComponentData<CollectedServiceBuildingBudgetData>(serviceBuildingPrefab);
			return new int2(componentData.m_Workers, componentData.m_Workplaces);
		}
		return new int2(0, 0);
	}

	public Entity[] GetServiceBuildings(Entity servicePrefab)
	{
		NativeArray<Entity> nativeArray = m_ServiceObjectQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<ServiceObjectData> nativeArray2 = m_ServiceObjectQuery.ToComponentDataArray<ServiceObjectData>(Allocator.TempJob);
		List<Entity> list = new List<Entity>(4);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray2[i].m_Service == servicePrefab)
			{
				list.Add(nativeArray[i]);
			}
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
		return list.ToArray();
	}

	private static int GetTotalTaxIncome(NativeArray<int> income)
	{
		return GetIncome(IncomeSource.TaxCommercial, income) + GetIncome(IncomeSource.TaxIndustrial, income) + GetIncome(IncomeSource.TaxResidential, income) + GetIncome(IncomeSource.TaxOffice, income);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_844909884_0 = entityQueryBuilder2.Build(ref state);
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
	public CityServiceBudgetSystem()
	{
	}
}
