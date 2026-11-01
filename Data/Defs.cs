using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NorvinskMaps.Data
{
    public class BoundingRectangle
    {
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }
    }

    public class BoundingRectangularSolid
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
    }

    public class MapLayerDef
    {
        public int Level { get; set; }

        public string ImagePath { get; set; }

        public int TesselationIndex { get; set; }

        public BoundingRectangle ImageBounds { get; set; }

        public List<BoundingRectangularSolid> GameBounds { get; set; }
    }

    public class MapMarkerDef
    {
        public string Text { get; set; } = "";

        public string ImagePath { get; set; }

        public Vector3 Position { get; set; }

        public Sprite Sprite { get; set; }
        public bool ShowInRaid { get; set; } = true;
        public string Category { get; set; } = "None";
        public Color Color { get; set; } = Color.white;
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public string AssociatedItemId { get; set; } = "";
    }

    public class MapLabelDef
    {
        public string Text { get; set; }

        public Vector3 Position { get; set; }

        public float FontSize { get; set; } = 14f;
        public float DegreesRotation { get; set; } = 0f;
        public Color Color { get; set; } = Color.white;
        public string Category { get; set; } = "None";
    }

    public class MapDef
    {
        public string DisplayName { get; set; }

        public BoundingRectangle Bounds { get; set; }

        public Dictionary<string, MapLayerDef> Layers { get; set; } = new Dictionary<string, MapLayerDef>();

        public List<string> MapInternalNames { get; set; } = new List<string>();
        public float CoordinateRotation { get; set; } = 0;
        public List<MapLabelDef> Labels { get; set; } = new List<MapLabelDef>();
        public List<MapMarkerDef> StaticMarkers { get; set; } = new List<MapMarkerDef>();
        public int DefaultLevel { get; set; } = 0;

        public string Author { get; set; }
        public string AuthorLink { get; set; }

        public static MapDef LoadFromPath(string absolutePath)
        {
            try
            {
                var json = File.ReadAllText(absolutePath);
                var root = JObject.Parse(json);

                var mapDef = new MapDef
                {
                    DisplayName        = GetString(root["DisplayName"]) ?? "",
                    Author             = GetString(root["Author"]),
                    AuthorLink         = GetString(root["AuthorLink"]),
                    CoordinateRotation = GetFloat(root["CoordinateRotation"]) ?? 0f,
                    DefaultLevel       = GetInt(root["DefaultLevel"]) ?? 0,
                    Bounds             = ParseRect(root["Bounds"]),
                    Layers             = new Dictionary<string, MapLayerDef>(),
                    MapInternalNames   = new List<string>(),
                    Labels             = new List<MapLabelDef>(),
                    StaticMarkers      = new List<MapMarkerDef>(),
                };

                // ------------------------------------------------------------
                // MapInternalNames
                // ------------------------------------------------------------
                var namesToken = root["MapInternalNames"];
                if (namesToken != null && namesToken.Type == JTokenType.Array)
                {
                    foreach (var n in namesToken.Children())
                    {
                        var s = GetString(n);
                        if (!string.IsNullOrEmpty(s))
                            mapDef.MapInternalNames.Add(s);
                    }
                }

                // ------------------------------------------------------------
                // Layers
                // ------------------------------------------------------------
                var layersToken = root["Layers"];
                if (layersToken != null && layersToken.Type == JTokenType.Object)
                {
                    foreach (var prop in layersToken.Children())
                    {
                        if (prop == null || prop.Type != JTokenType.Property) continue;

                        // Значение JProperty — её единственный ребёнок.
                        var layerValue = prop.First;
                        if (layerValue == null || layerValue.Type != JTokenType.Object) continue;

                        // Имя ключа — последний сегмент Path ("Layers.Underground" → "Underground").
                        string keyName = ExtractLastPathSegment(prop.Path);
                        if (string.IsNullOrEmpty(keyName)) continue;

                        var layer = new MapLayerDef
                        {
                            Level            = GetInt(layerValue["Level"]) ?? 0,
                            ImagePath        = GetString(layerValue["ImagePath"]),
                            TesselationIndex = GetInt(layerValue["TesselationIndex"]) ?? 0,
                            ImageBounds      = ParseRect(layerValue["ImageBounds"]),
                            GameBounds       = new List<BoundingRectangularSolid>(),
                        };

                        var gbsToken = layerValue["GameBounds"];
                        if (gbsToken != null && gbsToken.Type == JTokenType.Array)
                        {
                            foreach (var g in gbsToken.Children())
                            {
                                if (g == null || g.Type != JTokenType.Object) continue;

                                layer.GameBounds.Add(new BoundingRectangularSolid
                                {
                                    Min = ParseVec3(g["Min"]),
                                    Max = ParseVec3(g["Max"]),
                                });
                            }
                        }

                        mapDef.Layers[keyName] = layer;
                    }
                }

                // ------------------------------------------------------------
                // Labels
                // ------------------------------------------------------------
                var labelsToken = root["Labels"];
                if (labelsToken != null && labelsToken.Type == JTokenType.Array)
                {
                    foreach (var lb in labelsToken.Children())
                    {
                        if (lb == null || lb.Type != JTokenType.Object) continue;

                        mapDef.Labels.Add(new MapLabelDef
                        {
                            Text            = GetString(lb["Text"]),
                            Position        = ParseVec3(lb["Position"]),
                            FontSize        = GetFloat(lb["FontSize"]) ?? 14f,
                            DegreesRotation = GetFloat(lb["DegreesRotation"]) ?? 0f,
                            Color           = ParseColor(lb["Color"]),
                            Category        = GetString(lb["Category"]) ?? "None",
                        });
                    }
                }

                // ------------------------------------------------------------
                // StaticMarkers
                // ------------------------------------------------------------
                var markersToken = root["StaticMarkers"];
                if (markersToken != null && markersToken.Type == JTokenType.Array)
                {
                    foreach (var m in markersToken.Children())
                    {
                        if (m == null || m.Type != JTokenType.Object) continue;

                        mapDef.StaticMarkers.Add(new MapMarkerDef
                        {
                            Text             = GetString(m["Text"]) ?? "",
                            ImagePath        = GetString(m["ImagePath"]),
                            Position         = ParseVec3(m["Position"]),
                            ShowInRaid       = GetBool(m["ShowInRaid"]) ?? true,
                            Category         = GetString(m["Category"]) ?? "None",
                            Color            = ParseColor(m["Color"]),
                            Pivot            = ParseVec2(m["Pivot"]),
                            AssociatedItemId = GetString(m["AssociatedItemId"]) ?? "",
                        });
                    }
                }

                return mapDef;
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"Loading MapMappingDef failed from json at path: {absolutePath}");
                NorvinskMapsPlugin.Log.LogError($"Exception given was: {e.Message}");
                NorvinskMapsPlugin.Log.LogError($"{e.StackTrace}");
                return null;
            }
        }
        private static bool IsNullish(JToken t)
            => t == null || t.Type == JTokenType.Null;

        private static string GetString(JToken t)
        {
            if (IsNullish(t)) return null;
            return (string)t;
        }

        private static int? GetInt(JToken t)
        {
            if (IsNullish(t)) return null;
            return (int?)t;
        }

        private static float? GetFloat(JToken t)
        {
            if (IsNullish(t)) return null;
            return (float?)t;
        }

        private static bool? GetBool(JToken t)
        {
            if (IsNullish(t)) return null;
            return (bool?)t;
        }

        private static float GetFloatAt(JToken arr, int index, float fallback)
        {
            if (arr == null) return fallback;
            var child = arr[index];
            if (IsNullish(child)) return fallback;
            return (float)child;
        }
        private static string ExtractLastPathSegment(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            int idx = path.LastIndexOf('.');
            return idx >= 0 ? path.Substring(idx + 1) : path;
        }

        private static BoundingRectangle ParseRect(JToken t)
        {
            if (t == null || t.Type != JTokenType.Object) return null;

            return new BoundingRectangle
            {
                Min = ParseVec2(t["Min"]),
                Max = ParseVec2(t["Max"]),
            };
        }

        private static Vector2 ParseVec2(JToken t)
        {
            if (t == null) return Vector2.zero;

            if (t.Type == JTokenType.Array)
            {
                return new Vector2(
                    GetFloatAt(t, 0, 0f),
                    GetFloatAt(t, 1, 0f));
            }

            return new Vector2(
                GetFloat(t["x"]) ?? GetFloat(t["X"]) ?? 0f,
                GetFloat(t["y"]) ?? GetFloat(t["Y"]) ?? 0f);
        }

        private static Vector3 ParseVec3(JToken t)
        {
            if (t == null) return Vector3.zero;

            if (t.Type == JTokenType.Array)
            {
                return new Vector3(
                    GetFloatAt(t, 0, 0f),
                    GetFloatAt(t, 1, 0f),
                    GetFloatAt(t, 2, 0f));
            }

            return new Vector3(
                GetFloat(t["x"]) ?? GetFloat(t["X"]) ?? 0f,
                GetFloat(t["y"]) ?? GetFloat(t["Y"]) ?? 0f,
                GetFloat(t["z"]) ?? GetFloat(t["Z"]) ?? 0f);
        }

        private static Color ParseColor(JToken t)
        {
            if (t == null) return Color.white;

            if (t.Type == JTokenType.Array)
            {
                return new Color(
                    GetFloatAt(t, 0, 1f),
                    GetFloatAt(t, 1, 1f),
                    GetFloatAt(t, 2, 1f),
                    GetFloatAt(t, 3, 1f));
            }

            return new Color(
                GetFloat(t["r"]) ?? 1f,
                GetFloat(t["g"]) ?? 1f,
                GetFloat(t["b"]) ?? 1f,
                GetFloat(t["a"]) ?? 1f);
        }
    }
}