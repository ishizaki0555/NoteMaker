// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// NotePosition.cs
// ノートの位置情報（LPB / 拍数 num / ブロック block）を保持する
// 軽量な値型データ構造です。サンプル位置変換や比較処理も提供します。
// 
//========================================

using UnityEngine;

namespace NoteMaker.Notes
{
    /// <summary>
    /// ノートの位置情報を保持する構造体です。
    /// ・LPB … 1 小節あたりの分割数  
    /// ・num … 拍位置（LPB 基準の整数）  
    /// ・block … レーン番号  
    /// 
    /// サンプル位置への変換、比較、加算などのユーティリティも備えています。
    /// </summary>
    public struct NotePosition
    {
        public int LPB;   // 1 小節あたりの分割数
        public int num;   // 拍位置（LPB 基準）
        public int block; // レーン番号

        /// <summary>
        /// LPB / 拍位置 / ブロックを指定して生成します。
        /// </summary>
        public NotePosition(int LPB, int num, int block)
        {
            this.LPB = LPB;
            this.num = num;
            this.block = block;
        }

        /// <summary>
        /// この位置をサンプル位置に変換します。
        /// BPM と LPB に基づいて、拍位置 → サンプル数へ変換します。
        /// </summary>
        public int ToSamples(int frequency, float BPM, System.Collections.Generic.IEnumerable<NoteMaker.Model.BpmChange> changes = null)
        {
            return NoteMaker.Utility.BPMUtility.CalculateSamples(frequency, BPM, LPB, num, changes);
        }

        /// <summary>
        /// "LPB-num-block" の形式で文字列化します。
        /// </summary>
        public override string ToString()
        {
            return LPB + "-" + num + "-" + block;
        }

        /// <summary>
        /// ノート位置の等価判定を行います。
        /// LPB が異なる場合でも、拍位置の割合が一致すれば同じ位置とみなします。
        /// </summary>
        public override bool Equals(object obj)
        {
            // 型がなくなる場合は透過でないのみなす
            if (!(obj is NotePosition))
                return false;

            NotePosition target = (NotePosition)obj;

            // LPB が異なっても、拍位置の割合が同じなら同じ位置とみなす
            return Mathf.Approximately((float)num / LPB, (float)target.num / target.LPB)
                && block == target.block;
        }

        /// <summary>
        /// ハッシュコードを返します。
        /// </summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// 無効な位置を表す定数（-1, -1, -1）。
        /// </summary>
        public static NotePosition None
        {
            get { return new NotePosition(-1, -1, -1); }
        }

        /// <summary>
        /// 現在の位置に指定値を加算した新しい NotePosition を返します。
        /// </summary>
        public NotePosition Add(int LPB, int num, int block)
        {
            return new NotePosition(this.LPB + LPB, this.num + num, this.block + block);
        }
    }
}
