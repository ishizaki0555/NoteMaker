// ========================================
//
// CanvasOffsetXPresenter.cs
//
// ========================================
//
// キャンバスの横方向オフセット（OffsetX）を管理する Presenter。
// ・縦線（VerticalLine）ドラッグによるスクロール
// ・Undo / Redo 対応
// ・OffsetX の変化に応じて UI（縦線・波形画像）の位置を更新
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Model;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class CanvasOffsetXPresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;           // キャンバスのマウスイベント
        [SerializeField] RectTransform verticalLineRect = default;      // 再生位置を示す縦線
        [SerializeField] RectTransform waveformRenderImage = default;   // 波形描画用イメージ

        void Awake()
        {
            // ----------------------------------------
            // 初期 OffsetX を設定（ロード時）
            // ----------------------------------------
            Audio.OnLoad.Subscribe(_ =>
                NoteCanvas.OffsetX.Value = -Screen.width * 0.45f * NoteCanvas.ScaleFactor.Value);

            // ----------------------------------------
            // ドラッグによる OffsetX 操作
            // ----------------------------------------
            var operateCanvasOffsetXObservable = this.UpdateAsObservable()
                .SkipUntil(canvasEvents.VerticalLineOnMouseDownObservable) // ドラッグ開始
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))                // ドラッグ終了まで
                .Select(_ => Input.mousePosition.x)
                .Buffer(2, 1).Where(b => 2 <= b.Count)
                .RepeatSafe()
                .Select(b => (b[1] - b[0]) * NoteCanvas.ScaleFactor.Value) // ドラッグ量
                .Select(delta => delta + NoteCanvas.OffsetX.Value)         // 現在値に加算
                .Select(x => new
                {
                    x,
                    max = Screen.width * 0.5f * 0.95f * NoteCanvas.ScaleFactor.Value
                })
                .Select(v => Mathf.Clamp(v.x, -v.max, v.max))              // 範囲制限
                .DistinctUntilChanged();

            // OffsetX の更新
            operateCanvasOffsetXObservable.Subscribe(x => NoteCanvas.OffsetX.Value = x);

            // ----------------------------------------
            // Undo / Redo 用の履歴登録
            // ----------------------------------------
            operateCanvasOffsetXObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => 2 <= b.Count)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => NoteCanvas.OffsetX.Value = x.current,
                            () => NoteCanvas.OffsetX.Value = x.prev)));

            // ----------------------------------------
            // OffsetX → UI（縦線・波形画像）の位置反映
            // ----------------------------------------
            NoteCanvas.OffsetX.Subscribe(x =>
            {
                var pos = verticalLineRect.localPosition;
                var pos2 = waveformRenderImage.localPosition;

                pos.x = pos2.x = x;

                verticalLineRect.localPosition = pos;
                waveformRenderImage.localPosition = pos2;
            });
        }
    }
}
