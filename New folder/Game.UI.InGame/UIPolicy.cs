using System;
using Colossal.UI.Binding;
using Unity.Entities;

namespace Game.UI.InGame;

public readonly struct UIPolicy : IEquatable<UIPolicy>, IComparable<UIPolicy>
{
	private readonly string m_Id;

	private readonly string m_LocalizedName;

	private readonly int m_Priority;

	private readonly string m_Icon;

	private readonly Entity m_Entity;

	private readonly bool m_Locked;

	private readonly string m_UITag;

	private readonly int m_Milestone;

	private readonly bool m_Active;

	private readonly bool m_Slider;

	private readonly UIPolicySlider m_Data;

	public UIPolicy(string id, string localizedName, int priority, string icon, Entity entity, bool active, bool locked, string uiTag, int milestone, bool slider, UIPolicySlider data)
	{
		m_Id = id;
		m_LocalizedName = localizedName;
		m_Priority = priority;
		m_Icon = icon;
		m_Entity = entity;
		m_Active = active;
		m_Locked = locked;
		m_UITag = uiTag;
		m_Milestone = milestone;
		m_Slider = slider;
		m_Data = data;
	}

	public void Write(PrefabUISystem prefabUISystem, IJsonWriter writer)
	{
		writer.TypeBegin(TypeNames.kPolicy);
		writer.PropertyName("id");
		writer.Write(m_Id);
		writer.PropertyName("icon");
		writer.Write(m_Icon);
		writer.PropertyName("entity");
		if (m_Entity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(m_Entity);
		}
		writer.PropertyName("active");
		writer.Write(m_Active);
		writer.PropertyName("locked");
		writer.Write(m_Locked);
		writer.PropertyName("uiTag");
		writer.Write(m_UITag);
		writer.PropertyName("requirements");
		prefabUISystem.BindPrefabRequirements(writer, m_Entity);
		writer.PropertyName("data");
		if (m_Slider)
		{
			writer.Write(m_Data);
		}
		else
		{
			writer.WriteNull();
		}
		writer.TypeEnd();
	}

	public override int GetHashCode()
	{
		return (m_Id, m_Icon, m_Entity, m_Active, m_Slider, m_Data).GetHashCode();
	}

	public int CompareTo(UIPolicy other)
	{
		int milestone = m_Milestone;
		int num = milestone.CompareTo(other.m_Milestone);
		milestone = m_Priority;
		int num2 = milestone.CompareTo(other.m_Priority);
		if (num == 0)
		{
			if (num2 == 0)
			{
				return string.Compare(m_LocalizedName, other.m_LocalizedName, StringComparison.Ordinal);
			}
			return num2;
		}
		return num;
	}

	public override bool Equals(object obj)
	{
		if (obj is UIPolicy other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(UIPolicy other)
	{
		return m_Entity == other.m_Entity;
	}

	public static bool operator ==(UIPolicy left, UIPolicy right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UIPolicy left, UIPolicy right)
	{
		return !left.Equals(right);
	}
}
