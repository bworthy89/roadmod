using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.Simulation;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Editor;

public class SeasonsField : Widget
{
	public struct SeasonCurves : IJsonWritable, IJsonReadable
	{
		public AnimationCurve m_Temperature;

		public AnimationCurve m_Precipitation;

		public AnimationCurve m_Cloudiness;

		public AnimationCurve m_Aurora;

		public AnimationCurve m_Fog;

		public void Write(IJsonWriter writer)
		{
			if (m_Temperature != null)
			{
				writer.TypeBegin("SeasonsCurves");
				writer.PropertyName("temperature");
				writer.Write(m_Temperature);
				writer.PropertyName("precipitation");
				writer.Write(m_Precipitation);
				writer.PropertyName("cloudiness");
				writer.Write(m_Cloudiness);
				writer.PropertyName("aurora");
				writer.Write(m_Aurora);
				writer.PropertyName("fog");
				writer.Write(m_Fog);
				writer.TypeEnd();
			}
		}

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("temperature");
			reader.Read(out m_Temperature);
			reader.ReadProperty("precipitation");
			reader.Read(out m_Precipitation);
			reader.ReadProperty("cloudiness");
			reader.Read(out m_Cloudiness);
			reader.ReadProperty("aurora");
			reader.Read(out m_Aurora);
			reader.ReadProperty("fog");
			reader.Read(out m_Fog);
			reader.ReadMapEnd();
		}
	}

	public struct Season : IJsonWritable, IEquatable<Season>
	{
		public Entity entity;

		public string m_Name;

		public float m_StartTime;

		public float m_TempNightDay;

		public float m_TempDeviationNightDay;

		public float m_CloudChance;

		public float m_CloudAmount;

		public float m_CloudAmountDeviation;

		public float m_PrecipitationChance;

		public float m_PrecipitationAmount;

		public float m_PrecipitationAmountDeviation;

		public float m_Turbulence;

		public float m_AuroraAmount;

		public float m_AuroraChance;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("name");
			writer.Write(m_Name);
			writer.PropertyName("startTime");
			writer.Write(m_StartTime);
			writer.PropertyName("tempNightDay");
			writer.Write(m_TempNightDay);
			writer.PropertyName("tempDeviationNightDay");
			writer.Write(m_TempDeviationNightDay);
			writer.PropertyName("cloudChance");
			writer.Write(m_CloudChance);
			writer.PropertyName("cloudAmount");
			writer.Write(m_CloudAmount);
			writer.PropertyName("cloudAmountDeviation");
			writer.Write(m_CloudAmountDeviation);
			writer.PropertyName("cloudAmountDeviation");
			writer.Write(m_PrecipitationChance);
			writer.PropertyName("precipitationAmount");
			writer.Write(m_PrecipitationAmount);
			writer.PropertyName("precipitationAmountDeviation");
			writer.Write(m_PrecipitationAmountDeviation);
			writer.PropertyName("turbulence");
			writer.Write(m_Turbulence);
			writer.PropertyName("auroraAmount");
			writer.Write(m_AuroraAmount);
			writer.PropertyName("auroraChance");
			writer.Write(m_AuroraChance);
			writer.TypeEnd();
		}

		public bool Equals(Season other)
		{
			return m_Name.Equals(other.m_Name);
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
			return (m_Name.GetHashCode() * 397) ^ m_Name.GetHashCode();
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

		IEnumerable<ClimateSystem.SeasonInfo> seasons { get; set; }

		SeasonCurves curves { get; set; }

		void RebuildCurves();
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new CallBinding<IWidget, ClimateSystem.SeasonInfo, int>(group, "onUpdateSeason", delegate(IWidget widget, ClimateSystem.SeasonInfo season)
			{
				int result = -1;
				if (widget is SeasonsField seasonsField)
				{
					for (int i = 0; i < seasonsField.m_Seasons.Count; i++)
					{
						if (seasonsField.m_Seasons[i].m_NameID == season.m_NameID)
						{
							seasonsField.m_Seasons[i] = season;
							result = i;
						}
					}
					seasonsField.adapter.seasons = seasonsField.m_Seasons;
					seasonsField.adapter.RebuildCurves();
					widget.Update();
				}
				return result;
			}, pathResolver, new ValueReader<ClimateSystem.SeasonInfo>());
		}
	}

	private Entity m_SelectedSeason;

	private List<ClimateSystem.SeasonInfo> m_Seasons = new List<ClimateSystem.SeasonInfo>();

	private SeasonCurves m_SeasonCurves;

	public IAdapter adapter { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges num = base.Update();
		m_Seasons = adapter.seasons.ToList();
		m_SeasonCurves = adapter.curves;
		return num | WidgetChanges.Properties;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("seasons");
		writer.Write((IList<ClimateSystem.SeasonInfo>)m_Seasons);
		writer.PropertyName("curves");
		writer.Write(m_SeasonCurves);
	}
}
