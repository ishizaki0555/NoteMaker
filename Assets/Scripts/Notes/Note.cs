// ========================================
//
// Note.cs
//
// ========================================
//
// ノート情報を保持するクラス
//
// ========================================

namespace NoteMaker.Notes
{
    public class Note
    {
        public NotePosition position = NotePosition.None; // ノートの位置
        public NoteTypes type = NoteTypes.Single;         // ノートの種類（単ノート / ロング）
        public NotePosition next = NotePosition.None;     // ロングノートの次の位置
        public NotePosition prev = NotePosition.None;     // ロングノートの前の位置

        /// <summary>
        /// 位置・種類・前後関係を指定してノートを生成する。
        /// </summary>
        public Note(NotePosition position, NoteTypes type, NotePosition next, NotePosition prev)
        {
            this.position = position;
            this.type = type;
            this.next = next;
            this.prev = prev;
        }

        /// <summary>
        /// 位置と種類を指定してノートを生成する。
        /// </summary>
        public Note(NotePosition position, NoteTypes type)
        {
            this.position = position;
            this.type = type;
        }

        /// <summary>
        /// 位置のみ指定してノートを生成する。
        /// </summary>
        public Note(NotePosition position)
        {
            this.position = position;
        }

        /// <summary>
        /// 別のノートをコピーして生成する。
        /// </summary>
        public Note(Note note)
        {
            this.position = note.position;
            this.type = note.type;
            this.next = note.next;
            this.prev = note.prev;
        }

        /// <summary>
        /// デフォルトコンストラクタ。
        /// </summary>
        public Note() { }

        /// <summary>
        /// ノート同士が同じ内容かどうかを比較する。
        /// </summary>
        public override bool Equals(object obj)
        {
            // 型が違う、または null の場合は一致しない
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var c = (Note)obj;

            // 位置・種類・前後関係がすべて一致しているか判定
            return position.Equals(c.position) &&
                   type == c.type &&
                   next.Equals(c.next) &&
                   prev.Equals(c.prev);
        }
    }
}
