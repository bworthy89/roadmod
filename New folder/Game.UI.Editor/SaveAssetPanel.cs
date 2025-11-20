using System;
using System.Collections.Generic;
using Colossal;
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class SaveAssetPanel : EditorPanelBase
{
	public delegate void SaveCallback(string name, Hash128? overwriteGuid);

	private SaveCallback m_ConfirmCallback;

	private AssetPickerAdapter m_Adapter;

	private string m_FileName;

	public SaveAssetPanel(LocalizedString panelTitle, IEnumerable<AssetItem> items, Hash128? initialSelected, SaveCallback onConfirm, Action onCancel, LocalizedString saveButtonLabel = default(LocalizedString))
	{
		m_ConfirmCallback = onConfirm;
		m_Adapter = new AssetPickerAdapter(items);
		AssetPickerAdapter adapter = m_Adapter;
		adapter.EventItemSelected = (Action<AssetItem>)Delegate.Combine(adapter.EventItemSelected, new Action<AssetItem>(OnMapSelected));
		if (initialSelected.HasValue)
		{
			m_Adapter.SelectItemByGuid(initialSelected.Value);
		}
		m_FileName = m_Adapter.selectedItem?.fileName ?? string.Empty;
		base.title = panelTitle;
		base.children = new IWidget[4]
		{
			new ItemPicker<AssetItem>
			{
				adapter = m_Adapter,
				hasFavorites = true
			},
			new ItemPickerFooter
			{
				adapter = m_Adapter
			},
			new StringInputField
			{
				displayName = "Editor.FILE_NAME",
				accessor = new DelegateAccessor<string>(() => m_FileName, OnNameChange)
			},
			ButtonRow.WithChildren(new Button[2]
			{
				new Button
				{
					displayName = ((!saveButtonLabel.isEmpty) ? saveButtonLabel : ((LocalizedString)"Editor.SAVE")),
					disabled = () => string.IsNullOrEmpty(m_FileName),
					action = OnConfirm
				},
				new Button
				{
					displayName = "Common.CANCEL",
					action = onCancel
				}
			})
		};
	}

	private void OnMapSelected(AssetItem item)
	{
		if (!item.fileName.Equals(m_FileName, StringComparison.OrdinalIgnoreCase))
		{
			m_FileName = item.fileName;
		}
	}

	private void OnNameChange(string value)
	{
		m_Adapter.SelectItemByName(value, StringComparison.OrdinalIgnoreCase);
		m_FileName = value;
	}

	private void OnConfirm()
	{
		m_ConfirmCallback(m_FileName, m_Adapter.selectedItem?.guid);
	}
}
