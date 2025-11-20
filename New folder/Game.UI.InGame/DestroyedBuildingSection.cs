using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DestroyedBuildingSection : InfoSectionBase
{
	private enum Status
	{
		None,
		Waiting,
		NoService,
		Searching,
		Rebuild
	}

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private UpgradeToolSystem m_UpgradeToolSystem;

	private ValueBinding<bool> m_Rebuilding;

	private EntityQuery m_FireStationQuery;

	private EntityQuery m_ServiceDispatchQuery;

	protected override string group => "DestroyedBuildingSection";

	private Entity destroyer { get; set; }

	private bool cleared { get; set; }

	private float progress { get; set; }

	private Status status { get; set; }

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForUpgrades => true;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_UpgradeToolSystem = base.World.GetOrCreateSystemManaged<UpgradeToolSystem>();
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		m_FireStationQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.FireStation>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ServiceDispatchQuery = GetEntityQuery(ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		AddBinding(new TriggerBinding(group, "toggleRebuild", OnToggleRebuild));
		AddBinding(m_Rebuilding = new ValueBinding<bool>(group, "rebuilding", initialValue: false));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Remove(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		base.OnDestroy();
	}

	private void OnToolChanged(ToolBaseSystem tool)
	{
		m_Rebuilding.Update(tool == m_UpgradeToolSystem);
	}

	private void OnToggleRebuild()
	{
		if (m_ToolSystem.activeTool == m_UpgradeToolSystem)
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
			return;
		}
		m_UpgradeToolSystem.prefab = null;
		m_ToolSystem.activeTool = m_UpgradeToolSystem;
	}

	protected override void Reset()
	{
		destroyer = Entity.Null;
		status = Status.None;
		cleared = false;
		progress = 0f;
	}

	private bool Visible()
	{
		if (base.Destroyed && base.EntityManager.HasComponent<Building>(selectedEntity) && (!base.EntityManager.HasComponent<Owner>(selectedEntity) || base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity)))
		{
			if (base.EntityManager.HasComponent<SpawnableBuildingData>(selectedPrefab) && !base.EntityManager.HasComponent<PlacedSignatureBuildingData>(selectedPrefab))
			{
				return base.EntityManager.HasComponent<Attached>(selectedEntity);
			}
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Destroyed componentData = base.EntityManager.GetComponentData<Destroyed>(selectedEntity);
		base.EntityManager.TryGetComponent<PrefabRef>(componentData.m_Event, out var component);
		destroyer = component.m_Prefab;
		progress = math.max(0f, componentData.m_Cleared);
		cleared = progress >= 1f;
		if (!cleared)
		{
			NativeArray<PrefabRef> nativeArray = m_FireStationQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			NativeArray<Entity> nativeArray2 = m_ServiceDispatchQuery.ToEntityArray(Allocator.TempJob);
			bool flag = false;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<FireStationData>(nativeArray[i].m_Prefab, out var component2) && component2.m_DisasterResponseCapacity > 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				status = Status.NoService;
			}
			else
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					DynamicBuffer<ServiceDispatch> buffer = base.EntityManager.GetBuffer<ServiceDispatch>(nativeArray2[j], isReadOnly: true);
					for (int k = 0; k < buffer.Length; k++)
					{
						if (base.EntityManager.TryGetComponent<FireRescueRequest>(buffer[k].m_Request, out var component3) && component3.m_Type == FireRescueRequestType.Disaster && component3.m_Target == selectedEntity && VehicleAtTarget(nativeArray2[j]))
						{
							status = Status.Searching;
							break;
						}
					}
				}
			}
			if (status == Status.None)
			{
				status = Status.Waiting;
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
		else
		{
			status = Status.Rebuild;
		}
		if (status != Status.None)
		{
			base.tooltipKeys.Add(status.ToString());
		}
		m_InfoUISystem.tags.Add(SelectedInfoTags.Destroyed);
	}

	private bool VehicleAtTarget(Entity vehicle)
	{
		if (base.EntityManager.TryGetComponent<Game.Vehicles.FireEngine>(vehicle, out var component))
		{
			return (component.m_State & FireEngineFlags.Rescueing) != 0;
		}
		return false;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("destroyer");
		if (destroyer != Entity.Null)
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(destroyer);
			writer.Write(prefab.name);
		}
		else
		{
			writer.WriteNull();
		}
		writer.PropertyName("progress");
		writer.Write(progress * 100f);
		writer.PropertyName("cleared");
		writer.Write(cleared);
		writer.PropertyName("status");
		writer.Write(status.ToString());
	}

	[Preserve]
	public DestroyedBuildingSection()
	{
	}
}
