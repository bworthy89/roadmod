using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class PrefabToolPanelSystem : EditorPanelSystemBase
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

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_ModifiedPrefabQuery;

	private PrefabPickerAdapter m_Adapter;

	private HierarchyMenu<EditorAssetCategory> m_CategoryMenu;

	private EditorAssetCategory m_AllCategory;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CategorySystem = base.World.GetOrCreateSystemManaged<EditorAssetCategorySystem>();
		m_ModifiedPrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabData>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			}
		});
		m_Adapter = new PrefabPickerAdapter();
		PrefabPickerAdapter prefabPickerAdapter = m_Adapter;
		prefabPickerAdapter.EventPrefabSelected = (Action<PrefabBase>)Delegate.Combine(prefabPickerAdapter.EventPrefabSelected, new Action<PrefabBase>(OnPrefabSelected));
		title = "Editor.TOOL[PrefabTool]";
		IWidget[] obj = new IWidget[4]
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
			path = "PrefabToolCategories"
		};
		HierarchyMenu<EditorAssetCategory> hierarchyMenu = obj2;
		m_CategoryMenu = obj2;
		array[0] = hierarchyMenu;
		array[1] = new ItemPicker<PrefabItem>
		{
			adapter = m_Adapter,
			hasFavorites = true,
			flex = new FlexLayout(2f, 0f, 0),
			selectOnFocus = true
		};
		row.children = array;
		obj[2] = row;
		obj[3] = new ItemPickerFooter
		{
			adapter = m_Adapter
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
		m_ToolSystem.ActivatePrefabTool(m_Adapter.selectedPrefab);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (!m_ModifiedPrefabQuery.IsEmptyIgnoreFilter)
		{
			UpdatePrefabs();
		}
		m_Adapter.Update();
		m_Adapter.selectedPrefab = m_ToolSystem.activePrefab;
	}

	protected override bool OnCancel()
	{
		if (m_Adapter.selectedPrefab != null)
		{
			m_Adapter.selectedPrefab = null;
			m_ToolSystem.ActivatePrefabTool(null);
			return false;
		}
		return base.OnCancel();
	}

	private void OnPrefabSelected(PrefabBase prefab)
	{
		m_ToolSystem.ActivatePrefabTool(m_Adapter.selectedPrefab);
	}

	private void UpdatePrefabs()
	{
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
				Any = new ComponentType[6]
				{
					ComponentType.ReadOnly<ObjectData>(),
					ComponentType.ReadOnly<EffectData>(),
					ComponentType.ReadOnly<ActivityLocationData>(),
					ComponentType.ReadOnly<NetData>(),
					ComponentType.ReadOnly<NetLaneData>(),
					ComponentType.ReadOnly<AreaData>()
				},
				None = new ComponentType[4]
				{
					ComponentType.ReadOnly<BrandObjectData>(),
					ComponentType.ReadOnly<CarLaneData>(),
					ComponentType.ReadOnly<TrackLaneData>(),
					ComponentType.ReadOnly<ConnectionLaneData>()
				}
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
	public PrefabToolPanelSystem()
	{
	}
}
