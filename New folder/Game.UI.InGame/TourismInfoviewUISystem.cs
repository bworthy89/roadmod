using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TourismInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		Price,
		HotelCount,
		ResultCount
	}

	[BurstCompile]
	private struct CalculateAverageHotelPriceJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<LodgingProvider> m_LodgingProviderHandle;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<LodgingProvider> nativeArray = chunk.GetNativeArray(ref m_LodgingProviderHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				LodgingProvider lodgingProvider = nativeArray[i];
				if (lodgingProvider.m_Price > 0)
				{
					m_Results[0] += lodgingProvider.m_Price;
					m_Results[1]++;
				}
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
		public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_LodgingProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>(isReadOnly: true);
		}
	}

	private const string kGroup = "tourismInfo";

	private ClimateSystem m_ClimateSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CitySystem m_CitySystem;

	private ValueBinding<IndicatorValue> m_Attractiveness;

	private ValueBinding<int> m_TourismRate;

	private ValueBinding<float> m_AverageHotelPrice;

	private ValueBinding<float> m_WeatherEffect;

	private EntityQuery m_HotelQuery;

	private EntityQuery m_HotelModifiedQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1647950437_0;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_Attractiveness.active && !m_TourismRate.active && !m_AverageHotelPrice.active)
			{
				return m_WeatherEffect.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_HotelModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		AddBinding(m_Attractiveness = new ValueBinding<IndicatorValue>("tourismInfo", "attractiveness", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		AddBinding(m_TourismRate = new ValueBinding<int>("tourismInfo", "tourismRate", 0));
		AddBinding(m_AverageHotelPrice = new ValueBinding<float>("tourismInfo", "averageHotelPrice", 0f));
		AddBinding(m_WeatherEffect = new ValueBinding<float>("tourismInfo", "weatherEffect", 0f));
		m_HotelQuery = GetEntityQuery(ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<LodgingProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HotelModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<LodgingProvider>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Created>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		RequireForUpdate<AttractivenessParameterData>();
		m_Results = new NativeArray<int>(2, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		UpdateAttractiveness();
		UpdateTourismRate();
		UpdateWeatherEffect();
		UpdateAverageHotelPrice();
	}

	private void UpdateAttractiveness()
	{
		if (base.EntityManager.TryGetComponent<Tourism>(m_CitySystem.City, out var component))
		{
			m_Attractiveness.Update(new IndicatorValue(0f, 100f, component.m_Attractiveness));
		}
	}

	private void UpdateTourismRate()
	{
		m_TourismRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.TouristCount));
	}

	private void UpdateWeatherEffect()
	{
		m_WeatherEffect.Update(100f * (0f - (1f - TourismSystem.GetWeatherEffect(__query_1647950437_0.GetSingleton<AttractivenessParameterData>(), m_ClimateSystem.classification, m_ClimateSystem.temperature, m_ClimateSystem.precipitation, m_ClimateSystem.isRaining, m_ClimateSystem.isSnowing))));
	}

	private void UpdateAverageHotelPrice()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
		JobChunkExtensions.Schedule(new CalculateAverageHotelPriceJob
		{
			m_LodgingProviderHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_HotelQuery, base.Dependency).Complete();
		int num = m_Results[1];
		float newValue = ((num > 0) ? (m_Results[0] / num) : 0);
		m_AverageHotelPrice.Update(newValue);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<AttractivenessParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1647950437_0 = entityQueryBuilder2.Build(ref state);
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
	public TourismInfoviewUISystem()
	{
	}
}
