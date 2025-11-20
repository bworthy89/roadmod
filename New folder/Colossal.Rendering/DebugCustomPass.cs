using System.Collections.Generic;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.UI.Debug;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Colossal.Rendering;

public class DebugCustomPass : CustomPass
{
	public enum TextureDebugMode
	{
		None,
		SelectionOutlines,
		HeightMap,
		HeightMapCascades,
		TerrainOverlay,
		SplatMap,
		WaterDepth,
		WaterVelocity,
		WaterPolution,
		SnowAccumulation,
		RainAccumulation,
		WaterRawVelocity,
		Wind,
		TerrainTesselation,
		WaterSurfaceSpectrum,
		WaterSurfaceDisplacement,
		WaterSurfaceGradient,
		WaterSurfaceJacobianSurface,
		WaterSurfaceJacobianDeep,
		WaterSurfaceCaustics
	}

	private const int kPadding = 10;

	public const TextureDebugMode kGlobalMapStart = TextureDebugMode.HeightMap;

	public const TextureDebugMode kGlobalMapEnd = TextureDebugMode.Wind;

	public const TextureDebugMode kWaterSimulationMapStart = TextureDebugMode.WaterSurfaceSpectrum;

	public const TextureDebugMode kWaterSimulationMapEnd = TextureDebugMode.WaterSurfaceCaustics;

	private Material m_DebugBlitMaterial;

	private MaterialPropertyBlock m_MaterialPropertyBlock;

	private ComputeBuffer m_TopViewRenderIndirectArgs;

	private Material m_TopViewMaterial;

	private RTHandle m_TopViewRenderTexture;

	public int activeInstance { get; set; }

	public int sliceIndex { get; set; }

	public float debugOverlayRatio { get; set; } = 1f / 3f;

	public TextureDebugMode textureDebugMode { get; set; }

	public float zoom { get; set; }

	public bool showExtra { get; set; }

	public float minValue { get; set; }

	public float maxValue { get; set; }

	public float GetDefaultMinValue()
	{
		return textureDebugMode switch
		{
			TextureDebugMode.WaterRawVelocity => 0.5f, 
			TextureDebugMode.WaterVelocity => 0.03f, 
			_ => GetMinValue(), 
		};
	}

	public float GetDefaultMaxValue()
	{
		if (textureDebugMode == TextureDebugMode.WaterVelocity)
		{
			return 0.5f;
		}
		return GetMaxValue();
	}

	public float GetMinValue()
	{
		_ = textureDebugMode;
		return 0f;
	}

	public float GetMaxValue()
	{
		if (textureDebugMode == TextureDebugMode.WaterDepth)
		{
			return 4096f;
		}
		return 1f;
	}

