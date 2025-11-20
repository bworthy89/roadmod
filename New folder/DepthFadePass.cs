using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class DepthFadePass : CustomPass
{
	private static class ShaderID
	{
		public static readonly int _LinearDepthTarget = Shader.PropertyToID("_LinearDepthTarget");

		public static readonly int _LinearDepth = Shader.PropertyToID("_LinearDepth");

		public static readonly int _DepthHistory = Shader.PropertyToID("_DepthHistory");

		public static readonly int _DepthHistoryTarget = Shader.PropertyToID("_DepthHistoryTarget");

		public static readonly int _ShaderVariables = Shader.PropertyToID("_ShaderVariablesDepthFadePass");

		public static readonly int _GlobalDepthFadeTex = Shader.PropertyToID("_DepthFadeTex");
	}

	private struct ShaderVariablesDepthFadePass
	{
		public Vector4 _TextureSizes;

		public float _MotionAdaptation;

		public float _DepthAdaptationThreshold;
	}

	public float m_MotionAdaptation = 2f;

	public float m_DepthAdaptationThreshold = 0.7f;

	private RTHandle m_LinearDepthBuffer;

	private RTHandle[] m_HistoryBuffers;

	private int currentBuffer;

	private ComputeShader m_ComputeShader;

	private int m_LinearizePassKernel;

	private int m_HistoryPassKernel;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		CreateResources();
		m_ComputeShader = Resources.Load<ComputeShader>("DepthFadePass");
		m_LinearizePassKernel = m_ComputeShader.FindKernel("LinearizePass");
		m_HistoryPassKernel = m_ComputeShader.FindKernel("HistoryPass");
	}

	protected override void Execute(CustomPassContext ctx)
	{
		if (!ctx.hdCamera.RequiresCameraJitter())
		{
			ReleaseResources();
			ctx.cmd.DisableShaderKeyword("DEPTH_FADE_FROM_TEXTURE");
			return;
		}
		CreateResources();
		int threadGroupsX = (m_LinearDepthBuffer.rt.width + 7) / 8;
		int threadGroupsY = (m_LinearDepthBuffer.rt.height + 7) / 8;
		ctx.cmd.SetComputeTextureParam(m_ComputeShader, m_LinearizePassKernel, ShaderID._LinearDepthTarget, m_LinearDepthBuffer);
		ctx.cmd.DispatchCompute(m_ComputeShader, m_LinearizePassKernel, threadGroupsX, threadGroupsY, 1);
		RTHandle rTHandle = m_HistoryBuffers[currentBuffer];
		currentBuffer = (currentBuffer + 1) % 2;
		RTHandle rTHandle2 = m_HistoryBuffers[currentBuffer];
		ctx.cmd.SetComputeTextureParam(m_ComputeShader, m_HistoryPassKernel, ShaderID._LinearDepth, m_LinearDepthBuffer);
		ctx.cmd.SetComputeTextureParam(m_ComputeShader, m_HistoryPassKernel, ShaderID._DepthHistory, rTHandle);
		ctx.cmd.SetComputeTextureParam(m_ComputeShader, m_HistoryPassKernel, ShaderID._DepthHistoryTarget, rTHandle2);
		ShaderVariablesDepthFadePass data = new ShaderVariablesDepthFadePass
		{
			_TextureSizes = new Vector4(rTHandle.rt.width, rTHandle.rt.height, m_LinearDepthBuffer.rt.width, m_LinearDepthBuffer.rt.height),
			_MotionAdaptation = m_MotionAdaptation,
			_DepthAdaptationThreshold = m_DepthAdaptationThreshold
		};
		ConstantBuffer.Push(ctx.cmd, in data, m_ComputeShader, ShaderID._ShaderVariables);
		ctx.cmd.DispatchCompute(m_ComputeShader, m_HistoryPassKernel, threadGroupsX, threadGroupsY, 1);
		ctx.cmd.SetGlobalTexture(ShaderID._GlobalDepthFadeTex, rTHandle2);
		ctx.cmd.EnableShaderKeyword("DEPTH_FADE_FROM_TEXTURE");
	}

	protected override void Cleanup()
	{
		ReleaseResources();
	}

	private void CreateResources()
	{
		if (m_LinearDepthBuffer == null)
		{
			m_LinearDepthBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R32_SFloat, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: true, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "DepthFade Intermediate Linear Depth");
		}
		if (m_HistoryBuffers == null)
		{
			m_HistoryBuffers = new RTHandle[2];
			for (int i = 0; i < m_HistoryBuffers.Length; i++)
			{
				m_HistoryBuffers[i] = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: true, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: false, RenderTextureMemoryless.None, VRTextureUsage.None, "DepthFade History " + i);
			}
		}
	}

	private void ReleaseResources()
	{
		if (m_LinearDepthBuffer != null)
		{
			m_LinearDepthBuffer.Release();
			m_LinearDepthBuffer = null;
		}
		if (m_HistoryBuffers != null)
		{
			for (int i = 0; i < m_HistoryBuffers.Length; i++)
			{
				m_HistoryBuffers[i]?.Release();
				m_HistoryBuffers[i] = null;
			}
			m_HistoryBuffers = null;
		}
	}
}
