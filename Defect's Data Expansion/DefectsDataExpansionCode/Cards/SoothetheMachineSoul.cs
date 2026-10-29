using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using DefectsDataExpansion.DefectsDataExpansionCode.Config; // 内部配置

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 安抚机魂：失去1集中，获得1（2）仪式。
    /// </summary>
    [DisabledCard]
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class SoothetheMachineSoul : ModCardTemplate
    {
        public SoothetheMachineSoul() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<FocusPower>(1m),
            new PowerVar<RitualPower>(1m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://DefectsDataExpansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>
        /// 悬停卡牌时显示的提示框：集中、仪式（以及它每回合提供的力量）。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<FocusPower>(),
            HoverTipFactory.FromPower<RitualPower>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 集中可以扣成负数（原版「偏执认知」就是这么扣的），充能球数值最低只会到 0
            await PowerCmd.Apply<FocusPower>(
                choiceContext,
                Owner.Creature,
                -DynamicVars["FocusPower"].BaseValue,
                Owner.Creature,
                this);

            await PowerCmd.Apply<RitualPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["RitualPower"].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["RitualPower"].UpgradeValueBy(1m);
        }
    }
}
