using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PollutionInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		GroundPollution,
		GroundPollutionCount,
		AirPollution,
		AirPollutionCount,
		NoisePollution,
		NoisePollutionCount,
		ConsumedWater,
		PollutedWater,
		ResultCount
	}

	[BurstCompile]
	private struct CalculateAveragePollutionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerFromEntity;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformFromEntity;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_GroundPollutionMap;

		[ReadOnly]
		public Entity m_City;

		public CitizenHappinessParameterData m_HappinessParameters;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PropertyRenter> nativeArray = chunk.GetNativeArray(ref m_PropertyRenterType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			float num8 = 0f;
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity property = nativeArray[i].m_Property;
				int2 airPollutionBonuses = CitizenHappinessSystem.GetAirPollutionBonuses(property, ref m_TransformFromEntity, m_AirPollutionMap, cityModifiers, in m_HappinessParameters);
				num3 += airPollutionBonuses.x + airPollutionBonuses.y;
				num6++;
				int2 groundPollutionBonuses = CitizenHappinessSystem.GetGroundPollutionBonuses(property, ref m_TransformFromEntity, m_GroundPollutionMap, cityModifiers, in m_HappinessParameters);
				num += groundPollutionBonuses.x + groundPollutionBonuses.y;
				num4++;
				int2 noiseBonuses = CitizenHappinessSystem.GetNoiseBonuses(property, ref m_TransformFromEntity, m_NoisePollutionMap, in m_HappinessParameters);
				num2 += noiseBonuses.x + noiseBonuses.y;
				num5++;
				if (m_WaterConsumerFromEntity.TryGetComponent(property, out var componentData))
				{
					num7 += componentData.m_FulfilledFresh;
					num8 += componentData.m_Pollution * (float)componentData.m_FulfilledFresh;
				}
			}
			m_Results[0] += num;
			m_Results[1] += num4;
			m_Results[2] += num3;
			m_Results[3] += num6;
			m_Results[4] += num2;
			m_Results[5] += num5;
			m_Results[6] += num7;
			m_Results[7] += Mathf.RoundToInt(num8);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const string kGroup = "pollutionInfo";

	private AirPollutionSystem m_AirPollutionSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	protected CitySystem m_CitySystem;

	private ValueBinding<IndicatorValue> m_AverageGroundPollution;

	private ValueBinding<IndicatorValue> m_AverageWaterPollution;

	private ValueBinding<IndicatorValue> m_AverageAirPollution;

	private ValueBinding<IndicatorValue> m_AverageNoisePollution;

	private EntityQuery m_HouseholdQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_374463591_0;

	public override GameMode gameMode => GameMode.GameOrEditor;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_AverageAirPollution.active && !m_AverageGroundPollution.active && !m_AverageNoisePollution.active)
			{
				return m_AverageWaterPollution.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_Results = new NativeArray<int>(8, Allocator.Persistent);
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<PropertySeeker>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
		AddBinding(m_AverageGroundPollution = new ValueBinding<IndicatorValue>("pollutionInfo", "averageGroundPollution", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		AddBinding(m_AverageWaterPollution = new ValueBinding<IndicatorValue>("pollutionInfo", "averageWaterPollution", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		AddBinding(m_AverageAirPollution = new ValueBinding<IndicatorValue>("pollutionInfo", "averageAirPollution", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		AddBinding(m_AverageNoisePollution = new ValueBinding<IndicatorValue>("pollutionInfo", "averageNoisePollution", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		RequireForUpdate<CitizenHappinessParameterData>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		JobHandle dependencies;
		NativeArray<GroundPollution> map = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeArray<AirPollution> map2 = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2);
		JobHandle dependencies3;
		NativeArray<NoisePollution> map3 = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3);
		JobHandle job = JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3);
		CitizenHappinessParameterData singleton = __query_374463591_0.GetSingleton<CitizenHappinessParameterData>();
		ResetResults();
		JobChunkExtensions.Schedule(new CalculateAveragePollutionJob
		{
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterConsumerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_AirPollutionMap = map2,
			m_NoisePollutionMap = map3,
			m_GroundPollutionMap = map,
			m_HappinessParameters = singleton,
			m_City = m_CitySystem.City,
			m_Results = m_Results
		}, m_HouseholdQuery, JobHandle.CombineDependencies(job, base.Dependency)).Complete();
		int num = m_Results[0];
		int num2 = m_Results[4];
		int num3 = m_Results[2];
		int num4 = m_Results[1];
		int num5 = m_Results[5];
		int num6 = m_Results[3];
		int num7 = m_Results[6];
		int num8 = m_Results[7];
		int num9 = ((num4 > 0) ? (num / num4) : 0);
		int num10 = ((num6 > 0) ? (num3 / num6) : 0);
		int num11 = ((num5 > 0) ? (num2 / num5) : 0);
		m_AverageGroundPollution.Update(new IndicatorValue(0f, singleton.m_MaxAirAndGroundPollutionBonus, -num9));
		m_AverageAirPollution.Update(new IndicatorValue(0f, singleton.m_MaxAirAndGroundPollutionBonus, -num10));
		m_AverageNoisePollution.Update(new IndicatorValue(0f, singleton.m_MaxNoisePollutionBonus, -num11));
		m_AverageWaterPollution.Update(new IndicatorValue(0f, num7, num8));
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<CitizenHappinessParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_374463591_0 = entityQueryBuilder2.Build(ref state);
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
	public PollutionInfoviewUISystem()
	{
	}
}
