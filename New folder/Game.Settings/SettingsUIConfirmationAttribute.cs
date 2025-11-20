using System;

namespace Game.Settings;

public class SettingsUIConfirmationAttribute : Attribute
{
	public readonly string confirmMessageValue;

	public readonly string confirmMessageId;

	public SettingsUIConfirmationAttribute(string overrideConfirmMessageId = null, string overrideConfirmMessageValue = null)
	{
		confirmMessageValue = overrideConfirmMessageValue;
		confirmMessageId = overrideConfirmMessageId;
	}
}
