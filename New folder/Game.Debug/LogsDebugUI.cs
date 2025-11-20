using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game.SceneFlow;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public static class LogsDebugUI
{
	private static void Rebuild()
	{
		DebugSystem.Rebuild(BuildLogsDebugUI);
	}

	[DebugTab("Logs", 0)]
	private static List<DebugUI.Widget> BuildLogsDebugUI()
	{
		DebugUI.Container container = new DebugUI.Container();
		List<Level> levels = Level.GetLevels().ToList();
		foreach (ILog log in LogManager.GetAllLoggers())
		{
			container.children.Add(new DebugUI.EnumField
			{
				displayName = log.name,
				getter = () => log.effectivenessLevel.severity,
				setter = delegate(int value)
				{
					log.effectivenessLevel = Level.GetLevel(value);
				},
				getIndex = () => levels.FindIndex((Level level) => level == log.effectivenessLevel),
				setIndex = delegate(int index)
				{
					log.effectivenessLevel = levels[index = (index + levels.Count) % levels.Count];
				},
				enumNames = levels.Select((Level level) => ToTitleCase(level.name)).ToArray(),
				enumValues = levels.Select((Level level) => level.severity).ToArray()
			});
			DebugUI.Container container2 = new DebugUI.Container();
			container2.children.Add(new DebugUI.BoolField
			{
				displayName = "Show errors in UI",
				getter = () => log.showsErrorsInUI,
				setter = delegate(bool v)
				{
					log.showsErrorsInUI = v;
				}
			});
			container2.children.Add(new DebugUI.BoolField
			{
				displayName = "Log stack trace",
				getter = () => log.logStackTrace,
				setter = delegate(bool v)
				{
					log.logStackTrace = v;
				}
			});
			if (GameManager.instance.configuration.qaDeveloperMode)
			{
				container2.children.Add(new DebugUI.BoolField
				{
					displayName = "Disable backtrace",
					getter = () => log.disableBacktrace,
					setter = delegate(bool v)
					{
						log.disableBacktrace = v;
					}
				});
			}
			container2.children.Add(new DebugUI.EnumField
			{
				displayName = "Show stack trace below levels",
				getter = () => log.showsStackTraceAboveLevels.severity,
				setter = delegate(int value)
				{
					log.showsStackTraceAboveLevels = Level.GetLevel(value);
				},
				getIndex = () => levels.FindIndex((Level level) => level == log.showsStackTraceAboveLevels),
				setIndex = delegate(int index)
				{
					log.showsStackTraceAboveLevels = levels[index = (index + levels.Count) % levels.Count];
				},
				enumNames = levels.Select((Level level) => ToTitleCase(level.name)).ToArray(),
				enumValues = levels.Select((Level level) => level.severity).ToArray()
			});
			container.children.Add(container2);
		}
		return new List<DebugUI.Widget>
		{
			new DebugUI.Button
			{
				displayName = "Refresh",
				action = Rebuild
			},
			container
		};
		static GUIContent ToTitleCase(string input)
		{
			return new GUIContent(char.ToUpper(input[0]) + input.Substring(1).ToLower());
		}
	}
}
