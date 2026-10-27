using DefectsDataExpansion.DefectsDataExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 热量收集：每当你失去或获得过热时，对所有敌人造成 6(9) 点伤害。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class HeatCollection : ModCardTemplate
    {
        /// <summary>每次触发伤害的变量名。</summary>
        private const string DamageVar = "HeatCollectionPower";

        public HeatCollection() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            // 用 PowerVar 而不是 DamageVar：这个 6 是能力的层数（每次触发打多少），
            new PowerVar<HeatCollectionPower>(6m)
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
            HoverTipFactory.FromPower<OverheatPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            await PowerCmd.Apply<HeatCollectionPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[DamageVar].BaseValue,
                Owner.Creature,
                this);
        }

        /// <summary>升级只加每次触发的伤害 6 → 9。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars[DamageVar].UpgradeValueBy(3m);
        }
    }
}
