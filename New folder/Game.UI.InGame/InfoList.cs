using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Unity.Entities;

namespace Game.UI.InGame;

public class InfoList : ISubsectionSource, IJsonWritable
{
	public readonly struct Item : IJsonWritable
	{
		public static readonly Entity kNullEntity = Entity.Null;

		public string text { get; }

		public Entity entity { get; }

		public Item(string text, Entity entity)
		{
			this.text = text;
			this.entity = entity;
		}

		public Item(string text)
			: this(text, kNullEntity)
		{
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("text");
			writer.Write(text);
			writer.PropertyName("entity");
			if (entity == Entity.Null)
			{
				writer.WriteNull();
			}
			else
			{
				writer.Write(entity);
			}
			writer.TypeEnd();
		}
	}

	private readonly Func<Entity, Entity, bool> m_ShouldDisplay;

	private readonly Action<Entity, Entity, InfoList> m_OnUpdate;

	public string label { get; set; }

	private List<Item> list { get; set; }

	private bool expanded { get; set; }

	public InfoList(Func<Entity, Entity, bool> shouldDisplay, Action<Entity, Entity, InfoList> onUpdate)
	{
		list = new List<Item>();
		m_ShouldDisplay = shouldDisplay;
		m_OnUpdate = onUpdate;
	}

	public bool DisplayFor(Entity entity, Entity prefab)
	{
		return m_ShouldDisplay(entity, prefab);
	}

	public void OnRequestUpdate(Entity entity, Entity prefab)
	{
		list.Clear();
		m_OnUpdate(entity, prefab, this);
	}

	public void Add(Item item)
	{
		list.Add(item);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("expanded");
		writer.Write(expanded);
		writer.PropertyName("label");
		writer.Write(label);
		writer.PropertyName("list");
		writer.ArrayBegin(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			writer.Write(list[i]);
		}
		writer.ArrayEnd();
		writer.TypeEnd();
	}
}
