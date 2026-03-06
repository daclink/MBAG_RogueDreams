using UnityEditor;
using UnityEngine;
using Tables;

namespace Tables.Editor
{
    [CustomEditor(typeof(TextTable2D))]
    public class TextTable2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var table = (TextTable2D)target;
            if (table == null) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Text Table 2D", EditorStyles.boldLabel);

            int used = table.GetUsedSlotCount();
            int total = TextTable2D.TotalSlots;
            Table2DInspectorHelper.DrawSummary(used, total, "texts", Table2DInspectorHelper.DefaultEditMenuPath);
        }
    }
}
