using NoteMaker.Model;
using NoteMaker.Utility;
using System.IO;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private GameObject windowRoot = default;             // 切り抜き画面全体のルート
        [SerializeField] private Button openButton = default;                 // 開くボタン
        [SerializeField] private Button closeButton = default;                // 閉じるボタン
        [SerializeField] private Button previewButton = default;              // プレビュー再生・停止ボタン
        [SerializeField] private Button saveButton = default;                 // 切り抜き実行ボタン
        [SerializeField] private RawImage waveformImage = default;            // 波形を描画する画像
        [SerializeField] private RectTransform selectionOverlay = default;    // 選択範囲を示す半透明のオーバーレイ
        [SerializeField] private Text rangeText = default;                    // 選択範囲の時間表示用

        [Header("Waveform Settings")]
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 256;
        [SerializeField] private Color waveformColor = Color.green;

        private float selectionStartRatio = 0f;
        private float selectionEndRatio = 1f;
        private PointerEventData.InputButton? draggingButton = null;
        private bool isDragging = false;
        
        private AudioSource previewSource;
        private bool isPreviewPlaying = false;
        private int previewEndSample = 0;
        
        private Color[] cachedPixels;
        private Texture2D waveformTex;

        private void Start()
        {
            // 初期状態は非表示
            windowRoot.SetActive(false);

            // プレビュー用のAudioSourceを追加
            previewSource = gameObject.AddComponent<AudioSource>();
            previewSource.playOnAwake = false;

            // 開くボタンでウィンドウを表示
            openButton.onClick.AsObservable()
                .Subscribe(_ => OpenWindow())
                .AddTo(this);

            // ✕ボタンで閉じる
            closeButton.onClick.AsObservable()
                .Subscribe(_ => CloseWindow())
                .AddTo(this);

            // プレビューボタンで再生・停止
            if (previewButton != null)
            {
                previewButton.onClick.AsObservable()
                    .Subscribe(_ => TogglePreview())
                    .AddTo(this);
            }

            // 保存ボタンで切り抜き実行
            saveButton.onClick.AsObservable()
                .Subscribe(_ => SaveSample())
                .AddTo(this);

            // 波形画像上でのマウスクリック＆ドラッグによる範囲選択
            var trigger = waveformImage.gameObject.AddComponent<ObservableEventTrigger>();
            
            trigger.OnPointerDownAsObservable()
                .Subscribe(eventData => {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(waveformImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float ratio = Mathf.Clamp01((localPoint.x + waveformImage.rectTransform.rect.width / 2f) / waveformImage.rectTransform.rect.width);

                    // 左クリックで開始位置、右クリックで終了位置を設定
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        selectionStartRatio = ratio;
                        draggingButton = PointerEventData.InputButton.Left;
                        isDragging = true;
                    }
                    // 右クリックで終了位置を設定
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        selectionEndRatio = ratio;
                        draggingButton = PointerEventData.InputButton.Right;
                        isDragging = true;
                    }
                    UpdateSelectionUI();
                })
                .AddTo(this);

            trigger.OnDragAsObservable()
                .Where(_ => isDragging)
                .Subscribe(eventData => {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(waveformImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float ratio = Mathf.Clamp01((localPoint.x + waveformImage.rectTransform.rect.width / 2f) / waveformImage.rectTransform.rect.width);

                    if(draggingButton == PointerEventData.InputButton.Left)
                    {
                        selectionStartRatio = ratio;
                    }
                    else if (draggingButton == PointerEventData.InputButton.Right)
                    {
                        selectionEndRatio = ratio;
                    }

                    UpdateSelectionUI();
                })
                .AddTo(this);

            trigger.OnPointerUpAsObservable()
                .Subscribe(_ => {
                    isDragging = false;
                    draggingButton = null;
                })
                .AddTo(this);
        }

        private void Update()
        {
            // プレビュー再生中の終了判定
            if (isPreviewPlaying && previewSource.isPlaying)
            {
                if (previewSource.timeSamples >= previewEndSample)
                {
                    StopPreview();
                }
                else
                {
                    UpdatePlaybackLineUI();
                }
            }
            else if (isPreviewPlaying && !previewSource.isPlaying)
            {
                StopPreview(); // 末尾まで再生されて止まった場合
            }
        }
        
        private void UpdatePlaybackLineUI()
        {
            if (Audio.Source.clip == null || cachedPixels == null || waveformTex == null) return;
            
            float ratio = (float)previewSource.timeSamples / Audio.Source.clip.samples;
            
            Color[] currentPixels = (Color[])cachedPixels.Clone();
            int xLine = Mathf.RoundToInt(ratio * textureWidth);
            xLine = Mathf.Clamp(xLine, 0, textureWidth - 1);
            
            Color playLineColor = Color.white; // 再生ラインの色
            
            for (int y = 0; y < textureHeight; y++)
            {
                currentPixels[y * textureWidth + xLine] = playLineColor;
            }
            
            waveformTex.SetPixels(currentPixels);
            waveformTex.Apply();
        }

        [System.Serializable]
        private class AudioCutterSettings
        {
            public float startRatio;
            public float endRatio;
        }

        private string GetSettingsFilePath()
        {
            string workSpace = Settings.WorkSpacePath.Value;
            if (string.IsNullOrEmpty(workSpace)) return null;
            return Path.Combine(workSpace, "Notes", EditData.Name.Value, "SampleSettings.json");
        }

        private void SaveSettings()
        {
            string path = GetSettingsFilePath();
            if (string.IsNullOrEmpty(path)) return;

            var settings = new AudioCutterSettings
            {
                startRatio = Mathf.Min(selectionStartRatio, selectionEndRatio),
                endRatio = Mathf.Max(selectionStartRatio, selectionEndRatio)
            };
            File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(settings));
        }

        private void LoadSettings()
        {
            string path = GetSettingsFilePath();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = UnityEngine.JsonUtility.FromJson<AudioCutterSettings>(json);
                selectionStartRatio = settings.startRatio;
                selectionEndRatio = settings.endRatio;
                if (selectionStartRatio == 0f && selectionEndRatio == 0f)
                {
                    selectionEndRatio = 1f;
                }
            }
            else
            {
                selectionStartRatio = 0f;
                selectionEndRatio = 1f;
            }
        }

        /// <summary>
        /// オーディオカッターウィンドウを開きます。（外部のボタン等から呼ばれることを想定）
        /// </summary>
        public void OpenWindow()
        {
            windowRoot.SetActive(true);
            LoadSettings();
            
            if (previewSource != null && Audio.Source != null)
            {
                previewSource.clip = Audio.Source.clip;
            }

            UpdateSelectionUI();
            GenerateWaveformTexture();
        }

        /// <summary>
        /// オーディオカッターウィンドウを閉じます。
        /// </summary>
        public void CloseWindow()
        {
            StopPreview();
            windowRoot.SetActive(false);
        }

        private void TogglePreview()
        {
            if (isPreviewPlaying)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        }
        
        private void StartPreview()
        {
            var clip = Audio.Source.clip;
            if (clip == null || previewSource == null) return;
            
            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            int startSample = (int)(minRatio * clip.samples);
            previewEndSample = (int)(maxRatio * clip.samples);
            
            if (startSample >= previewEndSample) return;

            previewSource.clip = clip;
            previewSource.timeSamples = startSample;
            previewSource.Play();
            isPreviewPlaying = true;
            
            // プレビュー再生中のUI表示変更
            if (previewButton != null)
            {
                var text = previewButton.GetComponentInChildren<Text>();
                if (text != null) text.text = "Stop";
            }
        }
        
        private void StopPreview()
        {
            if (previewSource != null)
            {
                previewSource.Stop();
            }
            isPreviewPlaying = false;
            
            // 停止時はベースの波形画像に戻す
            if (cachedPixels != null && waveformTex != null)
            {
                waveformTex.SetPixels(cachedPixels);
                waveformTex.Apply();
            }
            
            if (previewButton != null)
            {
                var text = previewButton.GetComponentInChildren<Text>();
                if (text != null) text.text = "Play";
            }
        }

        private void UpdateSelectionUI()
        {
            if (selectionOverlay == null) return;

            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            // オーバーレイの位置と幅を更新
            selectionOverlay.anchorMin = new Vector2(minRatio, 0f);
            selectionOverlay.anchorMax = new Vector2(maxRatio, 1f);
            selectionOverlay.offsetMin = Vector2.zero;
            selectionOverlay.offsetMax = Vector2.zero;

            // 選択時間の表示
            var clip = Audio.Source.clip;
            if(clip != null && rangeText != null)
            {
                float length = clip.length;
                float startTime = minRatio * length;
                float endTime = maxRatio * length;

                rangeText.text = $"{FormatTime(startTime)} - {FormatTime(endTime)}";
            }

            GenerateWaveformTexture();
        }

        private string FormatTime(float time)
        {
            int tortalSecond = Mathf.FloorToInt(time);
            int minutes = tortalSecond / 60;
            int seconds = tortalSecond % 60;
            return $"{minutes:00} : {seconds:00}";
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

            DrawSelectionLines(pixels);

            // プレビュー再生時に使うためにベース画像をキャッシュ
            cachedPixels = (Color[])pixels.Clone();
            waveformTex = tex;

            tex.SetPixels(pixels);
            tex.Apply();
            waveformImage.texture = tex;
        }

        private void DrawSelectionLines(Color[] pixels)
        {
            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            int xStart = Mathf.RoundToInt(minRatio * textureWidth);
            int xEnd = Mathf.RoundToInt(maxRatio * textureWidth);

            Color lineColor = Color.mediumVioletRed;

            for (int y = 0; y < textureHeight; y++)
                pixels[y * textureWidth + Mathf.Clamp(xStart, 0, textureWidth - 1)] = lineColor;

            for (int y = 0; y < textureHeight; y++)
                pixels[y * textureWidth + Mathf.Clamp(xEnd, 0, textureWidth - 1)] = lineColor;
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

            string savePath = Path.Combine(workSpace, "Notes", EditData.Name.Value, "Sample.wav");

            // 書き出し実行
            WavUtility.Save(savePath, clip, startSample, lengthSamples);
            
            // JSONに設定を保存
            SaveSettings();

            // 完了したらウィンドウを閉じる
            CloseWindow();
        }
    }
}
