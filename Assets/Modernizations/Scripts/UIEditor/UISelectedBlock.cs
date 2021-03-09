using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modernizations
{
    public class UISelectedBlock : MonoBehaviour
    {
        public Color TextColor;
        Image Background;
        public PropertyBlock Block
        {
            get { return GetBlock(); }
            set { SetBlock(value); }
        }
        [SerializeField] PropertyBlock _block;
        PropertyBlock GetBlock()
        {
            return _block;
        }
        void SetBlock(PropertyBlock value)
        {
            _block = value;
            Refresh();
        }
        public Pooling TextsList;

        private void Awake()
        {
            Background = GetComponent<Image>();
        }

        public void Refresh()
        {
            TextsList.DeactiveAll();
            var button = TextsList.Tr.Find("Explore").GetComponent<UILink>();
            int i = 0;
            if (_block != null)
            {
                var hit = TextsList.Use(i++).UILink;
                hit.Text.text = _block.Description;
                hit.RectTransform.sizeDelta = new Vector2(hit.RectTransform.sizeDelta.x, 40);
                hit.Text.color = TextColor;
                foreach (var property in _block.Properties)
                {
                    hit = TextsList.Use(i++).UILink;
                    hit.Text.text = "    " + property.property.Description + property.GetDescription();
                    hit.Text.color = TextColor;
                }

                ItemSet Item = Storage.Instance.Items.Find(x => x.Modernizations.Contains(_block));


                if (_block.IsDefault || Item.Stock.Contains(_block.GetID()))
                {
                    hit = TextsList.Use(i++).UILink;
                    hit.Text.text = "    Explored";
                    hit.Text.color = Color.green * 0.8f;
                    button.gameObject.SetActive(false);
                }
                else
                {
                    int n;
                    string c = _block.GetCost(out n);
                    n++;
                    if (n > 1)
                        hit = TextsList.Use(i++).UILink;
                    hit.Text.text = "Cost:\n" + c;
                    hit.Text.color = TextColor;
                    hit.RectTransform.sizeDelta = new Vector2(hit.RectTransform.sizeDelta.x, 25 * n);


                    if (Storage.AvailableWithParents(Block, Item))
                    {
                        Color color = Storage.AvailableWithResources(_block, Item) ? new Color(0.2f, 0.7f, 0.2f, 1) : new Color(0.7f, 0.2f, 0.2f, 1);
                        button.Button.onClick.RemoveAllListeners();
                        button.Image.color = color;
                        button.gameObject.SetActive(true);
                        button.GetChildByName("Text").Text.text = "Explore";
                        button.transform.SetAsLastSibling();
                        button.Button.onClick.AddListener(delegate
                        {
                            UsersDATA.Instance.StartCoroutine(UsersDATA.Instance.Explore(_block.id, (v) =>
                            {
                                if (v)
                                {
                                    Storage.Explore(Item.Name, _block);
                                }
                            }));
                            UIStorageEditor.Refresh();
                        });
                    }
                    else
                    {
                        button.gameObject.SetActive(false);
                        hit = TextsList.Use(i++).UILink;
                        hit.Text.text = "To explore this modernization,\nexplore the previous one.";
                        hit.Text.color = Color.red;
                    }
                }
                Background.enabled = true;
            }
            else
            {
                button.gameObject.SetActive(false);
                Background.enabled = false;
            }
        }
    }
}