using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class EditSectionHandlePresenter : MonoBehaviour
    {
        [SerializeField] Image handleImage = default;
        [SerializeField] RectTransform lineRectTransform = default;

        ReactiveProperty<int> CurrentSamples = new ReactiveProperty<int>(0);
        ReactiveProperty<float> position_ = new ReactiveProperty<float>();

        public ReactiveProperty<float> Position => position_;

        public RectTransform HandleRectTransform =>
            handleRectTransform_ ?? (handleRectTransform_ = handleImage.GetComponent<RectTransform>());
        RectTransform handleRectTransform_;

        void Start()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());

            // ★ 縦向きなので Y を監視
            position_ = lineRectTransform
                .ObserveEveryValueChanged(rect => rect.localPosition.y)
                .ToReactiveProperty();
        }

        void Init()
        {
            var handlerOnMouseDownObservable = new Subject<Vector3>();

            handleImage.AddListener(
                EventTriggerType.PointerDown,
                (e) =>
                {
                    // ★ ドラッグ開始位置（Y）
                    handlerOnMouseDownObservable.OnNext(
                        Vector3.up * ConvertUtils.SamplesToCanvasPositionY(CurrentSamples.Value));
                });

            var operateHandleObservable = this.UpdateAsObservable()
                .SkipUntil(handlerOnMouseDownObservable)
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))
                .RepeatSafe()
                .Select(_ => ConvertUtils.ScreenToCanvasPosition(Input.mousePosition))
                // ★ Canvas Y → Samples
                .Select(canvasPos => ConvertUtils.CanvasPositionYToSamples(canvasPos.y))
                .Select(samples => Mathf.Clamp(samples, 0, Audio.Source.clip.samples))
                .DistinctUntilChanged();

            operateHandleObservable.Subscribe(samples => CurrentSamples.Value = samples);

            operateHandleObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => 2 <= b.Count)
                .Select(x => new { current = x.Last(), prev = x.First() })
                .Subscribe(x => EditCommandManager.Do(
                    new Command(
                        () => CurrentSamples.Value = x.current,
                        () => CurrentSamples.Value = x.prev)));

            // ★ Y方向の更新に対応
            Observable.Merge(
                    CurrentSamples.AsUnitObservable(),
                    NoteCanvas.OffsetY.AsUnitObservable(),
                    Audio.SmoothedTimeSamples.AsUnitObservable(),
                    NoteCanvas.Height.AsUnitObservable(),
                    EditData.OffsetSamples.AsUnitObservable())
                .Select(_ => CurrentSamples.Value)
                .Subscribe(x =>
                {
                    var pos = lineRectTransform.localPosition;
                    pos.y = ConvertUtils.SamplesToCanvasPositionY(x);
                    lineRectTransform.localPosition = pos;
                });
        }
    }
}
