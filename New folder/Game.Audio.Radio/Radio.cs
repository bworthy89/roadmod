using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Colossal;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.Randomization;
using Colossal.UI.Binding;
using Game.Assets;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Game.Audio.Radio;

public class Radio
{
	public struct ClipInfo
	{
		public AudioAsset m_Asset;

		public SegmentType m_SegmentType;

		public Entity m_Emergency;

		public Entity m_EmergencyTarget;

		public Task<AudioClip> m_LoadTask;

		public int m_ResumeAtPosition;

		public bool m_Replaying;
	}

	public delegate void OnRadioEvent(Radio radio);

	public delegate void OnClipChanged(Radio radio, AudioAsset asset);

	public delegate void OnDemandClips(RuntimeSegment segment);

	public class RadioChannel : IContentPrerequisite
	{
		public string name;

		[CanBeNull]
		public string nameId;

		public string description;

		public string icon;

		public int uiPriority;

		public string network;

		public Program[] programs;

		public string[] contentPrerequisites { get; set; }

		public RuntimeRadioChannel CreateRuntime(string path)
		{
			RuntimeRadioChannel runtimeRadioChannel = new RuntimeRadioChannel();
			runtimeRadioChannel.name = name;
			runtimeRadioChannel.description = description;
			runtimeRadioChannel.icon = icon;
			runtimeRadioChannel.uiPriority = uiPriority;
			runtimeRadioChannel.network = network;
			runtimeRadioChannel.Initialize(this, name + " (" + path + ")");
			return runtimeRadioChannel;
		}
	}

	public class RuntimeRadioChannel : IComparable<RuntimeRadioChannel>, IJsonWritable
	{
		public string name;

		public string description;

		public string icon;

		public int uiPriority;

		public string network;

		private readonly RuntimeProgram kNoProgram = new RuntimeProgram
		{
			name = "No program"
		};

		public RuntimeProgram currentProgram { get; private set; }

		public RuntimeProgram[] schedule { get; private set; }

		public void Initialize(RadioChannel radioChannel, string path)
		{
			BuildRuntimePrograms(radioChannel.programs, path);
		}

		public bool Update(int timeOfDaySeconds)
		{
			bool result = false;
			bool flag = false;
			for (int i = 0; i < schedule.Length; i++)
			{
				RuntimeProgram runtimeProgram = schedule[i];
				if (timeOfDaySeconds >= runtimeProgram.startTime && timeOfDaySeconds < runtimeProgram.endTime && (runtimeProgram.loopProgram || !runtimeProgram.hasEnded))
				{
					if (currentProgram != runtimeProgram || (runtimeProgram.hasEnded && runtimeProgram.loopProgram))
					{
						log.DebugFormat("Channel {1} - Program changed to {0}", runtimeProgram.name, name);
						result = true;
						runtimeProgram.Reset();
					}
					runtimeProgram.active = true;
					currentProgram = runtimeProgram;
					flag = true;
				}
				else
				{
					runtimeProgram.active = false;
				}
			}
			if (!flag)
			{
				currentProgram = null;
			}
			return result;
		}

		private bool IsValidTimestamp(int start, int end)
		{
			if (start != -1 && end != -1)
			{
				return start <= end;
			}
			return false;
		}

		private RuntimeProgram CreateRuntimeProgram(Program p, int startSecs, int endSecs, string path)
		{
			RuntimeProgram runtimeProgram = new RuntimeProgram();
			runtimeProgram.name = p.name;
			runtimeProgram.description = p.description;
			runtimeProgram.startTime = startSecs;
			runtimeProgram.endTime = endSecs;
			runtimeProgram.loopProgram = p.loopProgram;
			runtimeProgram.BuildRuntimeSegments(p, path);
			return runtimeProgram;
		}

		private RuntimeProgram ShallowCopyRuntimeProgram(RuntimeProgram p, int startSecs, int endSecs)
		{
			return new RuntimeProgram
			{
				name = p.name,
				description = p.description,
				startTime = startSecs,
				endTime = endSecs,
				segments = p.segments,
				loopProgram = p.loopProgram
			};
		}

		private void AddRuntimeProgram(Program p, int startSecs, int endSecs, List<RuntimeProgram> schedule, string path)
		{
			if (schedule.Count == 0)
			{
				schedule.Add(CreateRuntimeProgram(p, startSecs, endSecs, path));
				return;
			}
			for (int i = 0; i < schedule.Count; i++)
			{
				RuntimeProgram runtimeProgram = schedule[i];
				if (startSecs > runtimeProgram.startTime && endSecs < runtimeProgram.endTime)
				{
					RuntimeProgram runtimeProgram2 = CreateRuntimeProgram(p, startSecs, endSecs, path);
					schedule.Insert(++i, runtimeProgram2);
					schedule.Insert(++i, ShallowCopyRuntimeProgram(runtimeProgram, runtimeProgram2.endTime, runtimeProgram.endTime));
					runtimeProgram.endTime = runtimeProgram2.startTime;
					return;
				}
				if (startSecs < runtimeProgram.startTime && endSecs > runtimeProgram.startTime)
				{
					RuntimeProgram runtimeProgram3 = CreateRuntimeProgram(p, startSecs, endSecs, path);
					log.WarnFormat("Program '{0}' overlaps with '{1}' in radio channel '{2}'", runtimeProgram3.name, runtimeProgram.name, path);
					return;
				}
				if (startSecs < runtimeProgram.startTime && endSecs < runtimeProgram.startTime)
				{
					RuntimeProgram item = CreateRuntimeProgram(p, startSecs, endSecs, path);
					schedule.Insert(i, item);
					return;
				}
			}
			schedule.Add(CreateRuntimeProgram(p, startSecs, endSecs, path));
		}

		private void BuildRuntimePrograms(Program[] programs, string path)
		{
			if (programs != null)
			{
				List<RuntimeProgram> list = new List<RuntimeProgram>();
				foreach (Program program in programs)
				{
					int num = FormatUtils.ParseTimeToSeconds(program.startTime);
					int num2 = FormatUtils.ParseTimeToSeconds(program.endTime);
					if (IsValidTimestamp(num, num2))
					{
						if (num == num2)
						{
							num2 += 86400;
							if (num2 > 86400)
							{
								AddRuntimeProgram(program, 0, num2 - 86400, list, path);
								AddRuntimeProgram(program, num, 86400, list, path);
							}
							else
							{
								AddRuntimeProgram(program, num, num2, list, path);
							}
						}
						else
						{
							AddRuntimeProgram(program, num, num2, list, path);
						}
					}
					else
					{
						log.WarnFormat("Program '{0}' has invalid timestamps ({3} ({1})->{4} ({2})) in radio channel '{5}' and was ignored!", program.name, num, num2, FormatUtils.FormatTimeDebug(num), FormatUtils.FormatTimeDebug(num2), path);
					}
				}
				schedule = list.ToArray();
			}
			else
			{
				log.WarnFormat("No program founds in radio channel '{0}'", path);
			}
		}

