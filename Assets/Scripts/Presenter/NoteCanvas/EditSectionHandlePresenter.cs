// ========================================
//
// EditSectionHandlePresenter.cs
//
// ========================================
//
// セクション境界（区切り線）のドラッグ操作を管理する Presenter。
// ・ドラッグ開始位置の取得
// ・ドラッグ中のサンプル位置更新
// ・Undo / Redo 対応
// ・キャンバス座標 / サンプル値の相互変換
// ・OffsetX / Width / SmoothedTimeSamples などの変化に応じた位置更新
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Presenter;
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
        [SerializeField] Image handleImage = default;                           // ドラッグ可能なハンドル
        [SerializeField] RectTransform lineRectTransform = default;             // セクション境界線

        ReactiveProperty<int> CurrentSamples = new ReactiveProperty<int>(0);    // 現在のサンプル位置
        ReactiveProperty<float> position_ = new ReactiveProperty<float>();      // 境界線の X 座標

        public ReactiveProperty<float> Position => position_;

        RectTransform handleRectTransform_;
        public RectTransform HandleRectTransform =>
            handleRectTransform_ ?? (handleRectTransform_ = handleImage.GetComponent<RectTransform>());

        void Start()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());

            // lineRectTransform の X 座標を監視して Position に反映
            position_ = lineRectTransform
                .ObserveEveryValueChanged(rect => rect.localPosition.x)
                .ToReactiveProperty();
        }

        /// <summary>
        /// セクション境界ドラッグ処理の初期化。
        /// </summary>
        void Init()
        {
            var handlerOnMouseDownObservable = new Subject<Vector3>();

            // ----------------------------------------
            // ハンドル押下時の処理
            // ----------------------------------------
            handleImage.AddListener(
                EventTriggerType.PointerDown,
                (e) =>
                {
                    handlerOnMouseDownObservable.OnNext(
                        Vector3.right * ConvertUtils.SamplesToCanvasPositionX(CurrentSamples.Value));
                });

            // ----------------------------------------
            // ドラッグ中のサンプル位置更新
            // ----------------------------------------
            var operateHandleObservable = this.UpdateAsObservable()
                .SkipUntil(handlerOnMouseDownObservable)          // ドラッグ開始
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))       // ドラッグ終了まで
                .RepeatSafe()
                .Select(_ => ConvertUtils.ScreenToCanvasPosition(Input.mousePosition))
                .Select(canvasPos => ConvertUtils.CanvasPositionXToSamples(canvasPos.x))
                .Select(samples => Mathf.Clamp(samples, 0, Audio.Source.clip.samples))
                .DistinctUntilChanged();

            // サンプル値更新
            operateHandleObservable.Subscribe(samples => CurrentSamples.Value = samples);

            // ----------------------------------------
            // Undo / Redo 用の履歴登録
            // ----------------------------------------
            operateHandleObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => 2 <= b.Count)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => CurrentSamples.Value = x.current,
                            () => CurrentSamples.Value = x.prev)));

            // ----------------------------------------
            // サンプル値 / OffsetX / Width / SmoothedTimeSamples などの変化に応じて境界線位置を更新
            // ----------------------------------------
            Observable.Merge(
                    CurrentSamples.AsUnitObservable(),
                    NoteCanvas.OffsetX.AsUnitObservable(),
                    Audio.SmoothedTimeSamples.AsUnitObservable(),
                    NoteCanvas.Width.AsUnitObservable(),
                    EditData.OffsetSamples.AsUnitObservable())
                .Select(_ => CurrentSamples.Value)
                .Subscribe(samples =>
                {
                    var pos = lineRectTransform.localPosition;
                    pos.x = ConvertUtils.SamplesToCanvasPositionX(samples);
                    lineRectTransform.localPosition = pos;
                });
        }
    }
}
