using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class DeveloperSection : InfoSectionBase, ISubsectionProvider, ISectionSource, IJsonWritable
{
	protected override string group => "DeveloperSection";

	public List<ISubsectionSource> subsections { get; private set; }

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForUpgrades => true;

	public void AddSubsection(ISubsectionSource subsection)
	{
		subsections.Add(subsection);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		subsections = new List<ISubsectionSource>();
	}

	protected override void Reset()
	{
	}

	private bool Visible()
	{
		bool result = false;
		for (int i = 0; i < subsections.Count; i++)
		{
			if (subsections[i].DisplayFor(selectedEntity, selectedPrefab))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		for (int i = 0; i < subsections.Count; i++)
		{
			if (subsections[i].DisplayFor(selectedEntity, selectedPrefab))
			{
				subsections[i].OnRequestUpdate(selectedEntity, selectedPrefab);
			}
		}
	}

	private int GetSubsectionCount()
	{
		int num = 0;
		for (int i = 0; i < subsections.Count; i++)
		{
			if (subsections[i].DisplayFor(selectedEntity, selectedPrefab))
			{
				num++;
			}
		}
		return num;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("subsections");
		writer.ArrayBegin(GetSubsectionCount());
		for (int i = 0; i < subsections.Count; i++)
		{
			if (subsections[i].DisplayFor(selectedEntity, selectedPrefab))
			{
				writer.Write(subsections[i]);
			}
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public DeveloperSection()
	{
	}
}
