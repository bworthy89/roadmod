using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Input;
using Game.Prefabs;
using Game.PSI;
using Game.Serialization;
using Game.Settings;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class GamePanelUISystem : UISystemBase, IPreDeserialize
{
	private const string kGroup = "game";

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private SelectedInfoUISystem m_SelectedInfoUISystem;

	private ToolbarUISystem m_ToolbarUISystem;

	private PhotoModeUISystem m_PhotoModeUISystem;

	private EntityQuery m_TransportConfigQuery;

	private InputBarrier m_ToolBarrier;

	private ValueBinding<GamePanel> m_ActivePanelBinding;

	private Dictionary<string, GamePanel> m_defaultArgs;

	public Action<GamePanel> eventPanelOpened;

	public Action<GamePanel> eventPanelClosed;

	private Entity m_PreviousSelectedEntity;

	private InfoviewPrefab m_PreviousInfoview;

	[CanBeNull]
	public GamePanel activePanel => m_ActivePanelBinding.value;

	private bool NeedsClear
	{
		get
		{
			if (m_SelectedInfoUISystem.selectedEntity != Entity.Null)
			{
				return true;
			}
			if (!(activePanel is InfoviewMenu))
			{
				if (m_ToolSystem.activeTool == m_DefaultTool)
				{
					return m_ToolbarUISystem.hasActiveSelection;
				}
				return true;
			}
			return false;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_SelectedInfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_ToolbarUISystem = base.World.GetOrCreateSystemManaged<ToolbarUISystem>();
		m_PhotoModeUISystem = base.World.GetOrCreateSystemManaged<PhotoModeUISystem>();
		m_TransportConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		m_ToolBarrier = InputManager.instance.CreateMapBarrier("Tool", "GamePanelUISystem");
		AddBinding(m_ActivePanelBinding = new ValueBinding<GamePanel>("game", "activePanel", null, ValueWriters.Nullable(new ValueWriter<GamePanel>())));
		AddUpdateBinding(new GetterValueBinding<bool>("game", "blockingPanelActive", () => activePanel?.blocking ?? false));
		AddUpdateBinding(new GetterValueBinding<int>("game", "activePanelPosition", () => (int)((activePanel != null) ? activePanel.position : GamePanel.LayoutPosition.Undefined)));
		AddBinding(new TriggerBinding<string>("game", "togglePanel", TogglePanel));
		AddBinding(new TriggerBinding<string>("game", "showPanel", ShowPanel));
		AddBinding(new TriggerBinding<string>("game", "closePanel", ClosePanel));
		AddBinding(new TriggerBinding("game", "closeActivePanel", CloseActivePanel));
		m_defaultArgs = new Dictionary<string, GamePanel>();
		InitializeDefaults();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (activePanel != null && (NeedsClear || !IsPanelAllowed(activePanel)))
		{
			CloseActivePanel();
		}
		m_ToolBarrier.blocked = activePanel?.blocking ?? false;
	}

	public void PreDeserialize(Context context)
	{
		if (m_ActivePanelBinding.value is PhotoModePanel)
		{
			m_PhotoModeUISystem.Activate(enabled: false);
		}
		m_ActivePanelBinding.Update(null);
	}

	public void SetDefaultArgs(GamePanel defaultArgs)
	{
		m_defaultArgs[defaultArgs.GetType().FullName] = defaultArgs;
	}

	public void TogglePanel([CanBeNull] string panelType)
	{
		GamePanel value = m_ActivePanelBinding.value;
		if (value != null && value.GetType().FullName == panelType)
		{
			m_ActivePanelBinding.Update(null);
			OnPanelChanged(value, null);
		}
		else
		{
			ShowPanel(panelType);
		}
	}

	public void ShowPanel(string panelType)
	{
		if (m_defaultArgs.TryGetValue(panelType, out var value))
		{
			ShowPanel(value);
		}
	}

	public void ShowPanel(GamePanel panel)
	{
		if (IsPanelAllowed(panel))
		{
			GamePanel value = m_ActivePanelBinding.value;
			m_ActivePanelBinding.Update(panel);
			OnPanelChanged(value, panel);
		}
	}

	public void ClosePanel(string panelType)
	{
		GamePanel value = m_ActivePanelBinding.value;
		if (value != null && value.GetType().FullName == panelType)
		{
			m_ActivePanelBinding.Update(null);
			OnPanelChanged(value, null);
		}
	}

	private void CloseActivePanel()
	{
		GamePanel value = m_ActivePanelBinding.value;
		if (value != null)
		{
			m_ActivePanelBinding.Update(null);
			OnPanelChanged(value, null);
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		InitializeDefaults();
	}

	private void InitializeDefaults()
	{
		SetDefaultArgs(new InfoviewMenu());
		SetDefaultArgs(new ProgressionPanel());
		AddBinding(new TriggerBinding<int>("game", "showProgressionPanel", ShowPanel<ProgressionPanel>));
		SetDefaultArgs(new EconomyPanel());
		AddBinding(new TriggerBinding<int>("game", "showEconomyPanel", ShowPanel<EconomyPanel>));
		SetDefaultArgs(new CityInfoPanel());
		AddBinding(new TriggerBinding<int>("game", "showCityInfoPanel", ShowPanel<CityInfoPanel>));
		SetDefaultArgs(new StatisticsPanel());
		SetDefaultArgs(new TransportationOverviewPanel());
		AddBinding(new TriggerBinding<int>("game", "showTransportationOverviewPanel", ShowPanel<TransportationOverviewPanel>));
		SetDefaultArgs(new ChirperPanel());
		SetDefaultArgs(new LifePathPanel());
		AddBinding(new TriggerBinding<Entity>("game", "showLifePathDetail", ShowPanel<LifePathPanel>));
		SetDefaultArgs(new JournalPanel());
		SetDefaultArgs(new RadioPanel());
		SetDefaultArgs(new PhotoModePanel());
		SetDefaultArgs(new CinematicCameraPanel());
		SetDefaultArgs(new NotificationsPanel());
	}

	public void ShowPanel<T>(int tab) where T : TabbedGamePanel, new()
	{
		ShowPanel(new T
		{
			selectedTab = tab
		});
	}

	public void ShowPanel<T>(Entity selectedEntity) where T : EntityGamePanel, new()
	{
		ShowPanel(new T
		{
			selectedEntity = selectedEntity
		});
	}

	private bool IsPanelAllowed(GamePanel panel)
	{
		if (panel is RadioPanel)
		{
			return SharedSettings.instance.audio.radioActive;
		}
		if (panel is LifePathPanel lifePathPanel && lifePathPanel.selectedEntity != Entity.Null)
		{
			return base.EntityManager.HasComponent<Followed>(lifePathPanel.selectedEntity);
		}
		return true;
	}

	private void OnPanelChanged([CanBeNull] GamePanel previous, [CanBeNull] GamePanel next)
	{
		if (previous != null && (next == null || next.GetType() != previous.GetType()))
		{
			eventPanelClosed?.Invoke(previous);
			OnPanelClosed(previous);
			if (next == null)
			{
				if (!(previous is InfoviewMenu))
				{
					m_ToolSystem.infoview = ((m_PreviousInfoview != null) ? m_PreviousInfoview : m_ToolSystem.infoview);
					m_PreviousInfoview = null;
				}
				if (m_SelectedInfoUISystem.selectedEntity == Entity.Null)
				{
					m_SelectedInfoUISystem.SetSelection(m_PreviousSelectedEntity);
					m_PreviousSelectedEntity = Entity.Null;
				}
			}
		}
		if (next != null && (previous == null || next.GetType() != previous.GetType()))
		{
			OnPanelOpened(next);
			eventPanelOpened?.Invoke(next);
		}
	}

	private void OnPanelOpened(GamePanel panel)
	{
		m_PreviousInfoview = m_ToolSystem.activeInfoview;
		if (panel is PhotoModePanel)
		{
			m_PhotoModeUISystem.Activate(enabled: true);
		}
		if (!(panel is InfoviewMenu))
		{
			m_ToolSystem.activeTool = m_DefaultTool;
			m_ToolbarUISystem.ClearAssetSelection();
		}
		if (panel is TransportationOverviewPanel && TryGetTransportConfig(out var config))
		{
			m_ToolSystem.infoview = config.m_TransportInfoview;
		}
		Telemetry.PanelOpened(panel);
		if (panel.retainSelection)
		{
			m_PreviousSelectedEntity = m_SelectedInfoUISystem.selectedEntity;
		}
		m_SelectedInfoUISystem.SetSelection(Entity.Null);
	}

	private void OnPanelClosed(GamePanel panel)
	{
		if (panel is PhotoModePanel)
		{
			m_PhotoModeUISystem.Activate(enabled: false);
		}
		Telemetry.PanelClosed(panel);
		if (panel.retainProperties)
		{
			SetDefaultArgs(panel);
		}
	}

	private bool TryGetTransportConfig(out UITransportConfigurationPrefab config)
	{
		return m_PrefabSystem.TryGetSingletonPrefab<UITransportConfigurationPrefab>(m_TransportConfigQuery, out config);
	}

	[Preserve]
	public GamePanelUISystem()
	{
	}
}
