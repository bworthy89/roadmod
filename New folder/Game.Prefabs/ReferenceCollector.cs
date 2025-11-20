using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace Game.Prefabs;

public class ReferenceCollector
{
	public class CollectedReferences
	{
		private readonly HashSet<PrefabBase> m_PrefabReferences = new HashSet<PrefabBase>();

		private readonly HashSet<ScriptableObject> m_ScriptableObjectReferences = new HashSet<ScriptableObject>();

		private readonly HashSet<AssetReference> m_AssetReferences = new HashSet<AssetReference>();

		private readonly HashSet<AssetData> m_AssetDatas = new HashSet<AssetData>();

		public IReadOnlyCollection<PrefabBase> prefabReferences => m_PrefabReferences;

		public IReadOnlyCollection<ScriptableObject> scriptableObjectReferences => m_ScriptableObjectReferences;

		public IReadOnlyCollection<AssetReference> assetReferences => m_AssetReferences;

		public IReadOnlyCollection<AssetData> assetDatas => m_AssetDatas;

		public void Add(object obj)
		{
			if (obj != null)
			{
				if (obj is PrefabBase item)
				{
					m_PrefabReferences.Add(item);
				}
				else if (!(obj is ComponentBase) && obj is ScriptableObject item2)
				{
					m_ScriptableObjectReferences.Add(item2);
				}
				else if (obj is AssetReference item3)
				{
					m_AssetReferences.Add(item3);
				}
				else if (obj is AssetData item4)
				{
					m_AssetDatas.Add(item4);
				}
			}
		}
	}

	private readonly Dictionary<object, bool> m_VisitedObjects;

	private readonly Dictionary<Type, FieldInfo[]> m_CachedFields;

	public ReferenceCollector()
	{
		m_VisitedObjects = new Dictionary<object, bool>();
		m_CachedFields = new Dictionary<Type, FieldInfo[]>(100);
	}

	public CollectedReferences CollectDependencies(IEnumerable<IAssetData> objs, bool addRoot)
	{
		m_VisitedObjects.Clear();
		CollectedReferences collectedReferences = new CollectedReferences();
		foreach (IAssetData obj in objs)
		{
			TraverseObject(obj, collectedReferences);
			if (addRoot)
			{
				collectedReferences.Add(obj);
			}
		}
		return collectedReferences;
	}

	public CollectedReferences CollectDependencies(IAssetData obj)
	{
		m_VisitedObjects.Clear();
		CollectedReferences collectedReferences = new CollectedReferences();
		TraverseObject(obj, collectedReferences);
		return collectedReferences;
	}

	private void TraverseSurfaceAsset(SurfaceAsset surfaceAsset, CollectedReferences references)
	{
		if (surfaceAsset == null || !TryAddVisited(surfaceAsset))
		{
			return;
		}
		surfaceAsset.LoadProperties(useVT: false);
		foreach (KeyValuePair<string, TextureAsset> texture in surfaceAsset.textures)
		{
			references.Add(texture.Value);
		}
		if (!surfaceAsset.isVTMaterial)
		{
			return;
		}
		references.Add(surfaceAsset.vtSurfaceAsset);
		for (int i = 0; i < surfaceAsset.stackCount; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				Colossal.Hash128 preProcessedTextureGuid = surfaceAsset.GetPreProcessedTextureGuid(i, j);
				references.Add(AssetDatabase.global.GetAsset(preProcessedTextureGuid));
			}
		}
	}

	private void TraverseObject(object obj, CollectedReferences references)
	{
		if (obj == null || !TryAddVisited(obj))
		{
			return;
		}
		if (obj is PrefabBase prefabBase)
		{
			references.Add(prefabBase);
			references.Add(prefabBase.asset);
		}
		else if (obj is PrefabAsset prefabAsset)
		{
			TraverseObject(prefabAsset.Load<PrefabBase>(), references);
		}
		else if (obj is SurfaceAsset surfaceAsset)
		{
			TraverseSurfaceAsset(surfaceAsset, references);
		}
		else if (obj is AssetReference<SurfaceAsset> assetReference)
		{
			TraverseSurfaceAsset(assetReference, references);
		}
		if (obj is AssetReference assetReference2)
		{
			references.Add(AssetDatabase.global.GetAsset(assetReference2.guid));
		}
		Type type = obj.GetType();
		if (!m_CachedFields.TryGetValue(type, out var value))
		{
			List<FieldInfo> list = type.GetFields(BindingFlags.Instance | BindingFlags.Public).ToList();
			list.AddRange(from field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				where field.GetCustomAttribute<SerializeField>() != null
				select field);
			value = list.ToArray();
			m_CachedFields.Add(type, value);
		}
		FieldInfo[] array = value;
		for (int num = 0; num < array.Length; num++)
		{
			object value2 = array[num].GetValue(obj);
			if (value2 == null)
			{
				continue;
			}
			references.Add(value2);
			if (value2 is IDictionary dictionary)
			{
				foreach (DictionaryEntry item in dictionary)
				{
					references.Add(item.Key);
					references.Add(item.Value);
					TraverseObject(item.Key, references);
					TraverseObject(item.Value, references);
				}
			}
			else if (value2 is IEnumerable enumerable)
			{
				foreach (object item2 in enumerable)
				{
					references.Add(item2);
					TraverseObject(item2, references);
				}
			}
			else
			{
				TraverseObject(value2, references);
			}
		}
	}

	private bool TryAddVisited(object obj)
	{
		return m_VisitedObjects.TryAdd(obj, value: true);
	}
}
