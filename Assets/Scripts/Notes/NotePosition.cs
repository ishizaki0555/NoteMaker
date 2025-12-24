// ========================================
//
// NotePosition.cs
//
// ========================================
//
// ノートの位置情報（LPB / num / block）を保持するクラス
//
// ========================================

using UnityEngine;

namespace NoteMaker.Notes
{
    public class NotePosition
    {
        public int LPB, num, block;   // 拍子(LPB) / ノート番号(num) / ブロック(block)

        /// <summary>
        /// LPB・num・block を指定して位置を生成する。
        /// </summary>
        public NotePosition(int LPB, int num, int block)
        {
            this.LPB = LPB;
            this.num = num;
            this.block = block;
        }

        /// <summary>
        /// ノート位置をサンプル数に変換する。
        /// </summary>
        public int ToSamples(int Freqnency, int BPM)
        {
            return Mathf.FloorToInt(num * (Freqnency * 60 / BPM / LPB));
        }

        /// <summary>
        /// デバッグ用の文字列を返す。
        /// </summary>
        public override string ToString()
        {
            return LPB + "-" + num + "-" + block;
        }

        /// <summary>
        /// ノート位置が同じかどうかを判定する。
        /// </summary>
        public override bool Equals(object obj)
        {
            // 型が違う場合は一致しない
            if (!(obj is NotePosition))
            {
                return false;
            }

            NotePosition target = (NotePosition)obj;

            // LPB を考慮した num の比率と block が一致するか判定
            return (
                Mathf.Approximately((float)num / LPB, (float)target.num / target.LPB) &&
                block == target.block);
        }

        /// <summary>
        /// ハッシュコードを返す。
        /// </summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// 無効な位置を表す定数。
        /// </summary>
        public static NotePosition None
        {
            get { return new NotePosition(-1, -1, -1); }
        }

        /// <summary>
        /// 現在の位置に値を加算した新しい位置を返す。
        /// </summary>
        public NotePosition Add(int LPB, int num, int block)
        {
            return new NotePosition(this.LPB + LPB, this.num + num, this.block + block);
        }
    }
}
