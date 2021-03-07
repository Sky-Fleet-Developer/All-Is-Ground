using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_CrewCommands : MonoBehaviourPlus
{
    public CommandItem Commands;
    public static CommandItem current;
    bool Enabled = false;
    List<Button> listners;
    [System.Serializable]
    public class CommandItem
    {
        public GameObject Root;
        public List<CommandItem> items;
        public Button button;
        public string action;

        public void Init(UI_CrewCommands link, int last = 0)
        {
            if (action == string.Empty)
            {
                Root.SetActive(true);
                for(int i = 0; i < items.Count; i++)
                {
                    int n = i;
                    CommandItem N = items[n];
                    UnityAction act = new UnityAction(delegate
                    {
                        current.Root.SetActive(false);
                        current = N;
                        current.Init(link, n);
                    });
                    InputEvents.Instance.OnButtonDown("Command_" + (i + 1)).RemoveAllListeners();
                    InputEvents.Instance.OnButtonDown("Command_" + (i + 1)).AddListener(act);
                    items[i].button.onClick.AddListener(act);
                    link.listners.Add(items[i].button);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                    InputEvents.Instance.OnButtonDown("Command_" + (i + 1)).RemoveAllListeners();
                for (int i = 0; i < link.listners.Count; i++)
                    link.listners[i].onClick.RemoveAllListeners();

                GameManager.Instance.AIShip.Invoke(action, 0);

                //FPSInputController.Player.crew.CurrentShip.Invoke(action, 0);
                if (current.Root)
                {
                    current.Root.SetActive(false);
                    current = null;
                }
                link.Enabled = false;
            }
        }
    }

    void Start()
    {
        listners = new List<Button>();
        InputEvents.Instance.OnButtonDown("Commands").AddListener(delegate
        {
            if (Enabled)
            {
                current.Root.SetActive(false);
                Enabled = false;
                for (int i = 0; i < listners.Count; i++)
                    listners[i].onClick.RemoveAllListeners();
                for (int i = 0; i < 5; i++)
                    InputEvents.Instance.OnButtonDown("Command_" + (i + 1)).RemoveAllListeners();
            }
            else
            {
                current = Commands;
                Commands.Init(this);
                Enabled = true;
            }
        });
    }
}
