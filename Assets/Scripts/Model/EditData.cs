// ========================================
//
// EditData.cs
//
// ========================================
//
// 譜面編集データを保持するシングルトン
//
// ========================================

using NoteMaker.Utility;
using NoteMaker.Notes;
using System.Collections.Generic;
using UniRx;

namespace NoteMaker.Model
{
    public class EditData : SingletonMonoBehaviour<EditData>
    {
        ReactiveProperty<string> name_ = new ReactiveProperty<string>();                            // 曲名
        ReactiveProperty<int> maxBloak_ = new ReactiveProperty<int>(5);                             // 最大ブロック数
        ReactiveProperty<int> LPB_ = new ReactiveProperty<int>(4);                                  // 拍子（Lines Per Beat）
        ReactiveProperty<int> BPM_ = new ReactiveProperty<int>(120);                                // BPM
        ReactiveProperty<int> offsetSamples_ = new ReactiveProperty<int>(0);                        // 音声のオフセット（サンプル）
        Dictionary<NotePosition, NoteObject> notes_ = new Dictionary<NotePosition, NoteObject>();   // ノート一覧

        public static ReactiveProperty<string> Name { get { return Instance.name_; } }
        public static ReactiveProperty<int> MaxBloak { get { return Instance.maxBloak_; } }
        public static ReactiveProperty<int> LPB { get { return Instance.LPB_; } }
        public static ReactiveProperty<int> BPM { get { return Instance.BPM_; } }
        public static ReactiveProperty<int> OffsetSamples { get { return Instance.offsetSamples_; } }
        public static Dictionary<NotePosition, NoteObject> Notes { get { return Instance.notes_; } }
    }
}
