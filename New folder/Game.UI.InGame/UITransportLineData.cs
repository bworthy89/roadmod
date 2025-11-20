using System;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Routes;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.InGame;

public readonly struct UITransportLineData : IJsonWritable, IComparable<UITransportLineData>
{
	public Entity entity { get; }

	public bool active { get; }

	public bool visible { get; }

	public bool isCargo { get; }

	public Color32 color { get; }

	public int schedule { get; }

	public TransportType type { get; }

	public float length { get; }

	public int stops { get; }

	public int vehicles { get; }

	public int cargo { get; }

	public float usage { get; }

	public UITransportLineData(Entity entity, bool active, bool visible, bool isCargo, Game.Routes.Color color, RouteSchedule schedule, TransportType type, float length, int stops, int vehicles, int cargo, float usage)
	{
		this.entity = entity;
		this.active = active;
		this.visible = visible;
		this.isCargo = isCargo;
		this.color = color.m_Color;
		this.schedule = (int)schedule;
		this.type = type;
		this.length = length;
		this.stops = stops;
		this.vehicles = vehicles;
		this.cargo = cargo;
		this.usage = usage;
	}

	public int CompareTo(UITransportLineData other)
	{
		int num = type.CompareTo(other.type);
		if (num == 0)
		{
			return entity.Index.CompareTo(other.entity.Index);
		}
		return num;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("entity");
		writer.Write(entity);
		writer.PropertyName("active");
		writer.Write(active);
		writer.PropertyName("visible");
		writer.Write(visible);
		writer.PropertyName("isCargo");
		writer.Write(isCargo);
		writer.PropertyName("color");
		writer.Write(color);
		writer.PropertyName("schedule");
		writer.Write(schedule);
		writer.PropertyName("type");
		writer.Write(Enum.GetName(typeof(TransportType), type));
		writer.PropertyName("length");
		writer.Write(length);
		writer.PropertyName("stops");
		writer.Write(stops);
		writer.PropertyName("vehicles");
		writer.Write(vehicles);
		writer.PropertyName("cargo");
		writer.Write(cargo);
		writer.PropertyName("usage");
		writer.Write(usage);
		writer.TypeEnd();
	}
}
