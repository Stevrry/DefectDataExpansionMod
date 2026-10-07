using System.Collections.Generic;
using System.Linq;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 扩容磁盘：事件遗物。你的卡牌奖励中额外包含一张牌。
    /// 图标暂时复用原版数据磁盘
    /// </summary>
    [RegisterRelic(typeof(DefectRelicPool))]
    public sealed class ExpansionDisk : ModRelicTemplate
    {
        /// <summary>
        /// 事件遗物
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>暂时使用原版「数据磁盘」的图片。</summary>
        protected override string IconBaseName => "data_disk";

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>战斗卡牌奖励里额外追加一张牌。</summary>
        public override bool TryModifyCardRewardOptions(
            Player player, List<CardCreationResult> rewardOptions, CardCreationOptions creationOptions)
        {
            if (Owner != player)
            {
                return false;
            }

            // 与原版吃不完的糖一致：只影响"战斗结束后的卡牌奖励"，不影响商店、事件等其他来源
            if (creationOptions.Source != CardCreationSource.Encounter)
            {
                return false;
            }
            if (!creationOptions.Flags.HasFlag(CardCreationFlags.IsCardReward))
            {
                return false;
            }
            if (!creationOptions.Flags.HasFlag(CardCreationFlags.IsFromCombat))
            {
                return false;
            }

            List<CardModel> possible = creationOptions.GetPossibleCards(player).ToList();
            if (possible.Count == 0)
            {
                return false;
            }

            // 优先给出与本份奖励已出现的牌不重复的一张；牌池太小则允许重复（同原版做法）
            bool allowDupes = !possible.Any(card => IsNotDuplicate(card, rewardOptions));

            CardCreationOptions options = new CardCreationOptions(
                    creationOptions.CardPools,
                    CardCreationSource.Other,
                    creationOptions.RarityOdds,
                    card => (creationOptions.CardPoolFilter == null || creationOptions.CardPoolFilter(card))
                            && (allowDupes || IsNotDuplicate(card, rewardOptions)))
                .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);

            CardModel? extra = CardFactory.CreateForReward(Owner, 1, options).FirstOrDefault()?.Card;
            if (extra == null)
            {
                return false;
            }

            CardCreationResult result = new CardCreationResult(extra);
            result.ModifyCard(extra, this);
            rewardOptions.Add(result);
            return true;
        }

        private static bool IsNotDuplicate(CardModel card, List<CardCreationResult> rewardOptions) =>
            rewardOptions.TrueForAll(option => option.originalCard.Id != card.Id);
    }
}
