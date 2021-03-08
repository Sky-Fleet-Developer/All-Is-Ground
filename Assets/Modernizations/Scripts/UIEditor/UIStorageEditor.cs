using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modernizations
{
    public class UIStorageEditor : MonoBehaviour
    {
        public static UIStorageEditor Instance;
        public UISelectedBlock SelectedBlock;
        public UISelectedItem SelectedItem;

        public static void Refresh()
        {
            if (!Instance)
                return;

            if (Instance.SelectedItem.Item != null)
            {
                StorageEditor.SelectedItem = Instance.SelectedItem.Item;

                Instance.SelectedItem.Refresh();
                if (Instance.SelectedBlock.Block != null)
                {
                    Instance.SelectedBlock.Refresh();
                }
            }
        }

        private void Awake()
        {
            Instance = this;
            StorageEditor.Init();
        }

        void Start()
        {
            SelectedBlock.Block = null;
            StorageEditor.OnCloseWindow.AddListener(delegate
            {
                gameObject.SetActive(false);
            });
        }
    }
}