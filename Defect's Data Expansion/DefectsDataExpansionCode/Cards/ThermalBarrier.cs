using DefectsDataExpansion.DefectsDataExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using DefectsDataExpansion.DefectsDataExpansionCode.Config; // 内部配置

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 热量屏障：每当你失去或获得过热时，就获得2(3)点格挡。
    /// </summary>
    [DisabledCard]
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class ThermalBarrier : ModCardTemplate
    {
        public ThermalBarrier() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(2m, ValueProp.Move)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://DefectsDataExpansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时说明它盯的是哪个能力。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<ThermalBarrierPower>(),
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            await PowerCmd.Apply<ThermalBarrierPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Block.BaseValue,
                Owner.Creature,
                this);
        }

        /// <summary>升级只加「每次过热变化给几点格挡」2 → 3。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(1m);
        }
    }
}
