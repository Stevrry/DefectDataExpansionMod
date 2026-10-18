using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 被感染的鸟面瓮：拾起时获得一张独特的诅咒（蓝宝石蠕虫）。
    /// 每打出 1 张能力牌，回复 2 点生命。
    /// </summary>
    [RegisterRelic(typeof(EventRelicPool))]
    public sealed class InfectedBirdUrn : ModRelicTemplate
    {
        /// <summary>
        /// 事件遗物：只会从「废墟」事件的宝箱里出来，不进任何遗物袋。
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>
        /// 拾起时有一次性的塞诅咒效果，所以不能拿去和遗物商人交换
        /// </summary>
        public override bool HasUponPickupEffect => true;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>每张能力牌回复的生命。</summary>
        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new HealVar(2m)
        ];

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://Defect_s_Data_Expansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://Defect_s_Data_Expansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://Defect_s_Data_Expansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时把要拿到的那张诅咒原样展示出来。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
            HoverTipFactory.FromCardWithCardHoverTips<SapphireWorm>();

        /// <summary>拾起时往牌组里塞一张诅咒。塞的是「牌组」不是「手牌」，所以没有战斗中的问题。</summary>
        public override async Task AfterObtained()
        {
            await CardPileCmd.AddCurseToDeck<SapphireWorm>(Owner);
        }

        /// <summary>每打出一张能力牌就回血。</summary>
        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 和原版「木乃伊之手」同一套前置判断
            if (!CombatManager.Instance.IsInProgress)
            {
                return;
            }
            if (cardPlay.Card.Owner != Owner)
            {
                return;
            }
            if (cardPlay.Card.Type != CardType.Power)
            {
                return;
            }

            // 满血时不给触发，免得白闪一下
            if (Owner.Creature.CurrentHp >= Owner.Creature.MaxHp)
            {
                return;
            }

            Flash();

            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        }
    }
}
