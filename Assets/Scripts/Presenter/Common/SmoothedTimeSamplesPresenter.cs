// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SmoothedTimeSamplesPresenter.cs
// Audio.Source.timeSamples の生値をそのまま使うとフレーム間で揺れが大きいため、
// 「SmoothedTimeSamples」として滑らかに補間したサンプル位置を生成する
// プレゼンタークラスです。
// 再生中はフレームごとの増加量を推定し、停止中は timeSamples に同期します。
// 
//========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 再生位置を滑らかに補間した SmoothedTimeSamples を更新するクラスです。
    /// ・再生中は前フレームとの差分または周波数×ΔTime で増加量を推定  
    /// ・停止中は Audio.TimeSamples に完全同期  
    /// ・スクロールや UI 描画で滑らかな動きを実現  
    /// </summary>
    public class SmoothedTimeSamplesPresenter : MonoBehaviour
    {
        void Awake()
        {
            var prevFrameSamples = 0f; // 前フレームの SmoothedTimeSamples
            var counter = 0;           // 差分方式と ΔTime 方式を切り替えるカウンタ

            //===============================
            // 再生中：SmoothedTimeSamples を滑らかに更新
            //===============================
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Where(_ => Audio.IsPlaying.Value)
                .Subscribe(_ =>
                {
                    // 0フレーム目は timeSamples の差分を使う
                    // それ以外は frequency * deltaTime を使って滑らかに進める
                    var deltaSamples = counter == 0
                        ? (Audio.Source.timeSamples - prevFrameSamples)
                        : Audio.Source.clip.frequency * Time.deltaTime;

                    Audio.SmoothedTimeSamples.Value += deltaSamples;
                    prevFrameSamples = Audio.SmoothedTimeSamples.Value;

                    // 180 フレームごとに差分方式へ戻す
                    counter = ++counter % 180;
                });

            //===============================
            // 停止中：SmoothedTimeSamples を timeSamples に同期
            //===============================
            Audio.TimeSamples
                .Where(_ => Audio.Source.clip != null)
                .Where(_ => !Audio.IsPlaying.Value)
                .Subscribe(timeSamples =>
                {
                    counter = 0;
                    Audio.SmoothedTimeSamples.Value = timeSamples;
                    prevFrameSamples = timeSamples;
                });
        }
    }
}
