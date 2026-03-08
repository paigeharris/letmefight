using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace LetMeFight
{
    public class LetPlayerFight : CampaignBehaviorBase
    {
        private CampaignGameStarter _starter;

        private const int TargetHp = 21;
        private const int KnightsToAdd = 12;
        private const string KnightId = "vlandian_knight";

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _starter = starter;

            AddButton("encounter");
            AddButton("menu_siege_strategies");
        }

        private void AddButton(string menuId)
        {
            string optionId = $"LetMeFight_SantaRescue_{menuId}";

            _starter.AddGameMenuOption(
                menuId,
                optionId,
                "Santas Rescue Crew (Let Me Fight Option B)",
                (MenuCallbackArgs args) =>
                {
                    var hero = Hero.MainHero;
                    if (hero == null) return false;

                    bool show = hero.IsWounded || hero.HitPoints < TargetHp;
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                    if (show)
                    {
                        args.Tooltip = new TextObject(
                            $"Sets you to {TargetHp} HP, heals, adds {KnightsToAdd} Vlandian Knights, then brings you to your home settlement."
                        );
                    }

                    return show;
                },
                (MenuCallbackArgs args) =>
                {
                    ExecuteSantaRescue();
                },
                isLeave: true,
                index: 0
            );
        }

        private void ExecuteSantaRescue()
        {
            try
            {
                SantaLore();
                SantaWarn();

                HealHero();
                AddKnights();

                if (!TryGoToHomeSettlement())
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Santa healed you and brought knights, but could not safely move you home."
                    ));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("LetMeFight rescue failed: " + ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    "Let Me Fight blocked a rescue error and left you in the current menu instead of crashing."
                ));
            }
        }

        private void SantaWarn()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "<color=#FF3333>Santa Warns You Not to Wait in town</color>"
            ));
        }

        private void SantaLore()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "A bell rings over the snow. Twelve Vlandian riders appear through the whiteout, lances tucked, horses steaming."
            ));
            InformationManager.DisplayMessage(new InformationMessage(
                "They lift you onto a saddle blanket stitched with holly thread and ride hard for your home walls before the cold can claim you."
            ));
            InformationManager.DisplayMessage(new InformationMessage(
                "No banners. No names. Only hoofbeats... and a laugh that sounds like winter itself."
            ));
            InformationManager.DisplayMessage(new InformationMessage(
                " "
            ));
        }

        private void HealHero()
        {
            var hero = Hero.MainHero;
            if (hero == null) return;

            hero.HitPoints = Math.Min(TargetHp, hero.MaxHitPoints);
            hero.Heal(1, addXp: false);
        }

        private void AddKnights()
        {
            var mp = MobileParty.MainParty;
            if (mp == null) return;

            var knight = MBObjectManager.Instance.GetObject<CharacterObject>(KnightId);
            if (knight == null) return;

            mp.MemberRoster.AddToCounts(knight, KnightsToAdd);
        }

        private bool TryGoToHomeSettlement()
        {
            var mainParty = MobileParty.MainParty;
            Settlement home = ResolveHomeSettlement();
            if (mainParty == null || home == null)
                return false;

            ResetEncounterState(mainParty);
            EncounterManager.StartSettlementEncounter(mainParty, home);
            return true;
        }

        private void ResetEncounterState(MobileParty mainParty)
        {
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish();
                return;
            }

            if (mainParty.CurrentSettlement != null)
            {
                LeaveSettlementAction.ApplyForParty(mainParty);
                PlayerEncounter.LocationEncounter = null;
            }
        }

        private Settlement ResolveHomeSettlement()
        {
            Settlement home = Hero.MainHero?.HomeSettlement;
            if (IsUsableSettlement(home))
                return home;

            home = MobileParty.MainParty?.HomeSettlement;
            if (IsUsableSettlement(home))
                return home;

            home = Hero.MainHero?.CurrentSettlement;
            if (IsUsableSettlement(home))
                return home;

            home = Hero.MainHero?.BornSettlement;
            return IsUsableSettlement(home) ? home : null;
        }

        private bool IsUsableSettlement(Settlement settlement)
        {
            return settlement != null
                && settlement.IsActive
                && settlement.Party != null
                && settlement.SettlementComponent != null
                && !settlement.IsHideout;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
