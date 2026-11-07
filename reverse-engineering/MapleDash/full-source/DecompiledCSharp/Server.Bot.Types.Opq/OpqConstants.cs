namespace Server.Bot.Types.Opq;

public static class OpqConstants
{
	public const int OpqLobby = 200080101;

	public const int Stage1 = 920010000;

	public const int Tower = 920010100;

	public const int Stage2 = 920010400;

	public const int ExitLobby = 920011200;

	public const int ChamberlainEak = 2013001;

	public const int CloudPiece = 4001063;

	public static readonly int[] Stage2Items = new int[7] { 4001056, 4001057, 4001058, 4001059, 4001060, 4001061, 4001062 };

	public static readonly int[] Stage1Items = new int[1] { 4001063 };

	public const int Stage1AltarReactorId = 2006000;

	public const int Stage1CloudReactorId = 2002001;

	public const int Stage2BoxReactorFirst = 2002004;

	public const int Stage2BoxReactorLast = 2002010;

	public const int Stage2MusicBoxReactorId = 2008006;

	public static readonly string[] Stage2BoxOrdinals = new string[7] { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th" };

	public const int Stage1EntryX = 266;

	public const int Stage1EntryY = 143;

	public const int Stage1CompleteX = 165;

	public const int Stage1CompleteY = -1270;

	public const int Stage1DropX = 497;

	public const int Stage1DropY = 143;

	public const int Stage1ExitPortalId = 4;

	public const int TowerSpawnX = -260;

	public const int TowerSpawnY = -32;

	public const int TowerWalkX = 159;

	public const int TowerWalkY = -32;

	public const int Stage2SpawnX = -113;

	public const int Stage2SpawnY = -321;

	public const int Stage2DropX = -1588;

	public const int Stage2DropY = -127;

	public const int ExitLobbyX = -161;

	public const int ExitLobbyY = 323;

	public const int LobbyX = -233;

	public const int LobbyY = 174;

	public const long StageWaitTimeoutMs = 120000L;

	public const long RecruitMessageIntervalMs = 12000L;

	public const long NavigateSettleMs = 1500L;

	public const long AssignmentPollMs = 500L;

	public const int MaxReactorHits = 4;

	public const long SwingIntervalMs = 700L;

	public const int ReactorHitRangePx = 200;

	public const double LootScanRangeSq = 8000.0;

	public const int LootFallbackSteps = 20;

	public const int LootFallbackStepPx = 2500;
}
