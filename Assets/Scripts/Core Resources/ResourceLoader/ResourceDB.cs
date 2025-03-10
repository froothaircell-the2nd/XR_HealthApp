using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreResources.ResourceLoader
{
    [CreateAssetMenu(fileName = "ResourceDB", menuName = "Scriptable Objects/ResourceDB", order = 1)]
    public class ResourceDB : ScriptableObject
    {
        private static ResourceDB _instance;

        public static ResourceDB Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindInstance();
                
                if (_instance != null)
                    return _instance;

                _instance = CreateInstance<ResourceDB>();

#if UNITY_EDITOR
                var resDir = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources/"));
                if (!resDir.Exists)
                    AssetDatabase.CreateFolder("Assets/", "Resources");

                AssetDatabase.CreateAsset(_instance, "Assets/Resources/ResourceDB.asset");
                _instance = FindInstance();
#endif
                return _instance;
            }
        }

        [SerializeField] private List<ResourceItem> _prefabResourceItems = new List<ResourceItem>();
        [SerializeField] private List<ResourceItem> _otherResourceItems = new List<ResourceItem>();

        public int PrefabCount => _prefabResourceItems.Count;
        public int OtherCount => _otherResourceItems.Count;

        public static ResourceDB FindInstance()
        {
            return Resources.Load<ResourceDB>("ResourceDB");
        }
        
        // Can add a template to the functions below
        // later if we're adding more than just
        // prefabs to this DB
        public bool HasAsset(string assetName)
        {
            ResourceItem item = CheckForItemInList(assetName, _otherResourceItems) ?? CheckForItemInList(assetName, _prefabResourceItems);
            return item != null;
        }

        public ResourceItem GetResourceItem(string assetName)
        {
            ResourceItem item = CheckForItemInList(assetName, _otherResourceItems) ?? CheckForItemInList(assetName, _prefabResourceItems);
            return item;
        }

        private static string ConvertToPath(string aPath)
        {
            return aPath.Replace("\\", "/");
        }

        private ResourceItem CheckForItemInList(string assetName, List<ResourceItem> list)
        {
            foreach (var item in list)
            {
                if (item.Name.Equals(assetName))
                    return item;
            }

            return null;
        }

#if UNITY_EDITOR
        public void UpdateResourceDB()
        {
            Debug.Log("Updating ResourceDB");
            ClearAllLists();

            var topFolders = FindResourcesFolders(true);

            foreach (var folder in topFolders)
            {
                string path = folder.FullName;
                int prefix = path.Length;
                if (!path.EndsWith("/"))
                    prefix++;
                AddFileList(folder, prefix);
            }
        }

        private List<DirectoryInfo> FindResourcesFolders(bool onlyTopFolders)
        {
            var assets = new DirectoryInfo(Application.dataPath);
            var list = new List<DirectoryInfo>();
            ScanFolder(assets, list, onlyTopFolders);
            return list;
        }

        void ScanFolder(DirectoryInfo directoryInfo, List<DirectoryInfo> rFolderList, bool onlyTopFolders)
        {
            string directoryName = directoryInfo.Name.ToLower();

            if (directoryName == "editor")
                return;

            if (directoryName == "resources")
            {
                rFolderList.Add(directoryInfo);
                if (onlyTopFolders)
                    return;
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                ScanFolder(dir, rFolderList, onlyTopFolders);
            }
        }

        private void AddFileList(DirectoryInfo directoryInfo, int prefix)
        {
            string relFolder = directoryInfo.FullName;

            if (relFolder.Length < prefix)
                relFolder = "";
            else
                relFolder = relFolder.Substring(prefix);

            relFolder = ConvertToPath(relFolder);

            foreach (var folder in directoryInfo.GetDirectories())
            {
                AddFileList(folder, prefix);
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                string ext = file.Extension.ToLower();

                if (ext == ".meta")
                    continue;

                string assetPath = "assets/" + file.FullName.Substring(Application.dataPath.Length + 1);
                assetPath = ConvertToPath(assetPath);

                Object obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (obj == null)
                {
                    Debug.LogWarning("ResourceDB | File at path " + assetPath + " couldn't be loaded and is ignored");
                    continue;
                }

                string type = obj.GetType().AssemblyQualifiedName;

                GetResourceItemList(ext)?.Add(new ResourceItem(file.Name, relFolder));
            }

            Resources.UnloadUnusedAssets();
        }

        private List<ResourceItem> GetResourceItemList(string extension)
        {
            if (extension.Equals(".prefab"))
                return _prefabResourceItems;

            return _otherResourceItems;
        }

        private void ClearAllLists()
        {
            _prefabResourceItems.Clear();
            _otherResourceItems.Clear();
        }
#endif
    }
}