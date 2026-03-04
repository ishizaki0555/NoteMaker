// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SpinBoxPresenterBase.cs
// 数値入力 UI（InputField + 増減ボタン）を扱うための抽象プレゼンターです。
// ボタン長押し・連続入力・直接入力・Undo/Redo 対応など、
// SpinBox に必要な共通ロジックをすべてまとめています。
// 
//========================================

using NoteMaker.Common;
using NoteMaker.Utility;
using System;
using System.Text.RegularExpressions;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// SpinBox（数値入力 UI）の共通処理を提供する抽象クラスです。
    /// ・InputField の直接入力  
    /// ・増減ボタンのクリック / 長押し / 連続入力  
    /// ・値のクランプ（min / max）  
    /// ・Undo / Redo 対応  
    /// 
    /// 派生クラスは GetReactiveProperty() を実装し、
    /// 対応する ReactiveProperty<int> を返すだけで利用できます。
    /// </summary>
    public abstract class SpinBoxPresenterBase : MonoBehaviour
    {
        [SerializeField] InputField inputField = default;   // 数値入力フィールド
        [SerializeField] Button increaseButton = default;   // 増加ボタン
        [SerializeField] Button decreaseButton = default;   // 減少ボタン
        [SerializeField] int valueStep = default;           // 増減ステップ値
        [SerializeField] int minValue = default;            // 最小値
        [SerializeField] int maxValue = default;            // 最大値
        [SerializeField] int longPressTriggerMilliseconds = default;       // 長押し判定までの時間
        [SerializeField] int continuousPressIntervalMilliseconds = default; // 長押し中の連続入力間隔

        Subject<int> operateSpinButtonObservable = new Subject<int>(); // ボタン操作イベント

        /// <summary>
        /// 派生クラスが対象の ReactiveProperty<int> を返す。
        /// </summary>
        protected abstract ReactiveProperty<int> GetReactiveProperty();

        /// <summary>
        /// 初期化処理でボタンイベントの設定や、ReactiveProperty と InputField の双方向バインディングを行います。
        /// </summary>
        void Awake()
        {
            //===============================
            // ボタン押下イベント設定
            //===============================
            increaseButton.AddListener(EventTriggerType.PointerUp, _ => operateSpinButtonObservable.OnNext(0));
            decreaseButton.AddListener(EventTriggerType.PointerUp, _ => operateSpinButtonObservable.OnNext(0));
            increaseButton.AddListener(EventTriggerType.PointerDown, _ => operateSpinButtonObservable.OnNext(valueStep));
            decreaseButton.AddListener(EventTriggerType.PointerDown, _ => operateSpinButtonObservable.OnNext(-valueStep));

            var property = GetReactiveProperty();

            // ReactiveProperty → InputField
            property.Subscribe(x => inputField.text = x.ToString());

            // InputField → 値更新
            var updateValueFromInputFieldStream = inputField.OnValueChangedAsObservable()
                .Where(x => Regex.IsMatch(x, @"^[0-9]+$"))
                .Select(x => int.Parse(x));

            // ボタン長押し → 連続入力
            var updateValueFromSpinButtonStream = operateSpinButtonObservable
                .Throttle(TimeSpan.FromMilliseconds(longPressTriggerMilliseconds))
                .Where(delta => delta != 0)
                .SelectMany(delta =>
                    Observable.Interval(TimeSpan.FromMilliseconds(continuousPressIntervalMilliseconds))
                        .TakeUntil(operateSpinButtonObservable.Where(d => d == 0))
                        .Select(_ => delta))
                .Merge(operateSpinButtonObservable.Where(d => d != 0))
                .Select(delta => property.Value + delta);

            // Undo / Redo 対応
            var isUndoRedoAction = false;

            Observable.Merge(
                    updateValueFromSpinButtonStream,
                    updateValueFromInputFieldStream)
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
