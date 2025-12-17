using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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

        private const int TargetHp = 25;
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
                        args.Tooltip = new TextObject($"Sets you to {TargetHp} HP, heals, adds {KnightsToAdd} Vlandian Knights, then brings you to your home settlement.");

                    return show;
                },
                (MenuCallbackArgs args) =>
                {
                    SantaLore();
                    SantaWarn();

                    HealHero();
                    AddKnights();
                    GoToHomeSettlement();
                },
                isLeave: true,
                index: 0
            );
        }

        private void SantaWarn()
        {
            // "in red or something" — Bannerlord chat supports color tags.
            // If your build ignores this, it will just show plain text.
            InformationManager.DisplayMessage(new InformationMessage(
                "<color=#FF3333>Santa Warns You Not to Wait in town</color>"
            ));
        }

        private void SantaLore()
        {
            // A tiny Calradia-flavored Christmas tale in chat.
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

            hero.HitPoints = TargetHp;
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

        private void GoToHomeSettlement()
        {
            var mp = MobileParty.MainParty;
            if (mp == null) return;

            Settlement home = Hero.MainHero?.HomeSettlement;
            if (home == null) return;

            EnterSettlementAction.ApplyForParty(mp, home);

            string menuId = home.IsTown ? "town" : (home.IsCastle ? "castle" : "village");
            GameMenu.ActivateGameMenu(menuId);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
