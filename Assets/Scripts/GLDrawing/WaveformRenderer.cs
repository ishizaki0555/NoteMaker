// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// WaveformRenderer.cs
// 再生中の音声データから波形を取得し、縦スクロールエディタ用に
// RawImage 上へ縦方向の波形を描画します。
// キャンバス高さや再生位置の変化に応じて動的に更新されます。
// 
//========================================

using NoteMaker.Model;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// 音声クリップの波形を RawImage に縦方向へ描画するクラスです。
    /// 再生位置に応じて波形を更新し、エディタのスクロール量に合わせて
    /// 波形の見た目が変化するように処理します。
    /// </summary>
    public class WaveformRenderer : MonoBehaviour
    {
        [SerializeField] RawImage image = default; // 波形を表示する RawImage

        Texture2D texture;                         // 波形描画用テクスチャ
        int imageHeight = 720;                     // テクスチャの高さ（縦方向の解像度）
        float[] samples = new float[500000];       // 波形取得用バッファ

        float cachedCanvasHeight = 0;              // 前フレームのキャンバス高さ
        float cachedTimeSamples = 0;               // 前フレームのサンプル位置

        /// <summary>
        /// 初期化処理。縦長テクスチャを生成し、波形表示の初期状態を作ります。
        /// </summary>
        void Start()
        {
            // Texture を縦長で初期化（縦スクロール版）
            texture = new Texture2D(1, imageHeight);
            image.texture = texture;

            ResetTexture();

            // 波形表示が OFF になったらテクスチャをクリア
            EditorState.WaveformDisplayEnabled
                .Where(enabled => !enabled)
                .Subscribe(_ => ResetTexture());
        }

        /// <summary>
        /// 毎フレームの LateUpdate で波形を更新します。
        /// 再生位置が変化した場合のみ波形を再描画します。
        /// </summary>
        void LateUpdate()
        {
            if (Audio.Source.clip == null || !EditorState.WaveformDisplayEnabled.Value)
                return;

            // 現在のサンプル位置（Smoothed）
            var timeSamples = Mathf.Min(
                Audio.SmoothedTimeSamples.Value,
                Audio.Source.clip.samples - 1
            );

            // 更新が不要なら終了
            if (!HasUpdate(timeSamples))
                return;

            UpdateCache(timeSamples);

            // 波形データ取得
            Audio.Source.clip.GetData(samples, Mathf.RoundToInt(timeSamples));

            int textureY = 0;
            float maxSample = 0;

            // skipSamples（縦方向の密度調整）
            int skipSamples = Mathf.RoundToInt(
                1 / (NoteCanvas.Height.Value * 0.5f / Audio.Source.clip.samples)
            );

            // 波形描画（縦方向）
            for (int i = 0, l = samples.Length; textureY < imageHeight && i < l; i++)
            {
                maxSample = Mathf.Max(maxSample, Mathf.Abs(samples[i]));

                if (i % skipSamples == 0)
                {
                    texture.SetPixel(0, textureY, new Color(maxSample, 0, 0));
                    maxSample = 0;
                    textureY++;
                }
            }

            texture.Apply();
        }

        /// <summary>
        /// テクスチャを透明で初期化します。
        /// </summary>
        void ResetTexture()
        {
            texture.SetPixels(
                Enumerable.Range(0, imageHeight)
                .Select(_ => Color.clear)
                .ToArray()
            );
            texture.Apply();
        }

        /// <summary>
        /// 波形を更新する必要があるかどうかを判定します。
        /// </summary>
        bool HasUpdate(float timeSamples)
        {
            return cachedCanvasHeight != NoteCanvas.Height.Value
                || cachedTimeSamples != timeSamples;
        }

        /// <summary>
        /// 前フレームの状態を更新します。
        /// </summary>
        void UpdateCache(float timeSamples)
        {
            cachedCanvasHeight = NoteCanvas.Height.Value;
            cachedTimeSamples = timeSamples;
        }
    }
}
