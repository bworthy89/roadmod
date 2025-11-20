using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public class AssetDatabaseDebugUI : IDisposable
{
	public AssetDatabaseDebugUI()
	{
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe(Rebuild, delegate(AssetChangedEventArgs args)
		{
			ChangeType change = args.change;
			return change == ChangeType.DatabaseRegistered || change == ChangeType.DatabaseUnregistered;
		});
	}

	public void Dispose()
	{
		AssetDatabase.global.onAssetDatabaseChanged.Unsubscribe(Rebuild);
	}

	private void Rebuild(AssetChangedEventArgs args = default(AssetChangedEventArgs))
	{
		DebugSystem.Rebuild(BuildAssetDatabaseDebugUI);
	}

	[DebugTab("Asset Database", -20)]
	private List<DebugUI.Widget> BuildAssetDatabaseDebugUI()
	{
		DebugUI.Foldout foldout = new DebugUI.Foldout
		{
			displayName = "Asset Database Changed Handlers"
		};
		ICollection<(EventDelegate<AssetChangedEventArgs> handler, Predicate<AssetChangedEventArgs> filter)> globalChangedHandlers = AssetDatabase.global.onAssetDatabaseChanged.Handlers;
		foldout.children.Add(new DebugUI.Value
		{
			displayName = "Active Count",
			getter = () => globalChangedHandlers.Count
		});
		foreach (var item2 in globalChangedHandlers)
		{
			foldout.children.Add(new DebugUI.Container
			{
				displayName = item2.handler.Method.DeclaringType.Name + "." + item2.handler.Method.Name
			});
		}
		DebugUI.Container container = new DebugUI.Container
		{
			children = { (DebugUI.Widget)new DebugUI.Foldout
			{
				displayName = "Global database",
				children = 
				{
					(DebugUI.Widget)new DebugUI.Value
					{
						displayName = "Global mipbias",
						getter = () => AssetDatabase.global.mipBias
					},
					(DebugUI.Widget)new DebugUI.Value
					{
						displayName = "Asset count",
						getter = () => AssetDatabase.global.count
					},
					(DebugUI.Widget)new DebugUI.Value
					{
						displayName = "Hostname",
						getter = () => AssetDatabase.global.hostname
					},
					(DebugUI.Widget)foldout
				}
			} }
		};
		foreach (IAssetDatabase database in AssetDatabase.global.databases)
		{
			DebugUI.Foldout item = new DebugUI.Foldout
			{
				displayName = database.name,
				children = 
				{
					(DebugUI.Widget)new DebugUI.Value
					{
						displayName = "Asset count",
						getter = () => database.count
					},
					(DebugUI.Widget)new DebugUI.Value
					{
						displayName = "Hostname",
						getter = () => database.hostname
					}
				}
			};
			DebugUI.Foldout foldout2 = new DebugUI.Foldout
			{
				displayName = "Asset Database Changed Handlers"
			};
			ICollection<(EventDelegate<AssetChangedEventArgs> handler, Predicate<AssetChangedEventArgs> filter)> changedHandlers = database.onAssetDatabaseChanged.Handlers;
			foldout2.children.Add(new DebugUI.Value
			{
				displayName = "Active Count",
				getter = () => changedHandlers.Count
			});
			foreach (var item3 in changedHandlers)
			{
				foldout2.children.Add(new DebugUI.Container
				{
					displayName = item3.handler.Method.DeclaringType.Name + "." + item3.handler.Method.Name
				});
			}
			container.children.Add(item);
		}
		return new List<DebugUI.Widget>
		{
			container,
			new DebugUI.Button
			{
				displayName = "Apply settings",
				action = delegate
				{
					GameManager.instance.settings.Apply();
				}
			},
			new DebugUI.Button
			{
				displayName = "Reset settings",
				action = delegate
				{
					GameManager.instance.settings.Reset();
				}
			},
			new DebugUI.Button
			{
				displayName = "Refresh",
				action = delegate
				{
					Rebuild();
				}
			}
		};
	}
}
