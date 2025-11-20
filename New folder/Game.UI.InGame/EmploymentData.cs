using Colossal.UI.Binding;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Entities;

namespace Game.UI.InGame;

public readonly struct EmploymentData : IJsonWritable
{
	public int uneducated { get; }

	public int poorlyEducated { get; }

	public int educated { get; }

	public int wellEducated { get; }

	public int highlyEducated { get; }

	public int openPositions { get; }

	public int total { get; }

	public EmploymentData(int uneducated, int poorlyEducated, int educated, int wellEducated, int highlyEducated, int openPositions)
	{
		this.uneducated = uneducated;
		this.poorlyEducated = poorlyEducated;
		this.educated = educated;
		this.wellEducated = wellEducated;
		this.highlyEducated = highlyEducated;
		this.openPositions = openPositions;
		total = uneducated + poorlyEducated + educated + wellEducated + highlyEducated + openPositions;
	}

	public static EmploymentData operator +(EmploymentData left, EmploymentData right)
	{
		return new EmploymentData(left.uneducated + right.uneducated, left.poorlyEducated + right.poorlyEducated, left.educated + right.educated, left.wellEducated + right.wellEducated, left.highlyEducated + right.highlyEducated, left.openPositions + right.openPositions);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("selectedInfo.ChartData");
		writer.PropertyName("values");
		writer.ArrayBegin(6u);
		writer.Write(uneducated);
		writer.Write(poorlyEducated);
		writer.Write(educated);
		writer.Write(wellEducated);
		writer.Write(highlyEducated);
		writer.Write(openPositions);
		writer.ArrayEnd();
		writer.PropertyName("total");
		writer.Write(total);
		writer.TypeEnd();
	}

	public static EmploymentData GetWorkplacesData(int maxWorkers, int buildingLevel, WorkplaceComplexity complexity)
	{
		Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(maxWorkers, complexity, buildingLevel);
		return new EmploymentData(workplaces.m_Uneducated, workplaces.m_PoorlyEducated, workplaces.m_Educated, workplaces.m_WellEducated, workplaces.m_HighlyEducated, 0);
	}

	public static EmploymentData GetEmployeesData(DynamicBuffer<Employee> employees, int openPositions)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < employees.Length; i++)
		{
			switch (employees[i].m_Level)
			{
			case 0:
				num++;
				break;
			case 1:
				num2++;
				break;
			case 2:
				num3++;
				break;
			case 3:
				num4++;
				break;
			case 4:
				num5++;
				break;
			}
		}
		return new EmploymentData(num, num2, num3, num4, num5, openPositions);
	}
}
