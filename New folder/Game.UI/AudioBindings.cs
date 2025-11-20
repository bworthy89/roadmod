using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI;

public class AudioBindings : CompositeBinding
{
	private const string kGroup = "audio";

	private UISoundCollection m_SoundCollection;

	public AudioBindings()
	{
		m_SoundCollection = Resources.Load<UISoundCollection>("Audio/UI Sounds");
		AddBinding(new TriggerBinding<string, float>("audio", "playSound", PlayUISound));
	}

	private void PlayUISound(string soundName, float volume)
	{
		if (m_SoundCollection != null)
		{
			m_SoundCollection.PlaySound(soundName, volume);
		}
	}
}
