// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// Note.cs
// ノート 1 個分のデータ（位置・種類・Long ノーツの連結情報）を保持する
// 基本的なデータクラスです。コピーコンストラクタや複数の生成方法を備え、
// EditData や描画処理で扱いやすい構造になっています。
// 
//========================================

namespace NoteMaker.Notes
{
    /// <summary>
    /// ノート 1 個分のデータを保持するクラスです。
    /// ・position … ノートの位置（LPB / 拍 / ブロック）  
    /// ・type … Single / Long  
    /// ・next / prev … Long ノーツの連結情報  
    /// </summary>
    public class Note
    {
        public NotePosition position = NotePosition.None; // ノートの位置
        public NoteTypes type = NoteTypes.Single;         // ノートの種類（Single / Long）
        public NotePosition next = NotePosition.None;     // Long ノーツの次ノート位置
        public NotePosition prev = NotePosition.None;     // Long ノーツの前ノート位置

        /// <summary>
        /// 位置・種類・連結情報をすべて指定して生成します。
        /// </summary>
        public Note(NotePosition position, NoteTypes type, NotePosition next, NotePosition prev)
        {
            this.position = position;
            this.type = type;
            this.next = next;
            this.prev = prev;
        }

        /// <summary>
        /// 位置と種類のみ指定して生成します。
        /// </summary>
        public Note(NotePosition position, NoteTypes type)
        {
            this.position = position;
            this.type = type;
        }

        /// <summary>
        /// 位置のみ指定して生成します（Single ノーツ扱い）。
        /// </summary>
        public Note(NotePosition position)
        {
            this.position = position;
        }

        /// <summary>
        /// コピーコンストラクタ。既存の Note を複製します。
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
        /// ノートの内容が等しいかどうかを判定します。
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var c = (Note)obj;

            return position.Equals(c.position) &&
                   type == c.type &&
                   next.Equals(c.next) &&
                   prev.Equals(c.prev);
        }

        /// <summary>
        /// ハッシュコードを返します（必要に応じて拡張可能）。
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
