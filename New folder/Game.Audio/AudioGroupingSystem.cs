using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Debug;
using Game.Effects;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Reflection;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Audio;

[CompilerGenerated]
public class AudioGroupingSystem : GameSystemBase
{
	[BurstCompile]
	private struct AudioGroupingJob : IJob
	{
		public ComponentLookup<EffectInstance> m_EffectInstances;

		[ReadOnly]
		public ComponentLookup<EffectData> m_EffectDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public NativeArray<TrafficAmbienceCell> m_TrafficAmbienceMap;

		[ReadOnly]
		public NativeArray<ZoneAmbienceCell> m_AmbienceMap;

		[ReadOnly]
		public NativeArray<AudioGroupingSettingsData> m_Settings;

		public SourceUpdateData m_SourceUpdateData;

		public EffectFlagSystem.EffectFlagData m_EffectFlagData;

		public float3 m_CameraPosition;

		public NativeArray<Entity> m_AmbienceEntities;

		public NativeArray<Entity> m_NearAmbienceEntities;

		public NativeArray<float> m_CurrentValues;

		[DeallocateOnJobCompletion]
		public NativeArray<Entity> m_OnFireTrees;

		[ReadOnly]
		public TerrainHeightData m_TerrainData;

		[ReadOnly]
		public float m_ForestFireDistance;

		[ReadOnly]
		public float m_Precipitation;

		[ReadOnly]
		public bool m_IsRaining;

		public void Execute()
		{
			float3 @float = m_CameraPosition;
			float num = TerrainUtils.SampleHeight(ref m_TerrainData, m_CameraPosition);
			m_CameraPosition.y -= num;
			for (int i = 0; i < m_AmbienceEntities.Length; i++)
			{
				Entity entity = m_AmbienceEntities[i];
				Entity entity2 = m_NearAmbienceEntities[i];
				AudioGroupingSettingsData audioGroupingSettingsData = m_Settings[i];
				if (!m_EffectInstances.HasComponent(entity))
				{
					continue;
				}
				float num2 = 0f;
				float num3 = 0f;
				switch (audioGroupingSettingsData.m_Type)
				{
				case GroupAmbienceType.Traffic:
					num2 = TrafficAmbienceSystem.GetTrafficAmbience2(m_CameraPosition, m_TrafficAmbienceMap, 1f / audioGroupingSettingsData.m_Scale).m_Traffic;
					break;
				case GroupAmbienceType.Forest:
				case GroupAmbienceType.NightForest:
				{
					GroupAmbienceType groupAmbienceType = (m_EffectFlagData.m_IsNightTime ? GroupAmbienceType.NightForest : GroupAmbienceType.Forest);
					if (audioGroupingSettingsData.m_Type == groupAmbienceType && !IsNearForestOnFire(@float))
					{
						num2 = ZoneAmbienceSystem.GetZoneAmbience(GroupAmbienceType.Forest, m_CameraPosition, m_AmbienceMap, 1f / m_Settings[i].m_Scale);
						if (entity2 != Entity.Null)
						{
							num3 = ZoneAmbienceSystem.GetZoneAmbienceNear(GroupAmbienceType.Forest, m_CameraPosition, m_AmbienceMap, m_Settings[i].m_NearWeight, 1f / m_Settings[i].m_Scale);
						}
					}
					break;
				}
				case GroupAmbienceType.Rain:
					if (m_IsRaining)
					{
						num2 = math.min(1f / audioGroupingSettingsData.m_Scale, math.max(0f, m_Precipitation) * 2f);
						num3 = num2;
					}
					break;
				default:
					num2 = ZoneAmbienceSystem.GetZoneAmbience(audioGroupingSettingsData.m_Type, m_CameraPosition, m_AmbienceMap, 1f / audioGroupingSettingsData.m_Scale);
					if (entity2 != Entity.Null)
					{
						num3 = ZoneAmbienceSystem.GetZoneAmbienceNear(audioGroupingSettingsData.m_Type, m_CameraPosition, m_AmbienceMap, m_Settings[i].m_NearWeight, 1f / audioGroupingSettingsData.m_Scale);
					}
					break;
				}
				m_CurrentValues[(int)audioGroupingSettingsData.m_Type] = num2;
				bool flag = true;
				Entity prefab = m_PrefabRefs[entity].m_Prefab;
				bool flag2 = (m_EffectDatas[prefab].m_Flags.m_RequiredFlags & EffectConditionFlags.Cold) != 0;
				bool flag3 = (m_EffectDatas[prefab].m_Flags.m_ForbiddenFlags & EffectConditionFlags.Cold) != 0;
				if (flag2 || flag3)
				{
					bool isColdSeason = m_EffectFlagData.m_IsColdSeason;
					flag = (flag2 && isColdSeason) || (flag3 && !isColdSeason);
				}
				if (num2 > 0.001f && flag)
				{
					EffectInstance value = m_EffectInstances[entity];
					float num4 = math.saturate(audioGroupingSettingsData.m_Scale * num2);
					num4 *= math.saturate((audioGroupingSettingsData.m_Height.y - m_CameraPosition.y) / (audioGroupingSettingsData.m_Height.y - audioGroupingSettingsData.m_Height.x));
					num4 = math.lerp(value.m_Intensity, num4, audioGroupingSettingsData.m_FadeSpeed);
					value.m_Position = @float;
					value.m_Rotation = quaternion.identity;
					value.m_Intensity = math.saturate(num4);
					m_EffectInstances[entity] = value;
					m_SourceUpdateData.Add(entity, new Game.Objects.Transform
					{
						m_Position = @float,
						m_Rotation = quaternion.identity
					});
				}
				else
				{
					if (m_EffectInstances.HasComponent(entity))
					{
						EffectInstance value2 = m_EffectInstances[entity];
						value2.m_Intensity = 0f;
						m_EffectInstances[entity] = value2;
					}
					m_SourceUpdateData.Remove(entity);
				}
				flag = true;
				if (entity2 != Entity.Null)
				{
					prefab = m_PrefabRefs[entity2].m_Prefab;
					flag2 = (m_EffectDatas[prefab].m_Flags.m_RequiredFlags & EffectConditionFlags.Cold) != 0;
					flag3 = (m_EffectDatas[prefab].m_Flags.m_ForbiddenFlags & EffectConditionFlags.Cold) != 0;
					if (flag2 || flag3)
					{
						bool isColdSeason2 = m_EffectFlagData.m_IsColdSeason;
						flag = (flag2 && isColdSeason2) || (flag3 && !isColdSeason2);
					}
				}
				if (num3 > 0.001f && flag)
				{
					EffectInstance value3 = m_EffectInstances[entity2];
					float num5 = math.saturate(audioGroupingSettingsData.m_Scale * num3);
					num5 *= math.saturate((audioGroupingSettingsData.m_NearHeight.y - m_CameraPosition.y) / (audioGroupingSettingsData.m_NearHeight.y - audioGroupingSettingsData.m_NearHeight.x));
					num5 = math.lerp(value3.m_Intensity, num5, audioGroupingSettingsData.m_FadeSpeed);
					value3.m_Position = @float;
					value3.m_Rotation = quaternion.identity;
					value3.m_Intensity = math.saturate(num5);
					m_EffectInstances[entity2] = value3;
					m_SourceUpdateData.Add(entity2, new Game.Objects.Transform
					{
						m_Position = @float,
						m_Rotation = quaternion.identity
					});
				}
				else
				{
					if (m_EffectInstances.HasComponent(entity2))
					{
						EffectInstance value4 = m_EffectInstances[entity2];
						value4.m_Intensity = 0f;
						m_EffectInstances[entity2] = value4;
					}
					m_SourceUpdateData.Remove(entity2);
				}
			}
		}

