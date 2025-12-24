// ========================================
//
// MusicSelector.cs
//
// ========================================
//
// 楽曲選択画面で使用する状態（ディレクトリパス・ファイル一覧・選択中のファイル名）
// を保持するシングルトン
//
// ========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UniRx;

namespace NoteMaker.Model
{
    public class MusicSelector : SingletonMonoBehaviour<MusicSelector>
    {
        ReactiveProperty<string> directoryPath_ = new ReactiveProperty<string>();                                                   // 現在開いているディレクトリのパス
        ReactiveProperty<List<FileItemInfo>> filePathList_ = new ReactiveProperty<List<FileItemInfo>>(new List<FileItemInfo>());    // ディレクトリ内のファイル一覧
        ReactiveProperty<string> selectedFileName_ = new ReactiveProperty<string>();                                                // 選択中のファイル名

        /// <summary>
        /// 現在開いているディレクトリのパス
        /// </summary>
        public static ReactiveProperty<string> DirectoryPath
        {
            get { return Instance.directoryPath_; }
        }

        /// <summary>
        /// ディレクトリ内のファイル一覧
        /// </summary>
        public static ReactiveProperty<List<FileItemInfo>> FilePathList
        {
            get { return Instance.filePathList_; }
        }

        /// <summary>
        /// 選択中のファイル名
        /// </summary>
        public static ReactiveProperty<string> SelectedFileName
        {
            get { return Instance.selectedFileName_; }
        }
    }
}
