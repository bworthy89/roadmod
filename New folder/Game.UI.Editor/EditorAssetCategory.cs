using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Editor;

public class EditorAssetCategory
{
	public static readonly string kNameFormat = "Editor.ASSET_CATEGORY_TITLE[{0}]";

	private List<EditorAssetCategory> m_SubCategories = new List<EditorAssetCategory>();

	public IReadOnlyList<EditorAssetCategory> subCategories => m_SubCategories;

	public string id { get; set; }

	public string path { get; set; }

	public EntityQuery entityQuery { get; set; }

	public EditorAssetCategorySystem.IEditorAssetCategoryFilter filter { get; set; }

	private HashSet<Entity> exclude { get; set; }

	private List<Entity> include { get; set; }

	public string icon { get; set; }

	public bool includeChildCategories { get; set; } = true;

	public bool defaultSelection { get; set; }

	public void AddSubCategory(EditorAssetCategory category)
	{
		m_SubCategories.Add(category);
	}

	public HashSet<PrefabBase> GetPrefabs(EntityManager entityManager, PrefabSystem prefabSystem, EntityTypeHandle entityType)
	{
		HashSet<PrefabBase> hashSet = new HashSet<PrefabBase>();
		foreach (Entity entity in GetEntities(entityManager, prefabSystem, entityType))
		{
			if (prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab))
			{
				hashSet.Add(prefab);
			}
		}
		return hashSet;
	}

	public IEnumerable<Entity> GetEntities(EntityManager entityManager, PrefabSystem prefabSystem, EntityTypeHandle entityType)
	{
		if (entityQuery != default(EntityQuery))
		{
			using NativeArray<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkArray(Allocator.Temp);
			int i = 0;
			while (i < chunks.Length)
			{
				NativeArray<Entity> entities = chunks[i].GetNativeArray(entityType);
				int num;
				for (int j = 0; j < entities.Length; j = num)
				{
					if (CheckFilters(entities[j], entityManager, prefabSystem))
					{
						yield return entities[j];
					}
					num = j + 1;
				}
				num = i + 1;
				i = num;
			}
		}
		if (includeChildCategories)
		{
			foreach (EditorAssetCategory subCategory in m_SubCategories)
			{
				foreach (Entity entity in subCategory.GetEntities(entityManager, prefabSystem, entityType))
				{
					yield return entity;
				}
			}
		}
		if (include == null)
		{
			yield break;
		}
		foreach (Entity item in include)
		{
			yield return item;
		}
	}

	public bool IsEmpty(EntityManager entityManager, PrefabSystem prefabSystem, EntityTypeHandle entityType)
	{
		if (entityQuery != default(EntityQuery) && !entityQuery.IsEmptyIgnoreFilter && filter == null && exclude == null)
		{
			return false;
		}
		return !GetEntities(entityManager, prefabSystem, entityType).Any();
	}

	public void AddExclusion(Entity entity)
	{
		if (exclude == null)
		{
			exclude = new HashSet<Entity>(1);
		}
		exclude.Add(entity);
	}

	public void AddEntity(Entity entity)
	{
		if (include == null)
		{
			include = new List<Entity>(1);
		}
		include.Add(entity);
	}

	public string GetLocalizationID()
	{
		return string.Format(kNameFormat, path);
	}

	private bool CheckFilters(Entity entity, EntityManager entityManager, PrefabSystem prefabSystem)
	{
		if (filter == null || filter.Contains(entity, entityManager, prefabSystem))
		{
			if (exclude != null)
			{
				return !exclude.Contains(entity);
			}
			return true;
		}
		return false;
	}

	public HierarchyItem<EditorAssetCategory> ToHierarchyItem(int level = 0)
	{
		string localizationID = GetLocalizationID();
		return new HierarchyItem<EditorAssetCategory>
		{
			m_Data = this,
			m_DisplayName = LocalizedString.IdWithFallback(localizationID, id),
			m_Level = level,
			m_Icon = icon,
			m_Selectable = true,
			m_Selected = defaultSelection,
			m_Expandable = (m_SubCategories.Count > 0),
			m_Expanded = false
		};
	}
}
