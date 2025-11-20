namespace Game.Notifications;

public static class NotificationsUtils
{
	public const float ICON_VISIBLE_THROUGH_DISTANCE = 100f;

	public static IconLayerMask GetIconLayerMask(IconClusterLayer layer)
	{
		return (IconLayerMask)(1 << (int)layer);
	}
}
