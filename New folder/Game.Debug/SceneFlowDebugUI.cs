using System;
using System.Collections.Generic;
using Colossal.Logging;
using Game.SceneFlow;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Debug;

[DebugContainer]
public class SceneFlowDebugUI
{
	[DebugTab("Scene Flow", -10)]
	private static List<DebugUI.Widget> BuildSceneFlowDebugUI()
	{
		DebugUI.Foldout foldout = new DebugUI.Foldout();
		foldout.displayName = "Loaded Scenes + RootCount";
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene scene = SceneManager.GetSceneAt(i);
			foldout.children.Add(new DebugUI.Value
			{
				displayName = scene.name,
				getter = () => scene.rootCount
			});
		}
		DebugUI.Foldout foldout2 = new DebugUI.Foldout
		{
			displayName = "Crash tests"
		};
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Exception",
			action = delegate
			{
				throw new Exception("Test exception");
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "AccessViolation",
			action = delegate
			{
				Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Abort",
			action = delegate
			{
				Utils.ForceCrash(ForcedCrashCategory.Abort);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "FatalError",
			action = delegate
			{
				Utils.ForceCrash(ForcedCrashCategory.FatalError);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "MonoAbort",
			action = delegate
			{
				Utils.ForceCrash(ForcedCrashCategory.MonoAbort);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "PureVirtualFunction",
			action = delegate
			{
				Utils.ForceCrash(ForcedCrashCategory.PureVirtualFunction);
			}
		});
		return new List<DebugUI.Widget>
		{
			new DebugUI.Value
			{
				displayName = "Mode",
				getter = () => GameManager.instance.gameMode
			},
			foldout2,
			foldout,
			new DebugUI.Button
			{
				displayName = "Refresh",
				action = delegate
				{
					DebugSystem.Rebuild(BuildSceneFlowDebugUI);
				}
			},
			new DebugUI.Button
			{
				displayName = "Create 1 error",
				action = delegate
				{
					LogManager.FileSystem.Error(new ApplicationException("Create 1 error"));
				}
			},
			new DebugUI.Button
			{
				displayName = "Create 10 error",
				action = delegate
				{
					for (int j = 0; j < 10; j++)
					{
						LogManager.FileSystem.Error(new ApplicationException("Create 10 error"));
					}
				}
			},
			new DebugUI.Button
			{
				displayName = "Spam 1 debug error per frame",
				action = delegate
				{
					ErrorSpammer spammer = new ErrorSpammer(TimeSpan.Zero, 1, 2000);
					GameManager.instance.RegisterUpdater(delegate
					{
						spammer.Update();
						return !spammer.isRunning;
					});
				}
			},
			new DebugUI.Button
			{
				displayName = "Spam 1 debug errors every second",
				action = delegate
				{
					ErrorSpammer spammer = new ErrorSpammer(TimeSpan.FromSeconds(1.0), 1, 2000);
					GameManager.instance.RegisterUpdater(delegate
					{
						spammer.Update();
						return !spammer.isRunning;
					});
				}
			},
			new DebugUI.Button
			{
				displayName = "Spam 5 debug errors every 2 seconds",
				action = delegate
				{
					ErrorSpammer spammer = new ErrorSpammer(TimeSpan.FromSeconds(2.0), 5, 2000);
					GameManager.instance.RegisterUpdater(delegate
					{
						spammer.Update();
						return !spammer.isRunning;
					});
				}
			},
			new DebugUI.Button
			{
				displayName = "Spam 5 debug errors every 5 seconds",
				action = delegate
				{
					ErrorSpammer spammer = new ErrorSpammer(TimeSpan.FromSeconds(5.0), 1, 2000);
					GameManager.instance.RegisterUpdater(delegate
					{
						spammer.Update();
						return !spammer.isRunning;
					});
				}
			},
			new DebugUI.Button
			{
				displayName = "Dismiss all errors",
				action = delegate
				{
					GameManager.instance.userInterface.appBindings.DismissAllErrors();
				}
			}
		};
	}
}
