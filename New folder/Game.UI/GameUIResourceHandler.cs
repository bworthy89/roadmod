using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using cohtml.Net;
using Colossal.PSI.Common;
using Colossal.UI;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI.Menu;
using Game.UI.Thumbnails;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace Game.UI;

public class GameUIResourceHandler : DefaultResourceHandler
{
	protected class GameResourceRequestData : ResourceRequestData
	{
		public bool IsThumbnailRequest => UriBuilder.Scheme == "thumbnail";

		public bool IsScreenshotRequest => UriBuilder.Scheme == "screencapture";

		public bool IsUserAvatarRequest => UriBuilder.Scheme == "useravatar";

		public GameResourceRequestData(IResourceRequest request, IResourceResponse response)
			: base(request, response)
		{
		}
	}

	private const string kScreencaptureScheme = "screencapture";

	public const string kScreencaptureProtocol = "screencapture://";

	public const string kScreenshotOpString = "Screenshot";

	private const string kThumbnailScheme = "thumbnail";

	public const string kThumbnailProtocol = "thumbnail://";

	private const string kUserAvatarScheme = "useravatar";

	public const string kUserAvatarProtocol = "useravatar://";

	private Dictionary<string, Camera> m_HostCameraCache = new Dictionary<string, Camera>();

	public GameUIResourceHandler(MonoBehaviour coroutineHost)
	{
		base.coroutineHost = coroutineHost;
	}

	public override void OnResourceRequest(IResourceRequest request, IResourceResponse response)
	{
		try
		{
			DefaultResourceHandler.log.TraceFormat("OnResourceRequest {0}", request.GetURL());
			GameResourceRequestData requestData = new GameResourceRequestData(request, response);
			base.coroutineHost.StartCoroutine(TryGetResourceRequestAsync(requestData));
		}
		catch (Exception exception)
		{
			response.Finish(ResourceResponse.Status.Failure);
			DefaultResourceHandler.log.Error(exception, "URL: " + request.GetURL());
		}
	}

