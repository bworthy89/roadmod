using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Editor;

public class EditorTool : IEditorTool, IJsonWritable, IUITagProvider
{
	protected ToolSystem m_ToolSystem;

	protected EditorPanelUISystem m_EditorPanelUISystem;

	public string id { get; set; }

	public string icon { get; set; }

	public bool disabled { get; set; }

	public string shortcut { get; set; }

	public string uiTag { get; set; }

	[CanBeNull]
	public IEditorPanel panel { get; set; }

	[CanBeNull]
	public ToolBaseSystem tool { get; set; }

	public bool active
	{
		get
		{
			return IsActive();
		}
		set
		{
			if (value)
			{
				OnEnable();
			}
			else
			{
				OnDisable();
			}
		}
	}

	public EditorTool(World world)
	{
		m_ToolSystem = world.GetOrCreateSystemManaged<ToolSystem>();
		m_EditorPanelUISystem = world.GetOrCreateSystemManaged<EditorPanelUISystem>();
	}

	protected virtual bool IsActive()
	{
		if (m_EditorPanelUISystem.activePanel == panel)
		{
			if (tool != null)
			{
				return m_ToolSystem.activeTool == tool;
			}
			return true;
		}
		return false;
	}

	protected virtual void OnEnable()
	{
		m_ToolSystem.selected = Entity.Null;
		m_EditorPanelUISystem.activePanel = panel;
		if (tool != null)
		{
			m_ToolSystem.activeTool = tool;
		}
	}

	protected virtual void OnDisable()
	{
		if (m_EditorPanelUISystem.activePanel == panel)
		{
			m_EditorPanelUISystem.activePanel = null;
		}
		if (tool != null && m_ToolSystem.activeTool == tool)
		{
			m_ToolSystem.ActivatePrefabTool(null);
		}
	}
}
