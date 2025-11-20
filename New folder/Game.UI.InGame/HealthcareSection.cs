using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class HealthcareSection : InfoSectionBase
{
	protected override string group => "HealthcareSection";

	private int patientCount { get; set; }

	private int patientCapacity { get; set; }

	protected override void Reset()
	{
		patientCount = 0;
		patientCapacity = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (base.EntityManager.HasComponent<Game.Buildings.Hospital>(selectedEntity) && TryGetComponentWithUpgrades<HospitalData>(selectedEntity, selectedPrefab, out var data))
		{
			patientCapacity = data.m_PatientCapacity;
		}
		base.visible = patientCapacity > 0;
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Patient> buffer))
		{
			patientCount = buffer.Length;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("patientCount");
		writer.Write(patientCount);
		writer.PropertyName("patientCapacity");
		writer.Write(patientCapacity);
	}

	[Preserve]
	public HealthcareSection()
	{
	}
}
