// ========================================
//
// EditNotesPresenter.cs
//
// ========================================
//
// ノート編集全般を管理する Presenter。
// ・ノート追加 / 削除
// ・ロングノート編集（開始 / 接続 / 終了）
// ・クリック処理
// ・Undo / Redo 連携
//
// NoteObject や EditData と密接に連携し、
// ノート編集の中心的な役割を担う。
//
// ========================================

using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Notes;
using NoteMaker.Utility;

namespace NoteMaker.Presenter
{
    public class EditNotesPresenter : SingletonMonoBehaviour<EditNotesPresenter>
    {
        [SerializeField] CanvasEvents canvasEvents = default;

        // ノート編集要求イベント
        public readonly Subject<Note> RequestForEditNote = new Subject<Note>();
        public readonly Subject<Note> RequestForRemoveNote = new Subject<Note>();
        public readonly Subject<Note> RequestForAddNote = new Subject<Note>();
        public readonly Subject<Note> RequestForChangeNoteStatus = new Subject<Note>();

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// ノート編集処理の初期化。
        /// </summary>
        void Init()
        {
            // ----------------------------------------
            // ノート領域クリック（最寄りノート位置が有効）
            // ----------------------------------------
            var closestNoteAreaOnMouseDownObservable = canvasEvents.NotesRegionOnMouseDownObservable
                .Where(_ => !KeyInput.CtrlKey())
                .Where(_ => !Input.GetMouseButtonDown(1))
                .Where(_ => 0 <= NoteCanvas.ClosestNotePosition.Value.num);

            // ----------------------------------------
            // 単ノート or ロングノートのクリック処理
            // ----------------------------------------
            closestNoteAreaOnMouseDownObservable
                .Where(_ => EditState.NoteType.Value == NoteTypes.Single)
                .Where(_ => !KeyInput.ShiftKey())
                .Merge(closestNoteAreaOnMouseDownObservable
                    .Where(_ => EditState.NoteType.Value == NoteTypes.Long))
                .Subscribe(_ =>
                {
                    if (EditData.Notes.ContainsKey(NoteCanvas.ClosestNotePosition.Value))
                    {
                        // 既存ノート → NoteObject のクリック処理へ
                        EditData.Notes[NoteCanvas.ClosestNotePosition.Value]
                            .OnClickObservable.OnNext(Unit.Default);
                    }
                    else
                    {
                        // 新規ノート追加
                        RequestForEditNote.OnNext(
                            new Note(
                                NoteCanvas.ClosestNotePosition.Value,
                                EditState.NoteType.Value,
                                NotePosition.None,
                                EditState.LongNoteTailPosition.Value));
                    }
                });

            // ----------------------------------------
            // Shift + クリック → ロングノート編集開始
            // ----------------------------------------
            closestNoteAreaOnMouseDownObservable
                .Where(_ => EditState.NoteType.Value == NoteTypes.Single)
                .Where(_ => KeyInput.ShiftKey())
                .Do(_ => EditState.NoteType.Value = NoteTypes.Long)
                .Subscribe(_ =>
                    RequestForAddNote.OnNext(
                        new Note(
                            NoteCanvas.ClosestNotePosition.Value,
                            NoteTypes.Long,
                            NotePosition.None,
                            NotePosition.None)));

            // ----------------------------------------
            // ロングノート編集終了（Esc or 右クリック）
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => EditState.NoteType.Value == NoteTypes.Long)
                .Where(_ => Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                .Subscribe(_ => EditState.NoteType.Value = NoteTypes.Single);

            // ロングノート終了時は末尾位置をリセット
            EditState.NoteType
                .Where(editType => editType == NoteTypes.Single)
                .Subscribe(_ => EditState.LongNoteTailPosition.Value = NotePosition.None);

            // ----------------------------------------
            // ノート削除（Undo/Redo 対応）
            // ----------------------------------------
            RequestForRemoveNote
                .Buffer(RequestForRemoveNote.ThrottleFrame(1))
                .Select(b => b.OrderBy(note =>
                    note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(RemoveNote),
                            () => notes.ForEach(AddNote))));

            // ----------------------------------------
            // ノート追加（Undo/Redo 対応）
            // ----------------------------------------
            RequestForAddNote
                .Buffer(RequestForAddNote.ThrottleFrame(1))
                .Select(b => b.OrderBy(note =>
                    note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(AddNote),
                            () => notes.ForEach(RemoveNote))));

            // ----------------------------------------
            // ノート状態変更（Undo/Redo 対応）
            // ----------------------------------------
            RequestForChangeNoteStatus
                .Select(note => new { current = note, prev = EditData.Notes[note.position].note })
                .Buffer(RequestForChangeNoteStatus.ThrottleFrame(1))
                .Select(b => b.OrderBy(note =>
                    note.current.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(x => ChangeNoteStates(x.current)),
                            () => notes.ForEach(x => ChangeNoteStates(x.prev)))));

            // ----------------------------------------
            // RequestForEditNote → Add / Remove / Change に振り分け
            // ----------------------------------------
            RequestForEditNote.Subscribe(note =>
            {
                if (note.type == NoteTypes.Single)
                {
                    (EditData.Notes.ContainsKey(note.position)
                        ? RequestForRemoveNote
                        : RequestForAddNote)
                    .OnNext(note);
                }
                else if (note.type == NoteTypes.Long)
                {
                    if (!EditData.Notes.ContainsKey(note.position))
                    {
                        RequestForAddNote.OnNext(note);
                        return;
                    }

                    var noteObject = EditData.Notes[note.position];
                    (noteObject.note.type == NoteTypes.Long
                        ? RequestForRemoveNote
                        : RequestForChangeNoteStatus)
                    .OnNext(noteObject.note);
                }
            });
        }

        // ========================================
        // ノート操作（Add / Remove / Change）
        // ========================================

        public void AddNote(Note note)
        {
            if (EditData.Notes.ContainsKey(note.position))
            {
                if (!EditData.Notes[note.position].note.Equals(note))
                    RequestForChangeNoteStatus.OnNext(note);

                return;
            }

            var noteObject = new NoteObject();
            noteObject.SetState(note);
            noteObject.Init();
            EditData.Notes.Add(noteObject.note.position, noteObject);
        }

        void ChangeNoteStates(Note note)
        {
            if (!EditData.Notes.ContainsKey(note.position))
                return;

            EditData.Notes[note.position].SetState(note);
        }

        void RemoveNote(Note note)
        {
            if (!EditData.Notes.ContainsKey(note.position))
                return;

            var noteObject = EditData.Notes[note.position];
            noteObject.Dispose();
            EditData.Notes.Remove(noteObject.note.position);
        }
    }
}
