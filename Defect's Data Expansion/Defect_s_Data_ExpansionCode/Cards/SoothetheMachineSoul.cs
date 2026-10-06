using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 安抚机魂：在下一回合获得 1(2) 层仪式。
    /// </summary>
    [Pool(typeof(DefectCardPool))]
    public sealed class SoothetheMachineSoul : CustomCardModel
    {
        public SoothetheMachineSoul() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<RitualPower>(1m)
        ];

        /// <summary>
        /// 悬停卡牌时显示的提示框：仪式（以及它每回合提供的力量）。
        /// </summary>
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromPower<RitualPower>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            await PowerCmd.Apply<SoothetheMachineSoulRitualPower>(
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
