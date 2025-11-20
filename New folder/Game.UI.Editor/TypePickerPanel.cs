#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;
using UnityEngine.Assertions;

namespace Game.UI.Editor;

public class TypePickerPanel : DirectoryPanelBase
{
	public delegate void SelectCallback(Type type);

	private readonly SelectCallback m_SelectCallback;

	public TypePickerPanel(LocalizedString panelTitle, LocalizedString rootDirName, IEnumerable<Item> items, SelectCallback onSelect, Action onCancel)
	{
		m_RootDirName = rootDirName;
		m_SelectCallback = onSelect;
		m_Items = new List<Item>(items);
		foreach (Item item in m_Items)
		{
			Assert.IsNotNull(item.type);
			Assert.IsNotNull(item.name);
			Assert.IsFalse(item.directory);
			if (item.displayName.isEmpty)
			{
				item.displayName = item.name;
			}
			if (item.parentDir == null)
			{
				continue;
			}
			string[] array = item.parentDir.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			string text = null;
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				string parentDir = text;
				if (text == null)
				{
					text = string.Empty;
				}
				text += text2;
				text += "/";
				if (!m_Directories.ContainsKey(text))
				{
					m_Directories.Add(text, new Item
					{
						displayName = LocalizedString.Value(text2),
						directory = true,
						parentDir = parentDir,
						name = text2,
						fullName = item.relativePath
					});
				}
			}
			if (item.fullName == null)
			{
				item.fullName = text;
			}
			item.parentDir = text;
		}
		m_Items.AddRange(m_Directories.Values);
		base.title = panelTitle;
		base.children = new IWidget[3]
		{
			new SearchField
			{
				adapter = this
			},
			m_PageView = new PageView
			{
				currentPage = 0,
				children = new IWidget[0]
			},
			new Button
			{
				displayName = "Common.CANCEL",
				action = onCancel
			}
		};
		ShowSubDir(null);
	}

	public override void OnSelect(Item item)
	{
		if (item != null)
		{
			if (item.directory)
			{
				ShowSubDir(item.relativePath + "/");
			}
			else
			{
				m_SelectCallback(item.type);
			}
		}
	}

	public static IEnumerable<Type> GetAllConcreteTypesDerivedFrom<T>()
	{
		return from t in Assembly.GetExecutingAssembly().GetTypes()
			where !t.IsAbstract && t.IsSubclassOf(typeof(T))
			select t;
	}
}
