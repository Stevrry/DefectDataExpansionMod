using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 冷却：失去所有的过热。每失去1层过热，抽一张牌。获得融入暗影。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Cooling : ModCardTemplate
    {
        /// <summary>融入暗影的变量名。</summary>
        private const string ShadowmeldVar = "ShadowmeldPower";

        public Cooling() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CardsVar(1),
            // 融入暗影只给 1 层：它的层数是 2^层数 的倍率，给 2 层就是 4 倍格挡，不能按过热层数往上堆
            new PowerVar<ShadowmeldPower>(1m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时说明它会烧掉什么、换来什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.FromPower<ShadowmeldPower>(),
            // 融入暗影的收益是格挡，原版那张同类卡也挂这个提示
            HoverTipFactory.Static(StaticHoverTip.Block)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 一次烧完。必须走 ModifyAmount 而不是 PowerCmd.Remove：
            // Remove 直接删能力、不触发层数变化钩子，过热那笔「掉层返还能量」就拿不到了。
            if (Owner.Creature.GetPower<OverheatPower>() is { Amount: > 0 } overheat)
            {
                // 层数先记下来 —— 下面那一句跑完 Amount 就归零了，之后要拿它算抽几张
                int lostStacks = overheat.Amount;

                await PowerCmd.ModifyAmount(choiceContext, overheat, -lostStacks, Owner.Creature, this);

                // 每失去 1 层抽 1 张
                await CardPileCmd.Draw(choiceContext, lostStacks * DynamicVars.Cards.BaseValue, Owner);
            }

            // 没有过热也照样给融入暗影 —— 这张牌真正卖的就是它，烧热只是顺带兑现
            await PowerCmd.Apply<ShadowmeldPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[ShadowmeldVar].BaseValue,
                Owner.Creature,
                this);
        }

        /// <summary>升级只降费 3 → 2。</summary>
        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
