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
    /// 强制扩容：获得 2(3) 个充能球栏位。获得 1 层故障，将一张灼伤加入你的手牌中。
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
            new DynamicVar(OrbSlotsVar, 2m),
            new PowerVar<Malfunction>(1m)
        ];

        /// <summary>悬停时说明它会给自己叠的故障、以及塞进手牌的灼伤。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<Malfunction>(),
            HoverTipFactory.FromCard<Burn>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            await OrbCmd.AddSlots(Owner, DynamicVars[OrbSlotsVar].IntValue);

            await PowerCmd.Apply<Malfunction>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Malfunction"].BaseValue,
                Owner.Creature,
                this);

            // CombatState 声明成可空，但打牌时一定在
            if (CombatState is not { } combatState)
            {
                return;
            }

            CardModel burn = combatState.CreateCard<Burn>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Hand, Owner));
            await Cmd.Wait(0.5f);
        }

        /// <summary>升级只加栏位 2 → 3。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars[OrbSlotsVar].UpgradeValueBy(1m);
        }
    }
}
