using System;
using System.Collections.Generic;
using Colossal;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class LoadAssetPanel : EditorPanelBase
{
	public delegate void LoadCallback(Hash128 guid);

	private LoadCallback m_ConfirmCallback;

	private AssetPickerAdapter m_Adapter;

	public LoadAssetPanel(LocalizedString panelTitle, IEnumerable<AssetItem> items, LoadCallback onConfirm, Action onClose)
	{
		m_ConfirmCallback = onConfirm;
		m_Adapter = new AssetPickerAdapter(items);
		base.title = panelTitle;
		base.children = new IWidget[4]
		{
			new SearchField
			{
				adapter = m_Adapter
			},
			new ItemPicker<AssetItem>
			{
				adapter = m_Adapter,
				hasFavorites = true
			},
			new ItemPickerFooter
			{
				adapter = m_Adapter
			},
			ButtonRow.WithChildren(new Button[2]
			{
				new Button
				{
					displayName = "Editor.LOAD",
					disabled = () => m_Adapter.selectedItem == null,
					action = OnConfirm
				},
				new Button
				{
					displayName = "Common.CANCEL",
					action = onClose
				}
			})
		};
	}

	private void OnConfirm()
	{
		m_ConfirmCallback(m_Adapter.selectedItem.guid);
	}
}
