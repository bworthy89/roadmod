using Game.Notifications;
using Unity.Entities;

namespace Game.UI.InGame;

public readonly struct Notification
{
	public Entity entity { get; }

	public Entity target { get; }

	public IconPriority priority { get; }

	public Notification(Entity entity, Entity target, IconPriority priority)
	{
		this.entity = entity;
		this.target = target;
		this.priority = priority;
	}
}
