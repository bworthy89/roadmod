using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LifePathUISystem : UISystemBase
{
	private struct FollowedCitizenComparer : IComparer<Entity>
	{
		private EntityManager m_EntityManager;

		public FollowedCitizenComparer(EntityManager entityManager)
		{
			m_EntityManager = entityManager;
		}

		public int Compare(Entity a, Entity b)
		{
			int num = m_EntityManager.GetComponentData<Followed>(a).m_Priority.CompareTo(m_EntityManager.GetComponentData<Followed>(b).m_Priority);
			if (num == 0)
			{
				return a.CompareTo(b);
			}
			return num;
		}
	}

	private const string kGroup = "lifePath";

	private LifePathEventSystem m_LifePathEventSystem;

	private NameSystem m_NameSystem;

	private SelectedInfoUISystem m_SelectedInfoUISystem;

	private ChirperUISystem m_ChirperUISystem;

	private EntityQuery m_FollowedQuery;

	private EntityQuery m_HappinessParameterQuery;

	private int m_FollowedVersion;

	private int m_LifePathEntryVersion;

	private int m_ChirpVersion;

	private RawValueBinding m_FollowedCitizensBinding;

	private RawMapBinding<Entity> m_LifePathDetailsBinding;

	private RawMapBinding<Entity> m_LifePathItemsBinding;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LifePathEventSystem = base.World.GetOrCreateSystemManaged<LifePathEventSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_SelectedInfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_ChirperUISystem = base.World.GetOrCreateSystemManaged<ChirperUISystem>();
		m_FollowedQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<Followed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		AddBinding(m_FollowedCitizensBinding = new RawValueBinding("lifePath", "followedCitizens", BindFollowedCitizens));
		AddBinding(m_LifePathDetailsBinding = new RawMapBinding<Entity>("lifePath", "lifePathDetails", delegate(IJsonWriter binder, Entity citizen)
		{
			BindLifePathDetails(binder, citizen);
		}));
		AddBinding(m_LifePathItemsBinding = new RawMapBinding<Entity>("lifePath", "lifePathItems", delegate(IJsonWriter binder, Entity citizen)
		{
			BindLifePathItems(binder, citizen);
		}));
		AddBinding(new TriggerBinding<Entity>("lifePath", "followCitizen", FollowCitizen));
		AddBinding(new TriggerBinding<Entity>("lifePath", "unfollowCitizen", UnfollowCitizen));
		AddBinding(new ValueBinding<int>("lifePath", "maxFollowedCitizens", LifePathEventSystem.kMaxFollowed));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int componentOrderVersion = base.EntityManager.GetComponentOrderVersion<Followed>();
		if (m_FollowedVersion != componentOrderVersion)
		{
			m_FollowedCitizensBinding.Update();
			m_LifePathDetailsBinding.UpdateAll();
			m_FollowedVersion = componentOrderVersion;
		}
		int componentOrderVersion2 = base.EntityManager.GetComponentOrderVersion<LifePathEntry>();
		int componentOrderVersion3 = base.EntityManager.GetComponentOrderVersion<Game.Triggers.Chirp>();
		if (m_LifePathEntryVersion != componentOrderVersion2 || m_ChirpVersion != componentOrderVersion3)
		{
			m_LifePathItemsBinding.UpdateAll();
			m_LifePathEntryVersion = componentOrderVersion2;
			m_ChirpVersion = componentOrderVersion3;
		}
	}

	private void FollowCitizen(Entity citizen)
	{
		m_LifePathEventSystem.FollowCitizen(citizen);
	}

	private void UnfollowCitizen(Entity citizen)
	{
		m_LifePathEventSystem.UnfollowCitizen(citizen);
		m_SelectedInfoUISystem.SetDirty();
	}

	private void BindFollowedCitizens(IJsonWriter binder)
	{
		NativeArray<Entity> sortedFollowedCitizens = GetSortedFollowedCitizens();
		binder.ArrayBegin(sortedFollowedCitizens.Length);
		for (int i = 0; i < sortedFollowedCitizens.Length; i++)
		{
			Entity entity = sortedFollowedCitizens[i];
			binder.TypeBegin("lifePath.FollowedCitizen");
			binder.PropertyName("entity");
			binder.Write(entity);
			binder.PropertyName("name");
			m_NameSystem.BindName(binder, entity);
			binder.PropertyName("age");
			binder.Write(Enum.GetName(typeof(CitizenAgeKey), CitizenUIUtils.GetAge(base.EntityManager, entity)));
			binder.PropertyName("dead");
			binder.Write(CitizenUtils.IsDead(base.EntityManager, entity));
			binder.TypeEnd();
		}
		binder.ArrayEnd();
		sortedFollowedCitizens.Dispose();
	}

	private NativeArray<Entity> GetSortedFollowedCitizens()
	{
		NativeArray<Entity> nativeArray = m_FollowedQuery.ToEntityArray(Allocator.Temp);
		nativeArray.Sort(new FollowedCitizenComparer(base.EntityManager));
		return nativeArray;
	}

	private void BindLifePathDetails(IJsonWriter binder, Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Citizen>(entity, out var component) && base.EntityManager.HasComponent<Followed>(entity))
		{
			Entity residenceEntity = CitizenUIUtils.GetResidenceEntity(base.EntityManager, entity);
			CitizenResidenceKey residenceType = CitizenUIUtils.GetResidenceType(base.EntityManager, entity);
			Entity workplaceEntity = CitizenUIUtils.GetWorkplaceEntity(base.EntityManager, entity);
			Entity companyEntity = CitizenUIUtils.GetCompanyEntity(base.EntityManager, entity);
			CitizenWorkplaceKey workplaceType = CitizenUIUtils.GetWorkplaceType(base.EntityManager, entity);
			int level;
			Entity schoolEntity = CitizenUIUtils.GetSchoolEntity(base.EntityManager, entity, out level);
			CitizenOccupationKey occupation = CitizenUIUtils.GetOccupation(base.EntityManager, entity);
			CitizenJobLevelKey jobLevel = CitizenUIUtils.GetJobLevel(base.EntityManager, entity);
			CitizenAgeKey age = CitizenUIUtils.GetAge(base.EntityManager, entity);
			CitizenEducationKey education = CitizenUIUtils.GetEducation(component);
			HouseholdMember componentData = base.EntityManager.GetComponentData<HouseholdMember>(entity);
			NativeList<CitizenCondition> citizenConditions = CitizenUIUtils.GetCitizenConditions(base.EntityManager, entity, component, componentData, new NativeList<CitizenCondition>(Allocator.TempJob));
			HouseholdWealthKey householdWealth = CitizenUIUtils.GetHouseholdWealth(base.EntityManager, componentData.m_Household, m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>());
			bool flag = CitizenUtils.IsDead(base.EntityManager, entity);
			binder.TypeBegin("lifePath.LifePathDetails");
			binder.PropertyName("entity");
			binder.Write(entity);
			binder.PropertyName("name");
			m_NameSystem.BindName(binder, entity);
			binder.PropertyName("avatar");
			binder.WriteNull();
			binder.PropertyName("randomIndex");
			binder.Write(GetRandomIndex(entity));
			binder.PropertyName("birthDay");
			binder.Write(component.m_BirthDay);
			binder.PropertyName("age");
			binder.Write(Enum.GetName(typeof(CitizenAgeKey), age));
			binder.PropertyName("education");
			binder.Write(Enum.GetName(typeof(CitizenEducationKey), education));
			binder.PropertyName("wealth");
			binder.Write(Enum.GetName(typeof(HouseholdWealthKey), householdWealth));
			binder.PropertyName("occupation");
			binder.Write(Enum.GetName(typeof(CitizenOccupationKey), occupation));
			binder.PropertyName("jobLevel");
			binder.Write(Enum.GetName(typeof(CitizenJobLevelKey), jobLevel));
			binder.PropertyName("residenceName");
			if (residenceEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				m_NameSystem.BindName(binder, residenceEntity);
			}
			binder.PropertyName("residenceEntity");
			if (residenceEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				binder.Write(residenceEntity);
			}
			binder.PropertyName("residenceKey");
			binder.Write(Enum.GetName(typeof(CitizenResidenceKey), residenceType));
			binder.PropertyName("workplaceName");
			if (companyEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				m_NameSystem.BindName(binder, companyEntity);
			}
			binder.PropertyName("workplaceEntity");
			if (workplaceEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				binder.Write(workplaceEntity);
			}
			binder.PropertyName("workplaceKey");
			binder.Write(Enum.GetName(typeof(CitizenWorkplaceKey), workplaceType));
			binder.PropertyName("schoolName");
			if (schoolEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				m_NameSystem.BindName(binder, schoolEntity);
			}
			binder.PropertyName("schoolEntity");
			if (schoolEntity == Entity.Null)
			{
				binder.WriteNull();
			}
			else
			{
				binder.Write(schoolEntity);
			}
			binder.PropertyName("conditions");
			if (flag)
			{
				binder.WriteEmptyArray();
			}
			else
			{
				binder.ArrayBegin(citizenConditions.Length);
				for (int i = 0; i < citizenConditions.Length; i++)
				{
					binder.Write(citizenConditions[i]);
				}
				binder.ArrayEnd();
			}
			binder.PropertyName("happiness");
			if (flag)
			{
				binder.WriteNull();
			}
			else
			{
				binder.Write(CitizenUIUtils.GetCitizenHappiness(component));
			}
			binder.PropertyName("state");
			binder.Write(Enum.GetName(typeof(CitizenStateKey), CitizenUIUtils.GetStateKey(base.EntityManager, entity)));
			binder.TypeEnd();
			citizenConditions.Dispose();
		}
		else
		{
			binder.WriteNull();
		}
	}

	private void BindLifePathItems(IJsonWriter binder, Entity citizen)
	{
		if (base.EntityManager.TryGetBuffer(citizen, isReadOnly: true, out DynamicBuffer<LifePathEntry> buffer))
		{
			binder.ArrayBegin(buffer.Length);
			for (int num = buffer.Length - 1; num >= 0; num--)
			{
				Entity entity = buffer[num].m_Entity;
				if (!base.EntityManager.HasComponent<Deleted>(entity))
				{
					if (base.EntityManager.HasComponent<Game.Triggers.Chirp>(entity))
					{
						m_ChirperUISystem.BindChirp(binder, entity);
					}
					else if (base.EntityManager.HasComponent<Game.Triggers.LifePathEvent>(entity))
					{
						BindLifePathEvent(binder, entity);
					}
					else
					{
						binder.WriteNull();
					}
				}
				else
				{
					binder.WriteNull();
				}
			}
			binder.ArrayEnd();
		}
		else
		{
			binder.WriteEmptyArray();
		}
	}

	private void BindLifePathEvent(IJsonWriter binder, Entity entity)
	{
		string messageID = m_ChirperUISystem.GetMessageID(entity);
		Game.Triggers.LifePathEvent componentData = base.EntityManager.GetComponentData<Game.Triggers.LifePathEvent>(entity);
		binder.TypeBegin("lifePath.LifePathEvent");
		binder.PropertyName("entity");
		binder.Write(entity);
		binder.PropertyName("date");
		binder.Write(componentData.m_Date);
		binder.PropertyName("messageId");
		binder.Write(messageID);
		binder.TypeEnd();
	}

	private int GetRandomIndex(Entity entity)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RandomLocalizationIndex> buffer) && buffer.Length > 0)
		{
			return buffer[0].m_Index;
		}
		return 0;
	}

	[Preserve]
	public LifePathUISystem()
	{
	}
}
