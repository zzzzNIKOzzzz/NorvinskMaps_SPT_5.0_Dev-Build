using EFT.UI;
using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NorvinskMaps.UI.Controls
{
    public class MapSelectDropdown : MonoBehaviour
    {
        private static readonly HashSet<string> AcceptableExtensions = new HashSet<string> { "json", "jsonc" };

        [HideFromIl2Cpp]
        public event Action<MapDef> OnMapSelected;
        public RectTransform RectTransform => gameObject.transform as RectTransform;
        private DropDownBox _dropdown;
        private readonly Dictionary<string, DateTime> _writeTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, MapDef> _mapDefs = new Dictionary<string, MapDef>();
        private List<MapDef> _selectableMapDefs;
        private string _nameFilter = null;
        private int _lastSelectedValue = int.MinValue;
        private bool _trackingEnabled = false;
        private bool _loadedOnce = false;
        private float _pollTimer;
        private System.Reflection.PropertyInfo _valueProp = null;
        private System.Reflection.FieldInfo _valueField = null;

        public static MapSelectDropdown Create(GameObject prefab, Transform parent)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "MapSelectDropdown";

            var rectTransform = go.GetRectTransform();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;

            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(360f, 31f);

            var dropdown = go.AddComponent<MapSelectDropdown>();
            return dropdown;
        }

        private void Awake()
        {
            _dropdown = gameObject.GetComponentInChildren<DropDownBox>();
            _dropdown.SetLabelText(Localization.T("SelectMap"));

            SetupDropdownValueTracking();
        }

        [HideFromIl2Cpp]
        private void SetupDropdownValueTracking()
        {
            if (_dropdown == null) return;

            var t = _dropdown.GetType();
            var flags = System.Reflection.BindingFlags.Public
                      | System.Reflection.BindingFlags.NonPublic
                      | System.Reflection.BindingFlags.Instance;

            string[] candidateNames = {
                "CurrentIndex", "_CurrentIndex_k__BackingField",
                "CurrentValue", "SelectedValue", "Value",
                "SelectedIndex", "Index", "_currentValue",
                "_value", "_selectedIndex"
            };

            foreach (var n in candidateNames)
            {
                var p = t.GetProperty(n, flags);
                if (p != null && p.CanRead) { _valueProp = p; break; }
            }

            if (_valueProp == null)
            {
                foreach (var n in candidateNames)
                {
                    var f = t.GetField(n, flags);
                    if (f != null) { _valueField = f; break; }
                }
            }

            _trackingEnabled = (_valueProp != null || _valueField != null);
            if (_trackingEnabled) _lastSelectedValue = ReadDropdownValue();
        }

        [HideFromIl2Cpp]
        private int ReadDropdownValue()
        {
            try
            {
                object v = _valueProp != null
                    ? _valueProp.GetValue(_dropdown)
                    : _valueField?.GetValue(_dropdown);

                if (v == null) return _lastSelectedValue;
                return System.Convert.ToInt32(v);
            }
            catch { return _lastSelectedValue; }
        }

        private void Update()
        {
            if (!_trackingEnabled || _dropdown == null) return;

            _pollTimer -= UnityEngine.Time.deltaTime;
            if (_pollTimer > 0f) return;
            _pollTimer = 0.1f;

            int v = ReadDropdownValue();
            if (v != _lastSelectedValue)
            {
                _lastSelectedValue = v;

                if (v >= 0 && _selectableMapDefs != null && v < _selectableMapDefs.Count)
                {
                    OnMapSelected?.Invoke(_selectableMapDefs[v]);
                }
            }
        }

        [HideFromIl2Cpp]
        public IEnumerable<MapDef> GetMapDefs() => _mapDefs.Values;

        [HideFromIl2Cpp]
        public void LoadFirstAvailableMap()
        {
            if (_selectableMapDefs == null || _selectableMapDefs.Count == 0) return;
            OnMapSelected?.Invoke(_selectableMapDefs[0]);
        }

        [HideFromIl2Cpp]
        public void FilterByInternalMapName(string internalMapName)
        {
            if (_nameFilter == internalMapName
                && _selectableMapDefs != null
                && _selectableMapDefs.Count > 0)
            {
                return;
            }

            _nameFilter = internalMapName;
            if (FilteredMapDefs().FirstOrDefault() == null)
            {
                _nameFilter = "";
                NorvinskMapsPlugin.Log.LogWarning($"Cannot filter by {internalMapName}");
            }

            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        [HideFromIl2Cpp]
        public void ClearFilter()
        {
            if (_nameFilter == null
                && _selectableMapDefs != null
                && _selectableMapDefs.Count > 0)
            {
                return;
            }

            _nameFilter = null;
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        [HideFromIl2Cpp]
        public void OnLoadMap(MapDef mapDef)
        {
            if (mapDef == null || _selectableMapDefs == null) return;

            var index = _selectableMapDefs.IndexOf(mapDef);
            if (index < 0) return;
            SetDropdownIndex(index);
        }

        [HideFromIl2Cpp]
        private void SetDropdownIndex(int index)
        {
            if (_dropdown == null) return;

            try
            {
                var minEmpty = new Il2CppSystem.Nullable<int>();
                var maxEmpty = new Il2CppSystem.Nullable<int>();
                _dropdown.UpdateValue(index, false, minEmpty, maxEmpty, false);
                _lastSelectedValue = index;
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[MapSelectDropdown] SetDropdownIndex: {e.Message}");
            }
        }

        [HideFromIl2Cpp]
        public bool IsDropdownOpen()
        {
            if (_dropdown == null) return false;
            try { return Traverse.Create(_dropdown).Property<bool>("CurrentState").Value; }
            catch { return false; }
        }

        [HideFromIl2Cpp]
        public void TryCloseDropdown()
        {
            if (_dropdown == null) return;
            try { _dropdown.Close(); } catch { }
        }

        [HideFromIl2Cpp]
        public void LoadMapDefsFromPath(string relPath)
        {
            if (_loadedOnce) return;
            _loadedOnce = true;

            try
            {
                var absolutePath = Path.Combine(NorvinskMapsPlugin.Path, relPath);

                if (!Directory.Exists(absolutePath))
                {
                    NorvinskMapsPlugin.Log.LogError($"[MapSelectDropdown] Directory not found: {absolutePath}");
                    return;
                }

                var paths = Directory.EnumerateFiles(absolutePath, "*.*", SearchOption.AllDirectories)
                    .Where(p => AcceptableExtensions.Contains(Path.GetExtension(p).TrimStart('.').ToLowerInvariant()))
                    .ToList();

                foreach (var path in paths)
                {
                    string tempPath = null;
                    try
                    {
                        var raw = File.ReadAllText(path);
                        var clean = StripJsonComments(raw);

                        tempPath = Path.Combine(
                            Path.GetDirectoryName(path),
                            Path.GetFileNameWithoutExtension(path) + ".__stripped.tmp");

                        File.WriteAllText(tempPath, clean, new System.Text.UTF8Encoding(false));

                        var mapDef = MapDef.LoadFromPath(tempPath);
                        if (mapDef == null || (mapDef.Layers?.Count ?? 0) == 0) continue;

                        _mapDefs[path] = mapDef;
                    }
                    catch (Exception e)
                    {
                        NorvinskMapsPlugin.Log.LogError($"[MapSelectDropdown] Parse failed: {path}: {e.Message}");
                    }
                    finally
                    {
                        if (tempPath != null && File.Exists(tempPath))
                        {
                            try { File.Delete(tempPath); } catch { }
                        }
                    }
                }

                ChangeAvailableMapDefs(FilteredMapDefs());
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapSelectDropdown] Outer error: {e.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void ChangeAvailableMapDefs(IEnumerable<MapDef> mapDefs)
        {
            if (_selectableMapDefs != null && Enumerable.SequenceEqual(mapDefs, _selectableMapDefs))
                return;

            _selectableMapDefs = mapDefs.ToList();

            var names = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (var def in _selectableMapDefs) names.Add(def.DisplayName);

            _dropdown.Show(names);
            gameObject.SetActive(_selectableMapDefs.Count > 1);
        }

        [HideFromIl2Cpp]
        private IEnumerable<MapDef> FilteredMapDefs()
        {
            return _mapDefs.Values.Where(MatchesFilter).OrderBy(m => m.DisplayName);
        }

        [HideFromIl2Cpp]
        private bool MatchesFilter(MapDef m)
        {
            if (string.IsNullOrEmpty(_nameFilter)) return true;
            return m.MapInternalNames.Contains(_nameFilter);
        }

        private static string StripJsonComments(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            if (json[0] == '\uFEFF') json = json.Substring(1);

            var sb = new System.Text.StringBuilder(json.Length);
            bool inString = false, inLineComment = false, inBlockComment = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (inLineComment)
                {
                    if (c == '\n' || c == '\r') { inLineComment = false; sb.Append(c); }
                    continue;
                }
                if (inBlockComment)
                {
                    if (c == '*' && i + 1 < json.Length && json[i + 1] == '/') { inBlockComment = false; i++; }
                    continue;
                }
                if (inString)
                {
                    sb.Append(c);
                    if (c == '\\' && i + 1 < json.Length) sb.Append(json[++i]);
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') { inString = true; sb.Append(c); continue; }
                if (c == '/' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    if (next == '/') { inLineComment = true; i++; continue; }
                    if (next == '*') { inBlockComment = true; i++; continue; }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}