using Colossal.Entities;
using Game.Agents;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Citizens;

public static class CitizenUtils
{
	public static bool IsDead(Entity citizen, ref ComponentLookup<HealthProblem> healthProblems)
	{
		if (healthProblems.TryGetComponent(citizen, out var componentData))
		{
			return IsDead(componentData);
		}
		return false;
	}

	public static bool IsDead(EntityManager entityManager, Entity citizen)
	{
		if (entityManager.TryGetComponent<HealthProblem>(citizen, out var component))
		{
			return IsDead(component);
		}
		return false;
	}

	public static bool IsDead(HealthProblem healthProblem)
	{
		return (healthProblem.m_Flags & HealthProblemFlags.Dead) != 0;
	}

	public static bool IsCorpsePickedByHearse(Entity citizen, ref ComponentLookup<HealthProblem> healthProblems, ref ComponentLookup<TravelPurpose> travelPurposes)
	{
		if (IsDead(citizen, ref healthProblems) && travelPurposes.TryGetComponent(citizen, out var componentData) && (componentData.m_Purpose == Purpose.Deathcare || componentData.m_Purpose == Purpose.InDeathcare))
		{
			return true;
		}
		return false;
	}

	public static bool IsCorpsePickedByHearse(EntityManager entityManager, Entity citizen)
	{
		if (IsDead(entityManager, citizen) && entityManager.TryGetComponent<TravelPurpose>(citizen, out var component) && (component.m_Purpose == Purpose.Deathcare || component.m_Purpose == Purpose.InDeathcare))
		{
			return true;
		}
		return false;
	}

	public static bool TryGetResident(Entity entity, ComponentLookup<Citizen> citizenFromEntity, out Citizen citizen)
	{
		if (citizenFromEntity.TryGetComponent(entity, out citizen))
		{
			return (citizen.m_State & (CitizenFlags.MovingAwayReachOC | CitizenFlags.Tourist | CitizenFlags.Commuter)) == 0;
		}
		return false;
	}

	public static PathfindWeights GetPathfindWeights(Citizen citizen, Household household, int householdCitizens)
	{
		float time = 5f * (4f - 3.75f * (float)(int)citizen.m_LeisureCounter / 255f);
		float behaviour = 2f;
		float num = 2500f * math.max(1f, householdCitizens) / (float)math.max(250, household.m_ConsumptionPerDay);
		float comfort = 1f + 2f * citizen.GetPseudoRandom(CitizenPseudoRandom.TrafficComfort).NextFloat();
		num = math.select(num, num * 0.1f, (household.m_Flags & HouseholdFlags.MovedIn) == 0 && (citizen.m_State & (CitizenFlags.MovingAwayReachOC | CitizenFlags.Tourist | CitizenFlags.Commuter)) == 0);
		return new PathfindWeights(time, behaviour, num, comfort);
	}

	public static bool IsCommuter(Entity citizenEntity, ref ComponentLookup<Citizen> citizens)
	{
		return (citizens[citizenEntity].m_State & CitizenFlags.Commuter) != 0;
	}

	public static bool IsResident(EntityManager entityManager, Entity entity, out Citizen citizen)
	{
		if (!entityManager.TryGetComponent<Citizen>(entity, out citizen))
		{
			return false;
		}
		if (!entityManager.TryGetComponent<HouseholdMember>(entity, out var component))
		{
			return false;
		}
		if (entityManager.HasComponent<TouristHousehold>(component.m_Household))
		{
			return false;
		}
		if (entityManager.HasComponent<CommuterHousehold>(component.m_Household))
		{
			return false;
		}
		if (entityManager.HasComponent<MovingAway>(component.m_Household))
		{
			return false;
		}
		return (citizen.m_State & (CitizenFlags.MovingAwayReachOC | CitizenFlags.Tourist | CitizenFlags.Commuter)) == 0;
	}

	public static bool IsResident(Entity entity, Citizen citizen, ComponentLookup<HouseholdMember> householdMemberFromEntity, ComponentLookup<MovingAway> movingAwayFromEntity, ComponentLookup<TouristHousehold> touristHouseholdFromEntity, ComponentLookup<CommuterHousehold> commuterHouseholdFromEntity)
	{
		if (!householdMemberFromEntity.TryGetComponent(entity, out var componentData))
		{
			return false;
		}
		if (touristHouseholdFromEntity.HasComponent(componentData.m_Household))
		{
			return false;
		}
		if (commuterHouseholdFromEntity.HasComponent(componentData.m_Household))
		{
			return false;
		}
		if (movingAwayFromEntity.HasComponent(componentData.m_Household))
		{
			return false;
		}
		return (citizen.m_State & (CitizenFlags.MovingAwayReachOC | CitizenFlags.Tourist | CitizenFlags.Commuter)) == 0;
	}

