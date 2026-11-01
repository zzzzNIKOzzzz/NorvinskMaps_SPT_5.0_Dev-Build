using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NorvinskMaps;
using NorvinskMaps.Config;

namespace NorvinskMaps.Config
{
    internal class DmServerConfig
    {
        public bool AllowShowFriendlyPlayerMarkersInRaid { get; set; } = true;
        public bool AllowShowEnemyPlayerMarkersInRaid { get; set; } = true;
        public bool AllowShowBossMarkersInRaid { get; set; } = true;
        public bool AllowShowScavMarkersInRaid { get; set; } = true;
        public string ForcedLocale { get; set; } = "";
    }

    internal static class ServerConfigLoader
    {
        private const string ServerUrl = "https://127.0.0.1:6969/NorvinskMaps/load";
        private static bool _attempted;
        private static SynchronizationContext _mainThread;

        public static void CaptureMainThread()
        {
            _mainThread = SynchronizationContext.Current;
        }

        public static void LoadAsync()
        {
            if (_attempted) return;
            _attempted = true;

            Task.Run(() =>
            {
                try
                {
                    using var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };
                    using var client = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(5)
                    };

                    var json = client.GetStringAsync(ServerUrl).GetAwaiter().GetResult();

                    var cfg = new DmServerConfig
                    {
                        AllowShowFriendlyPlayerMarkersInRaid = ExtractBool(json, "AllowShowFriendlyPlayerMarkersInRaid", true),
                        AllowShowEnemyPlayerMarkersInRaid    = ExtractBool(json, "AllowShowEnemyPlayerMarkersInRaid",    true),
                        AllowShowBossMarkersInRaid           = ExtractBool(json, "AllowShowBossMarkersInRaid",           true),
                        AllowShowScavMarkersInRaid           = ExtractBool(json, "AllowShowScavMarkersInRaid",           true),
                        ForcedLocale                         = ExtractString(json, "ForcedLocale", ""),
                    };

                    if (_mainThread == null)
                    {
                        ApplyConfig(cfg);
                        return;
                    }

                    _mainThread.Post(_ => ApplyConfig(cfg), null);
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning(
                        $"[ServerConfig] load failed: {e.GetType().Name}: {e.Message}");
                }
            });
        }

        private static bool ExtractBool(string json, string key, bool defaultValue)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;

            var idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return defaultValue;

            var colon = json.IndexOf(':', idx);
            if (colon < 0) return defaultValue;

            var rest = json.Substring(colon + 1).TrimStart();
            if (rest.StartsWith("true",  StringComparison.OrdinalIgnoreCase)) return true;
            if (rest.StartsWith("false", StringComparison.OrdinalIgnoreCase)) return false;

            return defaultValue;
        }

        private static string ExtractString(string json, string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;

            var idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return defaultValue;

            var colon = json.IndexOf(':', idx);
            if (colon < 0) return defaultValue;

            var rest = json.Substring(colon + 1).TrimStart();
            if (!rest.StartsWith("\"")) return defaultValue;

            var end = rest.IndexOf('"', 1);
            if (end < 0) return defaultValue;

            return rest.Substring(1, end - 1);
        }

        private static void ApplyConfig(DmServerConfig cfg)
        {
            try
            {
                Settings.ShowFriendlyPlayerMarkersInRaid.Value = cfg.AllowShowFriendlyPlayerMarkersInRaid;
                Settings.ShowEnemyPlayerMarkersInRaid.Value    = cfg.AllowShowEnemyPlayerMarkersInRaid;
                Settings.ShowBossMarkersInRaid.Value           = cfg.AllowShowBossMarkersInRaid;
                Settings.ShowScavMarkersInRaid.Value           = cfg.AllowShowScavMarkersInRaid;

                if (!string.IsNullOrEmpty(cfg.ForcedLocale))
                {
                    Localization.SetLanguage(cfg.ForcedLocale);
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    $"[ServerConfig] apply failed: {e.GetType().Name}: {e.Message}");
            }
        }
    }
}