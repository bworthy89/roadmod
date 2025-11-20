using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class EducationSection : InfoSectionBase
{
	protected override string group => "EducationSection";

	private int studentCount { get; set; }

	private int studentCapacity { get; set; }

	private float graduationTime { get; set; }

	private float failProbability { get; set; }

	protected override void Reset()
	{
		studentCount = 0;
		studentCapacity = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.School>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<SchoolData>(selectedEntity, selectedPrefab, out var data))
		{
			studentCapacity = data.m_StudentCapacity;
		}
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Student> buffer))
		{
			studentCount = buffer.Length;
		}
		if (base.EntityManager.TryGetComponent<Game.Buildings.School>(selectedEntity, out var component))
		{
			graduationTime = ((component.m_AverageGraduationTime > 0f) ? component.m_AverageGraduationTime : 0.5f);
			failProbability = component.m_AverageFailProbability;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("studentCount");
		writer.Write(studentCount);
		writer.PropertyName("studentCapacity");
		writer.Write(studentCapacity);
		writer.PropertyName("graduationTime");
		writer.Write(graduationTime);
		writer.PropertyName("failProbability");
		writer.Write(failProbability);
	}

	[Preserve]
	public EducationSection()
	{
	}
}
