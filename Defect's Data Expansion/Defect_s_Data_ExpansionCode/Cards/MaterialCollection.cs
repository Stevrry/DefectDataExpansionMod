using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 素材收集：将 3 张随机状态牌加入你的手牌。消耗。（升级：耗能 1 → 0，移除消耗）
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class MaterialCollection : ModCardTemplate
    {
        /// <summary>
        /// 不参与随机生成的状态牌：呼唤、狂乱逃离、凋萎、煤灰。
        /// </summary>
        private static readonly Type[] ExcludedStatuses =
        [
            typeof(Beckon),
            typeof(FranticEscape),
            typeof(Wither),
            typeof(Soot)
        ];

        public MaterialCollection() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CardsVar(3)
        ];

        /// <summary>消耗。升级时移除。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Exhaust
        ];

        /// <summary>可生成的候选状态牌：状态牌池去掉 BOSS/遗物专属的四张。</summary>
        private static IEnumerable<CardModel> StatusCandidates =>
            ModelDb.CardPool<StatusCardPool>().AllCards.Where(card => !ExcludedStatuses.Contains(card.GetType()));

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // GetDistinctForCombat：从候选中随机抽互不相同的若干张，并直接创建为本场战斗可用的实例
            IEnumerable<CardModel> generated = CardFactory.GetDistinctForCombat(
                Owner, StatusCandidates, DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration);

            foreach (CardModel card in generated)
            {
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner));
                await Cmd.Wait(0.1f);
            }
        }

        /// <summary>升级：耗能 1 → 0，并移除消耗。</summary>
        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}
