// ========================================
//
// NoteMaker Project
//
// ========================================
//
// AudioCutterWindowPresenter.cs
//
// 音声切り抜き（サンプリング）用ウィンドウの制御を行う Presenter クラスです。
//
//========================================

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
        [SerializeField] private Slider volumeSlider = default;               // プレビュー音量スライダー

        [Header("Waveform Settings")]
        [SerializeField] private int textureWidth = 1024;                       // 波形画像の幅
        [SerializeField] private int textureHeight = 256;                       // 波形画像の高さ
        [SerializeField] private Color waveformColor = Color.green;             // 波形の色

        private float selectionStartRatio = 0f;                                 // 選択範囲の開始位置（0.0〜1.0）
        private float selectionEndRatio = 1f;                                   // 選択範囲の終了位置（0.0〜1.0）
        private PointerEventData.InputButton? draggingButton = null;            // ドラッグ中のマウスボタン（左クリック or 右クリック）
        private bool isDragging = false;                                        // ドラッグ中かどうかのフラグ

        private AudioSource previewSource;                                      // プレビュー再生用のAudioSource
        private bool isPreviewPlaying = false;                                  // プレビュー再生中かどうかのフラグ
        private int previewEndSample = 0;                                       // プレビュー再生の終了サンプル位置

        private Color[] cachedPixels;                                           // 波形画像のベースとなるピクセルデータをキャッシュ
        private Texture2D waveformTex;                                          // 波形画像のテクスチャ

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
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

            // 音量スライダーの値が変化したらプレビュー音量を更新
            if(volumeSlider != null)
            {
                volumeSlider.onValueChanged.AsObservable()
                    .Subscribe(value =>
                    {
                        if(previewSource != null)
                        {
                            previewSource.volume = value;
                        }
                    })
                    .AddTo(this);
            }

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
            
            // クリックで範囲の開始位置または終了位置を設定
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

            // ドラッグ中のマウス移動で範囲を更新
            trigger.OnDragAsObservable()
                .Where(_ => isDragging)
                .Subscribe(eventData => {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(waveformImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float ratio = Mathf.Clamp01((localPoint.x + waveformImage.rectTransform.rect.width / 2f) / waveformImage.rectTransform.rect.width);

                    // ドラッグ中のボタンに応じて開始位置または終了位置を更新
                    if (draggingButton == PointerEventData.InputButton.Left)
                    {
                        selectionStartRatio = ratio;
                    }
                    // 右クリックでドラッグ中の場合は終了位置を更新
                    else if (draggingButton == PointerEventData.InputButton.Right)
                    {
                        selectionEndRatio = ratio;
                    }

                    // UIを更新して選択範囲を反映
                    UpdateSelectionUI();
                })
                .AddTo(this);

            // ドラッグ終了時にフラグをリセット
            trigger.OnPointerUpAsObservable()
                .Subscribe(_ => {
                    isDragging = false;
                    draggingButton = null;
                })
                .AddTo(this);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            // プレビュー再生中の終了判定
            if (isPreviewPlaying && previewSource.isPlaying)
            {
                // プレビュー再生が終了位置に達したら停止
                if (previewSource.timeSamples >= previewEndSample)
                {
                    StopPreview();
                }
                else
                {
                    UpdatePlaybackLineUI();
                }
            }
            // プレビュー再生中にAudioSourceが止まった場合（末尾まで再生された場合など）
            else if (isPreviewPlaying && !previewSource.isPlaying)
            {
                StopPreview(); // 末尾まで再生されて止まった場合
            }
        }

        /// <summary>
        /// プレビュー再生中の再生位置を波形画像上に表示するためのUI更新処理
        /// </summary>
        private void UpdatePlaybackLineUI()
        {
            // 再生位置のラインを波形画像上に描画するための処理
            if (Audio.Source.clip == null || cachedPixels == null || waveformTex == null) return;

            // 再生位置の比率を計算
            float ratio = (float)previewSource.timeSamples / Audio.Source.clip.samples;

            // 再生位置のラインを描画するために、キャッシュされた波形画像のピクセルデータをコピーして使用
            Color[] currentPixels = (Color[])cachedPixels.Clone();
            int xLine = Mathf.RoundToInt(ratio * textureWidth);
            xLine = Mathf.Clamp(xLine, 0, textureWidth - 1);
            
            Color playLineColor = Color.white; // 再生ラインの色

            // 再生位置のラインを描画
            for (int y = 0; y < textureHeight; y++)
            {
                currentPixels[y * textureWidth + xLine] = playLineColor;
            }
            
            waveformTex.SetPixels(currentPixels);
            waveformTex.Apply();
        }

        /// <summary>
        /// オーディオカッターの設定を保存するためのクラス
        /// </summary>
        [System.Serializable]
        private class AudioCutterSettings
        {
            public float startRatio;        // 選択範囲の開始位置（0.0〜1.0）
            public float endRatio;          // 選択範囲の終了位置（0.0〜1.0）
        }

        /// <summary>
        /// 設定ファイルのパスを取得します。
        /// </summary>
        /// <returns>設定ファイルのフルパス</returns>
        private string GetSettingsFilePath()
        {
            // 譜面の保存先ディレクトリに SampleSettings.json を保存する
            string workSpace = Settings.WorkSpacePath.Value;
            if (string.IsNullOrEmpty(workSpace)) return null;
            return Path.Combine(workSpace, "Notes", EditData.Name.Value, "SampleSettings.json");
        }

        /// <summary>
        /// オーディオカッターの設定を保存します。
        /// </summary>
        private void SaveSettings()
        {
            // 設定ファイルのパスを取得
            string path = GetSettingsFilePath();
            if (string.IsNullOrEmpty(path)) return;

            // 設定をJSON形式で保存
            var settings = new AudioCutterSettings
            {
                // 選択範囲の開始位置と終了位置を保存する際に、常に小さい方を startRatio、大きい方を endRatio として保存
                startRatio = Mathf.Min(selectionStartRatio, selectionEndRatio),
                endRatio = Mathf.Max(selectionStartRatio, selectionEndRatio)
            };
            // JSON形式で保存
            File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(settings));
        }

        /// <summary>
        /// オーディオカッターの設定を読み込みます。
        /// </summary>
        private void LoadSettings()
        {
            // 設定ファイルのパスを取得
            string path = GetSettingsFilePath();

            // 設定ファイルが存在する場合は読み込み、存在しない場合はデフォルト値を使用
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                // JSON形式で保存された設定を読み込み
                var json = File.ReadAllText(path);
                var settings = UnityEngine.JsonUtility.FromJson<AudioCutterSettings>(json);
                selectionStartRatio = settings.startRatio;
                selectionEndRatio = settings.endRatio;

                // もし両方とも0の場合は、デフォルトで全体を選択するように設定
                if (selectionStartRatio == 0f && selectionEndRatio == 0f)
                {
                    selectionEndRatio = 1f;
                }
            }
            // 設定ファイルが存在しない場合は、デフォルトで全体を選択するように設定
            else
            {
                // デフォルトで全体を選択するように設定
                selectionStartRatio = 0f;
                selectionEndRatio = 1f;
            }
        }

        /// <summary>
        /// オーディオカッターウィンドウを開きます。（外部のボタン等から呼ばれることを想定）
        /// </summary>
        public void OpenWindow()
        {
            // ウィンドウを表示
            windowRoot.SetActive(true);
            LoadSettings();

            // プレビュー用のAudioSourceに現在のAudioClipを設定
            if (previewSource != null && Audio.Source != null)
            {
                previewSource.clip = Audio.Source.clip;
            }

            // UIを更新して選択範囲を反映
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

        /// <summary>
        /// プレビュー再生の開始・停止を切り替えます。
        /// </summary>
        private void TogglePreview()
        {
            // プレビュー再生中であれば停止、停止中であれば開始
            if (isPreviewPlaying)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        }

        /// <summary>
        /// プレビュー再生を開始します。
        /// </summary>
        private void StartPreview()
        {
            // AudioClipが設定されていない場合や、プレビュー用のAudioSourceが存在しない場合は再生しない
            var clip = Audio.Source.clip;
            if (clip == null || previewSource == null) return;

            // 選択範囲の開始位置と終了位置をサンプル数に変換して、プレビュー再生の開始位置と終了位置を設定
            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            // 選択範囲が無効な場合は再生しない
            int startSample = (int)(minRatio * clip.samples);
            previewEndSample = (int)(maxRatio * clip.samples);

            // 選択範囲が無効な場合は再生しない
            if (startSample >= previewEndSample) return;

            // プレビュー再生用のAudioSourceに設定
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

        /// <summary>
        /// プレビュー再生を停止します。
        /// </summary>
        private void StopPreview()
        {
            // プレビュー再生中でなければ何もしない
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

            // UI表示を元に戻す
            if (previewButton != null)
            {
                var text = previewButton.GetComponentInChildren<Text>();
                if (text != null) text.text = "Play";
            }
        }

        /// <summary>
        /// 選択範囲のUIを更新します。オーバーレイの位置と幅、選択時間の表示を更新します。
        /// </summary>
        private void UpdateSelectionUI()
        {
            // オーバーレイが存在しない場合は何もしない
            if (selectionOverlay == null) return;

            // 選択範囲の開始位置と終了位置の比率を計算
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

        /// <summary>
        /// 選択範囲のサンプルを切り抜いてWAVファイルとして保存します。
        /// </summary>
        private void SaveSample()
        {
            // AudioClipが設定されていない場合は保存しない
            var clip = Audio.Source.clip;
            if (clip == null) return;

            // 選択範囲の開始位置と終了位置をサンプル数に変換
            float minRatio = Mathf.Min(selectionStartRatio, selectionEndRatio);
            float maxRatio = Mathf.Max(selectionStartRatio, selectionEndRatio);

            // 選択範囲のサンプル数を計算
            int startSample = (int)(minRatio * clip.samples);
            int endSample = (int)(maxRatio * clip.samples);
            int lengthSamples = endSample - startSample;

            // 選択範囲が無効な場合は保存しない
#if UNITY_EDITOR
            if (lengthSamples <= 0)
            {
                Debug.LogWarning("Selection length is 0.");
                return;
            }
#endif

            // 保存先パスの決定 (譜面と同じディレクトリに保存)
            string workSpace = Settings.WorkSpacePath.Value;
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(workSpace))
            {
                Debug.LogError("Workspace path is not set.");
                return;
            }
#endif

            string savePath = Path.Combine(workSpace, "Notes", EditData.Name.Value, "Sample.wav");

            float volume = volumeSlider != null ? volumeSlider.value : 1f;

            // 書き出し実行
            WavUtility.Save(savePath, clip, startSample, lengthSamples, volume);
            
            // JSONに設定を保存
            SaveSettings();

            // 完了したらウィンドウを閉じる
            CloseWindow();
        }
    }
}
