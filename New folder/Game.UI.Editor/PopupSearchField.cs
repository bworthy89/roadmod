using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class PopupSearchField : Widget, ISettable, IWidget, IJsonWritable
{
	public interface IAdapter : SearchField.IAdapter
	{
		bool searchQueryIsFavorite { get; }

		IEnumerable<Suggestion> searchSuggestions { get; }

		void SetFavorite(string query, bool favorite);
	}

	public struct Suggestion : IComparable<Suggestion>, IEquatable<Suggestion>, IJsonWritable
	{
		public string value { get; set; }

		public bool favorite { get; set; }

		public Suggestion(string value, bool favorite)
		{
			this.value = value;
			this.favorite = favorite;
		}

		public static Suggestion NonFavorite(string value)
		{
			return new Suggestion(value, favorite: false);
		}

		public static Suggestion Favorite(string value)
		{
			return new Suggestion(value, favorite: true);
		}

		public int CompareTo(Suggestion other)
		{
			if (favorite == other.favorite)
			{
				return string.CompareOrdinal(value, other.value);
			}
			return favorite.CompareTo(other.favorite);
		}

		public bool Equals(Suggestion other)
		{
			if (value == other.value)
			{
				return favorite == other.favorite;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Suggestion other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (value.GetHashCode() * 397) ^ favorite.GetHashCode();
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("value");
			writer.Write(value);
			writer.PropertyName("favorite");
			writer.Write(favorite);
			writer.TypeEnd();
		}
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, string, bool>(group, "setSearchFavorite", delegate(IWidget widget, string query, bool favorite)
			{
				if (widget is PopupSearchField popupSearchField)
				{
					popupSearchField.adapter.SetFavorite(query, favorite);
				}
			}, pathResolver);
		}
	}

	private string m_Value;

	private bool m_ValueIsFavorite;

	private List<Suggestion> m_Suggestions = new List<Suggestion>();

	public IAdapter adapter { get; set; }

	public bool hasFavorites { get; set; }

	public bool shouldTriggerValueChangedEvent => true;

	public void SetValue(IJsonReader reader)
	{
		reader.Read(out string value);
		SetValue(value);
	}

	public void SetValue(string value)
	{
		if (value != m_Value)
		{
			adapter.searchQuery = value;
		}
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (adapter.searchQuery != m_Value)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Value = adapter.searchQuery;
		}
		if (adapter.searchQueryIsFavorite != m_ValueIsFavorite)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_ValueIsFavorite = adapter.searchQueryIsFavorite;
		}
		if (!adapter.searchSuggestions.SequenceEqual(m_Suggestions))
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Suggestions.Clear();
			m_Suggestions.AddRange(adapter.searchSuggestions);
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("hasFavorites");
		writer.Write(hasFavorites);
		writer.PropertyName("value");
		writer.Write(m_Value ?? string.Empty);
		writer.PropertyName("valueIsFavorite");
		writer.Write(m_ValueIsFavorite);
		writer.PropertyName("suggestions");
		writer.Write((IList<Suggestion>)m_Suggestions);
	}
}
