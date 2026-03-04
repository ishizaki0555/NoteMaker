// ======================================== 
// 
// NoetMaker Project 
// 
// ======================================== 
// 
// Command.cs 
// コマンドパターンを用いて、操作の実行・取り消し・やり直しを管理します。 
// 
//========================================

using System;

namespace NoteMaker.Common
{
    public class Command
    {
        private Action doAction;    // 実行処理を保持するデリゲート
        private Action undoAction;  // Undo（取り消し）処理を保持するデリゲート
        private Action redoAction;  // Redo（やり直し）処理を保持するデリゲート。

        /// <summary>
        /// 実行・Undo・Redo の 3 種類の処理を指定して Command を生成します。
        /// </summary>
        /// <param name="doAction">実行時に呼び出される処理。</param>
        /// <param name="undoAction">Undo（取り消し）時に呼び出される処理。</param>
        /// <param name="redoAction">Redo（やり直し）時に呼び出される処理。</param>
        public Command(Action doAction, Action undoAction, Action redoAction)
        {
            this.doAction = doAction;
            this.undoAction = undoAction;
            this.redoAction = redoAction;
        }

        /// <summary>
        /// 実行処理と Undo 処理のみを指定して Command を生成します。
        /// Redo 処理は doAction と同じ動作になります。
        /// </summary>
        /// <param name="doAction">実行時に呼び出される処理。</param>
        /// <param name="undoAction">Undo（取り消し）時に呼び出される処理。</param>
        public Command(Action doAction, Action undoAction)
        {
            this.doAction = doAction;
            this.undoAction = undoAction;
            this.redoAction = doAction; // Redo は doAction と同じ処理
        }

        /// <summary>
        /// 登録された実行処理を呼び出します。
        /// </summary>
        public void Do()
        {
            doAction();
        }

        /// <summary>
        /// 登録された Undo（取り消し）処理を呼び出します。
        /// </summary>
        public void Undo()
        {
            undoAction();
        }

        /// <summary>
        /// 登録された Redo（やり直し）処理を呼び出します。
        /// </summary>
        public void Redo()
        {
            redoAction();
        }
    }
}
