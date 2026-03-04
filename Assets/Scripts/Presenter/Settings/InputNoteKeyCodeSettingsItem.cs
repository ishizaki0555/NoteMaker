// ========================================
//
// NoteMaker Project
//
// ========================================
//
// InputNoteKeyCodeSettingsItem.cs
// ノーツ入力キー設定（1 ブロック＝1 キー）の UI アイテムです。
// ・選択中ブロックのハイライト表示
// ・キー入力の取得（設定ウィンドウ開放中のみ）
// ・設定値の反映と通知
//
//========================================

using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ノーツ入力キー設定の 1 行を表す UI コンポーネントです。
    /// ・クリックでブロック選択  
    /// ・選択中は背景色・文字色を変更  
    /// ・任意のキーを押すとそのキーを割り当て  
    /// ・Settings.NoteInputKeyCodes に反映し、変更通知を送信  
    /// </summary>
    public class InputNoteKeyCodeSettingsItem : MonoBehaviour
    {
        [SerializeField] Color selectedStateBackgroundColor = default; // 選択時の背景色
        [SerializeField] Color defaultBackgroundColor = default;       // 通常時の背景色
        [SerializeField] Color selectedTextColor = default;            // 選択時の文字色
        [SerializeField] Color defaultTextColor = default;             // 通常時の文字色

        ReactiveProperty<KeyCode> keyCode = new ReactiveProperty<KeyCode>(); // 割り当てキー
        int block; // 対象ブロック番号

        void Start()
        {
            GetComponent<RectTransform>().localScale = Vector3.one;

            var text = GetComponentInChildren<Text>();
            var image = GetComponent<Image>();

            //===============================
            // 選択中ブロックのハイライト
            //===============================
            Settings.SelectedBlock
                .Select(selectedBlock => block == selectedBlock)
                .Do(selected =>
                    image.color = selected ? selectedStateBackgroundColor : defaultBackgroundColor)
                .Subscribe(selected =>
                    text.color = selected ? selectedTextColor : defaultTextColor)
                .AddTo(this);

            //===============================
            // キー入力の取得（設定ウィンドウ開放中のみ）
            //===============================
            this.UpdateAsObservable()
                .Where(_ => Settings.IsOpen.Value)               // 設定ウィンドウが開いている
                .Where(_ => Settings.SelectedBlock.Value == block) // このブロックが選択中
                .Where(_ => Input.anyKeyDown)                    // 何かキーが押された
                .Select(_ => KeyInput.FetchKey())                // 押されたキーを取得
                .Where(k => k != KeyCode.None)                   // 無効キーは除外
                .Do(k => this.keyCode.Value = k)                 // UI 表示更新
                .Do(k => Settings.NoteInputKeyCodes.Value[block] = k) // 設定値反映
                .Subscribe(_ => Settings.RequestForChangeInputNoteKeyCode.OnNext(Unit.Default))
                .AddTo(this);

            //===============================
            // 表示テキスト更新
            //===============================
            this.keyCode
                .Select(k => block + ": " + k)
                .SubscribeToText(GetComponentInChildren<Text>())
                .AddTo(this);
        }

        /// <summary>
        /// 初期データ設定。
        /// </summary>
        public void SetData(int block, KeyCode keyCode)
        {
            this.block = block;
            this.keyCode.Value = keyCode;
        }

        /// <summary>
        /// クリックでこのブロックを選択状態にする。
        /// </summary>
        public void OnMouseDown()
        {
            Settings.SelectedBlock.Value = block;
        }
    }
}
