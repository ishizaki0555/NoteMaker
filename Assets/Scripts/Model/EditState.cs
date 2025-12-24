// ========================================
//
// EditState.cs
//
// ========================================
//
// 譜面編集時の状態（ノート種別・ロングノートの末尾位置など）を保持するシングルトン
//
// ========================================

using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Model
{
    public class EditState : SingletonMonoBehaviour<EditState>
    {
        ReactiveProperty<bool> isOperatingPlaybackPositionDuringPlay_ = new ReactiveProperty<bool>(false); // 再生中に再生位置を操作しているか
        ReactiveProperty<NoteTypes> noteType_ = new ReactiveProperty<NoteTypes>(NoteTypes.Single);         // 現在選択中のノート種別
        ReactiveProperty<NotePosition> longNoteTailPosition_ = new ReactiveProperty<NotePosition>();       // ロングノートの末尾位置

        /// <summary>
        /// 再生中に再生位置を操作しているかどうか
        /// </summary>
        public static ReactiveProperty<bool> IsOperatingPlaybackPositionDuringPlay
        {
            get { return Instance.isOperatingPlaybackPositionDuringPlay_; }
        }

        /// <summary>
        /// 現在選択されているノート種別
        /// </summary>
        public static ReactiveProperty<NoteTypes> NoteType
        {
            get { return Instance.noteType_; }
        }

        /// <summary>
        /// ロングノートの末尾位置
        /// </summary>
        public static ReactiveProperty<NotePosition> LongNoteTailPosition
        {
            get { return Instance.longNoteTailPosition_; }
        }
    }
}
