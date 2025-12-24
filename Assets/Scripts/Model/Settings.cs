// ========================================
//
// Settings.cs
//
// ========================================
//
// エディタ設定（ワークスペースパス・入力キー設定・選択ブロックなど）
// を保持するシングルトン
//
// ========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    public class Settings : SingletonMonoBehaviour<Settings>
    {
        ReactiveProperty<string> workSpacePath_ = new ReactiveProperty<string>();                   // ワークスペースのパス
        ReactiveProperty<List<KeyCode>> noteInputKeyCodes_ = new ReactiveProperty<List<KeyCode>>(); // ノート入力に使用するキー一覧
        ReactiveProperty<int> selectedBlock_ = new ReactiveProperty<int>();                         // 現在選択中のブロック番号
        ReactiveProperty<bool> isOpen_ = new ReactiveProperty<bool>(false);                         // 設定ウィンドウが開いているか
        Subject<Unit> requestForChangeInputNoteKeyCode_ = new Subject<Unit>();                      // 入力キー変更要求イベント

        public static ReactiveProperty<string> WorkSpacePath
        {
            get { return Instance.workSpacePath_; }
        }

        public static ReactiveProperty<List<KeyCode>> NoteInputKeyCodes
        {
            get { return Instance.noteInputKeyCodes_; }
        }

        public static ReactiveProperty<int> SelectedBlock
        {
            get { return Instance.selectedBlock_; }
        }

        public static ReactiveProperty<bool> IsOpen
        {
            get { return Instance.isOpen_; }
        }

        /// <summary>
        /// ノート入力キー変更要求イベント
        /// </summary>
        public static Subject<Unit> RequestForChangeInputNoteKeyCode
        {
            get { return Instance.requestForChangeInputNoteKeyCode_; }
        }

        /// <summary>
        /// ブロック数の最大値
        /// </summary>
        public static int MaxBlock = 0;
    }
}
