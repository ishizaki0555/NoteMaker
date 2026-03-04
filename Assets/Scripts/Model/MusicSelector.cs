// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// MusicSelector.cs
// 楽曲フォルダのパス、フォルダ内のファイル一覧、選択中のファイル名を保持する
// シンプルなセレクタクラスです。ReactiveProperty により UI 側が自動更新されます。
// 
//========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UniRx;

namespace NoteMaker.Model
{
    /// <summary>
    /// 楽曲選択画面で使用するデータを管理するクラスです。
    /// ・現在開いているディレクトリのパス  
    /// ・そのディレクトリ内のファイル/フォルダ一覧  
    /// ・選択中のファイル名  
    /// を ReactiveProperty として公開し、UI が購読して自動更新できるようにします。
    /// </summary>
    public class MusicSelector : SingletonMonoBehaviour<MusicSelector>
    {
        ReactiveProperty<string> directoryPath_ = new ReactiveProperty<string>();
        // 現在開いているディレクトリのパス

        ReactiveProperty<List<FileItemInfo>> filePathList_ =
            new ReactiveProperty<List<FileItemInfo>>(new List<FileItemInfo>());
        // ディレクトリ内のファイル/フォルダ一覧

        ReactiveProperty<string> selectedFileName_ = new ReactiveProperty<string>();
        // 現在選択中のファイル名

        /// <summary>
        /// 現在開いているディレクトリのパスを公開します。
        /// </summary>
        public static ReactiveProperty<string> DirectoryPath => Instance.directoryPath_;

        /// <summary>
        /// ディレクトリ内のファイル/フォルダ一覧を公開します。
        /// FileItemInfo のリストとして保持されます。
        /// </summary>
        public static ReactiveProperty<List<FileItemInfo>> FilePathList => Instance.filePathList_;

        /// <summary>
        /// 現在選択中のファイル名を公開します。
        /// </summary>
        public static ReactiveProperty<string> SelectedFileName => Instance.selectedFileName_;
    }
}
