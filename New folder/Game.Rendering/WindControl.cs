using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Rendering;

public class WindControl
{
	private struct SampledParameter<T>
	{
		public T current;

		public T previous;

		public void Reset(T value)
		{
			previous = value;
			current = value;
		}

		public void Update(T value)
		{
			previous = current;
			current = value;
		}
	}

	private static WindControl s_Instance;

	private ShaderVariablesWind m_ShaderVariablesWindCB;

	private static readonly int m_ShaderVariablesWind = Shader.PropertyToID("ShaderVariablesWind");

	private SampledParameter<float> _WindBaseStrengthPhase;

	private SampledParameter<float> _WindBaseStrengthPhase2;

	private SampledParameter<float> _WindTreeBaseStrengthPhase;

	private SampledParameter<float> _WindTreeBaseStrengthPhase2;

	private SampledParameter<float> _WindBaseStrengthVariancePeriod;

	private SampledParameter<float> _WindTreeBaseStrengthVariancePeriod;

	private SampledParameter<float> _WindGustStrengthPhase;

	private SampledParameter<float> _WindGustStrengthPhase2;

	private SampledParameter<float> _WindTreeGustStrengthPhase;

	private SampledParameter<float> _WindTreeGustStrengthPhase2;

	private SampledParameter<float> _WindGustStrengthVariancePeriod;

	private SampledParameter<float> _WindTreeGustStrengthVariancePeriod;

	private SampledParameter<float> _WindFlutterGustVariancePeriod;

	private SampledParameter<float> _WindTreeFlutterGustVariancePeriod;

	private float _LastParametersSamplingTime;

	private static readonly float3 kForward = new float3(0f, 0f, 1f);

	public static WindControl instance
	{
		get
		{
			if (s_Instance == null)
			{
				s_Instance = new WindControl();
			}
			return s_Instance;
		}
	}

	private WindControl()
	{
		RenderPipelineManager.beginCameraRendering += SetupGPUData;
	}

	public void Dispose()
	{
		RenderPipelineManager.beginCameraRendering -= SetupGPUData;
		s_Instance = null;
	}

