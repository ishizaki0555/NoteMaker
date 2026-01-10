using UnityEngine;
using UnityEngine.UI;
using UniRx;
using SFB;
using NoteMaker.Model;
using System.IO;

namespace NoteMaker.Presenter
{
    public class BannerSettingsPresenter : MonoBehaviour
    {
        [SerializeField] private Button selectBannerButton;
        [SerializeField] private Image previewImage;

        private void Awake()
        {
            selectBannerButton.OnClickAsObservable()
                .Subscribe(_ => OpenFileDialog());

            BannerSettings.BannerPath
                .Where(path => !string.IsNullOrEmpty(path))
                .Subscribe(path => LoadPreview(path));
        }

        void OpenFileDialog()
        {
            var extensions = new[]
            {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
            };

            var paths = StandaloneFileBrowser.OpenFilePanel("Select Banner Image", "", extensions, false);

            if(paths.Length > 0)
            {
                BannerSettings.BannerPath.Value = paths[0];
                var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
                BannerFileUtility.SaveBannerToMusicFolder(paths[0], musicName);
            }
        }
        
        void LoadPreview(string path)
        {
            var bytes = System.IO.File.ReadAllBytes(path);
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
    public static class BannerFileUtility
    {
        public static void SaveBannerToMusicFolder(string sourcePath, string musicName)
        {
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes"
            );

            var musicFolder = Path.Combine(notesRoot, musicName);

            if (!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }

            var ext = Path.GetExtension(sourcePath);
            var destPath = Path.Combine(musicFolder, "banner" + ext);

            File.Copy(sourcePath, destPath, overwrite: true);
        }

        public static string GetBannerPath(string musicName)
        {
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes"
            );

            var musicFolder = Path.Combine(notesRoot, musicName);

            var png = Path.Combine(musicFolder, "banner.png");
            var jpg = Path.Combine(musicFolder, "banner.jpg");
            var jpeg = Path.Combine(musicFolder, "banner.jpeg");

            if (File.Exists(png)) return png;
            if (File.Exists(jpg)) return jpg;
            if (File.Exists(jpeg)) return jpeg;

            return null;
        }
    }

}