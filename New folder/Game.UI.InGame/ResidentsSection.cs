using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
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
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ResidentsSection : InfoSectionBase
{
	public struct HouseholdWealthData : IJsonWritable
	{
		public int income;

		public int rent;

		public int upkeep;

		public uint resourceCost;

		public int fees;

		public static HouseholdWealthData operator +(HouseholdWealthData left, HouseholdWealthData right)
		{
			return new HouseholdWealthData
			{
				income = left.income + right.income,
				rent = left.rent + right.rent,
				upkeep = left.upkeep + right.upkeep,
				resourceCost = left.resourceCost + right.resourceCost,
				fees = left.fees + right.fees
			};
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(HouseholdWealthData).FullName);
			writer.PropertyName("income");
			writer.Write(income);
			writer.PropertyName("rent");
			writer.Write(rent);
			writer.PropertyName("upkeep");
			writer.Write(upkeep);
			writer.PropertyName("resourceCost");
			writer.Write(resourceCost);
			writer.PropertyName("fees");
			writer.Write(fees);
			writer.TypeEnd();
		}
	}

	public enum Result
	{
		Visible,
		ResidentCount,
		PetCount,
		HouseholdCount,
		MaxHouseholds,
		ResultCount
	}

	[BurstCompile]
	public struct CountHouseholdsJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public Entity m_SelectedPrefab;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkLookup;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedLookup;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholdLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterLookup;

		public NativeArray<int> m_Results;

		public NativeList<Entity> m_HouseholdsResult;

		public NativeValue<Entity> m_ResidenceResult;

		public void Execute()
		{
			int residentCount = 0;
			int petCount = 0;
			int householdCount = 0;
			int maxHouseholds = 0;
			if (m_BuildingLookup.HasComponent(m_SelectedEntity) && TryCountHouseholds(ref residentCount, ref petCount, ref householdCount, ref maxHouseholds, m_SelectedEntity, m_SelectedPrefab, ref m_ParkLookup, ref m_AbandonedLookup, ref m_PropertyDataLookup, ref m_HealthProblemLookup, ref m_TravelPurposeLookup, ref m_HouseholdLookup, ref m_RenterLookup, ref m_HouseholdCitizenLookup, ref m_HouseholdAnimalLookup, m_HouseholdsResult))
			{
				m_Results[0] = 1;
				m_Results[1] = residentCount;
				m_Results[2] = petCount;
				m_Results[3] = householdCount;
				m_Results[4] = maxHouseholds;
			}
			else
			{
				if (!m_HouseholdLookup.HasComponent(m_SelectedEntity) || !m_HouseholdCitizenLookup.TryGetBuffer(m_SelectedEntity, out var bufferData))
				{
					return;
				}
				HomelessHousehold componentData2;
				if (m_PropertyRenterLookup.TryGetComponent(m_SelectedEntity, out var componentData))
				{
					m_HouseholdsResult.Add(in m_SelectedEntity);
					m_ResidenceResult.value = componentData.m_Property;
				}
				else if (m_HomelessHouseholdLookup.TryGetComponent(m_SelectedEntity, out componentData2))
				{
					m_HouseholdsResult.Add(in m_SelectedEntity);
					m_ResidenceResult.value = componentData2.m_TempHome;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (!CitizenUtils.IsCorpsePickedByHearse(bufferData[i].m_Citizen, ref m_HealthProblemLookup, ref m_TravelPurposeLookup))
					{
						residentCount++;
					}
				}
				if (m_HouseholdAnimalLookup.TryGetBuffer(m_SelectedEntity, out var bufferData2))
				{
					petCount += bufferData2.Length;
				}
				m_Results[0] = 1;
				m_Results[1] = residentCount;
				m_Results[2] = petCount;
				m_Results[3] = 1;
				m_Results[4] = 1;
			}
		}
	}

	[BurstCompile]
	public struct CountDistrictHouseholdsJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterLookup;

		public NativeArray<int> m_Results;

		public NativeList<Entity> m_HouseholdsResult;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<CurrentDistrict> nativeArray2 = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			int num = 0;
			int residentCount = 0;
			int petCount = 0;
			int householdCount = 0;
			int maxHouseholds = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				CurrentDistrict currentDistrict = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				if (!(currentDistrict.m_District != m_SelectedEntity) && TryCountHouseholds(ref residentCount, ref petCount, ref householdCount, ref maxHouseholds, entity, prefabRef.m_Prefab, ref m_ParkLookup, ref m_AbandonedLookup, ref m_PropertyDataLookup, ref m_HealthProblemLookup, ref m_TravelPurposeLookup, ref m_HouseholdLookup, ref m_RenterLookup, ref m_HouseholdCitizenLookup, ref m_HouseholdAnimalLookup, m_HouseholdsResult))
				{
					num = 1;
				}
			}
			m_Results[0] += num;
			m_Results[1] += residentCount;
			m_Results[2] += petCount;
			m_Results[3] += householdCount;
			m_Results[4] += maxHouseholds;
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
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		}
	}

	private EntityQuery m_DistrictBuildingQuery;

	private EntityQuery m_HappinessParameterQuery;

	private NativeArray<int> m_Results;

	private NativeValue<Entity> m_ResidenceResult;

	private NativeList<Entity> m_HouseholdsResult;

	private CitySystem m_CitySystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_693086154_0;

	protected override string group => "ResidentsSection";

	private bool isHousehold { get; set; }

	private bool isDistrict { get; set; }

	private int householdCount { get; set; }

	private int maxHouseholds { get; set; }

	private HouseholdWealthKey wealthKey { get; set; }

	private int residentCount { get; set; }

	private int petCount { get; set; }

	private Entity residenceEntity { get; set; }

	private CitizenResidenceKey residenceKey { get; set; }

	private EducationData educationData { get; set; }

	private AgeData ageData { get; set; }

	private HouseholdWealthData wealthData { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_DistrictBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<CurrentDistrict>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_HouseholdsResult = new NativeList<Entity>(Allocator.Persistent);
		m_ResidenceResult = new NativeValue<Entity>(Allocator.Persistent);
		m_Results = new NativeArray<int>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_HouseholdsResult.Dispose();
		m_ResidenceResult.Dispose();
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		isHousehold = false;
		isDistrict = false;
		householdCount = 0;
		maxHouseholds = 0;
		residentCount = 0;
		petCount = 0;
		wealthData = default(HouseholdWealthData);
		educationData = default(EducationData);
		ageData = default(AgeData);
		m_HouseholdsResult.Clear();
		m_ResidenceResult.value = Entity.Null;
		m_Results[0] = 0;
		m_Results[1] = 0;
		m_Results[2] = 0;
		m_Results[4] = 0;
		m_Results[3] = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity))
		{
			JobChunkExtensions.Schedule(new CountDistrictHouseholdsJob
			{
				m_SelectedEntity = selectedEntity,
				m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CurrentDistrictHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AbandonedLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TravelPurposeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizenLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdAnimalLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
				m_RenterLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = m_Results,
				m_HouseholdsResult = m_HouseholdsResult
			}, m_DistrictBuildingQuery, base.Dependency).Complete();
			base.visible = m_Results[0] > 0;
		}
		else
		{
			IJobExtensions.Schedule(new CountHouseholdsJob
			{
				m_SelectedEntity = selectedEntity,
				m_SelectedPrefab = selectedPrefab,
				m_BuildingLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AbandonedLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HomelessHouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TravelPurposeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenterLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizenLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdAnimalLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
				m_RenterLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = m_Results,
				m_HouseholdsResult = m_HouseholdsResult,
				m_ResidenceResult = m_ResidenceResult
			}, base.Dependency).Complete();
			base.visible = m_Results[0] > 0;
		}
	}

	protected override void OnProcess()
	{
		isHousehold = base.EntityManager.HasComponent<Household>(selectedEntity);
		isDistrict = base.EntityManager.HasComponent<District>(selectedEntity);
		householdCount = m_Results[3];
		residentCount = m_Results[1];
		petCount = m_Results[2];
		maxHouseholds = m_Results[4];
		wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(base.EntityManager, m_HouseholdsResult, m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>());
		DynamicBuffer<HouseholdCitizen> buffer2;
		if (!isHousehold)
		{
			for (int i = 0; i < m_HouseholdsResult.Length; i++)
			{
				DynamicBuffer<HouseholdCitizen> buffer = base.EntityManager.GetBuffer<HouseholdCitizen>(m_HouseholdsResult[i], isReadOnly: true);
				wealthData += GetHouseholdEconomyData(m_HouseholdsResult[i]);
				ageData += GetAgeData(buffer);
				educationData += GetEducationData(buffer);
			}
		}
		else if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out buffer2))
		{
			wealthData += GetHouseholdEconomyData(selectedEntity);
			ageData += GetAgeData(buffer2);
			educationData += GetEducationData(buffer2);
		}
		if (!base.EntityManager.Exists(m_ResidenceResult.value))
		{
			residenceEntity = Entity.Null;
			residenceKey = CitizenResidenceKey.Home;
			return;
		}
		residenceEntity = m_ResidenceResult.value;
		residenceKey = (base.EntityManager.HasComponent<TouristHousehold>(selectedEntity) ? CitizenResidenceKey.Hotel : (base.EntityManager.HasComponent<HomelessHousehold>(selectedEntity) ? CitizenResidenceKey.Shelter : CitizenResidenceKey.Home));
		DynamicBuffer<Renter> buffer4;
		if ((base.EntityManager.HasComponent<Game.Buildings.Park>(residenceEntity) || base.EntityManager.HasComponent<Abandoned>(residenceEntity)) && base.EntityManager.TryGetBuffer(residenceEntity, isReadOnly: true, out DynamicBuffer<Renter> buffer3) && buffer3.Length > 0)
		{
			m_InfoUISystem.tags.Add(SelectedInfoTags.HomelessShelter);
			base.tooltipTags.Add("HomelessShelter");
			base.tooltipKeys.Add("HomelessShelter");
		}
		else if ((base.EntityManager.HasComponent<Game.Buildings.Park>(selectedEntity) || base.EntityManager.HasComponent<Abandoned>(selectedEntity)) && base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out buffer4) && buffer4.Length > 0)
		{
			m_InfoUISystem.tags.Add(SelectedInfoTags.HomelessShelter);
			base.tooltipTags.Add("HomelessShelter");
			base.tooltipKeys.Add("HomelessShelter");
		}
	}

	private HouseholdWealthData GetHouseholdEconomyData(Entity entity)
	{
		int rent = 0;
		int num = 0;
		if (base.EntityManager.TryGetComponent<Household>(entity, out var component) && base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component2))
		{
			int rent2 = component2.m_Rent;
			int num2 = 1;
			if (base.EntityManager.TryGetBuffer(component2.m_Property, isReadOnly: true, out DynamicBuffer<Renter> buffer))
			{
				num2 = math.max(buffer.Length, 1);
			}
			rent = rent2;
			if (base.EntityManager.TryGetBuffer(m_CitySystem.City, isReadOnly: true, out DynamicBuffer<ServiceFee> buffer2))
			{
				if (base.EntityManager.TryGetComponent<ElectricityConsumer>(component2.m_Property, out var component3))
				{
					num += (int)((float)component3.m_FulfilledConsumption * ServiceFeeSystem.GetFee(PlayerResource.Electricity, buffer2));
				}
				if (base.EntityManager.TryGetComponent<WaterConsumer>(component2.m_Property, out var component4))
				{
					num += (int)((float)component4.m_FulfilledFresh * ServiceFeeSystem.GetFee(PlayerResource.Water, buffer2));
					num += (int)((float)component4.m_FulfilledSewage * ServiceFeeSystem.GetFee(PlayerResource.Water, buffer2));
				}
			}
			num += __query_693086154_0.GetSingleton<ServiceFeeParameterData>().m_GarbageFeeRCIO.x;
			num /= num2;
		}
		return new HouseholdWealthData
		{
			income = component.m_SalaryLastDay,
			rent = rent,
			upkeep = math.abs(component.m_MoneySpendOnBuildingLevelingLastDay),
			resourceCost = component.m_ShoppedValuePerDay,
			fees = num
		};
	}

	private AgeData GetAgeData(DynamicBuffer<HouseholdCitizen> citizens)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < citizens.Length; i++)
		{
			Entity citizen = citizens[i].m_Citizen;
			if (base.EntityManager.TryGetComponent<Citizen>(citizen, out var component) && !CitizenUtils.IsCorpsePickedByHearse(base.EntityManager, citizen))
			{
				switch (component.GetAge())
				{
				case CitizenAge.Child:
					num++;
					break;
				case CitizenAge.Teen:
					num2++;
					break;
				case CitizenAge.Adult:
					num3++;
					break;
				case CitizenAge.Elderly:
					num4++;
					break;
				}
			}
		}
		return new AgeData(num, num2, num3, num4);
	}

	private EducationData GetEducationData(DynamicBuffer<HouseholdCitizen> citizens)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < citizens.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<Citizen>(citizens[i].m_Citizen, out var component) && !CitizenUtils.IsCorpsePickedByHearse(base.EntityManager, citizens[i].m_Citizen))
			{
				switch (component.GetEducationLevel())
				{
				case 0:
					num++;
					break;
				case 1:
					num2++;
					break;
				case 2:
					num3++;
					break;
				case 3:
					num4++;
					break;
				case 4:
					num5++;
					break;
				}
			}
		}
		return new EducationData(num, num2, num3, num4, num5);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("isHousehold");
		writer.Write(isHousehold);
		writer.PropertyName("isDistrict");
		writer.Write(isDistrict);
		writer.PropertyName("householdCount");
		writer.Write(householdCount);
		writer.PropertyName("maxHouseholds");
		writer.Write(maxHouseholds);
		writer.PropertyName("residentCount");
		writer.Write(residentCount);
		writer.PropertyName("petCount");
		writer.Write(petCount);
		writer.PropertyName("wealthKey");
		writer.Write(wealthKey.ToString());
		writer.PropertyName("wealthData");
		writer.Write(wealthData);
		writer.PropertyName("residence");
		if (residenceEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, residenceEntity);
		}
		writer.PropertyName("residenceEntity");
		if (residenceEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(residenceEntity);
		}
		writer.PropertyName("residenceKey");
		writer.Write(Enum.GetName(typeof(CitizenResidenceKey), residenceKey));
		writer.PropertyName("ageData");
		writer.Write(ageData);
		writer.PropertyName("educationData");
		writer.Write(educationData);
	}

	private static bool TryCountHouseholds(ref int residentCount, ref int petCount, ref int householdCount, ref int maxHouseholds, Entity entity, Entity prefab, ref ComponentLookup<Game.Buildings.Park> parkLookup, ref ComponentLookup<Abandoned> abandonedLookup, ref ComponentLookup<BuildingPropertyData> propertyDataLookup, ref ComponentLookup<HealthProblem> healthProblemLookup, ref ComponentLookup<TravelPurpose> travelPurposeLookup, ref ComponentLookup<Household> householdLookup, ref BufferLookup<Renter> renterLookup, ref BufferLookup<HouseholdCitizen> householdCitizenLookup, ref BufferLookup<HouseholdAnimal> householdAnimalLookup, NativeList<Entity> householdsResult)
	{
		bool result = false;
		bool flag = abandonedLookup.HasComponent(entity);
		DynamicBuffer<Renter> bufferData;
		bool flag2 = renterLookup.TryGetBuffer(entity, out bufferData) && bufferData.Length > 0;
		bool flag3 = parkLookup.HasComponent(entity);
		BuildingPropertyData componentData;
		bool num = propertyDataLookup.TryGetComponent(prefab, out componentData) && componentData.m_ResidentialProperties > 0 && !flag;
		bool flag4 = (flag3 || flag) && flag2;
		if (num || flag4)
		{
			result = true;
			maxHouseholds += componentData.m_ResidentialProperties;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity value = bufferData[i].m_Renter;
				if (!householdLookup.HasComponent(value) || !householdCitizenLookup.TryGetBuffer(value, out var bufferData2))
				{
					continue;
				}
				householdCount++;
				householdsResult.Add(in value);
				for (int j = 0; j < bufferData2.Length; j++)
				{
					if (!CitizenUtils.IsCorpsePickedByHearse(bufferData2[j].m_Citizen, ref healthProblemLookup, ref travelPurposeLookup))
					{
						residentCount++;
					}
				}
				if (householdAnimalLookup.TryGetBuffer(value, out var bufferData3))
				{
					petCount += bufferData3.Length;
				}
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_693086154_0 = entityQueryBuilder2.Build(ref state);
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
	public ResidentsSection()
	{
	}
}
