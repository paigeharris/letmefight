using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;

namespace LetMeFight
{
    public class LetPlayerFight : CampaignBehaviorBase
    {
        private const string MenuOptionId = "LetMeFight";
        private CampaignGameStarter _gameStarter;

        private static void DebugMsg(string message) => InformationManager.DisplayMessage(new InformationMessage(message));

        private void AddFightOption(string menuId)
        {
            if (_gameStarter == null)
            {
                DebugMsg("LetMeFight: game starter is not initialized yet.");
                //
                //
                return;
            }

            _gameStarter.AddGameMenuOption(menuId, MenuOptionId, "Let Me Fight!", grr =>
            {
                grr.optionLeaveType = GameMenuOption.LeaveType.Raid;
                return Hero.MainHero.IsWounded;
            }, grr =>
            {
                if (Hero.MainHero.HitPoints > 20)
                    return;

                Hero.MainHero.HitPoints = 25;
                GameMenu.ActivateGameMenu(menuId);
            }, true, 1, true);
        }

        private void RegisterFightOptions()
        {
            AddFightOption("encounter");
            AddFightOption("menu_siege_strategies");
        }

        private void OnSessionLaunched(CampaignGameStarter gameStarter)
        {
            _gameStarter = gameStarter;
            RegisterFightOptions();
        }

        public override void RegisterEvents() =>
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
