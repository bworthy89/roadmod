using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.Logging;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public abstract class EditorPanelSystemBase : GameSystemBase, IEditorPanel
{
	[CanBeNull]
	private IEditorPanel m_LastSubPanel;

	[CanBeNull]
	protected IEditorPanel activeSubPanel { get; set; }

	protected virtual LocalizedString title { get; set; }

	protected virtual IList<IWidget> children { get; set; } = Array.Empty<IWidget>();

	public virtual EditorPanelWidgetRenderer widgetRenderer => EditorPanelWidgetRenderer.Editor;

	protected ILog log { get; } = LogManager.GetLogger("Editor");

	LocalizedString IEditorPanel.title
	{
		get
		{
			if (activeSubPanel == null)
			{
				return title;
			}
			return activeSubPanel.title;
		}
	}

	IList<IWidget> IEditorPanel.children
	{
		get
		{
			if (activeSubPanel == null)
			{
				return children;
			}
			return activeSubPanel.children;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (activeSubPanel != m_LastSubPanel)
		{
			if (m_LastSubPanel is ComponentSystemBase componentSystemBase)
			{
				componentSystemBase.Enabled = false;
				componentSystemBase.Update();
			}
			m_LastSubPanel = activeSubPanel;
			if (activeSubPanel is ComponentSystemBase componentSystemBase2)
			{
				componentSystemBase2.Enabled = true;
			}
		}
		if (activeSubPanel is ComponentSystemBase componentSystemBase3)
		{
			componentSystemBase3.Update();
		}
	}

	protected virtual void OnValueChanged(IWidget widget)
	{
	}

	protected virtual bool OnCancel()
	{
		return OnClose();
	}

	protected virtual bool OnClose()
	{
		return true;
	}

	public void CloseSubPanel()
	{
		activeSubPanel = null;
	}

	void IEditorPanel.OnValueChanged(IWidget widget)
	{
		if (activeSubPanel != null)
		{
			activeSubPanel.OnValueChanged(widget);
		}
		else
		{
			OnValueChanged(widget);
		}
	}

	bool IEditorPanel.OnCancel()
	{
		if (activeSubPanel != null)
		{
			if (activeSubPanel.OnCancel())
			{
				activeSubPanel = null;
			}
			return false;
		}
		return OnCancel();
	}

	bool IEditorPanel.OnClose()
	{
		if (activeSubPanel != null)
		{
			if (activeSubPanel.OnClose())
			{
				activeSubPanel = null;
			}
			return false;
		}
		return OnClose();
	}

	[Preserve]
	protected EditorPanelSystemBase()
	{
	}
}
