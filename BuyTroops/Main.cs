using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace LetMeFight
{
    public class Main : MBSubModuleBase
    {
        private LetPlayerFight _letPlayerFightBehavior;

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;

            if (gameStarterObject is CampaignGameStarter campaignStarter)
                AddBehaviors(campaignStarter);
        }

        private void AddBehaviors(CampaignGameStarter gameInitializer)
        {
            _letPlayerFightBehavior = new LetPlayerFight();
            gameInitializer.AddBehavior(_letPlayerFightBehavior);
        }
    }
}
