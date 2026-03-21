// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// EditData.cs
// 楽曲編集に必要なメタ情報（曲名・レーン数・LPB・BPM・オフセット・難易度名）と
// ノーツ配置データを保持する中心的なデータモデルです。
// ReactiveProperty により UI や描画処理が自動的に更新される仕組みを提供します。
// 
//========================================

using NoteMaker.Notes;
using NoteMaker.Utility;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    public class BpmChange
    {
        public int tick;
        public float bpm;
        public BpmChange() { }

        public BpmChange(int tick, float bpm)
        {
            this.tick = tick;
            this.bpm = bpm;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BpmChange)) return false;
            var other = (BpmChange)obj;
            return tick == other.tick && Mathf.Approximately(bpm, other.bpm);
        }

        public override int GetHashCode()
        {
            return tick.GetHashCode() ^ bpm.GetHashCode();
        }
    }

    /// <summary>
    /// 楽曲編集に必要な設定値とノーツデータを保持するクラスです。
    /// 曲名、最大レーン数、LPB、BPM、オフセット、難易度名などの基本情報と、
    /// ノーツ配置データ（NotePosition → NoteObject）を管理します。
    /// 
    /// ReactiveProperty を用いているため、値が変更されると
    /// UI や描画処理が自動的に更新される仕組みになっています。
    /// </summary>
    public class EditData : SingletonMonoBehaviour<EditData>
    {
        ReactiveProperty<string> name_ = new ReactiveProperty<string>();                    // 楽曲名
        ReactiveProperty<int> maxBlock_ = new ReactiveProperty<int>(5);                     // 使用レーン数
        ReactiveProperty<int> LPB_ = new ReactiveProperty<int>(4);                          // 1 小節あたりの分割数
        ReactiveProperty<int> BPM_ = new ReactiveProperty<int>(120);                        // 楽曲 BPM
        ReactiveProperty<int> offsetSamples_ = new ReactiveProperty<int>(0);                // ノーツ開始位置のオフセット（サンプル単位）
        ReactiveProperty<string> difficultyName_ = new ReactiveProperty<string>("Easy");    // 難易度名
        ReactiveCollection<BpmChange> bpmChanges_ = new ReactiveCollection<BpmChange>();    

        Dictionary<NotePosition, NoteObject> notes_ = new Dictionary<NotePosition, NoteObject>(); // ノーツ配置データ

        /// <summary>
        /// 楽曲名を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<string> Name => Instance.name_;

        /// <summary>
        /// 使用レーン数（ブロック数）を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<int> MaxBlock => Instance.maxBlock_;

        /// <summary>
        /// 1 小節あたりの分割数（LPB）を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<int> LPB => Instance.LPB_;

        /// <summary>
        /// 楽曲 BPM を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<int> BPM => Instance.BPM_;

        /// <summary>
        /// ノーツ開始位置のオフセット（サンプル単位）を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<int> OffsetSamples => Instance.offsetSamples_;

        /// <summary>
        /// 難易度名を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<string> DifficultyName => Instance.difficultyName_;

        /// <summary>
        /// ノーツ配置データ（NotePosition → NoteObject）を公開します。
        /// ノーツの追加・削除・更新はこの辞書を通して行われます。
        /// </summary>
        public static Dictionary<NotePosition, NoteObject> Notes => Instance.notes_;

        /// <summary>どこでソフランを実行するかのデータ</summary>
        public static ReactiveCollection<BpmChange> BpmChanges => Instance.bpmChanges_;
    }
}
