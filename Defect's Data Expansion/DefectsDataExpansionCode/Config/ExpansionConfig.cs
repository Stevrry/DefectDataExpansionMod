using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Config
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
    /// 开关在三个地方出现：选人界面右上角、RitsuLib 原生设置页。两处读写的是同一份
    /// <see cref="EnableExpansion"/>。多人游戏里客户端不参与决定：主机通过 RitsuLib 的
    /// Sidecar 配置同步把值广播下来，客户端用 <see cref="EffectiveEnableExpansion"/> 跟随。
    /// </summary>
    public static class ExpansionConfig
    {
        private const string Key = "settings";

        private const string FileName = "expansion_settings.json";

        private static ModDataStoreCache<ExpansionSettings>? _cache;

        /// <summary>
        /// 主机同步下来的值。null = 不在联机客户端里（单人 / 主机自己），用自己的本地设置。
        /// </summary>
        private static bool? _hostValue;

        /// <summary>开关值变化时触发；界面（选人界面开关行、设置页）据此刷新。</summary>
        public static event Action? Changed;

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

        /// <summary>本地设置（主机 / 单人自己用的那份值）。</summary>
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

                Changed?.Invoke();
            }
        }

        /// <summary>
        /// 本局真正生效的值：联机客户端用主机同步下来的值，其他情况用本地设置。
        /// 卡牌 / 遗物 / 事件的内容隔离都读这个，而不是 <see cref="EnableExpansion"/>。
        /// </summary>
        public static bool EffectiveEnableExpansion => _hostValue ?? EnableExpansion;

        /// <summary>当前是否正在跟随主机（多人游戏的客户端，且已收到主机设置）。</summary>
        public static bool IsFollowingHost => _hostValue.HasValue;

        /// <summary>
        /// 由网络同步层写入主机的值；传 null 表示离开联机，回到本地设置。
        /// 可能在非主线程被调用，所以界面通知统一延迟到主线程。
        /// </summary>
        public static void SetHostValue(bool? value)
        {
            if (_hostValue == value)
            {
                return;
            }

            _hostValue = value;

            // Sidecar 的接收回调不保证在 Godot 主线程，界面刷新必须回主线程做
            Callable.From(() => Changed?.Invoke()).CallDeferred();
        }

        /// <summary>把当前值落盘（供 RitsuLib 设置页的绑定在写入后调用）。</summary>
        public static void Save()
        {
            TryGetStore()?.Save(Key);
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
