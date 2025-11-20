using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public class WatchersDebugUI : IDisposable
{
	private DebugWatchSystem m_WatchSystem;

	private void Rebuild()
	{
		DebugSystem.Rebuild(BuildWatchersDebugUI);
	}

	public void Dispose()
	{
		m_WatchSystem.Enabled = m_WatchSystem.watches.Count > 0;
	}

	[DebugTab("Watchers", -975)]
	private List<DebugUI.Widget> BuildWatchersDebugUI(World world)
	{
		m_WatchSystem = world.GetOrCreateSystemManaged<DebugWatchSystem>();
		m_WatchSystem.Enabled = true;
		List<DebugUI.Widget> list = new List<DebugUI.Widget>();
		list.Add(new DebugUI.Button
		{
			displayName = "Refresh System List",
			action = Rebuild
		});
		list.Add(new DebugUI.Button
		{
			displayName = "Clear Watches",
			action = m_WatchSystem.ClearWatches
		});
		list.AddRange(m_WatchSystem.BuildSystemFoldouts());
		return list;
	}
}
