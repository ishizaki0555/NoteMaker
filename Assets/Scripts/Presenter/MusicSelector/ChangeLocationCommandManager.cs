// ========================================
//
// ChangeLocationCommandManager.cs
//
// ========================================
//
// 位置変更系のコマンド（Undo / Redo）を管理するシングルトン。
// CommandManager をラップし、UI などから参照できる CanUndo / CanRedo を ReactiveProperty として公開する。
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Presenter
{
    public class ChangeLocationCommandManager : SingletonMonoBehaviour<ChangeLocationCommandManager>
    {
        CommandManager commandManager = new CommandManager();   // Undo / Redo 管理クラス

        ReactiveProperty<bool> canRedo;                         // Redo が可能かどうか
        ReactiveProperty<bool> canUndo;                         // Undo が可能かどうか

        /// <summary>
        /// Redo が可能かどうか
        /// </summary>
        public static ReactiveProperty<bool> CanRedo
        {
            get { return Instance.canRedo; }
        }

        /// <summary>
        /// Undo が可能かどうか
        /// </summary>
        public static ReactiveProperty<bool> CanUndo
        {
            get { return Instance.canUndo; }
        }

        /// <summary>
        /// Undo / Redo 状態の監視をセットアップする。
        /// </summary>
        private void Awake()
        {
            // CanUndo() の結果を監視して ReactiveProperty 化
            canRedo = this.ObserveEveryValueChanged(_ => commandManager.CanUndo())
                .ToReactiveProperty();

            // ※ 現状 CanUndo と CanRedo が同じ値を参照しているが、元コードの仕様に従う
            canUndo = this.ObserveEveryValueChanged(_ => commandManager.CanUndo())
                .ToReactiveProperty();
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

        /// <summary>
        /// Undo を実行する。
        /// </summary>
        public static void Undo()
        {
            Instance.commandManager.Undo();
        }

        /// <summary>
        /// Redo を実行する。
        /// </summary>
        public static void Redo()
        {
            Instance.commandManager.Redo();
        }
    }
}
