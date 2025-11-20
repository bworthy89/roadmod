using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PoliceSection : InfoSectionBase
{
	protected override string group => "PoliceSection";

	private int prisonerCount { get; set; }

	private int prisonerCapacity { get; set; }

	protected override void Reset()
	{
		prisonerCount = 0;
		prisonerCapacity = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.PoliceStation>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<PoliceStationData>(selectedEntity, selectedPrefab, out var data))
		{
			prisonerCapacity = data.m_JailCapacity;
		}
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Occupant> buffer))
		{
			prisonerCount = buffer.Length;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("prisonerCount");
		writer.Write(prisonerCount);
		writer.PropertyName("prisonerCapacity");
		writer.Write(prisonerCapacity);
	}

	[Preserve]
	public PoliceSection()
	{
	}
}
