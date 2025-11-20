using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace Game.UI.Thumbnails;

public class ThumbnailCache : IDisposable
{
	public enum Status
	{
		Ready,
		Pending,
		Unavailable,
		Refresh
	}

	public class ThumbnailInfo : IEquatable<ThumbnailInfo>
	{
		public object baseObjectRef;

		public Camera camera;

		public AtlasFrame atlasFrame;

		public Rect region;

		public Status status;

		public static bool operator ==(ThumbnailInfo left, ThumbnailInfo right)
		{
			return object.Equals(left, right);
		}

		public static bool operator !=(ThumbnailInfo left, ThumbnailInfo right)
		{
			return !object.Equals(left, right);
		}

		public override bool Equals(object obj)
		{
			if (obj is ThumbnailInfo other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(ThumbnailInfo other)
		{
			object obj = baseObjectRef;
			Camera camera = this.camera;
			AtlasFrame atlasFrame = this.atlasFrame;
			Rect rect = region;
			object obj2 = other.baseObjectRef;
			Camera camera2 = other.camera;
			AtlasFrame atlasFrame2 = other.atlasFrame;
			Rect rect2 = other.region;
			if (obj == obj2 && camera == camera2 && atlasFrame == atlasFrame2)
			{
				return rect == rect2;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (baseObjectRef, camera, atlasFrame, region.GetHashCode()).GetHashCode();
		}
	}

	public readonly struct ThumbnailKey : IEquatable<ThumbnailKey>
	{
		public string name { get; }

		public int width { get; }

		public int height { get; }

		public ThumbnailKey(string name, int width, int height)
		{
			this.name = name;
			this.width = width;
			this.height = height;
		}

		public static bool operator ==(ThumbnailKey left, ThumbnailKey right)
		{
			return object.Equals(left, right);
		}

		public static bool operator !=(ThumbnailKey left, ThumbnailKey right)
		{
			return !object.Equals(left, right);
		}

		public override bool Equals(object obj)
		{
			if (obj is ThumbnailKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(ThumbnailKey other)
		{
			string text = name;
			int num = width;
			int num2 = height;
			string text2 = other.name;
			int num3 = other.width;
			int num4 = other.height;
			if (text == text2 && num == num3)
			{
				return num2 == num4;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (name, width, height).GetHashCode();
		}

		public override string ToString()
		{
			return $"{name}_{width}x{height}";
		}
	}

	private Dictionary<ThumbnailKey, ThumbnailInfo> m_CacheData;

	public ThumbnailCache()
	{
		m_CacheData = new Dictionary<ThumbnailKey, ThumbnailInfo>();
	}

	private void OnAtlasAssetChanged(AssetChangedEventArgs args)
	{
		if (args.change == ChangeType.BulkAssetsChange)
		{
			m_CacheData.Clear();
			{
				foreach (AtlasAsset asset in AssetDatabase.global.GetAssets(default(SearchFilter<AtlasAsset>)))
				{
					LoadAtlas(asset);
				}
				return;
			}
		}
		if (args.change == ChangeType.AssetAdded || args.change == ChangeType.AssetUpdated)
		{
			LoadAtlas((AtlasAsset)args.asset);
		}
		else if (args.change == ChangeType.AssetDeleted)
		{
			UnloadAtlas((AtlasAsset)args.asset);
		}
	}

	private void LoadAtlas(AtlasAsset asset)
	{
		try
		{
			AtlasFrame atlasFrame = asset.Load();
			foreach (AtlasFrame.Entry item in asset)
			{
				m_CacheData.Add(new ThumbnailKey(item.name, (int)item.region.width, (int)item.region.height), new ThumbnailInfo
				{
					atlasFrame = atlasFrame,
					region = item.region,
					status = Status.Ready
				});
			}
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
	}

	private void UnloadAtlas(AtlasAsset asset)
	{
		try
		{
			using (asset)
			{
				asset.Load();
				foreach (AtlasFrame.Entry item in asset)
				{
					m_CacheData.Remove(new ThumbnailKey(item.name, (int)item.region.width, (int)item.region.height));
				}
				asset.Unload();
			}
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
	}

	public void Initialize()
	{
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe<AtlasAsset>(OnAtlasAssetChanged, AssetChangedEventArgs.Default);
	}

	public ThumbnailInfo GetCachedThumbnail(ThumbnailKey key)
	{
		if (m_CacheData.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public void Refresh()
	{
		foreach (KeyValuePair<ThumbnailKey, ThumbnailInfo> cacheDatum in m_CacheData)
		{
			cacheDatum.Value.status = Status.Pending;
		}
	}

	public ThumbnailInfo GetThumbnail(object obj, int width, int height, Camera camera = null)
	{
		ThumbnailInfo value = null;
		UnityEngine.Object obj2 = obj as UnityEngine.Object;
		if (obj2 != null)
		{
			ThumbnailKey key = new ThumbnailKey(obj2.name, width, height);
			m_CacheData.TryGetValue(key, out value);
		}
		return value;
	}

	public void Update()
	{
	}

	public void Dispose()
	{
		foreach (KeyValuePair<ThumbnailKey, ThumbnailInfo> cacheDatum in m_CacheData)
		{
			cacheDatum.Value.baseObjectRef = null;
		}
	}
}
