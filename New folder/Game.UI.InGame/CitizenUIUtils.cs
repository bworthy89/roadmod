using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Events;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.InGame;

public static class CitizenUIUtils
{
	public static HouseholdWealthKey GetHouseholdWealth(EntityManager entityManager, Entity householdEntity, CitizenHappinessParameterData happinessParameters)
	{
		if (entityManager.Exists(householdEntity))
		{
			int num = 0;
			if (entityManager.TryGetBuffer(householdEntity, isReadOnly: true, out DynamicBuffer<Resources> buffer) && entityManager.TryGetComponent<Household>(householdEntity, out var component))
			{
				num += EconomyUtils.GetHouseholdTotalWealth(component, buffer);
			}
			return GetHouseholdWealthKey(num, happinessParameters);
		}
		return HouseholdWealthKey.Modest;
	}

	public static int GetHouseholdTotalWealth(EntityManager entityManager, Entity householdEntity)
	{
		if (entityManager.Exists(householdEntity))
		{
			int num = 0;
			if (entityManager.TryGetBuffer(householdEntity, isReadOnly: true, out DynamicBuffer<Resources> buffer) && entityManager.TryGetComponent<Household>(householdEntity, out var component))
			{
				num += EconomyUtils.GetHouseholdTotalWealth(component, buffer);
			}
			return num;
		}
		return 0;
	}

	public static int GetHouseholdMoney(EntityManager entityManager, Entity householdEntity)
	{
		if (entityManager.Exists(householdEntity) && entityManager.TryGetBuffer(householdEntity, isReadOnly: true, out DynamicBuffer<Resources> buffer))
		{
			return EconomyUtils.GetResources(Resource.Money, buffer);
		}
		return 0;
	}