	public bool HasExtra()
	{
		if (textureDebugMode == TextureDebugMode.WaterVelocity)
		{
			return true;
		}
		return false;
	}

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		m_DebugBlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/BH/CustomPass/DebugBlitQuad"));
		m_MaterialPropertyBlock = new MaterialPropertyBlock();
		m_TopViewRenderIndirectArgs = new ComputeBuffer(8, 4, ComputeBufferType.DrawIndirect);
		m_TopViewMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.terrainCBTTopViewDebug);
	}

	protected override void Cleanup()
	{
		CoreUtils.Destroy(m_DebugBlitMaterial);
		RTHandles.Release(m_TopViewRenderTexture);
		m_TopViewRenderIndirectArgs.Dispose();
		CoreUtils.Destroy(m_TopViewMaterial);
	}

	private static T GetSystem<T>() where T : ComponentSystemBase
	{
		return World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<T>();
	}

	private static float RemToPxScale(HDCamera hdCamera)
	{
		InterfaceSettings interfaceSettings = GameManager.instance?.settings?.userInterface;
		if (interfaceSettings != null && interfaceSettings.interfaceScaling)
		{
			if ((double)hdCamera.finalViewport.height > 0.5625 * (double)hdCamera.finalViewport.width)
			{
				return hdCamera.finalViewport.width / 1920f;
			}
			return hdCamera.finalViewport.height / 1080f;
		}
		return 1f;
	}

	private static int GetRuntimeDebugPanelWidth(HDCamera hdCamera)
	{
		int b = (int)((float)(GetSystem<DebugUISystem>().visible ? 610 : 10) * RemToPxScale(hdCamera));
		return Mathf.Min(hdCamera.actualWidth, b);
	}

	private static int GetRuntimePadding(HDCamera hdCamera)
	{
		return (int)(10f * RemToPxScale(hdCamera));
	}

	private static T GetCustomPass<T>(string passName, CustomPassInjectionPoint injectionPoint) where T : CustomPass
	{
		List<CustomPassVolume> list = new List<CustomPassVolume>();
		CustomPassVolume.GetActivePassVolumes(injectionPoint, list);
		foreach (CustomPassVolume item in list)
		{
			foreach (CustomPass customPass in item.customPasses)
			{
				if (customPass.name == passName && customPass is T result)
				{
					return result;
				}
			}
		}
		return null;
	}

	public bool SetupTexture(out Texture tex, out int sliceCount)
	{
		Vector4 value = new Vector4(minValue, 1f / (maxValue - minValue));
		sliceCount = 0;
		tex = null;
		switch (textureDebugMode)
		{
		case TextureDebugMode.WaterSurfaceSpectrum:
			if (WaterSurface.instanceCount > 0)
			{
				tex = WaterSurface.instancesAsArray[activeInstance].simulation.gpuBuffers.phillipsSpectrumBuffer;
				m_MaterialPropertyBlock.SetInt("_Slice", sliceIndex);
				m_MaterialPropertyBlock.SetVector("_ValidRange", new Vector4(minValue, maxValue));
			}
			break;
		case TextureDebugMode.WaterSurfaceDisplacement:
			if (WaterSurface.instanceCount > 0)
			{
				tex = WaterSurface.instancesAsArray[activeInstance].simulation.gpuBuffers.displacementBuffer;
				m_MaterialPropertyBlock.SetInt("_Slice", sliceIndex);
				m_MaterialPropertyBlock.SetVector("_ValidRange", new Vector4(minValue, maxValue));
			}
			break;
		case TextureDebugMode.WaterSurfaceGradient:
		case TextureDebugMode.WaterSurfaceJacobianSurface:
		case TextureDebugMode.WaterSurfaceJacobianDeep:
			if (WaterSurface.instanceCount > 0)
			{
				tex = WaterSurface.instancesAsArray[activeInstance].simulation.gpuBuffers.additionalDataBuffer;
				m_MaterialPropertyBlock.SetInt("_Slice", sliceIndex);
				m_MaterialPropertyBlock.SetVector("_ValidRange", new Vector4(minValue, maxValue));
			}
			break;
		case TextureDebugMode.WaterSurfaceCaustics:
			if (WaterSurface.instanceCount > 0)
			{
				tex = WaterSurface.instancesAsArray[activeInstance].simulation.gpuBuffers.causticsBuffer;
				m_MaterialPropertyBlock.SetInt("_Slice", sliceIndex);
				m_MaterialPropertyBlock.SetVector("_ValidRange", new Vector4(minValue, maxValue));
			}
			break;
		case TextureDebugMode.WaterVelocity:
			tex = GetSystem<WaterRenderSystem>().waterTexture;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", new Vector4(minValue, maxValue));
			break;
		case TextureDebugMode.HeightMapCascades:
			tex = GetSystem<TerrainSystem>().GetCascadeTexture();
			m_MaterialPropertyBlock.SetInt("_Slice", sliceIndex);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.TerrainOverlay:
			tex = GetSystem<TerrainRenderSystem>().overrideOverlaymap;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.HeightMap:
			tex = GetSystem<TerrainSystem>().heightmap;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.SplatMap:
			tex = GetSystem<TerrainMaterialSystem>().splatmap;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.WaterDepth:
		case TextureDebugMode.WaterPolution:
		case TextureDebugMode.WaterRawVelocity:
			tex = GetSystem<WaterRenderSystem>().waterTexture;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.SnowAccumulation:
		case TextureDebugMode.RainAccumulation:
			tex = GetSystem<SnowSystem>().SnowDepth;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.Wind:
			tex = GetSystem<WindTextureSystem>().WindTexture;
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.TerrainTesselation:
			tex = GetDebugTesselationTexture();
			m_MaterialPropertyBlock.SetInt("_Slice", 0);
			m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			break;
		case TextureDebugMode.SelectionOutlines:
		{
			OutlinesWorldUIPass customPass = GetCustomPass<OutlinesWorldUIPass>("Outlines Pass", CustomPassInjectionPoint.AfterPostProcess);
			if (customPass != null)
			{
				tex = customPass.outlineBuffer;
				m_MaterialPropertyBlock.SetInt("_Slice", 0);
				m_MaterialPropertyBlock.SetVector("_ValidRange", value);
			}
			break;
		}
		default:
			tex = null;
			break;
		}
		if (tex != null)
		{
			if (tex.dimension == TextureDimension.Tex2DArray || tex.dimension == TextureDimension.Tex3D)
			{
				if (tex is Texture2DArray texture2DArray)
				{
					sliceCount = texture2DArray.depth - 1;
				}
				if (tex is Texture3D texture3D)
				{
					sliceCount = texture3D.depth - 1;
				}
				if (tex is RenderTexture renderTexture)
				{
					sliceCount = renderTexture.volumeDepth - 1;
				}
			}
			return true;
		}
		return false;
	}

	private Texture GetDebugTesselationTexture()
	{
		TerrainSurface validSurface = TerrainSurface.GetValidSurface();
		if (validSurface != null)
		{
			ComputeBuffer cameraCbtBuffer = validSurface.GetCameraCbtBuffer(Camera.main);
			if (cameraCbtBuffer != null)
			{
				ComputeShader terrainCBTTopViewDispatchDebug = HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.terrainCBTTopViewDispatchDebug;
				terrainCBTTopViewDispatchDebug.SetBuffer(0, "u_CbtBuffer", cameraCbtBuffer);
				terrainCBTTopViewDispatchDebug.SetBuffer(0, "u_DrawCommand", m_TopViewRenderIndirectArgs);
				terrainCBTTopViewDispatchDebug.Dispatch(0, 1, 1, 1);
				Graphics.SetRenderTarget(m_TopViewRenderTexture);
				GL.Clear(clearDepth: true, clearColor: true, new Color(0.8f, 0.8f, 0.8f, 1f));
				m_TopViewMaterial.SetBuffer("u_CbtBuffer", cameraCbtBuffer);
				m_TopViewMaterial.SetPass(0);
				bool wireframe = GL.wireframe;
				GL.wireframe = true;
				Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_TopViewRenderIndirectArgs);
				GL.wireframe = wireframe;
				Graphics.SetRenderTarget(null);
				return m_TopViewRenderTexture;
			}
		}
		return null;
	}

	private void CheckResources(int size)
	{
		if (m_TopViewRenderTexture == null || m_TopViewRenderTexture.rt.width != size || m_TopViewRenderTexture.rt.height != size)
		{
			m_TopViewRenderTexture?.Release();
			m_TopViewRenderTexture = RTHandles.Alloc(size, size, 1, DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB, FilterMode.Point, TextureWrapMode.Repeat, TextureDimension.Tex2D, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.MSAA8x, bindTextureMS: false, useDynamicScale: false, RenderTextureMemoryless.None, VRTextureUsage.None, "CBTTopDownView");
		}
	}

	protected override void Execute(CustomPassContext ctx)
	{
		if (ctx.hdCamera.camera.cameraType == CameraType.Game)
		{
			float num = debugOverlayRatio;
			int a = (int)ctx.hdCamera.finalViewport.width;
			int num2 = (int)ctx.hdCamera.finalViewport.height;
			int num3 = (int)((float)Mathf.Min(a, num2) * num);
			Rect viewportSize = new Rect(GetRuntimeDebugPanelWidth(ctx.hdCamera), num2 - num3 - GetRuntimePadding(ctx.hdCamera), num3, num3);
			CheckResources(num3);
			m_MaterialPropertyBlock.Clear();
			m_MaterialPropertyBlock.SetFloat("_Zoom", zoom);
			m_MaterialPropertyBlock.SetInt("_ShowExtra", showExtra ? 1 : 0);
			bool applyExposure = false;
			if (SetupTexture(out var tex, out var _))
			{
				DisplayTexture(ctx.cmd, viewportSize, tex, m_DebugBlitMaterial, (int)textureDebugMode, m_MaterialPropertyBlock, applyExposure);
			}
		}
	}

	private static void DisplayTexture(CommandBuffer cmd, Rect viewportSize, Texture texture, Material debugMaterial, int mode, MaterialPropertyBlock mpb, bool applyExposure)
	{
		mpb.SetFloat(HDShaderIDs._ApplyExposure, applyExposure ? 1f : 0f);
		mpb.SetFloat(HDShaderIDs._Mipmap, 0f);
		mpb.SetTexture(HDShaderIDs._InputTexture, texture);
		mpb.SetInt("_Mode", mode);
		if (texture.dimension == TextureDimension.Tex2DArray)
		{
			cmd.EnableShaderKeyword("TEXTURE_SOURCE_ARRAY");
		}
		else
		{
			cmd.DisableShaderKeyword("TEXTURE_SOURCE_ARRAY");
		}
		cmd.SetViewport(viewportSize);
		cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
	}
}