	private bool GetWindComponent(Camera camera, out WindVolumeComponent component)
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			Camera main = Camera.main;
			if (main == null)
			{
				component = null;
				return false;
			}
			camera = main;
		}
		HDCamera orCreate = HDCamera.GetOrCreate(camera);
		component = orCreate.volumeStack.GetComponent<WindVolumeComponent>();
		return true;
	}

	private void SetupGPUData(ScriptableRenderContext context, Camera camera)
	{
		if (GetWindComponent(camera, out var component))
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get("");
			UpdateCPUData(component);
			SetGlobalProperties(commandBuffer, component);
			context.ExecuteCommandBuffer(commandBuffer);
			context.Submit();
			commandBuffer.Clear();
			CommandBufferPool.Release(commandBuffer);
		}
	}

	private void UpdateCPUData(WindVolumeComponent wind)
	{
		if (Time.time - _LastParametersSamplingTime > wind.windParameterInterpolationDuration.value)
		{
			_LastParametersSamplingTime = Time.time;
			_WindBaseStrengthPhase.Update(wind.windBaseStrengthPhase.value);
			_WindBaseStrengthPhase2.Update(wind.windBaseStrengthPhase2.value);
			_WindTreeBaseStrengthPhase.Update(wind.windTreeBaseStrengthPhase.value);
			_WindTreeBaseStrengthPhase2.Update(wind.windTreeBaseStrengthPhase2.value);
			_WindBaseStrengthVariancePeriod.Update(wind.windBaseStrengthVariancePeriod.value);
			_WindTreeBaseStrengthVariancePeriod.Update(wind.windTreeBaseStrengthVariancePeriod.value);
			_WindGustStrengthPhase.Update(wind.windGustStrengthPhase.value);
			_WindGustStrengthPhase2.Update(wind.windGustStrengthPhase2.value);
			_WindTreeGustStrengthPhase.Update(wind.windTreeGustStrengthPhase.value);
			_WindTreeGustStrengthPhase2.Update(wind.windTreeGustStrengthPhase2.value);
			_WindGustStrengthVariancePeriod.Update(wind.windGustStrengthVariancePeriod.value);
			_WindTreeGustStrengthVariancePeriod.Update(wind.windTreeGustStrengthVariancePeriod.value);
			_WindFlutterGustVariancePeriod.Update(wind.windFlutterGustVariancePeriod.value);
			_WindTreeFlutterGustVariancePeriod.Update(wind.windTreeFlutterGustVariancePeriod.value);
		}
	}

	private void SetGlobalProperties(CommandBuffer cmd, WindVolumeComponent wind)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.WindGlobalProperties)))
		{
			float num = (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
			float y = math.radians(wind.windDirection.value) + math.cos(MathF.PI * 2f * num / wind.windDirectionVariancePeriod.value) * math.radians(wind.windDirectionVariance.value);
			float3 xyz = math.mul(quaternion.Euler(0f, y, 0f), kForward);
			float3 xyz2 = math.mul(quaternion.Euler(0f, wind.windDirection.value, 0f), kForward);
			float num2 = wind.windGustStrengthControl.value.Evaluate(num);
			float num3 = wind.windTreeGustStrengthControl.value.Evaluate(num);
			float4 c = new float4(xyz, 1f);
			float4 c2 = new float4(xyz2, num);
			float4 zero = float4.zero;
			zero.w = math.min(1f, (Time.time - _LastParametersSamplingTime) / wind.windParameterInterpolationDuration.value);
			float4 c3 = new float4(_WindBaseStrengthPhase.previous, _WindBaseStrengthPhase2.previous, _WindBaseStrengthPhase.current, _WindBaseStrengthPhase2.current);
			m_ShaderVariablesWindCB._WindData_0 = math.transpose(new float4x4(c, c2, zero, c3));
			float4 c4 = new float4(wind.windBaseStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windBaseStrengthOffset.value, wind.windTreeBaseStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windTreeBaseStrengthOffset.value);
			float4 c5 = new float4(0f, wind.windGustStrength.value * num2 * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windGustStrengthOffset.value, _WindFlutterGustVariancePeriod.current);
			float4 c6 = new float4(_WindGustStrengthVariancePeriod.current, _WindGustStrengthVariancePeriod.previous, wind.windGustInnerCosScale.value, wind.windFlutterStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value);
			float4 c7 = new float4(wind.windFlutterGustStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windFlutterGustStrengthOffset.value, wind.windFlutterGustStrengthScale.value, _WindFlutterGustVariancePeriod.previous);
			m_ShaderVariablesWindCB._WindData_1 = math.transpose(new float4x4(c4, c5, c6, c7));
			float4 c8 = new float4(_WindTreeBaseStrengthPhase.previous, _WindTreeBaseStrengthPhase2.previous, _WindTreeBaseStrengthPhase.current, _WindTreeBaseStrengthPhase2.current);
			float4 c9 = new float4(0f, wind.windTreeGustStrength.value * num3 * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windTreeGustStrengthOffset.value, _WindTreeFlutterGustVariancePeriod.current);
			float4 c10 = new float4(_WindTreeGustStrengthVariancePeriod.current, _WindTreeGustStrengthVariancePeriod.previous, wind.windTreeGustInnerCosScale.value, wind.windTreeFlutterStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value);
			float4 c11 = new float4(wind.windTreeFlutterGustStrength.value * wind.windGlobalStrengthScale.value * wind.windGlobalStrengthScale2.value, wind.windTreeFlutterGustStrengthOffset.value, wind.windTreeFlutterGustStrengthScale.value, _WindTreeFlutterGustVariancePeriod.previous);
			m_ShaderVariablesWindCB._WindData_2 = math.transpose(new float4x4(c8, c9, c10, c11));
			float4 c12 = new float4(_WindBaseStrengthVariancePeriod.previous, _WindTreeBaseStrengthVariancePeriod.previous, _WindBaseStrengthVariancePeriod.previous, _WindTreeBaseStrengthVariancePeriod.current);
			float4 c13 = new float4(_WindGustStrengthPhase.previous, _WindGustStrengthPhase2.previous, _WindGustStrengthPhase.current, _WindGustStrengthPhase2.current);
			float4 c14 = new float4(_WindTreeGustStrengthPhase.previous, _WindTreeGustStrengthPhase2.previous, _WindTreeGustStrengthPhase.current, _WindTreeGustStrengthPhase2.current);
			float4 c15 = new float4(0f, 0f, 0f, 0f);
			m_ShaderVariablesWindCB._WindData_3 = math.transpose(new float4x4(c12, c13, c14, c15));
			ConstantBuffer.PushGlobal(cmd, in m_ShaderVariablesWindCB, m_ShaderVariablesWind);
		}
	}
}
