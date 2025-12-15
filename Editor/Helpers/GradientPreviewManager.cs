using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Helpers
{
    [InitializeOnLoad]
    internal static class GradientPreviewManager
    {
        private struct Key
        {
            public int materialId;
            public string propName;
            public Key(int id, string prop)
            {
                materialId = id;
                propName = prop;
            }
        }

        private class Entry
        {
            public Material mat;
            public string propName;
            public Texture original;
            public Texture preview;
        }

        private static readonly Dictionary<Key, Entry> _entries = new Dictionary<Key, Entry>();
        private static bool _restoredForSave;

        static GradientPreviewManager()
        {
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAll;
            EditorApplication.quitting += RestoreAll;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode || s == PlayModeStateChange.ExitingPlayMode) RestoreAll();
            };
        }

        internal static void ApplyPreview(MaterialProperty prop, Texture previewTex)
        {
            if (prop == null || previewTex == null) return;

            foreach (Object o in prop.targets)
            {
                var mat = o as Material;
                if (mat == null) continue;

                var key = new Key(mat.GetInstanceID(), prop.name);
                if (!_entries.TryGetValue(key, out var entry))
                {
                    entry = new Entry
                    {
                        mat = mat,
                        propName = prop.name,
                        original = mat.GetTexture(prop.name),
                        preview = previewTex
                    };
                    _entries.Add(key, entry);
                }
                else
                {
                    entry.preview = previewTex;
                }

                mat.SetTexture(prop.name, previewTex);
            }
        }

        internal static void ClearPreview(MaterialProperty prop)
        {
            if (prop == null) return;

            foreach (Object o in prop.targets)
            {
                var mat = o as Material;
                if (mat == null) continue;

                var key = new Key(mat.GetInstanceID(), prop.name);
                if (_entries.TryGetValue(key, out var entry))
                {
                    if (entry.mat != null) entry.mat.SetTexture(entry.propName, entry.original);

                    _entries.Remove(key);
                }
            }
        }

        internal static void BeforeSaveRestoreOriginals()
        {
            if (_entries.Count == 0) return;

            foreach (var kv in _entries)
            {
                var e = kv.Value;
                if (e.mat != null) e.mat.SetTexture(e.propName, e.original);
            }

            _restoredForSave = true;
        }

        internal static void AfterSaveReapplyPreviews()
        {
            if (!_restoredForSave) return;
            _restoredForSave = false;

            foreach (var kv in _entries)
            {
                var e = kv.Value;
                if (e.mat != null && e.preview != null) e.mat.SetTexture(e.propName, e.preview);
            }
        }

        internal static void RestoreAll()
        {
            foreach (var kv in _entries)
            {
                var e = kv.Value;
                if (e.mat != null) e.mat.SetTexture(e.propName, e.original);
            }
            _entries.Clear();
            _restoredForSave = false;
        }
    }
}
