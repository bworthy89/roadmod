using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Colossal.Logging;
using UnityEngine;

namespace Game.Audio.Radio;

public class BpmAnalyzer
{
	public struct BpmMatchData
	{
		public int bpm;

		public float match;
	}

	private static ILog log = LogManager.GetLogger("Radio");

	private const int MIN_BPM = 60;

	private const int MAX_BPM = 400;

	private const int BASE_FREQUENCY = 44100;

	private const int BASE_CHANNELS = 2;

	private const int BASE_SPLIT_SAMPLE_SIZE = 2205;

	private static BpmMatchData[] bpmMatchDatas = new BpmMatchData[341];

	public static int AnalyzeBpm(AudioClip clip)
	{
		for (int i = 0; i < bpmMatchDatas.Length; i++)
		{
			bpmMatchDatas[i].match = 0f;
		}
		if (clip == null)
		{
			return -1;
		}
		log.InfoFormat("AnalyzeBpm audioClipName: {0}", clip.name);
		int frequency = clip.frequency;
		log.InfoFormat("Frequency: {0}", frequency);
		int channels = clip.channels;
		log.InfoFormat("Channels: {0}", channels);
		int splitFrameSize = Mathf.FloorToInt((float)frequency / 44100f * ((float)channels / 2f) * 2205f);
		float[] array = new float[clip.samples * channels];
		clip.GetData(array, 0);
		int num = SearchBpm(CreateVolumeArray(array, frequency, channels, splitFrameSize), frequency, splitFrameSize);
		log.InfoFormat("Matched BPM: {0}", num);
		StringBuilder stringBuilder = new StringBuilder("BPM Match Data List\n");
		for (int j = 0; j < bpmMatchDatas.Length; j++)
		{
			stringBuilder.Append("bpm : " + bpmMatchDatas[j].bpm + ", match : " + Mathf.FloorToInt(bpmMatchDatas[j].match * 10000f) + "\n");
		}
		log.Info(stringBuilder.ToString());
		return num;
	}

	private static float[] CreateVolumeArray(float[] allSamples, int frequency, int channels, int splitFrameSize)
	{
		float[] array = new float[Mathf.CeilToInt((float)allSamples.Length / (float)splitFrameSize)];
		int num = 0;
		for (int i = 0; i < allSamples.Length; i += splitFrameSize)
		{
			float num2 = 0f;
			for (int j = i; j < i + splitFrameSize && allSamples.Length > j; j++)
			{
				float num3 = Mathf.Abs(allSamples[j]);
				if (!(num3 > 1f))
				{
					num2 += num3 * num3;
				}
			}
			array[num] = Mathf.Sqrt(num2 / (float)splitFrameSize);
			num++;
		}
		float num4 = array.Max();
		for (int k = 0; k < array.Length; k++)
		{
			array[k] /= num4;
		}
		return array;
	}

	private static int SearchBpm(float[] volumeArr, int frequency, int splitFrameSize)
	{
		List<float> list = new List<float>();
		for (int i = 1; i < volumeArr.Length; i++)
		{
			list.Add(Mathf.Max(volumeArr[i] - volumeArr[i - 1], 0f));
		}
		int num = 0;
		float num2 = (float)frequency / (float)splitFrameSize;
		for (int j = 60; j <= 400; j++)
		{
			float num3 = 0f;
			float num4 = 0f;
			float num5 = (float)j / 60f;
			if (list.Count > 0)
			{
				for (int k = 0; k < list.Count; k++)
				{
					num3 += list[k] * Mathf.Cos((float)k * 2f * MathF.PI * num5 / num2);
					num4 += list[k] * Mathf.Sin((float)k * 2f * MathF.PI * num5 / num2);
				}
				num3 *= 1f / (float)list.Count;
				num4 *= 1f / (float)list.Count;
			}
			float match = Mathf.Sqrt(num3 * num3 + num4 * num4);
			bpmMatchDatas[num].bpm = j;
			bpmMatchDatas[num].match = match;
			num++;
		}
		int num6 = Array.FindIndex(bpmMatchDatas, (BpmMatchData x) => x.match == bpmMatchDatas.Max((BpmMatchData y) => y.match));
		return bpmMatchDatas[num6].bpm;
	}
}
