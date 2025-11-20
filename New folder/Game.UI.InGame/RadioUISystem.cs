using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game.Audio;
using Game.Audio.Radio;
using Game.Prefabs;
using Game.Rendering;
using Game.Settings;
using Game.UI.Localization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class RadioUISystem : UISystemBase
{
	public class ClipInfo : IJsonWritable
	{
		public string title;

		[CanBeNull]
		public string info;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("radio.Clip");
			writer.PropertyName("title");
			writer.Write(title);
			writer.PropertyName("info");
			writer.Write(info);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "radio";

	private PrefabSystem m_PrefabSystem;

	private Radio m_Radio;

	private GamePanelUISystem m_GamePanelUISystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private ValueBinding<bool> m_PausedBinding;

	private ValueBinding<bool> m_MutedBinding;

	private ValueBinding<bool> m_SkipAds;

	private GetterValueBinding<Radio.RadioNetwork[]> m_NetworksBinding;

	private GetterValueBinding<Radio.RuntimeRadioChannel[]> m_StationsBinding;

	private ValueBinding<ClipInfo> m_CurrentSegmentBinding;

	private EventBinding m_SegmentChangedBinding;

	private Dictionary<string, string> m_LastSelectedStations;

	private CachedLocalizedStringBuilder<string> m_EmergencyMessages;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_Radio = AudioManager.instance.radio;
		m_GamePanelUISystem = base.World.GetOrCreateSystemManaged<GamePanelUISystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_GamePanelUISystem.SetDefaultArgs(new RadioPanel());
		AddUpdateBinding(new GetterValueBinding<bool>("radio", "enabled", () => SharedSettings.instance.audio.radioActive));
		AddUpdateBinding(new GetterValueBinding<float>("radio", "volume", () => SharedSettings.instance.audio.radioVolume));
		AddBinding(m_PausedBinding = new ValueBinding<bool>("radio", "paused", m_Radio.paused));
		AddBinding(m_MutedBinding = new ValueBinding<bool>("radio", "muted", m_Radio.muted));
		AddBinding(m_SkipAds = new ValueBinding<bool>("radio", "skipAds", m_Radio.skipAds));
		AddUpdateBinding(new GetterValueBinding<bool>("radio", "emergencyMode", () => m_Radio.hasEmergency));
		AddUpdateBinding(new GetterValueBinding<bool>("radio", "emergencyFocusable", () => m_Radio.emergencyTarget != Entity.Null));
		AddUpdateBinding(new GetterValueBinding<Entity>("radio", "emergencyMessage", () => m_Radio.emergency, new DelegateWriter<Entity>(WriteEmergencyMessage)));
		AddUpdateBinding(new GetterValueBinding<string>("radio", "selectedNetwork", () => AudioManager.instance.radio.currentChannel?.network, ValueWriters.Nullable(new StringWriter())));
		AddUpdateBinding(new GetterValueBinding<string>("radio", "selectedStation", () => AudioManager.instance.radio.currentChannel?.name, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_NetworksBinding = new GetterValueBinding<Radio.RadioNetwork[]>("radio", "networks", () => AudioManager.instance.radio.networkDescriptors, new ArrayWriter<Radio.RadioNetwork>(new ValueWriter<Radio.RadioNetwork>())));
		AddBinding(m_StationsBinding = new GetterValueBinding<Radio.RuntimeRadioChannel[]>("radio", "stations", () => AudioManager.instance.radio.radioChannelDescriptors, new ArrayWriter<Radio.RuntimeRadioChannel>(new ValueWriter<Radio.RuntimeRadioChannel>())));
		AddBinding(m_CurrentSegmentBinding = new ValueBinding<ClipInfo>("radio", "currentSegment", GetCurrentClipInfo(), ValueWriters.Nullable(new ValueWriter<ClipInfo>())));
		AddBinding(m_SegmentChangedBinding = new EventBinding("radio", "segmentChanged"));
		AddBinding(new TriggerBinding<float>("radio", "setVolume", SetVolume));
		AddBinding(new TriggerBinding<bool>("radio", "setPaused", SetPaused));
		AddBinding(new TriggerBinding<bool>("radio", "setMuted", SetMuted));
		AddBinding(new TriggerBinding<bool>("radio", "setSkipAds", SetSkipAds));
		AddBinding(new TriggerBinding("radio", "playPrevious", PlayPrevious));
		AddBinding(new TriggerBinding("radio", "playNext", PlayNext));
		AddBinding(new TriggerBinding("radio", "focusEmergency", FocusEmergency));
		AddBinding(new TriggerBinding<string>("radio", "selectNetwork", SelectNetwork));
		AddBinding(new TriggerBinding<string>("radio", "selectStation", SelectStation));
		m_EmergencyMessages = CachedLocalizedStringBuilder<string>.Id((string name) => "Radio.EMERGENCY_MESSAGE[" + name + "]");
		m_LastSelectedStations = new Dictionary<string, string>();
		Radio radio = m_Radio;
		radio.Reloaded = (Radio.OnRadioEvent)Delegate.Combine(radio.Reloaded, new Radio.OnRadioEvent(OnRadioReloaded));
		Radio radio2 = m_Radio;
		radio2.ProgramChanged = (Radio.OnRadioEvent)Delegate.Combine(radio2.ProgramChanged, new Radio.OnRadioEvent(OnProgramChanged));
		Radio radio3 = m_Radio;
		radio3.ClipChanged = (Radio.OnClipChanged)Delegate.Combine(radio3.ClipChanged, new Radio.OnClipChanged(OnClipChanged));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		Radio radio = m_Radio;
		radio.Reloaded = (Radio.OnRadioEvent)Delegate.Remove(radio.Reloaded, new Radio.OnRadioEvent(OnRadioReloaded));
		Radio radio2 = m_Radio;
		radio2.ProgramChanged = (Radio.OnRadioEvent)Delegate.Remove(radio2.ProgramChanged, new Radio.OnRadioEvent(OnProgramChanged));
		Radio radio3 = m_Radio;
		radio3.ClipChanged = (Radio.OnClipChanged)Delegate.Remove(radio3.ClipChanged, new Radio.OnClipChanged(OnClipChanged));
		base.OnDestroy();
	}

	private void WriteEmergencyMessage(IJsonWriter writer, Entity entity)
	{
		if (entity != Entity.Null)
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(m_Radio.emergency);
			writer.Write(m_EmergencyMessages[prefab.name]);
		}
		else
		{
			writer.WriteNull();
		}
	}

	private AudioAsset.Metatag GetMetaType(Radio.SegmentType type)
	{
		return type switch
		{
			Radio.SegmentType.Playlist => AudioAsset.Metatag.Artist, 
			Radio.SegmentType.Commercial => AudioAsset.Metatag.Brand, 
			_ => AudioAsset.Metatag.Artist, 
		};
	}

	private ClipInfo GetClipInfo(Radio radio, AudioAsset asset)
	{
		if (asset != null)
		{
			if (asset.GetMetaTag(AudioAsset.Metatag.Type) == "Music")
			{
				return new ClipInfo
				{
					title = asset.GetMetaTag(AudioAsset.Metatag.Title),
					info = asset.GetMetaTag(AudioAsset.Metatag.Artist)
				};
			}
			return new ClipInfo
			{
				title = radio.currentChannel.name,
				info = radio.currentChannel.currentProgram.name
			};
		}
		return null;
	}

	private ClipInfo GetCurrentClipInfo()
	{
		return GetClipInfo(m_Radio, m_Radio.currentClip.m_Asset);
	}

	private void OnClipChanged(Radio radio, AudioAsset asset)
	{
		m_StationsBinding.TriggerUpdate();
		m_CurrentSegmentBinding.Update(GetClipInfo(radio, asset));
	}

	private void OnRadioReloaded(Radio radio)
	{
		m_NetworksBinding.Update();
		m_StationsBinding.Update();
		m_SkipAds.Update(radio.skipAds);
	}

	private void OnProgramChanged(Radio radio)
	{
		m_StationsBinding.TriggerUpdate();
	}

	private void SetVolume(float volume)
	{
		SharedSettings.instance.audio.radioVolume = volume;
		SharedSettings.instance.audio.Apply();
	}

	private void SetPaused(bool paused)
	{
		m_Radio.paused = paused;
		m_PausedBinding.Update(paused);
	}

	private void SetMuted(bool muted)
	{
		m_Radio.muted = muted;
		m_MutedBinding.Update(muted);
	}

	private void SetSkipAds(bool skipAds)
	{
		m_Radio.skipAds = skipAds;
		m_SkipAds.Update(skipAds);
	}

	private void PlayPrevious()
	{
		AudioManager.instance.radio.PreviousSong();
	}

	private void PlayNext()
	{
		AudioManager.instance.radio.NextSong();
	}

	private void FocusEmergency()
	{
		if (m_CameraUpdateSystem.orbitCameraController != null && m_Radio.emergencyTarget != Entity.Null)
		{
			m_CameraUpdateSystem.orbitCameraController.followedEntity = m_Radio.emergencyTarget;
			m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
		}
	}

	private void SelectNetwork(string name)
	{
		if (m_LastSelectedStations.TryGetValue(name, out var value))
		{
			SelectStation(value);
			return;
		}
		Radio.RuntimeRadioChannel[] radioChannelDescriptors = AudioManager.instance.radio.radioChannelDescriptors;
		foreach (Radio.RuntimeRadioChannel runtimeRadioChannel in radioChannelDescriptors)
		{
			if (runtimeRadioChannel.network == name)
			{
				SelectStation(runtimeRadioChannel.name);
				break;
			}
		}
	}

	private void SelectStation(string name)
	{
		Radio.RuntimeRadioChannel radioChannel = AudioManager.instance.radio.GetRadioChannel(name);
		if (radioChannel != null)
		{
			Radio.RuntimeRadioChannel currentChannel = AudioManager.instance.radio.currentChannel;
			if (currentChannel != null)
			{
				m_LastSelectedStations[currentChannel.network] = currentChannel.name;
			}
			AudioManager.instance.radio.currentChannel = radioChannel;
		}
	}

	[Preserve]
	public RadioUISystem()
	{
	}
}
