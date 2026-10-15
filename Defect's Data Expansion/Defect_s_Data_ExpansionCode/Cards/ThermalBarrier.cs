using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 热量屏障：在你的回合开始时，每有1层过热，就获得1(2)点格挡。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class ThermalBarrier : ModCardTemplate
    {
        public ThermalBarrier() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(1m, ValueProp.Move)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时说明它盯的是哪个能力。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<ThermalBarrierPower>(),
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

        /// <summary>升级只加「每层过热给几点格挡」1 → 2。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(1m);
        }
    }
}
