using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CitizenSection : InfoSectionBase
{
	private EntityQuery m_HappinessParameterQuery;

	protected override string group => "CitizenSection";

	private CitizenKey citizenKey { get; set; }

	private CitizenStateKey stateKey { get; set; }

	private Entity householdEntity { get; set; }

	private Entity residenceEntity { get; set; }

	private CitizenResidenceKey residenceKey { get; set; }

	private Entity workplaceEntity { get; set; }

	private Entity companyEntity { get; set; }

	private CitizenWorkplaceKey workplaceKey { get; set; }

	private CitizenOccupationKey occupationKey { get; set; }

	private CitizenJobLevelKey jobLevelKey { get; set; }

	private Entity schoolEntity { get; set; }

	private int schoolLevel { get; set; }

	private CitizenEducationKey educationKey { get; set; }

	private CitizenAgeKey ageKey { get; set; }

	private HouseholdWealthKey wealthKey { get; set; }

	private Entity destinationEntity { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		RequireForUpdate(m_HappinessParameterQuery);
	}

	protected override void Reset()
	{
		householdEntity = Entity.Null;
		residenceEntity = Entity.Null;
		workplaceEntity = Entity.Null;
		schoolEntity = Entity.Null;
		destinationEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<HouseholdMember>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Citizen>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Citizen componentData = base.EntityManager.GetComponentData<Citizen>(selectedEntity);
		if (base.EntityManager.TryGetComponent<HouseholdMember>(selectedEntity, out var component))
		{
			householdEntity = component.m_Household;
			citizenKey = CitizenKey.Citizen;
			if (base.EntityManager.HasComponent<CommuterHousehold>(component.m_Household))
			{
				citizenKey = CitizenKey.Commuter;
			}
			else if (base.EntityManager.HasComponent<TouristHousehold>(component.m_Household))
			{
				citizenKey = CitizenKey.Tourist;
			}
			wealthKey = CitizenUIUtils.GetHouseholdWealth(base.EntityManager, householdEntity, m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>());
		}
		stateKey = CitizenUIUtils.GetStateKey(base.EntityManager, selectedEntity);
		residenceEntity = CitizenUIUtils.GetResidenceEntity(base.EntityManager, selectedEntity);
		residenceKey = CitizenUIUtils.GetResidenceType(base.EntityManager, selectedEntity);
		workplaceEntity = CitizenUIUtils.GetWorkplaceEntity(base.EntityManager, selectedEntity);
		companyEntity = CitizenUIUtils.GetCompanyEntity(base.EntityManager, selectedEntity);
		workplaceKey = CitizenUIUtils.GetWorkplaceType(base.EntityManager, selectedEntity);
		schoolEntity = CitizenUIUtils.GetSchoolEntity(base.EntityManager, selectedEntity, out var level);
		schoolLevel = level;
		occupationKey = CitizenUIUtils.GetOccupation(base.EntityManager, selectedEntity);
		jobLevelKey = CitizenUIUtils.GetJobLevel(base.EntityManager, selectedEntity);
		ageKey = CitizenUIUtils.GetAge(base.EntityManager, selectedEntity);
		educationKey = CitizenUIUtils.GetEducation(componentData);
		destinationEntity = GetDestination();
		if ((componentData.m_State & CitizenFlags.Male) != CitizenFlags.None)
		{
			base.tooltipTags.Add(SelectedInfoTags.Male.ToString());
		}
	}

	private Entity GetDestination()
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(selectedEntity, out var component))
		{
			Entity entity = Entity.Null;
			if (base.EntityManager.TryGetComponent<Divert>(component.m_CurrentTransport, out var component2))
			{
				Purpose purpose = component2.m_Purpose;
				if (purpose == Purpose.Safety || purpose == Purpose.Shopping || purpose == Purpose.SendMail || purpose == Purpose.Escape)
				{
					entity = component2.m_Target;
				}
			}
			if (entity == Entity.Null && base.EntityManager.TryGetComponent<Target>(component.m_CurrentTransport, out var component3))
			{
				entity = component3.m_Target;
			}
			if (base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(entity))
			{
				return entity;
			}
			if (base.EntityManager.TryGetComponent<Owner>(entity, out var component4))
			{
				return component4.m_Owner;
			}
			if (base.EntityManager.Exists(entity))
			{
				return entity;
			}
		}
		return Entity.Null;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("citizenKey");
		writer.Write(Enum.GetName(typeof(CitizenKey), citizenKey));
		writer.PropertyName("stateKey");
		writer.Write(Enum.GetName(typeof(CitizenStateKey), stateKey));
		writer.PropertyName("household");
		if (householdEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, householdEntity);
		}
		writer.PropertyName("householdEntity");
		if (householdEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(householdEntity);
		}
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
		writer.PropertyName("workplace");
		if (companyEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, companyEntity);
		}
		writer.PropertyName("workplaceEntity");
		if (workplaceEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(workplaceEntity);
		}
		writer.PropertyName("workplaceKey");
		writer.Write(Enum.GetName(typeof(CitizenWorkplaceKey), workplaceKey));
		writer.PropertyName("occupationKey");
		writer.Write(Enum.GetName(typeof(CitizenOccupationKey), occupationKey));
		writer.PropertyName("jobLevelKey");
		writer.Write(Enum.GetName(typeof(CitizenJobLevelKey), jobLevelKey));
		writer.PropertyName("school");
		if (schoolEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, schoolEntity);
		}
		writer.PropertyName("schoolEntity");
		if (schoolEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(schoolEntity);
		}
		writer.PropertyName("schoolLevel");
		writer.Write(schoolLevel);
		writer.PropertyName("educationKey");
		writer.Write(Enum.GetName(typeof(CitizenEducationKey), educationKey));
		writer.PropertyName("ageKey");
		writer.Write(Enum.GetName(typeof(CitizenAgeKey), ageKey));
		writer.PropertyName("wealthKey");
		writer.Write(Enum.GetName(typeof(HouseholdWealthKey), wealthKey));
		writer.PropertyName("destination");
		if (destinationEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, destinationEntity);
		}
		writer.PropertyName("destinationEntity");
		if (destinationEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(destinationEntity);
		}
	}

	[Preserve]
	public CitizenSection()
	{
	}
}
