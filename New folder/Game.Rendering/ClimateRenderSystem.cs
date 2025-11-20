using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Audio;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Rendering.Climate;
using Game.Rendering.Utilities;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace Game.Rendering;

[CompilerGenerated]
public class ClimateRenderSystem : GameSystemBase
{
	private static class VFXIDs
	{
		public static readonly int CameraPosition = Shader.PropertyToID("CameraPosition");

		public static readonly int CameraDirection = Shader.PropertyToID("CameraDirection");

		public static readonly int VolumeScale = Shader.PropertyToID("VolumeScale");

		public static readonly int WindTexture = Shader.PropertyToID("WindTexture");

		public static readonly int CloudsAltitude = Shader.PropertyToID("CloudsAltitude");

		public static readonly int MapOffsetScale = Shader.PropertyToID("MapOffsetScale");

		public static readonly int RainStrength = Shader.PropertyToID("RainStrength");

		public static readonly int SnowStrength = Shader.PropertyToID("SnowStrength");

		public static readonly int LightningOrigin = Shader.PropertyToID("LightningOrigin");

		public static readonly int LightningTarget = Shader.PropertyToID("LightningTarget");
	}

	private enum PrecipitationType
	{
		Rain,
		Snow,
		Hail
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private RenderingSystem m_RenderingSystem;

	private ClimateSystem m_ClimateSystem;

	private SimulationSystem m_SimulationSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private PrefabSystem m_PrefabSystem;

	private WindTextureSystem m_WindTextureSystem;

	private TerrainSystem m_TerrainSystem;

	private TimeSystem m_TimeSystem;

	private AudioManager m_AudioManager;

	public bool globalEffectTimeStepFromSimulation;

	public bool weatherEffectTimeStepFromSimulation = true;

	private static VisualEffectAsset s_PrecipitationVFXAsset;

	private VisualEffect m_PrecipitationVFX;

	private static VisualEffectAsset s_LightningVFXAsset;

	private VisualEffect m_LightningVFX;

	private Volume m_ClimateControlVolume;

	private VolumetricClouds m_VolumetricClouds;

	private WindVolumeComponent m_Wind;

	private WindControl m_WindControl;

	private bool m_IsRaining;

	private bool m_IsSnowing;

	private bool m_HailStorm;

	private EntityQuery m_EventQuery;

	private NativeQueue<LightningStrike> m_LightningStrikeQueue;

	private JobHandle m_LightningStrikeDeps;

	private WeatherPropertiesStack m_PropertiesStack;

	private readonly List<WeatherPrefab> m_FromWeatherPrefabs = new List<WeatherPrefab>();

	private readonly List<WeatherPrefab> m_ToWeatherPrefabs = new List<WeatherPrefab>();

	private bool m_PropertiesChanged;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_100321765_0;

	private EntityQuery __query_100321765_1;

	public float precipitationVolumeScale { get; set; } = 30f;

	public bool editMode { get; set; }

	public bool pauseSimulationOnLightning { get; set; }

	internal WeatherPropertiesStack propertiesStack => m_PropertiesStack;

	public IReadOnlyList<WeatherPrefab> fromWeatherPrefabs => m_FromWeatherPrefabs;

	public IReadOnlyList<WeatherPrefab> toWeatherPrefabs => m_ToWeatherPrefabs;

	public bool IsAsync { get; set; }

