// ========================================
//
// NoteMaker Project
//
// ========================================
//
// BPMSpinBoxPresenter.cs
// ノーツ編集における BPM（EditData.BPM）を調整する
// スピンボックス UI のプレゼンターです。
// SpinBoxPresenterBase が提供する共通 UI ロジックに対して、
// このクラスは「どの値を操作するか」だけを指定します。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// BPM（EditData.BPM）を操作するスピンボックスの Presenter です。
    /// ・SpinBoxPresenterBase の UI 操作で BPM を増減  
    /// ・ReactiveProperty を返すだけで自動連動  
    /// </summary>
    public class BPMSpinBoxPresenter : SpinBoxPresenterBase
    {
        [SerializeField] Button tapButton = default; // タップでBPMを推定するボタン

        // Tap BPM用の変数
        List<float> tapTimes = new List<float>();
        const float tapTimeout = 3.0f; // 3秒以上間隔が空いたらリセット
        const int maxTaps = 8; // 最大8タップまでの平均を取る

        void Start()
        {
            if (tapButton != null)
            {
                tapButton.OnClickAsObservable().Subscribe(_ => OnTapButtonClicked()).AddTo(this);
            }
        }

        /// <summary>
        /// SpinBoxPresenterBase が操作する対象の ReactiveProperty を返します。
        /// </summary>
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.BPM;
        }

        void OnTapButtonClicked()
        {
            float currentTime = Time.realtimeSinceStartup;

            // タイムアウト or 逆行でリセット
            if (tapTimes.Count > 0)
            {
                float last = tapTimes[tapTimes.Count - 1];

                if (currentTime - last > tapTimeout)
                {
                    tapTimes.Clear();
                }
                else if (currentTime < last)
                {
                    tapTimes.Clear();
                }
            }

            // タップ記録
            tapTimes.Add(currentTime);

            // 最大数制限
            if (tapTimes.Count > maxTaps)
                tapTimes.RemoveAt(0);

            // 2回以上で計算
            if (tapTimes.Count >= 2)
            {
                float sum = 0f;
                for (int i = 1; i < tapTimes.Count; i++)
                {
                    float interval = tapTimes[i] - tapTimes[i - 1];

                    // 1000 BPM 以上は無視（誤タップ）
                    if (interval < 0.06f)
                        continue;

                    sum += interval;
                }

                float avg = sum / (tapTimes.Count - 1);
                if (avg > 0f)
                {
                    int bpm = Mathf.RoundToInt(60f / avg);
                    GetReactiveProperty().Value = bpm;
                }
            }
        }

    }
}
