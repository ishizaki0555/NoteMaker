// ========================================
//
// UIBehaviourExtensions.cs
//
// ========================================
//
// uGUI の UIBehaviour に EventTrigger を簡単に追加・削除する拡張メソッド
//
// ========================================

// for uGUI(form 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using UnityEngine.Events;
using UnityEngine.EventSystems;

public static partial class UIBehaviourExtensions
{
    /// <summary>
    /// 指定した UIBehaviour に EventTrigger を追加し、イベントリスナーを登録する。
    /// </summary>
    public static void AddListener(this UIBehaviour uiBehaviour, EventTriggerType eventID, UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = eventID;
        entry.callback.AddListener(callback);

        // EventTrigger が存在しなければ追加し、triggers を取得する
        var eventTriggers =
            (uiBehaviour.GetComponent<EventTrigger>() ??
             uiBehaviour.gameObject.AddComponent<EventTrigger>())
            .triggers;

        eventTriggers.Add(entry);
    }

    /// <summary>
    /// 指定したイベント ID の EventTrigger リスナーをすべて削除する。
    /// </summary>
    public static void RemoveAllListeners(this UIBehaviour uIBehaviour, EventTriggerType eventID)
    {
        var eventTrgger = uIBehaviour.GetComponent<EventTrigger>();

        // EventTrigger が存在しない場合は何もしない
        if (eventTrgger == null)
            return;

        // 指定 eventID のリスナーをすべて削除
        eventTrgger.triggers.RemoveAll(listener => listener.eventID == eventID);
    }
}
#endif