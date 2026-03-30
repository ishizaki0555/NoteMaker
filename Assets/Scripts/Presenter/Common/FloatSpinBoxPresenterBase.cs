// ========================================
//
// NoteMaker Project
//
// ========================================
//
// FloatSpinBoxPresenterBase.cs
// 数値入力 UI（InputField + 増減ボタン）を扱うための抽象プレゼンター（小数対応版）です。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Utility;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public abstract class FloatSpinBoxPresenterBase : MonoBehaviour
    {
        [SerializeField] InputField inputField = default;
        [SerializeField] Button increaseButton = default;
        [SerializeField] Button decreaseButton = default;
        [SerializeField] float valueStep = 1f;
        [SerializeField] float minValue = 1f;
        [SerializeField] float maxValue = 500f;
        [SerializeField] int longPressTriggerMilliseconds = 500;
        [SerializeField] int continuousPressIntervalMilliseconds = 100;

        Subject<float> operateSpinButtonObservable = new Subject<float>(); // ボタン操作イベント

        protected abstract ReactiveProperty<float> GetReactiveProperty();

        void Awake()
        {
            if (increaseButton != null)
            {
                increaseButton.AddListener(EventTriggerType.PointerUp, _ => operateSpinButtonObservable.OnNext(0f));
                increaseButton.AddListener(EventTriggerType.PointerDown, _ => operateSpinButtonObservable.OnNext(valueStep));
            }

            if (decreaseButton != null)
            {
                decreaseButton.AddListener(EventTriggerType.PointerUp, _ => operateSpinButtonObservable.OnNext(0f));
                decreaseButton.AddListener(EventTriggerType.PointerDown, _ => operateSpinButtonObservable.OnNext(-valueStep));
            }

            var property = GetReactiveProperty();

            // property が null の場合はここでエラーになるのを防ぐ
            if (property == null) return;

            // ReactiveProperty → InputField
            property.Subscribe(x => { 
                if (inputField != null && !inputField.isFocused)
                    inputField.text = x.ToString("G"); 
            }).AddTo(this);

            var isUndoRedoAction = false;

            if (inputField != null)
            {
                // UIからフォーカスが外れたタイミングでパースし直して正しい値を入力する
                inputField.onEndEdit.AsObservable()
                    .Select(x => float.TryParse(x, out float result) ? result : property.Value)
                    .Subscribe(x => {
                        float clamped = Mathf.Clamp(x, minValue, maxValue);
                        if (property.Value != clamped)
                        {
                            var prev = property.Value;
                            EditCommandManager.Do(
                                new Command(
                                    () => property.Value = clamped,
                                    () => { isUndoRedoAction = true; property.Value = prev; },
                                    () => { isUndoRedoAction = true; property.Value = clamped; }
                                ));
                        }
                        else
                        {
                            inputField.text = clamped.ToString("G");
                        }
                    }).AddTo(this);
            }

            // ボタンからの入力更新
            var updateValueFromSpinButtonStream = operateSpinButtonObservable
                .Throttle(TimeSpan.FromMilliseconds(longPressTriggerMilliseconds))
                .Where(delta => delta != 0f)
                .SelectMany(delta =>
                    Observable.Interval(TimeSpan.FromMilliseconds(continuousPressIntervalMilliseconds))
                        .TakeUntil(operateSpinButtonObservable.Where(d => d == 0f))
                        .Select(_ => delta))
                .Merge(operateSpinButtonObservable.Where(d => d != 0f))
                .Select(delta => property.Value + delta);

            updateValueFromSpinButtonStream
                .Select(x => Mathf.Clamp(x, minValue, maxValue))
                .DistinctUntilChanged()
                .Where(_ => isUndoRedoAction ? (isUndoRedoAction = false) : true)
                .Select(x => new { current = x, prev = property.Value })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => property.Value = x.current,
                            () => { isUndoRedoAction = true; property.Value = x.prev; },
                            () => { isUndoRedoAction = true; property.Value = x.current; }
                        )))
                .AddTo(this);
        }
    }
}
