// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// EditState.cs
// エディタ操作中の一時的な状態（再生中の位置操作・選択中ノートタイプ・
// Long ノーツの末尾位置など）を保持するステート管理クラスです。
// ReactiveProperty により UI や描画処理が自動的に更新されます。
// 
//========================================

using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Model
{
    /// <summary>
    /// エディタ操作中の一時的な状態を管理するクラスです。
    /// ・再生中に再生位置を操作しているか  
    /// ・現在選択中のノートタイプ（Single / Long）  
    /// ・Long ノーツの末尾位置（連結処理用）  
    /// といった編集操作に関わるフラグを ReactiveProperty として公開します。
    /// </summary>
    public class EditState : SingletonMonoBehaviour<EditState>
    {
        // 再生中にユーザーが再生位置を操作しているかどうか
        ReactiveProperty<bool> isOperatingPlaybackPositionDuringPlay_ = new ReactiveProperty<bool>(false);

        // 現在選択中のノートタイプ（Single / Long）
        ReactiveProperty<NoteTypes> noteType_ = new ReactiveProperty<NoteTypes>(NoteTypes.Single);

        // Long ノーツの末尾位置（連結処理のために保持）
        ReactiveProperty<NotePosition> longNoteTailPosition_ = new ReactiveProperty<NotePosition>();

        /// <summary>
        /// 再生中に再生位置を操作しているかどうかを公開します。
        /// </summary>
        public static ReactiveProperty<bool> IsOperatingPlaybackPositionDuringPlay
            => Instance.isOperatingPlaybackPositionDuringPlay_;

        /// <summary>
        /// 現在選択中のノートタイプを公開します。
        /// </summary>
        public static ReactiveProperty<NoteTypes> NoteType
            => Instance.noteType_;

        /// <summary>
        /// Long ノーツの末尾位置を公開します。
        /// Long ノーツ配置時の連結処理に使用されます。
        /// </summary>
        public static ReactiveProperty<NotePosition> LongNoteTailPosition
            => Instance.longNoteTailPosition_;
    }
}
