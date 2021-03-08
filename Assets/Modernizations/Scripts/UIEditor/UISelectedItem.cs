using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modernizations
{
    public class UISelectedItem : MonoBehaviour
    {
        public UIWorkspace Workspace;
        public UISelectedBlock SelectedBlock;
        public Button Exit;
        public Vector2 Offset;

        public ItemSet Item
        {
            get { return GetItem(); }
            set { SetItem(value); }
        }
        [SerializeField] ItemSet _item;
        ItemSet GetItem()
        {
            return _item;
        }
        void SetItem(ItemSet value)
        {
            _item = value;
            Refresh();
        }

        private void Start()
        {
            Workspace.OnMouseUp.AddListener(delegate
            {
                SelectedBlock.Block = null;
            });
            Exit.onClick.AddListener(delegate
            {
                Item = null;
                StorageEditor.OnCloseWindow?.Invoke();
            });
        }

        public void Refresh()
        {
            Workspace.BlocksList.DeactiveAll();
            if (_item != null)
            {
                foreach (Transform parent in transform)
                {
                    parent.gameObject.SetActive(true);
                }
                UILink hit;
                foreach (var Block in _item.Modernizations)
                {
                    hit = Workspace.BlocksList.Use().UILink;
                    hit.Button.onClick.RemoveAllListeners();
                    hit.Button.onClick.AddListener(delegate
                    {
                        SelectedBlock.Block = Block;
                    });
                    hit.name = Block.Name;
                    hit.GetChildByName("Name").Text.text = Block.Name;
                    hit.GetChildByName("Frame").Image.color = Block.IsDefault ? new Color(0.8f, 0.4f, 0) : (_item.Stock.Contains(Block.GetID()) ? Color.green : Color.clear);
                    hit.RectTransform.anchoredPosition = new Vector2(Mathf.Ceil(Block.position.x / 20) * 20, -Mathf.Ceil(Block.position.y / 20) * 20) + Offset;
                    var image = hit.GetChildByName("Image", false);
                    if (Block.Sprite != null)
                    {
                        image.Image.sprite = Block.Sprite;
                        image.Image.color = Color.white;
                    }
                    else
                    {
                        image.Image.color = Color.clear;
                    }
                    foreach (var Connection in Block.Connections)
                    {
                        hit = Workspace.ConnectionsList.Use().UILink;
                        hit.RectTransform.SetAsFirstSibling();
                        PropertyBlock target = _item.Modernizations.Find(x => x.GetID() == Connection);
                        Vector2 startPos = new Vector2(Mathf.Ceil(Block.position.x / 20) * 20, -Mathf.Ceil(Block.position.y / 20) * 20);
                        Vector2 endPos = new Vector2(Mathf.Ceil(target.position.x / 20) * 20, -Mathf.Ceil(target.position.y / 20) * 20);
                        Vector2 direction = endPos - startPos;
                        hit.RectTransform.anchoredPosition = (startPos + endPos) / 2 + Offset;
                        hit.RectTransform.eulerAngles = Vector3.forward * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        hit.RectTransform.sizeDelta = Vector2.right * direction.magnitude + Vector2.up * 6;
                    }
                }
            }
            else
            {
                foreach (Transform parent in transform)
                {
                    parent.gameObject.SetActive(false);
                }
            }
        }
    }
}