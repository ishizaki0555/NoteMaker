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

        int imageHeight = 1080;
        float[] samples = new float[500000];

        float cachedCanvasWidth = 0;
        float cachedTimeSamples = 0;

        void Start()
        {
            texture = new Texture2D(1, imageHeight);
            image.texture = texture;
            ResetTexture();

            EditorState.WaveformDisplayEnabled
                .Where(enabled => !enabled)
                .Subscribe(_ => ResetTexture());
        }

        void LateUpdate()
        {
            if (Audio.Source.clip == null || !EditorState.WaveformDisplayEnabled.Value)
                return;

            var timeSamples = Mathf.Min(Audio.SmoothedTimeSamples.Value, Audio.Source.clip.samples - 1);

            if (!HasUpdate(timeSamples))
                return;

            UpdateCache(timeSamples);

            Audio.Source.clip.GetData(samples, Mathf.RoundToInt(timeSamples));

            int textureY = 0;
            float maxSample = 0;
            int skipSamples = Mathf.RoundToInt(1 / (NoteCanvas.Width.Value * 0.5f / Audio.Source.clip.samples));

            for (int i = 0, l = samples.Length; textureY < imageHeight && i < l; i++)
            {
                maxSample = Mathf.Max(maxSample, samples[i]);

                if (i % skipSamples == 0)
                {
                    texture.SetPixel(0, textureY, new Color(maxSample, 0, 0));
                    maxSample = 0;
                    textureY++;
                }
            }

            texture.Apply();
        }

        void ResetTexture()
        {
            texture.SetPixels(Enumerable.Range(0, imageHeight).Select(_ => Color.clear).ToArray());
            texture.Apply();
        }

        bool HasUpdate(float timeSamples)
        {
            return cachedCanvasWidth != NoteCanvas.Width.Value || cachedTimeSamples != timeSamples;
        }

        void UpdateCache(float timeSamples)
        {
            cachedCanvasWidth = NoteCanvas.Width.Value;
            cachedTimeSamples = timeSamples;
        }
    }
}
