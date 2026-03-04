// ========================================
//
// NoteMaker Project
//
// ========================================
//
// BannerSettingsPresenter.cs
// 楽曲バナー画像の選択・保存・プレビュー表示を行うプレゼンターです。
// 画像ファイルの選択ダイアログ、BannerSettings へのパス反映、
// プレビュー画像の読み込み、楽曲フォルダへの保存処理を担当します。
//
//========================================

using UnityEngine;
using UnityEngine.UI;
using UniRx;
using SFB;
using NoteMaker.Model;
using System.IO;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// バナー画像の選択・プレビュー表示を行うクラスです。
    /// ・画像ファイル選択ダイアログの表示  
    /// ・BannerSettings へのパス反映  
    /// ・プレビュー画像の読み込み  
    /// ・楽曲フォルダへのバナー保存  
    /// </summary>
    public class BannerSettingsPresenter : MonoBehaviour
    {
        [SerializeField] private Button selectBannerButton; // バナー選択ボタン
        [SerializeField] private Image previewImage;        // プレビュー表示用 Image

        /// <summary>
        /// 初期化処理で、ボタンクリック→ファイル選択のダイアログ表示の設定を行います
        /// </summary>
        void Awake()
        {
            // ボタンクリック → ファイル選択ダイアログ
            selectBannerButton.OnClickAsObservable()
                .Subscribe(_ => OpenFileDialog());

            // BannerPath が変化したらプレビュー更新
            BannerSettings.BannerPath
                .Subscribe(path =>
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        previewImage.sprite = null;
                        return;
                    }
                    LoadPreview(path);
                })
                .AddTo(this);
        }

        /// <summary>
        /// 画像ファイル選択ダイアログを開き、選択された画像を保存・反映します。
        /// </summary>
        void OpenFileDialog()
        {
            // png/jpg.jpeg のみ選択可能なフィルタの設定
            var extensions = new[]
            {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
            };

            // ファイル選択のダイアログを表示
            var paths = StandaloneFileBrowser.OpenFilePanel("Select Banner Image", "", extensions, false);

            // 選択されたファイルがあれば、BannerSettingsにパスを反映し、楽曲フォルダに保存
            if (paths.Length > 0)
            {
                BannerSettings.BannerPath.Value = paths[0];

                var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
                BannerFileUtility.SaveBannerToMusicFolder(paths[0], musicName);
            }
        }

        /// <summary>
        /// 指定パスの画像を読み込み、プレビューに表示します。
        /// </summary>
        void LoadPreview(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

            previewImage.color = Color.white;
            previewImage.sprite = sprite;
        }
    }

    /// <summary>
    /// バナー画像の保存・取得を行うユーティリティクラスです。
    /// 楽曲フォルダに banner.png / banner.jpg / banner.jpeg として保存します。
    /// </summary>
    public static class BannerFileUtility
    {
        /// <summary>
        /// バナー画像を楽曲フォルダに保存します。
        /// 既存のバナー画像（png/jpg/jpeg）は削除されます。
        /// </summary>
        public static void SaveBannerToMusicFolder(string sourcePath, string musicName)
        {
            // 楽曲フォルダのパスを構築
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes");

            // Notes/曲名/ のフォルダを作成
            var musicFolder = Path.Combine(notesRoot, musicName);

            // 楽曲フォルダが存在しない場合は作成
            if (!Directory.Exists(musicFolder))
                Directory.CreateDirectory(musicFolder);

            // 保存先のパスを構築
            var ext = Path.GetExtension(sourcePath);
            var destPath = Path.Combine(musicFolder, "banner" + ext);

            // 既存バナー削除
            var oldPng = Path.Combine(musicFolder, "banner.png");
            var oldJpg = Path.Combine(musicFolder, "banner.jpg");
            var oldJpeg = Path.Combine(musicFolder, "banner.jpeg");

            // png/jpg/jpegの順で削除
            if (File.Exists(oldPng)) File.Delete(oldPng);
            if (File.Exists(oldJpg)) File.Delete(oldJpg);
            if (File.Exists(oldJpeg)) File.Delete(oldJpeg);

            // 新規保存
            File.Copy(sourcePath, destPath, overwrite: true);
        }

        /// <summary>
        /// 楽曲フォルダ内のバナー画像パスを取得します。
        /// png → jpg → jpeg の順で検索します。
        /// </summary>
        public static string GetBannerPath(string musicName)
        {
            // 楽曲フォルダのパスを構築
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes");

            // Notes/曲名/のフォルダを構築
            var musicFolder = Path.Combine(notesRoot, musicName);

            // png/jpg/jpegの順でバナー画像を検索
            var png = Path.Combine(musicFolder, "banner.png");
            var jpg = Path.Combine(musicFolder, "banner.jpg");
            var jpeg = Path.Combine(musicFolder, "banner.jpeg");

            // 存在するファイルのパスを返す
            if (File.Exists(png)) return png;
            if (File.Exists(jpg)) return jpg;
            if (File.Exists(jpeg)) return jpeg;

            return null;
        }
    }
}
