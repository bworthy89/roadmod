using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Settings;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class RenderingSystem : GameSystemBase, IPostDeserialize
{
	public const string kLoadingTask = "LoadMeshes";

	private SimulationSystem m_SimulationSystem;

	private PlanetarySystem m_PlanetarySystem;

	private UpdateSystem m_UpdateSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private BatchMeshSystem m_BatchMeshSystem;

	private AreaBatchSystem m_AreaBatchSystem;

	private TimeSystem m_TimeSystem;

	private Dictionary<Shader, bool> m_EnabledShaders;

	private EntityQuery m_TimeSettingGroup;

	private EntityQuery m_TimeDataQuery;

	private int m_TotalLoadingCount;

	private int m_EnabledShaderCount;

	private float m_LastFrameOffset;

	private float m_LodTimer;

	private bool m_IsLoading;

	private bool m_EnabledShadersUpdated;

	public uint frameIndex { get; private set; }

	public float frameTime { get; private set; }

	public float frameDelta { get; private set; }

	public float frameLod { get; private set; }

	public float timeOfDay { get; private set; }

	public int lodTimerDelta { get; private set; }

	public float frameOffset { get; set; }

	public bool hideOverlay { get; set; }

	public bool unspawnedVisible { get; set; }

	public bool markersVisible { get; set; }

	public float levelOfDetail { get; set; }

	public bool lodCrossFade { get; set; }

	public int maxLightCount { get; set; }

	public bool debugCrossFade { get; set; }

	public bool disableLodModels { get; set; }

	public float4 editorBuildingStateOverride { get; set; }

	public float loadingProgress
	{
		get
		{
			return TaskManager.instance.GetTaskProgress("LoadMeshes");
		}
		private set
		{
			TaskManager.instance.progress.Report(new ProgressTracker("LoadMeshes", ProgressTracker.Group.Group2)
			{
				progress = value
			});
		}
	}

	public bool motionVectors { get; private set; }

	public IReadOnlyDictionary<Shader, bool> enabledShaders => m_EnabledShaders;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
		m_AreaBatchSystem = base.World.GetOrCreateSystemManaged<AreaBatchSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_EnabledShaders = new Dictionary<Shader, bool>();
		levelOfDetail = 0.5f;
		maxLightCount = 2048;
		m_TimeSettingGroup = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		motionVectors = GetMotionVectorsEnabled();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_UpdateSystem.Update(SystemUpdatePhase.Rendering);
		if (m_IsLoading)
		{
			if (loadingProgress != 1f)
			{
				UpdateLoadingProgress();
			}
			else
			{
				m_IsLoading = false;
			}
		}
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		m_TotalLoadingCount = 0;
		m_IsLoading = true;
		frameLod = 0f;
	}

	private void UpdateLoadingProgress()
	{
		int num = m_BatchMeshSystem.loadingRemaining;
		if (frameLod < levelOfDetail)
		{
			num = math.max(1, (int)((float)num * levelOfDetail / math.max(frameLod, levelOfDetail * 0.01f)));
		}
		m_TotalLoadingCount = math.max(num, m_TotalLoadingCount);
		if (num > 0)
		{
			loadingProgress = math.clamp((float)(m_TotalLoadingCount - num) / (float)m_TotalLoadingCount, 0f, 0.99999f);
			return;
		}
		loadingProgress = 1f;
		m_IsLoading = false;
	}

	public void PrepareRendering()
	{
		int num = 15;
		if (m_LastFrameOffset != frameOffset)
		{
			float num2 = frameOffset * (float)num;
			int num3 = (int)math.floor(num2);
			uint num4 = frameIndex;
			float num5 = frameTime;
			frameIndex = m_SimulationSystem.frameIndex + (uint)num3;
			frameTime = num2 - (float)num3;
			frameDelta = (float)(int)(frameIndex - num4) + (frameTime - num5);
			m_LastFrameOffset = frameOffset;
		}
		else if (m_SimulationSystem.selectedSpeed < 1E-05f)
		{
			frameDelta = 0f;
		}
		else
		{
			int num6 = (int)(frameIndex - m_SimulationSystem.frameIndex);
			float num7 = m_SimulationSystem.frameTime;
			float num8 = (float)num6 + frameTime + UnityEngine.Time.deltaTime * m_SimulationSystem.smoothSpeed * 60f;
			float num9 = num8 - num7;
			float num10 = (float)num * (4f / MathF.PI);
			num9 = math.atan(num9 / num10) * num10;
			num9 = math.clamp(num9, -num, num);
			num8 = num7 + num9;
			num6 = (int)math.floor(num8);
			uint num11 = frameIndex;
			float num12 = frameTime;
			if (num6 < 0 && m_SimulationSystem.frameIndex < (uint)(-num6))
			{
				frameIndex = 0u;
				frameTime = 0f;
			}
			else
			{
				frameIndex = m_SimulationSystem.frameIndex + (uint)num6;
				frameTime = math.saturate(num8 - (float)num6);
			}
			frameDelta = (float)(int)(frameIndex - num11) + (frameTime - num12);
			if (frameDelta < 0f)
			{
				frameIndex = num11;
				frameTime = num12;
				frameDelta = 0f;
			}
			frameOffset = math.clamp((num8 - num7) / (float)num, -1f, 1f);
			m_LastFrameOffset = frameOffset;
		}
		float4 xyxy = (frameIndex % new uint2(60u, 3600u) + new float2(frameTime)).xyxy;
		xyxy *= new float4(1f / 60f, 0.00027777778f, MathF.PI / 30f, 0.0017453294f);
		Shader.SetGlobalVector("colossal_SimulationTime", xyxy);
		float value = (float)(frameIndex % 216000) + frameTime;
		Shader.SetGlobalFloat("colossal_SimulationTime2", value);
		if (m_TimeSettingGroup.TryGetSingleton<TimeSettingsData>(out var value2) && m_TimeDataQuery.TryGetSingleton<TimeData>(out var value3))
		{
			timeOfDay = m_TimeSystem.GetTimeOfDay(value2, value3, (double)(frameIndex - value3.m_FirstFrame) + (double)frameTime);
		}
		else
		{
			timeOfDay = -1f;
		}
		motionVectors = GetMotionVectorsEnabled();
		if (m_BatchManagerSystem.CheckPropertyUpdates())
		{
			frameLod = 0f;
			lodTimerDelta = 255;
		}
		else if (lodCrossFade)
		{
			m_LodTimer += UnityEngine.Time.deltaTime * (debugCrossFade ? 102f : 1020f);
			lodTimerDelta = Mathf.FloorToInt(m_LodTimer);
			m_LodTimer -= lodTimerDelta;
			lodTimerDelta = math.clamp(lodTimerDelta, 0, 255);
		}
		else
		{
			lodTimerDelta = 255;
		}
		frameLod = math.min(frameLod + levelOfDetail * 0.01f, levelOfDetail);
		if (m_EnabledShadersUpdated)
		{
			m_EnabledShadersUpdated = false;
			m_ManagedBatchSystem.EnabledShadersUpdated();
			m_AreaBatchSystem.EnabledShadersUpdated();
		}
	}

	public bool IsShaderEnabled(Shader shader)
	{
		if (m_EnabledShaders.TryGetValue(shader, out var value))
		{
			return value;
		}
		value = m_EnabledShaderCount != 0 || m_EnabledShaders.Count == 0;
		m_EnabledShaderCount += (value ? 1 : 0);
		m_EnabledShaders.Add(shader, value);
		return value;
	}

	public void SetShaderEnabled(Shader shader, bool isEnabled)
	{
		if (IsShaderEnabled(shader) != isEnabled)
		{
			m_EnabledShaderCount += (isEnabled ? 1 : (-1));
			m_EnabledShaders[shader] = isEnabled;
			m_EnabledShadersUpdated = true;
		}
	}

	private bool GetMotionVectorsEnabled()
	{
		return true;
	}

	public void PostDeserialize(Context context)
	{
		frameIndex = m_SimulationSystem.frameIndex;
		frameTime = m_SimulationSystem.frameTime;
		frameDelta = 0f;
		frameOffset = 1f;
		m_LastFrameOffset = 1f;
	}

	public float3 GetShadowCullingData()
	{
		float3 result = new float3(2048f, 1f, 1f);
		SharedSettings instance = SharedSettings.instance;
		if (instance != null && instance.graphics != null)
		{
			ShadowsQualitySettings qualitySetting = instance.graphics.GetQualitySetting<ShadowsQualitySettings>();
			if (qualitySetting != null)
			{
				result.y = qualitySetting.shadowCullingThresholdHeight;
				result.z = qualitySetting.shadowCullingThresholdVolume;
			}
		}
		return result;
	}

	[Preserve]
	public RenderingSystem()
	{
	}
}
