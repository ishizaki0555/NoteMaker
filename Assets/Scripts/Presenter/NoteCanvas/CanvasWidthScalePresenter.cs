// ========================================
//
// CanvasWidthScalePresenter.cs
//
// ========================================
//
// キャンバスの横幅スケール（NoteCanvas.Width）を管理する Presenter。
// ・Ctrl + マウスホイール
// ・↑ / ↓ キー
// ・スライダー操作
// などの入力からスケール値を更新し、Undo / Redo にも対応する。
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class CanvasWidthScalePresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;            // キャンバスのマウスイベント
        [SerializeField] Slider canvasWidthScaleController = default;    // スケール調整スライダー

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// キャンバス幅スケールの入力処理と Undo/Redo を初期化する。
        /// </summary>
        void Init()
        {
            // ----------------------------------------
            // スケール操作（Ctrl + ホイール / ↑ / ↓ / スライダー）
            // ----------------------------------------
            var operateCanvasScaleObservable = canvasEvents.MouseScrollWheelObservable
                    .Where(_ => KeyInput.CtrlKey())                           // Ctrl + ホイール
                .Merge(this.UpdateAsObservable()
                    .Where(_ => Input.GetKey(KeyCode.UpArrow))
                    .Select(_ => 0.05f))                                      // ↑ キー
                .Merge(this.UpdateAsObservable()
                    .Where(_ => Input.GetKey(KeyCode.DownArrow))
                    .Select(_ => -0.05f))                                     // ↓ キー
                .Select(delta => NoteCanvas.Width.Value * (1 + delta))        // スケール計算
                .Select(x => x / (Audio.Source.clip.samples / 100f))          // 正規化
                .Select(x => Mathf.Clamp(x, 0.1f, 2f))                        // 範囲制限
                .Merge(canvasWidthScaleController
                    .OnValueChangedAsObservable()
                    .DistinctUntilChanged())                                  // スライダー操作
                .DistinctUntilChanged()
                .Select(x => Audio.Source.clip.samples / 100f * x);           // 実際の幅に変換

            // スケール値を反映
            operateCanvasScaleObservable.Subscribe(x => NoteCanvas.Width.Value = x);

            // ----------------------------------------
            // Undo / Redo 用の履歴登録
            // ----------------------------------------
            operateCanvasScaleObservable
                .Buffer(operateCanvasScaleObservable.ThrottleFrame(2))
                .Where(b => 2 <= b.Count)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => NoteCanvas.Width.Value = x.current,
                            () => NoteCanvas.Width.Value = x.prev)));
        }
    }
}
