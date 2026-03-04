// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// CommandManager.cs
// Command クラスを用いて、操作の実行履歴を管理し、
// Undo / Redo を制御する機能を提供します。
// 
//========================================

using System.Collections.Generic;

namespace NoteMaker.Common
{
    /// <summary>
    /// Command オブジェクトを管理し、Undo / Redo の履歴制御を行うクラスです。
    /// 
    /// ・Do() で実行されたコマンドを undoStack に積む  
    /// ・Undo() で undoStack から取り出して取り消し、redoStack に積む  
    /// ・Redo() で redoStack から取り出してやり直し、undoStack に積む  
    /// 
    /// という仕組みにより、エディタ操作の履歴管理を実現します。
    /// </summary>
    public class CommandManager
    {
        private Stack<Command> undoStack = new Stack<Command>(); // Undo 用スタック
        private Stack<Command> redoStack = new Stack<Command>(); // Redo 用スタック

        /// <summary>
        /// コマンドを実行し、Undo スタックに積みます。
        /// Redo スタックはクリアされます。
        /// </summary>
        /// <param name="command">実行する Command オブジェクト。</param>
        public void Do(Command command)
        {
            command.Do();
            undoStack.Push(command);
            redoStack.Clear();
        }

        /// <summary>
        /// Undo / Redo の履歴をすべてクリアします。
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>
        /// Undo スタックの最新のコマンドを取り消し、
        /// Redo スタックに積み替えます。
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
        /// Redo スタックの最新のコマンドを再実行し、
        /// Undo スタックに積み替えます。
        /// </summary>
        public void Redo()
        {
            if (redoStack.Count == 0)
                return;

            var command = redoStack.Pop();
            command.Redo();
            undoStack.Push(command);
        }

        /// <summary>
        /// Undo が可能かどうかを返します。
        /// </summary>
        /// <returns>Undo スタックにコマンドが存在する場合 true。</returns>
        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        /// <summary>
        /// Redo が可能かどうかを返します。
        /// </summary>
        /// <returns>Redo スタックにコマンドが存在する場合 true。</returns>
        public bool CanRedo()
        {
            return redoStack.Count > 0;
        }
    }
}
