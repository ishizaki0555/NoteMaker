// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// MusicDTO.cs
// 楽曲データおよびノーツ情報を保持するための
// データ転送オブジェクト（DTO）を定義します。
// 
//========================================

using System.Collections.Generic;

namespace NoteMaker.DTO
{
    /// <summary>
    /// 楽曲データ全体を保持する DTO クラスです。
    /// EditData はエディタで扱う楽曲設定およびノーツ情報をまとめた構造体です。
    /// </summary>
    public class MusicDTO
    {
        /// <summary>
        /// 楽曲編集に必要なメタ情報とノーツ情報を保持するデータ構造です。
        /// </summary>
        [System.Serializable]
        public class EditData
        {
            public string name;          // 楽曲名
            public int maxBlock;         // 使用可能なレーン（ブロック）数
            public int maxLPB;           // 譜面内で使用されている最大のLPB数
            public int BPM;              // 楽曲の BPM（テンポ）
            public int offset;           // ノーツ開始位置のオフセット
            public List<BpmChangeDTO> bpmChanges; // BPM変更イベントリスト
            public List<Note> notes;     // 配置されたノーツ一覧
        }

        /// <summary>
        /// ノーツ情報を保持するデータ構造です。
        /// 単体ノーツだけでなく、ロングノーツの子ノーツも保持できます。
        /// </summary>
        [System.Serializable]
        public class Note
        {
            public int LPB;              // 1 小節あたりの分割数（Line Per Beat）
            public int num;              // 小節内での位置（LPB 基準）
            public int block;            // 配置レーン（ブロック番号）
            public int type;             // ノーツ種別（タップ・ロングなど）
            public List<Note> notes;     // ロングノーツなどの子ノーツ
        }

        [System.Serializable]
        public class BpmChangeDTO
        {
            public int tick;            // LPB基準のノーツ位置（num）
            public float bpm;           // 変更後のBPM
        }
    }
}
