// ========================================
// 
// MusicDTO.cs
// 
// ========================================
// 
// jsonに保存するノーツデータの型
// 
// ========================================

using System.Collections.Generic;

namespace NoteMaker.DTO
{
    public class MusicDTO
    {
        [System.Serializable]
        public class EditData
        {
            public string songName;     // 曲名
            public int maxNum;          // レーン数
            public int BPM;             // BPM
            public int offset;          // ノーツ一個一個の間隔
            public List<Note> notes;    // ノーツ
        }

        [System.Serializable]
        public class Note
        {
            public int LPB;             // 置く時間
            public int num;             // ノーツ個別に割り振られる連番
            public int block;           // ブロック
            public int type;            // どのノーツか
            public List <Note> notes;   // ノーツ
        }
    }
}
