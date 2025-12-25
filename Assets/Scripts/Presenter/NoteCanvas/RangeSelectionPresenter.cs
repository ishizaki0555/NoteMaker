// ========================================
//
// RangeSelectionPresenter.cs
//
// ========================================
//
// ノートの範囲選択・コピー・カット・ペースト・削除を管理する Presenter。
// ・ドラッグによる矩形選択
// ・Ctrl+A 全選択
// ・Ctrl+C コピー
// ・Ctrl+X カット
// ・Ctrl+V ペースト
// ・Delete / Backspace 削除
// ・クリックによる選択解除
//
// GLLineDrawer を用いて選択矩形を描画する。
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Notes;
using NoteMaker.Utility;
using NoteMaker.GLDrawing;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class RangeSelectionPresenter : MonoBehaviour
    {
        [SerializeField] Color selectionRectColor = default; // 選択矩形の色

        Dictionary<NotePosition, NoteObject> selectedNoteObjects = new Dictionary<NotePosition, NoteObject>(); // 選択中ノート
        List<Note> copiedNotes = new List<Note>(); // コピー済みノート
        EditNotesPresenter editPresenter;

        /// <summary>
        /// コンポーネント生成直後に呼ばれる初期化処理。
        /// ノート編集 Presenter の参照取得と、選択系イベントのセットアップを行う。
        /// </summary>
        void Awake()
        {
            editPresenter = EditNotesPresenter.Instance;

            // ----------------------------------------
            // ドラッグによる範囲選択
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlKey())
                .Where(_ => Input.GetMouseButtonDown(0))
                .Select(_ => Input.mousePosition)
                .SelectMany(startPos =>
                    this.UpdateAsObservable()
                        .TakeWhile(_ => !Input.GetMouseButtonUp(0))
                        .Where(_ => NoteCanvas.IsMouseOverNotesRegion.Value)
                        .Select(_ => Input.mousePosition)
                        .Select(currentPos => new Rect(startPos, currentPos - startPos)))
                .Do(rect => GLLineDrawer.Draw(ToLines(rect, selectionRectColor)))
                .Do(_ => { if (!Audio.IsPlaying.Value) Deselect(); })
                .SelectMany(rect => GetNotesWithin(rect))
                .Do(kv => selectedNoteObjects[kv.Key] = kv.Value)
                .Subscribe(kv => kv.Value.isSelected.Value = true);

            // ----------------------------------------
            // Ctrl+A 全選択
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.A))
                .SelectMany(_ => EditData.Notes.Values.ToList())
                .Do(noteObj => noteObj.isSelected.Value = true)
                .Subscribe(noteObj => selectedNoteObjects[noteObj.note.position] = noteObj);

            // ----------------------------------------
            // Ctrl+C コピー
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.C))
                .Subscribe(_ => CopyNotes(selectedNoteObjects.Values));

            // ----------------------------------------
            // Ctrl+X カット（コピー → 削除）
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.X))
                .Select(_ => selectedNoteObjects.Values
                    .Where(noteObj => EditData.Notes.ContainsKey(noteObj.note.position)))
                .Do(notes => CopyNotes(notes))
                .Subscribe(notes => DeleteNotes(notes));

            // ----------------------------------------
            // 左クリックで選択解除（波形領域以外）
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => !NoteCanvas.IsMouseOverWaveformRegion.Value)
                .Where(_ => Input.GetMouseButtonDown(0))
                .Subscribe(_ => Deselect());

            // ----------------------------------------
            // Delete / Backspace で削除
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                .Select(_ => selectedNoteObjects.Values
                    .Where(noteObj => EditData.Notes.ContainsKey(noteObj.note.position)).ToList())
                .Do(_ => selectedNoteObjects.Clear())
                .Subscribe(notes => DeleteNotes(notes));

            // ----------------------------------------
            // Ctrl+V ペースト（次のビートへ）
            // ----------------------------------------
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.V))
                .Where(_ => copiedNotes.Count > 0)
                .Select(_ => copiedNotes.OrderBy(note =>
                    note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)))
                .Subscribe(sortedCopiedNotes =>
                {
                    var firstPos = sortedCopiedNotes.First().position;
                    var lastPos = sortedCopiedNotes.Last().position;
                    var beatDiff = 1 + lastPos.num / lastPos.LPB - firstPos.num / firstPos.LPB;

                    // 貼り付け後に音源範囲外へ出ないノートのみ採用
                    var validNotes = copiedNotes
                        .Where(note =>
                            note.position.Add(0, note.position.LPB * beatDiff, 0)
                                .ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)
                            < Audio.Source.clip.samples)
                        .ToList();

                    copiedNotes.Clear();

                    // ノート生成 & 追加
                    validNotes.ToObservable()
                        .Select(note =>
                            note.type == NoteTypes.Single
                                ? new Note(note.position.Add(0, note.position.LPB * beatDiff, 0))
                                : new Note(
                                    note.position.Add(0, note.position.LPB * beatDiff, 0),
                                    note.type,
                                    note.next.Add(0, note.next.LPB * beatDiff, 0),
                                    note.prev.Add(0, note.prev.LPB * beatDiff, 0)))
                        .Do(note => copiedNotes.Add(note))
                        .Subscribe(note =>
                            (EditData.Notes.ContainsKey(note.position)
                                ? editPresenter.RequestForChangeNoteStatus
                                : editPresenter.RequestForAddNote)
                            .OnNext(note));

                    // 選択解除
                    Deselect();

                    // 貼り付けたノートを選択状態にする
                    validNotes.Select(obj => obj.position.Add(0, obj.position.LPB * beatDiff, 0))
                        .ToObservable()
                        .DelayFrame(1)
                        .Select(pastedPosition => EditData.Notes[pastedPosition])
                        .Do(pastedObj => selectedNoteObjects[pastedObj.note.position] = pastedObj)
                        .Subscribe(pastedObj => pastedObj.isSelected.Value = true);
                });
        }

        /// <summary>
        /// ロングノートの次の選択ノートを取得する。
        /// </summary>
        public NotePosition GetSelectedNextLongNote(NotePosition current, Func<NoteObject, NotePosition> accessor)
        {
            while (EditData.Notes.ContainsKey(current))
            {
                if (selectedNoteObjects.ContainsKey(current))
                    return current;

                current = accessor(EditData.Notes[current]);
            }

            return NotePosition.None;
        }

        /// <summary>
        /// 指定矩形内にあるノートを取得する。
        /// </summary>
        Dictionary<NotePosition, NoteObject> GetNotesWithin(Rect rect)
        {
            return EditData.Notes
                .Where(kv =>
                    rect.Contains(
                        ConvertUtils.CanvasToScreenPosition(
                            ConvertUtils.NoteToCanvasPosition(kv.Value.note.position)),
                        true))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// ノートをコピーする。
        /// ロングノートの場合は next / prev の接続も考慮する。
        /// </summary>
        void CopyNotes(IEnumerable<NoteObject> notes)
        {
            copiedNotes = notes.Select(noteObj =>
            {
                var note = noteObj.note;
                if (noteObj.note.type == NoteTypes.Long)
                {
                    note.next = GetSelectedNextLongNote(noteObj.note.next, c => c.note.next);
                    note.prev = GetSelectedNextLongNote(noteObj.note.prev, c => c.note.prev);
                }
                return note;
            })
            .ToList();
        }

        /// <summary>
        /// ノートを削除する。
        /// </summary>
        void DeleteNotes(IEnumerable<NoteObject> notes)
        {
            notes.ToList().ForEach(note =>
                editPresenter.RequestForRemoveNote.OnNext(note.note));
        }

        /// <summary>
        /// 選択状態をすべて解除する。
        /// </summary>
        void Deselect()
        {
            selectedNoteObjects.Values
                .Where(noteObj => EditData.Notes.ContainsKey(noteObj.note.position))
                .ToList()
                .ForEach(note => note.isSelected.Value = false);

            selectedNoteObjects.Clear();
        }

        /// <summary>
        /// Rect を GLLineDrawer 用の Line 配列に変換する。
        /// </summary>
        Line[] ToLines(Rect rect, Color color)
        {
            return new[]
            {
                new Line(rect.min, rect.min + Vector2.right * rect.size.x, color),
                new Line(rect.min, rect.min + Vector2.up    * rect.size.y, color),
                new Line(rect.max, rect.max + Vector2.left  * rect.size.x, color),
                new Line(rect.max, rect.max + Vector2.down  * rect.size.y, color)
            };
        }
    }
}
