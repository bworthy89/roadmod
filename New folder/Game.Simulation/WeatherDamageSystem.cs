using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
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
public class WeatherDamageSystem : GameSystemBase
{
	[BurstCompile]
	private struct WeatherDamageJob : IJobChunk
	{
		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public EntityArchetype m_DestroyEventArchetype;

		[ReadOnly]
		public DisasterConfigurationData m_DisasterConfigurationData;

		[ReadOnly]
		public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

		[ReadOnly]
		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		public ComponentTypeHandle<FacingWeather> m_FacingWeatherType;

		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentLookup<Game.Events.WeatherPhenomenon> m_WeatherPhenomenonData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<WeatherPhenomenonData> m_PrefabWeatherPhenomenonData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			float num = 1.0666667f;
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<FacingWeather> nativeArray3 = chunk.GetNativeArray(ref m_FacingWeatherType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			NativeArray<Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
			bool flag = chunk.Has(ref m_BuildingType);
			bool flag2 = chunk.Has(ref m_DestroyedType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				FacingWeather value = nativeArray3[i];
				Transform transform = nativeArray5[i];
				if (!flag2 && m_WeatherPhenomenonData.HasComponent(value.m_Event))
				{
					Game.Events.WeatherPhenomenon weatherPhenomenon = m_WeatherPhenomenonData[value.m_Event];
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Event];
					WeatherPhenomenonData weatherPhenomenonData = m_PrefabWeatherPhenomenonData[prefabRef2.m_Prefab];
					value.m_Severity = EventUtils.GetSeverity(transform.m_Position, weatherPhenomenon, weatherPhenomenonData);
				}
				else
				{
					value.m_Severity = 0f;
				}
				if (value.m_Severity > 0f)
				{
					float structuralIntegrity = m_StructuralIntegrityData.GetStructuralIntegrity(prefabRef.m_Prefab, flag);
					float value2 = value.m_Severity / structuralIntegrity;
					if (structuralIntegrity >= 100000000f)
					{
						value.m_Severity = 0f;
						value2 = 0f;
					}
					else
					{
						if (flag)
						{
							DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
							CityUtils.ApplyModifier(ref value2, modifiers, CityModifierType.DisasterDamageRate);
						}
						value2 = math.min(0.5f, value2 * num);
					}
					if (value2 > 0f)
					{
						if (nativeArray4.Length != 0)
						{
							Damaged damaged = nativeArray4[i];
							damaged.m_Damage.x = math.min(1f, damaged.m_Damage.x + value2);
							if (!flag2 && ObjectUtils.GetTotalDamage(damaged) == 1f)
							{
								Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DestroyEventArchetype);
								m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Destroy(entity, value.m_Event));
								m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
								m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
								m_IconCommandBuffer.Add(entity, m_DisasterConfigurationData.m_WeatherDestroyedNotificationPrefab, IconPriority.FatalProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, value.m_Event);
								value.m_Severity = 0f;
							}
							nativeArray4[i] = damaged;
						}
						else
						{
							Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DamageEventArchetype);
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new Damage(entity, new float3(value2, 0f, 0f)));
						}
					}
				}
				if (value.m_Severity > 0f)
				{
					m_IconCommandBuffer.Add(entity, m_DisasterConfigurationData.m_WeatherDamageNotificationPrefab, (value.m_Severity >= 30f) ? IconPriority.MajorProblem : IconPriority.Problem, IconClusterLayer.Default, IconFlags.IgnoreTarget, value.m_Event);
				}
				else
				{
					m_CommandBuffer.RemoveComponent<FacingWeather>(unfilteredChunkIndex, entity);
					m_IconCommandBuffer.Remove(entity, m_DisasterConfigurationData.m_WeatherDamageNotificationPrefab);
				}
				nativeArray3[i] = value;
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		public ComponentTypeHandle<FacingWeather> __Game_Events_FacingWeather_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Events.WeatherPhenomenon> __Game_Events_WeatherPhenomenon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WeatherPhenomenonData> __Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Events_FacingWeather_RW_ComponentTypeHandle = state.GetComponentTypeHandle<FacingWeather>();
			__Game_Objects_Damaged_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>();
			__Game_Events_WeatherPhenomenon_RO_ComponentLookup = state.GetComponentLookup<Game.Events.WeatherPhenomenon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup = state.GetComponentLookup<WeatherPhenomenonData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private IconCommandSystem m_IconCommandSystem;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_FacingQuery;

	private EntityQuery m_FireConfigQuery;

	private EntityQuery m_DisasterConfigQuery;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_DestroyEventArchetype;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_FacingQuery = GetEntityQuery(ComponentType.ReadWrite<FacingWeather>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_FireConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_DisasterConfigQuery = GetEntityQuery(ComponentType.ReadOnly<DisasterConfigurationData>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Damage>());
		m_DestroyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Destroy>());
		m_StructuralIntegrityData = new EventHelpers.StructuralIntegrityData(this);
		RequireForUpdate(m_FacingQuery);
		RequireForUpdate(m_FireConfigQuery);
		RequireForUpdate(m_DisasterConfigQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FireConfigurationData singleton = m_FireConfigQuery.GetSingleton<FireConfigurationData>();
		DisasterConfigurationData singleton2 = m_DisasterConfigQuery.GetSingleton<DisasterConfigurationData>();
		m_StructuralIntegrityData.Update(this, singleton);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WeatherDamageJob
		{
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_DestroyEventArchetype = m_DestroyEventArchetype,
			m_DisasterConfigurationData = singleton2,
			m_StructuralIntegrityData = m_StructuralIntegrityData,
			m_City = m_CitySystem.City,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FacingWeatherType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_FacingWeather_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WeatherPhenomenonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_WeatherPhenomenon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWeatherPhenomenonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef)
		}, m_FacingQuery, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public WeatherDamageSystem()
	{
	}
}
