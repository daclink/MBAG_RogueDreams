using UnityEditor;
using UnityEngine;

namespace Tables.Editor
{
    /// <summary>
    /// Shared inspector UI for slot-based tables (SpriteTable2D, TextTable2D, etc.).
    /// Shows used/total summary and edit prompt. Use from CustomEditor OnInspectorGUI.
    /// </summary>
    public static class Table2DInspectorHelper
    {
        public const string DefaultEditMenuPath = "Window/Item Authoring";

        /// <summary>Draws the standard table summary: used count, total slots, and edit prompt.</summary>
        /// <param name="usedCount">Number of populated slots.</param>
        /// <param name="totalSlots">Total capacity (e.g. PackedItemTableCore.TotalSlots).</param>
        /// <param name="tableKind">Display name, e.g. "Sprites" or "Text".</param>
        /// <param name="editMenuPath">Menu path for the editor window, e.g. "Window/Item Authoring".</param>
        public static void DrawSummary(int usedCount, int totalSlots, string tableKind, string editMenuPath = DefaultEditMenuPath)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"{usedCount} used / {totalSlots} total slots", EditorStyles.helpBox);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Edit {tableKind} via {editMenuPath}. Data is loaded/saved from file paths configured there.",
                MessageType.Info);
            if (GUILayout.Button($"Open {editMenuPath.Split('/')[^1]}"))
                UnityEditor.EditorApplication.ExecuteMenuItem(editMenuPath);
        }
    }
}
