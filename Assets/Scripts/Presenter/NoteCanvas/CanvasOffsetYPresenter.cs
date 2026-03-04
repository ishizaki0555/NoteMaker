// ========================================
//
// NoteMaker Project
//
// ========================================
//
// CanvasOffsetYPresenter.cs
// ノーツキャンバス全体の縦方向オフセット（OffsetY）を、
// ・楽曲読み込み時の初期化
// ・縦ラインドラッグによる移動
// ・Undo/Redo 対応
// ・UI 反映（水平ライン・波形表示）
// とともに管理するプレゼンターです。
// 
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// キャンバスの縦方向オフセット（OffsetY）を管理するクラスです。
    /// ・楽曲読み込み時に初期位置へリセット  
    /// ・縦ラインドラッグで OffsetY を変更  
    /// ・変更量を Clamp して画面外に行かないよう制御  
    /// ・Undo/Redo に対応  
    /// ・OffsetY の変化を UI（水平ライン・波形）へ反映  
    /// </summary>
    public class CanvasOffsetYPresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;          // マウスイベント
        [SerializeField] RectTransform horizontalLineRect = default;   // 水平ライン
        [SerializeField] RectTransform waveformRenderImage = default;  // 波形表示

        void Awake()
        {
            //===============================
            // 楽曲読み込み時の初期 OffsetY 設定
            //===============================
            Audio.OnLoad.Subscribe(_ =>
                NoteCanvas.OffsetY.Value = -Screen.height * 0.45f * NoteCanvas.ScaleFactor.Value);

            //===============================
            // 縦ラインドラッグによる OffsetY 操作
            //===============================
            var operateCanvasOffsetYObservable = this.UpdateAsObservable()
                .SkipUntil(canvasEvents.VerticalLineOnMouseDownObservable) // ドラッグ開始
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))                // ドラッグ終了まで
                .Select(_ => Input.mousePosition.y)
                .Buffer(2, 1).Where(b => b.Count >= 2)
                .RepeatSafe()
                .Select(b => (b[1] - b[0]) * NoteCanvas.ScaleFactor.Value) // 移動量
                .Select(delta => delta + NoteCanvas.OffsetY.Value)         // 現在値に加算
                .Select(y => new
                {
                    y,
                    max = Screen.height * 0.5f * 0.95f * NoteCanvas.ScaleFactor.Value
                })
                .Select(v => Mathf.Clamp(v.y, -v.max, v.max))              // 画面外に行かないよう制限
                .DistinctUntilChanged();

            // OffsetY を更新
            operateCanvasOffsetYObservable
                .Subscribe(y => NoteCanvas.OffsetY.Value = y);

            //===============================
            // Undo / Redo 対応
            //===============================
            operateCanvasOffsetYObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => b.Count >= 2)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => NoteCanvas.OffsetY.Value = x.current,
                            () => NoteCanvas.OffsetY.Value = x.prev)));

            //===============================
            // OffsetY → UI 反映
            //===============================
            NoteCanvas.OffsetY.Subscribe(y =>
            {
                var pos1 = horizontalLineRect.localPosition;
                var pos2 = waveformRenderImage.localPosition;

                pos1.y = pos2.y = y;

                horizontalLineRect.localPosition = pos1;
                waveformRenderImage.localPosition = pos2;
            });
        }
    }
}