	private Camera GetCameraFromHost(string host)
	{
		if (m_HostCameraCache.TryGetValue(host, out var value))
		{
			if (value != null && host == value.tag.ToLowerInvariant())
			{
				return value;
			}
			m_HostCameraCache.Remove(host);
		}
		value = null;
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene sceneAt = SceneManager.GetSceneAt(i);
			List<GameObject> list = new List<GameObject>(sceneAt.rootCount);
			sceneAt.GetRootGameObjects(list);
			foreach (GameObject item in list)
			{
				Camera[] componentsInChildren = item.GetComponentsInChildren<Camera>(includeInactive: true);
				foreach (Camera camera in componentsInChildren)
				{
					if (host == camera.tag.ToLowerInvariant())
					{
						value = camera;
						break;
					}
				}
			}
		}
		if (value != null)
		{
			m_HostCameraCache.Add(host, value);
		}
		return value;
	}

	private RenderTexture SetupCameraTarget(string name, Camera camera, int width, int height)
	{
		RenderTexture targetTexture = camera.targetTexture;
		if (targetTexture.name == string.Empty)
		{
			targetTexture.name = name;
		}
		camera.targetTexture = null;
		targetTexture.Release();
		targetTexture.width = width;
		targetTexture.height = height;
		targetTexture.Create();
		camera.targetTexture = targetTexture;
		return targetTexture;
	}

	private IEnumerator RequestScreenshot(GameResourceRequestData requestData)
	{
		AddPendingRequest(requestData);
		yield return new WaitForEndOfFrame();
		if (requestData.Aborted)
		{
			yield break;
		}
		try
		{
			Camera cameraFromHost = GetCameraFromHost(requestData.UriBuilder.Host);
			if (cameraFromHost != null)
			{
				UrlQuery urlQuery = new UrlQuery(requestData.UriBuilder.Query);
				if (!urlQuery.Read("width", out int result))
				{
					result = cameraFromHost.pixelWidth;
				}
				if (!urlQuery.Read("height", out int result2))
				{
					result2 = cameraFromHost.pixelHeight;
				}
				if (!urlQuery.Read("op", out string result3))
				{
					result3 = null;
				}
				if (!urlQuery.Read("alloc", out UserImagesManager.ResourceType result4))
				{
					result4 = UserImagesManager.ResourceType.Managed;
				}
				bool isDynamic = false;
				if (urlQuery.Read("liveView", out bool result5))
				{
					isDynamic = result5;
				}
				MenuHelpers.SaveGamePreviewSettings saveGamePreviewSettings = new MenuHelpers.SaveGamePreviewSettings();
				saveGamePreviewSettings.FromUri(urlQuery);
				string name = Uri.UnescapeDataString(requestData.UriBuilder.Path.Substring(1));
				Texture texture;
				if (result3 == "Screenshot")
				{
					texture = base.userImagesManager.GetUserImageTarget(name, result, result2);
					if (texture == null)
					{
						texture = ScreenCaptureHelper.CreateRenderTarget(name, result, result2);
					}
					if (texture is RenderTexture destination)
					{
						ScreenCaptureHelper.CaptureScreenshot(cameraFromHost, destination, saveGamePreviewSettings);
					}
				}
				else
				{
					texture = base.userImagesManager.GetUserImageTarget(name, result, result2);
					if (texture == null)
					{
						texture = SetupCameraTarget(name, cameraFromHost, result, result2);
					}
				}
				if (texture != null)
				{
					requestData.ReceiveUserImage(base.userImagesManager.GetUserImageData(texture, result4, isDynamic));
					RespondWithSuccess(requestData);
					RemovePendingRequest(requestData);
				}
				else
				{
					requestData.Error = "No available render target for '" + requestData.UriBuilder.Host + "'";
					CheckForFailedRequest(requestData);
				}
			}
			else
			{
				requestData.Error = "No camera '" + requestData.UriBuilder.Host + "'";
				CheckForFailedRequest(requestData);
			}
		}
		catch (Exception ex)
		{
			requestData.Error = ex.ToString();
			CheckForFailedRequest(requestData);
		}
	}

	private IEnumerator TryGetResourceRequestAsync(GameResourceRequestData requestData)
	{
		DefaultResourceHandler.log.Debug($"Requesting resource with URL: {requestData.UriBuilder.Uri}");
		if (requestData.IsDataRequest)
		{
			RespondWithSuccess(requestData);
			yield return 0;
			RemovePendingRequest(requestData);
		}
		else if (requestData.IsScreenshotRequest)
		{
			yield return RequestScreenshot(requestData);
		}
		else if (requestData.IsThumbnailRequest)
		{
			yield return TryThumbnailRequestAsync(requestData);
		}
		else if (requestData.IsUserAvatarRequest)
		{
			yield return RequestUserAvatarAsync(requestData);
		}
		else
		{
			yield return TryPreloadedResourceRequestAsync(requestData);
		}
	}

	private bool UpdateTexture(ref Texture target, string name, (int width, int height, byte[] data) p)
	{
		if (target == null)
		{
			Texture2D texture2D = new Texture2D(p.width, p.height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
			{
				name = name,
				hideFlags = HideFlags.HideAndDontSave
			};
			texture2D.LoadRawTextureData(p.data);
			texture2D.Apply();
			texture2D.IncrementUpdateCount();
			target = texture2D;
			return true;
		}
		if (target is Texture2D texture2D2)
		{
			IntPtr nativeTexturePtr = texture2D2.GetNativeTexturePtr();
			texture2D2.LoadRawTextureData(p.data);
			texture2D2.Apply();
			texture2D2.IncrementUpdateCount();
			IntPtr nativeTexturePtr2 = texture2D2.GetNativeTexturePtr();
			if (nativeTexturePtr != nativeTexturePtr2)
			{
				base.userImagesManager.UpdateNativePtr(nativeTexturePtr);
			}
			return true;
		}
		return false;
	}

	private IEnumerator RequestUserAvatarAsync(GameResourceRequestData requestData)
	{
		AddPendingRequest(requestData);
		new UrlQuery(requestData.UriBuilder.Query).Read("size", out AvatarSize result);
		string path = requestData.UriBuilder.Path;
		string name = Uri.UnescapeDataString(path.Substring(1, path.Length - 1));
		Task<(int width, int height, byte[] data)> avatarTask = PlatformManager.instance.GetAvatar(result);
		yield return new WaitUntil(() => avatarTask.IsCompleted);
		if (requestData.Aborted || avatarTask.IsFaulted)
		{
			yield break;
		}
		(int, int, byte[]) result2 = avatarTask.Result;
		if (result2.Item3 == null)
		{
			requestData.Error = "Getting user avatar failed.";
			CheckForFailedRequest(requestData);
			yield break;
		}
		Texture target = base.userImagesManager.GetUserImageTarget(name, result2.Item1, result2.Item2);
		if (UpdateTexture(ref target, name, result2))
		{
			requestData.ReceiveUserImage(base.userImagesManager.GetUserImageData(target, UserImagesManager.ResourceType.Managed, isDynamic: false));
			RespondWithSuccess(requestData);
			RemovePendingRequest(requestData);
		}
		else
		{
			requestData.Error = "No available render target.";
			CheckForFailedRequest(requestData);
		}
	}

	private IEnumerator TryThumbnailRequestAsync(GameResourceRequestData requestData)
	{
		ThumbnailCache thumbnailCache = GameManager.instance?.thumbnailCache;
		if (thumbnailCache != null)
		{
			Camera cameraFromHost = GetCameraFromHost(requestData.UriBuilder.Host);
			ThumbnailCache.ThumbnailInfo info = null;
			try
			{
				bool flag = cameraFromHost != null;
				UrlQuery urlQuery = new UrlQuery(requestData.UriBuilder.Query);
				if (!urlQuery.Read("width", out int result))
				{
					result = (flag ? cameraFromHost.pixelWidth : 0);
				}
				if (!urlQuery.Read("height", out int result2))
				{
					result2 = (flag ? cameraFromHost.pixelHeight : 0);
				}
				string text = requestData.UriBuilder.Path.Substring(1);
				string[] array = text.Split('/');
				if (array.Length != 2)
				{
					throw new ArgumentException("Invalid url path {0}", text);
				}
				string type = Uri.UnescapeDataString(array[0]);
				string name = Uri.UnescapeDataString(array[1]);
				PrefabSystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
				PrefabID id = new PrefabID(type, name);
				if (orCreateSystemManaged.TryGetPrefab(id, out var prefab))
				{
					info = thumbnailCache.GetThumbnail(prefab, result, result2, cameraFromHost);
				}
			}
			catch (Exception ex)
			{
				requestData.Error = ex.ToString();
				CheckForFailedRequest(requestData);
				yield break;
			}
			AddPendingRequest(requestData);
			if (info != null)
			{
				while (info.status == ThumbnailCache.Status.Pending)
				{
					if (requestData.Aborted)
					{
						yield break;
					}
					yield return 0;
				}
			}
			if (requestData.Aborted)
			{
				yield break;
			}
			if (info == null || info.status == ThumbnailCache.Status.Unavailable)
			{
				requestData.Error = $"Thumbnail not found {requestData.UriBuilder.Uri}";
				requestData.IsHandled = true;
				CheckForFailedRequest(requestData);
				yield break;
			}
			try
			{
				Texture texture = info.atlasFrame.texture;
				Rect region = info.region;
				if (texture != null)
				{
					requestData.ReceiveUserImage(base.userImagesManager.GetUserImageData(texture, UserImagesManager.ResourceType.Unmanaged, isDynamic: true, region, ResourceResponse.UserImageData.AlphaPremultiplicationMode.NonPremultiplied));
					RespondWithSuccess(requestData);
					RemovePendingRequest(requestData);
				}
				else
				{
					requestData.Error = $"Thumbnail not found {requestData.UriBuilder.Uri}";
					CheckForFailedRequest(requestData);
				}
			}
			catch (Exception ex2)
			{
				requestData.Error = ex2.ToString();
				CheckForFailedRequest(requestData);
			}
		}
		else
		{
			requestData.Error = "Thumbnails are not available at this time '" + requestData.UriBuilder.Host + "'";
			CheckForFailedRequest(requestData);
		}
	}
}
