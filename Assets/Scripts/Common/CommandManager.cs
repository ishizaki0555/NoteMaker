// ========================================
//
// CommandManager.cs
//
// ========================================
//
// コマンドの実行・取り消し・やり直しを管理するクラス
//
// ========================================

using System.Collections.Generic;

namespace NoteMaker.Common
{
    public class CommandManager
    {
        Stack<Command> undoStack = new Stack<Command>(); // Undo 用スタック
        Stack<Command> redoStack = new Stack<Command>(); // Redo 用スタック

        /// <summary>
        /// コマンドを実行し、Undo スタックに積む。
        /// 新しい操作が行われたため Redo スタックはクリアされる。
        /// </summary>
        public void Do(Command command)
        {
            command.Do();
            undoStack.Push(command);
            redoStack.Clear();
        }

        /// <summary>
        /// Undo / Redo の履歴をすべてクリアする。
        /// </summary>
        public void Crear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>
        /// 直前の操作を取り消し、Redo スタックに移動する。
        /// </summary>
        public void Undo()
        {
            if (undoStack.Count == 0)
                return;

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }

        /// <summary>
        /// Undo した操作をやり直す。
        /// </summary>
        public void Redo()
        {
            if (undoStack.Count == 0) // ※ 元コードのまま
                return;

            var command = redoStack.Pop();
            command.Redo();
            redoStack.Push(command); // ※ 元コードのまま
        }

        /// <summary>
        /// Undo が可能かどうかを返す。
        /// </summary>
        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        /// <summary>
        /// Redo が可能かどうかを返す。
        /// </summary>
        public bool CanRedo()
        {
            return redoStack.Count > 0;
        }
    }
}
