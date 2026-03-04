// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// EditCommandManager.cs
// Undo / Redo を管理する CommandManager を保持し、
// Ctrl+Z / Ctrl+Y の入力を監視して実行するプレゼンター層の管理クラスです。
// Audio 読み込み時には履歴をクリアし、常に正しい編集状態を保ちます。
// 
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 編集コマンド（Undo / Redo）を管理するクラスです。
    /// ・Ctrl+Z → Undo  
    /// ・Ctrl+Y → Redo  
    /// ・Audio 読み込み時に履歴クリア  
    /// 
    /// CommandManager を内部に保持し、外部からは静的メソッド経由で操作できます。
    /// </summary>
    public class EditCommandManager : SingletonMonoBehaviour<EditCommandManager>
    {
        CommandManager commandManager = new CommandManager(); // Undo/Redo 管理本体

        /// <summary>
        /// 入力監視と Audio 読み込み時の初期化を設定します。
        /// </summary>
        void Awake()
        {
            // Audio 読み込み後に履歴をクリア
            Audio.OnLoad
                .DelayFrame(1)
                .Subscribe(_ => Clear());

            // Ctrl+Z → Undo
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.Z))
                .Subscribe(_ => commandManager.Undo());

            // Ctrl+Y → Redo
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.Y))
                .Subscribe(_ => commandManager.Redo());
        }

        /// <summary>
        /// コマンドを実行し、Undo スタックに積みます。
        /// </summary>
        public static void Do(Command command)
        {
            Instance.commandManager.Do(command);
        }

        /// <summary>
        /// Undo / Redo 履歴をすべてクリアします。
        /// </summary>
        public static void Clear()
        {
            Instance.commandManager.Clear();
        }
    }
}
