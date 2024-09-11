using UnityEngine;
using UnityEngine.Events;

namespace SO
{
    [CreateAssetMenu(fileName = "ButtonEvent", menuName = "ScriptableObjects/ButtonEvent", order = 1)]
    public class ButtonEvent : ScriptableObject
    {
        public UnityEvent onClickEvent;

        public void RaiseEvent()
        {
            onClickEvent?.Invoke();
        }
    }
}