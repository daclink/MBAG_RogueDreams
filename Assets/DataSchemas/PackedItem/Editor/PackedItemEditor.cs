using UnityEditor;
using UnityEngine;

namespace DataSchemas.PackedItem.Editor
{
    [CustomEditor(typeof(PackedItemAsset))]
    public class PackedItemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var asset = (PackedItemAsset)target;
            if (asset == null) return;
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tables", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign Text Table and Sprite Table above, then enter display text and assign a sprite. Click the button to add them to the tables and set this item's keys.", MessageType.None);
            if (GUILayout.Button("Add to tables & set keys"))
            {
                Undo.RecordObject(asset, "Add to tables & set keys");
                if (asset.textTable != null) Undo.RecordObject(asset.textTable, "Add to tables & set keys");
                if (asset.spriteTable != null) Undo.RecordObject(asset.spriteTable, "Add to tables & set keys");
                if (asset.AddDisplayToTablesAndSetKeys())
                {
                    MarkDirtyAndRefresh();
                    if (asset.textTable != null) EditorUtility.SetDirty(asset.textTable);
                    if (asset.spriteTable != null) EditorUtility.SetDirty(asset.spriteTable);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Pack / Unpack", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Pack From Fields"))
            {
                Undo.RecordObject(asset, "Pack From Fields");
                asset.PackFromFields();
                MarkDirtyAndRefresh();
            }

            if (GUILayout.Button("Unpack To Fields"))
            {
                Undo.RecordObject(asset, "Unpack To Fields");
                asset.UnpackToFields();
                MarkDirtyAndRefresh();
            }

            if (GUILayout.Button("Randomize & Pack"))
            {
                Undo.RecordObject(asset, "Randomize & Pack");
                asset.RandomizeStatsAndPack();
                MarkDirtyAndRefresh();
            }

            EditorGUILayout.EndHorizontal();
        }

        void MarkDirtyAndRefresh()
        {
            EditorUtility.SetDirty(target);
            serializedObject.Update();
            Repaint();
        }
    }
}