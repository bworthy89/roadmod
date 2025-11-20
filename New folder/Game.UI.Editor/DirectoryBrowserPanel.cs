#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.UI.Localization;
using Game.UI.Widgets;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.Utilities;

namespace Game.UI.Editor;

public class DirectoryBrowserPanel : DirectoryPanelBase
{
	public delegate void SelectCallback(string directory);

	private readonly SelectCallback m_SelectCallback;

	private readonly Action m_OnCloseDirectoryBrowser;

	private string m_SelectedDirectory;

	private string m_RootDirectory;

	private bool m_LimitDepthToRoot;

	private bool ImportNotReady()
	{
		return true;
	}

	private void ImportAssets()
	{
	}

	public DirectoryBrowserPanel(string directory, string root, SelectCallback onSelect, Action onCancel)
	{
		m_LimitDepthToRoot = !string.IsNullOrEmpty(root);
		m_RootDirectory = root + "/";
		m_SelectCallback = onSelect;
		m_OnCloseDirectoryBrowser = onCancel;
		ListDrives();
		try
		{
			if (directory != null)
			{
				while (!Directory.Exists(directory))
				{
					directory = Path.GetDirectoryName(directory);
				}
				TraverseToDirectory(directory);
			}
		}
		catch (ArgumentException exception)
		{
			DirectoryPanelBase.log.Error(exception);
		}
		m_SelectedDirectory = m_Stack.LastOrDefault()?.directoryPath;
	}

	private void TraverseToDirectory(string directory)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(directory);
		List<string> list = new List<string>();
		for (DirectoryInfo directoryInfo2 = directoryInfo; directoryInfo2 != null; directoryInfo2 = directoryInfo2.Parent)
		{
			string item = ((directoryInfo2.Parent == null) ? directoryInfo2.Name.TrimEnd('\\') : directoryInfo2.Name);
			list.Add(item);
		}
		list.Reverse();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item2 in list)
		{
			stringBuilder.Append(item2);
			stringBuilder.Append('/');
			ShowSubDir(stringBuilder.ToString());
		}
	}

	private void ListDrives()
	{
		DriveInfo[] drives = DriveInfo.GetDrives();
		List<Item> list = new List<Item>();
		DriveInfo[] array = drives;
		foreach (DriveInfo driveInfo in array)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(driveInfo.VolumeLabel);
			try
			{
				DirectoryInfo[] directories = directoryInfo.GetDirectories("*", new EnumerationOptions
				{
					IgnoreInaccessible = true
				});
				foreach (DirectoryInfo directoryInfo2 in directories)
				{
					Item item = new Item();
					item.name = directoryInfo2.Name;
					item.tooltip = directoryInfo2.Name;
					item.fullName = directoryInfo2.FullName.Replace("\\", "/");
					item.parentDir = driveInfo.VolumeLabel.Replace("\\", "");
					item.directory = true;
					list.Add(item);
				}
			}
			catch (IOException)
			{
			}
		}
		ListItems("Editor.DRIVES", list);
		ShowSubDir(null);
	}

	private void ListItems(string directory, List<Item> items)
	{
		if (items.Count == 0)
		{
			return;
		}
		m_Directories.Clear();
		m_Items = items;
		m_RootDirName = directory;
		foreach (Item item in m_Items)
		{
			Assert.IsNotNull(item.name);
			if (item.displayName.isEmpty)
			{
				item.displayName = item.name;
			}
			if (item.parentDir == null)
			{
				continue;
			}
			string[] array = item.parentDir.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			string text = string.Empty;
			string text2 = null;
			string[] array2 = array;
			foreach (string text3 in array2)
			{
				text += text3;
				text += "/";
				if (!m_Directories.ContainsKey(text))
				{
					m_Directories.Add(text, new Item
					{
						displayName = LocalizedString.Value(text3),
						directory = true,
						parentDir = text2,
						name = text3,
						tooltip = text2
					});
				}
				text2 = text;
			}
			item.parentDir = text;
		}
		m_Items.AddRange(m_Directories.Values);
		m_Items.Sort();
		base.title = "Editor.OPEN_DIRECTORY";
		base.children = new IWidget[4]
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
				displayName = "Editor.SELECT_DIRECTORY",
				action = OnSelectDirectory,
				disabled = DirectoryNotSelectedOrRootSelected
			},
			new Button
			{
				displayName = "Common.CANCEL",
				action = m_OnCloseDirectoryBrowser
			}
		};
	}

	private bool DirectoryNotSelectedOrRootSelected()
	{
		if (m_SelectedDirectory != null)
		{
			return m_SelectedDirectory == m_RootDirectory;
		}
		return true;
	}

	private void OnSelectDirectory()
	{
		m_SelectCallback(m_SelectedDirectory);
	}

	public override void OnSelect(Item item)
	{
		if (item != null)
		{
			ShowSubDir((item.fullName == null) ? (item.name + "/") : (item.fullName + "/"));
		}
	}

	protected override void OnBack()
	{
		if (m_Stack.Count > 1)
		{
			m_Stack.RemoveAt(m_Stack.Count - 1);
			m_Stack.Last().selectedItem = null;
			m_Pages.RemoveAt(m_Pages.Count - 1);
			m_SelectedDirectory = m_Stack.LastOrDefault()?.directoryPath;
			if (m_LimitDepthToRoot && m_SelectedDirectory == m_RootDirectory)
			{
				m_Pages.Last().As<PageLayout>().backAction = null;
			}
			m_PageView.children = m_Pages.ToArray();
			m_PageView.currentPage = m_Stack.Count - 1;
		}
	}

	protected override void ShowSubDir(string dir)
	{
		if (!string.IsNullOrEmpty(dir))
		{
			m_SelectedDirectory = dir;
			if (m_Items != null)
			{
				m_Items.Clear();
			}
			try
			{
				List<Item> list = new List<Item>();
				DirectoryInfo[] directories = new DirectoryInfo(dir).GetDirectories("*", new EnumerationOptions
				{
					IgnoreInaccessible = true
				});
				foreach (DirectoryInfo directoryInfo in directories)
				{
					Item item = new Item();
					item.name = directoryInfo.Name;
					item.parentDir = dir;
					item.fullName = directoryInfo.FullName.Replace("\\", "/");
					item.directory = true;
					item.tooltip = directoryInfo.FullName;
					list.Add(item);
					DirectoryInfo[] directories2 = directoryInfo.GetDirectories("*", new EnumerationOptions
					{
						IgnoreInaccessible = true
					});
					item.directory = directories2.Length != 0;
					DirectoryInfo[] array = directories2;
					foreach (DirectoryInfo directoryInfo2 in array)
					{
						Item item2 = new Item();
						item2.name = directoryInfo2.Name;
						item2.parentDir = directoryInfo.Name;
						item2.fullName = directoryInfo2.FullName.Replace("\\", "/");
						item2.tooltip = directoryInfo2.FullName;
						list.Add(item2);
					}
				}
				if (list.Count == 0)
				{
					return;
				}
				ListItems(dir, list);
			}
			catch (Exception exception)
			{
				DirectoryPanelBase.log.Error(exception, "Error showing directory '" + dir + "'");
			}
		}
		base.ShowSubDir(dir);
		if (m_LimitDepthToRoot && m_SelectedDirectory == m_RootDirectory)
		{
			m_Pages.Last().As<PageLayout>().backAction = null;
		}
	}
}
