using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
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

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 被感染的齿轮工艺品：拾起时获得一张独特的诅咒（蓝宝石蠕虫）。
    /// 每场战斗开始时，获得 1 层人工制品。
    /// </summary>
    [RegisterRelic(typeof(DefectRelicPool))]
    public sealed class InfectedClockWork : ModRelicTemplate
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
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>每场战斗开始时给的人工制品层数。</summary>
        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<ArtifactPower>(1m)
        ];

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://Defect_s_Data_Expansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://Defect_s_Data_Expansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://Defect_s_Data_Expansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时先把拾起时拿到的诅咒原样展示出来，再说明「人工制品」是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            .. HoverTipFactory.FromCardWithCardHoverTips<SapphireWorm>(),
            HoverTipFactory.FromPower<ArtifactPower>()
        ];

        /// <summary>拾起时往牌组里塞一张诅咒。</summary>
        public override async Task AfterObtained()
        {
            await CardPileCmd.AddCurseToDeck<SapphireWorm>(Owner);
        }

        /// <summary>
        /// 每场战斗开始时给人工制品。发放方式和本模组的「敏捷的记忆」一致，
        /// </summary>
        public override async Task BeforeCombatStart()
        {
            Flash();

            await PowerCmd.Apply<ArtifactPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["ArtifactPower"].BaseValue,
                Owner.Creature,
                null);
        }
    }
}
