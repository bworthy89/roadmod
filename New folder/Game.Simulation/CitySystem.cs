using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Companies;
using Game.Economy;
using Game.Policies;
using Game.Prefabs;
using Game.Serialization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CitySystem : GameSystemBase, ICitySystem, IDefaultSerializable, ISerializable, IPostDeserialize
{
	private EntityQuery m_ServiceFeeParameterQuery;

	private EntityQuery m_EconomyParameterQuery;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private Entity m_City;

	private int m_Money;

	private int m_XP;

	public Entity City => m_City;

	public int moneyAmount => m_Money;

	public int XP => m_XP;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ServiceFeeParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceFeeParameterData>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_City != Entity.Null)
		{
			m_Money = base.EntityManager.GetComponentData<PlayerMoney>(m_City).money;
			m_XP = base.EntityManager.GetComponentData<XP>(m_City).m_XP;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_City);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_City);
		m_Money = 0;
	}

	public void SetDefaults(Context context)
	{
		m_City = Entity.Null;
		m_Money = 0;
	}

	public void PostDeserialize(Context context)
	{
		EconomyParameterData singleton = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
		if (context.purpose == Purpose.NewGame)
		{
			if (m_City == Entity.Null)
			{
				m_City = base.EntityManager.CreateEntity(typeof(Game.City.City));
				base.EntityManager.AddComponentData(m_City, new MilestoneLevel
				{
					m_AchievedMilestone = 0
				});
				base.EntityManager.AddComponentData(m_City, new XP
				{
					m_XP = 0
				});
				base.EntityManager.AddComponentData(m_City, new DevTreePoints
				{
					m_Points = 0
				});
				base.EntityManager.AddBuffer<Policy>(m_City);
				base.EntityManager.AddBuffer<CityModifier>(m_City);
				base.EntityManager.AddComponentData(m_City, default(Loan));
				base.EntityManager.AddComponentData(m_City, default(Creditworthiness));
				base.EntityManager.AddComponentData(m_City, default(DangerLevel));
				base.EntityManager.AddBuffer<TradeCost>(m_City);
				ServiceFeeParameterData singleton2 = m_ServiceFeeParameterQuery.GetSingleton<ServiceFeeParameterData>();
				DynamicBuffer<ServiceFee> dynamicBuffer = base.EntityManager.AddBuffer<ServiceFee>(m_City);
				foreach (ServiceFee defaultFee in singleton2.GetDefaultFees())
				{
					dynamicBuffer.Add(defaultFee);
				}
				base.EntityManager.AddComponentData(m_City, new PlayerMoney(singleton.m_PlayerStartMoney));
				base.EntityManager.AddBuffer<SpecializationBonus>(m_City);
				Population componentData = default(Population);
				componentData.SetDefaults(context);
				base.EntityManager.AddComponentData(m_City, componentData);
				Tourism componentData2 = default(Tourism);
				componentData2.SetDefaults(context);
				base.EntityManager.AddComponentData(m_City, componentData2);
			}
			else
			{
				base.EntityManager.SetComponentData(m_City, new PlayerMoney(singleton.m_PlayerStartMoney));
			}
		}
		if (context.purpose == Purpose.LoadGame && context.version < Version.loanComponent)
		{
			base.EntityManager.AddComponentData(m_City, default(Loan));
			base.EntityManager.AddComponentData(m_City, default(Creditworthiness));
		}
		if (context.purpose == Purpose.NewGame || context.purpose == Purpose.LoadGame)
		{
			PlayerMoney componentData3 = base.EntityManager.GetComponentData<PlayerMoney>(m_City);
			componentData3.m_Unlimited = m_CityConfigurationSystem.unlimitedMoney;
			base.EntityManager.SetComponentData(m_City, componentData3);
			if (base.EntityManager.HasComponent<Resources>(m_City))
			{
				base.EntityManager.RemoveComponent<Resources>(m_City);
			}
		}
		if (context.purpose == Purpose.LoadGame && context.version < Version.dangerLevel)
		{
			base.EntityManager.AddComponentData(m_City, default(DangerLevel));
		}
		if (context.version < Version.cityTradeCost && (context.purpose == Purpose.NewGame || context.purpose == Purpose.LoadGame))
		{
			DynamicBuffer<TradeCost> costs = base.EntityManager.AddBuffer<TradeCost>(m_City);
			ResourceIterator iterator = ResourceIterator.GetIterator();
			ResourcePrefabs prefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
			int num = 20000;
			while (iterator.Next())
			{
				float num2 = (float)EconomyUtils.GetTransportCost(10000f, iterator.resource, num, base.EntityManager.GetComponentData<ResourceData>(prefabs[iterator.resource]).m_Weight) / (float)num;
				EconomyUtils.SetTradeCost(iterator.resource, new TradeCost
				{
					m_BuyCost = num2,
					m_SellCost = num2,
					m_Resource = iterator.resource
				}, costs, keepLastTime: true);
			}
		}
	}

	[Preserve]
	public CitySystem()
	{
	}
}
