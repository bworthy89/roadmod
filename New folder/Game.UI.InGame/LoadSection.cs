using System;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class LoadSection : InfoSectionBase
{
	private enum LoadKey
	{
		Water,
		IndustrialWaste,
		Garbage,
		Mail,
		None
	}

	protected override string group => "LoadSection";

	private float load { get; set; }

	private float capacity { get; set; }

	private LoadKey loadKey { get; set; }

	protected override Entity selectedEntity
	{
		get
		{
			if (base.EntityManager.TryGetComponent<Controller>(base.selectedEntity, out var component))
			{
				return component.m_Controller;
			}
			return base.selectedEntity;
		}
	}

	protected override Entity selectedPrefab
	{
		get
		{
			if (base.EntityManager.TryGetComponent<Controller>(base.selectedEntity, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Controller, out var component2))
			{
				return component2.m_Prefab;
			}
			return base.selectedPrefab;
		}
	}

	protected override void Reset()
	{
		loadKey = LoadKey.None;
		load = 0f;
		capacity = 0f;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		GarbageTruckData component2;
		PostVanData component3;
		if (base.EntityManager.TryGetComponent<FireEngineData>(selectedPrefab, out var component))
		{
			capacity = component.m_ExtinguishingCapacity;
		}
		else if (base.EntityManager.TryGetComponent<GarbageTruckData>(selectedPrefab, out component2))
		{
			capacity = component2.m_GarbageCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PostVanData>(selectedPrefab, out component3))
		{
			capacity = component3.m_MailCapacity;
		}
		base.visible = capacity > 0f;
	}

	protected override void OnProcess()
	{
		Game.Vehicles.GarbageTruck component2;
		Game.Vehicles.PostVan component3;
		if (base.EntityManager.TryGetComponent<Game.Vehicles.FireEngine>(selectedEntity, out var component))
		{
			load = component.m_ExtinguishingAmount;
			loadKey = LoadKey.Water;
		}
		else if (base.EntityManager.TryGetComponent<Game.Vehicles.GarbageTruck>(selectedEntity, out component2))
		{
			load = component2.m_Garbage;
			loadKey = (((component2.m_State & GarbageTruckFlags.IndustrialWasteOnly) != 0) ? LoadKey.IndustrialWaste : LoadKey.Garbage);
		}
		else if (base.EntityManager.TryGetComponent<Game.Vehicles.PostVan>(selectedEntity, out component3))
		{
			load = component3.m_DeliveringMail + component3.m_CollectedMail;
			loadKey = LoadKey.Mail;
		}
		base.tooltipKeys.Add(loadKey.ToString());
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("load");
		writer.Write(load);
		writer.PropertyName("capacity");
		writer.Write(capacity);
		writer.PropertyName("loadKey");
		writer.Write(Enum.GetName(typeof(LoadKey), loadKey));
	}

	[Preserve]
	public LoadSection()
	{
	}
}
