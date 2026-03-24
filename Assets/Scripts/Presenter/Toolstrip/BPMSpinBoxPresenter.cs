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

using NoteMaker.Model;
using NoteMaker.Common;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class BPMSpinBoxPresenter : SpinBoxPresenterBase
    {
        [SerializeField] Button tapButton = default;

        void Start()
        {
            if (tapButton != null)
            {
                tapButton.OnClickAsObservable()
                    .Subscribe(_ => OnTapButtonClicked())
                    .AddTo(this);
            }
        }

        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.BPM;
        }

        void OnTapButtonClicked()
        {
            if (Audio.Source == null || Audio.Source.clip == null)
                return;

            var clip = Audio.Source.clip;

            // --- メインスレッドで clip から生データを取り出す ---
            int sampleRate = clip.frequency;
            int channels = clip.channels;

            // 解析する最大秒数（先頭から）
            const float maxAnalyzeSeconds = 10f;
            int maxSamples = Mathf.Min(clip.samples, Mathf.FloorToInt(maxAnalyzeSeconds * sampleRate));
            if (maxSamples <= 0) return;

            // raw バッファをメインスレッドで取得（GetData はメインスレッド必須）
            float[] raw = new float[maxSamples * channels];
            clip.GetData(raw, 0);

            // --- raw, sampleRate, channels を別スレッドで解析 ---
            Observable.Start(() =>
                {
                    float frameRate;
                    float[] onset = ComputeOnsetEnvelope(raw, channels, sampleRate, out frameRate);
                    return EstimateBpmFromOnsetByFFT(onset, frameRate);
                })
                .ObserveOnMainThread()
                .Subscribe(bpm =>
                {
                    Debug.Log("Estimated BPM: " + bpm);
                    if (bpm > 0)
                        GetReactiveProperty().Value = Mathf.RoundToInt(bpm);
                }, ex =>
                {
                    Debug.LogException(ex);
                })
                .AddTo(this);
        }

        float[] ComputeOnsetEnvelope(float[] raw, int channels, int sampleRate, out float frameRate)
        {
            frameRate = 100f; // 100 Hz resolution is common for BPM estimation
            int samplesPerFrame = Mathf.RoundToInt(sampleRate / frameRate);
            int frameCount = (raw.Length / channels) / samplesPerFrame;
            float[] onset = new float[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                float sum = 0f;
                int startIdx = i * samplesPerFrame * channels;
                int endIdx = startIdx + samplesPerFrame * channels;

                if (endIdx > raw.Length) endIdx = raw.Length;

                for (int j = startIdx; j < endIdx; j++)
                {
                    float sample = raw[j];
                    sum += sample * sample; // Energy
                }

                int count = endIdx - startIdx;
                if (count > 0)
                    onset[i] = Mathf.Sqrt(sum / count);
            }

            // Compute positive difference to isolate "attacks" (onsets)
            float[] diff = new float[frameCount];
            for (int i = 1; i < frameCount; i++)
            {
                diff[i] = Mathf.Max(0f, onset[i] - onset[i - 1]);
            }
            
            // Normalize the onset envelope for better FFT precision
            float maxDiff = 0f;
            for (int i = 0; i < frameCount; i++)
            {
                if (diff[i] > maxDiff) maxDiff = diff[i];
            }
            if (maxDiff > 0f)
            {
                for (int i = 0; i < frameCount; i++) diff[i] /= maxDiff;
            }

            return diff;
        }

        // onset: 正規化済みオンセット包絡（長さ N）
        // frameRate: onset のフレームレート（Hz）
        // minBPM, maxBPM: 検出範囲
        float EstimateBpmFromOnsetByFFT(float[] onset, float frameRate, int minBPM = 40, int maxBPM = 240)
        {
            int N = onset.Length;
            if (N < 4) return 0f;

            // 1) zero-pad to next power of two for FFT convolution
            int convSize = 1;
            while (convSize < N * 2) convSize <<= 1;

            // 2) prepare arrays for FFT (real -> complex)
            Complex[] a = new Complex[convSize];
            Complex[] b = new Complex[convSize];
            for (int i = 0; i < N; i++) a[i] = new Complex(onset[i], 0f);
            for (int i = N; i < convSize; i++) a[i] = new Complex(0f, 0f);

            // b = reversed onset, zero padded
            for (int i = 0; i < N; i++) b[i] = new Complex(onset[N - 1 - i], 0f);
            for (int i = N; i < convSize; i++) b[i] = new Complex(0f, 0f);

            // 3) FFT both, multiply, inverse FFT -> convolution
            FFT.FFTInPlace(a);
            FFT.FFTInPlace(b);
            for (int i = 0; i < convSize; i++) a[i] = a[i] * b[i];
            FFT.IFFTInPlace(a);

            // 4) convolution result: real parts contain cross-correlation; autocorr at lag L is conv[N-1 + L]
            int minLag = Mathf.Max(1, Mathf.FloorToInt(frameRate * 60f / maxBPM));
            int maxLag = Mathf.Min(N - 1, Mathf.CeilToInt(frameRate * 60f / minBPM));
            if (minLag >= maxLag) return 0f;

            float bestVal = 0f;
            int bestLag = 0;
            for (int lag = minLag; lag <= maxLag; lag++)
            {
                int idx = (N - 1) + lag;
                if (idx >= 0 && idx < convSize)
                {
                    float val = a[idx].re; // real part
                    if (val > bestVal)
                    {
                        bestVal = val;
                        bestLag = lag;
                    }
                }
            }

            if (bestLag == 0) return 0f;

            // 5) parabolic interpolation using neighboring points for sub-frame accuracy
            float y0 = a[(N - 1) + bestLag - 1].re;
            float y1 = a[(N - 1) + bestLag].re;
            float y2 = a[(N - 1) + bestLag + 1].re;
            float denom = (y0 - 2f * y1 + y2);
            float delta = 0f;
            if (Mathf.Abs(denom) > 1e-9f) delta = 0.5f * (y0 - y2) / denom;
            float refinedLag = bestLag + delta;

            float secondsPerBeat = refinedLag / frameRate;
            float bpm = 60f / secondsPerBeat;

            // 6) simple octave correction
            if (bpm < 50f) bpm *= 2f;
            else if (bpm > 200f) bpm /= 2f;

            return bpm;
        }

    }
}
