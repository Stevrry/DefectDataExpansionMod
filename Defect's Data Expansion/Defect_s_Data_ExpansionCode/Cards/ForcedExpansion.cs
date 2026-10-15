using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 强制扩容：获得1个充能球栏位，获得1层过热。
    /// 未升级时还会往手牌里塞一张灼伤。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class ForcedExpansion : ModCardTemplate
    {
        /// <summary>充能球栏位的变量名。</summary>
        private const string OrbSlotsVar = "OrbSlots";

        public ForcedExpansion() : base(1, CardType.Power, CardRarity.Common, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar(OrbSlotsVar, 1m),
            new PowerVar<OverheatPower>(1m)
        ];

        /// <summary>悬停时说明它会给自己叠的过热；升级后不再塞灼伤，就不显示灼伤的提示。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips => BuildHoverTips();

        private IEnumerable<IHoverTip> BuildHoverTips()
        {
            yield return HoverTipFactory.FromPower<OverheatPower>();

            if (!IsUpgraded)
            {
                yield return HoverTipFactory.FromCard<Burn>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            await OrbCmd.AddSlots(Owner, DynamicVars[OrbSlotsVar].IntValue);

            await PowerCmd.Apply<OverheatPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["OverheatPower"].BaseValue,
                Owner.Creature,
                this);

            // 升级后不再往手牌里塞灼伤
            if (IsUpgraded)
            {
                return;
            }

            // CombatState 声明成可空，但打牌时一定在
            if (CombatState is not { } combatState)
            {
                return;
            }

            CardModel burn = combatState.CreateCard<Burn>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Hand, Owner));
            await Cmd.Wait(0.5f);
        }

    }
}