		public int CompareTo(RuntimeRadioChannel other)
		{
			return uiPriority.CompareTo(other.uiPriority);
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("name");
			writer.Write(name);
			writer.PropertyName("description");
			writer.Write(description);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.PropertyName("network");
			writer.Write(network);
			writer.PropertyName("currentProgram");
			writer.Write(currentProgram ?? kNoProgram);
			writer.PropertyName("schedule");
			writer.ArrayBegin(schedule.Length);
			for (int i = 0; i < schedule.Length; i++)
			{
				writer.Write(schedule[i]);
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
	}

	public class RadioNetwork : IComparable<RadioNetwork>, IJsonWritable, IContentPrerequisite
	{
		public string name;

		[CanBeNull]
		public string nameId;

		public string description;

		public string descriptionId;

		public string icon;

		public bool allowAds;

		public int uiPriority;

		public string[] contentPrerequisites { get; set; }

		public int CompareTo(RadioNetwork other)
		{
			return uiPriority.CompareTo(other.uiPriority);
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("name");
			writer.Write(name);
			writer.PropertyName("nameId");
			writer.Write(nameId);
			writer.PropertyName("description");
			writer.Write(description);
			writer.PropertyName("descriptionId");
			writer.Write(descriptionId);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.TypeEnd();
		}
	}

	public class RadioPlayer : IDisposable
	{
		private AudioMixerGroup m_RadioGroup;

		private AudioSource m_AudioSource;

		private Stopwatch m_Timer = new Stopwatch();

		private double m_Elapsed;

		private Spectrum m_Spectrum;

		public bool isCreated => m_AudioSource != null;

		public bool isPlaying => m_AudioSource.isPlaying;

		public int playbackPosition => m_AudioSource.timeSamples;

		public bool muted
		{
			get
			{
				if (!(m_AudioSource != null))
				{
					return false;
				}
				return m_AudioSource.volume == 0f;
			}
			set
			{
				if (m_AudioSource != null)
				{
					m_AudioSource.volume = (value ? 0f : 1f);
				}
			}
		}

		public Texture equalizerTexture => m_Spectrum?.equalizerTexture;

		public string currentClipName
		{
			get
			{
				if (!isCreated || !(m_AudioSource.clip != null))
				{
					return "None";
				}
				return m_AudioSource.clip.name;
			}
		}

		public AudioClip currentClip => m_AudioSource.clip;

		public void Pause()
		{
			if (m_AudioSource != null)
			{
				m_AudioSource.Pause();
			}
			m_Timer.Stop();
		}

		public void Unpause()
		{
			if (m_AudioSource != null)
			{
				m_AudioSource.UnPause();
			}
			m_Timer.Start();
		}

		public RadioPlayer(AudioMixerGroup radioGroup)
		{
			m_RadioGroup = radioGroup;
		}

		public void SetSpectrumSettings(bool enabled, int numSamples, FFTWindow fftWindow, Spectrum.BandType bandType, float spacing, float padding)
		{
			if (m_Spectrum != null)
			{
				if (enabled)
				{
					m_Spectrum.Enable(numSamples, fftWindow, bandType, spacing, padding);
				}
				else
				{
					m_Spectrum.Disable();
				}
			}
		}

		public void UpdateSpectrum()
		{
			if (m_AudioSource != null)
			{
				m_Spectrum.Update(m_AudioSource);
			}
		}

		private AudioSource CreateAudioSource(GameObject listener)
		{
			AudioSource audioSource = listener.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = m_RadioGroup;
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0f;
			return audioSource;
		}

		public void Create(GameObject listener)
		{
			m_AudioSource = CreateAudioSource(listener);
			m_AudioSource.priority = 0;
			m_Spectrum = new Spectrum();
		}

		public void Dispose()
		{
			if (m_Spectrum != null)
			{
				m_Spectrum.Disable();
			}
			if (m_AudioSource != null)
			{
				m_AudioSource.Stop();
				UnityEngine.Object.Destroy(m_AudioSource);
				m_AudioSource = null;
			}
		}

		public static double GetDuration(AudioClip clip)
		{
			return (double)clip.samples / (double)clip.frequency;
		}

		public double GetAudioSourceDuration()
		{
			if (isCreated)
			{
				if (!(m_AudioSource.clip != null))
				{
					return 0.0;
				}
				return GetDuration(m_AudioSource.clip);
			}
			return 0.0;
		}

		public double GetAudioSourceTimeElapsed()
		{
			if (isCreated)
			{
				if (!(m_AudioSource.clip != null))
				{
					return 0.0;
				}
				return (double)m_AudioSource.timeSamples / (double)m_AudioSource.clip.frequency;
			}
			return 0.0;
		}

		public double GetAudioSourceTimeRemaining()
		{
			if (isCreated)
			{
				return GetAudioSourceDuration() - (m_Elapsed + (double)((float)m_Timer.ElapsedMilliseconds / 1000f));
			}
			return 0.0;
		}

		public void Rewind()
		{
			if (m_AudioSource.clip != null)
			{
				m_AudioSource.timeSamples = 0;
				if (m_AudioSource.isPlaying)
				{
					m_AudioSource.Play();
				}
				m_Elapsed = GetAudioSourceTimeElapsed();
				m_Timer.Restart();
			}
		}

		public void Play(AudioClip clip, int timeSamples = 0)
		{
			if (!(m_AudioSource == null))
			{
				m_AudioSource.clip = clip;
				m_AudioSource.timeSamples = timeSamples;
				m_AudioSource.Play();
				m_Elapsed = GetAudioSourceTimeElapsed();
				m_Timer.Restart();
			}
		}
	}

	public class Program
	{
		public string name;

		public string description;

		public string icon;

		public string startTime;

		public string endTime;

		public bool loopProgram;

		public bool pairIntroOutro;

		public Segment[] segments;
	}

	public class RuntimeProgram : IJsonWritable
	{
		public string name;

		public string description;

		public int startTime;

		public int endTime;

		public bool loopProgram;

		public bool active;

		public bool hasEnded;

		private int m_CurrentSegmentId;

		private List<RuntimeSegment> m_Segments = new List<RuntimeSegment>();

		public int duration => endTime - startTime;

		public RuntimeSegment currentSegment
		{
			get
			{
				if (m_CurrentSegmentId < m_Segments.Count)
				{
					return m_Segments[m_CurrentSegmentId];
				}
				return null;
			}
		}

		public IReadOnlyList<RuntimeSegment> segments
		{
			get
			{
				return m_Segments;
			}
			set
			{
				m_Segments = (List<RuntimeSegment>)value;
			}
		}

		public bool GoToNextSegment()
		{
			m_CurrentSegmentId++;
			if (m_CurrentSegmentId >= m_Segments.Count)
			{
				log.DebugFormat("Program {0} has ended (last segment)", name);
				hasEnded = true;
				return false;
			}
			return true;
		}

		public void Reset()
		{
			m_CurrentSegmentId = 0;
			hasEnded = false;
		}

		private AudioAsset[] GetClips(int count, Func<int, int> rand, List<AudioAsset> clips)
		{
			AudioAsset[] array = new AudioAsset[count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = clips[rand(i)];
			}
			return array;
		}

		public void BuildRuntimeSegments(Program program, string path)
		{
			if (program.segments == null)
			{
				return;
			}
			Segment[] array = program.segments;
			foreach (Segment segment in array)
			{
				if (segment.type == SegmentType.Commercial || segment.type == SegmentType.PSA)
				{
					RuntimeSegment item = new RuntimeSegment
					{
						type = segment.type,
						tags = segment.tags,
						clipsCap = segment.clipsCap
					};
					m_Segments.Add(item);
					continue;
				}
				List<AudioAsset> list = new List<AudioAsset>();
				if (segment.clips != null)
				{
					list.Capacity += segment.clips.Length;
					list.AddRange(segment.clips);
				}
				if (segment.tags != null)
				{
					IEnumerable<AudioAsset> assets = AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => segment.tags.All(asset.ContainsTag)));
					list.AddRange(assets);
				}
				if (list.Count > 0)
				{
					AudioAsset[] clips;
					switch (segment.type)
					{
					case SegmentType.Playlist:
					{
						System.Random rnd = new System.Random();
						List<int> randomNumbers = (from x in Enumerable.Range(0, list.Count)
							orderby rnd.Next()
							select x).Take(list.Count).ToList();
						clips = GetClips(list.Count, (int index) => randomNumbers[index], list);
						break;
					}
					case SegmentType.Talkshow:
					case SegmentType.News:
						clips = GetClips(list.Count, (int result) => result, list);
						break;
					default:
						clips = Array.Empty<AudioAsset>();
						break;
					}
					RuntimeSegment item2 = new RuntimeSegment
					{
						type = segment.type,
						tags = segment.tags,
						clipsCap = segment.clipsCap,
						clips = clips
					};
					m_Segments.Add(item2);
				}
				else
				{
					log.WarnFormat("No clips found in a segment '{2}' of program '{0}' founds in radio channel '{1}'. Tags: {3}", program.name, path, segment.type, string.Join(", ", segment.tags));
				}
			}
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("name");
			writer.Write(name);
			writer.PropertyName("description");
			writer.Write(description);
			writer.PropertyName("startTime");
			writer.Write(startTime / 60);
			writer.PropertyName("endTime");
			writer.Write(endTime / 60);
			writer.PropertyName("duration");
			writer.Write(duration / 60);
			writer.PropertyName("active");
			writer.Write(active);
			writer.TypeEnd();
		}
	}

