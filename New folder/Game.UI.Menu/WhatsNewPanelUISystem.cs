using System.Collections.Generic;
using System.Runtime.InteropServices;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Settings;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

public class WhatsNewPanelUISystem : UISystemBase
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct DlcComparer : IComparer<(UIWhatsNewPanelPrefab, DlcId dlcId)>
	{
		public int Compare((UIWhatsNewPanelPrefab, DlcId dlcId) a, (UIWhatsNewPanelPrefab, DlcId dlcId) b)
		{
			return a.dlcId.CompareTo(b.dlcId);
		}
	}

	private const string kGroup = "whatsnew";

	private RawValueBinding m_PanelBinding;

	private ValueBinding<bool> m_VisibilityBinding;

	private ValueBinding<int> m_InitialTabBinding;

	private EntityQuery m_Query;

	private PrefabSystem m_PrefabSystem;

	private static DlcComparer kDlcComparer;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		AddBinding(m_PanelBinding = new RawValueBinding("whatsnew", "panel", BindPanel));
		AddBinding(m_VisibilityBinding = new ValueBinding<bool>("whatsnew", "visible", initialValue: false));
		AddBinding(m_InitialTabBinding = new ValueBinding<int>("whatsnew", "initialTab", 0));
		AddBinding(new TriggerBinding<bool>("whatsnew", "close", OnClose));
		m_Query = GetEntityQuery(ComponentType.ReadOnly<UIWhatsNewPanelPrefabData>());
	}

	private void OnClose(bool dismiss)
	{
		m_VisibilityBinding.Update(newValue: false);
		foreach (var sortedWhatsNewTab in GetSortedWhatsNewTabs(m_Query))
		{
			string dlcName = PlatformManager.instance.GetDlcName(sortedWhatsNewTab.dlcId);
			if (!SharedSettings.instance.userState.seenWhatsNew.Contains(dlcName))
			{
				SharedSettings.instance.userState.seenWhatsNew.Add(dlcName);
			}
		}
		if (dismiss)
		{
			SharedSettings.instance.userInterface.showWhatsNewPanel = false;
		}
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
	{
		if (mode != GameMode.MainMenu)
		{
			return;
		}
		List<(UIWhatsNewPanelPrefab, DlcId)> sortedWhatsNewTabs = GetSortedWhatsNewTabs(m_Query);
		DlcId initialTab = new DlcId(int.MaxValue);
		bool flag = false;
		foreach (var item in sortedWhatsNewTabs)
		{
			if (!SharedSettings.instance.userState.seenWhatsNew.Contains(PlatformManager.instance.GetDlcName(item.Item2)))
			{
				if (item.Item2 < initialTab)
				{
					initialTab = item.Item2;
				}
				flag = true;
			}
		}
		if (flag)
		{
			int num = sortedWhatsNewTabs.FindIndex(((UIWhatsNewPanelPrefab prefab, DlcId dlcId) p) => p.dlcId == initialTab);
			m_InitialTabBinding.Update((num >= 0) ? num : (sortedWhatsNewTabs.Count - 1));
		}
		else
		{
			m_InitialTabBinding.Update(sortedWhatsNewTabs.Count - 1);
		}
		m_PanelBinding.Update();
		m_VisibilityBinding.Update(flag || SharedSettings.instance.userInterface.showWhatsNewPanel);
	}

	private void BindPanel(IJsonWriter writer)
	{
		List<(UIWhatsNewPanelPrefab, DlcId)> sortedWhatsNewTabs = GetSortedWhatsNewTabs(m_Query);
		writer.ArrayBegin(sortedWhatsNewTabs.Count);
		for (int i = 0; i < sortedWhatsNewTabs.Count; i++)
		{
			UIWhatsNewPanelPrefab item = sortedWhatsNewTabs[i].Item1;
			DlcId item2 = sortedWhatsNewTabs[i].Item2;
			writer.TypeBegin(typeof(UIWhatsNewPanelPrefab).FullName);
			writer.PropertyName("id");
			writer.Write(item2.id);
			writer.PropertyName("dlc");
			writer.Write(PlatformManager.instance.GetDlcName(item2));
			writer.PropertyName("pages");
			writer.ArrayBegin(item.m_Pages.Length);
			for (int j = 0; j < item.m_Pages.Length; j++)
			{
				UIWhatsNewPanelPrefab.UIWhatsNewPanelPage uIWhatsNewPanelPage = item.m_Pages[j];
				writer.TypeBegin(typeof(UIWhatsNewPanelPrefab.UIWhatsNewPanelPage).FullName);
				writer.PropertyName("items");
				writer.ArrayBegin(uIWhatsNewPanelPage.m_Items.Length);
				for (int k = 0; k < uIWhatsNewPanelPage.m_Items.Length; k++)
				{
					UIWhatsNewPanelPrefab.UIWhatsNewPanelPageItem uIWhatsNewPanelPageItem = uIWhatsNewPanelPage.m_Items[k];
					writer.TypeBegin(typeof(UIWhatsNewPanelPrefab.UIWhatsNewPanelPageItem).FullName);
					writer.PropertyName("images");
					writer.ArrayBegin(uIWhatsNewPanelPageItem.m_Images.Length);
					for (int l = 0; l < uIWhatsNewPanelPageItem.m_Images.Length; l++)
					{
						UIWhatsNewPanelPrefab.UIWhatsNewPanelImage uIWhatsNewPanelImage = uIWhatsNewPanelPageItem.m_Images[l];
						writer.TypeBegin(typeof(UIWhatsNewPanelPrefab.UIWhatsNewPanelImage).FullName);
						writer.PropertyName("image");
						writer.Write(uIWhatsNewPanelImage.m_Uri);
						writer.PropertyName("aspectRatio");
						writer.Write(uIWhatsNewPanelImage.m_AspectRatio);
						writer.PropertyName("width");
						writer.Write(uIWhatsNewPanelImage.m_Width);
						writer.TypeEnd();
					}
					writer.ArrayEnd();
					writer.PropertyName("title");
					if (uIWhatsNewPanelPageItem.m_TitleId != null)
					{
						writer.Write(uIWhatsNewPanelPageItem.m_TitleId);
					}
					else
					{
						writer.WriteNull();
					}
					writer.PropertyName("subtitle");
					if (uIWhatsNewPanelPageItem.m_SubTitleId != null)
					{
						writer.Write(uIWhatsNewPanelPageItem.m_SubTitleId);
					}
					else
					{
						writer.WriteNull();
					}
					writer.PropertyName("paragraphs");
					if (uIWhatsNewPanelPageItem.m_ParagraphsId != null)
					{
						writer.Write(uIWhatsNewPanelPageItem.m_ParagraphsId);
					}
					else
					{
						writer.WriteNull();
					}
					writer.PropertyName("justify");
					writer.Write((int)uIWhatsNewPanelPageItem.m_Justify);
					writer.PropertyName("width");
					writer.Write(uIWhatsNewPanelPageItem.m_Width);
					writer.TypeEnd();
				}
				writer.ArrayEnd();
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	private List<(UIWhatsNewPanelPrefab prefab, DlcId dlcId)> GetSortedWhatsNewTabs(EntityQuery query)
	{
		NativeArray<Entity> nativeArray = query.ToEntityArray(Allocator.Temp);
		List<(UIWhatsNewPanelPrefab, DlcId)> list = new List<(UIWhatsNewPanelPrefab, DlcId)>();
		foreach (Entity item in nativeArray)
		{
			if (m_PrefabSystem.TryGetPrefab<UIWhatsNewPanelPrefab>(item, out var prefab) && prefab.TryGet<ContentPrerequisite>(out var component) && component.m_ContentPrerequisite != null && component.m_ContentPrerequisite.TryGet<DlcRequirement>(out var component2))
			{
				list.Add((prefab, component2.m_Dlc));
			}
		}
		list.Sort(kDlcComparer);
		return list;
	}

	[Preserve]
	public WhatsNewPanelUISystem()
	{
	}
}
