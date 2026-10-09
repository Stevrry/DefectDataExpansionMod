using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events
{
    /// <summary>
    /// 事件：女巫之屋
    /// 今天你经历了很多的战斗，幸运的是你找到一栋屋子……你很想休息了，但你感觉在这里休息……似乎会发生不详的事情。
    /// 选项一：休息一晚 —— 回复 10 点生命，然后与当前阶段的一个随机精英敌人进行战斗；
    ///         这场战斗的奖励会被替换为 2 次来自其他角色的卡牌奖励。
    /// 选项二：搜刮一番，继续赶路 —— 获得一瓶鲜血药水。
    /// 事件图片暂时使用原版事件「镜中倒影，影倒中镜」的图片。
    /// </summary>
    [RegisterSharedEvent]
    public sealed class TheWitchsHouse : ModEventTemplate
    {
        /// <summary>
        /// 选精英时用的具名随机源标签。见 <see cref="PickEliteEncounter"/>，这关系到多人同队的同步。
        /// </summary>
        private const string EliteRngLabel = "the_witchs_house_elite";

        /// <summary>
        /// 从事件里转去战斗的事件必须是 shared 
        /// 会直接对非 shared 的事件抛异常。顺带这也让它出现在每一个阶段的事件池里（ActModel.GenerateRooms 会把共享事件拼到所有阶段后面）。
        /// </summary>
        public override bool IsShared => true;

        /// <summary>暂时复用原版事件「镜中倒影」的立绘。</summary>
        public override EventAssetProfile AssetProfile =>
            new(InitialPortraitPath: "res://images/events/reflections.png");

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new HealVar(10m),   // 选项一的回血
            new CardsVar(2)     // 战胜精英后「来自其他角色的卡牌奖励」的次数
        ];

        /// <summary>
        /// 只有全队都是残血（当前生命低于最大生命的 30%）时才可能被抽到。
        /// 用乘法而不是除法比较，避免小数误差；和原版一样对 runState.Players 全体生效（单人局即自己）。
        /// 关掉「数据扩展」总开关时本事件也不出现，与卡牌 / 遗物的隔离方式保持一致。
        /// </summary>
        public override bool IsAllowed(IRunState runState)
        {
            if (!ExpansionConfig.EnableExpansion)
            {
                return false;
            }

            return runState.Players.All(player => player.Creature.CurrentHp * 10 < player.Creature.MaxHp * 3);
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [
            new EventOption(this, RestForTheNight, InitialOptionKey("REST")),
            new EventOption(this, Scavenge, InitialOptionKey("SCAVENGE"), HoverTipFactory.FromPotion<BloodPotion>())
        ];

        /// <summary>选项一：先回血，再把房间翻到「休息」那一页（后一页才有战斗按钮，与原版「茂密植被」的休息 → 战斗同构）。</summary>
        private async Task RestForTheNight()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await CreatureCmd.Heal(owner.Creature, DynamicVars.Heal.IntValue);

            SetEventState(
                PageDescription("REST"),
                [new EventOption(this, Fight, ModOptionKey("REST", "FIGHT"))]);
        }

        /// <summary>选项二：给一瓶鲜血药水，事件结束。</summary>
        private async Task Scavenge()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await RewardsCmd.OfferCustom(owner, [new PotionReward(ModelDb.Potion<BloodPotion>().ToMutable(), owner)]);
            SetEventFinished(PageDescription("SCAVENGE"));
        }

        /// <summary>
        /// 「休息」页上的战斗按钮：打当前阶段的随机精英，并把两次「来自其他角色的卡牌奖励」作为奖励挂到这场战斗上（替换原本奖励）。
        /// </summary>
        private Task Fight()
        {
            if (Owner is not { } owner)
            {
                return Task.CompletedTask;
            }

            if (PickEliteEncounter(owner) is not { } elite)
            {
                return Task.CompletedTask;
            }

            EnterCombatWithoutExitingEvent(
                elite,
                BuildCardRewardsFromOtherCharacters(owner),
                shouldResumeAfterCombat: false);

            return Task.CompletedTask;
        }

        /// <summary>从当前阶段的精英遭遇里随机挑一个。</summary>
        private static EncounterModel? PickEliteEncounter(Player owner)
        {
            List<EncounterModel> elites = owner.RunState.Act.AllEliteEncounters.ToList();
            if (elites.Count == 0)
            {
                return null;
            }

            // 故意不用事件自带的 Rng：它的种子里混了玩家槽位
            // 多人同队时每个玩家会算出不同的精英，而 EventCombatSynchronizer 要求同一场 shared 事件里
            // 所有人进的是同一个遭遇，否则直接抛异常。改用「运行种子 + 标签 + 层数」，对所有人一致。
            var rng = new Rng(owner.RunState.Rng.Seed, $"{EliteRngLabel}_{owner.RunState.ActFloor}");
            return rng.NextItem(elites);
        }

        /// <summary>
        /// 造 N 次「来自其他角色的卡牌奖励」（N = <see cref="CardsVar"/> 的值）。
        /// 每次奖励里最多放 3 张牌，分别抽自 3 个不同角色的卡池 —— 与原版遗物「万花筒」同一套写法。
        /// </summary>
        private List<Reward> BuildCardRewardsFromOtherCharacters(Player owner)
        {
            List<Reward> rewards = new(DynamicVars.Cards.IntValue);

            CardCreationOptions rerollOptions = CardCreationOptions.ForNonCombatWithDefaultOdds(Array.Empty<CardPoolModel>());

            List<CardPoolModel> otherPools = owner.UnlockState.CharacterCardPools
                .Where(pool => pool != owner.Character.CardPool)
                .ToList();
            if (otherPools.Count == 0)
            {
                // 兜底：解锁状态异常时退回全部角色卡池。
                otherPools = ModelDb.AllCharacterCardPools.Where(pool => pool != owner.Character.CardPool).ToList();
            }

            for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            {
                List<CardModel> cards = new();
                foreach (CardPoolModel pool in otherPools.StableShuffle(owner.RunState.Rng.Niche).Take(3))
                {
                    CardCreationOptions options = new CardCreationOptions(
                        new List<CardPoolModel> { pool },
                        CardCreationSource.Other,
                        CardRarityOddsType.RegularEncounter).WithFlags(CardCreationFlags.NoCardPoolModifications);

                    cards.Add(CardFactory.CreateForReward(owner, 1, options).First().Card);
                }

                if (cards.Count > 0)
                {
                    rewards.Add(new CardReward(cards, CardCreationSource.Other, owner, rerollOptions));
                }
            }

            return rewards;
        }
    }
}
