// ========================================
//
// NoteMaker Project
//
// ========================================
//
// EditNotesPresenter.cs
// ノート編集（追加・削除・状態変更・ロングノート編集開始/終了）に関する
// すべての操作を統括するプレゼンターです。
// NotesRegion のクリックイベントを受け取り、
// EditData.Notes の更新と Undo/Redo 対応コマンド発行を行います。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ノーツ編集操作を統括するクラスです。
    /// ・クリックでノーツ追加/削除/状態変更  
    /// ・Shift クリックでロングノーツ開始  
    /// ・右クリック/Esc でロングノーツ編集終了  
    /// ・Undo/Redo 対応（追加・削除・状態変更）  
    /// 
    /// EditData.Notes を直接操作せず、必ずこのクラスを経由して編集します。
    /// </summary>
    public class EditNotesPresenter : SingletonMonoBehaviour<EditNotesPresenter>
    {
        [SerializeField] CanvasEvents canvasEvents = default;
        [SerializeField] BpmInputPresenter bpmInputPresenter = default; // ユーザーが追加したBPM変更用UIの管理スクリプト

        public readonly Subject<Note> RequestForEditNote = new Subject<Note>();          // 単ノート/ロングノーツ編集要求
        public readonly Subject<Note> RequestForRemoveNote = new Subject<Note>();        // ノーツ削除要求
        public readonly Subject<Note> RequestForAddNote = new Subject<Note>();           // ノーツ追加要求
        public readonly Subject<Note> RequestForChangeNoteStatus = new Subject<Note>();  // ノーツ状態変更要求

        public readonly Subject<BpmChange> RequestForRemoveBpmChange = new Subject<BpmChange>(); // BPM削除要求
        public readonly Subject<BpmChange> RequestForAddBpmChange = new Subject<BpmChange>();    // BPM追加要求

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// ノーツ編集に関するすべてのストリームを構築します。
        /// </summary>
        void Init()
        {
            //===============================
            // NotesRegion クリック → 最も近いノーツ位置を取得
            //===============================
            var closestNoteAreaOnMouseDownObservable =
                canvasEvents.NotesRegionOnMouseDownObservable
                    .Where(_ => !KeyInput.CtrlKey())               // Ctrl 中は別操作
                    .Where(_ => !Input.GetMouseButtonDown(1))      // 右クリックは除外
                    .Where(_ => !NoteCanvas.IsMouseOverBpmLine.Value) // BPM調整ライン上のクリックは除外
                    .Where(_ => 0 <= NoteCanvas.ClosestNotePosition.Value.num);

            var bpmChangeMouseDownObservable =
                canvasEvents.NotesRegionOnMouseDownObservable
                    .Where(_ => !KeyInput.CtrlKey())
                    .Where(_ => !Input.GetMouseButtonDown(1))
                    .Where(_ => NoteCanvas.IsMouseOverBpmLine.Value)   // BPM調整ライン上
                    .Where(_ => 0 <= NoteCanvas.ClosestNotePosition.Value.num);

            //===============================
            // 単ノーツ or ロングノーツの通常クリック
            //===============================
            closestNoteAreaOnMouseDownObservable
                .Where(_ => EditState.NoteType.Value == NoteTypes.Single && !KeyInput.ShiftKey())
                .Merge(
                    closestNoteAreaOnMouseDownObservable
                        .Where(_ => EditState.NoteType.Value == NoteTypes.Long))
                .Subscribe(_ =>
                {
                    var pos = NoteCanvas.ClosestNotePosition.Value;

                    // クリック時にロングノーツのTailを編集している場合、Tailの位置を更新
                    if (EditData.Notes.ContainsKey(pos))
                    {
                        // 既存ノーツ → OnClick（削除 or 状態変更）
                        EditData.Notes[pos].OnClickObservable.OnNext(Unit.Default);
                    }
                    // ない場合、単ノーツ追加orロングノーツを開始
                    else
                    {
                        // 新規ノーツ追加
                        RequestForEditNote.OnNext(
                            new Note(
                                pos,
                                EditState.NoteType.Value,
                                NotePosition.None,
                                EditState.LongNoteTailPosition.Value));
                    }
                });

            //===============================
            // Shift + クリック → ロングノーツ開始
            //===============================
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

            //===============================
            // ロングノーツ編集終了（Esc or 右クリック）
            //===============================
            this.UpdateAsObservable()
                .Where(_ => EditState.NoteType.Value == NoteTypes.Long)
                .Where(_ => Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                .Subscribe(_ => EditState.NoteType.Value = NoteTypes.Single);

            // ロングノーツ終了時 → TailPosition リセット
            EditState.NoteType
                .Where(type => type == NoteTypes.Single)
                .Subscribe(_ => EditState.LongNoteTailPosition.Value = NotePosition.None);

            //===============================
            // ノーツ削除（Undo/Redo 対応）
            //===============================
            RequestForRemoveNote
                .Buffer(RequestForRemoveNote.ThrottleFrame(1))
                .Select(list =>
                    list.OrderBy(n => n.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value, EditData.BpmChanges)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(RemoveNote),
                            () => notes.ForEach(AddNote))));

            //===============================
            // ノーツ追加（Undo/Redo 対応）
            //===============================
            RequestForAddNote
                .Buffer(RequestForAddNote.ThrottleFrame(1))
                .Select(list =>
                    list.OrderBy(n => n.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value, EditData.BpmChanges)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(AddNote),
                            () => notes.ForEach(RemoveNote))));

            //===============================
            // BPM変更追加・削除（Undo/Redo 対応）
            //===============================
            RequestForRemoveBpmChange
                .Subscribe(b => EditCommandManager.Do(
                    new Command(() => RemoveBpmChange(b), () => AddBpmChange(b))));

            RequestForAddBpmChange
                .Subscribe(b => EditCommandManager.Do(
                    new Command(() => AddBpmChange(b), () => RemoveBpmChange(b))));

            //===============================
            // BPMイベントクリック処理
            //===============================
            bpmChangeMouseDownObservable.Subscribe(_ =>
            {
                var tick = NoteCanvas.ClosestNotePosition.Value.num;
                Debug.Log($"BPM調整ラインがクリックされました: tick={tick}");
                
                // BPM入力用ダイアログを表示
                if (bpmInputPresenter != null)
                {
                    bpmInputPresenter.Show(tick);
                }
            });

            //===============================
            // ノーツ状態変更（Undo/Redo 対応）
            //===============================
            RequestForChangeNoteStatus
                .Select(note => new { current = note, prev = EditData.Notes[note.position].note })
                .Buffer(RequestForChangeNoteStatus.ThrottleFrame(1))
                .Select(list =>
                    list.OrderBy(n => n.current.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value, EditData.BpmChanges)).ToList())
                .Subscribe(notes =>
                    EditCommandManager.Do(
                        new Command(
                            () => notes.ForEach(x => ChangeNoteStates(x.current)),
                            () => notes.ForEach(x => ChangeNoteStates(x.prev)))));

            //===============================
            // RequestForEditNote → Add/Remove/Change の振り分け
            //===============================
            RequestForEditNote.Subscribe(note =>
            {
                var pos = note.position;

                if (note.type == NoteTypes.Single)
                {
                    // 単ノーツ：存在すれば削除、無ければ追加
                    (EditData.Notes.ContainsKey(pos)
                        ? RequestForRemoveNote
                        : RequestForAddNote)
                    .OnNext(note);
                }
                else if (note.type == NoteTypes.Long)
                {
                    // ロングノーツ：存在しなければ追加、存在すれば状態変更
                    if (!EditData.Notes.ContainsKey(pos))
                    {
                        RequestForAddNote.OnNext(note);
                        return;
                    }

                    var noteObj = EditData.Notes[pos];
                    (noteObj.note.type == NoteTypes.Long
                        ? RequestForRemoveNote
                        : RequestForChangeNoteStatus)
                    .OnNext(noteObj.note);
                }
            });
        }

        //===============================
        // ノーツ追加
        //===============================
        public void AddNote(Note note)
        {
            if (EditData.Notes.ContainsKey(note.position))
            {
                // 既存ノーツと異なる状態なら状態変更
                if (!EditData.Notes[note.position].note.Equals(note))
                    RequestForChangeNoteStatus.OnNext(note);

                return;
            }

            var noteObject = new NoteObject();
            noteObject.SetState(note);
            noteObject.Init();
            EditData.Notes.Add(noteObject.note.position, noteObject);
        }

        //===============================
        // ノーツ状態変更
        //===============================
        void ChangeNoteStates(Note note)
        {
            if (!EditData.Notes.ContainsKey(note.position))
                return;

            EditData.Notes[note.position].SetState(note);
        }

        //===============================
        // ノーツ削除
        //===============================
        void RemoveNote(Note note)
        {
            if (!EditData.Notes.ContainsKey(note.position))
                return;

            var noteObject = EditData.Notes[note.position];
            noteObject.Dispose();
            EditData.Notes.Remove(noteObject.note.position);
        }

        //===============================
        // BPM変更の追加・削除
        //===============================
        public void AddBpmChange(BpmChange b)
        {
            if (!EditData.BpmChanges.Any(x => x.tick == b.tick))
            {
                EditData.BpmChanges.Add(b);
            }
        }

        public void RemoveBpmChange(BpmChange b)
        {
            var target = EditData.BpmChanges.FirstOrDefault(x => x.tick == b.tick);
            if (target != null)
            {
                EditData.BpmChanges.Remove(target);
            }
        }
    }
}