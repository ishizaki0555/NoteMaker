// ========================================
//
// SmoothedTimeSamplesPresenter.cs
//
// ========================================
//
// 再生中の timeSamples を滑らかに補間し、
// 波形描画などで使用する SmoothedTimeSamples を更新するクラス。
// 再生中は補間値を加算し、停止中は Audio.TimeSamples に同期させる。
//
// ========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class SmoothedTimeSamplesPresenter : MonoBehaviour
    {
        void Awake()
        {
            var prevFrameSamples = 0f; // 前フレームの補間サンプル値
            var counter = 0;           // 補間方式切り替え用カウンタ

            // 再生中は補間しながら SmoothedTimeSamples を更新
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Where(_ => Audio.IsPlaying.Value)
                .Subscribe(_ =>
                {
                    // 100フレームに1回は実サンプル差分を使用し、それ以外は deltaTime から計算
                    var deltaSamples = counter == 0
                        ? (Audio.Source.timeSamples - prevFrameSamples)
                        : Audio.Source.clip.frequency * Time.deltaTime;

                    Audio.SmoothedTimeSamples.Value += deltaSamples;
                    prevFrameSamples = Audio.SmoothedTimeSamples.Value;

                    counter = ++counter % 100;
                });

            // 停止中は timeSamples に完全同期
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
