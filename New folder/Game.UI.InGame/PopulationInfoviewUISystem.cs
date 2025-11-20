using Colossal.UI.Binding;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class PopulationInfoviewUISystem : InfoviewUISystemBase
{
	private const string kGroup = "populationInfo";

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CitySystem m_CitySystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private ValueBinding<int> m_Population;

	private ValueBinding<int> m_Employed;

	private ValueBinding<int> m_Jobs;

	private ValueBinding<float> m_Unemployment;

	private ValueBinding<float> m_Homelessness;

	private ValueBinding<int> m_BirthRate;

	private ValueBinding<int> m_DeathRate;

	private ValueBinding<int> m_MovedIn;

	private ValueBinding<int> m_MovedAway;

	private ValueBinding<int> m_Homeless;

	private RawValueBinding m_AgeData;

	private EntityQuery m_WorkProviderModifiedQuery;

	private EntityQuery m_PopulationModifiedQuery;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_Population.active && !m_Employed.active && !m_Jobs.active && !m_Unemployment.active && !m_BirthRate.active && !m_DeathRate.active && !m_MovedIn.active && !m_MovedAway.active && !m_AgeData.active)
			{
				return m_Homeless.active;
			}
			return true;
		}
	}

	protected override bool Modified
	{
		get
		{
			if (m_WorkProviderModifiedQuery.IsEmptyIgnoreFilter)
			{
				return m_PopulationModifiedQuery.IsEmptyIgnoreFilter;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_WorkProviderModifiedQuery = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>());
		m_PopulationModifiedQuery = GetEntityQuery(ComponentType.ReadOnly<Population>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>());
		AddBinding(m_Population = new ValueBinding<int>("populationInfo", "population", 0));
		AddBinding(m_Employed = new ValueBinding<int>("populationInfo", "employed", 0));
		AddBinding(m_Jobs = new ValueBinding<int>("populationInfo", "jobs", 0));
		AddBinding(m_Unemployment = new ValueBinding<float>("populationInfo", "unemployment", 0f));
		AddBinding(m_BirthRate = new ValueBinding<int>("populationInfo", "birthRate", 0));
		AddBinding(m_DeathRate = new ValueBinding<int>("populationInfo", "deathRate", 0));
		AddBinding(m_MovedIn = new ValueBinding<int>("populationInfo", "movedIn", 0));
		AddBinding(m_MovedAway = new ValueBinding<int>("populationInfo", "movedAway", 0));
		AddBinding(m_Homeless = new ValueBinding<int>("populationInfo", "homeless", 0));
		AddBinding(m_Homelessness = new ValueBinding<float>("populationInfo", "homelessness", 0f));
		AddBinding(m_AgeData = new RawValueBinding("populationInfo", "ageData", UpdateAgeData));
	}

	protected override void PerformUpdate()
	{
		UpdateBindings();
	}

	private void UpdateBindings()
	{
		m_Jobs.Update(m_CountWorkplacesSystem.GetTotalWorkplaces().TotalCount);
		m_Employed.Update(m_CountHouseholdDataSystem.CityWorkerCount);
		m_Unemployment.Update(m_CountHouseholdDataSystem.UnemploymentRate);
		m_Homelessness.Update(m_CountHouseholdDataSystem.HomelessnessRate);
		m_Homeless.Update(m_CountHouseholdDataSystem.HomelessCitizenCount);
		Population componentData = base.EntityManager.GetComponentData<Population>(m_CitySystem.City);
		m_Population.Update(componentData.m_Population);
		m_AgeData.Update();
		UpdateStatistics();
	}

	private void UpdateAgeData(IJsonWriter binder)
	{
		binder.TypeBegin("infoviews.ChartData");
		binder.PropertyName("values");
		binder.ArrayBegin(4u);
		binder.Write(m_CountHouseholdDataSystem.ChildrenCount);
		binder.Write(m_CountHouseholdDataSystem.TeenCount);
		binder.Write(m_CountHouseholdDataSystem.AdultCount);
		binder.Write(m_CountHouseholdDataSystem.SeniorCount);
		binder.ArrayEnd();
		binder.PropertyName("total");
		binder.Write(m_CountHouseholdDataSystem.MovedInCitizenCount);
		binder.TypeEnd();
	}

	private void UpdateStatistics()
	{
		m_BirthRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.BirthRate));
		m_DeathRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.DeathRate));
		m_MovedIn.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn));
		m_MovedAway.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway));
	}

	[Preserve]
	public PopulationInfoviewUISystem()
	{
	}
}