	public enum SegmentType
	{
		Playlist,
		Talkshow,
		PSA,
		Weather,
		News,
		Commercial,
		Emergency
	}

	public class Segment
	{
		public SegmentType type;

		public AudioAsset[] clips;

		public string[] tags;

		public int clipsCap;
	}

	public class RuntimeSegment
	{
		public SegmentType type;

		public string[] tags;

		public int clipsCap;

		private int m_CapCount;

		private int m_CurrentClipId;

		private IReadOnlyList<AudioAsset> m_Clips;

		public AudioAsset currentClip
		{
			get
			{
				if (m_CurrentClipId < m_Clips.Count)
				{
					return m_Clips[m_CurrentClipId];
				}
				return null;
			}
		}

		public bool isSetUp { get; private set; }

		public IReadOnlyList<AudioAsset> clips
		{
			get
			{
				return m_Clips;
			}
			set
			{
				if (m_Clips == value)
				{
					return;
				}
				m_Clips = value;
				durationMs = 0.0;
				foreach (AudioAsset clip in m_Clips)
				{
					durationMs += clip.durationMs;
				}
			}
		}

		public double durationMs { get; private set; }

		public bool GoToNextClip()
		{
			m_CapCount++;
			m_CurrentClipId++;
			if (m_CurrentClipId >= m_Clips.Count)
			{
				log.Debug("Segment has ended (last clip)");
				Reset();
				return false;
			}
			if (m_CapCount >= clipsCap)
			{
				log.DebugFormat("Segment has ended (cap count reached {0}/{1})", m_CapCount, clipsCap);
				m_CapCount = 0;
				return false;
			}
			return true;
		}

		public bool GoToPreviousClip()
		{
			m_CurrentClipId--;
			if (m_CurrentClipId < 0)
			{
				m_CurrentClipId = 0;
				return false;
			}
			return true;
		}

		public void Reset()
		{
			m_CurrentClipId = 0;
			m_CapCount = 0;
			isSetUp = false;
		}

		public void Setup(OnDemandClips clipsCallback = null)
		{
			if (!isSetUp)
			{
				isSetUp = true;
				clipsCallback?.Invoke(this);
			}
		}
	}

	public class Spectrum
	{
		public enum BandType
		{
			FourBand,
			FourBandVisual,
			EightBand,
			TenBand,
			TwentySixBand,
			ThirtyOneBand
		}

		[BurstCompile]
		private struct CreateLevels : IJob
		{
			private NativeArray<float> m_FrequenciesForBands;

			private float m_BandWidth;

			private float m_Falldown;

			private float m_Filter;

			private int m_SpectrumLength;

			private float m_OutputSampleRate;

			[NativeDisableUnsafePtrRestriction]
			private unsafe void* m_SpectrumData;

			private int m_LevelsLength;

			[NativeDisableUnsafePtrRestriction]
			private unsafe void* m_Levels;

			public unsafe CreateLevels(ref NativeArray<float> frequenciesForBands, float bandWidth, float[] spectrumData, Vector4[] levels, float fallSpeed, float sensitivity)
			{
				m_FrequenciesForBands = frequenciesForBands;
				m_BandWidth = bandWidth;
				m_SpectrumLength = spectrumData.Length;
				m_SpectrumData = UnsafeUtility.AddressOf(ref spectrumData[0]);
				m_LevelsLength = levels.Length;
				m_Levels = UnsafeUtility.AddressOf(ref levels[0]);
				m_Falldown = fallSpeed * Time.deltaTime;
				m_Filter = Mathf.Exp((0f - sensitivity) * Time.deltaTime);
				m_OutputSampleRate = UnityEngine.AudioSettings.outputSampleRate;
			}

			private int FrequencyToSpectrumIndex(float f)
			{
				return math.clamp((int)math.floor(f / m_OutputSampleRate * 2f * (float)m_SpectrumLength), 0, m_SpectrumLength - 1);
			}

			public unsafe void Execute()
			{
				Vector4 value = default(Vector4);
				for (int i = 0; i < m_LevelsLength; i++)
				{
					int num = FrequencyToSpectrumIndex(m_FrequenciesForBands[i] / m_BandWidth);
					int num2 = FrequencyToSpectrumIndex(m_FrequenciesForBands[i] * m_BandWidth);
					float num3 = 0f;
					for (int j = num; j <= num2; j++)
					{
						num3 = math.max(num3, UnsafeUtility.ReadArrayElement<float>(m_SpectrumData, j));
					}
					value.x = num3;
					value.y = num3 - (num3 - UnsafeUtility.ReadArrayElement<Vector4>(m_Levels, i).y) * m_Filter;
					value.z = math.max(UnsafeUtility.ReadArrayElement<Vector4>(m_Levels, i).z - m_Falldown, num3);
					value.w = 0f;
					UnsafeUtility.WriteArrayElement(m_Levels, i, value);
				}
			}
		}

