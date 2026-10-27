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

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 超越时空：获得 2(3) 点能量。抽 3 张牌。将一张虚空和一张灼伤加入你的弃牌堆。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class TranscendingTimeAndSpace : ModCardTemplate
    {
        public TranscendingTimeAndSpace() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new EnergyVar(2),
            new CardsVar(3)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://DefectsDataExpansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>悬停时说明它塞进来的虚空、灼伤，以及它给的能量。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Void>(),
            HoverTipFactory.FromCard<Burn>(),
            EnergyHoverTip
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

            // CombatState 声明成可空，但打牌时一定在；顺手取出来也给下面塞状态牌用
            if (CombatState is not { } combatState)
            {
                return;
            }

            // 原版的「虚空」类名是 Void，和 C# 关键字 void 撞名，必须写全名
            CardModel voidCard = combatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(voidCard, PileType.Discard, Owner));

            CardModel burn = combatState.CreateCard<Burn>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Discard, Owner));

            await Cmd.Wait(0.5f);
        }

        /// <summary>升级只加能量 2 → 3，抽牌数不变。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1m);
        }
    }
}
