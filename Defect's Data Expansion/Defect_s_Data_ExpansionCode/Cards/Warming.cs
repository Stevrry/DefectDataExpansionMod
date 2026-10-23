using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 升温：消耗任意张手牌，在本场战斗中将过热的下限提高 1(2)。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Warming : ModCardTemplate
    {
        public Warming() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            // 每次打出把过热的层数地板提高几层
            new PowerVar<WarmingPower>(1m)
        ];

        /// <summary>悬停时说明它涉及的两个机制。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.FromPower<WarmingPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            // 任意张：最少 0 张、最多不设限（原版「净化」/「守卫」都是这个写法）
            foreach (CardModel card in await CardSelectCmd.FromHand(
                         prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, 999999999),
                         context: choiceContext,
                         player: Owner,
                         filter: null,
                         source: this))
            {
                await CardCmd.Exhaust(choiceContext, card);
            }

            await PowerCmd.Apply<WarmingPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["WarmingPower"].BaseValue,
                Owner.Creature,
                this);

            // 地板立即生效：当前过热不够就补到地板；身上没有过热时直接施加地板层数。
            // 之后所有掉层都会被 OverheatPower 拦在地板上。
            decimal floor = Owner.Creature.GetPower<WarmingPower>()?.Amount ?? 0m;
            if (floor <= 0m)
            {
                return;
            }

            if (Owner.Creature.GetPower<OverheatPower>() is { } overheat)
            {
                decimal missing = floor - overheat.Amount;
                if (missing > 0m)
                {
                    await PowerCmd.ModifyAmount(choiceContext, overheat, missing, Owner.Creature, this);
                }
            }
            else
            {
                await PowerCmd.Apply<OverheatPower>(
                    choiceContext,
                    Owner.Creature,
                    floor,
                    Owner.Creature,
                    this);
            }
        }

        /// <summary>升级把地板提高量 1 → 2。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars["WarmingPower"].UpgradeValueBy(1m);
        }
    }
}
