using System;
using Colossal.UI.Binding;
using Unity.Entities;

namespace Game.UI.InGame;

public class CapacityInfo : ISubsectionSource, IJsonWritable
{
	private readonly Func<Entity, Entity, bool> m_ShouldDisplay;

	private readonly Action<Entity, Entity, CapacityInfo> m_OnUpdate;

	public string label { get; set; }

	public int value { get; set; }

	public int max { get; set; }

	public CapacityInfo(Func<Entity, Entity, bool> shouldDisplay, Action<Entity, Entity, CapacityInfo> onUpdate)
	{
		m_ShouldDisplay = shouldDisplay;
		m_OnUpdate = onUpdate;
	}

	public bool DisplayFor(Entity entity, Entity prefab)
	{
		return m_ShouldDisplay(entity, prefab);
	}

	public void OnRequestUpdate(Entity entity, Entity prefab)
	{
		m_OnUpdate(entity, prefab, this);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("label");
		writer.Write(label);
		writer.PropertyName("value");
		writer.Write(value);
		writer.PropertyName("max");
		writer.Write(max);
		writer.TypeEnd();
	}
}
