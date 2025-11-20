using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.Debug;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.UI.Debug;

public class DebugUISystem : UISystemBase
{
	private class Panel : IJsonWritable, IDisposable
	{
		public readonly DebugUI.Panel panel;

		[CanBeNull]
		private string displayName;

		private bool childrenDirty;

		public List<IWidget> children { get; set; }

		public Panel(DebugUI.Panel panel)
		{
			this.panel = panel;
			displayName = panel.displayName;
			children = new List<IWidget>();
			childrenDirty = true;
			panel.onSetDirty += OnSetDirty;
		}

		private void OnSetDirty(DebugUI.Panel panel)
		{
			childrenDirty = true;
		}

		public bool Update()
		{
			bool result = false;
			if (panel.displayName != displayName)
			{
				result = true;
				displayName = panel.displayName;
			}
			if (childrenDirty)
			{
				childrenDirty = false;
				result = true;
				children.Clear();
				children.AddRange(DebugWidgetBuilders.BuildWidgets(panel.children));
			}
			return result;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("displayName");
			writer.Write(displayName);
			writer.TypeEnd();
		}

		public void Dispose()
		{
			panel.onSetDirty -= OnSetDirty;
		}
	}

	private const string kGroup = "debug";

	private ValueBinding<bool> m_EnabledBinding;

	private ValueBinding<bool> m_VisibleBinding;

	private ValueBinding<Panel> m_SelectedPanelBinding;

	private WidgetBindings m_WidgetBindings;

	private ValueBinding<IDebugBinding> m_ObservedBindingBinding;

	private EventBinding<IDebugBinding> m_BindingTriggeredBinding;

	private GetterValueBinding<List<DebugWatchSystem.Watch>> m_WatchesBinding;

	private string m_SelectedPanel;

	private bool m_ShowDeveloperInfo;

	public bool visible => m_VisibleBinding.value;

	[CanBeNull]
	public IDebugBinding observedBinding
	{
		get
		{
			return m_ObservedBindingBinding.value;
		}
		set
		{
			m_ObservedBindingBinding.Update(value);
		}
	}

	private string selectedPanel
	{
		get
		{
			return m_SelectedPanel;
		}
		set
		{
			m_SelectedPanel = value;
		}
	}

	public bool developerInfoVisible
	{
		get
		{
			return m_ShowDeveloperInfo;
		}
		set
		{
			m_ShowDeveloperInfo = value;
		}
	}

	private static IEnumerable<DebugUI.Panel> visiblePanels => DebugManager.instance.panels.Where((DebugUI.Panel panel) => !panel.isEditorOnly && panel.children.Any((DebugUI.Widget x) => !x.isEditorOnly));

