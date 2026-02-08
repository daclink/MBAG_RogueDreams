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