using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class HouseholdSidebarSection : InfoSectionBase
{
	public enum Result
	{
		Visible,
		ResidentCount,
		Type,
		ResultCount
	}

	private enum HouseholdSidebarVariant
	{
		Citizen,
		Household,
		Building
	}

	[BurstCompile]
	public struct CheckVisibilityJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public Entity m_SelectedPrefab;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkFromLookup;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> m_HouseholdPetLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterLookup;

		public NativeArray<int> m_Results;

		public void Execute()
		{
			int residentCount = 0;
			int householdCount = 0;
			if (m_CitizenLookup.HasComponent(m_SelectedEntity) || (m_HouseholdPetLookup.TryGetComponent(m_SelectedEntity, out var componentData) && componentData.m_Household != Entity.Null))
			{
				m_Results[0] = 1;
				m_Results[1] = 1;
				m_Results[2] = 0;
			}
			else if (m_BuildingLookup.HasComponent(m_SelectedEntity) && HasResidentialProperties(ref residentCount, ref householdCount, m_SelectedEntity, m_SelectedPrefab))
			{
				m_Results[0] = 1;
				m_Results[1] = residentCount;
				m_Results[2] = 2;
			}
			else
			{
				if (!m_HouseholdLookup.HasComponent(m_SelectedEntity) || !m_HouseholdCitizenLookup.TryGetBuffer(m_SelectedEntity, out var bufferData))
				{
					return;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (!CitizenUtils.IsCorpsePickedByHearse(bufferData[i].m_Citizen, ref m_HealthProblemLookup, ref m_TravelPurposeLookup))
					{
						residentCount++;
					}
				}
				m_Results[0] = 1;
				m_Results[1] = residentCount;
				m_Results[2] = 1;
			}
		}

		private bool HasResidentialProperties(ref int residentCount, ref int householdCount, Entity entity, Entity prefab)
		{
			bool result = false;
			bool flag = m_AbandonedLookup.HasComponent(entity);
			DynamicBuffer<Renter> bufferData;
			bool flag2 = m_RenterLookup.TryGetBuffer(entity, out bufferData) && bufferData.Length > 0;
			bool flag3 = m_ParkFromLookup.HasComponent(entity);
			BuildingPropertyData componentData;
			bool num = !flag && m_PropertyDataLookup.TryGetComponent(prefab, out componentData) && componentData.m_ResidentialProperties > 0;
			bool flag4 = (flag3 || flag) && flag2;
			if (num || flag4)
			{
				result = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity renter = bufferData[i].m_Renter;
					if (!m_HouseholdCitizenLookup.TryGetBuffer(renter, out var bufferData2))
					{
						continue;
					}
					householdCount++;
					for (int j = 0; j < bufferData2.Length; j++)
					{
						if (!CitizenUtils.IsCorpsePickedByHearse(bufferData2[j].m_Citizen, ref m_HealthProblemLookup, ref m_TravelPurposeLookup))
						{
							residentCount++;
						}
					}
				}
			}
			return result;
		}
	}

	[BurstCompile]
	private struct CollectDataJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingLookup;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholdLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> m_HouseholdPetLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;

		[ReadOnly]
		public BufferLookup<Resources> m_ResourcesLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimalsLookup;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterLookup;

		public NativeArray<Entity> m_ResidenceResult;

		public NativeArray<HouseholdResult> m_HouseholdResult;

		public NativeList<HouseholdResult> m_HouseholdsResult;

		public NativeList<ResidentResult> m_ResidentsResult;

		public NativeList<Entity> m_PetsResult;

		public void Execute()
		{
			if (m_HouseholdPetLookup.TryGetComponent(m_SelectedEntity, out var componentData))
			{
				Entity household = componentData.m_Household;
				m_HouseholdResult[0] = AddHousehold(household);
				HomelessHousehold componentData3;
				if (m_PropertyRenterLookup.TryGetComponent(household, out var componentData2))
				{
					m_ResidenceResult[0] = componentData2.m_Property;
				}
				else if (m_HomelessHouseholdLookup.TryGetComponent(household, out componentData3))
				{
					m_ResidenceResult[0] = componentData3.m_TempHome;
				}
			}
			else if (m_CitizenLookup.HasComponent(m_SelectedEntity))
			{
				Entity household2 = m_HouseholdMemberLookup[m_SelectedEntity].m_Household;
				m_HouseholdResult[0] = AddHousehold(household2);
				HomelessHousehold componentData5;
				if (m_PropertyRenterLookup.TryGetComponent(household2, out var componentData4))
				{
					m_ResidenceResult[0] = componentData4.m_Property;
				}
				else if (m_HomelessHouseholdLookup.TryGetComponent(household2, out componentData5))
				{
					m_ResidenceResult[0] = componentData5.m_TempHome;
				}
			}
			else if (m_BuildingLookup.HasComponent(m_SelectedEntity))
			{
				DynamicBuffer<Renter> dynamicBuffer = m_RenterLookup[m_SelectedEntity];
				m_ResidenceResult[0] = m_SelectedEntity;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity renter = dynamicBuffer[i].m_Renter;
					HouseholdResult value = AddHousehold(renter);
					if (i == 0)
					{
						m_HouseholdResult[0] = value;
					}
				}
			}
			else
			{
				m_HouseholdResult[0] = AddHousehold(m_SelectedEntity);
				HomelessHousehold componentData7;
				if (m_PropertyRenterLookup.TryGetComponent(m_SelectedEntity, out var componentData6))
				{
					m_ResidenceResult[0] = componentData6.m_Property;
				}
				else if (m_HomelessHouseholdLookup.TryGetComponent(m_SelectedEntity, out componentData7))
				{
					m_ResidenceResult[0] = componentData7.m_TempHome;
				}
			}
		}

		private HouseholdResult AddHousehold(Entity householdEntity)
		{
			HouseholdResult value = default(HouseholdResult);
			if (m_HouseholdLookup.TryGetComponent(householdEntity, out var componentData) && m_HouseholdCitizenLookup.TryGetBuffer(householdEntity, out var bufferData) && m_ResourcesLookup.TryGetBuffer(householdEntity, out var bufferData2))
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity citizen = bufferData[i].m_Citizen;
					if (m_CitizenLookup.TryGetComponent(citizen, out var componentData2) && !CitizenUtils.IsCorpsePickedByHearse(citizen, ref m_HealthProblemLookup, ref m_TravelPurposeLookup))
					{
						int happiness = componentData2.Happiness;
						CitizenAge age = componentData2.GetAge();
						int educationLevel = componentData2.GetEducationLevel();
						ref NativeList<ResidentResult> reference = ref m_ResidentsResult;
						ResidentResult value2 = new ResidentResult
						{
							m_Entity = citizen,
							m_Age = age,
							m_Happiness = happiness,
							m_Education = educationLevel
						};
						reference.Add(in value2);
						num++;
						num3 += happiness;
						num4 = (int)(num4 + age);
						if (age == CitizenAge.Adult || age == CitizenAge.Teen)
						{
							num2++;
							num5 += componentData2.GetEducationLevel();
						}
					}
				}
				if (m_HouseholdAnimalsLookup.TryGetBuffer(householdEntity, out var bufferData3))
				{
					for (int j = 0; j < bufferData3.Length; j++)
					{
						ref NativeList<Entity> reference2 = ref m_PetsResult;
						HouseholdAnimal householdAnimal = bufferData3[j];
						reference2.Add(in householdAnimal.m_HouseholdPet);
					}
				}
				value = new HouseholdResult
				{
					m_Entity = householdEntity,
					m_Members = num,
					m_Age = num4 / math.max(num, 1),
					m_Happiness = num3 / math.max(num, 1),
					m_Education = num5 / math.max(num2, 1),
					m_Wealth = EconomyUtils.GetHouseholdTotalWealth(componentData, bufferData2)
				};
				if (value.m_Members > 0)
				{
					m_HouseholdsResult.Add(in value);
				}
			}
			return value;
		}
	}

	public struct HouseholdResult : IComparable<HouseholdResult>, IEquatable<HouseholdResult>
	{
		public Entity m_Entity;

		public int m_Members;

		public int m_Age;

		public int m_Education;

		public int m_Happiness;

		public int m_Wealth;

		public int CompareTo(HouseholdResult other)
		{
			int num = other.m_Members.CompareTo(m_Members);
			if (num == 0)
			{
				num = other.m_Entity.CompareTo(m_Entity);
			}
			return num;
		}

		public bool Equals(HouseholdResult other)
		{
			if (m_Entity.Equals(other.m_Entity))
			{
				return m_Members == other.m_Members;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is HouseholdResult other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_Entity, m_Members);
		}
	}

	public class HouseholdComparer : IComparer<HouseholdResult>
	{
		public enum CompareBy
		{
			Members,
			Age,
			Education,
			Happiness,
			Wealth
		}

		private readonly Func<HouseholdResult, HouseholdResult, int> m_Compare;

		public HouseholdComparer(CompareBy compareBy)
		{
			m_Compare = compareBy switch
			{
				CompareBy.Members => (HouseholdResult x, HouseholdResult y) => y.m_Members.CompareTo(x.m_Members), 
				CompareBy.Age => (HouseholdResult x, HouseholdResult y) => y.m_Age.CompareTo(x.m_Age), 
				CompareBy.Education => (HouseholdResult x, HouseholdResult y) => y.m_Education.CompareTo(x.m_Education), 
				CompareBy.Wealth => (HouseholdResult x, HouseholdResult y) => y.m_Wealth.CompareTo(x.m_Wealth), 
				_ => (HouseholdResult x, HouseholdResult y) => 0, 
			};
		}

		public int Compare(HouseholdResult x, HouseholdResult y)
		{
			int num = m_Compare?.Invoke(x, y) ?? 0;
			if (num == 0)
			{
				num = y.m_Members.CompareTo(x.m_Members);
			}
			if (num == 0)
			{
				num = y.m_Age.CompareTo(x.m_Age);
			}
			if (num == 0)
			{
				num = y.m_Education.CompareTo(x.m_Education);
			}
			if (num == 0)
			{
				num = y.m_Wealth.CompareTo(x.m_Wealth);
			}
			if (num == 0)
			{
				num = y.m_Entity.CompareTo(x.m_Entity);
			}
			return num;
		}
	}

	public struct ResidentResult : IComparable<ResidentResult>, IEquatable<ResidentResult>
	{
		public Entity m_Entity;

		public CitizenAge m_Age;

		public int m_Education;

		public int m_Happiness;

		public int CompareTo(ResidentResult other)
		{
			int num = other.m_Age.CompareTo(m_Age);
			if (num == 0)
			{
				num = other.m_Education.CompareTo(m_Education);
			}
			if (num == 0)
			{
				num = other.m_Entity.CompareTo(m_Entity);
			}
			return num;
		}

		public bool Equals(ResidentResult other)
		{
			if (m_Entity.Equals(other.m_Entity))
			{
				return m_Age == other.m_Age;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ResidentResult other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_Entity, (int)m_Age);
		}
	}

	public class ResidentComparer : IComparer<ResidentResult>
	{
		public enum CompareBy
		{
			Age,
			Education,
			Happiness
		}

		private readonly Func<ResidentResult, ResidentResult, int> m_Compare;

		public ResidentComparer(CompareBy compareBy)
		{
			m_Compare = compareBy switch
			{
				CompareBy.Age => (ResidentResult x, ResidentResult y) => y.m_Age.CompareTo(x.m_Age), 
				CompareBy.Education => (ResidentResult x, ResidentResult y) => y.m_Education.CompareTo(x.m_Education), 
				_ => (ResidentResult x, ResidentResult y) => 0, 
			};
		}

		public int Compare(ResidentResult x, ResidentResult y)
		{
			int num = m_Compare?.Invoke(x, y) ?? 0;
			if (num == 0)
			{
				num = y.m_Age.CompareTo(x.m_Age);
			}
			if (num == 0)
			{
				num = y.m_Education.CompareTo(x.m_Education);
			}
			if (num == 0)
			{
				num = y.m_Entity.CompareTo(x.m_Entity);
			}
			return num;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> __Game_Citizens_HouseholdPet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdPet_RO_ComponentLookup = state.GetComponentLookup<HouseholdPet>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
		}
	}

	private const string kHouseholdIcon = "Media/Game/Icons/Household.svg";

	private const string kResidenceIcon = "Media/Glyphs/Residence.svg";

	private const string kHomelessShelterIcon = "Media/Glyphs/HomelessShelter.svg";

	private const string kPetIcon = "Media/Game/Icons/Pet.svg";

	private const string kItemType = "Game.UI.InGame.HouseholdSidebarSection+HouseholdSidebarItem";

	private NativeArray<int> m_Results;

	private NativeArray<Entity> m_ResidenceResult;

	private NativeArray<HouseholdResult> m_HouseholdResult;

	private NativeList<ResidentResult> m_ResidentsResult;

	private NativeList<Entity> m_PetsResult;

	private NativeList<HouseholdResult> m_HouseholdsResult;

	private RawMapBinding<int> m_HouseholdMap;

	private RawMapBinding<int> m_ResidentMap;

	private RawMapBinding<int> m_PetMap;

	private TypeHandle __TypeHandle;

	protected override string group => "HouseholdSidebarSection";

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForUnderConstruction => true;

	private Entity residenceEntity { get; set; }

	private HouseholdResult household { get; set; }

	private HouseholdSidebarVariant variant { get; set; }

	private bool residenceIsHomelessShelter { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResidentsResult = new NativeList<ResidentResult>(Allocator.Persistent);
		m_PetsResult = new NativeList<Entity>(Allocator.Persistent);
		m_HouseholdsResult = new NativeList<HouseholdResult>(Allocator.Persistent);
		m_Results = new NativeArray<int>(3, Allocator.Persistent);
		m_ResidenceResult = new NativeArray<Entity>(1, Allocator.Persistent);
		m_HouseholdResult = new NativeArray<HouseholdResult>(1, Allocator.Persistent);
		AddBinding(m_HouseholdMap = new RawMapBinding<int>(group, "householdMap", BindHousehold));
		AddBinding(m_ResidentMap = new RawMapBinding<int>(group, "residentMap", BindResident));
		AddBinding(m_PetMap = new RawMapBinding<int>(group, "petMap", BindPet));
	}

	private void BindHousehold(IJsonWriter writer, int index)
	{
		Entity entity = m_HouseholdsResult[index].m_Entity;
		WriteItem(writer, entity, "Media/Game/Icons/Household.svg", base.EntityManager.GetBuffer<HouseholdCitizen>(entity).Length);
	}

	private void BindResident(IJsonWriter writer, int index)
	{
		Entity entity = m_ResidentsResult[index].m_Entity;
		WriteItem(writer, entity, null);
	}

	private void BindPet(IJsonWriter writer, int index)
	{
		Entity entity = m_PetsResult[index];
		WriteItem(writer, entity, "Media/Game/Icons/Pet.svg");
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ResidentsResult.Dispose();
		m_HouseholdResult.Dispose();
		m_PetsResult.Dispose();
		m_HouseholdsResult.Dispose();
		m_Results.Dispose();
		m_ResidenceResult.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		m_ResidentsResult.Clear();
		m_PetsResult.Clear();
		m_HouseholdsResult.Clear();
		m_ResidenceResult[0] = Entity.Null;
		m_HouseholdResult[0] = default(HouseholdResult);
		m_Results[0] = 0;
		m_Results[1] = 0;
		m_Results[2] = 0;
		residenceIsHomelessShelter = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IJobExtensions.Schedule(new CheckVisibilityJob
		{
			m_SelectedEntity = selectedEntity,
			m_SelectedPrefab = selectedPrefab,
			m_ParkFromLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AbandonedLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPetLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TravelPurposeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyDataLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizenLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, base.Dependency).Complete();
		base.visible = m_Results[0] == 1 && m_Results[1] > 0;
		if (base.visible)
		{
			IJobExtensions.Schedule(new CollectDataJob
			{
				m_SelectedEntity = selectedEntity,
				m_BuildingLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HomelessHouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CitizenLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdMemberLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdPetLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TravelPurposeLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenterLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizenLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourcesLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdAnimalsLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
				m_RenterLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResidenceResult = m_ResidenceResult,
				m_HouseholdResult = m_HouseholdResult,
				m_HouseholdsResult = m_HouseholdsResult,
				m_ResidentsResult = m_ResidentsResult,
				m_PetsResult = m_PetsResult
			}, base.Dependency).Complete();
			m_HouseholdsResult.Sort();
			m_ResidentsResult.Sort();
			m_PetsResult.Sort();
			for (int i = 0; i < m_HouseholdsResult.Length; i++)
			{
				m_HouseholdMap.Update(i);
			}
			for (int j = 0; j < m_ResidentsResult.Length; j++)
			{
				m_ResidentMap.Update(j);
			}
			for (int k = 0; k < m_PetsResult.Length; k++)
			{
				m_PetMap.Update(k);
			}
		}
	}

	protected override void OnProcess()
	{
		residenceEntity = m_ResidenceResult[0];
		household = m_HouseholdResult[0];
		variant = (HouseholdSidebarVariant)m_Results[2];
		if (!base.EntityManager.Exists(residenceEntity))
		{
			residenceEntity = Entity.Null;
		}
		if ((base.EntityManager.HasComponent<Game.Buildings.Park>(residenceEntity) || base.EntityManager.HasComponent<Abandoned>(residenceEntity)) && base.EntityManager.TryGetBuffer(residenceEntity, isReadOnly: true, out DynamicBuffer<Renter> buffer) && buffer.Length > 0)
		{
			m_InfoUISystem.tags.Add(SelectedInfoTags.HomelessShelter);
			base.tooltipTags.Add(SelectedInfoTags.HomelessShelter.ToString());
			base.tooltipKeys.Add(SelectedInfoTags.HomelessShelter.ToString());
			residenceIsHomelessShelter = true;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("variant");
		writer.Write(variant.ToString());
		writer.PropertyName("residence");
		WriteItem(writer, residenceEntity, residenceIsHomelessShelter ? "Media/Glyphs/HomelessShelter.svg" : "Media/Glyphs/Residence.svg");
		writer.PropertyName("household");
		WriteItem(writer, household.m_Entity, "Media/Game/Icons/Household.svg", household.m_Members);
		writer.PropertyName("households");
		writer.Write(m_HouseholdsResult.Length);
		writer.PropertyName("residents");
		writer.Write(m_ResidentsResult.Length);
		writer.PropertyName("pets");
		writer.Write(m_PetsResult.Length);
	}

	private void WriteItem(IJsonWriter writer, Entity entity, string iconPath, int memberCount = 0)
	{
		writer.TypeBegin("Game.UI.InGame.HouseholdSidebarSection+HouseholdSidebarItem");
		writer.PropertyName("entity");
		writer.Write(entity);
		writer.PropertyName("name");
		m_NameSystem.BindName(writer, entity);
		writer.PropertyName("familyName");
		if (entity == Entity.Null || !base.EntityManager.HasComponent<Household>(entity))
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindFamilyName(writer, entity);
		}
		writer.PropertyName("icon");
		writer.Write(iconPath);
		writer.PropertyName("selected");
		writer.Write(entity == selectedEntity);
		writer.PropertyName("count");
		if (memberCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(memberCount);
		}
		writer.TypeEnd();
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
	public HouseholdSidebarSection()
	{
	}
}
