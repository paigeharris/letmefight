using System;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace LetMeFight
{
    public class LetPlayerFight : CampaignBehaviorBase
    {
        private static readonly bool VerboseLogging = true;
        private const int RecoveryHitPoints = 25;
        private static readonly object LogLock = new object();
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Configs",
            "LetMeFight.log");

        private bool _pendingMainHeroRecovery;
        private bool _lastLeaveEncounterFlag;
        private bool _handledRetreatSignalForCurrentEncounter;

        private void MarkMainHeroRecoveryPending(string reason)
        {
            _pendingMainHeroRecovery = true;
            Log("MarkMainHeroRecoveryPending: reason=" + reason);
        }

        private static void Log(string message)
        {
            if (!VerboseLogging)
                return;

            try
            {
                string directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [LetMeFight] " + message + Environment.NewLine;
                lock (LogLock)
                    File.AppendAllText(LogFilePath, line);
            }
            catch
            {
                // Keep gameplay unaffected even if logging fails.
            }
        }

        private static string HeroSnapshot(Hero hero)
        {
            if (hero == null)
                return "hero=null";

            return "state=" + hero.HeroState
                   + ", hp=" + hero.HitPoints
                   + ", woundedLimit=" + hero.WoundedHealthLimit
                   + ", isWounded=" + hero.IsWounded
                   + ", isDead=" + hero.IsDead
                   + ", isDisabled=" + hero.IsDisabled;
        }

        private static string MapEventSnapshot(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return "mapEvent=null";

            return "id=" + mapEvent.StringId
                   + ", type=" + mapEvent.EventType
                   + ", state=" + mapEvent.State
                   + ", battleState=" + mapEvent.BattleState
                   + ", isPlayerMapEvent=" + mapEvent.IsPlayerMapEvent
                   + ", endedByRetreat=" + mapEvent.EndedByRetreat
                   + ", isFinalized=" + mapEvent.IsFinalized;
        }

        private static int GetTargetHitPoints(Hero mainHero) =>
            Math.Max(RecoveryHitPoints, mainHero.WoundedHealthLimit + 1);

        private void TryRecoverMainHeroStateAndHealth(string reason)
        {
            Hero mainHero = Hero.MainHero;
            if (mainHero == null)
            {
                Log("TryRecoverMainHeroStateAndHealth: reason=" + reason + ", hero is null.");
                return;
            }

            int targetHitPoints = GetTargetHitPoints(mainHero);
            bool needsRecovery = _pendingMainHeroRecovery
                                 || mainHero.IsWounded
                                 || mainHero.HeroState != Hero.CharacterStates.Active
                                 || mainHero.HitPoints < targetHitPoints;

            Log("TryRecoverMainHeroStateAndHealth: reason=" + reason
                + ", pending=" + _pendingMainHeroRecovery
                + ", targetHitPoints=" + targetHitPoints
                + ", before={" + HeroSnapshot(mainHero) + "}");

            if (!needsRecovery)
            {
                Log("TryRecoverMainHeroStateAndHealth: skipped (no recovery needed).");
                return;
            }

            bool wasWounded = mainHero.IsWounded;
            Hero.CharacterStates previousState = mainHero.HeroState;
            int previousHitPoints = mainHero.HitPoints;

            if (mainHero.HeroState != Hero.CharacterStates.Active)
            {
                Log("TryRecoverMainHeroStateAndHealth: ChangeState -> Active");
                mainHero.ChangeState(Hero.CharacterStates.Active);
            }

            if (mainHero.HitPoints < targetHitPoints)
            {
                int healAmount = targetHitPoints - mainHero.HitPoints;
                if (healAmount > 0)
                {
                    Log("TryRecoverMainHeroStateAndHealth: Heal(" + healAmount + ", addXp=false)");
                    mainHero.Heal(healAmount, false);
                }

                if (mainHero.HitPoints < targetHitPoints)
                {
                    Log("TryRecoverMainHeroStateAndHealth: fallback set HitPoints=" + targetHitPoints);
                    mainHero.HitPoints = targetHitPoints;
                }
            }

            bool healthStatusChanged = wasWounded
                                       || previousState != mainHero.HeroState
                                       || previousHitPoints != mainHero.HitPoints;
            bool stillNeedsRecovery = mainHero.IsWounded
                                      || mainHero.HeroState != Hero.CharacterStates.Active
                                      || mainHero.HitPoints < targetHitPoints;

            _pendingMainHeroRecovery = mainHero.IsWounded
                                       || mainHero.HeroState != Hero.CharacterStates.Active
                                       || mainHero.HitPoints < targetHitPoints;

            Log("TryRecoverMainHeroStateAndHealth: healthStatusChanged=" + healthStatusChanged
                + ", stillNeedsRecovery=" + stillNeedsRecovery
                + ", after={" + HeroSnapshot(mainHero) + "}");

            MobileParty mainParty = MobileParty.MainParty;
            if (mainParty == null)
            {
                Log("TryRecoverMainHeroStateAndHealth: mainParty is null.");
                return;
            }

            if (healthStatusChanged)
            {
                Log("TryRecoverMainHeroStateAndHealth: MemberRoster.OnHeroHealthStatusChanged(mainHero)");
                mainParty.MemberRoster.OnHeroHealthStatusChanged(mainHero);
            }
        }

        private static bool IsPlayerMapEvent(MapEvent mapEvent) =>
            mapEvent != null && mapEvent.IsPlayerMapEvent;

        private static bool IsPlayerEncounterActiveForBattle()
        {
            if (!PlayerEncounter.IsActive)
                return false;

            return IsPlayerMapEvent(PlayerEncounter.Battle) || IsPlayerMapEvent(MapEvent.PlayerMapEvent);
        }

        private void OnMissionEnded(IMission mission)
        {
            MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
            Log("OnMissionEnded: " + MapEventSnapshot(playerMapEvent));
            if (!IsPlayerMapEvent(playerMapEvent))
                return;

            MarkMainHeroRecoveryPending("mission ended during player encounter");
            TryRecoverMainHeroStateAndHealth("OnMissionEnded");
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            Log("OnPlayerBattleEnd: " + MapEventSnapshot(mapEvent));
            if (!IsPlayerMapEvent(mapEvent))
                return;

            MarkMainHeroRecoveryPending("player battle ended");
            TryRecoverMainHeroStateAndHealth("OnPlayerBattleEnd");
        }

        private void OnMapEventContinuityNeedsUpdate(IFaction faction)
        {
            MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
            Log("OnMapEventContinuityNeedsUpdate: " + MapEventSnapshot(playerMapEvent));
            if (!IsPlayerMapEvent(playerMapEvent))
                return;

            MarkMainHeroRecoveryPending("map event continuity update");
            TryRecoverMainHeroStateAndHealth("OnMapEventContinuityNeedsUpdate");
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            Log("OnMapEventEnded: " + MapEventSnapshot(mapEvent));
            if (!IsPlayerMapEvent(mapEvent))
                return;

            MarkMainHeroRecoveryPending("player map event ended");
            TryRecoverMainHeroStateAndHealth("OnMapEventEnded");
        }

        private void OnHeroWounded(Hero hero)
        {
            Log("OnHeroWounded: heroIsMain=" + (hero == Hero.MainHero) + ", heroState={" + HeroSnapshot(hero) + "}");
            if (hero != Hero.MainHero)
                return;

            // Keep battle flow intact; recover once encounter transitions back to campaign.
            MarkMainHeroRecoveryPending("main hero wounded");
        }

        private void OnPlayerDesertedBattle(int desertingTroopCount)
        {
            Log("OnPlayerDesertedBattle: desertingTroopCount=" + desertingTroopCount + ", playerMapEvent=" + MapEventSnapshot(MapEvent.PlayerMapEvent));
            MarkMainHeroRecoveryPending("player clicked retreat/desert");
            TryRecoverMainHeroStateAndHealth("OnPlayerDesertedBattle");
        }

        private void OnCampaignTick(float dt)
        {
            if (!IsPlayerEncounterActiveForBattle())
            {
                _lastLeaveEncounterFlag = false;
                _handledRetreatSignalForCurrentEncounter = false;
                return;
            }

            bool leaveEncounterFlag = PlayerEncounter.LeaveEncounter;
            BattleSimulation simulation = PlayerEncounter.CurrentBattleSimulation;
            bool isPlayerRetreated = simulation != null && simulation.IsPlayerRetreated;

            bool retreatSignalRising = !_lastLeaveEncounterFlag && leaveEncounterFlag;
            _lastLeaveEncounterFlag = leaveEncounterFlag;

            if (_handledRetreatSignalForCurrentEncounter)
                return;

            if (!retreatSignalRising && !isPlayerRetreated)
                return;

            _handledRetreatSignalForCurrentEncounter = true;
            Log("OnCampaignTick: retreat signal detected. leaveEncounterFlag=" + leaveEncounterFlag + ", isPlayerRetreated=" + isPlayerRetreated + ", mapEvent=" + MapEventSnapshot(MapEvent.PlayerMapEvent));
            MarkMainHeroRecoveryPending("retreat signal detected via PlayerEncounter");
            TryRecoverMainHeroStateAndHealth("OnCampaignTick_RetreatSignal");
        }

        public override void RegisterEvents()
        {
            Log("RegisterEvents: initializing listeners. Log file=" + LogFilePath);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, new Action<float>(OnCampaignTick));
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(OnMissionEnded));
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, new Action<MapEvent>(OnPlayerBattleEnd));
            CampaignEvents.OnMapEventContinuityNeedsUpdateEvent.AddNonSerializedListener(this, new Action<IFaction>(OnMapEventContinuityNeedsUpdate));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(OnMapEventEnded));
            CampaignEvents.HeroWounded.AddNonSerializedListener(this, new Action<Hero>(OnHeroWounded));
            CampaignEvents.PlayerDesertedBattleEvent.AddNonSerializedListener(this, new Action<int>(OnPlayerDesertedBattle));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_pendingMainHeroRecovery", ref _pendingMainHeroRecovery);
        }
    }
}
