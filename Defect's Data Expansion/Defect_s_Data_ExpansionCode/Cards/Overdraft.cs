using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 透支：获得 5 层过热。在你的回合开始时，获得 1 点能量并额外多抽 1 张牌。（固有。）
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Overdraft : ModCardTemplate
    {
        public Overdraft() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new EnergyVar(1),
            new CardsVar(1),
            new PowerVar<OverheatPower>(5m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时说明它会给自己叠的过热，以及每回合多出来的那点能量。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            EnergyHoverTip
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            // 过热有 5 层上限，这里给满；已经满层时会被削顶，不会超
            await PowerCmd.Apply<OverheatPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["OverheatPower"].BaseValue,
                Owner.Creature,
                this);

            // 层数就是「每回合多出来的能量/抽牌数」。
            await PowerCmd.Apply<OverdraftPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Cards.BaseValue,
                Owner.Creature,
                this);
        }

        /// <summary>升级获得固有。</summary>
        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }
    }
}
