using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class FilterMenu : Widget
{
	public interface IAdapter
	{
		List<string> availableFilters { get; }

		List<string> activeFilters { get; }

		Action onAvailableFiltersChanged { get; set; }

		void ToggleFilter(string filter, bool active);

		void ClearFilters();
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, string, bool>(group, "toggleFilter", delegate(IWidget widget, string filter, bool active)
			{
				if (widget is FilterMenu filterMenu)
				{
					filterMenu.OnToggleFilter(filter, active);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget>(group, "clearFilters", delegate(IWidget widget)
			{
				if (widget is FilterMenu filterMenu)
				{
					filterMenu.OnClearFilters();
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	private bool m_Dirty;

	private IAdapter m_Adapter;

	public IAdapter adapter
	{
		get
		{
			return m_Adapter;
		}
		set
		{
			if (m_Adapter != null)
			{
				IAdapter obj = m_Adapter;
				obj.onAvailableFiltersChanged = (Action)Delegate.Remove(obj.onAvailableFiltersChanged, new Action(OnAvailableFiltersChanged));
			}
			m_Adapter = value;
			IAdapter obj2 = m_Adapter;
			obj2.onAvailableFiltersChanged = (Action)Delegate.Combine(obj2.onAvailableFiltersChanged, new Action(OnAvailableFiltersChanged));
		}
	}

	private void OnAvailableFiltersChanged()
	{
		m_Dirty = true;
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (m_Dirty)
		{
			widgetChanges |= WidgetChanges.Properties;
		}
		m_Dirty = false;
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("availableFilters");
		writer.Write((IList<string>)adapter.availableFilters);
		writer.PropertyName("activeFilters");
		writer.Write((IList<string>)adapter.activeFilters);
	}

	private void OnToggleFilter(string filter, bool active)
	{
		adapter.ToggleFilter(filter, active);
		m_Dirty = true;
	}

	private void OnClearFilters()
	{
		adapter.ClearFilters();
		m_Dirty = true;
	}
}
