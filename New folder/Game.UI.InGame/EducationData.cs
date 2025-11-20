using Colossal.UI.Binding;

namespace Game.UI.InGame;

public readonly struct EducationData : IJsonWritable
{
	public int uneducated { get; }

	public int poorlyEducated { get; }

	public int educated { get; }

	public int wellEducated { get; }

	public int highlyEducated { get; }

	public int total { get; }

	public EducationData(int uneducated, int poorlyEducated, int educated, int wellEducated, int highlyEducated)
	{
		this.uneducated = uneducated;
		this.poorlyEducated = poorlyEducated;
		this.educated = educated;
		this.wellEducated = wellEducated;
		this.highlyEducated = highlyEducated;
		total = uneducated + poorlyEducated + educated + wellEducated + highlyEducated;
	}

	public static EducationData operator +(EducationData left, EducationData right)
	{
		return new EducationData(left.uneducated + right.uneducated, left.poorlyEducated + right.poorlyEducated, left.educated + right.educated, left.wellEducated + right.wellEducated, left.highlyEducated + right.highlyEducated);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("selectedInfo.ChartData");
		writer.PropertyName("values");
		writer.ArrayBegin(5u);
		writer.Write(uneducated);
		writer.Write(poorlyEducated);
		writer.Write(educated);
		writer.Write(wellEducated);
		writer.Write(highlyEducated);
		writer.ArrayEnd();
		writer.PropertyName("total");
		writer.Write(total);
		writer.TypeEnd();
	}
}
