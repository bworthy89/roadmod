using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Input;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Serialization;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ToolbarUISystem : UISystemBase, IPreDeserialize
{
	private enum ToolbarItemType
	{
		Asset,
		Menu
	}

	private const string kGroup = "toolbar";

	private PrefabSystem m_PrefabSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ToolSystem m_ToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private UniqueAssetTrackingSystem m_UniqueAssetTrackingSystem;

	private PrefabUISystem m_PrefabUISystem;

	private ImageSystem m_ImageSystem;

	private UpgradeMenuUISystem m_UpgradeMenuUISystem;

	private ActionsSection m_ActionsSection;

	private EntityQuery m_ThemeQuery;

	private EntityQuery m_AssetPackQuery;

	private EntityQuery m_ToolbarGroupQuery;

	private EntityQuery m_UnlockedPrefabQuery;

	private RawValueBinding m_ToolbarGroupsBinding;

	private RawMapBinding<Entity> m_AssetMenuCategoriesBinding;

	private RawMapBinding<Entity> m_AssetsBinding;

	private RawValueBinding m_ThemesBinding;

	private RawValueBinding m_AssetPacksBinding;

	private ValueBinding<int> m_AgeMaskBinding;

	private GetterValueBinding<List<Entity>> m_SelectedThemesBinding;

	private GetterValueBinding<List<Entity>> m_SelectedAssetPacksBinding;

	private ValueBinding<Entity> m_SelectedAssetMenuBinding;

	private ValueBinding<Entity> m_SelectedAssetCategoryBinding;

	private ValueBinding<Entity> m_SelectedAssetBinding;

	private Dictionary<Entity, Entity> m_LastSelectedCategories;

	private Dictionary<Entity, Entity> m_LastSelectedAssets;

	private List<Entity> m_SelectedThemes;

	private List<Entity> m_SelectedAssetPacks;

	private bool m_UniqueAssetStatusChanged;

	private bool m_HasUnlockedPrefabLastFrame;

	public bool hasActiveSelection
	{
		get
		{
			if (!(m_SelectedAssetMenuBinding.value != Entity.Null))
			{
				return m_SelectedAssetBinding.value != Entity.Null;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_UniqueAssetTrackingSystem = base.World.GetOrCreateSystemManaged<UniqueAssetTrackingSystem>();
		UniqueAssetTrackingSystem uniqueAssetTrackingSystem = m_UniqueAssetTrackingSystem;
		uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged = (Action<Entity, bool>)Delegate.Combine(uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged, new Action<Entity, bool>(OnUniqueAssetStatusChanged));
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_UpgradeMenuUISystem = base.World.GetOrCreateSystemManaged<UpgradeMenuUISystem>();
		m_ActionsSection = base.World.GetOrCreateSystemManaged<ActionsSection>();
		m_ThemeQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<ThemeData>());
		m_AssetPackQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<AssetPackData>());
		m_ToolbarGroupQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<UIGroupElement>(), ComponentType.ReadOnly<UIToolbarGroupData>());
		m_UnlockedPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		AddBinding(m_ToolbarGroupsBinding = new RawValueBinding("toolbar", "toolbarGroups", BindToolbarGroups));
		AddBinding(m_AssetMenuCategoriesBinding = new RawMapBinding<Entity>("toolbar", "assetCategories", BindAssetCategories));
		AddBinding(m_AssetsBinding = new RawMapBinding<Entity>("toolbar", "assets", BindAssets));
		AddBinding(m_ThemesBinding = new RawValueBinding("toolbar", "themes", BindThemes));
		AddBinding(m_AssetPacksBinding = new RawValueBinding("toolbar", "assetPacks", BindPacks));
		AddBinding(m_AgeMaskBinding = new ValueBinding<int>("toolbar", "ageMask", (int)(m_ObjectToolSystem.allowAge ? m_ObjectToolSystem.actualAgeMask : ((Game.Tools.AgeMask)0))));
		AddBinding(m_SelectedThemesBinding = new GetterValueBinding<List<Entity>>("toolbar", "selectedThemes", () => m_SelectedThemes, new ListWriter<Entity>()));
		AddBinding(m_SelectedAssetPacksBinding = new GetterValueBinding<List<Entity>>("toolbar", "selectedAssetPacks", () => m_SelectedAssetPacks, new ListWriter<Entity>()));
		AddBinding(m_SelectedAssetMenuBinding = new ValueBinding<Entity>("toolbar", "selectedAssetMenu", Entity.Null));
		AddBinding(m_SelectedAssetCategoryBinding = new ValueBinding<Entity>("toolbar", "selectedAssetCategory", Entity.Null));
		AddBinding(m_SelectedAssetBinding = new ValueBinding<Entity>("toolbar", "selectedAsset", Entity.Null));
		AddBinding(new TriggerBinding<List<Entity>>("toolbar", "setSelectedThemes", SetSelectedThemes, new ListReader<Entity>()));
		AddBinding(new TriggerBinding<List<Entity>>("toolbar", "setSelectedAssetPacks", SetSelectedAssetPacks, new ListReader<Entity>()));
		AddBinding(new TriggerBinding<Entity>("toolbar", "selectAssetMenu", SelectAssetMenu));
		AddBinding(new TriggerBinding<Entity>("toolbar", "selectAssetCategory", SelectAssetCategory));
		AddBinding(new TriggerBinding<Entity, bool>("toolbar", "selectAsset", SelectAsset));
		AddBinding(new TriggerBinding("toolbar", "clearAssetSelection", ClearAssetSelection));
		AddBinding(new TriggerBinding<bool>("toolbar", "toggleToolOptions", ToggleToolOptions));
		AddBinding(new TriggerBinding<int>("toolbar", "setAgeMask", SetAgeMask));
		m_LastSelectedCategories = new Dictionary<Entity, Entity>();
		m_LastSelectedAssets = new Dictionary<Entity, Entity>();
		m_SelectedThemes = new List<Entity>();
		m_SelectedAssetPacks = new List<Entity>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		UniqueAssetTrackingSystem uniqueAssetTrackingSystem = m_UniqueAssetTrackingSystem;
		uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged = (Action<Entity, bool>)Delegate.Remove(uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged, new Action<Entity, bool>(OnUniqueAssetStatusChanged));
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PrefabBase activePrefab = m_ToolSystem.activePrefab;
		Entity value = m_SelectedAssetBinding.value;
		Entity value2 = m_SelectedAssetMenuBinding.value;
		if (activePrefab != null)
		{
			Entity entity = m_PrefabSystem.GetEntity(activePrefab);
			if (entity != value && !(m_ToolSystem.activeTool is AreaToolSystem) && !m_UpgradeMenuUISystem.upgrading && !m_ActionsSection.editingLot)
			{
				SelectAsset(entity, updateTool: false);
			}
		}
		else if (m_ToolSystem.activeTool != m_DefaultTool || (value2 == Entity.Null && value != Entity.Null))
		{
			bool updateTool = m_ToolSystem.activeTool is DefaultToolSystem;
			ClearAssetSelection(updateTool);
		}
		if (m_HasUnlockedPrefabLastFrame)
		{
			m_ToolbarGroupsBinding.Update();
			m_HasUnlockedPrefabLastFrame = false;
		}
		if (PrefabUtils.HasUnlockedPrefab<UIObjectData>(base.EntityManager, m_UnlockedPrefabQuery) || m_UniqueAssetStatusChanged)
		{
			if (value != Entity.Null && value2 != Entity.Null && activePrefab == null && InputManager.instance.activeControlScheme != InputManager.ControlScheme.Gamepad)
			{
				ActivatePrefabTool(value);
			}
			m_ToolbarGroupsBinding.Update();
			m_AssetMenuCategoriesBinding.UpdateAll();
			m_AssetsBinding.UpdateAll();
			m_ThemesBinding.Update();
			m_AssetPacksBinding.Update();
			m_HasUnlockedPrefabLastFrame = true;
		}
		m_UniqueAssetStatusChanged = false;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_SelectedThemes.Clear();
		m_SelectedAssetPacks.Clear();
		m_SelectedThemes.Add(m_CityConfigurationSystem.defaultTheme);
		m_LastSelectedCategories.Clear();
		m_LastSelectedAssets.Clear();
		Apply(m_SelectedThemes, m_SelectedAssetPacks, Entity.Null, Entity.Null, Entity.Null);
		m_ToolbarGroupsBinding.Update();
	}

	private void OnUniqueAssetStatusChanged(Entity prefabEntity, bool placed)
	{
		m_UniqueAssetStatusChanged = true;
	}

	public void PreDeserialize(Context context)
	{
		ClearAssetSelection();
		m_LastSelectedAssets.Clear();
		m_LastSelectedCategories.Clear();
	}

	private void BindToolbarGroups(IJsonWriter writer)
	{
		NativeArray<UIObjectInfo> sortedToolbarGroups = GetSortedToolbarGroups();
		writer.ArrayBegin(sortedToolbarGroups.Length);
		for (int i = 0; i < sortedToolbarGroups.Length; i++)
		{
			UIObjectInfo uIObjectInfo = sortedToolbarGroups[i];
			NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, base.EntityManager.GetBuffer<UIGroupElement>(uIObjectInfo.entity, isReadOnly: true), Allocator.TempJob);
			objects.Sort();
			writer.TypeBegin("toolbar.ToolbarGroup");
			writer.PropertyName("entity");
			writer.Write(uIObjectInfo.entity);
			writer.PropertyName("children");
			writer.ArrayBegin(objects.Length);
			for (int j = 0; j < objects.Length; j++)
			{
				Entity entity = objects[j].entity;
				PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(objects[j].prefabData);
				bool flag = base.EntityManager.HasComponent<UIAssetMenuData>(entity);
				writer.TypeBegin("toolbar.ToolbarItem");
				writer.PropertyName("entity");
				writer.Write(entity);
				writer.PropertyName("name");
				writer.Write(prefab.name);
				writer.PropertyName("type");
				writer.Write(flag ? 1 : 0);
				writer.PropertyName("icon");
				writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
				writer.PropertyName("locked");
				writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
				writer.PropertyName("uiTag");
				writer.Write(prefab.uiTag);
				writer.PropertyName("requirements");
				m_PrefabUISystem.BindPrefabRequirements(writer, entity);
				writer.PropertyName("highlight");
				writer.Write(base.EntityManager.HasComponent<UIHighlight>(entity));
				writer.PropertyName("selectSound");
				writer.Write((prefab is BulldozePrefab) ? "bulldoze" : null);
				writer.PropertyName("deselectSound");
				writer.Write((prefab is BulldozePrefab) ? "bulldoze-end" : null);
				writer.PropertyName("shortcut");
				writer.Write(prefab.TryGet<UIShortcut>(out var component) ? component.m_Action.m_AliasName : null);
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			writer.TypeEnd();
			objects.Dispose();
		}
		writer.ArrayEnd();
		sortedToolbarGroups.Dispose();
	}

	private void BindAssetCategories(IJsonWriter writer, Entity assetMenu)
	{
		if (base.EntityManager.HasComponent<UIAssetMenuData>(assetMenu) && base.EntityManager.TryGetBuffer(assetMenu, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> sortedCategories = GetSortedCategories(buffer);
			writer.ArrayBegin(sortedCategories.Length);
			for (int i = 0; i < sortedCategories.Length; i++)
			{
				Entity entity = sortedCategories[i].entity;
				PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(sortedCategories[i].prefabData);
				writer.TypeBegin("toolbar.AssetCategory");
				writer.PropertyName("entity");
				writer.Write(entity);
				writer.PropertyName("name");
				writer.Write(prefab.name);
				writer.PropertyName("icon");
				writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
				writer.PropertyName("locked");
				writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
				writer.PropertyName("uiTag");
				writer.Write(prefab.uiTag);
				writer.PropertyName("highlight");
				writer.Write(base.EntityManager.HasComponent<UIHighlight>(entity));
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			sortedCategories.Dispose();
		}
		else
		{
			writer.WriteEmptyArray();
		}
	}

	private NativeList<UIObjectInfo> GetSortedCategories(DynamicBuffer<UIGroupElement> elements)
	{
		NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, elements, Allocator.TempJob);
		for (int num = objects.Length - 1; num >= 0; num--)
		{
			if (!base.EntityManager.HasComponent<UIAssetCategoryData>(objects[num].entity) || !base.EntityManager.TryGetBuffer(objects[num].entity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer) || buffer.Length == 0)
			{
				objects.RemoveAtSwapBack(num);
			}
		}
		objects.Sort();
		return objects;
	}

	private void BindAssets(IJsonWriter writer, Entity assetCategory)
	{
		if (base.EntityManager.HasComponent<UIAssetCategoryData>(assetCategory) && base.EntityManager.TryGetBuffer(assetCategory, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, buffer, Allocator.TempJob);
			FilterByThemes(objects, m_SelectedThemes);
			FilterByPacks(objects, m_SelectedAssetPacks);
			FilterOutUpgrades(objects);
			objects.Sort();
			writer.ArrayBegin(objects.Length);
			for (int i = 0; i < objects.Length; i++)
			{
				Entity entity = objects[i].entity;
				bool unique = m_UniqueAssetTrackingSystem.IsUniqueAsset(entity);
				bool placed = m_UniqueAssetTrackingSystem.IsPlacedUniqueAsset(entity);
				BindAsset(writer, entity, unique, placed);
			}
			writer.ArrayEnd();
			objects.Dispose();
		}
		else
		{
			writer.WriteEmptyArray();
		}
	}

	public void BindAsset(IJsonWriter writer, Entity entity, bool unique = false, bool placed = false)
	{
		PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(entity);
		string text = null;
		if (prefab.TryGet<ContentPrerequisite>(out var component) && component.m_ContentPrerequisite.TryGet<DlcRequirement>(out var component2))
		{
			text = PlatformManager.instance.GetDlcName(component2.m_Dlc);
		}
		string text2 = null;
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity requirement = buffer[i].m_Requirement;
				if (base.EntityManager.HasComponent<ThemeData>(requirement))
				{
					text2 = ImageSystem.GetIcon(m_PrefabSystem.GetPrefab<PrefabBase>(requirement)) ?? m_ImageSystem.placeholderIcon;
				}
			}
		}
		int value = 0;
		if (base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component3))
		{
			value = component3.m_Priority;
		}
		writer.TypeBegin("toolbar.Asset");
		writer.PropertyName("entity");
		writer.Write(entity);
		writer.PropertyName("name");
		writer.Write(prefab.name);
		writer.PropertyName("priority");
		writer.Write(value);
		writer.PropertyName("icon");
		writer.Write(ImageSystem.GetThumbnail(prefab) ?? m_ImageSystem.placeholderIcon);
		writer.PropertyName("dlc");
		if (text != null)
		{
			writer.Write("Media/DLC/" + text + ".svg");
		}
		else
		{
			writer.WriteNull();
		}
		writer.PropertyName("theme");
		if (text2 != null)
		{
			writer.Write(text2);
		}
		else
		{
			writer.WriteNull();
		}
		writer.PropertyName("locked");
		writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
		writer.PropertyName("uiTag");
		writer.Write(prefab.uiTag);
		writer.PropertyName("highlight");
		writer.Write(base.EntityManager.HasComponent<UIHighlight>(entity));
		writer.PropertyName("unique");
		writer.Write(unique);
		writer.PropertyName("placed");
		writer.Write(placed);
		writer.PropertyName("constructionCost");
		m_PrefabUISystem.BindConstructionCost(writer, entity);
		writer.TypeEnd();
	}

	private void BindThemes(IJsonWriter writer)
	{
		if (GameManager.instance.gameMode.IsEditor())
		{
			writer.WriteEmptyArray();
			return;
		}
		Entity value = m_SelectedAssetCategoryBinding.value;
		if (base.EntityManager.HasComponent<UIAssetCategoryData>(value) && base.EntityManager.TryGetBuffer(value, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeParallelHashMap<Entity, bool> nativeParallelHashMap = new NativeParallelHashMap<Entity, bool>(10, Allocator.TempJob);
			for (int num = buffer.Length - 1; num >= 0; num--)
			{
				bool flag = base.EntityManager.HasComponent<UIHighlight>(buffer[num].m_Prefab);
				if (base.EntityManager.TryGetBuffer(buffer[num].m_Prefab, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer2))
				{
					for (int i = 0; i < buffer2.Length; i++)
					{
						Entity requirement = buffer2[i].m_Requirement;
						if (base.EntityManager.HasComponent<ThemeData>(requirement))
						{
							if (nativeParallelHashMap.TryGetValue(requirement, out var item))
							{
								nativeParallelHashMap[requirement] = flag || item;
							}
							else
							{
								nativeParallelHashMap[requirement] = flag;
							}
						}
					}
				}
			}
			if (!nativeParallelHashMap.IsEmpty)
			{
				NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, m_ThemeQuery, Allocator.TempJob);
				writer.ArrayBegin(sortedObjects.Length);
				for (int j = 0; j < sortedObjects.Length; j++)
				{
					ThemePrefab prefab = m_PrefabSystem.GetPrefab<ThemePrefab>(sortedObjects[j].prefabData);
					writer.TypeBegin("toolbar.Theme");
					writer.PropertyName("entity");
					writer.Write(sortedObjects[j].entity);
					writer.PropertyName("name");
					writer.Write(prefab.name);
					writer.PropertyName("icon");
					writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
					writer.PropertyName("highlight");
					writer.Write(nativeParallelHashMap.TryGetValue(sortedObjects[j].entity, out var item2) && item2);
					writer.TypeEnd();
				}
				writer.ArrayEnd();
				sortedObjects.Dispose();
			}
			else
			{
				writer.WriteEmptyArray();
			}
			nativeParallelHashMap.Dispose();
		}
		else
		{
			writer.WriteEmptyArray();
		}
	}

	private void SetSelectedThemes(List<Entity> themes)
	{
		Entity value = m_SelectedAssetMenuBinding.value;
		Entity value2 = m_SelectedAssetCategoryBinding.value;
		Entity entity = m_SelectedAssetBinding.value;
		List<Entity> packs = m_SelectedAssetPacks;
		if (value2 != Entity.Null)
		{
			entity = ((!(entity != Entity.Null)) ? GetFirstUnlockedItem(value2, themes, packs) : GetClosestAssetInThemes(entity, value2, themes));
		}
		else if (!IsMatchingTheme(entity, themes))
		{
			entity = Entity.Null;
		}
		Apply(themes, packs, value, value2, entity, InputManager.instance.activeControlScheme != InputManager.ControlScheme.Gamepad);
	}

	private void SetAgeMask(int ageMask)
	{
		m_ObjectToolSystem.ageMask = (Game.Tools.AgeMask)ageMask;
		m_AgeMaskBinding.Update((int)((m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.allowAge) ? m_ObjectToolSystem.actualAgeMask : ((Game.Tools.AgeMask)0)));
	}

	private void SetSelectedAssetPacks(List<Entity> packs)
	{
		Entity value = m_SelectedAssetMenuBinding.value;
		Entity value2 = m_SelectedAssetCategoryBinding.value;
		Entity entity = m_SelectedAssetBinding.value;
		List<Entity> themes = m_SelectedThemes;
		if (value2 != Entity.Null)
		{
			entity = ((!(entity != Entity.Null)) ? GetFirstUnlockedItem(value2, themes, packs) : GetClosestAssetInPacks(entity, value2, packs));
		}
		else if (!IsMatchingPack(entity, packs))
		{
			entity = Entity.Null;
		}
		Apply(themes, packs, value, value2, entity, InputManager.instance.activeControlScheme != InputManager.ControlScheme.Gamepad);
	}

	private void BindPacks(IJsonWriter writer)
	{
		if (GameManager.instance.gameMode.IsEditor())
		{
			writer.WriteEmptyArray();
			return;
		}
		Entity value = m_SelectedAssetCategoryBinding.value;
		if (base.EntityManager.HasComponent<UIAssetCategoryData>(value) && base.EntityManager.TryGetBuffer(value, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeParallelHashMap<Entity, bool> nativeParallelHashMap = new NativeParallelHashMap<Entity, bool>(10, Allocator.TempJob);
			for (int num = buffer.Length - 1; num >= 0; num--)
			{
				if (!base.EntityManager.HasComponent<ServiceUpgradeData>(buffer[num].m_Prefab))
				{
					bool flag = base.EntityManager.HasComponent<UIHighlight>(buffer[num].m_Prefab);
					if (base.EntityManager.TryGetBuffer(buffer[num].m_Prefab, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer2))
					{
						for (int i = 0; i < buffer2.Length; i++)
						{
							Entity pack = buffer2[i].m_Pack;
							if (base.EntityManager.HasComponent<AssetPackData>(pack))
							{
								if (nativeParallelHashMap.TryGetValue(pack, out var item))
								{
									nativeParallelHashMap[pack] = flag || item;
								}
								else
								{
									nativeParallelHashMap[pack] = flag;
								}
							}
						}
					}
				}
			}
			if (!nativeParallelHashMap.IsEmpty)
			{
				NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, m_AssetPackQuery, Allocator.TempJob);
				for (int num2 = sortedObjects.Length - 1; num2 >= 0; num2--)
				{
					if (!nativeParallelHashMap.ContainsKey(sortedObjects[num2].entity))
					{
						sortedObjects.RemoveAt(num2);
					}
				}
				writer.ArrayBegin(sortedObjects.Length);
				for (int j = 0; j < sortedObjects.Length; j++)
				{
					AssetPackPrefab prefab = m_PrefabSystem.GetPrefab<AssetPackPrefab>(sortedObjects[j].prefabData);
					writer.TypeBegin("toolbar.AssetPack");
					writer.PropertyName("entity");
					writer.Write(sortedObjects[j].entity);
					writer.PropertyName("name");
					writer.Write(prefab.name);
					writer.PropertyName("icon");
					writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
					writer.PropertyName("highlight");
					writer.Write(nativeParallelHashMap.TryGetValue(sortedObjects[j].entity, out var item2) && item2);
					writer.TypeEnd();
				}
				writer.ArrayEnd();
				sortedObjects.Dispose();
			}
			else
			{
				writer.WriteEmptyArray();
			}
			nativeParallelHashMap.Dispose();
		}
		else
		{
			writer.WriteEmptyArray();
		}
	}

	private void SelectAssetMenu(Entity assetMenu)
	{
		m_SelectedAssetPacks = new List<Entity>();
		List<Entity> themes = m_SelectedThemes;
		List<Entity> packs = m_SelectedAssetPacks;
		if (!(assetMenu != Entity.Null) || !base.EntityManager.HasComponent<UIAssetMenuData>(assetMenu))
		{
			return;
		}
		if (!m_LastSelectedCategories.TryGetValue(assetMenu, out var value))
		{
			value = GetFirstItem(assetMenu, themes, packs);
		}
		Entity value2 = Entity.Null;
		if (value != Entity.Null)
		{
			if (m_LastSelectedAssets.TryGetValue(value, out value2))
			{
				value2 = GetClosestAssetInThemes(value2, value, themes);
				value2 = GetClosestAssetInPacks(value2, value, packs);
			}
			else
			{
				value2 = GetFirstUnlockedItem(value, themes, packs);
			}
		}
		Apply(themes, packs, assetMenu, value, value2, InputManager.instance.activeControlScheme != InputManager.ControlScheme.Gamepad);
	}

	private void SelectAssetCategory(Entity assetCategory)
	{
		if (assetCategory != Entity.Null && base.EntityManager.TryGetComponent<UIAssetCategoryData>(assetCategory, out var component))
		{
			m_SelectedAssetPacks = new List<Entity>();
			Entity menu = component.m_Menu;
			List<Entity> themes = m_SelectedThemes;
			List<Entity> list = m_SelectedAssetPacks;
			if (m_LastSelectedAssets.TryGetValue(assetCategory, out var value))
			{
				value = GetClosestAssetInThemes(value, assetCategory, themes);
				value = GetClosestAssetInPacks(value, assetCategory, list);
			}
			else
			{
				value = GetFirstUnlockedItem(assetCategory, themes, list);
			}
			if (value == Entity.Null)
			{
				list.Clear();
				value = GetFirstUnlockedItem(assetCategory, themes, list);
			}
			Apply(themes, list, menu, assetCategory, value, InputManager.instance.activeControlScheme != InputManager.ControlScheme.Gamepad);
		}
	}

	private void SelectAsset(Entity assetEntity, bool updateTool)
	{
		List<Entity> themes = m_SelectedThemes;
		List<Entity> packs = m_SelectedAssetPacks;
		if (!IsMatchingTheme(assetEntity, themes))
		{
			NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, m_ThemeQuery, Allocator.TempJob);
			themes = FilterThemesByAsset(sortedObjects, assetEntity);
			sortedObjects.Dispose();
		}
		if (!IsMatchingPack(assetEntity, packs))
		{
			NativeList<UIObjectInfo> sortedObjects2 = UIObjectInfo.GetSortedObjects(base.EntityManager, m_AssetPackQuery, Allocator.TempJob);
			packs = FilterPacksByAsset(sortedObjects2, assetEntity);
			sortedObjects2.Dispose();
		}
		Entity assetMenuEntity = m_SelectedAssetMenuBinding.value;
		Entity assetCategoryEntity = m_SelectedAssetCategoryBinding.value;
		if (!IsMatchingAssetCategory(assetEntity, m_SelectedAssetCategoryBinding.value) && assetEntity != Entity.Null && base.EntityManager.TryGetComponent<UIObjectData>(assetEntity, out var component))
		{
			if (component.m_Group != Entity.Null && base.EntityManager.TryGetComponent<UIAssetCategoryData>(component.m_Group, out var component2))
			{
				assetMenuEntity = component2.m_Menu;
				assetCategoryEntity = component.m_Group;
			}
			else
			{
				assetMenuEntity = Entity.Null;
				assetCategoryEntity = Entity.Null;
			}
		}
		Apply(themes, packs, assetMenuEntity, assetCategoryEntity, assetEntity, updateTool);
	}

	public void ClearAssetSelection()
	{
		ClearAssetSelection(updateTool: true);
	}

	private void ClearAssetSelection(bool updateTool)
	{
		Apply(m_SelectedThemes, m_SelectedAssetPacks, Entity.Null, Entity.Null, Entity.Null, updateTool);
	}

	private void Apply(List<Entity> themes, List<Entity> packs, Entity assetMenuEntity, Entity assetCategoryEntity, Entity assetEntity, bool updateTool = false)
	{
		if (updateTool)
		{
			ActivatePrefabTool(assetEntity);
		}
		UpdateHighlights(themes, packs, assetMenuEntity, assetCategoryEntity, assetEntity);
		List<Entity> first = m_SelectedThemes;
		List<Entity> first2 = m_SelectedAssetPacks;
		Entity value = m_SelectedAssetCategoryBinding.value;
		m_SelectedThemes = themes;
		m_SelectedThemesBinding.Update();
		m_SelectedAssetPacks = packs;
		m_SelectedAssetPacksBinding.Update();
		m_SelectedAssetMenuBinding.Update(assetMenuEntity);
		m_SelectedAssetCategoryBinding.Update(assetCategoryEntity);
		if (updateTool)
		{
			m_SelectedAssetBinding.Update(assetEntity);
		}
		if (assetMenuEntity != Entity.Null && assetCategoryEntity != Entity.Null)
		{
			m_LastSelectedCategories[assetMenuEntity] = assetCategoryEntity;
		}
		if (assetCategoryEntity != Entity.Null && assetEntity != Entity.Null)
		{
			m_LastSelectedAssets[assetCategoryEntity] = assetEntity;
		}
		if (!first.SequenceEqual(themes) || !first2.SequenceEqual(packs))
		{
			m_AssetsBinding.UpdateAll();
		}
		if (!first.SequenceEqual(themes) || value != assetCategoryEntity)
		{
			m_ThemesBinding.Update();
		}
		if (!first2.SequenceEqual(packs) || value != assetCategoryEntity)
		{
			m_AssetPacksBinding.Update();
		}
		if (m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.allowAge)
		{
			m_AgeMaskBinding.Update((int)m_ObjectToolSystem.actualAgeMask);
		}
		else
		{
			m_AgeMaskBinding.Update(0);
		}
	}

	private void UpdateHighlights(List<Entity> themes, List<Entity> packs, Entity assetMenuEntity, Entity assetCategoryEntity, Entity assetEntity)
	{
		List<Entity> list = m_SelectedThemes;
		List<Entity> list2 = m_SelectedAssetPacks;
		Entity value = m_SelectedAssetMenuBinding.value;
		Entity value2 = m_SelectedAssetCategoryBinding.value;
		Entity value3 = m_SelectedAssetBinding.value;
		if ((value2 != Entity.Null && ((list.Count > 0 && !list.SequenceEqual(themes)) || (list2.Count > 0 && !list2.SequenceEqual(packs)))) || value2 != assetCategoryEntity)
		{
			bool flag = base.EntityManager.HasComponent<UIHighlight>(value2);
			if (base.EntityManager.TryGetBuffer(value2, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
			{
				NativeList<Entity> nativeList = new NativeList<Entity>(buffer.Length, Allocator.TempJob);
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity value4 = buffer[i].m_Prefab;
					if (base.EntityManager.HasComponent<UIHighlight>(value4))
					{
						if (IsMatchingTheme(value4, list) || IsMatchingPack(value4, list2))
						{
							nativeList.Add(in value4);
						}
						else
						{
							flag = false;
						}
					}
				}
				base.EntityManager.RemoveComponent<UIHighlight>(nativeList.AsArray());
				nativeList.Dispose();
			}
			if (flag)
			{
				base.EntityManager.RemoveComponent<UIHighlight>(value2);
				m_AssetMenuCategoriesBinding.UpdateAll();
			}
			if (base.EntityManager.HasComponent<UIHighlight>(value))
			{
				bool flag2 = true;
				if (base.EntityManager.TryGetBuffer(value, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer2))
				{
					for (int j = 0; j < buffer2.Length; j++)
					{
						if (base.EntityManager.HasComponent<UIHighlight>(buffer2[j].m_Prefab))
						{
							flag2 = false;
							break;
						}
					}
				}
				if (flag2)
				{
					base.EntityManager.RemoveComponent<UIHighlight>(value);
					m_ToolbarGroupsBinding.Update();
				}
			}
		}
		if (value == Entity.Null && value2 == Entity.Null && assetEntity != value3 && base.EntityManager.HasComponent<UIHighlight>(value3))
		{
			base.EntityManager.RemoveComponent<UIHighlight>(value3);
			m_ToolbarGroupsBinding.Update();
		}
	}

	private void ActivatePrefabTool(Entity assetEntity)
	{
		if (assetEntity != Entity.Null && !base.EntityManager.HasEnabledComponent<Locked>(assetEntity) && m_PrefabSystem.TryGetPrefab<PrefabBase>(assetEntity, out var prefab))
		{
			m_ToolSystem.ActivatePrefabTool(prefab);
		}
		else
		{
			m_ToolSystem.activeTool = m_DefaultTool;
		}
	}

	private bool IsMatchingTheme(Entity assetEntity, List<Entity> themes)
	{
		if (assetEntity == Entity.Null)
		{
			return false;
		}
		if (themes.Count > 0 && base.EntityManager.TryGetBuffer(assetEntity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
		{
			bool flag = false;
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity requirement = buffer[i].m_Requirement;
				for (int j = 0; j < themes.Count; j++)
				{
					if (requirement == themes[j])
					{
						return true;
					}
					if (base.EntityManager.HasComponent<ThemeData>(requirement))
					{
						flag = true;
					}
				}
			}
			return !flag;
		}
		return true;
	}

	private bool IsMatchingPack(Entity assetEntity, List<Entity> packs)
	{
		if (assetEntity == Entity.Null)
		{
			return false;
		}
		if (packs.Count > 0 && base.EntityManager.TryGetBuffer(assetEntity, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer))
		{
			bool flag = false;
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity pack = buffer[i].m_Pack;
				for (int j = 0; j < packs.Count; j++)
				{
					if (pack == packs[j])
					{
						return true;
					}
					if (base.EntityManager.HasComponent<AssetPackData>(pack))
					{
						flag = true;
					}
				}
			}
			return !flag;
		}
		return packs.Count == 0;
	}

	private bool IsMatchingAssetCategory(Entity assetEntity, Entity assetCategoryEntity)
	{
		if (assetEntity != Entity.Null && base.EntityManager.TryGetComponent<UIObjectData>(assetEntity, out var component))
		{
			return component.m_Group == assetCategoryEntity;
		}
		return false;
	}

	private Entity GetFirstTheme(Entity assetEntity)
	{
		if (assetEntity != Entity.Null && base.EntityManager.TryGetBuffer(assetEntity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity requirement = buffer[i].m_Requirement;
				if (base.EntityManager.HasComponent<ThemeData>(requirement))
				{
					return requirement;
				}
			}
		}
		return Entity.Null;
	}

	private Entity GetFirstPack(Entity assetEntity)
	{
		if (assetEntity != Entity.Null && base.EntityManager.TryGetBuffer(assetEntity, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity pack = buffer[i].m_Pack;
				if (base.EntityManager.HasComponent<AssetPackData>(pack))
				{
					return pack;
				}
			}
		}
		return Entity.Null;
	}

	private Entity GetClosestAssetInThemes(Entity oldAssetEntity, Entity groupEntity, List<Entity> themes)
	{
		if (IsMatchingTheme(oldAssetEntity, themes))
		{
			return oldAssetEntity;
		}
		Entity firstTheme = GetFirstTheme(oldAssetEntity);
		if (firstTheme != Entity.Null && base.EntityManager.TryGetBuffer(groupEntity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			for (int i = 0; i < themes.Count; i++)
			{
				Entity entity = themes[i];
				PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(oldAssetEntity);
				ThemePrefab prefab2 = m_PrefabSystem.GetPrefab<ThemePrefab>(firstTheme);
				ThemePrefab prefab3 = m_PrefabSystem.GetPrefab<ThemePrefab>(entity);
				if (!prefab.name.StartsWith(prefab2.assetPrefix))
				{
					continue;
				}
				string text = prefab.name.Substring(prefab2.assetPrefix.Length);
				string text2 = prefab3.assetPrefix + text;
				NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, buffer, Allocator.TempJob);
				FilterByTheme(objects, entity);
				FilterOutUpgrades(objects);
				try
				{
					for (int j = 0; j < objects.Length; j++)
					{
						Entity entity2 = objects[j].entity;
						if (m_PrefabSystem.GetPrefab<PrefabBase>(objects[j].prefabData).name == text2)
						{
							return entity2;
						}
					}
					for (int k = 0; k < objects.Length; k++)
					{
						Entity entity3 = objects[k].entity;
						if (m_PrefabSystem.GetPrefab<PrefabBase>(objects[k].prefabData).name == text)
						{
							return entity3;
						}
					}
				}
				finally
				{
					objects.Dispose();
				}
			}
		}
		return GetFirstUnlockedItem(groupEntity, themes, m_SelectedAssetPacks);
	}

	private Entity GetClosestAssetInPacks(Entity oldAssetEntity, Entity groupEntity, List<Entity> packs)
	{
		if (IsMatchingPack(oldAssetEntity, packs))
		{
			return oldAssetEntity;
		}
		if (base.EntityManager.TryGetBuffer(groupEntity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, buffer, Allocator.TempJob);
			FilterByPacks(objects, packs);
			FilterOutUpgrades(objects);
			try
			{
				for (int i = 0; i < objects.Length; i++)
				{
					Entity entity = objects[i].entity;
					if (IsMatchingPack(entity, packs))
					{
						return entity;
					}
				}
			}
			finally
			{
				objects.Dispose();
			}
		}
		return GetFirstUnlockedItem(groupEntity, m_SelectedThemes, packs);
	}

	private Entity GetFirstItem(Entity groupEntity, List<Entity> themes, List<Entity> packs)
	{
		if (base.EntityManager.TryGetBuffer(groupEntity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, buffer, Allocator.TempJob);
			FilterByThemes(objects, themes);
			FilterByPacks(objects, packs);
			FilterOutUpgrades(objects);
			objects.Sort();
			try
			{
				if (objects.Length > 0)
				{
					return objects[0].entity;
				}
			}
			finally
			{
				objects.Dispose();
			}
		}
		return Entity.Null;
	}

	private Entity GetFirstUnlockedItem(Entity groupEntity, List<Entity> themes, List<Entity> packs)
	{
		if (base.EntityManager.TryGetBuffer(groupEntity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, buffer, Allocator.TempJob);
			FilterByThemes(objects, themes);
			FilterByPacks(objects, packs);
			FilterOutUpgrades(objects);
			objects.Sort();
			try
			{
				for (int i = 0; i < objects.Length; i++)
				{
					Entity entity = objects[i].entity;
					if (!base.EntityManager.HasEnabledComponent<Locked>(entity))
					{
						return entity;
					}
				}
				if (objects.Length > 0)
				{
					return objects[0].entity;
				}
			}
			finally
			{
				objects.Dispose();
			}
		}
		return Entity.Null;
	}

	private NativeArray<UIObjectInfo> GetSortedToolbarGroups()
	{
		NativeArray<Entity> nativeArray = m_ToolbarGroupQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<UIToolbarGroupData> nativeArray2 = m_ToolbarGroupQuery.ToComponentDataArray<UIToolbarGroupData>(Allocator.TempJob);
		int length = nativeArray.Length;
		NativeArray<UIObjectInfo> nativeArray3 = new NativeArray<UIObjectInfo>(length, Allocator.Temp);
		for (int i = 0; i < length; i++)
		{
			nativeArray3[i] = new UIObjectInfo(nativeArray[i], nativeArray2[i].m_Priority);
		}
		nativeArray3.Sort();
		nativeArray.Dispose();
		nativeArray2.Dispose();
		return nativeArray3;
	}

	private void FilterByTheme(NativeList<UIObjectInfo> elementInfos, Entity themeEntity)
	{
		if (themeEntity == Entity.Null)
		{
			return;
		}
		for (int num = elementInfos.Length - 1; num >= 0; num--)
		{
			Entity entity = elementInfos[num].entity;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
			{
				bool flag = false;
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity requirement = buffer[i].m_Requirement;
					if (requirement == themeEntity)
					{
						flag = false;
						break;
					}
					if (base.EntityManager.HasComponent<ThemeData>(requirement))
					{
						flag = true;
					}
				}
				if (flag)
				{
					elementInfos.RemoveAtSwapBack(num);
				}
			}
		}
	}

	private List<Entity> FilterThemesByAsset(NativeList<UIObjectInfo> themes, Entity asset)
	{
		if (asset == Entity.Null)
		{
			return m_SelectedThemes;
		}
		for (int num = themes.Length - 1; num >= 0; num--)
		{
			Entity entity = themes[num].entity;
			if (base.EntityManager.TryGetBuffer(asset, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
			{
				bool flag = false;
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity requirement = buffer[i].m_Requirement;
					if (requirement == entity)
					{
						flag = false;
						break;
					}
					if (base.EntityManager.HasComponent<ThemeData>(requirement))
					{
						flag = true;
					}
				}
				if (flag)
				{
					themes.RemoveAtSwapBack(num);
				}
			}
		}
		m_SelectedThemes.Clear();
		for (int j = 0; j < themes.Length; j++)
		{
			m_SelectedThemes.Add(themes[j].entity);
		}
		return m_SelectedThemes;
	}

	private List<Entity> FilterPacksByAsset(NativeList<UIObjectInfo> assetPacks, Entity asset)
	{
		if (asset == Entity.Null)
		{
			return m_SelectedAssetPacks;
		}
		for (int num = assetPacks.Length - 1; num >= 0; num--)
		{
			Entity entity = assetPacks[num].entity;
			bool flag = true;
			if (base.EntityManager.TryGetBuffer(asset, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					if (buffer[i].m_Pack == entity)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				assetPacks.RemoveAtSwapBack(num);
			}
		}
		m_SelectedAssetPacks.Clear();
		for (int j = 0; j < assetPacks.Length; j++)
		{
			m_SelectedAssetPacks.Add(assetPacks[j].entity);
		}
		return m_SelectedAssetPacks;
	}

	private void FilterByPack(NativeList<UIObjectInfo> elementInfos, Entity packEntity)
	{
		if (packEntity == Entity.Null)
		{
			return;
		}
		for (int num = elementInfos.Length - 1; num >= 0; num--)
		{
			Entity entity = elementInfos[num].entity;
			bool flag = true;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					if (buffer[i].m_Pack == packEntity)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				elementInfos.RemoveAtSwapBack(num);
			}
		}
	}

	private void FilterByThemes(NativeList<UIObjectInfo> elementInfos, List<Entity> themes)
	{
		for (int num = elementInfos.Length - 1; num >= 0; num--)
		{
			Entity entity = elementInfos[num].entity;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
			{
				bool flag = false;
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity requirement = buffer[i].m_Requirement;
					if (base.EntityManager.HasComponent<ThemeData>(requirement))
					{
						flag = true;
					}
					for (int j = 0; j < themes.Count; j++)
					{
						Entity entity2 = themes[j];
						if (requirement == entity2)
						{
							flag = false;
							break;
						}
					}
					if (!flag)
					{
						break;
					}
				}
				if (flag)
				{
					elementInfos.RemoveAtSwapBack(num);
				}
			}
		}
	}

	private void FilterByPacks(NativeList<UIObjectInfo> elementInfos, List<Entity> packs)
	{
		if (packs == null || packs.Count == 0)
		{
			return;
		}
		for (int num = elementInfos.Length - 1; num >= 0; num--)
		{
			Entity entity = elementInfos[num].entity;
			bool flag = true;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<AssetPackElement> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity pack = buffer[i].m_Pack;
					for (int j = 0; j < packs.Count; j++)
					{
						Entity entity2 = packs[j];
						if (pack == entity2)
						{
							flag = false;
							break;
						}
					}
					if (!flag)
					{
						break;
					}
				}
			}
			if (flag)
			{
				elementInfos.RemoveAtSwapBack(num);
			}
		}
	}

	private void FilterOutUpgrades(NativeList<UIObjectInfo> elementInfos)
	{
		for (int num = elementInfos.Length - 1; num >= 0; num--)
		{
			if (base.EntityManager.HasComponent<ServiceUpgradeData>(elementInfos[num].entity))
			{
				elementInfos.RemoveAtSwapBack(num);
			}
		}
	}

	private void ToggleToolOptions(bool enabled)
	{
		m_ToolSystem.activeTool?.ToggleToolOptions(enabled);
	}

	[Preserve]
	public ToolbarUISystem()
	{
	}
}
