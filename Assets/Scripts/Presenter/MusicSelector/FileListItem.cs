// ========================================
//
// NoteMaker Project
//
// ========================================
//
// FileListItem.cs
// 楽曲フォルダ内のファイル・ディレクトリを 1 行として表示する UI 要素です。
// 選択状態の反映、アイコン切り替え、クリック時のディレクトリ移動や
// ファイル選択など、ファイルブラウザとしての基本動作を担当します。
//
//========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ファイルリストの 1 アイテムを表す UI コンポーネントです。
    /// ・選択状態に応じた背景色・文字色の切り替え  
    /// ・ディレクトリ / 音楽ファイル / その他ファイルのアイコン表示  
    /// ・クリック時のディレクトリ移動またはファイル選択  
    /// </summary>
    public class FileListItem : MonoBehaviour
    {
        [SerializeField] Color selectedStateBackgroundColor = default; // 選択時の背景色
        [SerializeField] Color defaultBackgroundColor = default;       // 通常時の背景色
        [SerializeField] Color selectedTextColor = default;            // 選択時の文字色
        [SerializeField] Color defaultTextColor = default;             // 通常時の文字色
        [SerializeField] Image itemTypeIcon = default;                 // ファイル種別アイコン
        [SerializeField] Sprite directoryIcon = default;               // ディレクトリ用アイコン
        [SerializeField] Sprite musicFileIcon = default;               // 音楽ファイル用アイコン
        [SerializeField] Sprite otherFileIcon = default;               // その他ファイル用アイコン

        string itemName;               // 表示名（ファイル名）
        FileItemInfo fileItemInfo;     // ファイル情報

        void Awake()
        {
            var text = GetComponentInChildren<Text>();
            var image = GetComponent<Image>();

            // 選択状態に応じて背景色・文字色を更新
            this.ObserveEveryValueChanged(_ => itemName == MusicSelector.SelectedFileName.Value)
                .Do(selected =>
                    image.color = selected ? selectedStateBackgroundColor : defaultBackgroundColor)
                .Subscribe(selected =>
                    text.color = selected ? selectedTextColor : defaultTextColor)
                .AddTo(this);
        }

        void Start()
        {
            // Prefab のスケールが崩れないように補正
            GetComponent<RectTransform>().localScale = Vector3.one;
        }

        /// <summary>
        /// ファイル情報を設定し、表示内容とアイコンを更新します。
        /// </summary>
        public void SetInfo(FileItemInfo info)
        {
            fileItemInfo = info;
            itemName = System.IO.Path.GetFileName(info.fullName);
            GetComponentInChildren<Text>().text = itemName;

            // アイコン切り替え
            itemTypeIcon.sprite = fileItemInfo.isDirectory
                ? directoryIcon
                : System.IO.Path.GetExtension(itemName) == ".wav"
                    ? musicFileIcon
                    : otherFileIcon;
        }

        /// <summary>
        /// クリック時の動作。
        /// ディレクトリなら移動、ファイルなら選択状態を更新します。
        /// </summary>
        public void OnMouseDown()
        {
            // ディレクトリを再クリック → そのディレクトリへ移動
            if (fileItemInfo.isDirectory && itemName == MusicSelector.SelectedFileName.Value)
            {
                MusicSelector.DirectoryPath.Value = fileItemInfo.fullName;
                return;
            }

            // ファイル選択
            MusicSelector.SelectedFileName.Value = itemName;
        }
    }
}
