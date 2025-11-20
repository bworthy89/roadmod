using System;
using Colossal.AssetPipeline.Native;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Simulation;

[FormerlySerializedAs("Colossal.Terrain.SnowSystem, Game")]
public class SnowSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	private static class ShaderIDs
	{
		public static int _LoadSourceR16G16_UNorm;

		public static int _LoadSourceR32G32B32A32_SFloat;

		public static int _LoadScale;

		public static int _Result;

		public static int colossal_SnowScale;

		public static int _SnowMap;

		public static int _Terrain;

		public static int _Water;

		public static int _HeightScale;

		public static int _SnowDepth;

		public static int _OldSnowDepth;

		public static int _Timestep;

		public static int _AddMultiplier;

		public static int _MeltMultiplier;

		public static int _AddWaterMultiplier;

		public static int _ElapseWaterMultiplier;

		public static int _Temperature;

		public static int _Rain;

		public static int _Wind;

		public static int _Time;

		public static int _SnowScale;

		public static int _MinHeights;

		public static int _SnowHeightBackdropBuffer;

		public static int _SnowHeightBackdropFinal;

		public static int _SnowBackdropUpdateLerpFactor;

		public static int _SnowHeightBackdropBufferSize;

		public static int _TerrainLod;

		public static int _SunDirection;

		static ShaderIDs()
		{
			_LoadSourceR16G16_UNorm = Shader.PropertyToID("_LoadSourceR16G16_UNorm");
			_LoadSourceR32G32B32A32_SFloat = Shader.PropertyToID("_LoadSourceR32G32B32A32_SFloat");
			_LoadScale = Shader.PropertyToID("_LoadScale");
			_Result = Shader.PropertyToID("_Result");
			colossal_SnowScale = Shader.PropertyToID("colossal_SnowScale");
			_SnowMap = Shader.PropertyToID("_SnowMap");
			_Terrain = Shader.PropertyToID("_Terrain");
			_Water = Shader.PropertyToID("_Water");
			_HeightScale = Shader.PropertyToID("_HeightScale");
			_SnowDepth = _Result;
			_OldSnowDepth = Shader.PropertyToID("_Previous");
			_Timestep = Shader.PropertyToID("_Timestep");
			_AddMultiplier = Shader.PropertyToID("_AddMultiplier");
			_MeltMultiplier = Shader.PropertyToID("_MeltMultiplier");
			_AddWaterMultiplier = Shader.PropertyToID("_AddWaterMultiplier");
			_ElapseWaterMultiplier = Shader.PropertyToID("_ElapseWaterMultiplier");
			_Temperature = Shader.PropertyToID("_Temperature");
			_Rain = Shader.PropertyToID("_Rain");
			_Time = Shader.PropertyToID("_SimTime");
			_Wind = Shader.PropertyToID("_Wind");
			_SnowScale = Shader.PropertyToID("_SnowScale");
			_MinHeights = Shader.PropertyToID("_MinHeights");
			_SnowHeightBackdropBuffer = Shader.PropertyToID("_SnowHeightBackdropBuffer");
			_SnowHeightBackdropFinal = Shader.PropertyToID("_SnowHeightBackdropTextureFinal");
			_SnowBackdropUpdateLerpFactor = Shader.PropertyToID("_SnowBackdropUpdateLerpFactor");
			_SnowHeightBackdropBufferSize = Shader.PropertyToID("_SnowHeightBackdropBufferSize");
			_TerrainLod = Shader.PropertyToID("_TerrainLod");
			_SunDirection = Shader.PropertyToID("_SunDirection");
		}
	}

	public struct ushort2
	{
		public ushort x;

		public ushort y;
	}

	private static ILog log = LogManager.GetLogger("SceneFlow");

	private const int kTexSize = 1024;

	private const int kGroupSizeAddSnow = 16;

	private const int kNumGroupAddSnow = 64;

	private const float kTimeStep = 0.2f;

	private const float kSnowHeightScale = 8f;

	private const float kSnowMeltScale = 1f;

	private const float m_SnowAddConstant = 1E-05f;

	private const float m_WaterAddConstant = 0.1f;

	private const int kSnowHeightBackdropTextureSize = 1024;

	private const float kSnowBackdropUpdateLerpFactor = 0.1f;

	private RenderTexture m_snowHeightBackdropTextureFinal;

	private ComputeBuffer m_snowBackdropBuffer;

	private ComputeBuffer m_MinHeights;

	private RenderTexture[] m_SnowHeights;

	private CommandBuffer m_CommandBuffer;

	private ComputeShader m_SnowUpdateShader;

	private int m_TransferKernel;

	private int m_AddKernel;

	private int m_ResetKernel;

	private int m_LoadKernelR16G16_UNorm;

	private int m_LoadKernelR32G32B32A32_SFloat;

	private int m_UpdateBackdropSnowHeightTextureKernel;

	private int m_ClearBackdropSnowHeightTextureKernel;

	private int m_FinalizeBackdropSnowHeightTextureKernel;

	private TerrainSystem m_TerrainSystem;

	private SimulationSystem m_SimulationSystem;

	private TimeSystem m_TimeSystem;

	private ClimateSystem m_ClimateSystem;

	private WindSimulationSystem m_WindSimulationSystem;

	private PlanetarySystem m_PlanetarySystem;

	private WaterSystem m_WaterSystem;

	public RenderTexture SnowHeightBackdropTexture => m_snowHeightBackdropTextureFinal;

	public int SnowSimSpeed { get; set; }

	public int2 TextureSize => new int2(1024, 1024);

	public bool Loaded => m_SnowUpdateShader != null;

	public ComputeShader m_SnowTransferShader { private get; set; }

	public ComputeShader m_DynamicHeightShader { private get; set; }

	private float4 SnowScaleVector => new float4(8f, 1f, 1f, 1f);

	private int Write { get; set; }

	private int Read => 1 - Write;

	public RenderTexture SnowDepth
	{
		get
		{
			if (m_SnowHeights != null)
			{
				return m_SnowHeights[Read];
			}
			return null;
		}
	}

	public bool IsAsync { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4;
	}

	private void InitShader()
	{
		m_SnowUpdateShader = AssetDatabase.global.resources.shaders.snowUpdate;
		m_ResetKernel = m_SnowUpdateShader.FindKernel("Reset");
		m_LoadKernelR16G16_UNorm = m_SnowUpdateShader.FindKernel("LoadR16G16_UNorm");
		m_LoadKernelR32G32B32A32_SFloat = m_SnowUpdateShader.FindKernel("LoadOldFormatR32G32B32A32_SFloat");
		m_AddKernel = m_SnowUpdateShader.FindKernel("Add");
		m_TransferKernel = m_SnowUpdateShader.FindKernel("Transfer");
		m_UpdateBackdropSnowHeightTextureKernel = m_SnowUpdateShader.FindKernel("UpdateBackdropSnowHeightTexture");
		m_ClearBackdropSnowHeightTextureKernel = m_SnowUpdateShader.FindKernel("ClearBackdropSnowHeightTexture");
		m_FinalizeBackdropSnowHeightTextureKernel = m_SnowUpdateShader.FindKernel("FinalizeBackdropSnowHeightTexture");
	}

	public unsafe void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(1024);
		writer.Write(1024);
		int num = UnsafeUtility.SizeOf(typeof(ushort2));
		NativeArray<byte> output = new NativeArray<byte>(1048576 * num, Allocator.Persistent);
		AsyncGPUReadbackRequest asyncGPUReadbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref output, m_SnowHeights[Read]);
		asyncGPUReadbackRequest.WaitForCompletion();
		if (!asyncGPUReadbackRequest.done)
		{
			log.Warn("Snow request not done after WaitForCompletion");
		}
		if (asyncGPUReadbackRequest.hasError)
		{
			log.Warn("Snow request has error after WaitForCompletion");
		}
		NativeArray<byte> nativeArray = new NativeArray<byte>(output.Length, Allocator.Temp);
		NativeCompression.FilterDataBeforeWrite((IntPtr)output.GetUnsafeReadOnlyPtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, num);
		output.Dispose();
		NativeArray<byte> value = nativeArray;
		writer.Write(value);
		nativeArray.Dispose();
	}

	public void PostDeserialize(Context context)
	{
		if (context.version < Version.snow)
		{
			m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Write]);
			m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
			m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Read]);
			m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
		}
		Shader.SetGlobalTexture(ShaderIDs._SnowMap, SnowDepth);
	}

	public void DebugReset()
	{
		m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Write]);
		m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
		m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Read]);
		m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
	}

	public unsafe void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		reader.Read(out int value2);
		bool flag = true;
		if (value != 1024)
		{
			UnityEngine.Debug.LogWarning("Saved snow width = " + value + ", snow tex width = " + 1024);
			flag = false;
		}
		if (value2 != 1024)
		{
			UnityEngine.Debug.LogWarning("Saved snow height = " + value2 + ", snow tex height = " + 1024);
			flag = false;
		}
		int num = value * value2;
		if (reader.context.version >= Version.snow16bits)
		{
			int num2 = UnsafeUtility.SizeOf(typeof(ushort2));
			NativeArray<ushort2> nativeArray = new NativeArray<ushort2>(num, Allocator.Temp);
			NativeArray<byte> nativeArray2 = new NativeArray<byte>(num * num2, Allocator.Temp);
			NativeArray<byte> value3 = nativeArray2;
			reader.Read(value3);
			NativeCompression.UnfilterDataAfterRead((IntPtr)nativeArray2.GetUnsafePtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray2.Length, num2);
			nativeArray2.Dispose();
			if (flag)
			{
				NativeArray<float2> data = new NativeArray<float2>(num, Allocator.Temp);
				int num3 = 0;
				foreach (ushort2 item in nativeArray)
				{
					data[num3++] = new float2((float)(int)item.x / 65535f, (float)(int)item.y / 65535f);
				}
				ComputeBuffer computeBuffer = new ComputeBuffer(num, UnsafeUtility.SizeOf<float2>(), ComputeBufferType.Default);
				computeBuffer.SetData(data);
				m_CommandBuffer.SetComputeBufferParam(m_SnowUpdateShader, m_LoadKernelR16G16_UNorm, ShaderIDs._LoadSourceR16G16_UNorm, computeBuffer);
				m_CommandBuffer.SetComputeTextureParam(m_SnowUpdateShader, m_LoadKernelR16G16_UNorm, ShaderIDs._Result, m_SnowHeights[Write]);
				m_CommandBuffer.DispatchCompute(m_SnowUpdateShader, m_LoadKernelR16G16_UNorm, 64, 64, 1);
				m_CommandBuffer.SetComputeTextureParam(m_SnowUpdateShader, m_LoadKernelR16G16_UNorm, ShaderIDs._Result, m_SnowHeights[Read]);
				m_CommandBuffer.DispatchCompute(m_SnowUpdateShader, m_LoadKernelR16G16_UNorm, 64, 64, 1);
				m_CommandBuffer.SetComputeBufferParam(m_SnowUpdateShader, m_ClearBackdropSnowHeightTextureKernel, ShaderIDs._SnowHeightBackdropBuffer, m_snowBackdropBuffer);
				m_CommandBuffer.DispatchCompute(m_SnowUpdateShader, m_ClearBackdropSnowHeightTextureKernel, 64, 1, 1);
				AddSnow(m_CommandBuffer);
				SnowTransfer(m_CommandBuffer);
				UpdateSnowBackdropTexture(m_CommandBuffer, 1f);
				Graphics.ExecuteCommandBuffer(m_CommandBuffer);
				computeBuffer.Dispose();
				data.Dispose();
			}
			nativeArray.Dispose();
		}
		else
		{
			NativeArray<float4> nativeArray3 = new NativeArray<float4>(num, Allocator.Temp);
			if (reader.context.version >= Version.terrainWaterSnowCompression)
			{
				NativeArray<byte> nativeArray4 = new NativeArray<byte>(num * 16, Allocator.Temp);
				NativeArray<byte> value4 = nativeArray4;
				reader.Read(value4);
				NativeCompression.UnfilterDataAfterRead((IntPtr)nativeArray4.GetUnsafePtr(), (IntPtr)nativeArray3.GetUnsafePtr(), nativeArray4.Length, 16);
				nativeArray4.Dispose();
			}
			else
			{
				NativeArray<float4> value5 = nativeArray3;
				reader.Read(value5);
			}
			if (flag)
			{
				ComputeBuffer computeBuffer2 = new ComputeBuffer(num, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Default);
				computeBuffer2.SetData(nativeArray3);
				m_SnowUpdateShader.SetVector(ShaderIDs._LoadScale, SnowScaleVector);
				m_SnowUpdateShader.SetBuffer(m_LoadKernelR32G32B32A32_SFloat, ShaderIDs._LoadSourceR32G32B32A32_SFloat, computeBuffer2);
				m_SnowUpdateShader.SetTexture(m_LoadKernelR32G32B32A32_SFloat, ShaderIDs._Result, m_SnowHeights[Write]);
				m_SnowUpdateShader.Dispatch(m_LoadKernelR32G32B32A32_SFloat, 64, 64, 1);
				m_SnowUpdateShader.SetTexture(m_LoadKernelR32G32B32A32_SFloat, ShaderIDs._Result, m_SnowHeights[Read]);
				m_SnowUpdateShader.Dispatch(m_LoadKernelR32G32B32A32_SFloat, 64, 64, 1);
				computeBuffer2.Dispose();
			}
			nativeArray3.Dispose();
		}
		Shader.SetGlobalVector(ShaderIDs.colossal_SnowScale, SnowScaleVector);
	}

	public void SetDefaults(Context context)
	{
		m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Write]);
		m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
		m_SnowUpdateShader.SetTexture(m_ResetKernel, ShaderIDs._Result, m_SnowHeights[Read]);
		m_SnowUpdateShader.Dispatch(m_ResetKernel, 64, 64, 1);
		Shader.SetGlobalTexture(ShaderIDs._SnowMap, SnowDepth);
	}

	public void UpdateDynamicHeights()
	{
	}

	private RenderTexture CreateTexture(string name)
	{
		RenderTexture renderTexture = new RenderTexture(1024, 1024, 0, GraphicsFormat.R16G16_UNorm);
		renderTexture.name = name;
		renderTexture.hideFlags = HideFlags.DontSave;
		renderTexture.enableRandomWrite = true;
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.Create();
		return renderTexture;
	}

	private void InitTextures()
	{
		m_SnowHeights = new RenderTexture[2];
		m_SnowHeights[0] = CreateTexture("SnowRT0");
		m_SnowHeights[1] = CreateTexture("SnowRT1");
		m_MinHeights = new ComputeBuffer(4096, UnsafeUtility.SizeOf<float2>(), ComputeBufferType.Default);
		m_snowBackdropBuffer = new ComputeBuffer(1024, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Default);
		m_snowHeightBackdropTextureFinal = new RenderTexture(1024, 1, 0, GraphicsFormat.R32_SFloat)
		{
			name = "SnowBackdropHeightTextureFinal",
			hideFlags = HideFlags.DontSave,
			enableRandomWrite = true,
			wrapMode = TextureWrapMode.Clamp,
			filterMode = FilterMode.Bilinear
		};
		m_snowHeightBackdropTextureFinal.Create();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		InitShader();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		InitTextures();
		RequireForUpdate<TerrainPropertiesData>();
		m_CommandBuffer = new CommandBuffer();
		m_CommandBuffer.name = "Snowsystem";
		SnowSimSpeed = 1;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_CommandBuffer.Dispose();
		CoreUtils.Destroy(m_SnowHeights[0]);
		CoreUtils.Destroy(m_SnowHeights[1]);
		m_MinHeights.Release();
		m_snowBackdropBuffer.Release();
		CoreUtils.Destroy(m_snowHeightBackdropTextureFinal);
	}

	private void FlipSnow()
	{
		Write = 1 - Write;
	}

	private float GetSnowiness()
	{
		return Mathf.Sin(MathF.PI * 40f * m_TimeSystem.normalizedDate);
	}

	private void AddSnow(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.AddSnow)))
		{
			PlanetarySystem.LightData sunLight = m_PlanetarySystem.SunLight;
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._Timestep, 0.2f);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._AddMultiplier, 1E-05f);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._MeltMultiplier, 2E-05f);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._AddWaterMultiplier, 0.1f);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._ElapseWaterMultiplier, 0.05f);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._Temperature, m_ClimateSystem.temperature);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._Rain, m_ClimateSystem.precipitation);
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._Wind, new float4(m_WindSimulationSystem.constantWind, 0f, 0f));
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._SnowScale, SnowScaleVector);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._Time, m_TimeSystem.normalizedTime);
			cmd.SetComputeIntParam(m_SnowUpdateShader, ShaderIDs._TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._SunDirection, (sunLight.transform != null) ? sunLight.transform.forward : Vector3.down);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_AddKernel, ShaderIDs._Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._HeightScale, new float4(m_TerrainSystem.heightScaleOffset, m_ClimateSystem.temperatureBaseHeight, m_ClimateSystem.snowTemperatureHeightScale));
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_AddKernel, ShaderIDs._OldSnowDepth, m_SnowHeights[Read]);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_AddKernel, ShaderIDs._SnowDepth, m_SnowHeights[Write]);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_AddKernel, ShaderIDs._Water, m_WaterSystem.WaterTexture);
			cmd.SetComputeBufferParam(m_SnowUpdateShader, m_AddKernel, ShaderIDs._MinHeights, m_MinHeights);
			cmd.DispatchCompute(m_SnowUpdateShader, m_AddKernel, 64, 64, 1);
		}
		FlipSnow();
	}

	private void SnowTransfer(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.TransferSnow)))
		{
			if ((float)m_ClimateSystem.precipitation < 0.1f || (float)m_ClimateSystem.temperature - 0.01f * (m_TerrainSystem.heightScaleOffset.x - m_ClimateSystem.temperatureBaseHeight) > 0f)
			{
				return;
			}
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._SnowScale, SnowScaleVector);
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._HeightScale, new float4(m_TerrainSystem.heightScaleOffset, m_ClimateSystem.temperatureBaseHeight, 0f));
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_TransferKernel, ShaderIDs._OldSnowDepth, m_SnowHeights[Read]);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_TransferKernel, ShaderIDs._SnowDepth, m_SnowHeights[Write]);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_TransferKernel, ShaderIDs._Terrain, m_TerrainSystem.heightmap);
			cmd.SetComputeVectorParam(m_SnowUpdateShader, ShaderIDs._Wind, new float4(m_WindSimulationSystem.constantWind, 0f, 0f));
			cmd.DispatchCompute(m_SnowUpdateShader, m_TransferKernel, 64, 64, 1);
		}
		FlipSnow();
	}

	private void UpdateSnowBackdropTexture(CommandBuffer cmd, float lerpFactor)
	{
		using (new ProfilingScope(m_CommandBuffer, ProfilingSampler.Get(ProfileId.UpdateSnowHeightBackdrop)))
		{
			cmd.SetComputeBufferParam(m_SnowUpdateShader, m_ClearBackdropSnowHeightTextureKernel, ShaderIDs._SnowHeightBackdropBuffer, m_snowBackdropBuffer);
			cmd.DispatchCompute(m_SnowUpdateShader, m_ClearBackdropSnowHeightTextureKernel, 64, 1, 1);
			cmd.SetComputeBufferParam(m_SnowUpdateShader, m_UpdateBackdropSnowHeightTextureKernel, ShaderIDs._SnowHeightBackdropBuffer, m_snowBackdropBuffer);
			cmd.SetComputeBufferParam(m_SnowUpdateShader, m_UpdateBackdropSnowHeightTextureKernel, ShaderIDs._MinHeights, m_MinHeights);
			cmd.SetComputeIntParam(m_SnowUpdateShader, ShaderIDs._SnowHeightBackdropBufferSize, 1024);
			cmd.DispatchCompute(m_SnowUpdateShader, m_UpdateBackdropSnowHeightTextureKernel, 256, 1, 1);
			cmd.SetComputeBufferParam(m_SnowUpdateShader, m_FinalizeBackdropSnowHeightTextureKernel, ShaderIDs._SnowHeightBackdropBuffer, m_snowBackdropBuffer);
			cmd.SetComputeTextureParam(m_SnowUpdateShader, m_FinalizeBackdropSnowHeightTextureKernel, ShaderIDs._SnowHeightBackdropFinal, m_snowHeightBackdropTextureFinal);
			cmd.SetComputeFloatParam(m_SnowUpdateShader, ShaderIDs._SnowBackdropUpdateLerpFactor, lerpFactor);
			cmd.DispatchCompute(m_SnowUpdateShader, m_FinalizeBackdropSnowHeightTextureKernel, 1, 1, 1);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_WaterSystem.Loaded)
		{
			m_CommandBuffer.Clear();
			for (int i = 0; i < SnowSimSpeed; i++)
			{
				AddSnow(m_CommandBuffer);
				SnowTransfer(m_CommandBuffer);
			}
			UpdateSnowBackdropTexture(m_CommandBuffer, 0.1f);
			Shader.SetGlobalTexture(ShaderIDs._SnowMap, SnowDepth);
			Graphics.ExecuteCommandBuffer(m_CommandBuffer);
		}
	}

	[Preserve]
	public SnowSystem()
	{
	}
}
