using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game.UI.Editor;
using Game.UI.Widgets;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

public class StandaloneAssetUploadPanelUISystem : UISystemBase
{
	private static readonly string kGroup = "assetUploadPanel";

	private AssetUploadPanelUISystem m_AssetUploadPanelUISystem;

	private WidgetBindings m_WidgetBindings;

	private ValueBinding<bool> m_Visible;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AssetUploadPanelUISystem = base.World.GetOrCreateSystemManaged<AssetUploadPanelUISystem>();
		m_AssetUploadPanelUISystem.Enabled = false;
		AddUpdateBinding(m_WidgetBindings = new WidgetBindings(kGroup));
		EditorPanelUISystem.AddEditorWidgetBindings(m_WidgetBindings);
		AddBinding(m_Visible = new ValueBinding<bool>(kGroup, "visible", initialValue: false));
		AddBinding(new TriggerBinding(kGroup, "close", Close));
	}

	public async Task Show(PdxAssetUploadHandle assetUploadHandle, bool allowManualFileCopy = true)
	{
		try
		{
			await m_AssetUploadPanelUISystem.Show(assetUploadHandle, allowManualFileCopy);
			AssetUploadPanelUISystem assetUploadPanelUISystem = m_AssetUploadPanelUISystem;
			assetUploadPanelUISystem.onChildrenChange = (Action<IList<IWidget>>)Delegate.Remove(assetUploadPanelUISystem.onChildrenChange, new Action<IList<IWidget>>(OnChildrenChange));
			AssetUploadPanelUISystem assetUploadPanelUISystem2 = m_AssetUploadPanelUISystem;
			assetUploadPanelUISystem2.onChildrenChange = (Action<IList<IWidget>>)Delegate.Combine(assetUploadPanelUISystem2.onChildrenChange, new Action<IList<IWidget>>(OnChildrenChange));
			m_AssetUploadPanelUISystem.Enabled = true;
			m_WidgetBindings.children = m_AssetUploadPanelUISystem.children;
			m_Visible.Update(newValue: true);
		}
		catch (Exception exception)
		{
			UISystemBase.log.Error(exception);
		}
	}

	public async Task Show(AssetData mainAsset, bool allowManualFileCopy = true)
	{
		await Show(new PdxAssetUploadHandle(mainAsset), allowManualFileCopy);
	}

	public void Close()
	{
		if (m_AssetUploadPanelUISystem.Close())
		{
			AssetUploadPanelUISystem assetUploadPanelUISystem = m_AssetUploadPanelUISystem;
			assetUploadPanelUISystem.onChildrenChange = (Action<IList<IWidget>>)Delegate.Remove(assetUploadPanelUISystem.onChildrenChange, new Action<IList<IWidget>>(OnChildrenChange));
			m_AssetUploadPanelUISystem.Enabled = false;
			m_WidgetBindings.children = Array.Empty<IWidget>();
			m_Visible.Update(newValue: false);
		}
	}

	private void OnChildrenChange(IList<IWidget> children)
	{
		m_WidgetBindings.children = children;
	}

	[Preserve]
	public StandaloneAssetUploadPanelUISystem()
	{
	}
}
