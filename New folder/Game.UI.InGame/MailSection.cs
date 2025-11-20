using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Economy;
using Game.Prefabs;
using Game.Routes;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class MailSection : InfoSectionBase
{
	private enum MailKey
	{
		ToDeliver,
		Collected,
		Unsorted,
		Local
	}

	private enum Type
	{
		PostFacility,
		MailBox
	}

	protected override string group => "MailSection";

	private int sortingRate { get; set; }

	private int sortingCapacity { get; set; }

	private int localAmount { get; set; }

	private int unsortedAmount { get; set; }

	private int outgoingAmount { get; set; }

	private int storedAmount { get; set; }

	private int storageCapacity { get; set; }

	private MailKey localKey { get; set; }

	private MailKey unsortedKey { get; set; }

	private Type type { get; set; }

	protected override void Reset()
	{
		sortingRate = 0;
		sortingCapacity = 0;
		localAmount = 0;
		unsortedAmount = 0;
		outgoingAmount = 0;
		storedAmount = 0;
		storageCapacity = 0;
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<Game.Buildings.PostFacility>(selectedEntity))
		{
			if (base.EntityManager.HasComponent<Game.Routes.MailBox>(selectedEntity))
			{
				return base.EntityManager.HasComponent<MailBoxData>(selectedPrefab);
			}
			return false;
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Game.Routes.MailBox component2;
		MailBoxData component3;
		if (TryGetComponentWithUpgrades<PostFacilityData>(selectedEntity, selectedPrefab, out var data))
		{
			type = Type.PostFacility;
			Game.Buildings.PostFacility componentData = base.EntityManager.GetComponentData<Game.Buildings.PostFacility>(selectedEntity);
			sortingRate = (data.m_SortingRate * componentData.m_ProcessingFactor + 50) / 100;
			sortingCapacity = data.m_SortingRate;
			DynamicBuffer<Resources> buffer = base.EntityManager.GetBuffer<Resources>(selectedEntity, isReadOnly: true);
			unsortedAmount = EconomyUtils.GetResources(Resource.UnsortedMail, buffer);
			localAmount = EconomyUtils.GetResources(Resource.LocalMail, buffer);
			outgoingAmount = EconomyUtils.GetResources(Resource.OutgoingMail, buffer);
			if (base.EntityManager.TryGetComponent<Game.Routes.MailBox>(selectedEntity, out var component))
			{
				unsortedAmount += component.m_MailAmount;
			}
			localKey = ((data.m_PostVanCapacity <= 0) ? MailKey.Local : MailKey.ToDeliver);
			unsortedKey = ((data.m_PostVanCapacity > 0) ? MailKey.Collected : MailKey.Unsorted);
			storedAmount = unsortedAmount + localAmount + outgoingAmount;
			storageCapacity = data.m_MailCapacity;
			base.tooltipKeys.Add(localKey.ToString());
			if (sortingCapacity > 0 || outgoingAmount > 0)
			{
				base.tooltipKeys.Add("Outgoing");
			}
			base.tooltipKeys.Add(unsortedKey.ToString());
			if (sortingCapacity > 0)
			{
				base.tooltipKeys.Add("Sorting");
			}
			if (storageCapacity > 0)
			{
				base.tooltipKeys.Add("Storage");
			}
		}
		else if (base.EntityManager.TryGetComponent<Game.Routes.MailBox>(selectedEntity, out component2) && base.EntityManager.TryGetComponent<MailBoxData>(selectedPrefab, out component3))
		{
			type = Type.MailBox;
			storageCapacity = component3.m_MailCapacity;
			storedAmount = component2.m_MailAmount;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("sortingRate");
		writer.Write(sortingRate);
		writer.PropertyName("sortingCapacity");
		writer.Write(sortingCapacity);
		writer.PropertyName("localAmount");
		writer.Write(localAmount);
		writer.PropertyName("unsortedAmount");
		writer.Write(unsortedAmount);
		writer.PropertyName("outgoingAmount");
		writer.Write(outgoingAmount);
		writer.PropertyName("storedAmount");
		writer.Write(storedAmount);
		writer.PropertyName("storageCapacity");
		writer.Write(storageCapacity);
		writer.PropertyName("localKey");
		writer.Write(Enum.GetName(typeof(MailKey), localKey));
		writer.PropertyName("unsortedKey");
		writer.Write(Enum.GetName(typeof(MailKey), unsortedKey));
		writer.PropertyName("type");
		writer.Write((int)type);
	}

	[Preserve]
	public MailSection()
	{
	}
}
