using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WeatherHazardSystem : GameSystemBase
{
	[BurstCompile]
	private struct WeatherHazardJob : IJobChunk
	{
		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public float m_TimeDelta;

		[ReadOnly]
		public float m_Temperature;

		[ReadOnly]
		public float m_Rain;

		[ReadOnly]
		public float m_Cloudiness;

		[ReadOnly]
		public bool m_NaturalDisasters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_PrefabEventType;

		[ReadOnly]
		public ComponentTypeHandle<WeatherPhenomenonData> m_PrefabWeatherPhenomenonType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<EventData> nativeArray2 = chunk.GetNativeArray(ref m_PrefabEventType);
			NativeArray<WeatherPhenomenonData> nativeArray3 = chunk.GetNativeArray(ref m_PrefabWeatherPhenomenonType);
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_LockedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (enabledMask.EnableBit.IsValid && enabledMask[i])
				{
					continue;
				}
				WeatherPhenomenonData weatherPhenomenonData = nativeArray3[i];
				if (weatherPhenomenonData.m_DamageSeverity != 0f && !m_NaturalDisasters)
				{
					continue;
				}
				float num = MathUtils.Center(weatherPhenomenonData.m_OccurenceTemperature);
				float num2 = math.max(0.5f, MathUtils.Extents(weatherPhenomenonData.m_OccurenceTemperature));
				float num3 = (m_Temperature - num) / num2;
				num3 = math.max(0f, 1f - num3 * num3);
				float num4 = 1f;
				if (weatherPhenomenonData.m_OccurenceRain == new Bounds1(0f, 0f))
				{
					num4 = 1f;
				}
				else if (weatherPhenomenonData.m_OccurenceRain.max > 0.999f)
				{
					if (weatherPhenomenonData.m_OccurenceRain.min >= 0.001f)
					{
						num4 = math.saturate((m_Rain - weatherPhenomenonData.m_OccurenceRain.min) / math.max(0.001f, weatherPhenomenonData.m_OccurenceRain.max - weatherPhenomenonData.m_OccurenceRain.min));
					}
				}
				else if (weatherPhenomenonData.m_OccurenceRain.min < 0.001f)
				{
					num4 = math.saturate((weatherPhenomenonData.m_OccurenceRain.max - m_Rain) / math.max(0.001f, weatherPhenomenonData.m_OccurenceRain.max - weatherPhenomenonData.m_OccurenceRain.min));
				}
				float num5 = 1f;
				if (weatherPhenomenonData.m_OccurenceCloudiness == new Bounds1(0f, 0f))
				{
					num5 = 1f;
				}
				else if (weatherPhenomenonData.m_OccurenceCloudiness.max > 0.999f)
				{
					if (weatherPhenomenonData.m_OccurenceCloudiness.min >= 0.001f)
					{
						num5 = math.saturate((m_Cloudiness - weatherPhenomenonData.m_OccurenceCloudiness.min) / math.max(0.001f, weatherPhenomenonData.m_OccurenceCloudiness.max - weatherPhenomenonData.m_OccurenceCloudiness.min));
					}
				}
				else if (weatherPhenomenonData.m_OccurenceCloudiness.min < 0.001f)
				{
					num5 = math.saturate((weatherPhenomenonData.m_OccurenceCloudiness.max - m_Cloudiness) / math.max(0.001f, weatherPhenomenonData.m_OccurenceCloudiness.max - weatherPhenomenonData.m_OccurenceCloudiness.min));
				}
				float num6 = weatherPhenomenonData.m_OccurenceProbability * num3 * num4 * num5 * m_TimeDelta;
				while (random.NextFloat(100f) < num6)
				{
					Entity eventPrefab = nativeArray[i];
					EventData eventData = nativeArray2[i];
					CreateWeatherEvent(unfilteredChunkIndex, eventPrefab, eventData);
					num6 -= 100f;
				}
			}
		}

		private void CreateWeatherEvent(int jobIndex, Entity eventPrefab, EventData eventData)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WeatherPhenomenonData> __Game_Prefabs_WeatherPhenomenonData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_WeatherPhenomenonData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WeatherPhenomenonData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
		}
	}

	private const int UPDATES_PER_DAY = 128;

	private ClimateSystem m_ClimateSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_PhenomenonQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 2048;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PhenomenonQuery = GetEntityQuery(ComponentType.ReadOnly<EventData>(), ComponentType.ReadOnly<WeatherPhenomenonData>(), ComponentType.Exclude<Locked>());
		RequireForUpdate(m_PhenomenonQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		WeatherHazardJob jobData = new WeatherHazardJob
		{
			m_RandomSeed = RandomSeed.Next(),
			m_TimeDelta = 34.133335f,
			m_Temperature = m_ClimateSystem.temperature,
			m_Rain = m_ClimateSystem.precipitation,
			m_Cloudiness = m_ClimateSystem.cloudiness,
			m_NaturalDisasters = m_CityConfigurationSystem.naturalDisasters,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabWeatherPhenomenonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WeatherPhenomenonData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PhenomenonQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public WeatherHazardSystem()
	{
	}
}