	private void SetData(WeatherPropertiesStack stack, IReadOnlyList<WeatherPrefab> fromPrefab, IReadOnlyList<WeatherPrefab> toPrefab)
	{
		for (int i = 0; i < fromPrefab.Count; i++)
		{
			foreach (OverrideablePropertiesComponent overrideableProperty in fromPrefab[i].overrideableProperties)
			{
				if (overrideableProperty.active && !overrideableProperty.hasTimeBasedInterpolation)
				{
					stack.SetFrom(overrideableProperty.GetType(), overrideableProperty);
				}
			}
		}
		for (int j = 0; j < toPrefab.Count; j++)
		{
			WeatherPrefab weatherPrefab = toPrefab[j];
			foreach (OverrideablePropertiesComponent overrideableProperty2 in weatherPrefab.overrideableProperties)
			{
				if (overrideableProperty2.active)
				{
					if (overrideableProperty2.hasTimeBasedInterpolation)
					{
						stack.SetTarget(overrideableProperty2.GetType(), overrideableProperty2);
					}
					else
					{
						stack.SetTo(overrideableProperty2.GetType(), overrideableProperty2, j == 1, new Bounds1(weatherPrefab.m_CloudinessRange));
					}
				}
			}
		}
	}

	public void Clear()
	{
		m_PropertiesChanged = true;
		m_FromWeatherPrefabs.Clear();
		m_ToWeatherPrefabs.Clear();
	}

	public void ScheduleFrom(WeatherPrefab prefab)
	{
		m_FromWeatherPrefabs.Add(prefab);
	}

	public void ScheduleTo(WeatherPrefab prefab)
	{
		m_ToWeatherPrefabs.Add(prefab);
	}

	private float GetTimeOfYear()
	{
		if (m_ClimateSystem.currentDate.overrideState)
		{
			return m_ClimateSystem.currentDate.overrideValue;
		}
		if (__query_100321765_0.TryGetSingleton<TimeSettingsData>(out var value) && __query_100321765_1.TryGetSingleton<TimeData>(out var value2))
		{
			double renderingFrame = (float)(m_RenderingSystem.frameIndex - value2.m_FirstFrame) + m_RenderingSystem.frameTime;
			return m_TimeSystem.GetTimeOfYear(value, value2, renderingFrame);
		}
		return 0.5f;
	}

	private void UpdateWeather()
	{
		float timeOfYear = GetTimeOfYear();
		ClimateSystem.ClimateSample sample = m_ClimateSystem.SampleClimate(timeOfYear);
		if (m_PropertiesChanged)
		{
			SetData(m_PropertiesStack, m_FromWeatherPrefabs, m_ToWeatherPrefabs);
			m_PropertiesChanged = false;
		}
		float renderingDeltaTime = m_RenderingSystem.frameDelta / 60f;
		float deltaTime = base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime;
		m_PropertiesStack.InterpolateOverrideData(deltaTime, renderingDeltaTime, sample, editMode);
	}

