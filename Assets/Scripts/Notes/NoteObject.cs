using NoteMaker.GLDrawing;
using NoteMaker.Model;
using NoteMaker.Presenter;
using NoteMaker.Utility;
using NUnit.Framework.Constraints;
using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Notes
{
    public class NoteObject : IDisposable
    {
        public Note note = new Note();
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>();
        public Subject<Unit> LateUpdateObdervable = new Subject<Unit>();
        public Subject<Unit> OnClickObservable = new Subject<Unit>();
        public Color NoteColor { get { return noteColor_.Value; } }
        ReactiveProperty<Color> noteColor_ = new ReactiveProperty<Color>();

        Color selectedStateColor = new Color(255 / 255f, 0 / 255f, 255 / 255f);
        Color singleNoteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);
        Color longNoteColor = new Color(0 / 255f, 255f, 255 / 255f);
        Color invalidStateColor = new Color(255 / 255f, 0 / 255f, 0 / 255f);

        ReactiveProperty<NoteTypes> noteType = new ReactiveProperty<NoteTypes>();
        CompositeDisposable disposable = new CompositeDisposable();
    }
}