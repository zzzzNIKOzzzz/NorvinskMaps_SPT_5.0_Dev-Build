using Comfort.Common;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace NorvinskMaps.Utils
{
    public static class DumpUtils
    {
        private const string ExtractCategory = "Extract";
        private const string ExtractImagePath = "Markers/exit.png";

        private const string SecretCategory = "Secret";
        private const string SecretImagePath = "Markers/exit.png";

        private const string TransitCategory = "Transit";
        private const string TransitImagePath = "Markers/transit.png";

        private static readonly Color ExtractScavColor = Color.Lerp(Color.yellow, Color.red, 0.5f);
        private static readonly Color TransitColor = Color.Lerp(Color.yellow, Color.red, 0.6f);
        private static readonly Color SecretColor = new Color(0.1f, 0.6f, 0.6f);
        private static readonly Color ExtractPmcColor = Color.green;

        private const string SwitchCategory = "Switch";
        private const string SwitchImagePath = "Markers/lever.png";

        private const string LockedDoorCategory = "Locked Door";
        private const string LockedDoorImagePath = "Markers/door_with_lock.png";
        private static readonly Color LockedDoorColor = Color.yellow;

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static void DumpExtracts()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.ExfiltrationController == null)
            {
                NorvinskMapsPlugin.Log.LogWarning("[DumpExtracts] gameWorld or ExfiltrationController is null");
                return;
            }

            List<MapMarkerDef> dump;
            string filePath;

            try
            {
                var scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;
                var pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;
                var secretExfils = gameWorld.ExfiltrationController.SecretExfiltrationPoints;

                dump = new List<MapMarkerDef>();

                if (scavExfils != null)
                {
                    foreach (var scavExfil in scavExfils)
                    {
                        if (scavExfil == null) continue;
                        try
                        {
                            dump.Add(new MapMarkerDef
                            {
                                Category = ExtractCategory,
                                ShowInRaid = false,
                                ImagePath = ExtractImagePath,
                                Text = scavExfil.Settings.Name.BSGLocalized(),
                                Position = MathUtils.ConvertToMapPosition(scavExfil.transform),
                                Color = ExtractScavColor
                            });
                        }
                        catch (Exception e)
                        {
                            NorvinskMapsPlugin.Log.LogWarning($"[DumpExtracts] scavExfil skip: {e.Message}");
                        }
                    }
                }

                if (pmcExfils != null)
                {
                    foreach (var pmcExfil in pmcExfils)
                    {
                        if (pmcExfil == null) continue;
                        try
                        {
                            dump.Add(new MapMarkerDef
                            {
                                Category = ExtractCategory,
                                ShowInRaid = false,
                                ImagePath = ExtractImagePath,
                                Text = pmcExfil.Settings.Name.BSGLocalized(),
                                Position = MathUtils.ConvertToMapPosition(pmcExfil.transform),
                                Color = ExtractPmcColor
                            });
                        }
                        catch (Exception e)
                        {
                            NorvinskMapsPlugin.Log.LogWarning($"[DumpExtracts] pmcExfil skip: {e.Message}");
                        }
                    }
                }

                try
                {
                    foreach (var transit in LocationScene.GetAllObjects<TransitPoint>(true))
                    {
                        if (transit == null) continue;
                        dump.Add(new MapMarkerDef
                        {
                            Category = TransitCategory,
                            ShowInRaid = false,
                            ImagePath = TransitImagePath,
                            Text = transit.Description.BSGLocalized(),
                            Position = MathUtils.ConvertToMapPosition(transit.transform),
                            Color = TransitColor
                        });
                    }
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning($"[DumpExtracts] transit skip: {e.Message}");
                }

                if (secretExfils != null)
                {
                    foreach (var secret in secretExfils)
                    {
                        if (secret == null) continue;
                        try
                        {
                            dump.Add(new MapMarkerDef
                            {
                                Category = SecretCategory,
                                ShowInRaid = false,
                                ImagePath = SecretImagePath,
                                Text = secret.Settings.Name.BSGLocalized(),
                                Position = MathUtils.ConvertToMapPosition(secret.transform),
                                Color = SecretColor
                            });
                        }
                        catch (Exception e)
                        {
                            NorvinskMapsPlugin.Log.LogWarning($"[DumpExtracts] secret skip: {e.Message}");
                        }
                    }
                }

                var mapName = GameUtils.GetCurrentMapInternalName() ?? "unknown";
                filePath = Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-extracts.json");
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[DumpExtracts] Сбор данных: {e.Message}\n{e.StackTrace}");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(dump, JsonOpts);
                    File.WriteAllText(filePath, json);
                    NorvinskMapsPlugin.Log.LogInfo($"[DumpExtracts] OK → {filePath} ({dump.Count} markers)");
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[DumpExtracts] FAIL: {e.Message}");
                }
            });
        }

        public static void DumpSwitches()
        {
            List<MapMarkerDef> dump;
            string filePath;

            try
            {
                var switches = GameObject.FindObjectsOfType<Switch>();
                dump = new List<MapMarkerDef>();

                if (switches != null)
                {
                    foreach (var @switch in switches)
                    {
                        if (@switch == null) continue;
                        try
                        {
                            if (!@switch.Operatable || !@switch.HasAuthority) continue;
                            dump.Add(new MapMarkerDef
                            {
                                Category = SwitchCategory,
                                ImagePath = SwitchImagePath,
                                Text = @switch.name,
                                Position = MathUtils.ConvertToMapPosition(@switch.transform)
                            });
                        }
                        catch (Exception e)
                        {
                            NorvinskMapsPlugin.Log.LogWarning($"[DumpSwitches] skip: {e.Message}");
                        }
                    }
                }

                var mapName = GameUtils.GetCurrentMapInternalName() ?? "unknown";
                filePath = Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-switches.json");
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[DumpSwitches] Сбор данных: {e.Message}\n{e.StackTrace}");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(dump, JsonOpts);
                    File.WriteAllText(filePath, json);
                    NorvinskMapsPlugin.Log.LogInfo($"[DumpSwitches] OK → {filePath} ({dump.Count} switches)");
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[DumpSwitches] FAIL: {e.Message}");
                }
            });
        }

        public static void DumpLocks()
        {
            List<MapMarkerDef> dump;
            string filePath;

            try
            {
                var objects = GameObject.FindObjectsOfType<WorldInteractiveObject>();
                dump = new List<MapMarkerDef>();
                var i = 1;

                if (objects != null)
                {
                    foreach (var locked in objects)
                    {
                        if (locked == null) continue;
                        try
                        {
                            if (string.IsNullOrEmpty(locked.KeyId) || !locked.Operatable) continue;
                            dump.Add(new MapMarkerDef
                            {
                                Text = $"door {i++}",
                                Category = LockedDoorCategory,
                                ImagePath = LockedDoorImagePath,
                                Position = MathUtils.ConvertToMapPosition(locked.transform),
                                AssociatedItemId = locked.KeyId,
                                Color = LockedDoorColor
                            });
                        }
                        catch (Exception e)
                        {
                            NorvinskMapsPlugin.Log.LogWarning($"[DumpLocks] skip: {e.Message}");
                        }
                    }
                }

                var mapName = GameUtils.GetCurrentMapInternalName() ?? "unknown";
                filePath = Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-locked.json");
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[DumpLocks] Сбор данных: {e.Message}\n{e.StackTrace}");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(dump, JsonOpts);
                    File.WriteAllText(filePath, json);
                    NorvinskMapsPlugin.Log.LogInfo($"[DumpLocks] OK → {filePath} ({dump.Count} locked doors)");
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[DumpLocks] FAIL: {e.Message}");
                }
            });
        }
    }
}