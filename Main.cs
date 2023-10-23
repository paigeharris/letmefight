using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace LetMeFight
{
    public class Main : MBSubModuleBase
    {
        private LetPlayerFight _letPlayerFightBehavior = (LetPlayerFight)null;
        public string menuOptionId = "LetMeFight";

        protected override void OnSubModuleLoad()
        {
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            this.AddBehaviors((CampaignGameStarter)gameStarterObject);
        }

        private void AddBehaviors(CampaignGameStarter gameIntializer)
        {
            this._letPlayerFightBehavior = new LetPlayerFight();
            gameIntializer.AddBehavior((CampaignBehaviorBase)this._letPlayerFightBehavior);
        }
    }
}
