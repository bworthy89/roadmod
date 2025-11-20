using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
	public string ConditionName { get; private set; }

	public int EnumValue { get; private set; }

	public bool Inverse { get; private set; }

	public ShowIfAttribute(string conditionName, int enumValue, bool inverse = false)
	{
		ConditionName = conditionName;
		EnumValue = enumValue;
		Inverse = inverse;
	}
}
