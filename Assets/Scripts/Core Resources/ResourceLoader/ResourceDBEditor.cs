using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreResources.ResourceLoader
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ResourceDB))]
    public class ResourceDBEditor : Editor
    {
        private ResourceDB _target;

        private void OnEnable()
        {
            _target = (ResourceDB) target;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            DrawDefaultInspector();
            GUI.enabled = true;
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Now"))
            {
                _target.UpdateResourceDB();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssets();
            }
            
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Prefabs: ", _target.PrefabCount.ToString());
        }
    }
#endif
}