	private void UpdateEffectsState()
	{
		if (m_ClimateSystem.isPrecipitating && m_ClimateSystem.hail < 0.001f)
		{
			if ((float)m_ClimateSystem.temperature > 0f)
			{
				if (m_IsSnowing)
				{
					UpdateEffectState(PrecipitationType.Snow, start: false);
					m_IsSnowing = false;
				}
				if (!m_IsRaining)
				{
					UpdateEffectState(PrecipitationType.Rain, start: true);
					m_IsRaining = true;
				}
			}
			else
			{
				if (!m_IsSnowing)
				{
					UpdateEffectState(PrecipitationType.Snow, start: true);
					m_IsSnowing = true;
				}
				if (m_IsRaining)
				{
					UpdateEffectState(PrecipitationType.Rain, start: false);
					m_IsRaining = false;
				}
			}
		}
		else
		{
			if (m_IsRaining)
			{
				UpdateEffectState(PrecipitationType.Rain, start: false);
				m_IsRaining = false;
			}
			if (m_IsSnowing)
			{
				UpdateEffectState(PrecipitationType.Snow, start: false);
				m_IsSnowing = false;
			}
		}
		if (m_HailStorm && m_ClimateSystem.hail <= 0.001f)
		{
			UpdateEffectState(PrecipitationType.Hail, start: false);
			m_HailStorm = false;
		}
		else if (!m_HailStorm && m_ClimateSystem.hail > 0.001f)
		{
			UpdateEffectState(PrecipitationType.Hail, start: true);
			m_HailStorm = true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_WindTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_ClimateControlVolume = VolumeHelper.CreateVolume("ClimateControlVolume", 50);
		m_PropertiesStack = new WeatherPropertiesStack(m_ClimateControlVolume);
		VolumeHelper.GetOrCreateVolumeComponent(m_ClimateControlVolume, ref m_VolumetricClouds);
		VolumeHelper.GetOrCreateVolumeComponent(m_ClimateControlVolume, ref m_Wind);
		m_WindControl = WindControl.instance;
		ResetOverrides();
		s_PrecipitationVFXAsset = Resources.Load<VisualEffectAsset>("Precipitation/PrecipitationVFX");
		s_LightningVFXAsset = Resources.Load<VisualEffectAsset>("Lightning/LightningBolt");
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Events.WeatherPhenomenon>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_LightningStrikeQueue = new NativeQueue<LightningStrike>(Allocator.Persistent);
	}

	private void ResetOverrides()
	{
		m_VolumetricClouds.SetAllOverridesTo(state: false);
	}

	public NativeQueue<LightningStrike> GetLightningStrikeQueue(out JobHandle dependencies)
	{
		dependencies = m_LightningStrikeDeps;
		return m_LightningStrikeQueue;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_CameraUpdateSystem.activeViewer != null)
		{
			UpdateWeather();
			UpdateVolumetricClouds();
			CreateDynamicVFXIfNeeded();
			UpdateEffectsState();
			UpdateEffectsProperties();
			UpdateVFXSpeed();
		}
		NativeArray<Entity> nativeArray = m_EventQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (base.EntityManager.GetComponentData<Game.Events.WeatherPhenomenon>(entity).m_Intensity != 0f)
				{
					base.EntityManager.GetComponentData<InterpolatedTransform>(entity);
					PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(entity);
					m_PrefabSystem.GetPrefab<EventPrefab>(componentData).GetComponent<Game.Prefabs.WeatherPhenomenon>();
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		m_LightningStrikeDeps.Complete();
		LightningStrike item;
		while (m_LightningStrikeQueue.TryDequeue(out item))
		{
			LightningStrike(item.m_Position, item.m_Position);
		}
	}

	public void AddLightningStrikeWriter(JobHandle jobHandle)
	{
		m_LightningStrikeDeps = jobHandle;
	}

	private void UpdateVolumetricClouds()
	{
		float num = 1f + (1f - math.abs(math.dot(m_CameraUpdateSystem.direction, new float3(0f, 1f, 0f))));
		m_VolumetricClouds.fadeInMode.Override(VolumetricClouds.CloudFadeInMode.Manual);
		m_VolumetricClouds.fadeInStart.Override(math.max((m_CameraUpdateSystem.position.y - m_VolumetricClouds.bottomAltitude.value) * num, m_CameraUpdateSystem.nearClipPlane));
		m_VolumetricClouds.fadeInDistance.Override(m_VolumetricClouds.altitudeRange.value * 0.3f);
		m_VolumetricClouds.renderHook.Override((!(m_CameraUpdateSystem.position.y < m_VolumetricClouds.bottomAltitude.value)) ? VolumetricClouds.CloudHook.PostTransparent : VolumetricClouds.CloudHook.PreTransparent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LightningStrikeDeps.Complete();
		m_LightningStrikeQueue.Dispose();
		m_WindControl.Dispose();
		m_PropertiesStack.Dispose();
		VolumeHelper.DestroyVolume(m_ClimateControlVolume);
		base.OnDestroy();
	}

	private void CreateDynamicVFXIfNeeded()
	{
		if (s_PrecipitationVFXAsset != null && m_PrecipitationVFX == null)
		{
			COSystemBase.baseLog.DebugFormat("Creating VFXs pool");
			m_PrecipitationVFX = new GameObject("PrecipitationVFX").AddComponent<VisualEffect>();
			m_PrecipitationVFX.visualEffectAsset = s_PrecipitationVFXAsset;
			m_LightningVFX = new GameObject("LightningVFX").AddComponent<VisualEffect>();
			m_LightningVFX.visualEffectAsset = s_LightningVFXAsset;
		}
	}

	public void LightningStrike(float3 start, float3 target, bool useCloudsAltitude = true)
	{
		if (pauseSimulationOnLightning)
		{
			m_SimulationSystem.selectedSpeed = 0f;
		}
		if (useCloudsAltitude)
		{
			start.y = m_VolumetricClouds.bottomAltitude.value + m_VolumetricClouds.altitudeRange.value * 0.1f;
		}
		COSystemBase.baseLog.DebugFormat("Lightning strike {0}->{1}", start, target);
		m_LightningVFX.SetVector3(VFXIDs.LightningOrigin, start);
		m_LightningVFX.SetVector3(VFXIDs.LightningTarget, target);
		m_LightningVFX.SendEvent("OnPlay");
		m_AudioManager.PlayLightningSFX((start - target) / 2f);
	}

	private bool GetEventName(PrecipitationType type, bool start, out string name)
	{
		switch (type)
		{
		case PrecipitationType.Rain:
			name = (start ? "OnRainStart" : "OnRainStop");
			return true;
		case PrecipitationType.Snow:
			name = (start ? "OnSnowStart" : "OnSnowStop");
			return true;
		case PrecipitationType.Hail:
			name = (start ? "OnHailStart" : "OnHailStop");
			return true;
		default:
			name = null;
			return false;
		}
	}

	private void UpdateEffectState(PrecipitationType type, bool start)
	{
		if (GetEventName(type, start, out var name))
		{
			COSystemBase.baseLog.DebugFormat("PrecipitationVFX event {0}", name);
			m_PrecipitationVFX.SendEvent(name);
		}
	}

	private void UpdateEffectsProperties()
	{
		m_PrecipitationVFX.SetCheckedVector3(VFXIDs.CameraPosition, m_CameraUpdateSystem.position);
		m_PrecipitationVFX.SetCheckedVector3(VFXIDs.CameraDirection, m_CameraUpdateSystem.direction);
		m_PrecipitationVFX.SetCheckedVector3(VFXIDs.VolumeScale, new Vector3(precipitationVolumeScale, precipitationVolumeScale, precipitationVolumeScale));
		m_PrecipitationVFX.SetCheckedTexture(VFXIDs.WindTexture, m_WindTextureSystem.WindTexture);
		m_PrecipitationVFX.SetCheckedFloat(VFXIDs.CloudsAltitude, m_VolumetricClouds.bottomAltitude.value);
		m_PrecipitationVFX.SetCheckedVector4(VFXIDs.MapOffsetScale, m_TerrainSystem.mapOffsetScale);
		m_PrecipitationVFX.SetCheckedFloat(VFXIDs.RainStrength, m_ClimateSystem.precipitation);
		m_PrecipitationVFX.SetCheckedFloat(VFXIDs.SnowStrength, m_ClimateSystem.precipitation);
	}

	private void UpdateVFXSpeed()
	{
		if (globalEffectTimeStepFromSimulation)
		{
			float num = m_RenderingSystem.frameDelta / 60f;
			float smoothSpeed = m_SimulationSystem.smoothSpeed;
			VFXManager.fixedTimeStep = num * smoothSpeed;
			UnityEngine.Debug.Log("smoothedRenderTimeStep: " + num + " simulationSpeedMultiplier: " + smoothSpeed);
		}
		else
		{
			float num2 = m_RenderingSystem.frameDelta / math.max(1E-06f, base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime * 60f);
			m_PrecipitationVFX.playRate = (weatherEffectTimeStepFromSimulation ? num2 : 1f);
			m_LightningVFX.playRate = (weatherEffectTimeStepFromSimulation ? num2 : 1f);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeSettingsData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_100321765_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_100321765_1 = entityQueryBuilder2.Build(ref state);
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
	public ClimateRenderSystem()
	{
	}
}
