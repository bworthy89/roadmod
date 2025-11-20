using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct ZoneAmbiences : IStrideSerializable, ISerializable
{
	public float m_ResidentialLow;

	public float m_CommercialLow;

	public float m_Industrial;

	public float m_Agriculture;

	public float m_Forestry;

	public float m_Oil;

	public float m_Ore;

	public float m_OfficeLow;

	public float m_OfficeHigh;

	public float m_ResidentialMedium;

	public float m_ResidentialHigh;

	public float m_ResidentialMixed;

	public float m_CommercialHigh;

	public float m_ResidentialLowRent;

	public float m_Forest;

	public float m_WaterfrontLow;

	public float m_AquacultureLand;

	public float m_SeagullAmbience;

	public float m_WaterfrontLowCommercial;

	public float m_OldTown;

	public float GetAmbience(GroupAmbienceType type)
	{
		return type switch
		{
			GroupAmbienceType.ResidentialLow => m_ResidentialLow, 
			GroupAmbienceType.CommercialLow => m_CommercialLow, 
			GroupAmbienceType.Industrial => m_Industrial, 
			GroupAmbienceType.Agriculture => m_Agriculture, 
			GroupAmbienceType.Forestry => m_Forestry, 
			GroupAmbienceType.Oil => m_Oil, 
			GroupAmbienceType.Ore => m_Ore, 
			GroupAmbienceType.OfficeLow => m_OfficeLow, 
			GroupAmbienceType.OfficeHigh => m_OfficeHigh, 
			GroupAmbienceType.ResidentialMedium => m_ResidentialMedium, 
			GroupAmbienceType.ResidentialHigh => m_ResidentialHigh, 
			GroupAmbienceType.ResidentialMixed => m_ResidentialMixed, 
			GroupAmbienceType.CommercialHigh => m_CommercialHigh, 
			GroupAmbienceType.ResidentialLowRent => m_ResidentialLowRent, 
			GroupAmbienceType.Forest => m_Forest, 
			GroupAmbienceType.WaterfrontLow => m_WaterfrontLow, 
			GroupAmbienceType.AquacultureLand => m_AquacultureLand, 
			GroupAmbienceType.SeagullAmbience => m_SeagullAmbience, 
			GroupAmbienceType.WaterfrontLowCommercial => m_WaterfrontLowCommercial, 
			GroupAmbienceType.OldTown => m_OldTown, 
			_ => 0f, 
		};
	}

	public void AddAmbience(GroupAmbienceType type, float value)
	{
		switch (type)
		{
		case GroupAmbienceType.ResidentialLow:
			m_ResidentialLow += value;
			break;
		case GroupAmbienceType.CommercialLow:
			m_CommercialLow += value;
			break;
		case GroupAmbienceType.Industrial:
			m_Industrial += value;
			break;
		case GroupAmbienceType.Agriculture:
			m_Agriculture += value;
			break;
		case GroupAmbienceType.Forestry:
			m_Forestry += value;
			break;
		case GroupAmbienceType.Oil:
			m_Oil += value;
			break;
		case GroupAmbienceType.Ore:
			m_Ore += value;
			break;
		case GroupAmbienceType.OfficeLow:
			m_OfficeLow += value;
			break;
		case GroupAmbienceType.OfficeHigh:
			m_OfficeHigh += value;
			break;
		case GroupAmbienceType.ResidentialMedium:
			m_ResidentialMedium += value;
			break;
		case GroupAmbienceType.ResidentialHigh:
			m_ResidentialHigh += value;
			break;
		case GroupAmbienceType.ResidentialMixed:
			m_ResidentialMixed += value;
			break;
		case GroupAmbienceType.CommercialHigh:
			m_CommercialHigh += value;
			break;
		case GroupAmbienceType.ResidentialLowRent:
			m_ResidentialLowRent += value;
			break;
		case GroupAmbienceType.Forest:
			m_Forest += value;
			break;
		case GroupAmbienceType.WaterfrontLow:
			m_WaterfrontLow += value;
			break;
		case GroupAmbienceType.AquacultureLand:
			m_AquacultureLand += value;
			break;
		case GroupAmbienceType.SeagullAmbience:
			m_SeagullAmbience += value;
			break;
		case GroupAmbienceType.WaterfrontLowCommercial:
			m_WaterfrontLowCommercial += value;
			break;
		case GroupAmbienceType.OldTown:
			m_OldTown += value;
			break;
		case GroupAmbienceType.Traffic:
		case GroupAmbienceType.Rain:
		case GroupAmbienceType.NightForest:
			break;
		}
	}

	public static ZoneAmbiences operator +(ZoneAmbiences a, ZoneAmbiences b)
	{
		return new ZoneAmbiences
		{
			m_ResidentialLow = a.m_ResidentialLow + b.m_ResidentialLow,
			m_CommercialLow = a.m_CommercialLow + b.m_CommercialLow,
			m_Industrial = a.m_Industrial + b.m_Industrial,
			m_Agriculture = a.m_Agriculture + b.m_Agriculture,
			m_Forestry = a.m_Forestry + b.m_Forestry,
			m_Oil = a.m_Oil + b.m_Oil,
			m_Ore = a.m_Ore + b.m_Ore,
			m_OfficeLow = a.m_OfficeLow + b.m_OfficeLow,
			m_OfficeHigh = a.m_OfficeHigh + b.m_OfficeHigh,
			m_ResidentialMedium = a.m_ResidentialMedium + b.m_ResidentialMedium,
			m_ResidentialHigh = a.m_ResidentialHigh + b.m_ResidentialHigh,
			m_ResidentialMixed = a.m_ResidentialMixed + b.m_ResidentialMixed,
			m_CommercialHigh = a.m_CommercialHigh + b.m_CommercialHigh,
			m_ResidentialLowRent = a.m_ResidentialLowRent + b.m_ResidentialLowRent,
			m_Forest = a.m_Forest + b.m_Forest,
			m_WaterfrontLow = a.m_WaterfrontLow + b.m_WaterfrontLow,
			m_AquacultureLand = a.m_AquacultureLand + b.m_AquacultureLand,
			m_SeagullAmbience = a.m_SeagullAmbience + b.m_SeagullAmbience,
			m_WaterfrontLowCommercial = a.m_WaterfrontLowCommercial + b.m_WaterfrontLowCommercial,
			m_OldTown = a.m_OldTown + b.m_OldTown
		};
	}

	public static ZoneAmbiences operator /(ZoneAmbiences a, float b)
	{
		return new ZoneAmbiences
		{
			m_ResidentialLow = a.m_ResidentialLow / b,
			m_CommercialLow = a.m_CommercialLow / b,
			m_Industrial = a.m_Industrial / b,
			m_Agriculture = a.m_Agriculture / b,
			m_Forestry = a.m_Forestry / b,
			m_Oil = a.m_Oil / b,
			m_Ore = a.m_Ore / b,
			m_OfficeLow = a.m_OfficeLow / b,
			m_OfficeHigh = a.m_OfficeHigh / b,
			m_ResidentialMedium = a.m_ResidentialMedium / b,
			m_ResidentialHigh = a.m_ResidentialHigh / b,
			m_ResidentialMixed = a.m_ResidentialMixed / b,
			m_CommercialHigh = a.m_CommercialHigh / b,
			m_ResidentialLowRent = a.m_ResidentialLowRent / b,
			m_Forest = a.m_Forest / b,
			m_WaterfrontLow = a.m_WaterfrontLow / b,
			m_AquacultureLand = a.m_AquacultureLand / b,
			m_SeagullAmbience = a.m_SeagullAmbience / b,
			m_WaterfrontLowCommercial = a.m_WaterfrontLowCommercial / b,
			m_OldTown = a.m_OldTown / b
		};
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float residentialLow = m_ResidentialLow;
		writer.Write(residentialLow);
		float commercialHigh = m_CommercialHigh;
		writer.Write(commercialHigh);
		float industrial = m_Industrial;
		writer.Write(industrial);
		float agriculture = m_Agriculture;
		writer.Write(agriculture);
		float forestry = m_Forestry;
		writer.Write(forestry);
		float oil = m_Oil;
		writer.Write(oil);
		float ore = m_Ore;
		writer.Write(ore);
		float officeLow = m_OfficeLow;
		writer.Write(officeLow);
		float officeHigh = m_OfficeHigh;
		writer.Write(officeHigh);
		float residentialMedium = m_ResidentialMedium;
		writer.Write(residentialMedium);
		float residentialHigh = m_ResidentialHigh;
		writer.Write(residentialHigh);
		float residentialMixed = m_ResidentialMixed;
		writer.Write(residentialMixed);
		float commercialHigh2 = m_CommercialHigh;
		writer.Write(commercialHigh2);
		float residentialLowRent = m_ResidentialLowRent;
		writer.Write(residentialLowRent);
		float forest = m_Forest;
		writer.Write(forest);
		float waterfrontLow = m_WaterfrontLow;
		writer.Write(waterfrontLow);
		float aquacultureLand = m_AquacultureLand;
		writer.Write(aquacultureLand);
		float seagullAmbience = m_SeagullAmbience;
		writer.Write(seagullAmbience);
		float waterfrontLowCommercial = m_WaterfrontLowCommercial;
		writer.Write(waterfrontLowCommercial);
		float oldTown = m_OldTown;
		writer.Write(oldTown);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float residentialLow = ref m_ResidentialLow;
		reader.Read(out residentialLow);
		ref float commercialHigh = ref m_CommercialHigh;
		reader.Read(out commercialHigh);
		ref float industrial = ref m_Industrial;
		reader.Read(out industrial);
		ref float agriculture = ref m_Agriculture;
		reader.Read(out agriculture);
		ref float forestry = ref m_Forestry;
		reader.Read(out forestry);
		ref float oil = ref m_Oil;
		reader.Read(out oil);
		ref float ore = ref m_Ore;
		reader.Read(out ore);
		if (!(reader.context.version > Version.zoneAmbience))
		{
			return;
		}
		ref float officeLow = ref m_OfficeLow;
		reader.Read(out officeLow);
		ref float officeHigh = ref m_OfficeHigh;
		reader.Read(out officeHigh);
		ref float residentialMedium = ref m_ResidentialMedium;
		reader.Read(out residentialMedium);
		ref float residentialHigh = ref m_ResidentialHigh;
		reader.Read(out residentialHigh);
		ref float residentialMixed = ref m_ResidentialMixed;
		reader.Read(out residentialMixed);
		ref float commercialHigh2 = ref m_CommercialHigh;
		reader.Read(out commercialHigh2);
		ref float residentialLowRent = ref m_ResidentialLowRent;
		reader.Read(out residentialLowRent);
		if (reader.context.version > Version.forestAmbience)
		{
			ref float forest = ref m_Forest;
			reader.Read(out forest);
			if (reader.context.version > Version.waterfrontAmbience)
			{
				ref float waterfrontLow = ref m_WaterfrontLow;
				reader.Read(out waterfrontLow);
			}
		}
		if (reader.context.version < Version.forestAmbientFix)
		{
			m_Forest *= 0.0625f;
		}
		if (reader.context.format.Has(FormatTags.AquacultureLandAmbience))
		{
			ref float aquacultureLand = ref m_AquacultureLand;
			reader.Read(out aquacultureLand);
		}
		if (reader.context.format.Has(FormatTags.SeagullAmbience))
		{
			ref float seagullAmbience = ref m_SeagullAmbience;
			reader.Read(out seagullAmbience);
		}
		if (reader.context.format.Has(FormatTags.WaterfrontLowCommercialZone))
		{
			ref float waterfrontLowCommercial = ref m_WaterfrontLowCommercial;
			reader.Read(out waterfrontLowCommercial);
		}
		if (reader.context.format.Has(FormatTags.OldTownAmbience))
		{
			ref float oldTown = ref m_OldTown;
			reader.Read(out oldTown);
		}
	}

	public int GetStride(Context context)
	{
		return 80;
	}
}
