using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class PrefabEdítorPanelSystem : EditorPanelSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private EditorAssetCategorySystem m_CategorySystem;

	private InspectorPanelSystem m_InspectorPanelSystem;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_ModifiedPrefabQuery;

	private PrefabPickerAdapter m_Adapter;

	private HierarchyMenu<EditorAssetCategory> m_CategoryMenu;

	private EditorAssetCategory m_AllCategory;

	private bool m_PrefabsDirty;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CategorySystem = base.World.GetOrCreateSystemManaged<EditorAssetCategorySystem>();
		m_InspectorPanelSystem = base.World.GetOrCreateSystemManaged<InspectorPanelSystem>();
		m_ModifiedPrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabData>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_Adapter = new PrefabPickerAdapter
		{
			displayPrefabTypeTooltip = true
		};
		PrefabPickerAdapter prefabPickerAdapter = m_Adapter;
		prefabPickerAdapter.EventPrefabSelected = (Action<PrefabBase>)Delegate.Combine(prefabPickerAdapter.EventPrefabSelected, new Action<PrefabBase>(OnPrefabSelected));
		title = "Editor.TOOL[PrefabEditorTool]";
		IWidget[] obj = new IWidget[5]
		{
			new PopupSearchField
			{
				adapter = m_Adapter
			},
			new FilterMenu
			{
				adapter = m_Adapter
			},
			null,
			null,
			null
		};
		Row row = new Row
		{
			flex = FlexLayout.Fill
		};
		IWidget[] array = new IWidget[2];
		HierarchyMenu<EditorAssetCategory> obj2 = new HierarchyMenu<EditorAssetCategory>
		{
			selectionType = HierarchyMenu.SelectionType.singleSelection,
			onSelectionChange = UpdatePrefabs,
			flex = new FlexLayout(1f, 0f, 0),
			path = "PrefabEditorToolCategories"
		};
		HierarchyMenu<EditorAssetCategory> hierarchyMenu = obj2;
		m_CategoryMenu = obj2;
		array[0] = hierarchyMenu;
		array[1] = new ItemPicker<PrefabItem>
		{
			adapter = m_Adapter,
			hasFavorites = true,
			flex = new FlexLayout(2f, 0f, 0)
		};
		row.children = array;
		obj[2] = row;
		obj[3] = new ItemPickerFooter
		{
			adapter = m_Adapter
		};
		obj[4] = new Button
		{
			displayName = "Editor.CREATE_NEW_PREFAB",
			action = OnCreatePrefabSelected
		};
		children = obj;
		GenerateCategories();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_Adapter.searchQuery = string.Empty;
		m_Adapter.selectedPrefab = null;
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_Adapter.LoadSettings();
		m_CategoryMenu.items = GetHierarchy();
		UpdatePrefabs();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (!m_ModifiedPrefabQuery.IsEmptyIgnoreFilter)
		{
			m_PrefabsDirty = true;
		}
		if (m_PrefabsDirty && base.activeSubPanel == null)
		{
			UpdatePrefabs();
		}
		m_Adapter.Update();
	}

	private void OnPrefabSelected(PrefabBase prefab)
	{
		m_InspectorPanelSystem.SelectPrefab(prefab);
		base.activeSubPanel = m_InspectorPanelSystem;
	}

	private void UpdatePrefabs()
	{
		m_PrefabsDirty = false;
		if (m_CategoryMenu.GetSelectedItem(out var selection))
		{
			HashSet<PrefabBase> prefabs = selection.GetPrefabs(base.EntityManager, m_PrefabSystem, InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef));
			m_Adapter.SetPrefabs(prefabs);
		}
	}

	private void GenerateCategories()
	{
		m_AllCategory = new EditorAssetCategory
		{
			id = "All",
			path = "All",
			entityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<PrefabData>() },
				None = new ComponentType[1] { ComponentType.ReadOnly<MeshData>() }
			}),
			defaultSelection = true,
			includeChildCategories = false
		};
	}

	private IEnumerable<HierarchyItem<EditorAssetCategory>> GetHierarchy()
	{
		yield return m_AllCategory.ToHierarchyItem();
		foreach (HierarchyItem<EditorAssetCategory> item in m_CategorySystem.GetHierarchy())
		{
			yield return item;
		}
	}

	private void OnCreatePrefabSelected()
	{
		base.activeSubPanel = new TypePickerPanel("Editor.CREATE_NEW_PREFAB", "Editor.PREFAB_TYPES", GetPrefabTypeItems().ToList(), OnCreatePrefab, base.CloseSubPanel);
	}

	private void OnCreatePrefab(Type type)
	{
		CloseSubPanel();
		PrefabBase prefabBase = (PrefabBase)ScriptableObject.CreateInstance(type);
		prefabBase.name = type.Name;
		m_PrefabSystem.AddPrefab(prefabBase);
		OnPrefabSelected(prefabBase);
	}

	private IEnumerable<Item> GetPrefabTypeItems()
	{
		foreach (Type item in TypePickerPanel.GetAllConcreteTypesDerivedFrom<PrefabBase>())
		{
			ComponentMenu customAttribute = item.GetCustomAttribute<ComponentMenu>();
			yield return new Item
			{
				type = item,
				name = WidgetReflectionUtils.NicifyVariableName(item.Name),
				parentDir = customAttribute?.menu
			};
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public PrefabEdítorPanelSystem()
	{
	}
}
