using NoteMaker.GLDrawing;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class EditSectionPresenter : MonoBehaviour
    {
        [SerializeField] RectTransform markerRect = default;
        [SerializeField] EditSectionHandlePresenter point1 = default;
        [SerializeField] EditSectionHandlePresenter point2 = default;
        [SerializeField] RectTransform playbackPositionSliderRectTransform = default;
        [SerializeField] RectTransform sliderMarker = default;
        [SerializeField] Color markerColor = default;

        Geometry drawData = new Geometry(Enumerable.Range(0, 4).Select(_ => Vector3.zero).ToArray(), Color.clear);

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        void Init()
        {
            var sliderHeight = playbackPositionSliderRectTransform.sizeDelta.y;

            Observable.Merge(point1.Position, point2.Position)
                .Subscribe(_ =>
                {
                    var sortedPoints = new[] { point1, point2 }.OrderBy(p => p.Position.Value);
                    var start = sortedPoints.First();
                    var end = sortedPoints.Last();

                    // ★ ハンドルの向き（縦向きなので Y 方向）
                    var scaleStart = start.HandleRectTransform.localScale;
                    scaleStart.y = -1;
                    start.HandleRectTransform.localScale = scaleStart;

                    var scaleEnd = end.HandleRectTransform.localScale;
                    scaleEnd.y = 1;
                    end.HandleRectTransform.localScale = scaleEnd;

                    // ★ マーカーの高さ（Y方向）
                    var markerCanvasHeight = end.Position.Value - start.Position.Value;

                    // ★ Canvas → Screen（縦向き）
                    var startPos = start.Position.Value / NoteCanvas.ScaleFactor.Value + Screen.height / 2f;

                    var halfScreenWidth = Screen.width / 2f;
                    var halfWidth = markerRect.sizeDelta.x / NoteCanvas.ScaleFactor.Value / 2;

                    // ★ 縦向き矩形（上下に伸びる）
                    var min = new Vector2(halfScreenWidth - halfWidth, startPos);
                    var max = new Vector2(halfScreenWidth + halfWidth, startPos + markerCanvasHeight / NoteCanvas.ScaleFactor.Value);

                    drawData = new Geometry(
                        new[] {
                            new Vector3(min.x, max.y, 0),
                            new Vector3(max.x, max.y, 0),
                            new Vector3(max.x, min.y, 0),
                            new Vector3(min.x, min.y, 0)
                        },
                        markerColor);

                    // ★ スライダーのサイズ（縦方向）
                    var sliderMarkerSize = sliderMarker.sizeDelta;
                    sliderMarkerSize.y = sliderHeight * markerCanvasHeight / NoteCanvas.Height.Value;
                    sliderMarker.sizeDelta = sliderMarkerSize;

                    // ★ スライダーの位置（縦方向）
                    if (NoteCanvas.Height.Value > 0)
                    {
                        var startPer = (start.Position.Value - ConvertUtils.SamplesToCanvasPositionY(0)) / NoteCanvas.Height.Value;
                        var sliderMarkerPos = sliderMarker.localPosition;
                        sliderMarkerPos.y = sliderHeight * startPer - sliderHeight / 2f;
                        sliderMarker.localPosition = sliderMarkerPos;
                    }
                });
        }

        void LateUpdate()
        {
            GLQuadDrawer.Draw(drawData);
        }
    }
}
