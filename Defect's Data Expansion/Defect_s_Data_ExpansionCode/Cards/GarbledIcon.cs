using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 乱码的图标：将你身上所有能力与状态的层数除以 2，并重置成同类里的随机一项。
    /// 正面能力只从打出这张牌的角色的池子里抽，负面状态从全部负面状态里抽。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class GarbledIcon : ModCardTemplate
    {
        /// <summary>「衰退」悬停说明所在的本地化表。</summary>
        private const string StaticHoverTipTable = "static_hover_tips";

        /// <summary>「衰退」在 static_hover_tips 里的条目名（后面接 .title / .description）。</summary>
        private const string DeclineLocEntry = "DEFECT_S_DATA_EXPANSION_DECLINE";

        public GarbledIcon() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            Creature creature = Owner.Creature;

            // 先快照：下面要一边移除一边施加，枚举中途改集合会直接抛异常。
            // 寄生在别的能力上的临时能力（ITemporaryPower）跳过 —— 硬拆掉会让它的宿主对不上账。
            List<PowerModel> powers = creature.Powers
                .Where(power => power is not ITemporaryPower)
                .ToList();

            foreach (PowerModel power in powers)
            {
                bool wasBuff = power.TypeForCurrentAmount == PowerType.Buff;

                int halved = power.Amount / 2;

                await PowerCmd.Remove(power);

                if (halved == 0)
                {
                    continue;
                }

                PowerModel? replacement = PickReplacement(wasBuff);
                if (replacement is null)
                {
                    continue;
                }

                // 施加者填自己、来源填这张牌，和原版自施加能力的写法一致
                await PowerCmd.Apply(choiceContext, replacement, creature, halved, creature, this);
            }
        }

        /// <summary>按类别抽一个新能力。</summary>
        private PowerModel? PickReplacement(bool wasBuff)
        {
            IEnumerable<PowerModel> pool = wasBuff
                ? CharacterBuffPool()
                : ModelDb.AllPowers.Where(power => power.IsVisible && power.Type == PowerType.Debuff);

            return Owner.RunState.Rng.CombatCardGeneration.NextItem(pool.ToList())?.ToMutable();
        }

        /// <summary>
        /// 「这个角色能获得的能力」引擎里没有现成的表（没有 PowerPool 这种东西），
        /// 所以从角色自己的卡池反推：把该角色每张牌的 PowerVar&lt;T&gt; 里的 T 收集起来。
        /// IsVisible 用来滤掉那些幕后干活、不该露面的能力。
        /// </summary>
        private IEnumerable<PowerModel> CharacterBuffPool()
        {
            HashSet<Type> types = new();

            foreach (CardModel card in Owner.Character.CardPool.AllCards)
            {
                foreach (KeyValuePair<string, DynamicVar> entry in card.DynamicVars)
                {
                    Type varType = entry.Value.GetType();
                    if (!varType.IsGenericType || varType.GetGenericTypeDefinition() != typeof(PowerVar<>))
                    {
                        continue;
                    }

                    types.Add(varType.GetGenericArguments()[0]);
                }
            }

            return ModelDb.AllPowers.Where(power => power.IsVisible && power.Type == PowerType.Buff && types.Contains(power.GetType()));
        }

        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            new HoverTip(
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".title"),
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".description"))
        ];

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