	private static bool debugSystemEnabled => (GameManager.instance?.configuration)?.developerMode ?? false;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_EnabledBinding = new ValueBinding<bool>("debug", "enabled", debugSystemEnabled));
		AddBinding(m_VisibleBinding = new ValueBinding<bool>("debug", "visible", initialValue: false));
		AddUpdateBinding(new GetterValueBinding<List<string>>("debug", "panels", GetPanels, new ListWriter<string>()));
		AddUpdateBinding(new GetterValueBinding<int>("debug", "selectedIndex", () => GetPanelIndex(selectedPanel)));
		AddBinding(m_SelectedPanelBinding = new ValueBinding<Panel>("debug", "selectedPanel", null, ValueWriters.Nullable(new ValueWriter<Panel>())));
		AddUpdateBinding(m_WidgetBindings = new WidgetBindings("debug"));
		m_WidgetBindings.AddDefaultBindings();
		AddBinding(m_ObservedBindingBinding = new ValueBinding<IDebugBinding>("debug", "observedBinding", null, ValueWriters.Nullable(new DebugBindingWriter())));
		AddBinding(m_BindingTriggeredBinding = new EventBinding<IDebugBinding>("debug", "bindingTriggered", new DebugBindingWriter()));
		AddUpdateBinding(new GetterValueBinding<bool>("debug", "developerInfoVisible", () => developerInfoVisible));
		AddBinding(m_WatchesBinding = new GetterValueBinding<List<DebugWatchSystem.Watch>>("debug", "watches", () => base.World.GetOrCreateSystemManaged<DebugWatchSystem>().watches, new ListWriter<DebugWatchSystem.Watch>(new ValueWriter<DebugWatchSystem.Watch>())));
		AddBinding(new TriggerBinding("debug", "show", Show));
		AddBinding(new TriggerBinding("debug", "hide", Hide));
		AddBinding(new TriggerBinding<int>("debug", "selectPanel", SelectPanel));
		AddBinding(new TriggerBinding("debug", "selectPreviousPanel", SelectPreviousPanel));
		AddBinding(new TriggerBinding("debug", "selectNextPanel", SelectNextPanel));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (visible)
		{
			UpdateSelectedPanel();
			DebugWatchSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<DebugWatchSystem>();
			if (orCreateSystemManaged.watchesChanged)
			{
				m_WatchesBinding.TriggerUpdate();
				orCreateSystemManaged.ClearWatchesChanged();
			}
		}
		base.OnUpdate();
	}

	public void Trigger(IDebugBinding binding)
	{
		m_BindingTriggeredBinding.Trigger(binding);
	}

	public void Show()
	{
		if (visible || !m_EnabledBinding.value)
		{
			return;
		}
		m_VisibleBinding.Update(newValue: true);
		if (!SharedSettings.instance.userInterface.dismissedConfirmations.Contains("DebugMenu") && PlatformManager.instance.achievementsEnabled)
		{
			GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new DismissibleConfirmationDialog("Common.DIALOG_TITLE[Warning]", "Common.DIALOG_MESSAGE[DisableAchievements]", "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]"), delegate(int msg, bool dismiss)
			{
				if (msg == 0)
				{
					if (dismiss)
					{
						SharedSettings.instance.userInterface.dismissedConfirmations.Add("DebugMenu");
						SharedSettings.instance.userInterface.ApplyAndSave();
					}
					base.World.GetOrCreateSystemManaged<DebugSystem>().Enabled = true;
					PlatformManager.instance.achievementsEnabled = false;
				}
			});
		}
		else
		{
			base.World.GetOrCreateSystemManaged<DebugSystem>().Enabled = true;
			PlatformManager.instance.achievementsEnabled = false;
		}
		UpdateSelectedPanel();
	}

	public void Hide()
	{
		if (visible)
		{
			m_VisibleBinding.Update(newValue: false);
			base.World.GetOrCreateSystemManaged<DebugSystem>().Enabled = false;
			UpdateSelectedPanel();
		}
	}

	private void SelectPanel(int index)
	{
		if (visible)
		{
			selectedPanel = GetPanel(index)?.displayName;
			UpdateSelectedPanel();
		}
	}

	private void SelectPreviousPanel()
	{
		if (!visible)
		{
			return;
		}
		int panelCount = GetPanelCount();
		if (panelCount != 0)
		{
			int panelIndex = GetPanelIndex(selectedPanel);
			if (panelIndex <= 0 || panelIndex >= panelCount)
			{
				SelectPanel(panelCount - 1);
			}
			else
			{
				SelectPanel(panelIndex - 1);
			}
		}
	}

	private void SelectNextPanel()
	{
		if (!visible)
		{
			return;
		}
		int panelCount = GetPanelCount();
		if (panelCount != 0)
		{
			int panelIndex = GetPanelIndex(selectedPanel);
			if (panelIndex < 0 || panelIndex >= panelCount - 1)
			{
				SelectPanel(0);
			}
			else
			{
				SelectPanel(panelIndex + 1);
			}
		}
	}

	private List<string> GetPanels()
	{
		return visiblePanels.Select((DebugUI.Panel p) => p.displayName).ToList();
	}

	private void UpdateSelectedPanel()
	{
		Panel value = m_SelectedPanelBinding.value;
		DebugUI.Panel obj = value?.panel;
		DebugUI.Panel panel = (visible ? GetPanel(GetPanelIndex(selectedPanel)) : null);
		if (obj != panel)
		{
			value?.Dispose();
			m_SelectedPanelBinding.Update((panel != null) ? new Panel(panel) : null);
			m_WidgetBindings.children = ((m_SelectedPanelBinding.value != null) ? m_SelectedPanelBinding.value.children : new List<IWidget>());
		}
		else if (value != null && value.Update())
		{
			m_SelectedPanelBinding.TriggerUpdate();
			m_WidgetBindings.children = ((m_SelectedPanelBinding.value != null) ? m_SelectedPanelBinding.value.children : new List<IWidget>());
		}
	}

	private static int GetPanelCount()
	{
		return visiblePanels.Count();
	}

	[CanBeNull]
	private static DebugUI.Panel GetPanel(int panelIndex)
	{
		if (panelIndex < 0)
		{
			return null;
		}
		return visiblePanels.Skip(panelIndex).FirstOrDefault();
	}

	private static int GetPanelIndex(string name)
	{
		return visiblePanels.ToList().FindIndex((DebugUI.Panel panel) => panel.displayName == name);
	}

	[Preserve]
	public DebugUISystem()
	{
	}
}
