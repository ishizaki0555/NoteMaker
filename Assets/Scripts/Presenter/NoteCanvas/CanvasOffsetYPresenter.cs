using NoteMaker.Common;
using NoteMaker.Model;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class CanvasOffsetYPresenter : MonoBehaviour
    {
        [SerializeField]
        CanvasEvents canvasEvents = default;
        [SerializeField]
        RectTransform horizontalLineRect = default;   // ← 縦向きなので水平ライン
        [SerializeField]
        RectTransform waveformRenderImage = default;

        void Awake()
        {
            // Initialize canvas offset Y
            Audio.OnLoad.Subscribe(_ =>
                NoteCanvas.OffsetY.Value = -Screen.height * 0.45f * NoteCanvas.ScaleFactor.Value);

            var operateCanvasOffsetYObservable = this.UpdateAsObservable()
                .SkipUntil(canvasEvents.VerticalLineOnMouseDownObservable) // ← 必要ならイベント名も変更
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition.y) // ← X → Y に変更
                .Buffer(2, 1).Where(b => 2 <= b.Count)
                .RepeatSafe()
                .Select(b => (b[1] - b[0]) * NoteCanvas.ScaleFactor.Value)
                .Select(y => y + NoteCanvas.OffsetY.Value)
                .Select(y => new { y, max = Screen.height * 0.5f * 0.95f * NoteCanvas.ScaleFactor.Value })
                .Select(v => Mathf.Clamp(v.y, -v.max, v.max))
                .DistinctUntilChanged();

            operateCanvasOffsetYObservable.Subscribe(y => NoteCanvas.OffsetY.Value = y);

            operateCanvasOffsetYObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => 2 <= b.Count)
                .Select(x => new { current = x.Last(), prev = x.First() })
                .Subscribe(x => EditCommandManager.Do(
                    new Command(
                        () => NoteCanvas.OffsetY.Value = x.current,
                        () => NoteCanvas.OffsetY.Value = x.prev)));

            NoteCanvas.OffsetY.Subscribe(y =>
            {
                var pos = horizontalLineRect.localPosition;
                var pos2 = waveformRenderImage.localPosition;

                pos.y = pos2.y = y;

                horizontalLineRect.localPosition = pos;
                waveformRenderImage.localPosition = pos2;
            });
        }
    }
}