		private static readonly float[][] kMiddleFrequenciesForBands = new float[6][]
		{
			new float[4] { 125f, 500f, 1000f, 2000f },
			new float[4] { 250f, 400f, 600f, 800f },
			new float[8] { 63f, 125f, 500f, 1000f, 2000f, 4000f, 6000f, 8000f },
			new float[10] { 31.5f, 63f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f },
			new float[26]
			{
				25f, 31.5f, 40f, 50f, 63f, 80f, 100f, 125f, 160f, 200f,
				250f, 315f, 400f, 500f, 630f, 800f, 1000f, 1250f, 1600f, 2000f,
				2500f, 3150f, 4000f, 5000f, 6300f, 8000f
			},
			new float[31]
			{
				20f, 25f, 31.5f, 40f, 50f, 63f, 80f, 100f, 125f, 160f,
				200f, 250f, 315f, 400f, 500f, 630f, 800f, 1000f, 1250f, 1600f,
				2000f, 2500f, 3150f, 4000f, 5000f, 6300f, 8000f, 10000f, 12500f, 16000f,
				20000f
			}
		};

		private static readonly float[] kBandwidthForBands = new float[6] { 1.414f, 1.26f, 1.414f, 1.414f, 1.122f, 1.122f };

		private static readonly string[] kKeywords = new string[6] { "EQ_FOUR_BAND", "EQ_FOUR_BAND_VISUAL", "EQ_HEIGHT_BAND", "EQ_TEN_BAND", "EQ_TWENTYSIX_BAND", "EQ_THIRTYONE_BAND" };

		private float[] m_SpectrumData;

		private NativeArray<float> m_Frequencies;

		private float m_Bandwidth;

		private Vector4[] m_Levels;

		private FFTWindow m_FFTWindow;

		private BandType m_BandType;

		private RenderTexture m_VURender;

		private Material m_Equalizer;

		private RenderTargetIdentifier m_VURenderId;

		private const int kTexWidth = 96;

		private const int kTexHeight = 36;

		public Texture equalizerTexture => m_VURender;

		public void Enable(int samplesCount, FFTWindow fftWindow, BandType bandType, float spacing = 10f, float padding = 2f)
		{
			if (m_Frequencies.IsCreated)
			{
				Disable();
			}
			m_FFTWindow = fftWindow;
			m_BandType = bandType;
			m_SpectrumData = new float[samplesCount];
			int num = kMiddleFrequenciesForBands[(int)m_BandType].Length;
			m_Frequencies = new NativeArray<float>(kMiddleFrequenciesForBands[(int)m_BandType], Allocator.Persistent);
			m_Bandwidth = kBandwidthForBands[(int)m_BandType];
			m_Levels = new Vector4[num];
			m_VURender = new RenderTexture(96, 36, 0, GraphicsFormat.R8G8B8A8_UNorm, 0)
			{
				name = "RadioEqualizer",
				hideFlags = HideFlags.HideAndDontSave
			};
			m_VURender.Create();
			m_VURenderId = new RenderTargetIdentifier(m_VURender);
			m_Equalizer = new Material(Shader.Find("Hidden/HDRP/Radio/Equalizer"));
			m_Equalizer.SetFloat("_Padding", padding / 96f);
			m_Equalizer.SetFloat("_Spacing", spacing / 96f);
			m_Equalizer.EnableKeyword(kKeywords[(int)m_BandType]);
			RenderPipelineManager.beginFrameRendering += SpectrumBlit;
		}

		public void Disable()
		{
			RenderPipelineManager.beginFrameRendering -= SpectrumBlit;
			if (m_Equalizer != null)
			{
				UnityEngine.Object.Destroy(m_Equalizer);
			}
			if (m_VURender != null)
			{
				m_VURender.Release();
				UnityEngine.Object.Destroy(m_VURender);
			}
			m_SpectrumData = null;
			if (m_Frequencies.IsCreated)
			{
				m_Frequencies.Dispose();
			}
			m_Levels = null;
		}

		private void SpectrumBlit(ScriptableRenderContext context, Camera[] camera)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get("BlitRadio");
			m_Equalizer.SetVectorArray("_Levels", m_Levels);
			commandBuffer.Blit(null, m_VURenderId, m_Equalizer);
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
			m_VURender.IncrementUpdateCount();
		}

