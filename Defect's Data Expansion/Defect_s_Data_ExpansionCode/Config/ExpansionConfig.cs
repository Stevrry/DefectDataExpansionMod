using System;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config
{
    /// <summary>
    /// 落盘的模组设置。保存成 class 而不是裸 bool，
    /// 以后加字段不需要换存储槽（见 RitsuLib 的持久化指南）。
    /// </summary>
    public sealed class ExpansionSettings
    {
        /// <summary>是否启用「鸡煲：数据扩展」的内容。</summary>
        public bool EnableExpansion { get; set; } = true;
    }

    /// <summary>
    /// 模组总开关的读写入口。
    ///
    /// 存储由 RitsuLib 的 <see cref="ModDataStore"/> 负责：槽位 key = settings，
    /// 文件 = expansion_settings.json，作用域 <see cref="SaveScope.Global"/>（所有存档位共享）。
    ///
    /// 开启：本模组的卡牌与遗物正常进入对局，且「故障机器人」开局额外获得起始遗物「扩容磁盘」。
    /// 关闭：本模组内容不会在本局游戏中出现（卡池过滤 + 遗物 IsAllowed），也不发放扩容磁盘。
    ///
    /// 开关的界面在选人界面（见 Patches/CharacterSelectTogglePatch）。
    /// </summary>
    public static class ExpansionConfig
    {
        private const string Key = "settings";

        private const string FileName = "expansion_settings.json";

        private static ModDataStoreCache<ExpansionSettings>? _cache;

        /// <summary>在 Mod 初始化入口调用一次。必须先注册数据，再取缓存。</summary>
        public static void Register(string modId)
        {
            using (RitsuLibFramework.BeginModDataRegistration(modId))
            {
                RitsuLibFramework.GetDataStore(modId).Register(
                    key: Key,
                    fileName: FileName,
                    scope: SaveScope.Global,
                    defaultFactory: () => new ExpansionSettings(),
                    autoCreateIfMissing: true);
            }

            _cache = RitsuLibFramework.GetDataStore(modId).CreateCache<ExpansionSettings>(Key);
        }

        /// <summary>是否启用本模组的内容。数据还没就绪时按「开启」处理。</summary>
        public static bool EnableExpansion
        {
            get
            {
                try
                {
                    return _cache?.Value.EnableExpansion ?? true;
                }
                catch (Exception)
                {
                    // 档案还没加载完（或已被卸载）时，不要因为读设置而炸掉调用方
                    return true;
                }
            }
            set
            {
                ModDataStore? store = TryGetStore();
                if (store == null)
                {
                    return;
                }

                store.Modify<ExpansionSettings>(Key, settings => settings.EnableExpansion = value);
                store.Save(Key);
            }
        }

        private static ModDataStore? TryGetStore()
        {
            if (_cache == null)
            {
                return null;
            }

            try
            {
                return RitsuLibFramework.GetDataStore(MainFile.ModId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
