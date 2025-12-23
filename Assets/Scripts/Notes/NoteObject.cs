using NoteEditor.GLDrawing;
using NoteEditor.Model;
using NoteEditor.presenter;
using NoteEditor.Utility;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

namespace NoteEditor.Notes
{
    public class NoteObject : IDisposable
    {
        public Note note = new Note();
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>();
        public Subject<Unit> a;
    }
}