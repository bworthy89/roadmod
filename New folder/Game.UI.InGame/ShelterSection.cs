using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ShelterSection : InfoSectionBase
{
	protected override string group => "ShelterSection";

	private int sheltered { get; set; }

	private int shelterCapacity { get; set; }

	private int consumables { get; set; }

	private int consumableCapacity { get; set; }

	protected override void Reset()
	{
		sheltered = 0;
		shelterCapacity = 0;
		consumables = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.EmergencyShelter>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<EmergencyShelterData>(selectedEntity, selectedPrefab, out var data))
		{
			shelterCapacity = data.m_ShelterCapacity;
		}
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Occupant> buffer))
		{
			sheltered = buffer.Length;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("sheltered");
		writer.Write(sheltered);
		writer.PropertyName("shelterCapacity");
		writer.Write(shelterCapacity);
	}

	[Preserve]
	public ShelterSection()
	{
	}
}
