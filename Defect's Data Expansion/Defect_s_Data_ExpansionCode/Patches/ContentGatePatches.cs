using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
using System.Reflection;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Patches
{
    /// <summary>
    /// 「数据扩展：关闭」时的内容隔离。
    /// 遗物不走这里：本模组的遗物各自在自己的 IsAllowed 里判断
    /// </summary>
    public sealed class ContentGateCardsPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_content_gate_cards";

        public static string Description => "数据扩展打开时，把本模组的卡牌从卡池查询结果里筛掉";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<CardPoolModel>(nameof(CardPoolModel.GetUnlockedCards))
        ];

        public static IEnumerable<CardModel> Postfix(IEnumerable<CardModel> __result)
        {
            // 标了 [DisabledCard] 的卡：不管是奖励、商店、事件还是战斗内生成，都不进卡池查询结果。
            // 这条和总开关无关，始终生效。
            __result = __result.Where(card => !card.GetType().IsDefined(typeof(DisabledCardAttribute), false));

            if (ExpansionConfig.EffectiveEnableExpansion)
            {
                return __result;
            }

            // 只筛掉本程序集里的卡，不影响原版和其他模组的卡
            Assembly modAssembly = typeof(MainFile).Assembly;
            return __result.Where(card => card.GetType().Assembly != modAssembly);
        }
    }

    /// <summary>
    /// 「数据扩展：开启」时，开局直接给鸡煲发一个事件遗物「扩容磁盘」。
    /// </summary>
    public sealed class ExpansionDiskGrantPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_grant_expansion_disk";

        public static string Description => "开启数据扩展时，开局给故障机器人塞一个事件遗物「扩容磁盘」";

        // 这个补丁失败只是少一个遗物，不该让整个模组失效
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<Player>("PopulateStartingRelics")
        ];

        /// <summary>
        /// 加入遗物“扩容磁盘”
        /// </summary>
        public static void Postfix(Player __instance)
        {
            if (!ExpansionConfig.EffectiveEnableExpansion || __instance.Character is not Defect)
            {
                return;
            }

            RelicModel model = ModelDb.Relic<ExpansionDisk>();

            // 兜底：万一这个方法被调用两次
            if (__instance.Relics.Any(relic => relic.Id.Equals(model.Id)))
            {
                return;
            }

            RelicModel granted = model.ToMutable();
            granted.FloorAddedToDeck = 1;                       // 与起始遗物一致
            SaveManager.Instance.MarkRelicAsSeen(granted);      // 图鉴里记为已获得

            __instance.AddRelicInternal(granted);
        }
    }
}
