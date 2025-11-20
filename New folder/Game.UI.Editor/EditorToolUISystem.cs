using System.Linq;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class EditorToolUISystem : UISystemBase
{
	private const string kGroup = "editorTool";

	private ToolSystem m_ToolSystem;

	private EditorPanelUISystem m_EditorPanelUISystem;

	private InspectorPanelSystem m_InspectorPanelSystem;

	private GetterValueBinding<IEditorTool[]> m_ToolsBinding;

	private IEditorTool[] m_Tools;

	private bool[] m_Disabled;

	private IEditorTool m_ActiveTool;

	private Entity m_LastSelectedEntity;

	public override GameMode gameMode => GameMode.Editor;

	public IEditorTool[] tools
	{
		get
		{
			return m_Tools;
		}
		set
		{
			m_Tools = value;
			m_Disabled = value.Select((IEditorTool t) => t.disabled).ToArray();
		}
	}

	[CanBeNull]
	public IEditorTool activeTool
	{
		get
		{
			return m_ActiveTool;
		}
		set
		{
			if (value != m_ActiveTool || (m_ActiveTool != null && !m_ActiveTool.active))
			{
				if (m_ActiveTool != null)
				{
					m_ActiveTool.active = false;
				}
				m_ActiveTool = value;
				if (m_ActiveTool != null)
				{
					m_ActiveTool.active = true;
				}
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_EditorPanelUISystem = base.World.GetOrCreateSystemManaged<EditorPanelUISystem>();
		m_InspectorPanelSystem = base.World.GetOrCreateSystemManaged<InspectorPanelSystem>();
		tools = new IEditorTool[6]
		{
			new EditorAssetImportTool(base.World),
			new EditorTerrainTool(base.World),
			new EditorPrefabTool(base.World),
			new EditorPrefabEditorTool(base.World),
			new EditorPhotoTool(base.World),
			new EditorBulldozeTool(base.World)
		};
		AddUpdateBinding(m_ToolsBinding = new GetterValueBinding<IEditorTool[]>("editorTool", "tools", () => tools, new ArrayWriter<IEditorTool>(new ValueWriter<IEditorTool>())));
		AddUpdateBinding(new GetterValueBinding<string>("editorTool", "activeTool", () => activeTool?.id, ValueWriters.Nullable(new StringWriter())));
		AddBinding(new TriggerBinding<string>("editorTool", "selectTool", SelectTool));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (activeTool != null && !activeTool.active)
		{
			activeTool = null;
		}
		if (m_ToolSystem.selected != m_LastSelectedEntity)
		{
			SelectEntity(m_ToolSystem.selected);
		}
		if (UpdateToolState())
		{
			m_ToolsBinding.TriggerUpdate();
		}
	}

	public void SelectEntity(Entity entity)
	{
		m_LastSelectedEntity = entity;
		if (m_InspectorPanelSystem.SelectEntity(entity))
		{
			activeTool = null;
			m_EditorPanelUISystem.activePanel = m_InspectorPanelSystem;
		}
		else if (m_EditorPanelUISystem.activePanel == m_InspectorPanelSystem)
		{
			m_EditorPanelUISystem.activePanel = null;
		}
	}

	public void SelectEntitySubMesh(Entity entity, int subMeshIndex)
	{
		m_LastSelectedEntity = entity;
		if (m_InspectorPanelSystem.SelectMesh(entity, subMeshIndex))
		{
			activeTool = null;
			m_EditorPanelUISystem.activePanel = m_InspectorPanelSystem;
		}
		else if (m_EditorPanelUISystem.activePanel == m_InspectorPanelSystem)
		{
			m_EditorPanelUISystem.activePanel = null;
		}
	}

	public void SelectTool([CanBeNull] string id)
	{
		activeTool = tools.FirstOrDefault((IEditorTool t) => t.id == id);
	}

	private bool UpdateToolState()
	{
		bool result = false;
		for (int i = 0; i < m_Tools.Length; i++)
		{
			bool disabled = m_Tools[i].disabled;
			if (disabled != m_Disabled[i])
			{
				m_Disabled[i] = disabled;
				result = true;
			}
		}
		return result;
	}

	[Preserve]
	public EditorToolUISystem()
	{
	}
}