		private bool IsNearForestOnFire(float3 cameraPosition)
		{
			for (int i = 0; i < m_OnFireTrees.Length; i++)
			{
				Entity entity = m_OnFireTrees[i];
				if (m_TransformData.HasComponent(entity) && math.distancesq(m_TransformData[entity].m_Position, cameraPosition) < m_ForestFireDistance * m_ForestFireDistance)
				{
					return true;
				}
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<EffectInstance> __Game_Effects_EffectInstance_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Effects_EffectInstance_RW_ComponentLookup = state.GetComponentLookup<EffectInstance>();
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
		}
	}

	private TrafficAmbienceSystem m_TrafficAmbienceSystem;

	private ZoneAmbienceSystem m_ZoneAmbienceSystem;

	private EffectFlagSystem m_EffectFlagSystem;

	private SimulationSystem m_SimulationSystem;

	private ClimateSystem m_ClimateSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_AudioGroupingConfigurationQuery;

	private EntityQuery m_AudioGroupingMiscSettingQuery;

	private NativeArray<Entity> m_AmbienceEntities;

	private NativeArray<Entity> m_NearAmbienceEntities;

	private NativeArray<AudioGroupingSettingsData> m_Settings;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_OnFireTreeQuery;

	[EnumArray(typeof(GroupAmbienceType))]
	[DebugWatchValue]
	private NativeArray<float> m_CurrentValues;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_TrafficAmbienceSystem = base.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>();
		m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_EffectFlagSystem = base.World.GetOrCreateSystemManaged<EffectFlagSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CurrentValues = new NativeArray<float>(24, Allocator.Persistent);
		m_AudioGroupingConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<AudioGroupingSettingsData>());
		m_AudioGroupingMiscSettingQuery = GetEntityQuery(ComponentType.ReadOnly<AudioGroupingMiscSetting>());
		m_OnFireTreeQuery = GetEntityQuery(ComponentType.ReadOnly<Tree>(), ComponentType.ReadOnly<OnFire>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_AudioGroupingConfigurationQuery);
	}

	private Entity CreateEffect(Entity sfx)
	{
		Entity entity = base.EntityManager.CreateEntity();
		base.EntityManager.AddComponentData(entity, default(EffectInstance));
		base.EntityManager.AddComponentData(entity, new PrefabRef
		{
			m_Prefab = sfx
		});
		return entity;
	}

	private void Initialize()
	{
		NativeArray<Entity> nativeArray = m_AudioGroupingConfigurationQuery.ToEntityArray(Allocator.Temp);
		List<AudioGroupingSettingsData> list = new List<AudioGroupingSettingsData>();
		foreach (Entity item in nativeArray)
		{
			list.AddRange(base.World.EntityManager.GetBuffer<AudioGroupingSettingsData>(item, isReadOnly: true).AsNativeArray());
		}
		if (!m_Settings.IsCreated)
		{
			m_Settings = list.ToNativeArray(Allocator.Persistent);
		}
		nativeArray.Dispose();
		if (!m_AmbienceEntities.IsCreated)
		{
			m_AmbienceEntities = new NativeArray<Entity>(m_Settings.Length, Allocator.Persistent);
		}
		if (!m_NearAmbienceEntities.IsCreated)
		{
			m_NearAmbienceEntities = new NativeArray<Entity>(m_Settings.Length, Allocator.Persistent);
		}
		for (int i = 0; i < m_Settings.Length; i++)
		{
			m_AmbienceEntities[i] = CreateEffect(m_Settings[i].m_GroupSoundFar);
			m_NearAmbienceEntities[i] = ((m_Settings[i].m_GroupSoundNear != Entity.Null) ? CreateEffect(m_Settings[i].m_GroupSoundNear) : Entity.Null);
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_AmbienceEntities.IsCreated)
		{
			m_AmbienceEntities.Dispose();
		}
		if (m_NearAmbienceEntities.IsCreated)
		{
			m_NearAmbienceEntities.Dispose();
		}
		if (m_Settings.IsCreated)
		{
			m_Settings.Dispose();
		}
		m_CurrentValues.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (GameManager.instance.gameMode == GameMode.Game && !GameManager.instance.isGameLoading)
		{
			if (m_AmbienceEntities.Length == 0 || !base.EntityManager.HasComponent<EffectInstance>(m_AmbienceEntities[0]))
			{
				Initialize();
			}
			Camera main = Camera.main;
			if (!(main == null))
			{
				float3 cameraPosition = main.transform.position;
				AudioGroupingMiscSetting singleton = m_AudioGroupingMiscSettingQuery.GetSingleton<AudioGroupingMiscSetting>();
				JobHandle deps;
				JobHandle dependencies;
				JobHandle dependencies2;
				AudioGroupingJob jobData = new AudioGroupingJob
				{
					m_CameraPosition = cameraPosition,
					m_SourceUpdateData = m_AudioManager.GetSourceUpdateData(out deps),
					m_TrafficAmbienceMap = m_TrafficAmbienceSystem.GetMap(readOnly: true, out dependencies),
					m_AmbienceMap = m_ZoneAmbienceSystem.GetMap(readOnly: true, out dependencies2),
					m_Settings = m_Settings,
					m_EffectFlagData = m_EffectFlagSystem.GetData(),
					m_AmbienceEntities = m_AmbienceEntities,
					m_NearAmbienceEntities = m_NearAmbienceEntities,
					m_OnFireTrees = m_OnFireTreeQuery.ToEntityArray(Allocator.TempJob),
					m_EffectInstances = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup, ref base.CheckedStateRef),
					m_EffectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TerrainData = m_TerrainSystem.GetHeightData(),
					m_ForestFireDistance = singleton.m_ForestFireDistance,
					m_Precipitation = m_ClimateSystem.precipitation,
					m_IsRaining = m_ClimateSystem.isRaining,
					m_CurrentValues = m_CurrentValues
				};
				base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(JobHandle.CombineDependencies(dependencies2, deps), dependencies, base.Dependency));
				m_TerrainSystem.AddCPUHeightReader(base.Dependency);
				m_AudioManager.AddSourceUpdateWriter(base.Dependency);
				m_TrafficAmbienceSystem.AddReader(base.Dependency);
			}
		}
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
	public AudioGroupingSystem()
	{
	}
}
