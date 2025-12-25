// ========================================
//
// InputNoteKeyCodeSettingsItem.cs
//
// ========================================
//
// ノート入力キー設定画面の 1 アイテムを管理する Presenter。
// ・選択状態の背景色・文字色切り替え
// ・キー入力の取得（設定画面が開いている時のみ）
// ・キーコードの更新と通知
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using System.Runtime.CompilerServices;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class InputNoteKeyCodeSettingsItem : MonoBehaviour
    {
        [SerializeField] Color selectedStateBackgroundColor = default; // 選択時の背景色
        [SerializeField] Color defaultBackgroundColor = default;       // 通常時の背景色
        [SerializeField] Color selectedTextColor = default;            // 選択時の文字色
        [SerializeField] Color defaultTextColor = default;             // 通常時の文字色

        ReactiveProperty<KeyCode> keyCode = new ReactiveProperty<KeyCode>(); // このブロックのキーコード
        int block; // 対応するブロック番号

        /// <summary>
        /// Start は Awake の後、すべてのコンポーネントが初期化されたタイミングで呼ばれる。
        /// UI の初期設定とキー入力監視のセットアップを行う。
        /// </summary>
        private void Start()
        {
            // スケールを 1 に固定（Prefab の影響を受けないように）
            GetComponent<RectTransform>().localScale = Vector3.one;

            var text = GetComponentInChildren<Text>();
            var image = GetComponent<Image>();

            // ----------------------------------------
            // 選択状態の背景色・文字色切り替え
            // ----------------------------------------
            Settings.SelectedBlock
                .Select(selectedBlock => block == selectedBlock)
                .Do(selected =>
                    image.color = selected ? selectedStateBackgroundColor : defaultBackgroundColor)
                .Subscribe(selected =>
                    text.color = selected ? selectedTextColor : defaultTextColor)
                .AddTo(this);

            // ----------------------------------------
            // キー入力の取得（設定画面が開いている時のみ）
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => Settings.IsOpen.Value)                 // 設定画面が開いている
                .Where(_ => Settings.SelectedBlock.Value == block) // このブロックが選択されている
                .Where(_ => Input.anyKeyDown)                      // 何かキーが押された
                .Select(_ => KeyInput.FetchKey())                  // 押されたキーを取得
                .Where(keyCode => keyCode != KeyCode.None)         // 無効キーは除外
                .Do(keyCode => this.keyCode.Value = keyCode)       // ReactiveProperty 更新
                .Do(keyCode => Settings.NoteInputKeyCodes.Value[block] = keyCode) // 設定更新
                .Subscribe(_ => Settings.RequestForChangeInputNoteKeyCode.OnNext(Unit.Default))
                .AddTo(this);

            // ----------------------------------------
            // 表示テキスト更新（例: "3: Space"）
            // ----------------------------------------
            this.keyCode
                .Select(keyCode => block + ": " + keyCode)
                .SubscribeToText(GetComponentInChildren<Text>())
                .AddTo(this);
        }

        /// <summary>
        /// この設定アイテムにブロック番号とキーコードを設定する。
        /// </summary>
        public void SetData(int block, KeyCode keyCode)
        {
            this.block = block;
            this.keyCode.Value = keyCode;
        }

        /// <summary>
        /// マウスクリックでこのブロックを選択状態にする。
        /// </summary>
        public void OnMouseDown()
        {
            Settings.SelectedBlock.Value = block;
        }
    }
}
