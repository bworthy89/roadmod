using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Economy;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class GarbageSection : InfoSectionBase
{
	private enum LoadKey
	{
		IndustrialWaste,
		Garbage
	}

	protected override string group => "GarbageSection";

	private int garbage { get; set; }

	private int garbageCapacity { get; set; }

	private int processingSpeed { get; set; }

	private int processingCapacity { get; set; }

	private LoadKey loadKey { get; set; }

	protected override void Reset()
	{
		garbage = 0;
		garbageCapacity = 0;
		processingSpeed = 0;
		processingCapacity = 0;
		loadKey = LoadKey.Garbage;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.GarbageFacility>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetComponent<Game.Buildings.GarbageFacility>(selectedEntity, out var component))
		{
			if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Resources> buffer))
			{
				garbage = EconomyUtils.GetResources(Resource.Garbage, buffer);
			}
			if (TryGetComponentWithUpgrades<GarbageFacilityData>(selectedEntity, selectedPrefab, out var data))
			{
				garbageCapacity = data.m_GarbageCapacity;
				processingSpeed = component.m_ProcessingRate;
				processingCapacity = data.m_ProcessingSpeed;
				if (data.m_LongTermStorage)
				{
					base.tooltipKeys.Add("Landfill");
				}
				if (base.EntityManager.HasComponent<Game.Buildings.ResourceProducer>(selectedEntity))
				{
					base.tooltipKeys.Add("RecyclingCenter");
				}
				if (base.EntityManager.HasComponent<ElectricityProducer>(selectedEntity))
				{
					base.tooltipKeys.Add("Incinerator");
				}
				if (data.m_IndustrialWasteOnly)
				{
					base.tooltipKeys.Add("HazardousWaste");
					loadKey = LoadKey.IndustrialWaste;
				}
			}
		}
		if (!base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.SubArea> buffer2))
		{
			return;
		}
		for (int i = 0; i < buffer2.Length; i++)
		{
			Entity area = buffer2[i].m_Area;
			if (base.EntityManager.TryGetComponent<Storage>(area, out var component2))
			{
				PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(area);
				Geometry componentData2 = base.EntityManager.GetComponentData<Geometry>(area);
				if (base.EntityManager.TryGetComponent<StorageAreaData>(componentData.m_Prefab, out var component3))
				{
					garbageCapacity += AreaUtils.CalculateStorageCapacity(componentData2, component3);
					garbage += component2.m_Amount;
				}
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("garbage");
		writer.Write(garbage);
		writer.PropertyName("garbageCapacity");
		writer.Write(garbageCapacity);
		writer.PropertyName("processingSpeed");
		writer.Write(processingSpeed);
		writer.PropertyName("processingCapacity");
		writer.Write(processingCapacity);
		writer.PropertyName("loadKey");
		writer.Write(Enum.GetName(typeof(LoadKey), loadKey));
	}

	[Preserve]
	public GarbageSection()
	{
	}
}
