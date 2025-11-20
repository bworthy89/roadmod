using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Prefabs.Modes;

[Serializable]
public class GameModeRule
{
	public enum ArgumentUnit
	{
		integer,
		percentage,
		money,
		xp,
		custom
	}

	private static Dictionary<ArgumentUnit, string> kUnitDict = new Dictionary<ArgumentUnit, string>
	{
		{
			ArgumentUnit.integer,
			"integer"
		},
		{
			ArgumentUnit.percentage,
			"percentage"
		},
		{
			ArgumentUnit.money,
			"money"
		},
		{
			ArgumentUnit.xp,
			"xp"
		},
		{
			ArgumentUnit.custom,
			"custom"
		}
	};

	[Tooltip("The id of the term in the localization file. Do include the 'Menu.GAME_MODE_RULES' prefix.")]
	public string m_Term;

	[Tooltip("If the text has argument, use this field to define the argument name.")]
	public string m_ArgName;

	[Tooltip("If the text has argument, use this field to define the argument value.")]
	public int m_ArgValue;

	[Tooltip("The unit for argument value. Can be 'integer', 'percentage','money','xp', 'custom',v.v..")]
	public ArgumentUnit m_ArgUnit;

	public string GetUnit()
	{
		return kUnitDict[m_ArgUnit];
	}
}