	public static HouseholdWealthKey GetAverageHouseholdWealth(EntityManager entityManager, NativeList<Entity> households, CitizenHappinessParameterData happinessParameters)
	{
		int num = 0;
		for (int i = 0; i < households.Length; i++)
		{
			Entity entity = households[i];
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Resources> buffer) && entityManager.TryGetComponent<Household>(entity, out var component))
			{
				num += EconomyUtils.GetHouseholdTotalWealth(component, buffer);
			}
		}
		num /= math.select(households.Length, 1, households.Length == 0);
		return GetHouseholdWealthKey(num, happinessParameters);
	}

	public static HouseholdWealthKey GetHouseholdWealthKey(int wealth, CitizenHappinessParameterData happinessParameters)
	{
		if (wealth < happinessParameters.m_WealthyMoneyAmount.x)
		{
			return HouseholdWealthKey.Wretched;
		}
		if (wealth < happinessParameters.m_WealthyMoneyAmount.y)
		{
			return HouseholdWealthKey.Poor;
		}
		if (wealth < happinessParameters.m_WealthyMoneyAmount.z)
		{
			return HouseholdWealthKey.Modest;
		}
		if (wealth < happinessParameters.m_WealthyMoneyAmount.w)
		{
			return HouseholdWealthKey.Comfortable;
		}
		return HouseholdWealthKey.Wealthy;
	}

	public static CitizenJobLevelKey GetJobLevel(EntityManager entityManager, Entity entity)
	{
		if (entityManager.TryGetComponent<Worker>(entity, out var component))
		{
			return (CitizenJobLevelKey)component.m_Level;
		}
		return CitizenJobLevelKey.Unknown;
	}

	public static CitizenAgeKey GetAge(EntityManager entityManager, Entity entity)
	{
		if (entityManager.TryGetComponent<Citizen>(entity, out var component))
		{
			return (CitizenAgeKey)component.GetAge();
		}
		return CitizenAgeKey.Adult;
	}

	public static CitizenOccupationKey GetOccupation(EntityManager entityManager, Entity entity)
	{
		Entity household = entityManager.GetComponentData<HouseholdMember>(entity).m_Household;
		if (entityManager.Exists(household) && entityManager.HasComponent<TouristHousehold>(household))
		{
			return CitizenOccupationKey.Tourist;
		}
		if (entityManager.TryGetComponent<Criminal>(entity, out var component))
		{
			if ((component.m_Flags & CriminalFlags.Robber) == 0)
			{
				return CitizenOccupationKey.Criminal;
			}
			return CitizenOccupationKey.Robber;
		}
		if (entityManager.HasComponent<Worker>(entity))
		{
			return CitizenOccupationKey.Worker;
		}
		if (entityManager.HasComponent<Game.Citizens.Student>(entity))
		{
			return CitizenOccupationKey.Student;
		}
		if (entityManager.TryGetComponent<Citizen>(entity, out var component2))
		{
			switch (component2.GetAge())
			{
			case CitizenAge.Adult:
				return CitizenOccupationKey.Unemployed;
			case CitizenAge.Child:
			case CitizenAge.Teen:
				return CitizenOccupationKey.None;
			case CitizenAge.Elderly:
				return CitizenOccupationKey.Retired;
			}
		}
		return CitizenOccupationKey.Unknown;
	}

	public static CitizenEducationKey GetEducation(Citizen citizen)
	{
		return citizen.GetEducationLevel() switch
		{
			1 => CitizenEducationKey.PoorlyEducated, 
			2 => CitizenEducationKey.Educated, 
			3 => CitizenEducationKey.WellEducated, 
			4 => CitizenEducationKey.HighlyEducated, 
			_ => CitizenEducationKey.Uneducated, 
		};
	}

	public static Entity GetResidenceEntity(EntityManager entityManager, Entity citizenEntity)
	{
		Entity household = entityManager.GetComponentData<HouseholdMember>(citizenEntity).m_Household;
		if (entityManager.TryGetComponent<TouristHousehold>(household, out var component) && entityManager.Exists(component.m_Hotel))
		{
			return component.m_Hotel;
		}
		if (entityManager.TryGetComponent<PropertyRenter>(household, out var component2) && entityManager.Exists(component2.m_Property))
		{
			return component2.m_Property;
		}
		if (entityManager.TryGetComponent<HomelessHousehold>(household, out var component3) && entityManager.Exists(component3.m_TempHome))
		{
			return component3.m_TempHome;
		}
		return Entity.Null;
	}

	public static CitizenResidenceKey GetResidenceType(EntityManager entityManager, Entity entity)
	{
		Citizen componentData = entityManager.GetComponentData<Citizen>(entity);
		HouseholdMember componentData2 = entityManager.GetComponentData<HouseholdMember>(entity);
		bool num = (componentData.m_State & CitizenFlags.Tourist) != 0;
		bool flag = entityManager.HasComponent<HomelessHousehold>(componentData2.m_Household);
		if (!num)
		{
			if (!flag)
			{
				return CitizenResidenceKey.Home;
			}
			return CitizenResidenceKey.Shelter;
		}
		return CitizenResidenceKey.Hotel;
	}

	public static Entity GetWorkplaceEntity(EntityManager entityManager, Entity citizenEntity)
	{
		if (!entityManager.TryGetComponent<Worker>(citizenEntity, out var component))
		{
			return Entity.Null;
		}
		if (!entityManager.TryGetComponent<PropertyRenter>(component.m_Workplace, out var component2))
		{
			return component.m_Workplace;
		}
		return component2.m_Property;
	}

	public static Entity GetCompanyEntity(EntityManager entityManager, Entity citizenEntity)
	{
		if (!entityManager.TryGetComponent<Worker>(citizenEntity, out var component))
		{
			return Entity.Null;
		}
		return component.m_Workplace;
	}

	public static CitizenWorkplaceKey GetWorkplaceType(EntityManager entityManager, Entity entity)
	{
		if (!entityManager.TryGetComponent<Worker>(entity, out var component) || !entityManager.HasComponent<CompanyData>(component.m_Workplace))
		{
			return CitizenWorkplaceKey.Building;
		}
		return CitizenWorkplaceKey.Company;
	}

	public static Entity GetSchoolEntity(EntityManager entityManager, Entity citizenEntity, out int level)
	{
		if (entityManager.TryGetComponent<Game.Citizens.Student>(citizenEntity, out var component))
		{
			level = component.m_Level;
			return component.m_School;
		}
		level = 0;
		return Entity.Null;
	}

	public static CitizenStateKey GetStateKey(EntityManager entityManager, Entity entity)
	{
		Citizen componentData = entityManager.GetComponentData<Citizen>(entity);
		Household componentData2 = entityManager.GetComponentData<Household>(entityManager.GetComponentData<HouseholdMember>(entity).m_Household);
		if (CitizenUtils.IsDead(entityManager, entity))
		{
			return CitizenStateKey.Dead;
		}
		if (entityManager.TryGetComponent<CurrentTransport>(entity, out var component) && entityManager.HasComponent<Creature>(component.m_CurrentTransport) && entityManager.HasComponent<InvolvedInAccident>(component.m_CurrentTransport))
		{
			return CitizenStateKey.InvolvedInAccident;
		}
		if (TryGetTravelPurpose(entityManager, entity, out var purpose))
		{
			bool flag = (componentData.m_State & CitizenFlags.Tourist) != 0;
			bool flag2 = (componentData.m_State & CitizenFlags.Commuter) != 0;
			bool flag3 = (componentData2.m_Flags & HouseholdFlags.MovedIn) != 0;
			switch (purpose)
			{
			case Purpose.Shopping:
				return CitizenStateKey.Shopping;
			case Purpose.Leisure:
			case Purpose.VisitAttractions:
				if (!flag)
				{
					return CitizenStateKey.FreeTime;
				}
				return CitizenStateKey.Sightseeing;
			case Purpose.GoingHome:
				if (!flag)
				{
					if (!(flag3 || flag2))
					{
						return CitizenStateKey.MovingIn;
					}
					return CitizenStateKey.GoingHome;
				}
				return CitizenStateKey.GoingBackToHotel;
			case Purpose.GoingToWork:
				return CitizenStateKey.GoingToWork;
			case Purpose.Working:
				return CitizenStateKey.Working;
			case Purpose.Sleeping:
				return CitizenStateKey.Sleeping;
			case Purpose.Exporting:
				return CitizenStateKey.Traveling;
			case Purpose.MovingAway:
				if (!flag)
				{
					return CitizenStateKey.MovingAway;
				}
				return CitizenStateKey.LeavingCity;
			case Purpose.Studying:
				return CitizenStateKey.Studying;
			case Purpose.GoingToSchool:
				return CitizenStateKey.GoingToSchool;
			case Purpose.Hospital:
				return CitizenStateKey.SeekingMedicalHelp;
			case Purpose.Safety:
				if (!PathEndReached(entityManager, entity))
				{
					return CitizenStateKey.GettingToSafety;
				}
				return CitizenStateKey.Safe;
			case Purpose.EmergencyShelter:
				return CitizenStateKey.Evacuating;
			case Purpose.Crime:
				return CitizenStateKey.CommittingCrime;
			case Purpose.GoingToJail:
				return CitizenStateKey.GoingToJail;
			case Purpose.GoingToPrison:
				return CitizenStateKey.GoingToPrison;
			case Purpose.InJail:
				return CitizenStateKey.InJail;
			case Purpose.InPrison:
				return CitizenStateKey.InPrison;
			case Purpose.Escape:
				return CitizenStateKey.Escaping;
			case Purpose.InHospital:
				return CitizenStateKey.InHospital;
			case Purpose.SendMail:
				return CitizenStateKey.SendMail;
			case Purpose.InEmergencyShelter:
				return CitizenStateKey.InEmergencyShelter;
			default:
				return CitizenStateKey.Idling;
			}
		}
		return CitizenStateKey.Idling;
	}

	private static bool TryGetTravelPurpose(EntityManager entityManager, Entity entity, out Purpose purpose)
	{
		if (entityManager.TryGetComponent<CurrentTransport>(entity, out var component) && entityManager.TryGetComponent<Divert>(component.m_CurrentTransport, out var component2))
		{
			Purpose purpose2 = component2.m_Purpose;
			if (purpose2 == Purpose.Safety || purpose2 == Purpose.Shopping || purpose2 == Purpose.SendMail)
			{
				purpose = component2.m_Purpose;
				return true;
			}
		}
		if (entityManager.TryGetComponent<TravelPurpose>(entity, out var component3))
		{
			purpose = component3.m_Purpose;
			return true;
		}
		purpose = Purpose.None;
		return false;
	}

	private static bool PathEndReached(EntityManager entityManager, Entity citizen)
	{
		if (entityManager.TryGetComponent<CurrentTransport>(citizen, out var component) && entityManager.TryGetComponent<HumanCurrentLane>(component.m_CurrentTransport, out var component2))
		{
			return CreatureUtils.PathEndReached(component2);
		}
		return false;
	}

	public static NativeList<CitizenCondition> GetCitizenConditions(EntityManager entityManager, Entity entity, Citizen citizen, HouseholdMember householdMember, NativeList<CitizenCondition> conditions)
	{
		if (entityManager.TryGetComponent<HealthProblem>(entity, out var component))
		{
			if ((component.m_Flags & HealthProblemFlags.Sick) != HealthProblemFlags.None)
			{
				conditions.Add(new CitizenCondition(CitizenConditionKey.Sick));
			}
			if ((component.m_Flags & HealthProblemFlags.Injured) != HealthProblemFlags.None)
			{
				conditions.Add(new CitizenCondition(CitizenConditionKey.Injured));
			}
			if ((component.m_Flags & (HealthProblemFlags.InDanger | HealthProblemFlags.Trapped)) != HealthProblemFlags.None)
			{
				conditions.Add(new CitizenCondition(CitizenConditionKey.InDistress));
			}
		}
		if (!entityManager.HasComponent<CommuterHousehold>(householdMember.m_Household) && !entityManager.HasComponent<TouristHousehold>(householdMember.m_Household) && BuildingUtils.IsHomelessHousehold(entityManager, householdMember.m_Household))
		{
			conditions.Add(new CitizenCondition(CitizenConditionKey.Homeless));
		}
		if (entityManager.TryGetComponent<CurrentBuilding>(entity, out var component2) && entityManager.HasComponent<Game.Buildings.EmergencyShelter>(component2.m_CurrentBuilding))
		{
			conditions.Add(new CitizenCondition(CitizenConditionKey.Evacuated));
		}
		if (citizen.m_Health <= 25)
		{
			conditions.Add(new CitizenCondition(CitizenConditionKey.Weak));
		}
		if (citizen.m_WellBeing <= 25)
		{
			conditions.Add(new CitizenCondition(CitizenConditionKey.Unwell));
		}
		conditions.Sort();
		return conditions;
	}

	public static CitizenHappiness GetCitizenHappiness(Citizen citizen)
	{
		return new CitizenHappiness((CitizenHappinessKey)CitizenUtils.GetHappinessKey(citizen.Happiness));
	}

	public static CitizenHappiness GetCitizenHappiness(int happiness)
	{
		return new CitizenHappiness((CitizenHappinessKey)CitizenUtils.GetHappinessKey(happiness));
	}
}
