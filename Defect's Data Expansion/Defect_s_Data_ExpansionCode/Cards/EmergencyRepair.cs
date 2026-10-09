using System.Collections.Generic;
using System.Linq;
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
    /// 抢修：失去 1(2) 层故障。消耗。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class EmergencyRepair : ModCardTemplate
    {
        public EmergencyRepair() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<Malfunction>(1m)
        ];

        /// <summary>消耗。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<Malfunction>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 必须判「身上有没有故障」：PowerCmd.Apply 在目标没有该能力时会新建一个实例，
            // 而新建那条路径不检查 ShouldRemoveDueToAmount，负数层数的故障会一直挂在身上。
            // 已经存在时会走 ModifyAmount，那边会自动把降到 0 以下的能力移除。
            if (!Owner.Creature.Powers.Any(power => power is Malfunction))
            {
                return;
            }

            await PowerCmd.Apply<Malfunction>(
                choiceContext,
                Owner.Creature,
                -DynamicVars["Malfunction"].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["Malfunction"].UpgradeValueBy(1m);
        }
    }
}
