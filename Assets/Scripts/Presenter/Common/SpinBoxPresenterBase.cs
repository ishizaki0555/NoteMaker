// ========================================
//
// SpinBoxPresenterBase.cs
//
// ========================================
//
// 数値入力用スピンボックス（InputField + 増減ボタン）の共通処理を提供する抽象クラス。
// ・ボタン押下（単押し / 長押し）
// ・テキスト入力
// ・値のクランプ
// ・Undo / Redo 対応
// を UniRx を用いて実装する。
//
// ========================================

using NoteMaker.Common;
using System;
using System.Text.RegularExpressions;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public abstract class SpinBoxPresenterBase : MonoBehaviour
    {
        [SerializeField] InputField inputField = default;                   // 数値入力フィールド
        [SerializeField] Button increaseButton = default;                   // 増加ボタン
        [SerializeField] Button decreaseButton = default;                   // 減少ボタン
        [SerializeField] int valueStep = default;                           // 増減ステップ
        [SerializeField] int minValue = default;                            // 最小値
        [SerializeField] int maxValue = default;                            // 最大値
        [SerializeField] int longPressTriggerMilliseconds = default;        // 長押し判定時間
        [SerializeField] int continuousPressIntervalMilliseconds = default; // 長押し時の連続入力間隔

        Subject<int> _operateSpinButtonObservable = new Subject<int>();     // ボタン操作ストリーム（+step / -step / 0）

        /// <summary>
        /// 派生クラスがバインドする ReactiveProperty を返す。
        /// </summary>
        protected abstract ReactiveProperty<int> GetReactiveProperty();

        void Awake()
        {
            // -----------------------------
            // ボタン押下イベントの登録
            // -----------------------------
            increaseButton.AddListener(EventTriggerType.PointerUp, e => _operateSpinButtonObservable.OnNext(0));
            decreaseButton.AddListener(EventTriggerType.PointerUp, e => _operateSpinButtonObservable.OnNext(0));
            increaseButton.AddListener(EventTriggerType.PointerDown, e => _operateSpinButtonObservable.OnNext(valueStep));
            decreaseButton.AddListener(EventTriggerType.PointerDown, e => _operateSpinButtonObservable.OnNext(-valueStep));

            var property = GetReactiveProperty();

            // ReactiveProperty → InputField
            property.Subscribe(x => inputField.text = x.ToString());

            // -----------------------------
            // InputField からの値更新
            // -----------------------------
            var updateValueFromInputFieldStream = inputField.OnValueChangedAsObservable()
                .Where(x => Regex.IsMatch(x, @"^[0-9]+$")) // 数字のみ
                .Select(x => int.Parse(x));

            // -----------------------------
            // ボタン操作からの値更新
            // -----------------------------
            var updateValueFromSpinButtonStream = _operateSpinButtonObservable
                // 長押し判定
                .Throttle(TimeSpan.FromMilliseconds(longPressTriggerMilliseconds))
                .Where(delta => delta != 0)
                // 長押し中は一定間隔で連続入力
                .SelectMany(delta =>
                    Observable.Interval(TimeSpan.FromMilliseconds(continuousPressIntervalMilliseconds))
                        .TakeUntil(_operateSpinButtonObservable.Where(d => d == 0))
                        .Select(_ => delta))
                // 単押しも含める
                .Merge(_operateSpinButtonObservable.Where(d => d != 0))
                .Select(delta => property.Value + delta);

            var isUndoRedoAction = false;

            // -----------------------------
            // 値更新（InputField + ボタン）
            // -----------------------------
            Observable.Merge(
                    updateValueFromSpinButtonStream,
                    updateValueFromInputFieldStream)
                .Select(x => Mathf.Clamp(x, minValue, maxValue)) // 範囲制限
                .DistinctUntilChanged()
                .Where(_ => isUndoRedoAction ? (isUndoRedoAction = false) : true)
                .Select(x => new { current = x, prev = property.Value })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => property.Value = x.current,
                            () => { isUndoRedoAction = true; property.Value = x.prev; },
                            () => { isUndoRedoAction = true; property.Value = x.current; })))
                .AddTo(this);
        }
    }
}
