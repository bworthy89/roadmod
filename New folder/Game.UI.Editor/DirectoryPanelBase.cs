using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Colossal.UI;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public abstract class DirectoryPanelBase : EditorPanelBase, SearchField.IAdapter
{
	protected static ILog log = UIManager.log;

	protected const char kDirSeparator = '/';

	protected List<Item> m_Items;

	protected Dictionary<string, Item> m_Directories = new Dictionary<string, Item>();

	protected LocalizedString m_RootDirName;

	protected PageView m_PageView;

	protected readonly List<DirectoryAdapter> m_Stack = new List<DirectoryAdapter>();

	protected readonly List<IWidget> m_Pages = new List<IWidget>();

	string SearchField.IAdapter.searchQuery
	{
		get
		{
			return m_Stack.Last().searchQuery;
		}
		set
		{
			m_Stack.Last().searchQuery = value;
		}
	}

	public abstract void OnSelect(Item item);

	protected virtual void ShowSubDir(string dir)
	{
		DirectoryAdapter directoryAdapter = BuildAdapter(dir);
		m_Stack.Add(directoryAdapter);
		m_Pages.Add(BuildPage(directoryAdapter));
		m_PageView.children = m_Pages.ToArray();
		m_PageView.currentPage = m_Stack.Count - 1;
	}

	private DirectoryAdapter BuildAdapter(string dir)
	{
		return new DirectoryAdapter(this)
		{
			directoryPath = dir,
			items = m_Items.ToList()
		};
	}

	private IWidget BuildPage(DirectoryAdapter adapter)
	{
		PageLayout pageLayout = new PageLayout();
		pageLayout.title = ((adapter.directoryPath != null) ? m_Directories[adapter.directoryPath].displayName : m_RootDirName);
		pageLayout.backAction = ((adapter.directoryPath != null) ? new Action(OnBack) : null);
		pageLayout.children = new IWidget[1]
		{
			new ItemPicker<Item>
			{
				adapter = adapter,
				hasFavorites = true,
				hasImages = false
			}
		};
		return pageLayout;
	}

	protected virtual void OnBack()
	{
		if (m_Stack.Count > 1)
		{
			m_Stack.RemoveAt(m_Stack.Count - 1);
			m_Stack.Last().selectedItem = null;
			m_Pages.RemoveAt(m_Pages.Count - 1);
			m_PageView.children = m_Pages.ToArray();
			m_PageView.currentPage = m_Stack.Count - 1;
		}
	}
}