		public void Update(AudioSource source)
		{
			if (source != null && m_Frequencies.IsCreated)
			{
				source.GetSpectrumData(m_SpectrumData, 0, m_FFTWindow);
				IJobExtensions.Schedule(new CreateLevels(ref m_Frequencies, m_Bandwidth, m_SpectrumData, m_Levels, 0.25f, 4f)).Complete();
			}
		}
	}

	public OnRadioEvent Reloaded;

	public OnRadioEvent SettingsChanged;

	public OnRadioEvent ProgramChanged;

	public OnClipChanged ClipChanged;

	private const float kSimulationSecondsPerDay = 4369.067f;

	private const float kSimtimeToRealtime = 0.050567903f;

	private static readonly string kAlertsTag = "type:Alerts";

	private static readonly string kAlertsIntroTag = "type:Alerts Intro";

	private static ILog log = LogManager.GetLogger("Radio");

	private const int kSecondsPerDay = 86400;

	private Dictionary<string, RadioNetwork> m_Networks = new Dictionary<string, RadioNetwork>();

	private Dictionary<string, RuntimeRadioChannel> m_RadioChannels = new Dictionary<string, RuntimeRadioChannel>();

	private RuntimeRadioChannel m_CurrentChannel;

	private bool m_Paused;

	private bool m_Muted;

	private static readonly int kMaxHistoryLength = 5;

	private int m_ReplayIndex;

	private List<ClipInfo> m_PlaylistHistory = new List<ClipInfo>(kMaxHistoryLength);

	private List<ClipInfo> m_Queue = new List<ClipInfo>(2);

	private RadioPlayer m_RadioPlayer;

	private bool m_IsEnabled;

	private string m_LastSaveRadioChannel;

	private bool m_LastSaveRadioAdsState;

	private bool m_IsActive;

	private RuntimeRadioChannel[] m_CachedRadioChannelDescriptors;

	private Dictionary<SegmentType, OnDemandClips> m_OnDemandClips;

	private const string kUniqueNameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

	public RuntimeRadioChannel currentChannel
	{
		get
		{
			return m_CurrentChannel;
		}
		set
		{
			if (m_CurrentChannel != value)
			{
				m_CurrentChannel = value;
				if (currentClip.m_Emergency == Entity.Null)
				{
					FinishCurrentClip();
				}
				SetupOrSkipSegment();
				ClearQueue();
				m_ReplayIndex = 0;
			}
		}
	}

	public bool paused
	{
		get
		{
			return m_Paused;
		}
		set
		{
			m_Paused = value;
			if (currentClip.m_Emergency == Entity.Null)
			{
				if (m_Paused)
				{
					m_RadioPlayer.Pause();
				}
				else
				{
					m_RadioPlayer.Unpause();
				}
			}
		}
	}

	public bool skipAds { get; set; }

	public bool muted
	{
		get
		{
			return m_Muted;
		}
		set
		{
			m_Muted = value;
			m_RadioPlayer.muted = value;
		}
	}

	public ClipInfo currentClip { get; private set; }

	public bool isEnabled => m_IsEnabled;

	public bool isActive
	{
		get
		{
			return m_IsActive;
		}
		set
		{
			if (m_IsActive != value)
			{
				m_IsActive = value;
				SettingsChanged?.Invoke(this);
			}
			if (!value)
			{
				OnDisabled();
			}
		}
	}

	public RadioNetwork[] networkDescriptors => GetSortedUIDescriptor(m_Networks);

	public RuntimeRadioChannel[] radioChannelDescriptors
	{
		get
		{
			if (m_CachedRadioChannelDescriptors == null)
			{
				m_CachedRadioChannelDescriptors = GetSortedUIDescriptor(m_RadioChannels);
			}
			return m_CachedRadioChannelDescriptors;
		}
	}

	public string currentlyPlayingClipName => m_RadioPlayer.currentClipName;

	public double currentlyPlayingDuration => m_RadioPlayer.GetAudioSourceDuration();

	public double currentlyPlayingElapsed => m_RadioPlayer.GetAudioSourceTimeElapsed();

	public double currentlyPlayingRemaining => m_RadioPlayer.GetAudioSourceTimeRemaining();

	public double nextTimeCheck => 0.0;

	public AudioAsset pendingClip
	{
		get
		{
			if (m_Queue.Count <= 0)
			{
				return null;
			}
			return m_Queue[0].m_Asset;
		}
	}

	public bool hasEmergency => currentClip.m_Emergency != Entity.Null;

	public Entity emergency => currentClip.m_Emergency;

	public Entity emergencyTarget => currentClip.m_EmergencyTarget;

	public Texture equalizerTexture => m_RadioPlayer.equalizerTexture;

	public int GetActiveSource()
	{
		return 0;
	}

	public double GetAudioSourceDuration(int i)
	{
		return m_RadioPlayer.GetAudioSourceDuration();
	}

	public double GetAudioSourceTimeElapsed(int i)
	{
		return m_RadioPlayer.GetAudioSourceTimeElapsed();
	}

	public double GetAudioSourceTimeRemaining(int i)
	{
		return m_RadioPlayer.GetAudioSourceTimeRemaining();
	}

	private T[] GetSortedUIDescriptor<T>(Dictionary<string, T> desc) where T : IComparable<T>
	{
		T[] array = desc.Values.ToArray();
		Array.Sort(array);
		return array;
	}

	public RuntimeRadioChannel GetRadioChannel(string name)
	{
		if (m_RadioChannels.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public Radio(AudioMixerGroup radioGroup)
	{
		m_OnDemandClips = new Dictionary<SegmentType, OnDemandClips>();
		m_OnDemandClips[SegmentType.Commercial] = GetCommercialClips;
		m_OnDemandClips[SegmentType.PSA] = GetPSAClips;
		m_OnDemandClips[SegmentType.Playlist] = GetPlaylistClips;
		m_OnDemandClips[SegmentType.Talkshow] = GetTalkShowClips;
		m_OnDemandClips[SegmentType.News] = GetNewsClips;
		m_OnDemandClips[SegmentType.Weather] = GetWeatherClips;
		m_RadioPlayer = new RadioPlayer(radioGroup);
		SettingsChanged = (OnRadioEvent)Delegate.Combine(SettingsChanged, new OnRadioEvent(OnSettingsChanged));
	}

	private void OnSettingsChanged(Radio radio)
	{
		if (radio.isActive)
		{
			if (GameManager.instance != null && GameManager.instance.gameMode == GameMode.Game && Camera.main != null && radio.radioChannelDescriptors.Length != 0)
			{
				radio.Enable(Camera.main.gameObject);
			}
		}
		else
		{
			Disable();
		}
	}

	public void ForceRadioPause(bool pause)
	{
		if (pause)
		{
			m_RadioPlayer.Pause();
		}
		else
		{
			m_RadioPlayer.Unpause();
		}
	}

	public void SetSpectrumSettings(bool enabled, int numSamples, FFTWindow fftWindow, Spectrum.BandType bandType, float spacing, float padding)
	{
		m_RadioPlayer.SetSpectrumSettings(enabled, numSamples, fftWindow, bandType, spacing, padding);
	}

	private bool CheckEntitlement(IContentPrerequisite target)
	{
		if (target.contentPrerequisites != null)
		{
			return target.contentPrerequisites.All(delegate(string x)
			{
				DlcId dlcId = PlatformManager.instance.GetDlcId(x);
				return PlatformManager.instance.IsDlcOwned(dlcId);
			});
		}
		return true;
	}

	private void LoadRadio(bool enable)
	{
		try
		{
			Clear();
			using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
			{
				log.DebugFormat("Loaded radio configuration in {0}ms", t.TotalMilliseconds);
			}))
			{
				AssetDatabase.global.LoadSettings("Radio Network", delegate(RadioNetwork network, SourceMeta meta)
				{
					if (CheckEntitlement(network))
					{
						m_Networks.Add(network.name, network);
					}
				});
				AssetDatabase.global.LoadSettings("Radio Channel", delegate(RadioChannel channel, SourceMeta meta)
				{
					if (CheckEntitlement(channel))
					{
						string text = channel.name;
						while (m_RadioChannels.ContainsKey(text))
						{
							text = text + "_" + MakeUniqueRandomName(text, 4);
						}
						log.InfoFormat("Radio channel id '{0}' added", text);
						m_RadioChannels.Add(text, channel.CreateRuntime(meta.path));
					}
				});
			}
			LogRadio();
			if (enable)
			{
				Enable(Camera.main.gameObject);
			}
		}
		catch (Exception exception)
		{
			log.Error(exception);
		}
	}

	private void Clear()
	{
		m_CachedRadioChannelDescriptors = null;
		m_Networks.Clear();
		m_RadioChannels.Clear();
		currentChannel = null;
		OnDisabled();
	}

	public void Reload(bool enable = true)
	{
		LoadRadio(enable);
		Reloaded?.Invoke(this);
	}

	public void RestoreRadioSettings(string savedChannel, bool savedAds)
	{
		m_LastSaveRadioChannel = savedChannel;
		m_LastSaveRadioAdsState = savedAds;
	}

	public void Enable(GameObject listener)
	{
		if (currentChannel == null)
		{
			if (m_LastSaveRadioChannel != null && m_RadioChannels.TryGetValue(m_LastSaveRadioChannel, out var value))
			{
				currentChannel = value;
				skipAds = m_LastSaveRadioAdsState;
			}
			else
			{
				skipAds = false;
				currentChannel = radioChannelDescriptors[0];
			}
		}
		if (m_IsActive && !m_IsEnabled && listener != null)
		{
			m_RadioPlayer.Create(listener);
			m_RadioPlayer.muted = m_Muted;
			SetSpectrumSettings(SharedSettings.instance.radio.enableSpectrum, SharedSettings.instance.radio.spectrumNumSamples, SharedSettings.instance.radio.fftWindowType, SharedSettings.instance.radio.bandType, SharedSettings.instance.radio.equalizerBarSpacing, SharedSettings.instance.radio.equalizerSidesPadding);
			m_IsEnabled = true;
		}
	}

	public void Disable()
	{
		m_RadioPlayer?.Dispose();
		m_IsEnabled = false;
		OnDisabled();
	}

	private void OnDisabled()
	{
		FinishCurrentClip();
		ClearQueue(clearEmergencies: true);
		m_ReplayIndex = 0;
		if (currentClip.m_LoadTask != null)
		{
			currentClip.m_Asset.Unload();
		}
		currentClip = default(ClipInfo);
		m_PlaylistHistory.Clear();
	}

	public void Update(float normalizedTime)
	{
		if (!isActive || !isEnabled)
		{
			ClearEmergencyQueue();
			return;
		}
		try
		{
			m_RadioPlayer.UpdateSpectrum();
			int timeOfDaySeconds = Mathf.RoundToInt(normalizedTime * 24f * 3600f);
			bool flag = false;
			bool flag2 = false;
			RuntimeRadioChannel[] array = radioChannelDescriptors;
			foreach (RuntimeRadioChannel obj in array)
			{
				bool flag3 = obj.Update(timeOfDaySeconds);
				if (obj == currentChannel)
				{
					flag = flag3;
				}
				flag2 = flag2 || flag3;
			}
			if (flag)
			{
				log.DebugFormat("Program changed callback for on-demand clips initialization");
				SetupOrSkipSegment();
			}
			QueueEmergencyClips();
			ValidateQueue();
			if (m_Queue.Count > 0)
			{
				ClipInfo clipInfo = m_Queue[0];
				if (currentClip.m_Emergency == Entity.Null && clipInfo.m_Emergency != Entity.Null)
				{
					if (clipInfo.m_LoadTask != null && clipInfo.m_LoadTask.IsCompleted)
					{
						m_RadioPlayer.Unpause();
						ClipInfo clip = currentClip;
						clip.m_ResumeAtPosition = m_RadioPlayer.playbackPosition;
						m_RadioPlayer.Play(clipInfo.m_LoadTask.Result);
						currentClip = clipInfo;
						m_Queue.RemoveAt(0);
						QueueClip(clip);
						InvokeClipcallback(currentClip.m_Asset);
					}
				}
				else if (m_RadioPlayer.GetAudioSourceTimeRemaining() <= 0.0 && clipInfo.m_LoadTask != null && clipInfo.m_LoadTask.IsCompleted)
				{
					if (clipInfo.m_SegmentType == SegmentType.Commercial && skipAds)
					{
						clipInfo.m_Asset.Unload();
						m_Queue.RemoveAt(0);
					}
					else
					{
						m_RadioPlayer.Play(clipInfo.m_LoadTask.Result, (clipInfo.m_ResumeAtPosition >= 0) ? clipInfo.m_ResumeAtPosition : 0);
						if (currentClip.m_LoadTask != null)
						{
							currentClip.m_Asset.Unload();
						}
						currentClip = clipInfo;
						if (currentClip.m_SegmentType == SegmentType.Playlist && currentClip.m_ResumeAtPosition < 0 && !currentClip.m_Replaying)
						{
							ClipInfo item = currentClip;
							item.m_LoadTask = null;
							m_PlaylistHistory.Insert(0, item);
							while (m_PlaylistHistory.Count > kMaxHistoryLength)
							{
								m_PlaylistHistory.RemoveAt(m_PlaylistHistory.Count - 1);
							}
						}
						if (!currentClip.m_Replaying)
						{
							m_ReplayIndex = 0;
						}
						m_Queue.RemoveAt(0);
						if (paused && currentClip.m_Emergency == Entity.Null)
						{
							m_RadioPlayer.Pause();
						}
						InvokeClipcallback(currentClip.m_Asset);
					}
				}
			}
			QueueNextClip();
			if (flag2)
			{
				ProgramChanged(this);
			}
		}
		catch (Exception exception)
		{
			log.Fatal(exception);
		}
	}

	private void QueueClip(ClipInfo clip, bool pushToFront = false)
	{
		if (clip.m_Emergency != Entity.Null || clip.m_ResumeAtPosition >= 0 || pushToFront)
		{
			int num = m_Queue.FindIndex((ClipInfo info) => info.m_Emergency == Entity.Null);
			m_Queue.Insert((num < 0) ? m_Queue.Count : num, clip);
		}
		else
		{
			m_Queue.Add(clip);
		}
	}

	private void ValidateQueue()
	{
		for (int i = 0; i < m_Queue.Count; i++)
		{
			while (i < m_Queue.Count && (m_Queue[i].m_LoadTask == null || m_Queue[i].m_LoadTask.IsFaulted || m_Queue[i].m_LoadTask.IsCanceled))
			{
				if (m_Queue[i].m_LoadTask != null)
				{
					m_Queue[i].m_Asset.Unload();
				}
				m_Queue.RemoveAt(i);
			}
		}
	}

	private void ClearQueue(bool clearEmergencies = false)
	{
		for (int i = 0; i < m_Queue.Count; i++)
		{
			if (m_Queue[i].m_LoadTask != null && (clearEmergencies || m_Queue[i].m_Emergency == Entity.Null))
			{
				m_Queue[i].m_Asset.Unload();
			}
		}
		if (clearEmergencies)
		{
			m_Queue.Clear();
			return;
		}
		m_Queue.RemoveAll((ClipInfo clip) => clip.m_Emergency == Entity.Null);
	}

	private void ClearEmergencyQueue()
	{
		JobHandle deps;
		NativeQueue<RadioTag> emergencyQueue = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RadioTagSystem>().GetEmergencyQueue(out deps);
		deps.Complete();
		emergencyQueue.Clear();
	}

	private void QueueEmergencyClips()
	{
		JobHandle deps;
		NativeQueue<RadioTag> emergencyQueue = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RadioTagSystem>().GetEmergencyQueue(out deps);
		deps.Complete();
		while (emergencyQueue.Count > 0)
		{
			RadioTag tag = emergencyQueue.Dequeue();
			if (IsEmergencyClipInQueue(tag))
			{
				continue;
			}
			List<AudioAsset> list = new List<AudioAsset>();
			PrefabBase prefab = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(tag.m_Event);
			foreach (AudioAsset asset in AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => asset.ContainsTag(kAlertsTag))))
			{
				if (asset.GetMetaTag(AudioAsset.Metatag.AlertType) == prefab.name)
				{
					list.Add(asset);
				}
			}
			if (list.Count > 0)
			{
				if (!AlertPlayingOrQueued())
				{
					QueueEmergencyIntroClip(tag.m_Event, tag.m_Target);
				}
				AudioAsset audioAsset = list[new Unity.Mathematics.Random((uint)DateTime.Now.Ticks).NextInt(0, list.Count)];
				QueueClip(new ClipInfo
				{
					m_Asset = audioAsset,
					m_Emergency = tag.m_Event,
					m_EmergencyTarget = tag.m_Target,
					m_SegmentType = SegmentType.Emergency,
					m_LoadTask = audioAsset.LoadAsync(),
					m_ResumeAtPosition = -1
				});
			}
		}
		emergencyQueue.Clear();
	}

	private void QueueEmergencyIntroClip(Entity emergency, Entity emergencyTarget)
	{
		List<AudioAsset> list = new List<AudioAsset>();
		foreach (AudioAsset asset in AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => asset.ContainsTag(kAlertsIntroTag))))
		{
			list.Add(asset);
		}
		if (list.Count > 0)
		{
			AudioAsset audioAsset = list[new Unity.Mathematics.Random((uint)DateTime.Now.Ticks).NextInt(0, list.Count)];
			QueueClip(new ClipInfo
			{
				m_Asset = audioAsset,
				m_Emergency = emergency,
				m_EmergencyTarget = emergencyTarget,
				m_SegmentType = SegmentType.Emergency,
				m_LoadTask = audioAsset.LoadAsync(),
				m_ResumeAtPosition = -1
			});
		}
	}

	private bool IsEmergencyClipInQueue(RadioTag tag)
	{
		if (currentClip.m_Emergency != Entity.Null && currentClip.m_Emergency == tag.m_Event)
		{
			return true;
		}
		for (int i = 0; i < m_Queue.Count; i++)
		{
			if (m_Queue[i].m_Emergency != Entity.Null && m_Queue[i].m_Emergency == tag.m_Event)
			{
				return true;
			}
		}
		return false;
	}

	private bool AlertPlayingOrQueued()
	{
		if (!(currentClip.m_Emergency != Entity.Null))
		{
			if (m_Queue.Count > 0)
			{
				return m_Queue[0].m_Emergency != Entity.Null;
			}
			return false;
		}
		return true;
	}

	private void QueueNextClip()
	{
		if (m_Queue.Count == 0 && currentChannel?.currentProgram?.currentSegment?.currentClip != null)
		{
			QueueClip(new ClipInfo
			{
				m_Asset = currentChannel.currentProgram.currentSegment.currentClip,
				m_SegmentType = currentChannel.currentProgram.currentSegment.type,
				m_Emergency = Entity.Null,
				m_LoadTask = currentChannel.currentProgram.currentSegment.currentClip.LoadAsync(),
				m_ResumeAtPosition = -1
			});
			SetupNextClip();
		}
	}

	private void GetPSAClips(RuntimeSegment segment)
	{
		if (m_Networks.TryGetValue(currentChannel.network, out var value) && !value.allowAds)
		{
			List<AudioAsset> eventClips = GetEventClips(segment, AudioAsset.Metatag.PSAType);
			segment.clips = eventClips;
			Log(segment);
		}
	}

	private void GetNewsClips(RuntimeSegment segment)
	{
		List<AudioAsset> eventClips = GetEventClips(segment, AudioAsset.Metatag.NewsType);
		segment.clips = eventClips;
		Log(segment);
	}

	private void GetWeatherClips(RuntimeSegment segment)
	{
		List<AudioAsset> eventClips = GetEventClips(segment, AudioAsset.Metatag.WeatherType, newestFirst: true, flush: true);
		segment.clips = eventClips;
		Log(segment);
	}

	private List<AudioAsset> GetEventClips(RuntimeSegment segment, AudioAsset.Metatag metatag, bool newestFirst = false, bool flush = false)
	{
		RadioTagSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RadioTagSystem>();
		PrefabSystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
		List<AudioAsset> list = new List<AudioAsset>(segment.clipsCap);
		List<AudioAsset> list2 = new List<AudioAsset>();
		RadioTag radioTag;
		while (list.Count < segment.clipsCap && existingSystemManaged.TryPopEvent(segment.type, newestFirst, out radioTag))
		{
			list2.Clear();
			foreach (AudioAsset asset in AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => segment.tags.All(asset.ContainsTag))))
			{
				if (asset.GetMetaTag(metatag) == orCreateSystemManaged.GetPrefab<PrefabBase>(radioTag.m_Event).name)
				{
					list2.Add(asset);
				}
			}
			if (list2.Count > 0)
			{
				list.Add(list2[new Unity.Mathematics.Random((uint)DateTime.Now.Ticks).NextInt(0, list2.Count)]);
			}
		}
		if (flush)
		{
			existingSystemManaged.FlushEvents(segment.type);
		}
		return list;
	}

	private void GetCommercialClips(RuntimeSegment segment)
	{
		if (!m_Networks.TryGetValue(currentChannel.network, out var value) || !value.allowAds)
		{
			return;
		}
		WeightedRandom<AudioAsset> weightedRandom = new WeightedRandom<AudioAsset>();
		Dictionary<string, List<AudioAsset>> dictionary = new Dictionary<string, List<AudioAsset>>();
		foreach (AudioAsset asset in AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => segment.tags.All(asset.ContainsTag))))
		{
			string metaTag = asset.GetMetaTag(AudioAsset.Metatag.Brand);
			if (metaTag != null)
			{
				if (!dictionary.TryGetValue(metaTag, out var value2))
				{
					value2 = new List<AudioAsset>();
					dictionary.Add(metaTag, value2);
				}
				value2.Add(asset);
			}
			else
			{
				log.ErrorFormat("Asset {0} ({1}) does not contain a brand metatag (for Commercial segment)", asset.id, asset.GetMetaTag(AudioAsset.Metatag.Title) ?? "<No title>");
			}
		}
		LogMap(dictionary);
		JobHandle dependency;
		NativeList<BrandPopularitySystem.BrandPopularity> brandPopularity = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BrandPopularitySystem>().ReadBrandPopularity(out dependency);
		dependency.Complete();
		LogBrandPopularity(brandPopularity);
		for (int num = 0; num < brandPopularity.Length; num++)
		{
			if (World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>().TryGetPrefab<BrandPrefab>(brandPopularity[num].m_BrandPrefab, out var prefab) && dictionary.TryGetValue(prefab.name, out var value3))
			{
				weightedRandom.AddRange(value3, brandPopularity[num].m_Popularity);
			}
		}
		List<AudioAsset> list = new List<AudioAsset>();
		for (int num2 = 0; num2 < segment.clipsCap; num2++)
		{
			AudioAsset audioAsset = weightedRandom.NextAndRemove();
			if (audioAsset != null)
			{
				list.Add(audioAsset);
			}
		}
		segment.clips = list;
		Log(segment);
	}

	private void GetPlaylistClips(RuntimeSegment segment)
	{
		segment.clips = GetSegmentAudioClip(segment.clipsCap, segment.tags, segment.type);
	}

	private void GetTalkShowClips(RuntimeSegment segment)
	{
		segment.clips = GetSegmentAudioClip(segment.clipsCap, segment.tags, segment.type);
	}

	private AudioAsset[] GetSegmentAudioClip(int clipsCap, string[] requiredTags, SegmentType segmentType)
	{
		IEnumerable<AudioAsset> assets = AssetDatabase.global.GetAssets(SearchFilter<AudioAsset>.ByCondition((AudioAsset asset) => requiredTags.All(asset.ContainsTag)));
		List<AudioAsset> list = new List<AudioAsset>();
		list.AddRange(assets);
		System.Random rnd = new System.Random();
		List<int> list2 = (from x in Enumerable.Range(0, list.Count)
			orderby rnd.Next()
			select x).Take(clipsCap).ToList();
		AudioAsset[] array = new AudioAsset[clipsCap];
		for (int num = 0; num < array.Length; num++)
		{
			array[num] = list[list2[num]];
		}
		return array;
	}

	private void LogMap(Dictionary<string, List<AudioAsset>> map)
	{
		if (!log.isDebugEnabled)
		{
			return;
		}
		string text = "Audio asset map:\n";
		foreach (KeyValuePair<string, List<AudioAsset>> item in map)
		{
			text = text + item.Key + "\n";
			foreach (AudioAsset item2 in item.Value)
			{
				text += $"  {item2.GetMetaTag(AudioAsset.Metatag.Title)} ({item2.id})\n";
			}
		}
		log.Verbose(text);
	}

	private void LogBrandPopularity(NativeList<BrandPopularitySystem.BrandPopularity> brandPopularity)
	{
		if (log.isDebugEnabled)
		{
			string text = "Brands popularity:\n";
			PrefabSystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
			for (int i = 0; i < brandPopularity.Length; i++)
			{
				string prefabName = orCreateSystemManaged.GetPrefabName(brandPopularity[i].m_BrandPrefab);
				text += $"{prefabName} - {brandPopularity[i].m_Popularity}\n";
			}
			log.Verbose(text);
		}
	}

	private bool SetupNextClip()
	{
		if (currentChannel?.currentProgram?.currentSegment == null)
		{
			return false;
		}
		if (!currentChannel.currentProgram.currentSegment.GoToNextClip())
		{
			currentChannel.currentProgram.GoToNextSegment();
			if (!SetupOrSkipSegment())
			{
				return false;
			}
		}
		return true;
	}

	private bool SetupOrSkipSegment()
	{
		if (currentChannel?.currentProgram == null)
		{
			return false;
		}
		RuntimeProgram currentProgram = currentChannel.currentProgram;
		while (true)
		{
			RuntimeSegment currentSegment = currentProgram.currentSegment;
			if (currentSegment == null)
			{
				return false;
			}
			if (m_OnDemandClips.TryGetValue(currentSegment.type, out var value))
			{
				value(currentSegment);
			}
			if (currentSegment.clips.Count != 0)
			{
				break;
			}
			if (!currentProgram.GoToNextSegment())
			{
				return false;
			}
		}
		return true;
	}

	private void InvokeClipcallback(AudioAsset currentAsset)
	{
		try
		{
			ClipChanged?.Invoke(this, currentAsset);
		}
		catch (Exception exception)
		{
			log.Critical(exception);
		}
	}

	public void NextSong()
	{
		if (m_ReplayIndex > 0)
		{
			m_ReplayIndex--;
			ClipInfo clip = m_PlaylistHistory[m_ReplayIndex];
			clip.m_Replaying = true;
			clip.m_LoadTask = clip.m_Asset.LoadAsync();
			QueueClip(clip, pushToFront: true);
		}
		FinishCurrentClip();
	}

	public void PreviousSong()
	{
		if (m_RadioPlayer.GetAudioSourceTimeElapsed() > 2.0 || m_ReplayIndex >= m_PlaylistHistory.Count - 1)
		{
			m_RadioPlayer.Rewind();
			return;
		}
		m_ReplayIndex++;
		ClipInfo clip = m_PlaylistHistory[m_ReplayIndex];
		clip.m_Replaying = true;
		clip.m_LoadTask = clip.m_Asset.LoadAsync();
		QueueClip(clip, pushToFront: true);
		FinishCurrentClip();
	}

	private void FinishCurrentClip()
	{
		m_RadioPlayer.Play(null);
	}

	private static void SupportValueTypesForAOT()
	{
		JSON.SupportTypeForAOT<SegmentType>();
	}

	private static string MakeUniqueName(string name, int length)
	{
		char[] array = new char[length];
		for (int i = 0; i < name.Length; i++)
		{
			array[i % (length - 1)] += name[i];
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[array[j] % "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length];
		}
		return new string(array);
	}

	private static string MakeUniqueRandomName(string name, int length)
	{
		char[] array = new char[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[UnityEngine.Random.Range(0, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length) % "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length];
		}
		return new string(array);
	}

	private void Log(RadioNetwork network)
	{
		log.Debug("name: " + network.name);
		using (log.indent.scoped)
		{
			log.Verbose("description: " + network.description);
			log.Verbose("icon: " + network.icon);
			log.Verbose($"uiPriority: {network.uiPriority}");
			log.Verbose($"allowAds: {network.allowAds}");
		}
	}

	private void Log(RuntimeRadioChannel channel)
	{
		log.Debug("name: " + channel.name);
		using (log.indent.scoped)
		{
			log.Verbose("description: " + channel.description);
			log.Verbose("icon: " + channel.icon);
			log.Verbose($"uiPriority: {channel.uiPriority}");
			log.Verbose("network: " + channel.network);
			log.DebugFormat("Programs ({0})", channel.schedule.Length);
			using (log.indent.scoped)
			{
				RuntimeProgram[] schedule = channel.schedule;
				foreach (RuntimeProgram program in schedule)
				{
					Log(program);
				}
			}
		}
	}

	private void Log(AudioAsset clip)
	{
		if (clip == null)
		{
			log.Debug("id: <missing>");
		}
		else
		{
			log.Debug(string.Format("id: {0} tags: {1} duration: {2}", clip.id, string.Join(", ", clip.tags), FormatUtils.FormatTimeDebug(clip.durationMs)));
		}
	}

	private void Log(RuntimeProgram program)
	{
		log.Debug("name: " + program.name + " [" + FormatUtils.FormatTimeDebug(program.startTime) + " -> " + FormatUtils.FormatTimeDebug(program.endTime) + "]");
		using (log.indent.scoped)
		{
			log.Verbose("description: " + program.description);
			log.Verbose($"estimatedStart: {FormatUtils.FormatTimeDebug(program.startTime)} ({program.startTime}s)");
			log.Verbose($"estimatedEnd: {FormatUtils.FormatTimeDebug(program.endTime)} ({program.endTime}s)");
			log.Verbose($"loopProgram: {program.loopProgram}");
			log.Verbose($"estimatedDuration: {FormatUtils.FormatTimeDebug(program.duration)} ({program.duration}s) (realtime at x1: {FormatUtils.FormatTimeDebug((int)((float)program.duration * 0.050567903f))})");
			log.DebugFormat("Segments ({0})", program.segments.Count);
			using (log.indent.scoped)
			{
				foreach (RuntimeSegment segment in program.segments)
				{
					Log(segment);
				}
			}
		}
	}

	private void Log(RuntimeSegment segment)
	{
		log.Debug($"type: {segment.type}");
		using (log.indent.scoped)
		{
			if (segment.tags != null)
			{
				log.Debug("tags: " + string.Join(", ", segment.tags));
			}
			if (segment.clips == null)
			{
				return;
			}
			log.Verbose($"duration total: {segment.durationMs}ms ({FormatUtils.FormatTimeDebug(segment.durationMs)})");
			log.DebugFormat("Clips ({0})", segment.clips.Count);
			using (log.indent.scoped)
			{
				foreach (AudioAsset clip in segment.clips)
				{
					Log(clip);
				}
			}
			log.DebugFormat("Clips cap: {0}", segment.clipsCap);
		}
	}

	private void LogRadio()
	{
		if (!log.isDebugEnabled)
		{
			return;
		}
		log.DebugFormat("Networks ({0})", m_Networks.Count);
		using (log.indent.scoped)
		{
			foreach (RadioNetwork value in m_Networks.Values)
			{
				Log(value);
			}
		}
		log.DebugFormat("Channels ({0})", m_RadioChannels.Count);
		using (log.indent.scoped)
		{
			foreach (RuntimeRadioChannel value2 in m_RadioChannels.Values)
			{
				Log(value2);
			}
		}
	}
}
