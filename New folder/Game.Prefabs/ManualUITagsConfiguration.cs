using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class ManualUITagsConfiguration : PrefabBase
{
	[Header("Chirper")]
	public UITagPrefab m_ChirperPanel;

	public UITagPrefab m_ChirperPanelButton;

	public UITagPrefab m_ChirperPanelChirps;

	[Header("City Info Panel")]
	public UITagPrefab m_CityInfoPanel;

	public UITagPrefab m_CityInfoPanelButton;

	public UITagPrefab m_CityInfoPanelDemandPage;

	public UITagPrefab m_CityInfoPanelDemandTab;

	public UITagPrefab m_CityInfoPanelPoliciesPage;

	public UITagPrefab m_CityInfoPanelPoliciesTab;

	[Header("Economy Panel")]
	public UITagPrefab m_EconomyPanelBudgetBalance;

	public UITagPrefab m_EconomyPanelBudgetExpenses;

	public UITagPrefab m_EconomyPanelBudgetPage;

	public UITagPrefab m_EconomyPanelBudgetRevenue;

	public UITagPrefab m_EconomyPanelBudgetTab;

	public UITagPrefab m_EconomyPanelButton;

	public UITagPrefab m_EconomyPanelLoansAccept;

	public UITagPrefab m_EconomyPanelLoansPage;

	public UITagPrefab m_EconomyPanelLoansSlider;

	public UITagPrefab m_EconomyPanelLoansTab;

	public UITagPrefab m_EconomyPanelProductionPage;

	public UITagPrefab m_EconomyPanelProductionResources;

	public UITagPrefab m_EconomyPanelProductionDiagram;

	public UITagPrefab m_EconomyPanelProductionData;

	public UITagPrefab m_EconomyPanelProductionTab;

	public UITagPrefab m_EconomyPanelServicesBudget;

	public UITagPrefab m_EconomyPanelServicesList;

	public UITagPrefab m_EconomyPanelServicesPage;

	public UITagPrefab m_EconomyPanelServicesTab;

	public UITagPrefab m_EconomyPanelTaxationEstimate;

	public UITagPrefab m_EconomyPanelTaxationPage;

	public UITagPrefab m_EconomyPanelTaxationRate;

	public UITagPrefab m_EconomyPanelTaxationTab;

	public UITagPrefab m_EconomyPanelTaxationType;

	[Header("Event Journal")]
	public UITagPrefab m_EventJournalPanel;

	public UITagPrefab m_EventJournalPanelButton;

	[Header("Infoviews")]
	public UITagPrefab m_InfoviewsButton;

	public UITagPrefab m_InfoviewsMenu;

	public UITagPrefab m_InfoviewsPanel;

	public UITagPrefab m_InfoviewsFireHazard;

	[Header("Life Path Panel")]
	public UITagPrefab m_LifePathPanel;

	public UITagPrefab m_LifePathPanelBackButton;

	public UITagPrefab m_LifePathPanelButton;

	public UITagPrefab m_LifePathPanelChirps;

	public UITagPrefab m_LifePathPanelDetails;

	[Header("Map Tile Panel")]
	public UITagPrefab m_MapTilePanel;

	public UITagPrefab m_MapTilePanelButton;

	public UITagPrefab m_MapTilePanelResources;

	public UITagPrefab m_MapTilePanelPurchase;

	[Header("Photo Mode Panel")]
	public UITagPrefab m_PhotoModePanel;

	public UITagPrefab m_PhotoModePanelButton;

	public UITagPrefab m_PhotoModePanelHideUI;

	public UITagPrefab m_PhotoModePanelTakePicture;

	public UITagPrefab m_PhotoModeTab;

	public UITagPrefab m_PhotoModePanelTitle;

	public UITagPrefab m_PhotoModeCinematicCameraToggle;

	[Header("Cinematic Camera Panel")]
	public UITagPrefab m_CinematicCameraPanel;

	public UITagPrefab m_CinematicCameraPanelCaptureKey;

	public UITagPrefab m_CinematicCameraPanelPlay;

	public UITagPrefab m_CinematicCameraPanelStop;

	public UITagPrefab m_CinematicCameraPanelHideUI;

	public UITagPrefab m_CinematicCameraPanelSaveLoad;

	public UITagPrefab m_CinematicCameraPanelReset;

	public UITagPrefab m_CinematicCameraPanelTimelineSlider;

	public UITagPrefab m_CinematicCameraPanelTransformCurves;

	public UITagPrefab m_CinematicCameraPanelPropertyCurves;

	public UITagPrefab m_CinematicCameraPanelPlaybackDurationSlider;

	[Header("Progression Panel")]
	public UITagPrefab m_ProgressionPanel;

	public UITagPrefab m_ProgressionPanelButton;

	public UITagPrefab m_ProgressionPanelDevelopmentNode;

	public UITagPrefab m_ProgressionPanelDevelopmentPage;

	public UITagPrefab m_ProgressionPanelDevelopmentService;

	public UITagPrefab m_ProgressionPanelDevelopmentTab;

	public UITagPrefab m_ProgressionPanelDevelopmentUnlockableNode;

	public UITagPrefab m_ProgressionPanelDevelopmentUnlockNode;

	public UITagPrefab m_ProgressionPanelMilestoneRewards;

	public UITagPrefab m_ProgressionPanelMilestoneRewardsMoney;

	public UITagPrefab m_ProgressionPanelMilestoneRewardsDevPoints;

	public UITagPrefab m_ProgressionPanelMilestoneRewardsMapTiles;

	public UITagPrefab m_ProgressionPanelMilestonesList;

	public UITagPrefab m_ProgressionPanelMilestonesPage;

	public UITagPrefab m_ProgressionPanelMilestonesTab;

	public UITagPrefab m_ProgressionPanelMilestoneXP;

	[Header("Radio Panel")]
	public UITagPrefab m_RadioPanel;

	public UITagPrefab m_RadioPanelAdsToggle;

	public UITagPrefab m_RadioPanelButton;

	public UITagPrefab m_RadioPanelNetworks;

	public UITagPrefab m_RadioPanelStations;

	public UITagPrefab m_RadioPanelVolumeSlider;

	[Header("Statistics Panel")]
	public UITagPrefab m_StatisticsPanel;

	public UITagPrefab m_StatisticsPanelButton;

	public UITagPrefab m_StatisticsPanelMenu;

	public UITagPrefab m_StatisticsPanelTimeScale;

	[Header("Toolbar")]
	public UITagPrefab m_ToolbarBulldozerBar;

	public UITagPrefab m_ToolbarDemand;

	public UITagPrefab m_ToolbarSimulationDateTime;

	public UITagPrefab m_ToolbarSimulationSpeed;

	public UITagPrefab m_ToolbarSimulationToggle;

	public UITagPrefab m_ToolbarUnderground;

	[Header("Tool Options")]
	public UITagPrefab m_ToolOptions;

	public UITagPrefab m_ToolOptionsBrushSize;

	public UITagPrefab m_ToolOptionsBrushStrength;

	public UITagPrefab m_ToolOptionsElevation;

	public UITagPrefab m_ToolOptionsElevationDecrease;

	public UITagPrefab m_ToolOptionsElevationIncrease;

	public UITagPrefab m_ToolOptionsElevationStep;

	public UITagPrefab m_ToolOptionsModes;

	public UITagPrefab m_ToolOptionsModesComplexCurve;

	public UITagPrefab m_ToolOptionsModesContinuous;

	public UITagPrefab m_ToolOptionsModesGrid;

	public UITagPrefab m_ToolOptionsModesReplace;

	public UITagPrefab m_ToolOptionsModesSimpleCurve;

	public UITagPrefab m_ToolOptionsModesStraight;

	public UITagPrefab m_ToolOptionsParallelMode;

	public UITagPrefab m_ToolOptionsParallelModeOffset;

	public UITagPrefab m_ToolOptionsParallelModeOffsetDecrease;

	public UITagPrefab m_ToolOptionsParallelModeOffsetIncrease;

	public UITagPrefab m_ToolOptionsSnapping;

	public UITagPrefab m_ToolOptionsThemes;

	public UITagPrefab m_ToolOptionsAssetPacks;

	public UITagPrefab m_ToolOptionsUnderground;

	[Header("Transportation Overview Panel")]
	public UITagPrefab m_TransportationOverviewPanel;

	public UITagPrefab m_TransportationOverviewPanelButton;

	public UITagPrefab m_TransportationOverviewPanelLegend;

	public UITagPrefab m_TransportationOverviewPanelLines;

	public UITagPrefab m_TransportationOverviewPanelTabCargo;

	public UITagPrefab m_TransportationOverviewPanelTabPublicTransport;

	public UITagPrefab m_TransportationOverviewPanelTransportTypes;

	[Header("Selected Info Panel")]
	public UITagPrefab m_SelectedInfoPanel;

	public UITagPrefab m_SelectedInfoPanelTitle;

	public UITagPrefab m_SelectedInfoPanelPolicies;

	public UITagPrefab m_SelectedInfoPanelDelete;

	[Header("General")]
	public UITagPrefab m_PauseMenuButton;

	public UITagPrefab m_UpgradeGrid;

	public UITagPrefab m_AssetGrid;

	public UITagPrefab m_ActionHints;

	[Header("Editor")]
	public UITagPrefab m_AssetImportButton;

	public UITagPrefab m_EditorInfoViewsPanel;

	public UITagPrefab m_ResetTODButton;

	public UITagPrefab m_SimulationPlayButton;

	public UITagPrefab m_TutorialsToggle;

	public UITagPrefab m_WorkspaceTitleBar;

	public UITagPrefab m_SelectProjectRoot;

	public UITagPrefab m_SelectAssets;

	public UITagPrefab m_SelectTemplate;

	public UITagPrefab m_ImportButton;

	public UITagPrefab m_ModifyTerrainButton;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ChirperPanel);
		prefabs.Add(m_ChirperPanelButton);
		prefabs.Add(m_ChirperPanelChirps);
		prefabs.Add(m_CityInfoPanel);
		prefabs.Add(m_CityInfoPanelButton);
		prefabs.Add(m_CityInfoPanelDemandPage);
		prefabs.Add(m_CityInfoPanelDemandTab);
		prefabs.Add(m_CityInfoPanelPoliciesPage);
		prefabs.Add(m_CityInfoPanelPoliciesTab);
		prefabs.Add(m_EconomyPanelBudgetBalance);
		prefabs.Add(m_EconomyPanelBudgetExpenses);
		prefabs.Add(m_EconomyPanelBudgetPage);
		prefabs.Add(m_EconomyPanelBudgetRevenue);
		prefabs.Add(m_EconomyPanelBudgetTab);
		prefabs.Add(m_EconomyPanelButton);
		prefabs.Add(m_EconomyPanelLoansAccept);
		prefabs.Add(m_EconomyPanelLoansPage);
		prefabs.Add(m_EconomyPanelLoansSlider);
		prefabs.Add(m_EconomyPanelLoansTab);
		prefabs.Add(m_EconomyPanelProductionPage);
		prefabs.Add(m_EconomyPanelProductionResources);
		prefabs.Add(m_EconomyPanelProductionDiagram);
		prefabs.Add(m_EconomyPanelProductionData);
		prefabs.Add(m_EconomyPanelProductionTab);
		prefabs.Add(m_EconomyPanelServicesBudget);
		prefabs.Add(m_EconomyPanelServicesList);
		prefabs.Add(m_EconomyPanelServicesPage);
		prefabs.Add(m_EconomyPanelServicesTab);
		prefabs.Add(m_EconomyPanelTaxationEstimate);
		prefabs.Add(m_EconomyPanelTaxationPage);
		prefabs.Add(m_EconomyPanelTaxationRate);
		prefabs.Add(m_EconomyPanelTaxationTab);
		prefabs.Add(m_EconomyPanelTaxationType);
		prefabs.Add(m_EventJournalPanel);
		prefabs.Add(m_EventJournalPanelButton);
		prefabs.Add(m_InfoviewsButton);
		prefabs.Add(m_InfoviewsMenu);
		prefabs.Add(m_InfoviewsPanel);
		prefabs.Add(m_InfoviewsFireHazard);
		prefabs.Add(m_LifePathPanel);
		prefabs.Add(m_LifePathPanelBackButton);
		prefabs.Add(m_LifePathPanelButton);
		prefabs.Add(m_LifePathPanelChirps);
		prefabs.Add(m_LifePathPanelDetails);
		prefabs.Add(m_MapTilePanel);
		prefabs.Add(m_MapTilePanelButton);
		prefabs.Add(m_MapTilePanelResources);
		prefabs.Add(m_MapTilePanelPurchase);
		prefabs.Add(m_PhotoModePanel);
		prefabs.Add(m_PhotoModePanelButton);
		prefabs.Add(m_PhotoModePanelHideUI);
		prefabs.Add(m_PhotoModePanelTakePicture);
		prefabs.Add(m_PhotoModeTab);
		prefabs.Add(m_PhotoModePanelTitle);
		prefabs.Add(m_PhotoModeCinematicCameraToggle);
		prefabs.Add(m_CinematicCameraPanel);
		prefabs.Add(m_CinematicCameraPanelCaptureKey);
		prefabs.Add(m_CinematicCameraPanelPlay);
		prefabs.Add(m_CinematicCameraPanelStop);
		prefabs.Add(m_CinematicCameraPanelHideUI);
		prefabs.Add(m_CinematicCameraPanelSaveLoad);
		prefabs.Add(m_CinematicCameraPanelReset);
		prefabs.Add(m_CinematicCameraPanelTimelineSlider);
		prefabs.Add(m_CinematicCameraPanelTransformCurves);
		prefabs.Add(m_CinematicCameraPanelPropertyCurves);
		prefabs.Add(m_ProgressionPanel);
		prefabs.Add(m_ProgressionPanelButton);
		prefabs.Add(m_ProgressionPanelDevelopmentNode);
		prefabs.Add(m_ProgressionPanelDevelopmentPage);
		prefabs.Add(m_ProgressionPanelDevelopmentService);
		prefabs.Add(m_ProgressionPanelDevelopmentTab);
		prefabs.Add(m_ProgressionPanelDevelopmentUnlockableNode);
		prefabs.Add(m_ProgressionPanelDevelopmentUnlockNode);
		prefabs.Add(m_ProgressionPanelMilestoneRewards);
		prefabs.Add(m_ProgressionPanelMilestoneRewardsMoney);
		prefabs.Add(m_ProgressionPanelMilestoneRewardsDevPoints);
		prefabs.Add(m_ProgressionPanelMilestoneRewardsMapTiles);
		prefabs.Add(m_ProgressionPanelMilestonesList);
		prefabs.Add(m_ProgressionPanelMilestonesPage);
		prefabs.Add(m_ProgressionPanelMilestonesTab);
		prefabs.Add(m_ProgressionPanelMilestoneXP);
		prefabs.Add(m_RadioPanel);
		prefabs.Add(m_RadioPanelAdsToggle);
		prefabs.Add(m_RadioPanelButton);
		prefabs.Add(m_RadioPanelNetworks);
		prefabs.Add(m_RadioPanelStations);
		prefabs.Add(m_RadioPanelVolumeSlider);
		prefabs.Add(m_StatisticsPanel);
		prefabs.Add(m_StatisticsPanelButton);
		prefabs.Add(m_StatisticsPanelMenu);
		prefabs.Add(m_StatisticsPanelTimeScale);
		prefabs.Add(m_ToolbarBulldozerBar);
		prefabs.Add(m_ToolbarDemand);
		prefabs.Add(m_ToolbarSimulationDateTime);
		prefabs.Add(m_ToolbarSimulationSpeed);
		prefabs.Add(m_ToolbarSimulationToggle);
		prefabs.Add(m_ToolbarUnderground);
		prefabs.Add(m_ToolOptions);
		prefabs.Add(m_ToolOptionsBrushSize);
		prefabs.Add(m_ToolOptionsBrushStrength);
		prefabs.Add(m_ToolOptionsElevation);
		prefabs.Add(m_ToolOptionsElevationDecrease);
		prefabs.Add(m_ToolOptionsElevationIncrease);
		prefabs.Add(m_ToolOptionsElevationStep);
		prefabs.Add(m_ToolOptionsModes);
		prefabs.Add(m_ToolOptionsModesComplexCurve);
		prefabs.Add(m_ToolOptionsModesContinuous);
		prefabs.Add(m_ToolOptionsModesGrid);
		prefabs.Add(m_ToolOptionsModesReplace);
		prefabs.Add(m_ToolOptionsModesSimpleCurve);
		prefabs.Add(m_ToolOptionsModesStraight);
		prefabs.Add(m_ToolOptionsParallelMode);
		prefabs.Add(m_ToolOptionsParallelModeOffset);
		prefabs.Add(m_ToolOptionsParallelModeOffsetDecrease);
		prefabs.Add(m_ToolOptionsParallelModeOffsetIncrease);
		prefabs.Add(m_ToolOptionsSnapping);
		prefabs.Add(m_ToolOptionsThemes);
		prefabs.Add(m_ToolOptionsAssetPacks);
		prefabs.Add(m_ToolOptionsUnderground);
		prefabs.Add(m_TransportationOverviewPanel);
		prefabs.Add(m_TransportationOverviewPanelButton);
		prefabs.Add(m_TransportationOverviewPanelLegend);
		prefabs.Add(m_TransportationOverviewPanelLines);
		prefabs.Add(m_TransportationOverviewPanelTabCargo);
		prefabs.Add(m_TransportationOverviewPanelTabPublicTransport);
		prefabs.Add(m_TransportationOverviewPanelTransportTypes);
		prefabs.Add(m_SelectedInfoPanel);
		prefabs.Add(m_SelectedInfoPanelTitle);
		prefabs.Add(m_PauseMenuButton);
		prefabs.Add(m_UpgradeGrid);
		prefabs.Add(m_AssetGrid);
		prefabs.Add(m_ActionHints);
		prefabs.Add(m_AssetImportButton);
		prefabs.Add(m_EditorInfoViewsPanel);
		prefabs.Add(m_ResetTODButton);
		prefabs.Add(m_SimulationPlayButton);
		prefabs.Add(m_TutorialsToggle);
		prefabs.Add(m_WorkspaceTitleBar);
		prefabs.Add(m_SelectProjectRoot);
		prefabs.Add(m_SelectAssets);
		prefabs.Add(m_SelectTemplate);
		prefabs.Add(m_ImportButton);
		prefabs.Add(m_ModifyTerrainButton);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ManualUITagsConfigurationData>());
	}
}
