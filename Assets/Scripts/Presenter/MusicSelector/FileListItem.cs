// ========================================
//
// FileListItem.cs
//
// ========================================
//
// 楽曲選択画面のファイル一覧に表示される 1 アイテムを管理するクラス。
// ・選択状態の背景色・文字色の切り替え
// ・アイコンの設定（ディレクトリ / wav / その他）
// ・クリック時のディレクトリ移動 / ファイル選択
//
// ========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class FileListItem : MonoBehaviour
    {
        [SerializeField] Color selectedStateBackgroundColor = default; // 選択時の背景色
        [SerializeField] Color defaultBackgroundColor = default;       // 通常時の背景色
        [SerializeField] Color selectedTextColor = default;            // 選択時の文字色
        [SerializeField] Color defaultTextColor = default;             // 通常時の文字色
        [SerializeField] Image itemTypeIcon = default;                 // アイコン表示
        [SerializeField] Sprite directoryIcon = default;               // ディレクトリ用アイコン
        [SerializeField] Sprite musicFileIcon = default;               // wav ファイル用アイコン
        [SerializeField] Sprite otherFileIcon = default;               // その他ファイル用アイコン

        string itemName;                                               // 表示名（ファイル名）
        FileItemInfo fileItemInfo;                                     // ファイル情報

        /// <summary>
        /// 選択状態に応じて背景色・文字色を更新する。
        /// </summary>
        private void Awake()
        {
            var text = GetComponentInChildren<Text>();
            var image = GetComponent<Image>();

            this.ObserveEveryValueChanged(_ => itemName == MusicSelector.SelectedFileName.Value)
                .Do(selected => image.color = selected ? selectedStateBackgroundColor : defaultBackgroundColor)
                .Subscribe(selected => text.color = selected ? selectedTextColor : defaultTextColor)
                .AddTo(this);
        }

        /// <summary>
        /// RectTransform のスケールを 1 に固定。
        /// （Prefab のスケールが影響しないようにするため）
        /// </summary>
        private void Start()
        {
            GetComponent<RectTransform>().localScale = Vector3.one;
        }

        /// <summary>
        /// ファイル情報を設定し、表示内容とアイコンを更新する。
        /// </summary>
        public void SetInfo(FileItemInfo info)
        {
            fileItemInfo = info;
            itemName = System.IO.Path.GetFileName(info.fullName);

            GetComponentInChildren<Text>().text = itemName;

            // アイコン設定
            itemTypeIcon.sprite = fileItemInfo.isDirectory
                ? directoryIcon
                : System.IO.Path.GetExtension(itemName) == ".wav"
                    ? musicFileIcon
                    : otherFileIcon;
        }

        /// <summary>
        /// クリック時の処理。
        /// ・ディレクトリの場合：同じ名前を選択中ならそのディレクトリへ移動
        /// ・ファイルの場合：選択状態を更新
        /// </summary>
        public void OnMouseDown()
        {
            if (fileItemInfo.isDirectory && itemName == MusicSelector.SelectedFileName.Value)
            {
                MusicSelector.DirectoryPath.Value = fileItemInfo.fullName;
                return;
            }

            MusicSelector.SelectedFileName.Value = itemName;
        }
    }
}
