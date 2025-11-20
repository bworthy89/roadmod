using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Editor;

public class SeasonWheel : Widget
{
	public struct Season : IJsonWritable, IEquatable<Season>
	{
		public Entity entity;

		public Bounds1 startTimeOfYear;

		public float temperature;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("startTimeOfYear");
			writer.Write(startTimeOfYear);
			writer.PropertyName("temperature");
			writer.Write(temperature);
			writer.TypeEnd();
		}

		public bool Equals(Season other)
		{
			if (startTimeOfYear.Equals(other.startTimeOfYear))
			{
				return temperature.Equals(other.temperature);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Season other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (startTimeOfYear.GetHashCode() * 397) ^ temperature.GetHashCode();
		}

		public static bool operator ==(Season left, Season right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Season left, Season right)
		{
			return !left.Equals(right);
		}
	}

	public interface IAdapter
	{
		Entity selectedSeason { get; set; }

		IEnumerable<Season> seasons { get; }

		void SetStartTimeOfYear(Entity season, Bounds1 startTimeOfYear);
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, Entity>(group, "setSelectedSeason", delegate(IWidget widget, Entity season)
			{
				if (widget is SeasonWheel seasonWheel)
				{
					seasonWheel.adapter.selectedSeason = season;
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, Entity, Bounds1>(group, "setSeasonStartTimeOfYear", delegate(IWidget widget, Entity season, Bounds1 startTimeOfYear)
			{
				if (widget is SeasonWheel seasonWheel)
				{
					seasonWheel.adapter.SetStartTimeOfYear(season, startTimeOfYear);
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	private Entity m_SelectedSeason;

	private List<Season> m_Seasons = new List<Season>();

	public IAdapter adapter { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (adapter.selectedSeason != m_SelectedSeason)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_SelectedSeason = adapter.selectedSeason;
		}
		if (!m_Seasons.SequenceEqual(adapter.seasons))
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Seasons.Clear();
			m_Seasons.AddRange(adapter.seasons);
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("selectedSeason");
		writer.Write(m_SelectedSeason);
		writer.PropertyName("seasons");
		writer.Write((IList<Season>)m_Seasons);
	}
}
