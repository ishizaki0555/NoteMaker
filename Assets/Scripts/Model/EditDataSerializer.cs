// ========================================
//
// EditDataSerializer.cs
//
// ========================================
//
// EditData と MusicDTO の相互変換（保存・読み込み）を行うクラス
//
// ========================================

using NoteMaker.DTO;
using NoteMaker.Notes;
using NoteMaker.Presenter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoteMaker.Model
{
    public class EditDataSerializer
    {
        /// <summary>
        /// 現在の EditData を MusicDTO.EditData に変換し、JSON 文字列として返す。
        /// </summary>
        public static string Serialize()
        {
            var dto = new MusicDTO.EditData();
            dto.BPM = EditData.BPM.Value;
            dto.maxNum = EditData.MaxBlock.Value;
            dto.offset = EditData.OffsetSamples.Value;
            dto.songName = Path.GetFileNameWithoutExtension(EditData.Name.Value);

            // ロングノートの先頭だけを抽出し、時間順に並べる
            var sortedNoteObjects = EditData.Notes.Values
                .Where(note => !(note.note.type == NoteTypes.Long && EditData.Notes.ContainsKey(note.note.prev)))
                .OrderBy(note => note.note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value));

            dto.notes = new List<MusicDTO.Note>();

            // すべてのノートを DTO に変換して追加する
            foreach (var noteObjct in sortedNoteObjects)
            {
                // 単ノートの場合はそのまま追加
                if (noteObjct.note.type == NoteTypes.Single)
                {
                    dto.notes.Add(ToDTO(noteObjct));
                }
                // ロングノートの場合はチェーンを辿ってまとめる
                else if (noteObjct.note.type == NoteTypes.Long)
                {
                    var current = noteObjct;
                    var note = ToDTO(noteObjct);

                    // ロングノートの次のノートを順に辿って追加する
                    while (EditData.Notes.ContainsKey(current.note.next))
                    {
                        var nextObj = EditData.Notes[current.note.next];
                        note.notes.Add(ToDTO(nextObj));
                        current = nextObj;
                    }

                    dto.notes.Add(note);
                }
            }

            return UnityEngine.JsonUtility.ToJson(dto, true);
        }

        /// <summary>
        /// JSON 文字列を MusicDTO.EditData として読み込み、EditData に反映する。
        /// </summary>
        public static void Deserialize(string json)
        {
            var editData = UnityEngine.JsonUtility.FromJson<MusicDTO.EditData>(json);
            var notePresenter = EditNotesPresenter.Instance;

            EditData.BPM.Value = editData.BPM;
            EditData.MaxBlock.Value = editData.maxNum;
            EditData.OffsetSamples.Value = editData.offset;

            // すべてのノートデータを復元する
            foreach (var note in editData.notes)
            {
                // 単ノートの場合はそのまま追加
                if (note.type == 1)
                {
                    notePresenter.AddNote(ToNotesobject(note));
                    continue;
                }

                // ロングノートの場合はチェーンをまとめて追加する
                var longNoteObjects = new[] { note }.Concat(note.notes)
                    .Select(note_ =>
                    {
                        notePresenter.AddNote(ToNotesobject(note_));
                        return EditData.Notes[ToNotesobject(note_).position];
                    }).ToList();

                // ロングノートの prev / next を繋ぎ直す
                for (int i = 0; i < longNoteObjects.Count; i++)
                {
                    longNoteObjects[i].note.prev = longNoteObjects[i - 1].note.position;
                    longNoteObjects[i - 1].note.next = longNoteObjects[i].note.position;
                }

                EditState.LongNoteTailPosition.Value = NotePosition.None;
            }
        }

        /// <summary>
        /// NoteObject を MusicDTO.Note に変換する。
        /// </summary>
        static MusicDTO.Note ToDTO(NoteObject noteObject)
        {
            var note = new MusicDTO.Note();
            note.num = noteObject.note.position.num;
            note.block = noteObject.note.position.block;
            note.LPB = noteObject.note.position.LPB;
            note.type = noteObject.note.type == NoteTypes.Long ? 2 : 1;
            note.notes = new List<MusicDTO.Note>();
            return note;
        }

        /// <summary>
        /// MusicDTO.Note を Note に変換する。
        /// </summary>
        public static Note ToNotesobject(MusicDTO.Note musicNote)
        {
            return new Note(
                new NotePosition(musicNote.LPB, musicNote.num, musicNote.block),
                musicNote.type == 1 ? NoteTypes.Single : NoteTypes.Long);
        }
    }
}
