using Comfort.Common;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
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

        public static void DumpExtracts()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;
            var pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;
            var secretExfils = gameWorld.ExfiltrationController.SecretExfiltrationPoints;

            var dump = new List<MapMarkerDef>();

            if (scavExfils is not null)
            {
                foreach (var scavExfil in scavExfils)
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
            }

            foreach (var pmcExfil in pmcExfils)
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

            foreach (var transit in LocationScene.GetAllObjects<TransitPoint>(true))
            {
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

            if (secretExfils is not null)
            {
                foreach (var secret in secretExfils)
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
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-extracts.json"), dumpString);
        }

        public static void DumpSwitches()
        {
            var switches = GameObject.FindObjectsOfType<Switch>();
            var dump = new List<MapMarkerDef>();

            foreach (var @switch in switches)
            {
                if (!@switch.Operatable || !@switch.HasAuthority)
                {
                    continue;
                }

                dump.Add(new MapMarkerDef
                {
                    Category = SwitchCategory,
                    ImagePath = SwitchImagePath,
                    Text = @switch.name,
                    Position = MathUtils.ConvertToMapPosition(@switch.transform)
                });
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-switches.json"), dumpString);
        }

        public static void DumpLocks()
        {
            var objects = GameObject.FindObjectsOfType<WorldInteractiveObject>();
            var dump = new List<MapMarkerDef>();
            var i = 1;

            foreach (var locked in objects)
            {
                if (string.IsNullOrEmpty(locked.KeyId) || !locked.Operatable)
                {
                    continue;
                }

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

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(NorvinskMapsPlugin.Path, $"{mapName}-locked.json"), dumpString);
        }
    }
}