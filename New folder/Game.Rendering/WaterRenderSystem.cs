using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Rendering;

[BurstCompile]
[FormerlySerializedAs("Colossal.Terrain.WaterRenderSystem, Game")]
[CompilerGenerated]
public class WaterRenderSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[Serializable]
	public class WaterMaterialParams : ISerializable
	{
		private const float m_wavesMutiplierMaxDistance = 750f;

		private const float m_wavesMutiplierMaxFadeDistance = 250f;

		private int m_ID_WavesMutiplier = Shader.PropertyToID("_WavesMultiplier");

		private int m_ID_LakeWavesMutiplier = Shader.PropertyToID("_LakeWavesMultiplier");

		private int m_ID_MinWaterAmountForWaves = Shader.PropertyToID("_MinWaterAmountForWaves");

		public float m_seaWindStrength;

		public float m_seaWindDirection;

		public float m_foamAmount;

		public float m_wavesMultiplier;

		public float m_causticsPlaneDistance;

		public float m_causticsIntensity;

		public float m_minWaterAmountForWaves;

		public float m_absorbtionDistance;

		public float m_ripplesWindSpeed;

		public float m_lakeWavesMultiplier;

		public bool m_seaWindAffectWavesDirection;

		public Color m_waterColor;

		public Color m_waterScatteringColor;

		public float m_foamFadeStart;

		public float m_foamFadeDistance;

		private WaterSystem m_WaterSystem;

		private int m_ID_SeaFlowParams = Shader.PropertyToID("colossal_SeaFlowParams");

		public float CameraHeight { get; set; }

		public float SeaWindDirection
		{
			get
			{
				return m_seaWindDirection;
			}
			set
			{
				m_seaWindDirection = value;
				ApplyMaterialParams();
			}
		}

		public float SeaWindStrength
		{
			get
			{
				return m_seaWindStrength;
			}
			set
			{
				m_seaWindStrength = value;
				ApplyMaterialParams();
			}
		}

		private float DistanceWindSpeed { get; set; }

		public WaterMaterialParams()
		{
			SetDefaults();
		}

		public void Init(WaterSurface surface, WaterSystem waterSystem)
		{
			m_foamAmount = surface.simulationFoamAmount;
			m_wavesMultiplier = surface.largeBand0Multiplier;
			m_causticsPlaneDistance = surface.causticsPlaneBlendDistance;
			m_causticsIntensity = surface.causticsIntensity;
			m_absorbtionDistance = surface.absorptionDistance;
			m_ripplesWindSpeed = surface.ripplesWindSpeed;
			m_waterColor = surface.refractionColor;
			m_waterScatteringColor = surface.scatteringColor;
			m_WaterSystem = waterSystem;
			DistanceWindSpeed = surface.largeWindSpeed;
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref float value = ref m_seaWindStrength;
			reader.Read(out value);
			ref float value2 = ref m_seaWindDirection;
			reader.Read(out value2);
			ref float value3 = ref m_foamAmount;
			reader.Read(out value3);
			ref float value4 = ref m_wavesMultiplier;
			reader.Read(out value4);
			ref float value5 = ref m_causticsPlaneDistance;
			reader.Read(out value5);
			ref float value6 = ref m_causticsIntensity;
			reader.Read(out value6);
			ref float value7 = ref m_minWaterAmountForWaves;
			reader.Read(out value7);
			ref float value8 = ref m_absorbtionDistance;
			reader.Read(out value8);
			ref float value9 = ref m_ripplesWindSpeed;
			reader.Read(out value9);
			ref Color value10 = ref m_waterColor;
			reader.Read(out value10);
			ref Color value11 = ref m_waterScatteringColor;
			reader.Read(out value11);
			ref float value12 = ref m_lakeWavesMultiplier;
			reader.Read(out value12);
			ref float value13 = ref m_foamFadeStart;
			reader.Read(out value13);
			ref float value14 = ref m_foamFadeDistance;
			reader.Read(out value14);
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			float value = m_seaWindStrength;
			writer.Write(value);
			float value2 = m_seaWindDirection;
			writer.Write(value2);
			float value3 = m_foamAmount;
			writer.Write(value3);
			float value4 = m_wavesMultiplier;
			writer.Write(value4);
			float value5 = m_causticsPlaneDistance;
			writer.Write(value5);
			float value6 = m_causticsIntensity;
			writer.Write(value6);
			float value7 = m_minWaterAmountForWaves;
			writer.Write(value7);
			float value8 = m_absorbtionDistance;
			writer.Write(value8);
			float value9 = m_ripplesWindSpeed;
			writer.Write(value9);
			Color value10 = m_waterColor;
			writer.Write(value10);
			Color value11 = m_waterScatteringColor;
			writer.Write(value11);
			float value12 = m_lakeWavesMultiplier;
			writer.Write(value12);
			float value13 = m_foamFadeStart;
			writer.Write(value13);
			float value14 = m_foamFadeDistance;
			writer.Write(value14);
		}

		internal void SetDefaults()
		{
			m_waterColor = new Color(0.14f, 0.62f, 0.62f);
			m_waterScatteringColor = new Color(0.19f, 0.33f, 0.39f);
			m_foamAmount = 0.3f;
			m_wavesMultiplier = 1f;
			m_causticsIntensity = 1f;
			m_causticsPlaneDistance = 4f;
			m_minWaterAmountForWaves = 10f;
			m_ripplesWindSpeed = 5f;
			m_absorbtionDistance = 8f;
			m_lakeWavesMultiplier = 1f;
			m_foamFadeStart = 500f;
			m_foamFadeDistance = 1500f;
			m_seaWindAffectWavesDirection = true;
		}

		private float ComputeFoamFade()
		{
			if (m_foamFadeStart == 0f)
			{
				return 1f;
			}
			return 1f - math.saturate((CameraHeight - m_foamFadeStart) / m_foamFadeDistance);
		}

		public void ApplyMaterialParams()
		{
			float num = m_wavesMultiplier;
			float t = math.saturate((CameraHeight - 750f) / 250f);
			if (num > 1f)
			{
				num = math.lerp(m_wavesMultiplier, 1f, t);
			}
			float largeWindSpeed = math.lerp(0f, DistanceWindSpeed, m_wavesMultiplier * m_wavesMultiplier);
			foreach (WaterSurface instance in WaterSurface.instances)
			{
				if ((bool)instance.customMaterial)
				{
					instance.customMaterial.SetFloat(m_ID_WavesMutiplier, num);
					instance.customMaterial.SetFloat(m_ID_LakeWavesMutiplier, m_lakeWavesMultiplier);
					instance.customMaterial.SetFloat(m_ID_MinWaterAmountForWaves, m_minWaterAmountForWaves);
					instance.simulationFoamAmount = m_foamAmount * ComputeFoamFade();
					instance.causticsPlaneBlendDistance = m_causticsPlaneDistance;
					instance.causticsIntensity = m_causticsIntensity;
					instance.refractionColor = m_waterColor;
					instance.scatteringColor = m_waterScatteringColor;
					instance.absorptionDistance = m_absorbtionDistance;
					instance.ripplesWindSpeed = m_ripplesWindSpeed;
					instance.largeBand0Multiplier = math.saturate(num);
					instance.largeBand1Multiplier = math.saturate(num);
					instance.largeChaos = ((SeaWindStrength > 0f && m_seaWindAffectWavesDirection) ? 0.1f : 1f);
					instance.largeWindOrientationValue = SeaWindDirection;
					instance.largeWindSpeed = largeWindSpeed;
				}
			}
			m_WaterSystem.SeaFlowDirection = m_seaWindDirection;
			m_WaterSystem.SeaFlowStrength = m_seaWindStrength;
			Shader.SetGlobalVector(m_ID_SeaFlowParams, m_WaterSystem.GetSeaFlowParams());
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	[BurstCompile]
	public struct RequestDistanceComparer : IComparer<WaterHeightRequest>
	{
		public static readonly RequestDistanceComparer Instance;

		public int Compare(WaterHeightRequest x, WaterHeightRequest y)
		{
			return x.distance.CompareTo(y.distance);
		}
	}

	public delegate void DequeueAndSort_000045A6_0024PostfixBurstDelegate(ref NativeQueue<WaterHeightRequest> requests, ref NativeArray<WaterHeightRequest> outRequests, ref float3 camPosition);

	internal static class DequeueAndSort_000045A6_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(DequeueAndSort_000045A6_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static DequeueAndSort_000045A6_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref NativeQueue<WaterHeightRequest> requests, ref NativeArray<WaterHeightRequest> outRequests, ref float3 camPosition)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativeQueue<WaterHeightRequest>, ref NativeArray<WaterHeightRequest>, ref float3, void>)functionPointer)(ref requests, ref outRequests, ref camPosition);
					return;
				}
			}
			DequeueAndSort_0024BurstManaged(ref requests, ref outRequests, ref camPosition);
		}
	}

	private TerrainRenderSystem m_TerrainRenderSystem;

	private TerrainSystem m_TerrainSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private WaterSystem m_WaterSystem;

	private float3 m_LastCameraLocation;

	private RenderingSystem m_RenderingSystem;

	private ComputeShader m_UtilsShader;

	private const int MAX_WATER_HEIGHT_REQUESTS = 1024;

	public NativeQueue<WaterHeightRequest> m_RequestedPositions;

	private NativeArray<WaterHeightRequest> m_HeightsRequestsResults;

	private ComputeBuffer m_reqVerticesPositionsIn;

	private ComputeBuffer m_reqVerticesPositionsOut;

	private bool m_PendingReadback;

	private AsyncGPUReadbackRequest m_AsyncReadback;

	private NativeArray<WaterHeightRequest> m_CPUTemp;

	private CommandBuffer m_CommandBuffer;

	private int m_VertexPositionComputeKernel;

	private int m_ID_RequestedVerticesIn;

	private int m_ID_RequestedVerticesOut;

	private int m_ID_WavesMultiplier;

	private int m_ID_MinWaterForWaves;

	private int m_ID_LakeWavesMultiplier;

	private JobHandle m_heightsReaders;

	private Volume m_WaterGridControlVolume;

	private WaterRendering m_WaterRendering;

	private const float CAMERA_HEIGHT_TO_CHANGE_WATER_GRID = 3300f;

	private const float WATER_GRID_SIZE_AT_MAX_HEIGHT = 5000f;

	public WaterMaterialParams m_WaterMaterialParams;

	public Texture overrideOverlaymap { get; set; }

	public Texture overlayExtramap { get; set; }

	public float4 overlayPollutionMask { get; set; }

	public float4 overlayArrowMask { get; set; }

	public Texture waterTexture => m_WaterSystem.WaterRenderTexture;

	public Texture flowTexture => m_WaterSystem.FlowTextureUpdated;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		foreach (WaterSurface instance in WaterSurface.instances)
		{
			if (instance.customMaterial != null)
			{
				instance.customMaterial = new Material(instance.customMaterial);
			}
		}
		m_WaterMaterialParams = new WaterMaterialParams();
		using (HashSet<WaterSurface>.Enumerator enumerator = WaterSurface.instances.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				WaterSurface current2 = enumerator.Current;
				m_WaterMaterialParams.Init(current2, m_WaterSystem);
			}
		}
		InitShader();
		m_WaterGridControlVolume = VolumeHelper.CreateVolume("WaterGridControlVolume", 5000);
		VolumeHelper.GetOrCreateVolumeComponent(m_WaterGridControlVolume, ref m_WaterRendering);
		m_WaterRendering.maxGridSize.Override(5000f);
	}

	private void InitShader()
	{
		m_UtilsShader = AssetDatabase.global.resources.shaders.waterRenderUtils;
		m_VertexPositionComputeKernel = m_UtilsShader.FindKernel("VertexPositionCompute");
		m_ID_RequestedVerticesIn = Shader.PropertyToID("RequestedVerticesIn");
		m_ID_RequestedVerticesOut = Shader.PropertyToID("RequestedVerticesOut");
		m_ID_WavesMultiplier = Shader.PropertyToID("WavesMultiplier");
		m_ID_MinWaterForWaves = Shader.PropertyToID("MinWaterForWaves");
		m_ID_LakeWavesMultiplier = Shader.PropertyToID("LakeWavesMultiplier");
		m_CommandBuffer = new CommandBuffer();
		m_RequestedPositions = new NativeQueue<WaterHeightRequest>(Allocator.Persistent);
		m_HeightsRequestsResults = new NativeArray<WaterHeightRequest>(1024, Allocator.Persistent);
		m_reqVerticesPositionsIn = new ComputeBuffer(1024, UnsafeUtility.SizeOf<WaterHeightRequest>(), ComputeBufferType.Default);
		m_reqVerticesPositionsOut = new ComputeBuffer(1024, UnsafeUtility.SizeOf<WaterHeightRequest>(), ComputeBufferType.Default);
		m_CPUTemp = new NativeArray<WaterHeightRequest>(1024, Allocator.Persistent);
	}

	private void ReleaseShader()
	{
		if (!m_AsyncReadback.done)
		{
			m_AsyncReadback.WaitForCompletion();
		}
		if (m_RequestedPositions.IsCreated)
		{
			m_RequestedPositions.Dispose();
		}
		if (m_HeightsRequestsResults.IsCreated)
		{
			m_HeightsRequestsResults.Dispose();
		}
		if (m_CPUTemp.IsCreated)
		{
			m_CPUTemp.Dispose();
		}
		m_reqVerticesPositionsIn?.Release();
		m_reqVerticesPositionsOut?.Release();
		m_CommandBuffer?.Release();
	}

	public void AddHeightReader(JobHandle handle)
	{
		m_heightsReaders = JobHandle.CombineDependencies(m_heightsReaders, handle);
	}

	private void ExecuteReadBack()
	{
		if (!m_PendingReadback)
		{
			m_AsyncReadback = AsyncGPUReadback.RequestIntoNativeArray(ref m_CPUTemp, m_reqVerticesPositionsOut, CopyVerticesValues);
			m_PendingReadback = true;
		}
	}

	private void CopyVerticesValues(AsyncGPUReadbackRequest request)
	{
		m_heightsReaders.Complete();
		m_HeightsRequestsResults.CopyFrom(m_CPUTemp);
		m_PendingReadback = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		foreach (WaterSurface instance in WaterSurface.instances)
		{
			if (instance.customMaterial != null)
			{
				CoreUtils.Destroy(instance.customMaterial);
			}
		}
		ReleaseShader();
		VolumeHelper.DestroyVolume(m_WaterGridControlVolume);
		base.OnDestroy();
	}

	public WaterRenderSurfaceData GetRenderSurfaceData()
	{
		return new WaterRenderSurfaceData(m_HeightsRequestsResults);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TerrainSystem.GetCascadeInfo(out var _, out var baseLOD, out var areas, out var _, out var _);
		foreach (WaterSurface instance in WaterSurface.instances)
		{
			float timeMultiplier = m_RenderingSystem.frameDelta / math.max(1E-06f, base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime * 60f);
			instance.timeMultiplier = timeMultiplier;
			instance.CascadeArea = areas;
			if (baseLOD == 0)
			{
				instance.WaterSimArea = new Vector4(areas.c0.x, areas.c1.x, areas.c2.x - areas.c0.x, areas.c3.x - areas.c1.x);
			}
			else
			{
				instance.WaterSimArea = new Vector4(areas.c0.y, areas.c1.y, areas.c2.y - areas.c0.y, areas.c3.y - areas.c1.y);
			}
			instance.TerrainScaleOffset = m_TerrainSystem.heightScaleOffset;
			instance.TerrainCascadeTexture = m_TerrainSystem.GetCascadeTexture();
			if (m_WaterSystem.Loaded)
			{
				instance.WaterSimulationTexture = m_WaterSystem.WaterTexture;
			}
			else
			{
				instance.WaterSimulationTexture = Texture2D.blackTexture;
			}
			if (!instance.customMaterial)
			{
				continue;
			}
			instance.customMaterial.SetVector(TerrainRenderSystem.ShaderID._OverlayArrowMask, overlayArrowMask);
			instance.customMaterial.SetVector(TerrainRenderSystem.ShaderID._OverlayPollutionMask, overlayPollutionMask);
			if (overrideOverlaymap != null)
			{
				instance.customMaterial.SetTexture(TerrainRenderSystem.ShaderID._BaseColorMap, overrideOverlaymap);
			}
			if (overlayExtramap != null)
			{
				if (overrideOverlaymap == null)
				{
					overrideOverlaymap = Texture2D.whiteTexture;
				}
				if (overlayExtramap == flowTexture)
				{
					instance.customMaterial.SetFloat(TerrainRenderSystem.ShaderID._OverlayArrowSource, 1f);
				}
				else
				{
					instance.customMaterial.SetTexture(TerrainRenderSystem.ShaderID._OverlayExtra, overlayExtramap);
					instance.customMaterial.SetFloat(TerrainRenderSystem.ShaderID._OverlayArrowSource, 0f);
				}
				instance.customMaterial.EnableKeyword("OVERRIDE_OVERLAY_EXTRA");
			}
			else
			{
				instance.customMaterial.DisableKeyword("OVERRIDE_OVERLAY_EXTRA");
			}
		}
		if (m_WaterSystem.Loaded)
		{
			float3 position = m_CameraUpdateSystem.position;
			if (position.y != m_LastCameraLocation.y)
			{
				m_LastCameraLocation = position;
				m_WaterMaterialParams.CameraHeight = m_LastCameraLocation.y - m_WaterSystem.SeaLevel;
				m_WaterMaterialParams.ApplyMaterialParams();
				m_WaterGridControlVolume.weight = ((m_WaterMaterialParams.CameraHeight > 3300f) ? 1f : 0f);
			}
			UpdateWaterVerticesQueries();
		}
	}

	private void UpdateWaterVerticesQueries()
	{
		m_CommandBuffer.Clear();
		if (m_RequestedPositions.Count > 0)
		{
			using (new ProfilingScope(m_CommandBuffer, ProfilingSampler.Get(ProfileId.ComputeWaterDisplacementRequests)))
			{
				int num = math.min(m_RequestedPositions.Count, 1024);
				NativeArray<WaterHeightRequest> outRequests = new NativeArray<WaterHeightRequest>(m_RequestedPositions.Count, Allocator.Temp);
				float3 camPosition = m_CameraUpdateSystem.position;
				DequeueAndSort(ref m_RequestedPositions, ref outRequests, ref camPosition);
				m_reqVerticesPositionsIn.SetData(outRequests, 0, 0, num);
				WaterSurface waterSurface = null;
				using (HashSet<WaterSurface>.Enumerator enumerator = WaterSurface.instances.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						waterSurface = enumerator.Current;
					}
				}
				if (RenderPipelineManager.currentPipeline is HDRenderPipeline hDRenderPipeline && waterSurface != null)
				{
					hDRenderPipeline.SetWaterParametersComputeBuffer(m_CommandBuffer, m_UtilsShader);
					m_CommandBuffer.SetComputeBufferParam(m_UtilsShader, m_VertexPositionComputeKernel, m_ID_RequestedVerticesIn, m_reqVerticesPositionsIn);
					m_CommandBuffer.SetComputeBufferParam(m_UtilsShader, m_VertexPositionComputeKernel, m_ID_RequestedVerticesOut, m_reqVerticesPositionsOut);
					m_CommandBuffer.SetComputeTextureParam(m_UtilsShader, m_VertexPositionComputeKernel, "_WaterDisplacementBuffer", waterSurface.simulation.gpuBuffers.displacementBuffer);
					m_CommandBuffer.SetComputeTextureParam(m_UtilsShader, m_VertexPositionComputeKernel, "_WaterMask", (waterSurface.waterMask != null) ? waterSurface.waterMask : Texture2D.whiteTexture);
					m_CommandBuffer.SetComputeFloatParam(m_UtilsShader, m_ID_WavesMultiplier, m_WaterMaterialParams.m_wavesMultiplier);
					m_CommandBuffer.SetComputeFloatParam(m_UtilsShader, m_ID_MinWaterForWaves, m_WaterMaterialParams.m_minWaterAmountForWaves);
					m_CommandBuffer.SetComputeFloatParam(m_UtilsShader, m_ID_LakeWavesMultiplier, m_WaterMaterialParams.m_lakeWavesMultiplier);
					m_CommandBuffer.DispatchCompute(m_UtilsShader, m_VertexPositionComputeKernel, num, 1, 1);
				}
				outRequests.Dispose();
			}
			Graphics.ExecuteCommandBuffer(m_CommandBuffer);
			ExecuteReadBack();
		}
		m_RequestedPositions.Clear();
	}

	public void SetDefaults(Context context)
	{
		m_WaterMaterialParams.SetDefaults();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		m_WaterMaterialParams.Serialize(writer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.format.Has(FormatTags.NewWaterSources))
		{
			m_WaterMaterialParams.Deserialize(reader);
		}
	}

	[BurstCompile]
	public static void DequeueAndSort(ref NativeQueue<WaterHeightRequest> requests, ref NativeArray<WaterHeightRequest> outRequests, ref float3 camPosition)
	{
		DequeueAndSort_000045A6_0024BurstDirectCall.Invoke(ref requests, ref outRequests, ref camPosition);
	}

	[Preserve]
	public WaterRenderSystem()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	public static void DequeueAndSort_0024BurstManaged(ref NativeQueue<WaterHeightRequest> requests, ref NativeArray<WaterHeightRequest> outRequests, ref float3 camPosition)
	{
		int num = 0;
		WaterHeightRequest item;
		while (requests.TryDequeue(out item))
		{
			item.distance = math.distancesq(item.position, camPosition);
			outRequests[num++] = item;
		}
		outRequests.Sort(RequestDistanceComparer.Instance);
	}
}
