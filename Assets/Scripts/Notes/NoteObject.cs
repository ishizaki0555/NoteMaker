// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// NoteObject.cs
// ノート 1 個分の振る舞い（選択状態・色変化・クリック処理・Long ノーツ連結処理・
// ロングノーツ線描画など）を担当するクラスです。
// EditData に保持される Note のラッパーとして動作し、
// Presenter と連携してノートの追加・削除・状態変更を行います。
// 
//========================================

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
    /// <summary>
    /// ノート 1 個分の振る舞いを管理するクラスです。
    /// ・選択状態の管理  
    /// ・クリック処理（削除 / Long ノーツ編集）  
    /// ・色の更新（Single / Long / 選択中 / 無効状態）  
    /// ・Long ノーツの prev / next 連結処理  
    /// ・ロングノーツ線の描画  
    /// などを担当します。
    /// </summary>
    public class NoteObject : IDisposable
    {
        public Note note = new Note();                               // ノート本体データ
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>(); // 選択状態
        public Subject<Unit> LateUpdateObservable = new Subject<Unit>();         // 毎フレームの更新イベント
        public Subject<Unit> OnClickObservable = new Subject<Unit>();            // クリックイベント

        public Color NoteColor => noteColor_.Value;                  // 現在のノート色
        ReactiveProperty<Color> noteColor_ = new ReactiveProperty<Color>();

        // 色設定
        Color selectedStateColor = new Color(255 / 255f, 0 / 255f, 255 / 255f); // 選択状態の色
        Color singleNoteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);   // 単ノーツの色
        Color longNoteColor = new Color(0 / 255f, 255 / 255f, 255 / 255f);      // ロングノーツの色
        Color invalidStateColor = new Color(255 / 255f, 0 / 255f, 0 / 255f);    // 無効状態の色

        ReactiveProperty<NoteTypes> noteType = new ReactiveProperty<NoteTypes>();
        CompositeDisposable disposable = new CompositeDisposable();

        /// <summary>
        /// NoteObject の初期化処理。
        /// 色更新、クリック処理、Long ノーツ線描画などの購読をセットアップします。
        /// </summary>
        public void Init()
        {
            disposable = new CompositeDisposable(
                isSelected,
                LateUpdateObservable,
                OnClickObservable,
                noteColor_,
                noteType
            );

            var editPresenter = EditNotesPresenter.Instance;

            // ノートタイプの変化を監視
            noteType = this.ObserveEveryValueChanged(_ => note.type).ToReactiveProperty();

            // 色の更新（選択状態とノートタイプに応じて変化）
            disposable.Add(
                noteType.Where(_ => !isSelected.Value)
                    .Merge(isSelected.Select(_ => noteType.Value))
                    .Select(type => type == NoteTypes.Long)
                    .Subscribe(isLong => noteColor_.Value = isLong ? longNoteColor : singleNoteColor)
            );

            // 選択時の色
            disposable.Add(
                isSelected.Where(selected => selected)
                    .Subscribe(_ => noteColor_.Value = selectedStateColor)
            );

            // クリック処理（ノートタイプに応じて削除 or Long ノーツ編集）
            var mouseDownObservable = OnClickObservable
                .Select(_ => EditState.NoteType.Value)
                .Where(_ => NoteCanvas.ClosestNotePosition.Value.Equals(note.position));

            // 単ノーツ削除
            disposable.Add(
                mouseDownObservable.Where(editType => editType == NoteTypes.Single)
                    .Where(editType => editType == noteType.Value)
                    .Subscribe(_ => editPresenter.RequestForRemoveNote.OnNext(note))
            );

            // ロングノーツ編集（連結 or 削除）
            disposable.Add(
                mouseDownObservable.Where(editType => editType == NoteTypes.Long)
                    .Where(editType => editType == noteType.Value)
                    .Subscribe(_ =>
                    {
                        // 連結処理
                        if (EditData.Notes.ContainsKey(EditState.LongNoteTailPosition.Value)
                            && note.prev.Equals(NotePosition.None))
                        {
                            var currentTail = new Note(EditData.Notes[EditState.LongNoteTailPosition.Value].note);
                            currentTail.next = note.position;
                            editPresenter.RequestForChangeNoteStatus.OnNext(currentTail);

                            var selfNote = new Note(note);
                            selfNote.prev = currentTail.position;
                            editPresenter.RequestForChangeNoteStatus.OnNext(selfNote);
                        }
                        else
                        {
                            // 削除処理（prev / next の連結解除）
                            if (EditData.Notes.ContainsKey(note.prev) && !EditData.Notes.ContainsKey(note.next))
                                EditState.LongNoteTailPosition.Value = note.prev;

                            editPresenter.RequestForRemoveNote.OnNext(
                                new Note(note.position, EditState.NoteType.Value, note.next, note.prev)
                            );
                            RemoveLink();
                        }
                    })
            );

            // ロングノーツ線描画（縦向き対応）
            var longNoteUpdateObservable = LateUpdateObservable
                .Where(_ => noteType.Value == NoteTypes.Long);

            // 次ノートの位置、または現在のノートがロングノーツの末尾でマウス位置が変化した場合に線を更新
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
                    .Select(nextPos => new Line(
                        ConvertUtils.CanvasToScreenPosition(ConvertUtils.NoteToCanvasPosition(note.position)),
                        ConvertUtils.CanvasToScreenPosition(nextPos),

                        // 縦向き：次ノートが下方向にあるかどうかで色を変える
                        isSelected.Value ||
                        (EditData.Notes.ContainsKey(note.next) && EditData.Notes[note.next].isSelected.Value)
                            ? selectedStateColor
                            : 0 < nextPos.y - ConvertUtils.NoteToCanvasPosition(note.position).y
                                ? longNoteColor
                                : invalidStateColor
                    ))
                    .Subscribe(line => GLLineDrawer.Draw(line))
            );
        }

        /// <summary>
        /// Long ノーツの prev / next の連結を解除します。
        /// </summary>
        void RemoveLink()
        {
            // 現在のprev/nextをそれぞれnext/prevに差し替える
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = note.next;

            // 現在のprev/nextをそれぞれnext/prevに差し替える
            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = note.prev;
        }

        /// <summary>
        /// Long ノーツの prev / next を指定位置に差し替えます。
        /// </summary>
        void InsertLink(NotePosition position)
        {
            // 現在のprev/nextをpositionに差し替える
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = position;

            // positionのprev/nextを現在のprev/nextに差し替える
            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = position;
        }

        /// <summary>
        /// ノートの状態を更新し、必要に応じて Long ノーツの連結を再構築します。
        /// </summary>
        public void SetState(Note note)
        {
            // 単ノーツからロングノーツ、またはロングノーツから単ノーツに変化する場合は連結の構築/解除を行う
            if (note.type == NoteTypes.Single)
                RemoveLink();

            this.note = note;

            // ロングノーツに変化した場合は連結を構築する
            if (note.type == NoteTypes.Long)
            {
                InsertLink(note.position);

                EditState.LongNoteTailPosition.Value =
                    EditState.LongNoteTailPosition.Value.Equals(note.prev)
                        ? note.position
                        : NotePosition.None;
            }
        }

        /// <summary>
        /// 登録した購読を破棄します。
        /// </summary>
        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
