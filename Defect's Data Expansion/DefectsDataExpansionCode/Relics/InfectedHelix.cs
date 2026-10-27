using DefectsDataExpansion.DefectsDataExpansionCode.Cards;
using DefectsDataExpansion.DefectsDataExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Relics
{
    /// <summary>
    /// 被感染的螺类化石：拾起时获得一张独特的诅咒（蓝宝石蠕虫）。
    /// 每场战斗开始时，获得 1 层缓冲（抵消下一次掉的血）。
    /// </summary>
    [RegisterRelic(typeof(EventRelicPool))]
    public sealed class InfectedHelix : ModRelicTemplate
    {
        /// <summary>
        /// 事件遗物：只会从「废墟」事件的宝箱里出来，不进任何遗物袋。
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>
        /// 拾起时有一次性的塞诅咒效果，所以不能拿去和遗物商人交换
        /// </summary>
        public override bool HasUponPickupEffect => true;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EffectiveEnableExpansion;

        /// <summary>每场战斗开始时给的缓冲层数。</summary>
        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<BufferPower>(1m)
        ];

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://DefectsDataExpansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://DefectsDataExpansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://DefectsDataExpansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时先把拾起时拿到的诅咒原样展示出来，再说明「缓冲」是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            .. HoverTipFactory.FromCardWithCardHoverTips<SapphireWorm>(),
            HoverTipFactory.FromPower<BufferPower>()
        ];

        /// <summary>拾起时往牌组里塞一张诅咒。</summary>
        public override async Task AfterObtained()
        {
            await CardPileCmd.AddCurseToDeck<SapphireWorm>(Owner);
        }

        /// <summary>
        /// 每场战斗开始时给缓冲。发放方式和「被感染的齿轮工艺品」一致。
        /// 免不免伤交给「缓冲」自己管：它只在你真的要掉血时才减 1 层，
        /// 被格挡干净的伤害不会消耗它。
        /// </summary>
        public override async Task BeforeCombatStart()
        {
            Flash();

            await PowerCmd.Apply<BufferPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["BufferPower"].BaseValue,
                Owner.Creature,
                null);
        }
    }
}
