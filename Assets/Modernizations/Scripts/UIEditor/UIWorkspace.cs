using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Modernizations
{
    public class UIWorkspace : MonoBehaviour, IPointerClickHandler
    {
        public UnityEvent OnMouseUp;
        public Pooling BlocksList;
        public Pooling ConnectionsList;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnMouseUp.Invoke();
        }
    }
}