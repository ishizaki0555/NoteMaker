using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    public class NoteCanvas : SingletonMonoBehaviour<NoteCanvas>
    {
        ReactiveProperty<float> height_ = new ReactiveProperty<float>();
        ReactiveProperty<float> width_ = new ReactiveProperty<float>();
        ReactiveProperty<float> offsetY_ = new ReactiveProperty<float>();
        ReactiveProperty<float> scaleFactor_ = new ReactiveProperty<float>();
        ReactiveProperty<bool> isMouseOverNotesRegion_ = new ReactiveProperty<bool>();
        ReactiveProperty<bool> isMouseOverWaveformRegion_ = new ReactiveProperty<bool>();
        ReactiveProperty<NotePosition> closestNotePosition_ = new ReactiveProperty<NotePosition>();

        public static ReactiveProperty<float> Height => Instance.height_;
        public static ReactiveProperty<float> Width => Instance.width_;
        public static ReactiveProperty<float> OffsetY => Instance.offsetY_;
        public static ReactiveProperty<float> ScaleFactor => Instance.scaleFactor_;
        public static ReactiveProperty<bool> IsMouseOverNotesRegion => Instance.isMouseOverNotesRegion_;
        public static ReactiveProperty<bool> IsMouseOverWaveformRegion => Instance.isMouseOverWaveformRegion_;
        public static ReactiveProperty<NotePosition> ClosestNotePosition => Instance.closestNotePosition_;

        void Awake()
        {
            this.ObserveEveryValueChanged(_ => Screen.height)
                .DistinctUntilChanged()
                .Subscribe(w => ScaleFactor.Value = 720f / w);
        }
    }
}
