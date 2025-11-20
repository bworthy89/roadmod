using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Editor;

public class PrefabPickerPopup : IValueFieldPopup<PrefabBase>
{
	private ITypedValueAccessor<PrefabBase> m_Accessor;

	private Type m_PrefabType;

	private Func<PrefabBase, bool> m_Filter;

	private PrefabPickerAdapter m_Adapter;

	public bool nullable { get; set; }

	public IList<IWidget> children { get; }

	public PrefabPickerPopup(Type prefabType, Func<PrefabBase, bool> filter = null)
	{
		m_PrefabType = prefabType;
		m_Filter = filter;
		m_Adapter = new PrefabPickerAdapter();
		PrefabPickerAdapter adapter = m_Adapter;
		adapter.EventPrefabSelected = (Action<PrefabBase>)Delegate.Combine(adapter.EventPrefabSelected, new Action<PrefabBase>(OnPrefabSelected));
		children = new IWidget[3]
		{
			new PopupSearchField
			{
				adapter = m_Adapter,
				hasFavorites = true
			},
			new ItemPicker<PrefabItem>
			{
				adapter = m_Adapter,
				hasFavorites = true,
				hasImages = true
			},
			new ItemPickerFooter
			{
				adapter = m_Adapter
			}
		};
		ContainerExtensions.SetDefaults(children);
	}

	public bool Update()
	{
		m_Adapter.selectedPrefab = m_Accessor.GetTypedValue();
		m_Adapter.Update();
		return false;
	}

	public void Attach(ITypedValueAccessor<PrefabBase> accessor)
	{
		m_Accessor = accessor;
		List<PrefabBase> list = new List<PrefabBase>();
		if (nullable)
		{
			list.Add(null);
		}
		PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<PrefabSystem>();
		if (prefabSystem != null)
		{
			foreach (PrefabBase prefab in prefabSystem.prefabs)
			{
				if (m_PrefabType.IsInstanceOfType(prefab) && (m_Filter == null || m_Filter(prefab)))
				{
					list.Add(prefab);
				}
			}
		}
		m_Adapter.SetPrefabs(list);
		m_Adapter.LoadSettings();
	}

	public void Detach()
	{
		m_Adapter.searchQuery = string.Empty;
		m_Adapter.SetPrefabs(Array.Empty<PrefabBase>());
	}

	public LocalizedString GetDisplayValue(PrefabBase value)
	{
		return EditorPrefabUtils.GetPrefabLabel(value);
	}

	private void OnPrefabSelected(PrefabBase prefab)
	{
		m_Accessor.SetTypedValue(prefab);
	}
}
