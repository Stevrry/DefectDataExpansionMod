using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events;
using STS2RitsuLib.Patching.Models;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Patches
{
    /// <summary>
    /// 「女巫之屋」那场精英战的奖励是**替换**而不是追加：
    /// 打完之后不发这个房间原本的（金币 / 卡牌 / 药水 / 遗物），只留下事件挂上去的那两次
    /// 「来自其他角色的卡牌奖励」。
    ///
    /// 为什么不改遭遇本身：引擎里唯一控制「这场战斗发不发常规奖励」的开关是
    /// <see cref="EncounterModel.ShouldGiveRewards"/>，而它是按遭遇类型写死的
    /// （原版只有「饱经风霜的假人」那几个遭遇覆盖成 false）。我们借用的是当前阶段真正的精英遭遇，
    /// 动不了它，所以改成在「组装奖励集合」这一步把常规奖励换掉。
    ///
    /// 判定条件是战斗房间的 <see cref="CombatRoom.ParentEventId"/>（由 EventCombatSynchronizer
    /// 在转场时填的本事件 Id），所以只对女巫之屋开的那场战斗生效，不影响其他任何房间。
    /// </summary>
    public sealed class WitchsHouseRewardPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_witchs_house_rewards";

        public static string Description => "女巫之屋的精英战不发常规奖励，只发事件给的其他角色卡牌奖励";

        // 补丁失效只是退回成「常规奖励 + 额外奖励」，不该让整个模组失效
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<RewardsSet>(nameof(RewardsSet.WithRewardsFromRoom))
        ];

        /// <summary>
        /// 接管房间奖励的组装。返回 false 表示跳过原版实现（也就是不生成这个房间的常规奖励）。
        /// </summary>
        public static bool Prefix(RewardsSet __instance, AbstractRoom room, ref RewardsSet __result)
        {
            if (room is not CombatRoom { ParentEventId: { } parentEventId } combatRoom)
            {
                return true;
            }

            // 用 Id 反查而不是 ModelDb.Event&lt;TheWitchsHouse&gt;()：查不到时返回 null 而不是抛异常，
            // 在补丁里更安全。
            if (ModelDb.GetByIdOrNull<EventModel>(parentEventId) is not TheWitchsHouse)
            {
                return true;
            }

            // EmptyForRoom 会把这个集合和房间绑定（奖励界面因此是带「继续」的终局界面），
            // 然后只把我们挂在这场战斗上的额外奖励塞进去。
            __instance.EmptyForRoom(room);
            if (combatRoom.ExtraRewards.TryGetValue(__instance.Player, out List<Reward>? extraRewards))
            {
                __instance.WithCustomRewards(extraRewards);
            }

            __result = __instance;
            return false;
        }
    }
}
