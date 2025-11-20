using System;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using UnityEngine;

public class UberZOrdererTest : MonoBehaviour
{
	private UberZOrderer m_UberZOrderer;

	private void TestSmall()
	{
		int num = 16;
		int num2 = num * num;
		num2 *= 4;
		num2 *= 4;
		UberZOrderer uberZOrderer = new UberZOrderer(512, 512 * num, num2, 1);
		if (uberZOrderer.ReserveRect(1024, 4096) != 0)
		{
			throw new Exception();
		}
		int num3 = uberZOrderer.ReserveRect(8192, 8192);
		int num4 = uberZOrderer.ReserveRect(8192, 8192);
		int num5 = uberZOrderer.ReserveRect(8192, 8192);
		if (num3 != 1024)
		{
			throw new Exception();
		}
		if (num4 != 1280)
		{
			throw new Exception();
		}
		if (num5 != 1536)
		{
			throw new Exception();
		}
		int num6 = uberZOrderer.ReserveRect(512, 512);
		int num7 = uberZOrderer.ReserveRect(512, 512);
		if (num6 != 2048)
		{
			throw new Exception();
		}
		if (num7 != 2049)
		{
			throw new Exception();
		}
		int num8 = uberZOrderer.ReserveRect(8192, 8192);
		int num9 = uberZOrderer.ReserveRect(8192, 8192);
		if (num8 != 1792)
		{
			throw new Exception();
		}
		if (num9 != 3072)
		{
			throw new Exception();
		}
		if (uberZOrderer.ReserveRect(512, 1024) != -1)
		{
			throw new Exception();
		}
	}

	private void OnEnable()
	{
		m_UberZOrderer = new UberZOrderer(512, 8192, 4194304, 3);
		int num = m_UberZOrderer.ReserveRect(1024, 4096);
		int num2 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num3 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num4 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num5 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num6 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num7 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num8 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num9 = m_UberZOrderer.ReserveRect(1024, 4096);
		int num10 = m_UberZOrderer.ReserveRect(1024, 4096);
		if (num != 0)
		{
			throw new Exception();
		}
		if (num2 != 4)
		{
			throw new Exception();
		}
		if (num3 != 16)
		{
			throw new Exception();
		}
		if (num4 != 20)
		{
			throw new Exception();
		}
		if (num5 != 64)
		{
			throw new Exception();
		}
		if (num6 != 68)
		{
			throw new Exception();
		}
		if (num7 != 80)
		{
			throw new Exception();
		}
		if (num8 != 84)
		{
			throw new Exception();
		}
		if (num9 != 128)
		{
			throw new Exception();
		}
		if (num10 != 132)
		{
			throw new Exception();
		}
		int index = m_UberZOrderer.GetIndex(0, 0);
		int index2 = m_UberZOrderer.GetIndex(0, 512);
		int index3 = m_UberZOrderer.GetIndex(0, 1024);
		int index4 = m_UberZOrderer.GetIndex(0, 1536);
		int index5 = m_UberZOrderer.GetIndex(512, 0);
		int index6 = m_UberZOrderer.GetIndex(512, 512);
		int index7 = m_UberZOrderer.GetIndex(512, 1024);
		int index8 = m_UberZOrderer.GetIndex(512, 1536);
		int index9 = m_UberZOrderer.GetIndex(1024, 0);
		if (index != num)
		{
			throw new Exception();
		}
		if (index2 != num)
		{
			throw new Exception();
		}
		if (index3 != num)
		{
			throw new Exception();
		}
		if (index4 != num)
		{
			throw new Exception();
		}
		if (index5 != num)
		{
			throw new Exception();
		}
		if (index6 != num)
		{
			throw new Exception();
		}
		if (index7 != num)
		{
			throw new Exception();
		}
		if (index8 != num)
		{
			throw new Exception();
		}
		if (index9 != num2)
		{
			throw new Exception();
		}
		int num11 = m_UberZOrderer.ReserveRect(512, 2048);
		int num12 = m_UberZOrderer.ReserveRect(512, 2048);
		int num13 = m_UberZOrderer.ReserveRect(512, 2048);
		int num14 = m_UberZOrderer.ReserveRect(512, 2048);
		int num15 = m_UberZOrderer.ReserveRect(512, 2048);
		int num16 = m_UberZOrderer.ReserveRect(512, 2048);
		int num17 = m_UberZOrderer.ReserveRect(512, 2048);
		int num18 = m_UberZOrderer.ReserveRect(512, 2048);
		int num19 = m_UberZOrderer.ReserveRect(512, 2048);
		int num20 = m_UberZOrderer.ReserveRect(512, 2048);
		if (num12 != num11 + 1)
		{
			throw new Exception();
		}
		if (num13 != num11 + 4)
		{
			throw new Exception();
		}
		if (num14 != num11 + 5)
		{
			throw new Exception();
		}
		if (num15 != num11 + 16)
		{
			throw new Exception();
		}
		if (num16 != num11 + 17)
		{
			throw new Exception();
		}
		if (num17 != num11 + 20)
		{
			throw new Exception();
		}
		if (num18 != num11 + 21)
		{
			throw new Exception();
		}
		if (num19 != num11 + 32)
		{
			throw new Exception();
		}
		if (num20 != num11 + 33)
		{
			throw new Exception();
		}
		int num21 = 65536;
		int index10 = m_UberZOrderer.GetIndex(num21, 0);
		int index11 = m_UberZOrderer.GetIndex(num21, 512);
		int index12 = m_UberZOrderer.GetIndex(num21, 1024);
		int index13 = m_UberZOrderer.GetIndex(num21, 1536);
		int index14 = m_UberZOrderer.GetIndex(num21 + 512, 0);
		int index15 = m_UberZOrderer.GetIndex(num21 + 512, 512);
		int index16 = m_UberZOrderer.GetIndex(num21 + 512, 1024);
		int index17 = m_UberZOrderer.GetIndex(num21 + 512, 1536);
		int index18 = m_UberZOrderer.GetIndex(num21 + 1024, 0);
		if (index10 != num11)
		{
			throw new Exception();
		}
		if (index11 != num11)
		{
			throw new Exception();
		}
		if (index12 != num11)
		{
			throw new Exception();
		}
		if (index13 != num11)
		{
			throw new Exception();
		}
		if (index14 != num12)
		{
			throw new Exception();
		}
		if (index15 != num12)
		{
			throw new Exception();
		}
		if (index16 != num12)
		{
			throw new Exception();
		}
		if (index17 != num12)
		{
			throw new Exception();
		}
		if (index18 != num13)
		{
			throw new Exception();
		}
		Debug.Log("All UberZOrdererTests passed");
		Debug.Log(m_UberZOrderer.ReserveRect(2048, 512));
		Debug.Log(m_UberZOrderer.ReserveRect(2048, 512));
		Debug.Log(m_UberZOrderer.ReserveRect(2048, 512));
		Debug.Log(m_UberZOrderer.ReserveRect(4096, 4096));
		Debug.Log(m_UberZOrderer.ReserveRect(4096, 4096));
		Debug.Log(m_UberZOrderer.ReserveRect(512, 512));
		Debug.Log(m_UberZOrderer.ReserveRect(512, 512));
	}

	private void Update()
	{
	}
}
