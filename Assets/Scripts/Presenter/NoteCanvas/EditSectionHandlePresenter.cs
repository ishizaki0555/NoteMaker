// ========================================
//
// NoteMaker Project
//
// ========================================
//
// EditSectionHandlePresenter.cs
// セクション境界ハンドル（縦ライン）をドラッグして位置（サンプル値）を変更する
// プレゼンターです。ドラッグ開始 → Canvas 座標 → サンプル値変換 → Clamp →
// Undo/Redo 対応 → UI 反映、という一連の流れを管理します。
// 
//========================================

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
    /// <summary>
    /// セクション境界ハンドル（縦ライン）の位置を管理するクラスです。
    /// ・ドラッグ開始位置の取得  
    /// ・Canvas 座標 → サンプル値への変換  
    /// ・サンプル値の Clamp  
    /// ・Undo / Redo 対応  
    /// ・ライン位置の UI 反映  
    /// </summary>
    public class EditSectionHandlePresenter : MonoBehaviour
    {
        [SerializeField] Image handleImage = default;                 // ハンドル画像
        [SerializeField] RectTransform lineRectTransform = default;   // 縦ライン RectTransform

        ReactiveProperty<int> CurrentSamples = new ReactiveProperty<int>(0); // 現在のサンプル位置
        ReactiveProperty<float> position_ = new ReactiveProperty<float>();   // ラインの Y 座標

        public ReactiveProperty<float> Position => position_;

        RectTransform handleRectTransform_;
        public RectTransform HandleRectTransform =>
            handleRectTransform_ ?? (handleRectTransform_ = handleImage.GetComponent<RectTransform>());

        void Start()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());

            // ラインの Y 座標を監視
            position_ = lineRectTransform
                .ObserveEveryValueChanged(rect => rect.localPosition.y)
                .ToReactiveProperty();
        }

        /// <summary>
        /// ドラッグ操作・Undo/Redo・UI 反映のストリームを構築します。
        /// </summary>
        void Init()
        {
            var handlerOnMouseDownObservable = new Subject<Vector3>();

            //===============================
            // ドラッグ開始（PointerDown）
            //===============================
            handleImage.AddListener(
                EventTriggerType.PointerDown,
                (e) =>
                {
                    handlerOnMouseDownObservable.OnNext(
                        Vector3.up * ConvertUtils.SamplesToCanvasPositionY(CurrentSamples.Value));
                });

            //===============================
            // ドラッグ中のサンプル値更新
            //===============================
            var operateHandleObservable = this.UpdateAsObservable()
                .SkipUntil(handlerOnMouseDownObservable)          // ドラッグ開始
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))       // ドラッグ終了まで
                .RepeatSafe()
                .Select(_ => ConvertUtils.ScreenToCanvasPosition(Input.mousePosition))
                .Select(canvasPos => ConvertUtils.CanvasPositionYToSamples(canvasPos.y)) // Canvas → Samples
                .Select(samples => Mathf.Clamp(samples, 0, Audio.Source.clip.samples))   // 範囲制限
                .DistinctUntilChanged();

            operateHandleObservable.Subscribe(samples => CurrentSamples.Value = samples);

            //===============================
            // Undo / Redo 対応
            //===============================
            operateHandleObservable
                .Buffer(this.UpdateAsObservable().Where(_ => Input.GetMouseButtonUp(0)))
                .Where(b => b.Count >= 2)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => CurrentSamples.Value = x.current,
                            () => CurrentSamples.Value = x.prev)));

            //===============================
            // UI 反映（縦ライン位置更新）
            //===============================
            Observable.Merge(
                    CurrentSamples.AsUnitObservable(),
                    NoteCanvas.OffsetY.AsUnitObservable(),
                    Audio.SmoothedTimeSamples.AsUnitObservable(),
                    NoteCanvas.Height.AsUnitObservable(),
                    EditData.OffsetSamples.AsUnitObservable())
                .Select(_ => CurrentSamples.Value)
                .Subscribe(samples =>
                {
                    var pos = lineRectTransform.localPosition;
                    pos.y = ConvertUtils.SamplesToCanvasPositionY(samples);
                    lineRectTransform.localPosition = pos;
                });
        }
    }
}
