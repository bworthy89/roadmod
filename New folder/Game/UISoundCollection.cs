using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Audio;
using UnityEngine;

namespace Game;

[CreateAssetMenu(menuName = "Colossal/UI/UISoundCollection", order = 1)]
public class UISoundCollection : ScriptableObject
{
	[Serializable]
	public class SoundInfo
	{
		public string m_Name;

		public AudioClip m_Clip;

		[Range(0f, 1f)]
		public float m_Volume = 1f;
	}

	public SoundInfo[] m_Sounds;

	private Dictionary<string, SoundInfo> m_SoundsDict;

	private void OnEnable()
	{
		if (m_Sounds == null)
		{
			m_Sounds = new SoundInfo[0];
		}
		m_SoundsDict = new Dictionary<string, SoundInfo>();
		RefreshSoundsDict();
	}

	public void PlaySound(int soundIndex, float volume = 1f)
	{
		if (soundIndex >= 0 && m_Sounds.Length > soundIndex)
		{
			SoundInfo soundInfo = m_Sounds[soundIndex];
			PlaySound(soundInfo.m_Clip, volume * soundInfo.m_Volume);
		}
	}

	public void PlaySound(string soundName, float volume = 1f)
	{
		if (m_SoundsDict.TryGetValue(soundName, out var value))
		{
			PlaySound(value.m_Clip, volume * value.m_Volume);
		}
	}

	private void PlaySound([NotNull] AudioClip clip, float volume)
	{
		if ((bool)Camera.main)
		{
			AudioManager.instance?.PlayUISound(clip, volume);
		}
	}

	public void RefreshSoundsDict()
	{
		m_SoundsDict.Clear();
		SoundInfo[] sounds = m_Sounds;
		foreach (SoundInfo soundInfo in sounds)
		{
			m_SoundsDict[soundInfo.m_Name] = soundInfo;
		}
	}
}
