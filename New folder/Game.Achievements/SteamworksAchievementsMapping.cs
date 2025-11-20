using Colossal.PSI.Steamworks;
using UnityEngine.Scripting;

namespace Game.Achievements;

[Preserve]
public class SteamworksAchievementsMapping : SteamworksAchievementsMapper
{
	public SteamworksAchievementsMapping()
	{
		Map(Achievements.MyFirstCity, "ACH_MYFIRSTCITY");
		Map(Achievements.TheInspector, "ACH_THEINSPECTOR");
		Map(Achievements.HappytobeofService, "ACH_HAPPYTOBEOFSERVICE");
		Map(Achievements.RoyalFlush, "ACH_ROYALFLUSH");
		Map(Achievements.KeyToTheCity, "ACH_KEYTOTHECITY");
		Map(Achievements.SixFigures, "ACH_SIXFIGURES", "STAT_POPULATION");
		Map(Achievements.GoAnywhere, "ACH_GOANYWHERE", "STAT_TRANSPORTLINES");
		Map(Achievements.TheSizeOfGolfBalls, "ACH_THESIZEOFGOLFBALLS");
		Map(Achievements.OutforaSpin, "ACH_OUTFORASPIN");
		Map(Achievements.NowTheyreAllAshTrees, "ACH_NOWTHEYREALLASHTREES");
		Map(Achievements.ZeroEmission, "ACH_ZEROEMISSION");
		Map(Achievements.UpAndAway, "ACH_UPANDAWAY", "STAT_AIRPORTS");
		Map(Achievements.MakingAMark, "ACH_MAKINGAMARK", "STAT_SIGNATUREBUILDINGS");
		Map(Achievements.EverythingTheLightTouches, "ACH_EVERYTHINGTHELIGHTTOUCHES", "STAT_MAPTILES");
		Map(Achievements.CallingtheShots, "ACH_CALLINGTHESHOTS", "STAT_CALLINGTHESHOTS");
		Map(Achievements.WideVariety, "ACH_WIDEVARIETY", "STAT_WIDEVARIETY");
		Map(Achievements.ExecutiveDecision, "ACH_EXECUTIVEDECISION");
		Map(Achievements.AllSmiles, "ACH_ALLSMILES");
		Map(Achievements.YouLittleStalker, "ACH_YOULITTLESTALKER");
		Map(Achievements.IMadeThis, "ACH_IMADETHIS");
		Map(Achievements.Cartography, "ACH_CARTOGRAPHY");
		Map(Achievements.TheExplorer, "ACH_THEEXPLORER", "STAT_MAPTILES");
		Map(Achievements.TheLastMileMarker, "ACH_THELASTMILEMARKER", "STAT_MILESTONES");
		Map(Achievements.FourSeasons, "ACH_FOURSEASONS");
		Map(Achievements.Spiderwebbing, "ACH_SPIDERWEBBING", "STAT_TRANSPORTLINES");
		Map(Achievements.Snapshot, "ACH_SNAPSHOT");
		Map(Achievements.ThisIsNotMyHappyPlace, "ACH_THISISNOTMYHAPPYPLACE");
		Map(Achievements.TheArchitect, "ACH_THEARCHITECT", "STAT_SIGNATUREBUILDINGS");
		Map(Achievements.SimplyIrresistible, "ACH_SIMPLYIRRESISTIBLE");
		Map(Achievements.TopoftheClass, "ACH_TOPOFTHECLASS");
		Map(Achievements.TheDeepEnd, "ACH_THEDEEPEND", "STAT_LOAN");
		Map(Achievements.Groundskeeper, "ACH_GROUNDSKEEPER", "STAT_PARKS");
		Map(Achievements.ColossalGardener, "ACH_COLOSSALGARDENER");
		Map(Achievements.StrengthThroughDiversity, "ACH_STRENGTHTHROUGHDIVERSITY");
		Map(Achievements.SquasherDowner, "ACH_SQUASHERDOWNER", "STAT_BUILDINGSBULLDOZED");
		Map(Achievements.ALittleBitofTLC, "ACH_ALITTLEBITOFTLC", "STAT_TREATEDCITIZENS");
		Map(Achievements.WelcomeOneandAll, "ACH_WELCOMEONEANDALL", "STAT_TOURISTS");
		Map(Achievements.OneofEverything, "ACH_ONEOFEVERYTHING");
		Map(Achievements.HowMuchIsTheFish, "ACH_HOWMUCHISTHEFISH", "STAT_FISH");
		Map(Achievements.ShipIt, "ACH_SHIPIT", "STAT_CARGOPORT_RESOURCES");
		Map(Achievements.ADifferentPlatformer, "ACH_ADIFFERENTPLATFORMER", "STAT_OFFSHORE_OIL");
		Map(Achievements.DrawMeLikeOneOfYourLiftBridges, "ACH_DRAWMELIKEONEOFYOURLIFTBRIDGES", "STAT_BRIDGES");
		Map(Achievements.ItsPronouncedKey, "ACH_ITSPRONOUNCEDKEY", "STAT_QUAYS");
		Map(Achievements.Pierfect, "ACH_PIERFECT", "STAT_PIERS");
	}
}
