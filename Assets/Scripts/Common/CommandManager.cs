using System.Collections.Generic;

namespace NoteEditor.Common
{
    public class CommandManager
    {
        Stack<Command> undoStack = new Stack<Command>();
        Stack<Command> redoStack = new Stack<Command>();

        public void Do(Command command)
        {
            command.Do();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void Crear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count == 0)
                return;

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }

        public void Redo()
        {
            if(undoStack.Count == 0) 
                return;

            var command = redoStack.Pop();
            command.Redo();
            redoStack.Push(command);
        }

        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        public bool CanRedo()
        {
            return redoStack.Count > 0;
        }
    }
}