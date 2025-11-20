using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Prefabs.Modes;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GameModeGovernmentSubsidiesSystem : GameSystemBase
{
	public static readonly int kUpdatesPerDay = 128;

	private int m_LastSubsidyCoverPerDay;

	private int m_MonthlySubsidy;

	private ICityServiceBudgetSystem m_CityServiceBudgetSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_GameModeSettingQuery;

	public int LastSubsidyCoverPerDay => m_LastSubsidyCoverPerDay;

	public int monthlySubsidy => m_MonthlySubsidy;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public bool GetGovernmentSubsidiesEnabled()
	{
		return base.Enabled;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityServiceBudgetSystem = base.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_GameModeSettingQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		RequireForUpdate(m_GameModeSettingQuery);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_MonthlySubsidy = 0;
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			base.Enabled = false;
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable && singleton.m_EnableGovernmentSubsidies)
		{
			base.Enabled = true;
		}
		else
		{
			base.Enabled = false;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_MonthlySubsidy = 0;
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter || m_CitySystem.City == Entity.Null)
		{
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		PlayerMoney componentData = base.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);
		if (componentData.money < singleton.m_MoneyCoverThreshold.x)
		{
			float num = singleton.m_MoneyCoverThreshold.x - singleton.m_MoneyCoverThreshold.y;
			float num2 = math.clamp(1f - (float)(componentData.money - singleton.m_MoneyCoverThreshold.y) / num, 0f, 1f) * ((float)singleton.m_MaxMoneyCoverPercentage / 100f);
			if (num2 > 0f)
			{
				m_MonthlySubsidy = math.abs((int)(num2 * (float)m_CityServiceBudgetSystem.GetTotalExpenses()));
				m_LastSubsidyCoverPerDay = m_MonthlySubsidy / kUpdatesPerDay;
			}
		}
	}

	[Preserve]
	public GameModeGovernmentSubsidiesSystem()
	{
	}
}
