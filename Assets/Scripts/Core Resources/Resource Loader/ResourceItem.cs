using UnityEngine;

namespace CoreResources.ResourceLoader
{
    // Currently we're only using this to load
    // prefabs so there's really no need to add
    // an extension parameter as well
    [System.Serializable]
    public class ResourceItem
    {
        [SerializeField] private string _name;
        [SerializeField] private string _path;
        
        public string Name => _name;
        public string Path => _path;
        private string _resourcesPath;
        public UnityEngine.Object retrievedObject;

        public string ResourcesPath
        {
            get
            {
                if (string.IsNullOrEmpty(_resourcesPath))
                {
                    _resourcesPath = string.IsNullOrEmpty(Path) ? Name : Path + "/" + Name;
                }

                return _resourcesPath;
            }
        }

        public ResourceItem(string name, string path)
        {
            int index = name.LastIndexOf(".", System.StringComparison.Ordinal);
            _name = name.Substring(0, index);
            _path = path;
        }

        public T Load<T>() where T : UnityEngine.Object
        {
            return Resources.Load<T>(ResourcesPath);
        }
    }
}