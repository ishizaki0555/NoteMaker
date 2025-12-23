// for uGUI(form 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using UnityEngine.Events;
using UnityEngine.EventSystems;

public static partial class UIBehaviourExtensions
{
    public static void AddListener(this UIBehaviour uiBehaviour, EventTriggerType eventID, UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = eventID;
        entry.callback.AddListener(callback);

        var eventTriggers = (uiBehaviour.GetComponent<EventTrigger>() ?? uiBehaviour.gameObject.AddComponent<EventTrigger>()).triggers;
        eventTriggers.Add(entry);
    }

    public static void RemoveAllListeners(this UIBehaviour uIBehaviour, EventTriggerType eventID)
    {
        var eventTrgger = uIBehaviour.GetComponent<EventTrigger>();

        if (eventTrgger == null)
            return;

        eventTrgger.triggers.RemoveAll(listener => listener.eventID == eventID);
    }
}

#endif