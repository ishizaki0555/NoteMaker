using NoteMaker.Model;
using NoteMaker.Utility;
using System.IO;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter.AudioCutter
{
    /// <summary>
    /// 音声切り抜き（サンプリング）用ウィンドウの制御を行うPresenterクラスです。
    /// 波形の描画、範囲選択、ファイルへの切り抜き保存を担当します。
    /// </summary>
    public class AudioCutterWindowPresenter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject windowRoot;             // 切り抜き画面全体のルート
        [SerializeField] private Button openButton;                 // 開くボタン
        [SerializeField] private Button closeButton;                // 閉じるボタン
        [SerializeField] private Button saveButton;                 // 切り抜き実行ボタン
        [SerializeField] private RawImage waveformImage;            // 波形を描画する画像
        [SerializeField] private RectTransform selectionOverlay;    // 選択範囲を示す半透明のオーバーレイ
        [SerializeField] private Text rangeText;                    // 選択範囲の時間表示用

        [Header("Waveform Settings")]
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 256;
        [SerializeField] private Color waveformColor = Color.green;

        private float selectionStartRatio = 0f;
        private float selectionEndRatio = 1f;
        private bool isDragging = false;

        private void Start()
        {
            // 初期状態は非表示
            windowRoot.SetActive(false);

            // 開くボタンでウィンドウを表示
            openButton.onClick.AsObservable()
                .Subscribe(_ => OpenWindow())
                .AddTo(this);

            // ✕ボタンで閉じる
            closeButton.onClick.AsObservable()
                .Subscribe(_ => CloseWindow())
                .AddTo(this);

            // 保存ボタンで切り抜き実行
            saveButton.onClick.AsObservable()
                .Subscribe(_ => SaveSample())
                .AddTo(this);

            // 波形画像上でのマウスクリック＆ドラッグによる範囲選択
            var trigger = waveformImage.gameObject.AddComponent<ObservableEventTrigger>();
            
            trigger.OnPointerDownAsObservable()
                .Subscribe(eventData => {
                    isDragging = true;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(waveformImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float ratio = Mathf.Clamp01((localPoint.x + waveformImage.rectTransform.rect.width / 2f) / waveformImage.rectTransform.rect.width);
                    selectionStartRatio = ratio;
                    selectionEndRatio = ratio;
                    UpdateSelectionUI();
                })
                .AddTo(this);

            trigger.OnDragAsObservable()
                .Where(_ => isDragging)
                .Subscribe(eventData => {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(waveformImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float ratio = Mathf.Clamp01((localPoint.x + waveformImage.rectTransform.rect.width / 2f) / waveformImage.rectTransform.rect.width);
                    selectionEndRatio = ratio;
                    UpdateSelectionUI();
                })
                .AddTo(this);

            trigger.OnPointerUpAsObservable()
                .Subscribe(_ => {
                    isDragging = false;
                })
                .AddTo(this);
        }

        /// <summary>
        /// オーディオカッターウィンドウを開きます。（外部のボタン等から呼ばれることを想定）
        /// </summary>
        public void OpenWindow()
        {
            windowRoot.SetActive(true);
            selectionStartRatio = 0f;
            selectionEndRatio = 1f;
            UpdateSelectionUI();
            GenerateWaveformTexture();
        }

        /// <summary>
        /// オーディオカッターウィンドウを閉じます。
        /// </summary>
        public void CloseWindow()
        {
            windowRoot.SetActive(false);
        }

        private void UpdateSelectionUI()
        {
            if (selectionOverlay == null) return;

            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            // オーバーレイの位置と幅を更新
            float width = waveformImage.rectTransform.rect.width;
            selectionOverlay.anchorMin = new Vector2(minRatio, 0f);
            selectionOverlay.anchorMax = new Vector2(maxRatio, 1f);
            selectionOverlay.offsetMin = Vector2.zero;
            selectionOverlay.offsetMax = Vector2.zero;

            // 選択時間の表示
            var clip = Audio.Source.clip;
            if (clip != null && rangeText != null)
            {
                float length = clip.length;
                rangeText.text = $"Selected: {minRatio * length:F2}s - {maxRatio * length:F2}s";
            }
        }

        private void GenerateWaveformTexture()
        {
            var clip = Audio.Source.clip;
            if (clip == null) return;

            Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[textureWidth * textureHeight];
            // 背景クリア
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0.5f);

            int channels = clip.channels;
            float[] samples = new float[clip.samples * channels];
            clip.GetData(samples, 0);

            int samplesPerPixel = samples.Length / channels / textureWidth;
            if (samplesPerPixel == 0) samplesPerPixel = 1;

            for (int x = 0; x < textureWidth; x++)
            {
                int startSampleIndex = x * samplesPerPixel * channels;
                float maxVal = 0f;

                // 簡易的に最大値をサンプリング
                for (int i = 0; i < samplesPerPixel; i++)
                {
                    int index = startSampleIndex + i * channels;
                    if (index < samples.Length)
                    {
                        float val = Mathf.Abs(samples[index]);
                        if (val > maxVal) maxVal = val;
                    }
                }

                int height = (int)(maxVal * textureHeight);
                int startY = (textureHeight - height) / 2;
                for (int y = startY; y < startY + height; y++)
                {
                    pixels[y * textureWidth + x] = waveformColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            waveformImage.texture = tex;
        }

        private void SaveSample()
        {
            var clip = Audio.Source.clip;
            if (clip == null) return;

            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            int startSample = (int)(minRatio * clip.samples);
            int endSample = (int)(maxRatio * clip.samples);
            int lengthSamples = endSample - startSample;

            if (lengthSamples <= 0)
            {
                Debug.LogWarning("Selection length is 0.");
                return;
            }

            // 保存先パスの決定 (譜面と同じディレクトリに保存)
            string workSpace = Settings.WorkSpacePath.Value;
            if (string.IsNullOrEmpty(workSpace))
            {
                Debug.LogError("Workspace path is not set.");
                return;
            }

            string savePath = Path.Combine(workSpace, "Sample.wav");

            // 書き出し実行
            WavUtility.Save(savePath, clip, startSample, lengthSamples);
            
            // 完了したらウィンドウを閉じる
            CloseWindow();
        }
    }
}
