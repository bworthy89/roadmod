using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Objects;
using Game.Prefabs;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PollutionTriggerSystem : GameSystemBase
{
	[BurstCompile]
	private struct CalculateAverageAirPollutionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		public CitizenHappinessParameterData m_HappinessParameters;

		public Entity m_City;

		public NativeAccumulator<AverageFloat>.ParallelWriter m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			NativeArray<PropertyRenter> nativeArray = chunk.GetNativeArray(ref m_PropertyRenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				int2 airPollutionBonuses = CitizenHappinessSystem.GetAirPollutionBonuses(nativeArray[i].m_Property, ref m_Transforms, m_AirPollutionMap, cityModifiers, in m_HappinessParameters);
				m_Result.Accumulate(new AverageFloat
				{
					m_Total = math.csum(airPollutionBonuses),
					m_Count = 1
				});
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SendPollutionTriggerJob : IJob
	{
		public NativeAccumulator<AverageFloat> m_Result;

		public NativeQueue<TriggerAction> m_TriggerQueue;

		public void Execute()
		{
			float average = m_Result.GetResult().average;
			m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.AverageAirPollution, Entity.Null, average));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private AirPollutionSystem m_AirPollutionSystem;

	private TriggerSystem m_TriggerSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_HappinessParameterQuery;

	private NativeArray<float> m_AirPollutionResult;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_380796347_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
		m_AirPollutionResult = new NativeArray<float>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_AirPollutionResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeAccumulator<AverageFloat> result = new NativeAccumulator<AverageFloat>(Allocator.TempJob);
		JobHandle dependencies;
		CalculateAverageAirPollutionJob jobData = new CalculateAverageAirPollutionJob
		{
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_HappinessParameters = __query_380796347_0.GetSingleton<CitizenHappinessParameterData>(),
			m_City = m_CitySystem.City,
			m_Result = result.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_AirPollutionSystem.AddReader(base.Dependency);
		SendPollutionTriggerJob jobData2 = new SendPollutionTriggerJob
		{
			m_Result = result,
			m_TriggerQueue = m_TriggerSystem.CreateActionBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		result.Dispose(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<CitizenHappinessParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_380796347_0 = entityQueryBuilder2.Build(ref state);
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
	public PollutionTriggerSystem()
	{
	}
}
