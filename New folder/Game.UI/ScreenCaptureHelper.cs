using System.Threading.Tasks;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;
using Game.Rendering;
using Game.UI.Menu;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.UI;

public static class ScreenCaptureHelper
{
	public class AsyncRequest
	{
		private readonly TaskCompletionSource<bool> m_TaskCompletionSource;

		private NativeArray<byte> m_Data;

		public int width { get; }

		public int height { get; }

		public GraphicsFormat format { get; }

		public ref NativeArray<byte> result => ref m_Data;

		public AsyncRequest(Texture previewTexture)
		{
			width = previewTexture.width;
			height = previewTexture.height;
			format = previewTexture.graphicsFormat;
			int num = TextureUtils.ComputeMipchainSize(previewTexture.width, previewTexture.height, previewTexture.graphicsFormat, 1);
			m_Data = new NativeArray<byte>(num, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			log.DebugFormat("Issued request {0}x{1} size: {2}", width, height, num);
			m_TaskCompletionSource = new TaskCompletionSource<bool>();
			AsyncGPUReadback.RequestIntoNativeArray(ref m_Data, previewTexture, 0, OnCompleted);
		}

		private void OnCompleted(AsyncGPUReadbackRequest request)
		{
			if (width == request.width && height == request.height && m_Data.Length == request.layerDataSize)
			{
				if (!request.done)
				{
					log.ErrorFormat("Waiting for request {0}x{1} size: {2}. This should never happen!", request.width, request.height, request.layerDataSize);
					request.WaitForCompletion();
				}
				if (request.done && request.hasError)
				{
					log.ErrorFormat("Request failed {0}x{1} size: {2}. Result will be incorrect.", request.width, request.height, request.layerDataSize);
				}
				m_TaskCompletionSource.SetResult(result: true);
			}
			else
			{
				log.WarnFormat("Request failed {0}x{1} size: {2}. Completed successfully but not matching any request.", request.width, request.height, request.layerDataSize);
				m_TaskCompletionSource.SetResult(result: false);
			}
		}

		public async Task Dispose()
		{
			log.DebugFormat("Manual release of request {0}x{1}. Probably due to an error.", width, height);
			await Complete();
			m_Data.Dispose();
		}

		public Task Complete()
		{
			return m_TaskCompletionSource.Task;
		}
	}

	private static ILog log = LogManager.GetLogger("SceneFlow");

	private const string kOutlinesPassName = "Outlines Pass";

	public static RenderTexture CreateRenderTarget(string name, int width, int height, GraphicsFormat format = GraphicsFormat.R8G8B8A8_UNorm)
	{
		RenderTexture renderTexture = new RenderTexture(width, height, 0, format, 0);
		renderTexture.name = name;
		renderTexture.hideFlags = HideFlags.HideAndDontSave;
		renderTexture.Create();
		return renderTexture;
	}

	public static void CaptureScreenshot(Camera camera, RenderTexture destination, MenuHelpers.SaveGamePreviewSettings settings)
	{
		if (!(destination == null) && !(camera == null))
		{
			HDRPDotsInputs.punctualLightsJobHandle.Complete();
			RenderPipelineSettings.ColorBufferFormat colorBufferFormat = (QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.colorBufferFormat;
			RenderTexture renderTexture = new RenderTexture(destination.width, destination.height, 16, (GraphicsFormat)colorBufferFormat, 0);
			RenderingSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RenderingSystem>();
			CustomPassCache.SetPassEnabled("Outlines Pass", enabled: false);
			UIManager.defaultUISystem.enabled = false;
			existingSystemManaged.hideOverlay = true;
			RenderTexture targetTexture = camera.targetTexture;
			camera.forceIntoRenderTexture = true;
			camera.targetTexture = renderTexture;
			for (int i = 0; i < 8; i++)
			{
				camera.Render();
			}
			camera.targetTexture = targetTexture;
			camera.forceIntoRenderTexture = false;
			existingSystemManaged.hideOverlay = false;
			UIManager.defaultUISystem.enabled = true;
			CustomPassCache.SetPassEnabled("Outlines Pass", enabled: true);
			Material material = new Material(Shader.Find("Hidden/ScreenCaptureCompose"));
			if (settings.stylized)
			{
				material.EnableKeyword("STYLIZE");
			}
			else
			{
				material.DisableKeyword("STYLIZE");
			}
			material.SetFloat("_Radius", settings.stylizedRadius);
			TextureAsset overlayImage = settings.overlayImage;
			if (overlayImage != null)
			{
				material.SetTexture("_Overlay", overlayImage.Load(0));
			}
			Graphics.Blit(renderTexture, destination, material, 0);
			if (overlayImage != null)
			{
				overlayImage.Unload();
			}
			Object.Destroy(material);
			destination.IncrementUpdateCount();
			Object.Destroy(renderTexture);
		}
	}
}
