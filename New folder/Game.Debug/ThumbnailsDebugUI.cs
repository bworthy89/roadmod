using System.Collections.Generic;
using Game.SceneFlow;
using Game.UI.Thumbnails;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public static class ThumbnailsDebugUI
{
	[DebugTab("Thumbnails", 0)]
	private static List<DebugUI.Widget> BuildThumbnailsDebugUI()
	{
		ThumbnailCache tc = GameManager.instance?.thumbnailCache;
		if (tc != null)
		{
			new DebugUI.Foldout().displayName = "Thumbnails";
			return new List<DebugUI.Widget>
			{
				new DebugUI.Button
				{
					displayName = "Refresh",
					action = delegate
					{
						tc.Refresh();
					}
				}
			};
		}
		return null;
	}
}
