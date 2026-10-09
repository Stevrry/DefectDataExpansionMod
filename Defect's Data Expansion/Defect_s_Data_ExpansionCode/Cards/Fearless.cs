using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 无惧：造成 6 点伤害。每有一层故障，则增加 7(10) 点额外伤害。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Fearless : ModCardTemplate
    {
        public Fearless() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CalculationBaseVar(6m),          // 基础伤害
            new ExtraDamageVar(7m),              // 每层故障的额外伤害 → {ExtraDamage:diff()}
            new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((CardModel card, Creature? _) => card.Owner.Creature.GetPowerAmount<Malfunction>())
        ];

        /// <summary>伤害随故障层数变化，悬停时给出故障的说明。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<Malfunction>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

            // CalculatedDamage 是算好的总伤害（基础 + 额外 × 故障层数），直接用
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
                .Execute(choiceContext);
        }

        /// <summary>升级只加「每层额外伤害」7 → 10，基础 6 点不变。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.ExtraDamage.UpgradeValueBy(3m);
        }
    }
}
