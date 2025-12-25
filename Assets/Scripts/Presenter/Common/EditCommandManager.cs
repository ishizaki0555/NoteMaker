// ========================================
//
// EditCommandManager.cs
//
// ========================================
//
// 編集操作（ノート追加・削除・変更など）の Undo / Redo を管理するクラス。
// CommandManager をラップし、Ctrl+Z / Ctrl+Y による操作を提供する。
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class EditCommandManager : SingletonMonoBehaviour<EditCommandManager>
    {
        CommandManager commandManager = new CommandManager(); // Undo / Redo 管理クラス

        /// <summary>
        /// Undo / Redo の入力監視と、音声ロード時の初期化を設定する。
        /// </summary>
        private void Awake()
        {
            // 音声ロード後にコマンド履歴をクリア
            Audio.OnLoad
                .DelayFrame(1)
                .Subscribe(_ => Clear());

            // Ctrl + Z → Undo
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.Z))
                .Subscribe(_ => commandManager.Undo());

            // Ctrl + Y → Redo
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.Y))
                .Subscribe(_ => commandManager.Redo());
        }

        /// <summary>
        /// コマンドを実行し、Undo スタックに積む。
        /// </summary>
        public static void Do(Command command)
        {
            Instance.commandManager.Do(command);
        }

        /// <summary>
        /// コマンド履歴をクリアする。
        /// </summary>
        public static void Clear()
        {
            Instance.commandManager.Crear();
        }
    }
}
