using System;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DistrictsSection : InfoSectionBase
{
	private ToolSystem m_ToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private SelectionToolSystem m_SelectionToolSystem;

	private EntityQuery m_ConfigQuery;

	private EntityQuery m_DistrictQuery;

	private EntityQuery m_DistrictPrefabQuery;

	private EntityQuery m_DistrictModifiedQuery;

	private ValueBinding<bool> m_Selecting;

	protected override string group => "DistrictsSection";

	private NativeList<Entity> districts { get; set; }

	private bool districtMissing { get; set; }

	protected override void Reset()
	{
		districts.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_SelectionToolSystem = base.World.GetOrCreateSystemManaged<SelectionToolSystem>();
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		districts = new NativeList<Entity>(Allocator.Persistent);
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<AreasConfigurationData>());
		m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.Exclude<Temp>());
		m_DistrictPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictData>(), ComponentType.Exclude<Locked>());
		m_DistrictModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<District>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(new TriggerBinding<Entity>(group, "removeDistrict", RemoveServiceDistrict));
		AddBinding(new TriggerBinding(group, "toggleSelectionTool", ToggleSelectionTool));
		AddBinding(new TriggerBinding(group, "toggleDistrictTool", ToggleDistrictTool));
		AddBinding(new TriggerBinding(group, "disableTool", DisableTool));
		AddBinding(m_Selecting = new ValueBinding<bool>(group, "selecting", initialValue: false));
	}

	private void OnToolChanged(ToolBaseSystem tool)
	{
		bool flag = tool == m_SelectionToolSystem && m_SelectionToolSystem.selectionType == SelectionType.ServiceDistrict;
		if (m_Selecting.value && !flag)
		{
			m_SelectionToolSystem.selectionOwner = Entity.Null;
			m_SelectionToolSystem.selectionType = SelectionType.None;
		}
		m_Selecting.Update(flag);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		districts.Dispose();
		base.OnDestroy();
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<ServiceDistrict>(selectedEntity))
		{
			return !m_DistrictPrefabQuery.IsEmpty;
		}
		return false;
	}

	protected override void OnPreUpdate()
	{
		base.OnPreUpdate();
		if (!m_DistrictModifiedQuery.IsEmptyIgnoreFilter)
		{
			RequestUpdate();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
		districtMissing = m_DistrictQuery.IsEmptyIgnoreFilter;
	}

	protected override void OnProcess()
	{
		DynamicBuffer<ServiceDistrict> buffer = base.EntityManager.GetBuffer<ServiceDistrict>(selectedEntity, isReadOnly: true);
		for (int i = 0; i < buffer.Length; i++)
		{
			NativeList<Entity> nativeList = districts;
			ServiceDistrict serviceDistrict = buffer[i];
			nativeList.Add(in serviceDistrict.m_District);
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("districtMissing");
		writer.Write(districtMissing);
		writer.PropertyName("districts");
		writer.ArrayBegin(districts.Length);
		for (int i = 0; i < districts.Length; i++)
		{
			Entity entity = districts[i];
			writer.TypeBegin("selectedInfo.District");
			writer.PropertyName("name");
			m_NameSystem.BindName(writer, entity);
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	public void RemoveServiceDistrict(Entity district)
	{
		DynamicBuffer<ServiceDistrict> buffer = base.EntityManager.GetBuffer<ServiceDistrict>(selectedEntity);
		bool flag = false;
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_District == district)
			{
				buffer.RemoveAt(i);
				flag = true;
			}
		}
		if (flag)
		{
			m_InfoUISystem.RequestUpdate();
			if (m_ToolSystem.activeTool == m_SelectionToolSystem)
			{
				m_SelectionToolSystem.requestSelectionUpdate = true;
			}
		}
	}

	private void ToggleSelectionTool()
	{
		if (m_ToolSystem.activeTool == m_SelectionToolSystem)
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
			return;
		}
		m_SelectionToolSystem.selectionType = SelectionType.ServiceDistrict;
		m_SelectionToolSystem.selectionOwner = selectedEntity;
		m_ToolSystem.activeTool = m_SelectionToolSystem;
	}

	private void ToggleDistrictTool()
	{
		if (m_ToolSystem.activeTool == m_AreaToolSystem)
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
			return;
		}
		AreasConfigurationPrefab prefab = m_PrefabSystem.GetPrefab<AreasConfigurationPrefab>(m_ConfigQuery.GetSingletonEntity());
		m_AreaToolSystem.prefab = prefab.m_DefaultDistrictPrefab;
		m_ToolSystem.activeTool = m_AreaToolSystem;
	}

	private void DisableTool()
	{
		m_ToolSystem.activeTool = m_DefaultToolSystem;
	}

	[Preserve]
	public DistrictsSection()
	{
	}
}
