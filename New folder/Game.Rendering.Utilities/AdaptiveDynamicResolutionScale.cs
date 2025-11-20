using System;
using Game.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Rendering.Utilities;

public class AdaptiveDynamicResolutionScale
{
	public enum DynResUpscaleFilter
	{
		CatmullRom,
		ContrastAdaptiveSharpen,
		EdgeAdaptiveScaling,
		TAAU
	}

	private static AdaptiveDynamicResolutionScale s_Instance;

	public float DefaultTargetFrameRate = 30f;

	public int EvaluationFrameCount = 15;

	public uint ScaleUpDuration = 4u;

	public uint ScaleDownDuration = 2u;

	public int ScaleUpStepCount = 5;

	public int ScaleDownStepCount = 2;

	private const uint InitialFramesToSkip = 1u;

	private float m_AccumGPUFrameTime;

	private float m_GPULimitedFrames;

	private int m_CurrentFrameSlot;

	private uint m_ScaleUpCounter;

	private uint m_ScaleDownCounter;

	private static float s_CurrentScaleFraction = 1f;

	private bool m_Initialized;

	private uint m_InitialFrameCounter;

	private float m_AvgGPUTime;

	private float m_AvgGPULimited;

	public static AdaptiveDynamicResolutionScale instance
	{
		get
		{
			if (s_Instance == null)
			{
				s_Instance = new AdaptiveDynamicResolutionScale();
			}
			return s_Instance;
		}
	}

	public DynResUpscaleFilter upscaleFilter { get; private set; }

	public bool isEnabled { get; private set; }

	public bool isAdaptive { get; private set; }

	public float minScale { get; private set; } = 0.5f;

	public float currentScale => s_CurrentScaleFraction;

	public string debugState
	{
		get
		{
			if (!isAdaptive)
			{
				return $"Scale {currentScale:F2}";
			}
			return $"Scale {currentScale:F2} GPU lim {m_AvgGPULimited:P1} dur {m_AvgGPUTime:F1}ms";
		}
	}

	public void SetParams(bool enabled, bool adaptive, float minScale, DynResUpscaleFilter filter, Camera camera)
	{
		isEnabled = enabled;
		isAdaptive = adaptive;
		this.minScale = minScale;
		upscaleFilter = filter;
		if (camera != null)
		{
			if (!SharedSettings.instance.graphics.isDlssActive && !SharedSettings.instance.graphics.isFsr2Active)
			{
				HDAdditionalCameraData component = camera.GetComponent<HDAdditionalCameraData>();
				component.allowDeepLearningSuperSampling = false;
				component.allowFidelityFX2SuperResolution = false;
				DynamicResolutionHandler.SetUpscaleFilter(camera, (!enabled) ? DynamicResUpscaleFilter.CatmullRom : GetFilterFromUiEnum(filter));
			}
			else
			{
				DynamicResolutionHandler.ClearSelectedCamera();
			}
		}
	}

	private static DynamicResUpscaleFilter GetFilterFromUiEnum(DynResUpscaleFilter filter)
	{
		return filter switch
		{
			DynResUpscaleFilter.CatmullRom => DynamicResUpscaleFilter.CatmullRom, 
			DynResUpscaleFilter.EdgeAdaptiveScaling => DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres, 
			DynResUpscaleFilter.ContrastAdaptiveSharpen => DynamicResUpscaleFilter.TAAU, 
			DynResUpscaleFilter.TAAU => DynamicResUpscaleFilter.ContrastAdaptiveSharpen, 
			_ => throw new NotSupportedException($"{filter} is not a supported upscaler"), 
		};
	}

	private static bool IsGpuBottleneck(float fullFrameTime, float mainThreadCpuTime, float renderThreadCpuTime, float gpuTime)
	{
		if (gpuTime == 0f || mainThreadCpuTime == 0f)
		{
			return false;
		}
		float num = fullFrameTime * 0.8f;
		if (gpuTime > num && mainThreadCpuTime < num)
		{
			return renderThreadCpuTime < num;
		}
		return false;
	}

	public void UpdateDRS(float fullFrameTime, float mainThreadCpuTime, float renderThreadCpuTime, float gpuTime)
	{
		if (!FrameTimingManager.IsFeatureEnabled())
		{
			return;
		}
		if (!m_Initialized)
		{
			if (m_InitialFrameCounter >= 1)
			{
				DynamicResolutionHandler.SetDynamicResScaler(() => s_CurrentScaleFraction * 100f, DynamicResScalePolicyType.ReturnsPercentage);
				m_Initialized = true;
			}
			else
			{
				m_InitialFrameCounter++;
			}
		}
		if (!m_Initialized)
		{
			return;
		}
		if (!isEnabled)
		{
			s_CurrentScaleFraction = 1f;
			return;
		}
		if (!isAdaptive)
		{
			s_CurrentScaleFraction = minScale;
			return;
		}
		m_AccumGPUFrameTime += gpuTime;
		m_GPULimitedFrames += (IsGpuBottleneck(fullFrameTime, mainThreadCpuTime, renderThreadCpuTime, gpuTime) ? 1 : 0);
		m_CurrentFrameSlot++;
		if (m_CurrentFrameSlot != EvaluationFrameCount)
		{
			return;
		}
		m_AvgGPUTime = m_AccumGPUFrameTime / (float)EvaluationFrameCount;
		m_AvgGPULimited = m_GPULimitedFrames / (float)EvaluationFrameCount;
		float defaultTargetFrameRate = DefaultTargetFrameRate;
		if (1000f / defaultTargetFrameRate - m_AvgGPUTime < 0f && m_AvgGPULimited > 0.3f)
		{
			m_ScaleUpCounter = 0u;
			m_ScaleDownCounter++;
			if (m_ScaleDownCounter >= ScaleDownDuration)
			{
				m_ScaleDownCounter = 0u;
				s_CurrentScaleFraction -= (1f - minScale) / (float)ScaleDownStepCount;
				s_CurrentScaleFraction = math.clamp(s_CurrentScaleFraction, minScale, 1f);
			}
		}
		else
		{
			m_ScaleDownCounter = 0u;
			m_ScaleUpCounter++;
			if (m_ScaleUpCounter >= ScaleUpDuration)
			{
				m_ScaleUpCounter = 0u;
				s_CurrentScaleFraction += (1f - minScale) / (float)ScaleUpStepCount;
				s_CurrentScaleFraction = math.clamp(s_CurrentScaleFraction, minScale, 1f);
			}
		}
		m_AccumGPUFrameTime = 0f;
		m_GPULimitedFrames = 0f;
		m_CurrentFrameSlot = 0;
	}

	private static void ResetScale()
	{
		s_CurrentScaleFraction = 1f;
	}

	public static void Dispose()
	{
		ResetScale();
		s_Instance = null;
	}
}
