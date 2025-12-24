using System;

namespace NoteMaker.Common
{
    /// <summary>
    /// 単一の操作（Do / Undo / Redo）をまとめて扱うためのコマンドクラス。
    /// Undo/Redo 機能を持つアプリでよく使われるパターン。
    /// </summary>
    public class Command
    {
        Action doAction;    // 実行時の処理
        Action undoAction;  // Undo（取り消し）時の処理
        Action redoAction;  // Redo（やり直し）時の処理

        /// <summary>
        /// Do / Undo / Redo を個別に指定するコンストラクタ。
        /// </summary>
        public Command(Action doAction, Action undoAction, Action redoAction)
        {
            this.doAction = doAction;
            this.undoAction = undoAction;
            this.redoAction = redoAction;
        }

        /// <summary>
        /// Redo が Do と同じ処理で良い場合の簡易コンストラクタ。
        /// </summary>
        public Command(Action doAction, Action undoAction)
        {
            this.doAction = doAction;
            this.undoAction = undoAction;

            // Redo は Do と同じ動作を行う
            this.redoAction = doAction;
        }

        /// <summary>
        /// コマンドを実行する。
        /// </summary>
        public void Do() { doAction(); }

        /// <summary>
        /// コマンドを取り消す。
        /// </summary>
        public void Undo() { undoAction(); }

        /// <summary>
        /// コマンドをやり直す。
        /// </summary>
        public void Redo() { redoAction(); }
    }
}
