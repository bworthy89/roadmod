using System.Collections.Generic;
using Game.Rendering.CinematicCamera;

namespace Game.UI.InGame;

public class PresetDescriptor
{
	private List<string> m_OptionsId = new List<string>();

	private Dictionary<PhotoModeProperty, float[]> m_Values = new Dictionary<PhotoModeProperty, float[]>();

	public IReadOnlyCollection<string> optionsId => m_OptionsId;

	public IReadOnlyDictionary<PhotoModeProperty, float[]> values => m_Values;

	public bool Validate()
	{
		int count = m_OptionsId.Count;
		foreach (KeyValuePair<PhotoModeProperty, float[]> value in m_Values)
		{
			if (value.Value.Length != count)
			{
				return false;
			}
		}
		return true;
	}

	public void AddOptions(IEnumerable<string> optionIds)
	{
		foreach (string optionId in optionIds)
		{
			AddOption(optionId);
		}
	}

	public void AddOption(string optionId)
	{
		m_OptionsId.Add(optionId);
	}

	public void AddValues(PhotoModeProperty targetProperty, float[] values)
	{
		m_Values.Add(targetProperty, values);
	}
}
