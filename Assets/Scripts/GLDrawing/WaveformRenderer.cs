// ========================================
//
// WaveformRenderer.cs
//
// ========================================
//
// 波形（Waveform）を RawImage に描画するクラス。
// AudioClip のサンプルを取得し、Texture2D に可視化する。
//
// ========================================

using NoteMaker.Model;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.GLDrawing
{
    public class WaveformRenderer : MonoBehaviour
    {
        [SerializeField]
        RawImage image = default;   // 波形を表示する UI

        Texture2D texture;          // 波形描画用テクスチャ

        int imageWidth = 1280;      // テクスチャ横幅
        float[] samples = new float[500000]; // サンプル取得用バッファ

        float cachedCanvasWidth = 0; // 前回のキャンバス幅
        float cachedTimeSamples = 0; // 前回のサンプル位置

        /// <summary>
        /// 初期化処理。テクスチャを生成し、非表示時のリセット処理を登録する。
        /// </summary>
        void Start()
        {
            texture = new Texture2D(imageWidth, 1);
            image.texture = texture;
            ResetTexture();

            // 波形表示が OFF になったらテクスチャをクリア
            EditorState.WaveformDisplayEnabled
                .Where(enabled => !enabled)
                .Subscribe(_ => ResetTexture());
        }

        /// <summary>
        /// 毎フレーム、波形の更新が必要なら描画する。
        /// </summary>
        void LateUpdate()
        {
            // 音声がない or 波形表示が OFF の場合は描画しない
            if (Audio.Source.clip == null || !EditorState.WaveformDisplayEnabled.Value)
                return;

            // 現在のサンプル位置（範囲内に収める）
            var timeSamples = Mathf.Min(Audio.SmoothedTimeSamples.Value, Audio.Source.clip.samples - 1);

            // 更新が不要ならスキップ
            if (!HasUpdate(timeSamples))
                return;

            // キャッシュ更新
            UpdateCache(timeSamples);

            // サンプルデータを取得
            Audio.Source.clip.GetData(samples, Mathf.RoundToInt(timeSamples));

            int textureX = 0;
            float maxSample = 0;

            // 画面幅に応じてサンプルを間引く
            int skipSamples = Mathf.RoundToInt(1 / (NoteCanvas.Width.Value * 0.5f / Audio.Source.clip.samples));

            // 波形をテクスチャに描画
            for (int i = 0, l = samples.Length; textureX < imageWidth && i < l; i++)
            {
                // 最大値を記録（ピーク検出）
                maxSample = Mathf.Max(maxSample, samples[i]);

                // 一定サンプルごとに描画
                if (i % skipSamples == 0)
                {
                    texture.SetPixel(textureX, 0, new Color(maxSample, 0, 0));
                    maxSample = 0;
                    textureX++;
                }
            }

            texture.Apply();
        }

        /// <summary>
        /// テクスチャをクリアする。
        /// </summary>
        void ResetTexture()
        {
            texture.SetPixels(Enumerable.Range(0, imageWidth).Select(_ => Color.clear).ToArray());
            texture.Apply();
        }

        /// <summary>
        /// 波形の再描画が必要かどうかを判定する。
        /// </summary>
        bool HasUpdate(float timeSamples)
        {
            // キャンバス幅 or サンプル位置が変わった場合に更新
            return cachedCanvasWidth != NoteCanvas.Width.Value ||
                   cachedTimeSamples != timeSamples;
        }

        /// <summary>
        /// 前回の状態をキャッシュする。
        /// </summary>
        void UpdateCache(float timeSamples)
        {
            cachedCanvasWidth = NoteCanvas.Width.Value;
            cachedTimeSamples = timeSamples;
        }
    }
}
