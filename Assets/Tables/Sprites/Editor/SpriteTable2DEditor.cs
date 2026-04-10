using UnityEditor;
using UnityEngine;
using Tables;

namespace Tables.Editor
{
    [CustomEditor(typeof(SpriteTable2D))]
    public class SpriteTable2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var table = (SpriteTable2D)target;
            if (table == null) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Sprite Table 2D", EditorStyles.boldLabel);

            int used = table.GetUsedSlotCount();
            int total = SpriteTable2D.TotalSlots;
            Table2DInspectorHelper.DrawSummary(used, total, "sprites", Table2DInspectorHelper.DefaultEditMenuPath);
        }
    }
}
