using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 被感染的香炉：拾起时获得一张独特的诅咒（蓝宝石蠕虫）。
    /// 每 6 个自己的回合，获得 1 层无实体。
    /// </summary>
    [RegisterRelic(typeof(DefectRelicPool))]
    public sealed class InfectedIncenseBurner : ModRelicTemplate
    {
        private int _turnsSeen;

        /// <summary>
        /// 事件遗物：只会从「废墟」事件的宝箱里出来，不进任何遗物袋。
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>
        /// 拾起时有一次性的塞诅咒效果，所以不能拿去和遗物商人交换
        /// </summary>
        public override bool HasUponPickupEffect => true;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>遗物图标上显示还差几个回合。</summary>
        public override bool ShowCounter => true;

        public override int DisplayAmount => TurnsSeen;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<IntangiblePower>(1m),
            new DynamicVar("Turns", 6m)
        ];

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://Defect_s_Data_Expansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://Defect_s_Data_Expansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://Defect_s_Data_Expansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时先把拾起时拿到的诅咒原样展示出来，再说明「无实体」是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            .. HoverTipFactory.FromCardWithCardHoverTips<SapphireWorm>(),
            HoverTipFactory.FromPower<IntangiblePower>()
        ];

        /// <summary>
        /// 已经过了几个自己的回合。
        /// </summary>
        [SavedProperty]
        public int TurnsSeen
        {
            get => _turnsSeen;
            set
            {
                AssertMutable();
                _turnsSeen = value;
                InvokeDisplayAmountChanged();
            }
        }

        /// <summary>拾起时往牌组里塞一张诅咒。</summary>
        public override async Task AfterObtained()
        {
            await CardPileCmd.AddCurseToDeck<SapphireWorm>(Owner);
        }

        /// <summary>
        /// 数自己回合；数满 6 个就给 1 层无实体。
        /// </summary>
        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            // 这个钩子双方回合都触发，只认自己那一侧
            if (!participants.Contains(Owner.Creature))
            {
                return;
            }

            int turns = DynamicVars["Turns"].IntValue;
            TurnsSeen = (TurnsSeen + 1) % turns;

            // 还差一回合就高亮，提醒玩家下回合要发（同原版「快乐花」）
            Status = TurnsSeen == turns - 1 ? RelicStatus.Active : RelicStatus.Normal;

            if (TurnsSeen != 0)
            {
                return;
            }

            Flash();

            await PowerCmd.Apply<IntangiblePower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["IntangiblePower"].BaseValue,
                Owner.Creature,
                null);
        }

        /// <summary>战斗结束后把高亮收掉，别留到地图上。</summary>
        public override Task AfterCombatEnd(CombatRoom _)
        {
            Status = RelicStatus.Normal;
            return Task.CompletedTask;
        }
    }
}
