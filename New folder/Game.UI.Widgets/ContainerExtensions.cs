using System.Collections.Generic;
using Colossal.Annotations;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public static class ContainerExtensions
{
	[CanBeNull]
	public static IWidget FindChild(this IWidget widget, PathSegment path)
	{
		return FindChild(widget.visibleChildren, path);
	}

	[CanBeNull]
	public static T FindChild<T>(IEnumerable<T> children, PathSegment path) where T : IWidget
	{
		if (path != PathSegment.Empty && children != null)
		{
			foreach (T child in children)
			{
				if (child.path == path)
				{
					return child;
				}
			}
		}
		return default(T);
	}

	public static void SetDefaults<T>(IList<T> children) where T : IWidget
	{
		for (int i = 0; i < children.Count; i++)
		{
			T val = children[i];
			if (val.path.m_Key == null)
			{
				PathSegment path = new PathSegment(i);
				val.path = path;
			}
			if (val is INamed { displayName: { isEmpty: not false } } named)
			{
				named.displayName = LocalizedString.Value($"<{i}>");
			}
		}
	}
}