	public static bool HasMovedIn(Entity householdEntity, ComponentLookup<Household> householdDatas)
	{
		if (householdDatas.TryGetComponent(householdEntity, out var componentData))
		{
			return (componentData.m_Flags & HouseholdFlags.MovedIn) != 0;
		}
		return false;
	}

	public static bool HasMovedIn(Entity citizen, ref ComponentLookup<HouseholdMember> householdMembers, ref ComponentLookup<Household> households, ref ComponentLookup<HomelessHousehold> homelessHouseholds)
	{
		if (householdMembers.TryGetComponent(citizen, out var componentData) && households.TryGetComponent(componentData.m_Household, out var componentData2) && (componentData2.m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None)
		{
			return !homelessHouseholds.HasComponent(componentData.m_Household);
		}
		return false;
	}

	public static CitizenHappiness GetHappinessKey(int happiness)
	{
		if (happiness > 70)
		{
			return CitizenHappiness.Happy;
		}
		if (happiness > 55)
		{
			return CitizenHappiness.Content;
		}
		if (happiness > 40)
		{
			return CitizenHappiness.Neutral;
		}
		if (happiness > 25)
		{
			return CitizenHappiness.Sad;
		}
		return CitizenHappiness.Depressed;
	}

	public static Entity GetCitizenSelectedSound(EntityManager entityManager, Entity entity, Citizen citizen, Entity citizenPrefabRef)
	{
		if (!entityManager.HasComponent<CitizenSelectedSoundData>(citizenPrefabRef))
		{
			return Entity.Null;
		}
		CitizenHappiness happinessKey = GetHappinessKey(citizen.Happiness);
		bool isSickOrInjured = false;
		if (entityManager.TryGetComponent<HealthProblem>(entity, out var component) && (component.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured)) != HealthProblemFlags.None)
		{
			isSickOrInjured = true;
		}
		DynamicBuffer<CitizenSelectedSoundData> buffer = entityManager.GetBuffer<CitizenSelectedSoundData>(citizenPrefabRef, isReadOnly: true);
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].Equals(new CitizenSelectedSoundData(isSickOrInjured, citizen.GetAge(), happinessKey, Entity.Null)))
			{
				return buffer[i].m_SelectedSound;
			}
		}
		return Entity.Null;
	}

	public static bool IsHouseholdNeedSupport(DynamicBuffer<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<Student> students)
	{
		bool result = true;
		for (int i = 0; i < householdCitizens.Length; i++)
		{
			Entity citizen = householdCitizens[i].m_Citizen;
			if (citizens[citizen].GetAge() == CitizenAge.Adult && !students.HasComponent(citizen))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	public static bool IsWorkableCitizen(Entity citizenEntity, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<Student> m_Students, ref ComponentLookup<HealthProblem> healthProblems)
	{
		if ((!healthProblems.HasComponent(citizenEntity) || !IsDead(healthProblems[citizenEntity])) && !m_Students.HasComponent(citizenEntity) && (citizens[citizenEntity].m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) == 0 && (citizens[citizenEntity].GetAge() == CitizenAge.Teen || citizens[citizenEntity].GetAge() == CitizenAge.Adult))
		{
			return true;
		}
		return false;
	}

	public static Entity GetCitizenPrefabFromCitizen(NativeList<Entity> citizenPrefabs, Citizen citizen, ComponentLookup<CitizenData> citizenDatas, Random rnd)
	{
		int num = 0;
		for (int i = 0; i < citizenPrefabs.Length; i++)
		{
			CitizenData citizenData = citizenDatas[citizenPrefabs[i]];
			if (((citizen.m_State & CitizenFlags.Male) == 0) ^ citizenData.m_Male)
			{
				num++;
			}
		}
		if (num > 0)
		{
			int num2 = rnd.NextInt(num);
			for (int j = 0; j < citizenPrefabs.Length; j++)
			{
				CitizenData citizenData2 = citizenDatas[citizenPrefabs[j]];
				if (((citizen.m_State & CitizenFlags.Male) == 0) ^ citizenData2.m_Male)
				{
					num2--;
					if (num2 < 0)
					{
						PrefabRef prefabRef = new PrefabRef
						{
							m_Prefab = citizenPrefabs[j]
						};
						return prefabRef.m_Prefab;
					}
				}
			}
		}
		return Entity.Null;
	}

	public static void HouseholdMoveAway(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity householdEntity, MoveAwayReason reason)
	{
		commandBuffer.AddComponent(sortKey, householdEntity, new MovingAway
		{
			m_Reason = reason
		});
	}

	public static void HouseholdMoveAway(EntityCommandBuffer commandBuffer, Entity householdEntity, MoveAwayReason reason = MoveAwayReason.NoSuitableProperty)
	{
		commandBuffer.AddComponent(householdEntity, new MovingAway
		{
			m_Reason = reason
		});
	}
}
