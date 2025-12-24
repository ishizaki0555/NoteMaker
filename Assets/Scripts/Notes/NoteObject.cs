// ========================================
//
// NoteObject.cs
//
// ========================================
//
// ノート 1 個分の状態・色・選択状態・クリック処理・ロングノート接続処理などを管理するクラス。
// ReactiveProperty と UniRx を用いて、描画更新やクリックイベントを通知する。
//
// ========================================

using NoteMaker.GLDrawing;
using NoteMaker.Model;
using NoteMaker.Presenter;
using NoteMaker.Utility;
using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Notes
{
    public class NoteObject : IDisposable
    {
        public Note note = new Note();                                              // ノート本体データ
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>();    // 選択状態
        public Subject<Unit> LateUpdateObservable = new Subject<Unit>();            // LateUpdate 通知
        public Subject<Unit> OnClickObservable = new Subject<Unit>();               // クリック通知

        public Color NoteColor { get { return noteColor_.Value; } } // 現在のノート色
        ReactiveProperty<Color> noteColor_ = new ReactiveProperty<Color>();

        // ノートの状態に応じた色
        Color selectedStateColor = new Color(255 / 255f, 0 / 255f, 255 / 255f);
        Color singleNoteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);
        Color longNoteColor = new Color(0 / 255f, 255 / 255f, 255 / 255f);
        Color invalidStateColor = new Color(255 / 255f, 0 / 255f, 0 / 255f);

        ReactiveProperty<NoteTypes> noteType = new ReactiveProperty<NoteTypes>();
        CompositeDisposable disposable = new CompositeDisposable();

        /// <summary>
        /// ノートの状態・色・クリック処理・ロングノート描画処理などを初期化する。
        /// </summary>
        public void Init()
        {
            // Dispose 時に破棄するものをまとめる
            disposable = new CompositeDisposable(
                isSelected,
                LateUpdateObservable,
                OnClickObservable,
                noteColor_,
                noteType);

            var editPresenter = EditNotesPresenter.Instance;

            // ノート種別を監視
            noteType = this.ObserveEveryValueChanged(_ => note.type).ToReactiveProperty();

            // ノート種別 or 選択状態に応じて色を更新
            disposable.Add(
                noteType.Where(_ => !isSelected.Value)
                    .Merge(isSelected.Select(_ => noteType.Value))
                    .Select(type => type == NoteTypes.Long)
                    .Subscribe(isLongNote => noteColor_.Value = isLongNote ? longNoteColor : singleNoteColor)
            );

            // 選択されたら選択色に変更
            disposable.Add(
                isSelected.Where(selected => selected)
                    .Subscribe(_ => noteColor_.Value = selectedStateColor)
            );

            // クリックされたときの処理
            var mouseDownObservable = OnClickObservable
                .Select(_ => EditState.NoteType.Value)
                .Where(_ => NoteCanvas.ClosestNotePosition.Value.Equals(note.position));

            // 単ノート削除
            disposable.Add(
                mouseDownObservable.Where(editType => editType == NoteTypes.Single)
                    .Where(editType => editType == noteType.Value)
                    .Subscribe(_ => editPresenter.RequestForRemoveNote.OnNext(note))
            );

            // ロングノート編集
            disposable.Add(
                mouseDownObservable.Where(editType => editType == NoteTypes.Long)
                    .Where(editType => editType == noteType.Value)
                    .Subscribe(_ =>
                    {
                        // ロングノートの接続を作る場合
                        if (EditData.Notes.ContainsKey(EditState.LongNoteTailPosition.Value) &&
                            note.prev.Equals(NotePosition.None))
                        {
                            var currentTailNote = new Note(EditData.Notes[EditState.LongNoteTailPosition.Value].note);
                            currentTailNote.next = note.position;
                            editPresenter.RequestForChangeNoteStatus.OnNext(currentTailNote);

                            var selfNote = new Note(note);
                            selfNote.prev = currentTailNote.position;
                            editPresenter.RequestForChangeNoteStatus.OnNext(selfNote);
                        }
                        else
                        {
                            // ロングノートの接続を切る場合
                            if (EditData.Notes.ContainsKey(note.prev) && !EditData.Notes.ContainsKey(note.next))
                                EditState.LongNoteTailPosition.Value = note.prev;

                            editPresenter.RequestForRemoveNote.OnNext(
                                new Note(note.position, EditState.NoteType.Value, note.next, note.prev));

                            RemoveLink();
                        }
                    })
            );

            // ロングノートの線描画
            var longNoteUpdateObservable = LateUpdateObservable
                .Where(_ => noteType.Value == NoteTypes.Long);

            disposable.Add(
                longNoteUpdateObservable
                    .Where(_ => EditData.Notes.ContainsKey(note.next))
                    .Select(_ => ConvertUtils.NoteToCanvasPosition(note.next))
                    .Merge(
                        longNoteUpdateObservable
                            .Where(_ => EditState.NoteType.Value == NoteTypes.Long)
                            .Where(_ => EditState.LongNoteTailPosition.Value.Equals(note.position))
                            .Select(_ => ConvertUtils.ScreenToCanvasPosition(Input.mousePosition))
                    )
                    .Select(nextPosition => new Line(
                        ConvertUtils.CanvasToScreenPosition(ConvertUtils.NoteToCanvasPosition(note.position)),
                        ConvertUtils.CanvasToScreenPosition(nextPosition),
                        isSelected.Value ||
                        (EditData.Notes.ContainsKey(note.next) && EditData.Notes[note.next].isSelected.Value)
                            ? selectedStateColor
                            : 0 < nextPosition.x - ConvertUtils.NoteToCanvasPosition(note.position).x
                                ? longNoteColor
                                : invalidStateColor))
                    .Subscribe(line => GLLineDrawer.Draw(line))
            );
        }

        /// <summary>
        /// ロングノートのリンクを削除する。
        /// </summary>
        void RemoveLink()
        {
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = note.next;

            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = note.prev;
        }

        /// <summary>
        /// ロングノートのリンクを挿入する。
        /// </summary>
        void InsertLink(NotePosition position)
        {
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = position;

            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = position;
        }

        /// <summary>
        /// ノートの状態を更新し、ロングノートの接続も調整する。
        /// </summary>
        public void SetState(Note note)
        {
            // 単ノートになったらリンクを削除
            if (note.type == NoteTypes.Single)
            {
                RemoveLink();
            }

            this.note = note;

            // ロングノートならリンクを挿入
            if (note.type == NoteTypes.Long)
            {
                InsertLink(note.position);

                // ロングノート末尾の更新
                EditState.LongNoteTailPosition.Value =
                    EditState.LongNoteTailPosition.Value.Equals(note.prev)
                        ? note.position
                        : NotePosition.None;
            }
        }

        /// <summary>
        /// 登録した Observable / ReactiveProperty を破棄する。
        /// </summary>
        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
