using Game.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace Game.Rendering;

public class OutlinesWorldUIPass : CustomPass
{
	private static class ShaderID
	{
		public static readonly int _OutlineBuffer = Shader.PropertyToID("_OutlineBuffer");

		public static readonly int _Outlines_MaxDistance = Shader.PropertyToID("_Outlines_MaxDistance");

		public static readonly int _DRSScale = Shader.PropertyToID("_DRSScale");

		public static readonly int _DRSScaleSquared = Shader.PropertyToID("_DRSScaleSquared");
	}

	public LayerMask m_OutlineLayer = 0;

	public Material m_FullscreenOutline;

	public float m_MaxDistance = 16000f;

	private MaterialPropertyBlock m_OutlineProperties;

	private ShaderTagId[] m_ShaderTags;

	private RTHandle m_OutlineBuffer;

	private CustomSampler m_OutlinesSampler;

	public RTHandle outlineBuffer => m_OutlineBuffer;

	private void CheckResource()
	{
		MSAASamples mSAASamples = (SharedSettings.instance?.graphics?.GetQualitySetting<AntiAliasingQualitySettings>())?.outlinesMSAA ?? MSAASamples.None;
		if (mSAASamples < MSAASamples.None)
		{
			mSAASamples = MSAASamples.None;
		}
		if (mSAASamples > MSAASamples.MSAA8x)
		{
			mSAASamples = MSAASamples.MSAA8x;
		}
		if (m_OutlineBuffer == null || m_OutlineBuffer.rt == null || m_OutlineBuffer.rt.antiAliasing != (int)mSAASamples)
		{
			ReleaseResources();
			CreateResources(mSAASamples);
		}
	}

	private void CreateResources(MSAASamples msaaSamples)
	{
		m_OutlineBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, msaaSamples, bindTextureMS: false, useDynamicScale: false, RenderTextureMemoryless.None, VRTextureUsage.None, "Outline Buffer");
	}

	private void ReleaseResources()
	{
		if (m_OutlineBuffer != null)
		{
			m_OutlineBuffer.Release();
		}
	}

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		m_OutlinesSampler = CustomSampler.Create("Outlines pass");
		m_OutlineProperties = new MaterialPropertyBlock();
		m_ShaderTags = new ShaderTagId[3]
		{
			new ShaderTagId("Forward"),
			new ShaderTagId("ForwardOnly"),
			new ShaderTagId("SRPDefaultUnlit")
		};
	}

	protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
	{
		cullingParameters.cullingMask |= (uint)(int)m_OutlineLayer;
	}

	private static RendererListDesc CreateOpaqueRendererListDesc(CullingResults cull, Camera camera, ShaderTagId passName, PerObjectData rendererConfiguration = PerObjectData.None, RenderQueueRange? renderQueueRange = null, RenderStateBlock? stateBlock = null, Material overrideMaterial = null, bool excludeObjectMotionVectors = false)
	{
		RendererListDesc result = new RendererListDesc(passName, cull, camera);
		result.rendererConfiguration = rendererConfiguration;
		result.renderQueueRange = (renderQueueRange.HasValue ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllOpaque);
		result.sortingCriteria = SortingCriteria.CommonOpaque;
		result.stateBlock = stateBlock;
		result.overrideMaterial = overrideMaterial;
		result.excludeObjectMotionVectors = excludeObjectMotionVectors;
		return result;
	}

	private static RendererListDesc CreateTransparentRendererListDesc(CullingResults cull, Camera camera, ShaderTagId passName, PerObjectData rendererConfiguration = PerObjectData.None, RenderQueueRange? renderQueueRange = null, RenderStateBlock? stateBlock = null, Material overrideMaterial = null, bool excludeObjectMotionVectors = false)
	{
		RendererListDesc result = new RendererListDesc(passName, cull, camera);
		result.rendererConfiguration = rendererConfiguration;
		result.renderQueueRange = (renderQueueRange.HasValue ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllTransparent);
		result.sortingCriteria = SortingCriteria.CommonTransparent | SortingCriteria.RendererPriority;
		result.stateBlock = stateBlock;
		result.overrideMaterial = overrideMaterial;
		result.excludeObjectMotionVectors = excludeObjectMotionVectors;
		return result;
	}

	private void DrawOutlineMeshes(CustomPassContext ctx)
	{
		RendererListDesc rendererListDesc = new RendererListDesc(m_ShaderTags, ctx.cullingResults, ctx.hdCamera.camera);
		rendererListDesc.rendererConfiguration = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps;
		rendererListDesc.renderQueueRange = RenderQueueRange.all;
		rendererListDesc.sortingCriteria = SortingCriteria.BackToFront;
		rendererListDesc.excludeObjectMotionVectors = false;
		rendererListDesc.layerMask = m_OutlineLayer;
		RendererListDesc desc = rendererListDesc;
		ctx.cmd.EnableShaderKeyword("SHADERPASS_OUTLINES");
		ctx.cmd.SetGlobalFloat(ShaderID._Outlines_MaxDistance, m_MaxDistance);
		CoreUtils.SetRenderTarget(ctx.cmd, m_OutlineBuffer, ClearFlag.Color);
		CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(desc));
		ctx.cmd.DisableShaderKeyword("SHADERPASS_OUTLINES");
	}

	private void DrawAfterDRSObjects(CustomPassContext ctx)
	{
		float currentScale = DynamicResolutionHandler.instance.GetCurrentScale();
		ctx.cmd.SetGlobalFloat(ShaderID._DRSScale, currentScale);
		ctx.cmd.SetGlobalFloat(ShaderID._DRSScaleSquared, currentScale * currentScale);
		CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(CreateOpaqueRendererListDesc(ctx.cameraCullingResults, ctx.hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, PerObjectData.None, HDRenderQueue.k_RenderQueue_AfterDRSOpaque)));
		CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(CreateTransparentRendererListDesc(ctx.cameraCullingResults, ctx.hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, PerObjectData.None, HDRenderQueue.k_RenderQueue_AfterDRSTransparent)));
	}

	protected override void Execute(CustomPassContext ctx)
	{
		CheckResource();
		using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Outlines and World UI Pass")))
		{
			DrawOutlineMeshes(ctx);
			CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
			DrawAfterDRSObjects(ctx);
			m_OutlineProperties.SetTexture(ShaderID._OutlineBuffer, m_OutlineBuffer);
			CoreUtils.DrawFullScreen(ctx.cmd, m_FullscreenOutline, m_OutlineProperties);
		}
	}

	protected override void Cleanup()
	{
		ReleaseResources();
	}
}
