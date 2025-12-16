using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;

namespace LetMeFight
{
    public class LetPlayerFight : CampaignBehaviorBase
    {
        private CampaignGameStarter _obj = (CampaignGameStarter)null;
        private string _menuOptionId = "LetMeFight";

        public void debugMsg(string message) => InformationManager.DisplayMessage(new InformationMessage(message));

        public void addFightOption(string menuId) => this._obj.AddGameMenuOption(menuId, this._menuOptionId, "Let Me Fight!", (GameMenuOption.OnConditionDelegate)(grr =>
        {
            grr.optionLeaveType = GameMenuOption.LeaveType.Raid;
            return Hero.MainHero.IsWounded;
        }), (GameMenuOption.OnConsequenceDelegate)(grr =>
        {
            if (Hero.MainHero.HitPoints > 20)
                return;
            Hero.MainHero.HitPoints = 25;
            GameMenu.ActivateGameMenu(menuId);
        }), true, 1, true);

        public void letPlayerFightFunc(CampaignGameStarter obj)
        {
            this.addFightOption("encounter");
            this.addFightOption("menu_siege_strategies");
        }

        public void OnSessionLaunched(CampaignGameStarter obj)
        {
            this._obj = obj;
            this.letPlayerFightFunc(this._obj);
        }

        public override void RegisterEvents() => CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
