// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// Settings.cs
// エディタ全体の設定（ワークスペースパス、ノート入力キー、選択中ブロック、
// 設定ウィンドウの開閉状態など）を保持する設定クラスです。
// ReactiveProperty により UI 側が自動更新される仕組みを提供します。
// 
//========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    /// <summary>
    /// エディタの設定値を管理するクラスです。
    /// ・ワークスペースのパス  
    /// ・ノート入力に使用するキー設定  
    /// ・現在選択中のブロック番号  
    /// ・設定ウィンドウの開閉状態  
    /// ・ノート入力キー変更要求イベント  
    /// などを ReactiveProperty / Subject として公開します。
    /// </summary>
    public class Settings : SingletonMonoBehaviour<Settings>
    {
        // 作業フォルダ（ワークスペース）のパス
        ReactiveProperty<string> workSpacePath_ = new ReactiveProperty<string>();

        // ノート入力に使用するキーの一覧
        ReactiveProperty<List<KeyCode>> noteInputKeyCodes_ =
            new ReactiveProperty<List<KeyCode>>();

        // 現在選択中のブロック番号
        ReactiveProperty<int> selectedBlock_ = new ReactiveProperty<int>();

        // 設定ウィンドウが開いているかどうか
        ReactiveProperty<bool> isOpen_ = new ReactiveProperty<bool>(false);

        // ノート入力キー変更要求イベント
        Subject<Unit> requestForChangeInputNoteKeyCode_ = new Subject<Unit>();

        /// <summary>ワークスペースのパス。</summary>
        public static ReactiveProperty<string> WorkSpacePath => Instance.workSpacePath_;

        /// <summary>ノート入力に使用するキーの一覧。</summary>
        public static ReactiveProperty<List<KeyCode>> NoteInputKeyCodes => Instance.noteInputKeyCodes_;

        /// <summary>現在選択中のブロック番号。</summary>
        public static ReactiveProperty<int> SelectedBlock => Instance.selectedBlock_;

        /// <summary>設定ウィンドウが開いているかどうか。</summary>
        public static ReactiveProperty<bool> IsOpen => Instance.isOpen_;

        /// <summary>ノート入力キー変更要求イベント。</summary>
        public static Subject<Unit> RequestForChangeInputNoteKeyCode
            => Instance.requestForChangeInputNoteKeyCode_;

        /// <summary>
        /// 最大ブロック数（外部から参照される固定値）。
        /// EditData.MaxBlock と連動させる場合は別途処理が必要。
        /// </summary>
        public static int MaxBlock = 0;
    }
}
