using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.UI.InGame;

public class NotificationInfo : IComparable<NotificationInfo>
{
	private readonly List<Entity> m_Targets;

	public Entity entity { get; }

	public Entity target { get; }

	public int priority { get; }

	public int count => m_Targets?.Count ?? 0;

	public NotificationInfo(Notification notification)
	{
		entity = notification.entity;
		target = notification.target;
		priority = (int)notification.priority;
		m_Targets = new List<Entity>(10) { notification.target };
	}

	public void AddTarget(Entity otherTarget)
	{
		if (!m_Targets.Contains(otherTarget))
		{
			m_Targets.Add(otherTarget);
		}
	}

	public int CompareTo(NotificationInfo other)
	{
		return priority - other.priority;
	}
}
