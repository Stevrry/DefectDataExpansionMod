using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events
{
    /// <summary>
    /// 事件：废墟
    /// 碎石间，半截石门斜斜地立着。门后是一只古老的宝箱，箱盖上刻满了你认不出的符号，
    /// 同时宝箱里传来细微的沙沙声。你会打开宝箱，还是在一旁搜寻？
    /// 选项一：打开宝箱 —— 获得一件随机「被感染的旧日遗物」（外加它自带的那张诅咒）。
    /// 选项二：四周搜寻 —— 获得一张旧日卡牌（一次可以跳过的卡牌奖励）。
    ///
    /// 事件图片暂时使用原版事件「沉没的宝库」的图片。
    /// </summary>
    // 文档没给层数，所以三个阶段都注册（和「好心人」一样）
    [RegisterActEvent(typeof(Overgrowth))]
    [RegisterActEvent(typeof(Underdocks))]
    [RegisterActEvent(typeof(Hive))]
    public sealed class Ruins : ModEventTemplate
    {
        /// <summary>
        /// 宝箱里能开出的四件「被感染的旧日遗物」。
        /// 诅咒不在这儿发：每件遗物自己的会往牌组里塞一张蓝宝石蠕虫。
        /// </summary>
        private static readonly Type[] InfectedRelics =
        [
            typeof(InfectedBirdUrn),
            typeof(InfectedClockWork),
            typeof(InfectedHelix),
            typeof(InfectedIncenseBurner)
        ];

        /// <summary>暂时复用原版事件「沉没的宝库」的立绘。</summary>
        public override EventAssetProfile AssetProfile =>
            new(InitialPortraitPath: "res://images/events/sunken_treasury.png");

        /// <summary>关掉「数据扩展」总开关时本事件也不出现，与卡牌 / 遗物 / 其他事件保持一致。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [
            // 宝箱里是哪一件是随机的，悬停里说不出具体是哪件（同「好心人」选项二）
            new EventOption(this, OpenTheChest, InitialOptionKey("OPEN_CHEST")),
            new EventOption(this, SearchAround, InitialOptionKey("SEARCH"))
        ];

        /// <summary>选项一：开箱，随机给一件还没拿到的被感染遗物。</summary>
        private async Task OpenTheChest()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            List<Type> candidates = InfectedRelics.Where(type => !OwnsRelic(owner, type)).ToList();

            // 四件都拿过了（得碰上这个事件四次），那就随便再开一件，别让按钮点了没反应
            if (candidates.Count == 0)
            {
                candidates = InfectedRelics.ToList();
            }

            Type chosen = Rng.NextItem(candidates)!;
            RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(chosen)).ToMutable();

            await RelicCmd.Obtain(relic, owner);

            SetEventFinished(PageDescription("OPEN_CHEST"));
        }

        /// <summary>
        /// 选项二：一张「被遗忘的旧日卡牌」，从下面那张固定的牌表里随机抽。
        /// 走原版卡牌奖励界面（<see cref="CardReward.CanSkip"/> 默认就是 true），所以玩家可以跳过。
        /// </summary>
        private async Task SearchAround()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            // 牌表是写死的非空数组，NextItem 不可能抽空
            CardModel card = owner.RunState.CreateCard(Rng.NextItem(OldWorldCards)!, owner);

            // 第二个参数是「给几张」，这里只给 1 张
            CardCreationOptions rerollOptions = CardCreationOptions.ForNonCombatWithDefaultOdds([]);

            await RewardsCmd.OfferCustom(owner, [new CardReward([card], CardCreationSource.Other, owner, rerollOptions)]);

            SetEventFinished(PageDescription("SEARCH"));
        }

        /// <summary>
        /// 「被遗忘的旧日卡牌」的牌表，和原版事件「垃圾堆」完全一致。
        /// </summary>
        private static CardModel[] OldWorldCards =>
        [
            ModelDb.Card<HelloWorld>(),     // 你好世界
            ModelDb.Card<Rebound>(),        // 弹回
            ModelDb.Card<Stack>(),          // 堆栈
            ModelDb.Card<RipAndTear>(),     // 狂乱撕扯
            ModelDb.Card<Caltrops>(),       // 铁蒺藜
            ModelDb.Card<Outmaneuver>(),    // 抢占先机
            ModelDb.Card<Distraction>(),    // 声东击西
            ModelDb.Card<DualWield>(),      // 双持
            ModelDb.Card<Clash>(),          // 交锋
            ModelDb.Card<Entrench>()        // 巩固
        ];

        /// <summary>玩家身上是否已经有这件遗物（遗物本身不允许重复拿）。</summary>
        private static bool OwnsRelic(Player owner, Type relicType) =>
            owner.Relics.Any(relic => relic.GetType() == relicType);
    }
}
