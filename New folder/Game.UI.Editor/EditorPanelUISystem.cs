using System;
using Colossal.Annotations;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class EditorPanelUISystem : UISystemBase
{
	private const string kGroup = "editorPanel";

	[CanBeNull]
	private IEditorPanel m_LastPanel;

	private ValueBinding<bool> m_ActiveBinding;

	private ValueBinding<LocalizedString?> m_TitleBinding;

	private WidgetBindings m_WidgetBindings;

	public override GameMode gameMode => GameMode.GameOrEditor;

	[CanBeNull]
	public IEditorPanel activePanel { get; set; }

	private EditorPanelWidgetRenderer widgetRenderer
	{
		get
		{
			if (activePanel != null)
			{
				return activePanel.widgetRenderer;
			}
			return EditorPanelWidgetRenderer.Editor;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_ActiveBinding = new ValueBinding<bool>("editorPanel", "active", initialValue: false));
		AddBinding(m_TitleBinding = new ValueBinding<LocalizedString?>("editorPanel", "title", null, ValueWritersStruct.Nullable(new ValueWriter<LocalizedString>())));
		AddUpdateBinding(new GetterValueBinding<int>("editorPanel", "width", GetWidth));
		AddUpdateBinding(new GetterValueBinding<int>("editorPanel", "widgetRenderer", () => (int)widgetRenderer));
		AddUpdateBinding(m_WidgetBindings = new WidgetBindings("editorPanel"));
		AddEditorWidgetBindings(m_WidgetBindings);
		m_WidgetBindings.EventValueChanged += OnValueChanged;
		AddBinding(new TriggerBinding("editorPanel", "cancel", Cancel));
		AddBinding(new TriggerBinding("editorPanel", "close", Close));
		AddBinding(new TriggerBinding<int>("editorPanel", "setWidth", SetWidth));
	}

	public static void AddEditorWidgetBindings(WidgetBindings widgetBindings)
	{
		widgetBindings.AddDefaultBindings();
		widgetBindings.AddBindings<EditorSection.Bindings>();
		widgetBindings.AddBindings<SeasonsField.Bindings>();
		widgetBindings.AddBindings<IItemPicker.Bindings>();
		widgetBindings.AddBindings<PopupSearchField.Bindings>();
		widgetBindings.AddBindings<AnimationCurveField.Bindings>();
		widgetBindings.AddBindings<LocalizationField.Bindings>();
		widgetBindings.AddBindings<FilterMenu.Bindings>();
		widgetBindings.AddBindings<HierarchyMenu.Bindings>();
		widgetBindings.AddBindings<ExternalLinkField.Bindings>();
		widgetBindings.AddBindings<ListField.Bindings>();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		activePanel = null;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (activePanel != m_LastPanel)
		{
			if (m_LastPanel is ComponentSystemBase componentSystemBase)
			{
				componentSystemBase.Enabled = false;
				componentSystemBase.Update();
			}
			m_LastPanel = activePanel;
			if (activePanel is ComponentSystemBase componentSystemBase2)
			{
				componentSystemBase2.Enabled = true;
			}
		}
		if (activePanel != null)
		{
			if (activePanel is ComponentSystemBase componentSystemBase3)
			{
				componentSystemBase3.Update();
			}
			m_ActiveBinding.Update(newValue: true);
			m_TitleBinding.Update(activePanel.title);
			m_WidgetBindings.children = activePanel.children;
		}
		else
		{
			m_ActiveBinding.Update(newValue: false);
			m_TitleBinding.Update(null);
			m_WidgetBindings.children = Array.Empty<IWidget>();
		}
		base.OnUpdate();
	}

	public void OnValueChanged(IWidget widget)
	{
		activePanel?.OnValueChanged(widget);
	}

	private int GetWidth()
	{
		return (SharedSettings.instance?.editor)?.inspectorWidth ?? 450;
	}

	private void SetWidth(int width)
	{
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.inspectorWidth = width;
		}
	}

	private void Cancel()
	{
		if (activePanel != null && activePanel.OnCancel())
		{
			activePanel = null;
		}
	}

	private void Close()
	{
		if (activePanel != null && activePanel.OnClose())
		{
			activePanel = null;
		}
	}

	[Preserve]
	public EditorPanelUISystem()
	{
	}
}
