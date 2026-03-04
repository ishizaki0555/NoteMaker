// ========================================
//
// NoteMaker Project
//
// ========================================
//
// ChangeLocationCommandManager.cs
// ノート位置変更など「位置に関する操作」の Undo / Redo を管理する
// コマンドマネージャです。CommandManager を内部に保持し、
// CanUndo / CanRedo を ReactiveProperty として公開することで、
// UI ボタンの活性・非活性制御にも利用できます。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 位置変更系コマンドの Undo / Redo を管理するクラスです。
    /// ・CanUndo / CanRedo を ReactiveProperty として公開  
    /// ・UI から Undo / Redo ボタンの活性状態を監視可能  
    /// ・Do / Clear / Undo / Redo を静的メソッドで提供  
    /// </summary>
    public class ChangeLocationCommandManager : SingletonMonoBehaviour<ChangeLocationCommandManager>
    {
        CommandManager commandManager = new CommandManager(); // Undo/Redo 管理本体

        ReactiveProperty<bool> canRedo; // Redo 可能かどうか
        ReactiveProperty<bool> canUndo; // Undo 可能かどうか

        /// <summary>
        /// Redo 可能状態を公開します。
        /// </summary>
        public static ReactiveProperty<bool> CanRedo => Instance.canRedo;

        /// <summary>
        /// Undo 可能状態を公開します。
        /// </summary>
        public static ReactiveProperty<bool> CanUndo => Instance.canUndo;

        /// <summary>
        /// CanUndo / CanRedo を監視する ReactiveProperty を初期化します。
        /// </summary>
        void Awake()
        {
            canRedo = this.ObserveEveryValueChanged(_ => commandManager.CanRedo())
                .ToReactiveProperty();

            canUndo = this.ObserveEveryValueChanged(_ => commandManager.CanUndo())
                .ToReactiveProperty();
        }

        /// <summary>
        /// コマンドを実行し Undo スタックに積みます。
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

        /// <summary>
        /// Undo を実行します。
        /// </summary>
        public static void Undo()
        {
            Instance.commandManager.Undo();
        }

        /// <summary>
        /// Redo を実行します。
        /// </summary>
        public static void Redo()
        {
            Instance.commandManager.Redo();
        }
    }
}
