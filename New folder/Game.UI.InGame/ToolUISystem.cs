using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class ToolUISystem : UISystemBase
{
	public struct Brush : IJsonWritable
	{
		public Entity m_Entity;

		public string m_Name;

		public string m_Icon;

		public int m_Priority;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("entity");
			writer.Write(m_Entity);
			writer.PropertyName("name");
			writer.Write(m_Name);
			writer.PropertyName("icon");
			writer.Write(m_Icon);
			writer.PropertyName("priority");
			writer.Write(m_Priority);
			writer.TypeEnd();
		}
	}

	public const string kGroup = "tool";

	private ToolSystem m_ToolSystem;

	private NetToolSystem m_NetToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private ZoneToolSystem m_ZoneToolSystem;

	private RouteToolSystem m_RouteToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private TerrainToolSystem m_TerrainToolSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private UpgradeToolSystem m_UpgradeToolSystem;

	private BulldozeToolSystem m_BulldozeToolSystem;

	private SelectionToolSystem m_SelectionToolSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_BulldozeQuery;

	private EntityQuery m_BrushQuery;

	private RawValueBinding m_ActiveToolBinding;

	private GetterValueBinding<bool> m_UndergroundModeSupported;

	private List<ToolMode> m_ToolModes;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
		m_RouteToolSystem = base.World.GetOrCreateSystemManaged<RouteToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_TerrainToolSystem = base.World.GetOrCreateSystemManaged<TerrainToolSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_UpgradeToolSystem = base.World.GetOrCreateSystemManaged<UpgradeToolSystem>();
		m_BulldozeToolSystem = base.World.GetOrCreateSystemManaged<BulldozeToolSystem>();
		m_SelectionToolSystem = base.World.GetOrCreateSystemManaged<SelectionToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_BulldozeQuery = GetEntityQuery(ComponentType.ReadOnly<BulldozeData>(), ComponentType.ReadOnly<PrefabData>());
		m_BrushQuery = GetEntityQuery(ComponentType.ReadOnly<BrushData>(), ComponentType.ReadOnly<PrefabData>());
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		ToolSystem toolSystem2 = m_ToolSystem;
		toolSystem2.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
		BulldozeToolSystem bulldozeToolSystem = m_BulldozeToolSystem;
		bulldozeToolSystem.EventConfirmationRequested = (Action)Delegate.Combine(bulldozeToolSystem.EventConfirmationRequested, new Action(OnBulldozeConfirmationRequested));
		AddBinding(m_ActiveToolBinding = new RawValueBinding("tool", "activeTool", BindActiveTool));
		AddUpdateBinding(new GetterValueBinding<uint>("tool", "availableSnapMask", delegate
		{
			if (m_ToolSystem.activeTool == null)
			{
				return 0u;
			}
			m_ToolSystem.activeTool.GetAvailableSnapMask(out var onMask, out var offMask);
			return (uint)(onMask & offMask);
		}));
		AddUpdateBinding(new GetterValueBinding<uint>("tool", "allSnapMask", delegate
		{
			if (m_ToolSystem.activeTool == null)
			{
				return 0u;
			}
			m_ToolSystem.activeTool.GetAvailableSnapMask(out var onMask, out var offMask);
			return (uint)(onMask & offMask) & 0xFFF8FFFFu;
		}));
		AddUpdateBinding(new GetterValueBinding<uint>("tool", "selectedSnapMask", () => (uint)((m_ToolSystem.activeTool != null) ? m_ToolSystem.activeTool.selectedSnap : Snap.None)));
		AddBinding(new ValueBinding<string[]>("tool", "snapOptionNames", InitSnapOptionNames(), new ArrayWriter<string>(new StringWriter())));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "colorSupported", GetColorSupported));
		AddUpdateBinding(new GetterValueBinding<Color32>("tool", "color", () => m_ToolSystem.activeTool?.color ?? default(Color32)));
		AddUpdateBinding(new GetterValueBinding<Bounds1>("tool", "elevationRange", GetElevationRange));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "elevation", () => m_NetToolSystem.elevation));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "elevationStep", () => m_NetToolSystem.elevationStep));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "parallelModeSupported", GetParallelModeSupported));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "parallelMode", () => GetParallelModeSupported() && m_NetToolSystem.parallelCount != 0));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "parallelOffset", () => m_NetToolSystem.parallelOffset));
		AddUpdateBinding(m_UndergroundModeSupported = new GetterValueBinding<bool>("tool", "undergroundModeSupported", () => m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.allowUnderground));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "undergroundMode", () => m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.requireUnderground));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "elevationDownDisabled", GetElevationDownDisabled));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "elevationUpDisabled", GetElevationUpDisabled));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "replacingTrees", () => !m_ObjectToolSystem.GetNetUpgradeStates(out var _).IsEmpty));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "distance", () => m_ObjectToolSystem.distance));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "distanceScale", () => m_ObjectToolSystem.distanceScale));
		AddBinding(new TriggerBinding<string>("tool", "selectTool", SelectTool));
		AddBinding(new TriggerBinding<int>("tool", "selectToolMode", SelectToolMode));
		AddBinding(new TriggerBinding<uint>("tool", "setSelectedSnapMask", SetSelectedSnapMask));
		AddBinding(new TriggerBinding("tool", "elevationUp", OnElevationUp));
		AddBinding(new TriggerBinding("tool", "elevationDown", OnElevationDown));
		AddBinding(new TriggerBinding("tool", "elevationScroll", OnElevationScroll));
		AddBinding(new TriggerBinding<float>("tool", "setElevationStep", SetElevationStep));
		AddBinding(new TriggerBinding("tool", "toggleParallelMode", ToggleParallelMode));
		AddBinding(new TriggerBinding<float>("tool", "setParallelOffset", SetParallelOffset));
		AddBinding(new TriggerBinding<bool>("tool", "setUndergroundMode", SetUndergroundMode));
		AddBinding(new TriggerBinding<float>("tool", "setDistance", SetDistance));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "allowBrush", AllowBrush));
		AddUpdateBinding(new GetterValueBinding<Entity>("tool", "selectedBrush", () => (!AllowBrush() || !m_ToolSystem.activeTool.brushing) ? Entity.Null : m_PrefabSystem.GetEntity(m_ToolSystem.activeTool.brushType)));
		AddBinding(new GetterValueBinding<Brush[]>("tool", "brushes", BindBrushTypes, new ArrayWriter<Brush>(new ValueWriter<Brush>())));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushSize", () => (!AllowBrush()) ? 0f : m_ToolSystem.activeTool.brushSize));
		AddUpdateBinding(new GetterValueBinding<float?>("tool", "brushHeight", () => (m_ToolSystem.activeTool != m_TerrainToolSystem || (m_TerrainToolSystem.prefab.m_Type != TerraformingType.Level && m_TerrainToolSystem.prefab.m_Type != TerraformingType.Slope)) ? ((float?)null) : new float?(m_TerrainToolSystem.brushHeight - m_WaterSystem.SeaLevel), ValueWritersStruct.Nullable(ValueWriters.Create<float>())));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushStrength", () => (!AllowBrush()) ? 0f : m_ToolSystem.activeTool.brushStrength));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushAngle", () => (!AllowBrush()) ? 0f : m_ToolSystem.activeTool.brushAngle));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushSizeMin", () => 10f));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushSizeMax", () => (!m_ToolSystem.actionMode.IsEditor()) ? 1000f : 5000f));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushHeightMin", () => 0f - m_TerrainSystem.heightScaleOffset.y - m_WaterSystem.SeaLevel));
		AddUpdateBinding(new GetterValueBinding<float>("tool", "brushHeightMax", () => m_TerrainSystem.heightScaleOffset.x - m_TerrainSystem.heightScaleOffset.y - m_WaterSystem.SeaLevel));
		AddBinding(new TriggerBinding<Entity>("tool", "selectBrush", SelectBrush));
		AddBinding(new TriggerBinding<float>("tool", "setBrushHeight", SetBrushHeight));
		AddBinding(new TriggerBinding<float>("tool", "setBrushSize", SetBrushSize));
		AddBinding(new TriggerBinding<float>("tool", "setBrushStrength", SetBrushStrength));
		AddBinding(new TriggerBinding<float>("tool", "setBrushAngle", SetBrushAngle));
		AddBinding(new TriggerBinding<Color32>("tool", "setColor", SetColor));
		AddUpdateBinding(new GetterValueBinding<bool>("tool", "isEditor", IsEditor));
		m_ToolModes = new List<ToolMode>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Remove(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		ToolSystem toolSystem2 = m_ToolSystem;
		toolSystem2.EventPrefabChanged = (Action<PrefabBase>)Delegate.Remove(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
		BulldozeToolSystem bulldozeToolSystem = m_BulldozeToolSystem;
		bulldozeToolSystem.EventConfirmationRequested = (Action)Delegate.Remove(bulldozeToolSystem.EventConfirmationRequested, new Action(OnBulldozeConfirmationRequested));
		base.OnDestroy();
	}

	private void BindActiveTool(IJsonWriter binder)
	{
		binder.TypeBegin("tool.UITool");
		binder.PropertyName("id");
		binder.Write(m_ToolSystem.activeTool.toolID);
		binder.PropertyName("modeIndex");
		binder.Write(m_ToolSystem.activeTool.uiModeIndex);
		binder.PropertyName("modes");
		BindToolModes(binder);
		binder.TypeEnd();
	}

	private void BindToolModes(IJsonWriter binder)
	{
		m_ToolModes.Clear();
		m_ToolSystem.activeTool.GetUIModes(m_ToolModes);
		binder.ArrayBegin(m_ToolModes.Count);
		for (int i = 0; i < m_ToolModes.Count; i++)
		{
			ToolMode toolMode = m_ToolModes[i];
			binder.TypeBegin("tool.ToolMode");
			binder.PropertyName("id");
			binder.Write(toolMode.name);
			binder.PropertyName("index");
			binder.Write(toolMode.index);
			binder.PropertyName("icon");
			binder.Write("Media/Tools/" + m_ToolSystem.activeTool.toolID + "/" + toolMode.name + ".svg");
			binder.TypeEnd();
		}
		binder.ArrayEnd();
	}

	private void SetSelectedSnapMask(uint mask)
	{
		m_ToolSystem.activeTool.selectedSnap = (Snap)mask;
	}

	private string[] InitSnapOptionNames()
	{
		uint[] obj = (uint[])Enum.GetValues(typeof(Snap));
		List<string> list = new List<string>();
		uint[] array = obj;
		foreach (uint num in array)
		{
			if (num != uint.MaxValue && num != 0)
			{
				list.Add(Enum.GetName(typeof(Snap), num));
			}
		}
		return list.ToArray();
	}

	private void SelectTool(string tool)
	{
		SelectTool(GetToolSystem(tool));
	}

	public void SelectTool(ToolBaseSystem tool)
	{
		if (m_ToolSystem.activeTool == tool)
		{
			return;
		}
		m_ToolSystem.activeTool = tool;
		if (tool == m_BulldozeToolSystem && !m_BulldozeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<PrefabData> nativeArray = m_BulldozeQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
			try
			{
				m_BulldozeToolSystem.prefab = m_PrefabSystem.GetPrefab<BulldozePrefab>(nativeArray[0]);
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
	}

	private void SelectToolMode(int modeIndex)
	{
		ToolBaseSystem activeTool = m_ToolSystem.activeTool;
		if (!(activeTool is NetToolSystem netToolSystem))
		{
			if (!(activeTool is ZoneToolSystem zoneToolSystem))
			{
				if (!(activeTool is BulldozeToolSystem bulldozeToolSystem))
				{
					if (!(activeTool is AreaToolSystem areaToolSystem))
					{
						if (activeTool is ObjectToolSystem objectToolSystem)
						{
							objectToolSystem.mode = (ObjectToolSystem.Mode)modeIndex;
						}
					}
					else
					{
						areaToolSystem.mode = (AreaToolSystem.Mode)modeIndex;
					}
				}
				else
				{
					bulldozeToolSystem.mode = (BulldozeToolSystem.Mode)modeIndex;
				}
			}
			else
			{
				zoneToolSystem.mode = (ZoneToolSystem.Mode)modeIndex;
			}
		}
		else
		{
			netToolSystem.mode = (NetToolSystem.Mode)modeIndex;
		}
		m_ActiveToolBinding.Update();
	}

	private ToolBaseSystem GetToolSystem(string tool)
	{
		return tool switch
		{
			"Net Tool" => m_NetToolSystem, 
			"Area Tool" => m_AreaToolSystem, 
			"Zone Tool" => m_ZoneToolSystem, 
			"Route Tool" => m_RouteToolSystem, 
			"Object Tool" => m_ObjectToolSystem, 
			"Terrain Tool" => m_TerrainToolSystem, 
			"Upgrade Tool" => m_UpgradeToolSystem, 
			"Bulldoze Tool" => m_BulldozeToolSystem, 
			"Selection Tool" => m_SelectionToolSystem, 
			_ => m_DefaultToolSystem, 
		};
	}

	private void OnToolChanged(ToolBaseSystem tool)
	{
		if (tool != m_TerrainToolSystem)
		{
			m_TerrainToolSystem.SetDisableFX();
		}
		m_ActiveToolBinding.Update();
		m_UndergroundModeSupported.Update();
	}

	private void OnPrefabChanged(PrefabBase prefab)
	{
		m_ActiveToolBinding.Update();
	}

	private void OnBulldozeConfirmationRequested()
	{
		GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new ConfirmationDialog(null, "Common.DIALOG_MESSAGE[Bulldozer]", "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]"), OnConfirmBulldoze);
	}

	private void OnConfirmBulldoze(int msg)
	{
		m_BulldozeToolSystem.ConfirmAction(msg == 0);
	}

	private bool GetElevationUpDisabled()
	{
		if (m_ToolSystem.activeTool == m_NetToolSystem)
		{
			Bounds1 elevationRange = GetElevationRange();
			if (elevationRange != default(Bounds1))
			{
				return elevationRange.max <= m_NetToolSystem.elevation;
			}
		}
		return !m_ToolSystem.activeTool.requireUnderground;
	}

	private void OnElevationUp()
	{
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.ElevationUp();
		}
	}

	private bool GetElevationDownDisabled()
	{
		if (m_ToolSystem.activeTool == m_NetToolSystem)
		{
			Bounds1 elevationRange = GetElevationRange();
			if (elevationRange != default(Bounds1))
			{
				return elevationRange.min >= m_NetToolSystem.elevation;
			}
		}
		if (!m_ToolSystem.activeTool.requireUnderground)
		{
			return !m_ToolSystem.activeTool.allowUnderground;
		}
		return true;
	}

	private void OnElevationDown()
	{
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.ElevationDown();
		}
	}

	private void OnElevationScroll()
	{
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.ElevationScroll();
		}
	}

	private Bounds1 GetElevationRange()
	{
		if (m_ToolSystem.activeTool == m_NetToolSystem && m_NetToolSystem.mode != NetToolSystem.Mode.Replace && m_NetToolSystem.prefab != null && m_NetToolSystem.prefab.TryGet<PlaceableNet>(out var component))
		{
			if (component.m_UndergroundPrefab != null && component.m_UndergroundPrefab.TryGet<PlaceableNet>(out var component2))
			{
				return component.m_ElevationRange | component2.m_ElevationRange;
			}
			return component.m_ElevationRange;
		}
		return default(Bounds1);
	}

	private void SetElevationStep(float step)
	{
		m_NetToolSystem.elevationStep = step;
	}

	private bool GetParallelModeSupported()
	{
		if (m_ToolSystem.activeTool == m_NetToolSystem && m_NetToolSystem.mode != NetToolSystem.Mode.Grid && m_NetToolSystem.mode != NetToolSystem.Mode.Replace)
		{
			if (m_NetToolSystem.prefab != null && m_NetToolSystem.prefab.TryGet<PlaceableNet>(out var component) && component.m_AllowParallelMode)
			{
				return true;
			}
			if (m_NetToolSystem.lane != null)
			{
				return true;
			}
		}
		return false;
	}

	private void ToggleParallelMode()
	{
		m_NetToolSystem.parallelCount = ((m_NetToolSystem.parallelCount == 0) ? 1 : 0);
	}

	private void SetParallelOffset(float offset)
	{
		m_NetToolSystem.parallelOffset = offset;
	}

	private void SetUndergroundMode(bool enabled)
	{
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.SetUnderground(enabled);
		}
	}

	private void SetDistance(float distance)
	{
		m_ObjectToolSystem.distance = distance;
	}

	private Brush[] BindBrushTypes()
	{
		PrefabSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		NativeArray<Entity> nativeArray = m_BrushQuery.ToEntityArray(Allocator.TempJob);
		Brush[] array = new Brush[nativeArray.Length];
		for (int i = 0; i < nativeArray.Length; i++)
		{
			BrushPrefab prefab = orCreateSystemManaged.GetPrefab<BrushPrefab>(nativeArray[i]);
			array[i] = new Brush
			{
				m_Entity = nativeArray[i],
				m_Name = prefab.name,
				m_Icon = string.Empty,
				m_Priority = prefab.m_Priority
			};
		}
		nativeArray.Dispose();
		return array;
	}

	private void SelectBrush(Entity entity)
	{
		if (!AllowBrush())
		{
			return;
		}
		if (entity != Entity.Null)
		{
			BrushPrefab prefab = m_PrefabSystem.GetPrefab<BrushPrefab>(entity);
			m_ToolSystem.activeTool.brushType = prefab;
			if (m_ToolSystem.activeTool == m_ObjectToolSystem)
			{
				m_ObjectToolSystem.mode = ObjectToolSystem.Mode.Brush;
			}
		}
		else if (m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Brush)
		{
			m_ObjectToolSystem.mode = ObjectToolSystem.Mode.Create;
		}
	}

	private bool AllowBrush()
	{
		if (m_ToolSystem.activeTool == m_ObjectToolSystem)
		{
			return m_ObjectToolSystem.allowBrush;
		}
		if (m_ToolSystem.activeTool == m_TerrainToolSystem)
		{
			return true;
		}
		return false;
	}

	private void SetBrushSize(float size)
	{
		m_ToolSystem.activeTool.brushSize = size;
	}

	private void SetBrushHeight(float height)
	{
		m_TerrainToolSystem.brushHeight = height + m_WaterSystem.SeaLevel;
	}

	private void SetBrushStrength(float strength)
	{
		m_ToolSystem.activeTool.brushStrength = strength;
	}

	private void SetBrushAngle(float angle)
	{
		m_ToolSystem.activeTool.brushAngle = angle;
	}

	private void SetColor(Color32 color)
	{
		m_ToolSystem.activeTool.color = color;
	}

	private bool GetColorSupported()
	{
		return m_ToolSystem.activePrefab is IColored;
	}

	private bool IsEditor()
	{
		return GameManager.instance.gameMode.IsEditor();
	}

	[Preserve]
	public ToolUISystem()
	{
	}
}
