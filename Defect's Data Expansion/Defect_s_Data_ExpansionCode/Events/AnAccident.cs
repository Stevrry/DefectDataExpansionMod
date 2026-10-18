using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events
{
    /// <summary>
    /// 事件：一场意外
    /// 三个戴着面具的土匪从灌木中跳出来挡住你去路，为首的家伙把你错认成了「外高尔爵士」。
    /// 你三下五除二把他们打趴在地，现在考虑怎么处置他们。
    /// 选项一：押去审判 —— 获得一张疑虑（诅咒），外加一组卡牌奖励和一件随机遗物。
    /// 选项二：拿走钱财 —— 获得 60~80 金币。
    ///
    /// 事件图片暂时使用原版事件「丛林迷宫冒险」的图片。
    /// </summary>
    [RegisterActEvent(typeof(Glory))]
    public sealed class AnAccident : ModEventTemplate
    {
        /// <summary>选项二金币的下限（含）。</summary>
        private const int MinGold = 60;

        /// <summary>选项二金币的上限（含）。</summary>
        private const int MaxGold = 80;

        /// <summary>暂时复用原版事件「丛林迷宫冒险」的立绘。</summary>
        public override EventAssetProfile AssetProfile =>
            new(InitialPortraitPath: "res://images/events/jungle_maze_adventure.png");

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new GoldVar(0)
        ];

        /// <summary>关掉「数据扩展」总开关时本事件也不出现，与卡牌 / 遗物 / 其他事件保持一致。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EffectiveEnableExpansion;

        /// <summary>
        /// 每次进事件先掷好这一局的钱数，所以按钮上显示的就是实际会给的数，玩家不用赌（同原版「大打出手」）。
        /// </summary>
        public override void CalculateVars()
        {
            DynamicVars.Gold.BaseValue = Rng.NextInt(MinGold, MaxGold + 1);
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [
            // 悬停里把「疑虑」这张诅咒原样展示出来（同原版「大打出手」展示「受伤」）
            new EventOption(this, SendToTrial, InitialOptionKey("TRIAL"), HoverTipFactory.FromCardWithCardHoverTips<Doubt>()),
            new EventOption(this, TakeTheMoney, InitialOptionKey("TAKE_MONEY"))
        ];

        /// <summary>选项一：先塞一张疑虑，再给「一组卡牌奖励 + 一件随机遗物」。</summary>
        private async Task SendToTrial()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await CardPileCmd.AddCurseToDeck<Doubt>(owner);

            List<Reward> rewards =
            [
                // 3 张自选 1 张，牌池就是玩家自己的（鸡煲的卡池，含本模组新增的卡）
                new CardReward(CardCreationOptions.ForNonCombatWithDefaultOdds([owner.Character.CardPool]), 3, owner),
                // 从本局遗物袋里抽一件，稀有度随机
                new RelicReward(owner)
            ];

            await RewardsCmd.OfferCustom(owner, rewards);

            SetEventFinished(PageDescription("TRIAL"));
        }

        /// <summary>选项二：直接给钱。</summary>
        private async Task TakeTheMoney()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, owner);

            SetEventFinished(PageDescription("TAKE_MONEY"));
        }
    }
}
