// ========================================
//
// EditSectionPresenter.cs
//
// ========================================
//
// セクション範囲（point1 ～ point2）の可視化を行う Presenter。
// ・2 つのハンドル位置から矩形領域を算出して GL 描画
// ・スライダー上のマーカー位置と幅を更新
// ・ハンドルの向き（左右反転）を調整
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Utility;
using NoteMaker.GLDrawing;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class EditSectionPresenter : MonoBehaviour
    {
        [SerializeField] RectTransform markerRect = default;                     // セクション矩形の UI サイズ参照
        [SerializeField] EditSectionHandlePresenter point1 = default;            // 左右どちらにもなり得るハンドル1
        [SerializeField] EditSectionHandlePresenter point2 = default;            // ハンドル2
        [SerializeField] RectTransform playbackPositionSliderRectTransform = default; // スライダー全体
        [SerializeField] RectTransform sliderMarker = default;                   // スライダー上のセクション表示
        [SerializeField] Color markerColor = default;                            // セクション矩形の色

        Geometry drawData = new Geometry(
            Enumerable.Range(0, 4).Select(_ => Vector3.zero).ToArray(),
            Color.clear);

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// セクション範囲の描画・スライダー更新処理を初期化。
        /// </summary>
        void Init()
        {
            var sliderWidth = playbackPositionSliderRectTransform.sizeDelta.x;

            // point1 / point2 の位置が変わるたびに矩形とスライダーマーカーを更新
            Observable.Merge(point1.Position, point2.Position)
                .Subscribe(_ =>
                {
                    // ----------------------------------------
                    // ハンドルの左右を決定（小さい方が start）
                    // ----------------------------------------
                    var sortedPoints = new[] { point1, point2 }.OrderBy(p => p.Position.Value);
                    var start = sortedPoints.First();
                    var end = sortedPoints.Last();

                    // ハンドルの向きを左右反転
                    var scaleStart = start.HandleRectTransform.localScale;
                    scaleStart.x = -1;
                    start.HandleRectTransform.localScale = scaleStart;

                    var scaleEnd = end.HandleRectTransform.localScale;
                    scaleEnd.x = 1;
                    end.HandleRectTransform.localScale = scaleEnd;

                    // ----------------------------------------
                    // キャンバス上の矩形座標を算出
                    // ----------------------------------------
                    var markerCanvasWidth = end.Position.Value - start.Position.Value;

                    var startPos = start.Position.Value / NoteCanvas.ScaleFactor.Value + Screen.width / 2f;
                    var halfScreenHeight = Screen.height / 2f;
                    var halfHeight = markerRect.sizeDelta.y / NoteCanvas.ScaleFactor.Value / 2f;

                    var min = new Vector2(startPos, halfScreenHeight - halfHeight);
                    var max = new Vector2(startPos + markerCanvasWidth / NoteCanvas.ScaleFactor.Value,
                                          halfScreenHeight + halfHeight);

                    // GL 描画用の矩形データ更新
                    drawData = new Geometry(
                        new[]
                        {
                            new Vector3(min.x, max.y, 0),
                            new Vector3(max.x, max.y, 0),
                            new Vector3(max.x, min.y, 0),
                            new Vector3(min.x, min.y, 0)
                        },
                        markerColor);

                    // ----------------------------------------
                    // スライダー上のマーカー更新
                    // ----------------------------------------
                    var sliderMarkerSize = sliderMarker.sizeDelta;
                    sliderMarkerSize.x = sliderWidth * markerCanvasWidth / NoteCanvas.Width.Value;
                    sliderMarker.sizeDelta = sliderMarkerSize;

                    if (NoteCanvas.Width.Value > 0)
                    {
                        var startPer =
                            (start.Position.Value - ConvertUtils.SamplesToCanvasPositionX(0))
                            / NoteCanvas.Width.Value;

                        var sliderMarkerPos = sliderMarker.localPosition;
                        sliderMarkerPos.x = sliderWidth * startPer - sliderWidth / 2f;
                        sliderMarker.localPosition = sliderMarkerPos;
                    }
                });
        }

        /// <summary>
        /// GL 描画は LateUpdate で行う。
        /// </summary>
        void LateUpdate()
        {
            GLQuadDrawer.Draw(drawData);
        }
    }
}
