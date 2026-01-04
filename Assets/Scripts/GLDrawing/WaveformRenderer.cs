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
        RawImage image = default;

        Texture2D texture;

        int imageHeight = 720;
        float[] samples = new float[500000];

        float cachedCanvasHeight = 0;
        float cachedTimeSamples = 0;

        void Start()
        {
            // ============================
            // ★ Texture を縦長で初期化（縦スクロール版）
            // ============================
            texture = new Texture2D(1, imageHeight);
            image.texture = texture;

            ResetTexture();

            // ============================
            // ★ 横版と同じ構造のイベント
            // ============================
            EditorState.WaveformDisplayEnabled
                .Where(enabled => !enabled)
                .Subscribe(_ => ResetTexture());
        }

        void LateUpdate()
        {
            if (Audio.Source.clip == null || !EditorState.WaveformDisplayEnabled.Value)
                return;

            // ============================
            // ★ 現在のサンプル位置（横版と同じ構造）
            // ============================
            var timeSamples = Mathf.Min(
                Audio.SmoothedTimeSamples.Value,
                Audio.Source.clip.samples - 1
            );

            if (!HasUpdate(timeSamples))
                return;

            UpdateCache(timeSamples);

            // ============================
            // ★ 波形データ取得（横版と同じ構造）
            // ============================
            Audio.Source.clip.GetData(samples, Mathf.RoundToInt(timeSamples));

            int textureY = 0;
            float maxSample = 0;

            // ============================
            // ★ skipSamples（横版構造 × 縦方向）
            // ============================
            int skipSamples = Mathf.RoundToInt(
                1 / (NoteCanvas.Height.Value * 0.5f / Audio.Source.clip.samples)
            );

            // ============================
            // ★ 波形描画（縦方向）
            // ============================
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

        // ============================
        // ★ Texture 初期化（横版構造）
        // ============================
        void ResetTexture()
        {
            texture.SetPixels(
                Enumerable.Range(0, imageHeight)
                .Select(_ => Color.clear)
                .ToArray()
            );
            texture.Apply();
        }

        // ============================
        // ★ 更新判定（横版構造）
        // ============================
        bool HasUpdate(float timeSamples)
        {
            return cachedCanvasHeight != NoteCanvas.Height.Value
                || cachedTimeSamples != timeSamples;
        }

        // ============================
        // ★ キャッシュ更新（横版構造）
        // ============================
        void UpdateCache(float timeSamples)
        {
            cachedCanvasHeight = NoteCanvas.Height.Value;
            cachedTimeSamples = timeSamples;
        }
    }
}
