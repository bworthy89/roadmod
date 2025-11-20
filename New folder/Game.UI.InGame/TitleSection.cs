using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TitleSection : InfoSectionBase
{
	private ImageSystem m_ImageSystem;

	protected override string group => "TitleSection";

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForUpgrades => true;

	[CanBeNull]
	private string icon { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		AddBinding(new TriggerBinding<string>(group, "renameEntity", OnRename));
	}

	protected override void Reset()
	{
		icon = null;
	}

	private void OnRename(string newName)
	{
		m_NameSystem.SetCustomName(selectedEntity, newName);
		m_InfoUISystem.RequestUpdate();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = selectedEntity != Entity.Null;
	}

	protected override void OnProcess()
	{
		icon = m_ImageSystem.GetInstanceIcon(selectedEntity, selectedPrefab);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("name");
		m_NameSystem.BindName(writer, selectedEntity);
		writer.PropertyName("vkName");
		m_NameSystem.BindNameForVirtualKeyboard(writer, selectedEntity);
		writer.PropertyName("vkLocaleKey");
		writer.Write(GetVirtualKeyboardLocaleKey(base.EntityManager, selectedEntity));
		writer.PropertyName("icon");
		if (icon == null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(icon);
		}
	}

	public static string GetVirtualKeyboardLocaleKey(EntityManager entityManager, Entity entity)
	{
		if (entityManager.HasComponent<Building>(entity))
		{
			return "BuildingName";
		}
		if (entityManager.HasComponent<Tree>(entity))
		{
			return "PlantName";
		}
		if (entityManager.HasComponent<Citizen>(entity))
		{
			return "CitizenName";
		}
		if (entityManager.HasComponent<Vehicle>(entity))
		{
			return "VehicleName";
		}
		if (entityManager.HasComponent<Animal>(entity))
		{
			return "AnimalName";
		}
		if (entityManager.HasComponent<TransportLine>(entity))
		{
			return "LineName";
		}
		if (entityManager.HasComponent<Aggregate>(entity))
		{
			return "RoadName";
		}
		if (entityManager.HasComponent<District>(entity))
		{
			return "DistrictName";
		}
		return "ObjectName";
	}

	[Preserve]
	public TitleSection()
	{
	}
}
