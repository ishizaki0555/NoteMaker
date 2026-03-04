// ========================================
//
// NoteMaker Project
//
// ========================================
//
// RangeSelectionPresenter.cs
// ノーツの範囲選択・コピー・カット・ペースト・削除・選択解除をまとめて扱う
// プレゼンターです。ドラッグ矩形での選択、Ctrl+A 全選択、Ctrl+C コピー、
// Ctrl+X カット、Ctrl+V ペースト、Delete 削除など、
// ノーツ編集の複合操作を一括で管理します。
//
//========================================

using NoteMaker.GLDrawing;
using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ノーツの範囲選択・コピー・カット・ペースト・削除を管理するクラスです。
    /// ・ドラッグ矩形での範囲選択  
    /// ・Ctrl+A 全選択  
    /// ・Ctrl+C コピー  
    /// ・Ctrl+X カット  
    /// ・Ctrl+V ペースト（次の拍へ）  
    /// ・Delete / Backspace で削除  
    /// ・選択解除  
    /// </summary>
    public class RangeSelectionPresenter : MonoBehaviour
    {
        [SerializeField] Color selectionRectColor = default; // 選択矩形の色

        Dictionary<NotePosition, NoteObject> selectedNoteObjects = new Dictionary<NotePosition, NoteObject>(); // 選択中ノーツ
        List<Note> copiedNotes = new List<Note>(); // コピーされたノーツ
        EditNotesPresenter editPresenter;

        void Awake()
        {
            editPresenter = EditNotesPresenter.Instance;

            //===============================
            // ドラッグによる範囲選択
            //===============================
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

            //===============================
            // Ctrl+A 全選択
            //===============================
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.A))
                .SelectMany(_ => EditData.Notes.Values.ToList())
                .Do(obj => obj.isSelected.Value = true)
                .Subscribe(obj => selectedNoteObjects[obj.note.position] = obj);

            //===============================
            // Ctrl+C コピー
            //===============================
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.C))
                .Subscribe(_ => CopyNotes(selectedNoteObjects.Values));

            //===============================
            // Ctrl+X カット（コピー → 削除）
            //===============================
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.X))
                .Select(_ => selectedNoteObjects.Values
                    .Where(obj => EditData.Notes.ContainsKey(obj.note.position)))
                .Do(notes => CopyNotes(notes))
                .Subscribe(notes => DeleteNotes(notes));

            //===============================
            // 左クリックで選択解除（Waveform 以外）
            //===============================
            this.UpdateAsObservable()
                .Where(_ => !NoteCanvas.IsMouseOverWaveformRegion.Value)
                .Where(_ => Input.GetMouseButtonDown(0))
                .Subscribe(_ => Deselect());

            //===============================
            // Delete / Backspace で削除
            //===============================
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                .Select(_ => selectedNoteObjects.Values
                    .Where(obj => EditData.Notes.ContainsKey(obj.note.position)).ToList())
                .Do(_ => selectedNoteObjects.Clear())
                .Subscribe(notes => DeleteNotes(notes));

            //===============================
            // Ctrl+V ペースト（次の拍へ）
            //===============================
            this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.V))
                .Where(_ => copiedNotes.Count > 0)
                .Select(_ => copiedNotes.OrderBy(n => n.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)))
                .Subscribe(sortedCopiedNotes =>
                {
                    var first = sortedCopiedNotes.First().position;
                    var last = sortedCopiedNotes.Last().position;

                    // 次の拍へ移動するための差分
                    var beatDiff = 1 + last.num / last.LPB - first.num / first.LPB;

                    // 曲の長さを超えないノーツだけペースト対象にする
                    var validNotes = copiedNotes
                        .Where(note =>
                            note.position.Add(0, note.position.LPB * beatDiff, 0)
                                .ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)
                            < Audio.Source.clip.samples)
                        .ToList();

                    copiedNotes.Clear();

                    // ノーツを複製してペースト
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

                    Deselect();

                    // ペーストされたノーツを選択状態にする
                    validNotes.Select(n => n.position.Add(0, n.position.LPB * beatDiff, 0))
                        .ToObservable()
                        .DelayFrame(1)
                        .Select(pos => EditData.Notes[pos])
                        .Do(obj => selectedNoteObjects[obj.note.position] = obj)
                        .Subscribe(obj => obj.isSelected.Value = true);
                });
        }

        /// <summary>
        /// ロングノーツの next/prev を辿りながら、選択されている次のノーツを探す。
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
        /// 範囲内にあるノーツを取得。
        /// </summary>
        Dictionary<NotePosition, NoteObject> GetNotesWithin(Rect rect)
        {
            return EditData.Notes
                .Where(kv =>
                    rect.Contains(
                        ConvertUtils.CanvasToScreenPosition(
                            ConvertUtils.NoteToCanvasPosition(kv.Value.note.position)), true))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// ノーツをコピー。
        /// ロングノーツの場合は next/prev も選択範囲内で繋ぎ直す。
        /// </summary>
        void CopyNotes(IEnumerable<NoteObject> notes)
        {
            copiedNotes = notes.Select(obj =>
            {
                var note = obj.note;

                if (note.type == NoteTypes.Long)
                {
                    note.next = GetSelectedNextLongNote(note.next, c => c.note.next);
                    note.prev = GetSelectedNextLongNote(note.prev, c => c.note.prev);
                }

                return note;
            }).ToList();
        }

        /// <summary>
        /// ノーツ削除。
        /// </summary>
        void DeleteNotes(IEnumerable<NoteObject> notes)
        {
            notes.ToList().ForEach(obj =>
                editPresenter.RequestForRemoveNote.OnNext(obj.note));
        }

        /// <summary>
        /// 選択解除。
        /// </summary>
        void Deselect()
        {
            selectedNoteObjects.Values
                .Where(obj => EditData.Notes.ContainsKey(obj.note.position))
                .ToList()
                .ForEach(obj => obj.isSelected.Value = false);

            selectedNoteObjects.Clear();
        }

        /// <summary>
        /// Rect → 4 辺の Line 配列へ変換。
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
