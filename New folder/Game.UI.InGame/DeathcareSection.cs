using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DeathcareSection : InfoSectionBase
{
	protected override string group => "DeathcareSection";

	private int bodyCount { get; set; }

	private int bodyCapacity { get; set; }

	private float processingSpeed { get; set; }

	private float processingCapacity { get; set; }

	protected override void Reset()
	{
		bodyCount = 0;
		bodyCapacity = 0;
		processingSpeed = 0f;
		processingCapacity = 0f;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.DeathcareFacility>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<DeathcareFacilityData>(selectedEntity, selectedPrefab, out var data))
		{
			bodyCapacity = data.m_StorageCapacity;
			base.tooltipKeys.Add(data.m_LongTermStorage ? "Cemetery" : "Crematorium");
		}
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			processingSpeed = data.m_ProcessingRate * BuildingUtils.GetEfficiency(buffer);
		}
		bodyCount = base.EntityManager.GetComponentData<Game.Buildings.DeathcareFacility>(selectedEntity).m_LongTermStoredCount;
		processingCapacity = data.m_ProcessingRate;
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Patient> buffer2))
		{
			bodyCount += buffer2.Length;
		}
		if (bodyCount <= 0)
		{
			processingSpeed = 0f;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("bodyCount");
		writer.Write(bodyCount);
		writer.PropertyName("bodyCapacity");
		writer.Write(bodyCapacity);
		writer.PropertyName("processingSpeed");
		writer.Write(processingSpeed);
		writer.PropertyName("processingCapacity");
		writer.Write(processingCapacity);
	}

	[Preserve]
	public DeathcareSection()
	{
	}
}
