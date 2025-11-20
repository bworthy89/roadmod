using Game.Prefabs;
using Unity.Mathematics;

namespace Game.UI.InGame;

public static class ModifierUIUtils
{
	public static float GetModifierDelta(ModifierValueMode mode, float delta)
	{
		return mode switch
		{
			ModifierValueMode.Relative => 100f * delta, 
			ModifierValueMode.InverseRelative => 100f * (1f / math.max(0.001f, 1f + delta) - 1f), 
			_ => delta, 
		};
	}

	public static string GetModifierUnit(ModifierValueMode mode)
	{
		if (mode == ModifierValueMode.Absolute)
		{
			return "floatSingleFraction";
		}
		return "percentage";
	}
}
