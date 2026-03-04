// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// UIBehaviourExtensions.cs
// uGUI（Unity UI）の EventTrigger に対して、イベント登録・削除を簡潔に行うための
// 拡張メソッドを提供します。UI 要素に対して任意のイベントを動的に追加できます。
// 
//========================================

#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace NoteMaker.Utility
{
    /// <summary>
    /// UIBehaviour に対して EventTrigger のイベント登録・削除を行う拡張メソッドです。
    /// ・AddListener … 指定イベントにコールバックを追加  
    /// ・RemoveAllListeners … 指定イベントのリスナーをすべて削除  
    /// 
    /// EventTrigger コンポーネントが存在しない場合は自動的に追加されます。
    /// </summary>
    public static partial class UIBehaviourExtensions
    {
        /// <summary>
        /// 指定した EventTriggerType にコールバックを追加します。
        /// EventTrigger が存在しない場合は自動的に追加されます。
        /// </summary>
        public static void AddListener(this UIBehaviour uiBehaviour, EventTriggerType eventID, UnityAction<BaseEventData> callback)
        {
            // EventTrigger.Entry を作成してコールバックを追加
            var entry = new EventTrigger.Entry();
            entry.eventID = eventID;
            entry.callback.AddListener(callback);

            // EventTrigger コンポーネントを取得（存在しない場合は追加）して、Entry を登録
            var eventTriggers =
                (uiBehaviour.GetComponent<EventTrigger>() ??
                 uiBehaviour.gameObject.AddComponent<EventTrigger>())
                .triggers;

            eventTriggers.Add(entry);
        }

        /// <summary>
        /// 指定した EventTriggerType のリスナーをすべて削除します。
        /// EventTrigger が存在しない場合は何もしません。
        /// </summary>
        public static void RemoveAllListeners(this UIBehaviour uiBehaviour, EventTriggerType eventID)
        {
            // EventTrigger コンポーネントを取得
            var eventTrigger = uiBehaviour.GetComponent<EventTrigger>();

            // EventTrigger が存在しない場合は何もしない
            if (eventTrigger == null)
                return;

            eventTrigger.triggers.RemoveAll(listener => listener.eventID == eventID);
        }
    }
}

#endif
