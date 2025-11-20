using System.Collections.Generic;
using System.Threading;
using Colossal.TestFramework;
using Game.SceneFlow;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public static class TestsDebugUI
{
	[DebugTab("Test Scenarios", -21)]
	private static List<DebugUI.Widget> BuildTestScenariosDebugUI()
	{
		if (!GameManager.instance.configuration.qaDeveloperMode)
		{
			return null;
		}
		List<DebugUI.Widget> list = new List<DebugUI.Widget>();
		TestScenarioSystem tss = TestScenarioSystem.instance;
		Dictionary<Category, DebugUI.Foldout> dictionary = new Dictionary<Category, DebugUI.Foldout>();
		foreach (KeyValuePair<string, TestScenarioSystem.Scenario> scenario in tss.scenarios)
		{
			if (!dictionary.TryGetValue(scenario.Value.category, out var value))
			{
				value = new DebugUI.Foldout
				{
					displayName = scenario.Value.category.ToString()
				};
				dictionary.Add(scenario.Value.category, value);
				list.Add(value);
			}
			value.children.Add(new DebugUI.Button
			{
				displayName = scenario.Key,
				action = delegate
				{
					tss.RunScenario(scenario.Key, CancellationToken.None);
				}
			});
		}
		return list;
	}
}
