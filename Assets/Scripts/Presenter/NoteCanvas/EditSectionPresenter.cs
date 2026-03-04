// ========================================
//
// NoteMaker Project
//
// ========================================
//
// EditSectionPresenter.cs
// 2 本のセクションハンドル（縦ライン）で囲まれた範囲を可視化し、
// ・ハンドル位置の変化に応じたマーカー矩形の再描画
// ・スライダー側のマーカー位置・サイズ更新
// ・GL 描画データの更新
// を行うプレゼンターです。
//
//========================================

using NoteMaker.GLDrawing;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// セクション範囲（2 本の縦ハンドルで挟まれた区間）を管理するクラスです。
    /// ・ハンドル位置の監視  
    /// ・Canvas 座標 → Screen 座標変換  
    /// ・縦方向の矩形（マーカー）描画データ生成  
    /// ・スライダー側のマーカー位置・サイズ更新  
    /// </summary>
    public class EditSectionPresenter : MonoBehaviour
    {
        [SerializeField] RectTransform markerRect = default;                     // マーカー矩形の基準サイズ
        [SerializeField] EditSectionHandlePresenter point1 = default;            // ハンドル1
        [SerializeField] EditSectionHandlePresenter point2 = default;            // ハンドル2
        [SerializeField] RectTransform playbackPositionSliderRectTransform = default; // スライダー本体
        [SerializeField] RectTransform sliderMarker = default;                   // スライダー上のマーカー
        [SerializeField] Color markerColor = default;                            // マーカー色

        Geometry drawData = new Geometry(
            Enumerable.Range(0, 4).Select(_ => Vector3.zero).ToArray(),
            Color.clear);

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// ハンドル位置の変化に応じてマーカー矩形とスライダー表示を更新します。
        /// </summary>
        void Init()
        {
            var sliderHeight = playbackPositionSliderRectTransform.sizeDelta.y;

            Observable.Merge(point1.Position, point2.Position)
                .Subscribe(_ =>
                {
                    //===============================
                    // ハンドル位置のソート
                    //===============================
                    var sorted = new[] { point1, point2 }.OrderBy(p => p.Position.Value);
                    var start = sorted.First();
                    var end = sorted.Last();

                    //===============================
                    // ハンドルの向き（縦向きなので Y 反転）
                    //===============================
                    var scaleStart = start.HandleRectTransform.localScale;
                    scaleStart.y = -1;
                    start.HandleRectTransform.localScale = scaleStart;

                    var scaleEnd = end.HandleRectTransform.localScale;
                    scaleEnd.y = 1;
                    end.HandleRectTransform.localScale = scaleEnd;

                    //===============================
                    // マーカーの高さ（Canvas Y 差分）
                    //===============================
                    var markerCanvasHeight = end.Position.Value - start.Position.Value;

                    //===============================
                    // Canvas → Screen 座標変換（縦方向）
                    //===============================
                    var startPos = start.Position.Value / NoteCanvas.ScaleFactor.Value + Screen.height / 2f;

                    var halfScreenWidth = Screen.width / 2f;
                    var halfWidth = markerRect.sizeDelta.x / NoteCanvas.ScaleFactor.Value / 2f;

                    //===============================
                    // 縦方向矩形の 4 頂点生成
                    //===============================
                    var min = new Vector2(halfScreenWidth - halfWidth, startPos);
                    var max = new Vector2(
                        halfScreenWidth + halfWidth,
                        startPos + markerCanvasHeight / NoteCanvas.ScaleFactor.Value);

                    drawData = new Geometry(
                        new[]
                        {
                            new Vector3(min.x, max.y, 0),
                            new Vector3(max.x, max.y, 0),
                            new Vector3(max.x, min.y, 0),
                            new Vector3(min.x, min.y, 0)
                        },
                        markerColor);

                    //===============================
                    // スライダー側のマーカーサイズ（縦方向）
                    //===============================
                    var sliderMarkerSize = sliderMarker.sizeDelta;
                    sliderMarkerSize.y = sliderHeight * markerCanvasHeight / NoteCanvas.Height.Value;
                    sliderMarker.sizeDelta = sliderMarkerSize;

                    //===============================
                    // スライダー側のマーカー位置（縦方向）
                    //===============================
                    if (NoteCanvas.Height.Value > 0)
                    {
                        var startPer =
                            (start.Position.Value - ConvertUtils.SamplesToCanvasPositionY(0))
                            / NoteCanvas.Height.Value;

                        var sliderMarkerPos = sliderMarker.localPosition;
                        sliderMarkerPos.y = sliderHeight * startPer - sliderHeight / 2f;
                        sliderMarker.localPosition = sliderMarkerPos;
                    }
                });
        }

        /// <summary>
        /// GL 描画（マーカー矩形）を毎フレーム描画します。
        /// </summary>
        void LateUpdate()
        {
            GLQuadDrawer.Draw(drawData);
        }
    }
}
