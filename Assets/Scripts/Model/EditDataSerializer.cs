// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// EditDataSerializer.cs
// EditData（エディタ内部データ）と MusicDTO（保存用データ）の相互変換を行います。
// ノート配置・Long ノーツの連結情報・楽曲設定（BPM / LPB / Offset など）を
// JSON 形式で保存・読み込みするためのシリアライザです。
// 
//========================================

using NoteMaker.DTO;
using NoteMaker.Notes;
using NoteMaker.Presenter;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoteMaker.Model
{
    /// <summary>
    /// EditData と MusicDTO の相互変換を行うシリアライザです。
    /// ・Serialize()   : EditData → JSON（保存用）  
    /// ・Deserialize() : JSON → EditData（読み込み）  
    /// ノートの並び替え、Long ノーツの連結処理などもここで行います。
    /// </summary>
    public class EditDataSerializer
    {
        /// <summary>
        /// 現在の EditData を MusicDTO.EditData に変換し、JSON 文字列として返します。
        /// ノートはサンプル位置順に並び替え、Long ノーツは子ノーツを連結して保存します。
        /// </summary>
        public static string Serialize()
        {
            var dto = new MusicDTO.EditData();
            dto.BPM = EditData.BPM.Value;
            dto.maxBlock = EditData.MaxBlock.Value;
            dto.offset = EditData.OffsetSamples.Value;
            dto.name = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            dto.maxLPB = EditData.Notes.Count > 0 ? EditData.Notes.Values.Max(n => n.note.position.LPB) : EditData.LPB.Value;
            
            dto.bpmChanges = new List<MusicDTO.BpmChangeDTO>();
            foreach (var b in EditData.BpmChanges.OrderBy(x => x.tick))
            {
                dto.bpmChanges.Add(new MusicDTO.BpmChangeDTO { tick = b.tick, bpm = b.bpm });
            }

            // Long ノーツの子ノーツは prev を持つものを除外して並び替え
            var sortedNoteObjects = EditData.Notes.Values
                .Where(note => !(note.note.type == NoteTypes.Long && EditData.Notes.ContainsKey(note.note.prev)))
                .OrderBy(note => note.note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value, EditData.BpmChanges));

            dto.notes = new List<MusicDTO.Note>();

            foreach (var noteObject in sortedNoteObjects)
            {
                if (noteObject.note.type == NoteTypes.Single)
                {
                    // 単ノーツはそのまま DTO 化
                    dto.notes.Add(ToDTO(noteObject));
                }
                else if (noteObject.note.type == NoteTypes.Long)
                {
                    // Long ノーツは prev → next の順に子ノーツを連結
                    var current = noteObject;
                    var note = ToDTO(noteObject);

                    while (EditData.Notes.ContainsKey(current.note.next))
                    {
                        var nextObj = EditData.Notes[current.note.next];
                        note.notes.Add(ToDTO(nextObj));
                        current = nextObj;
                    }

                    dto.notes.Add(note);
                }
            }

            return UnityEngine.JsonUtility.ToJson(dto);
        }

        /// <summary>
        /// JSON 文字列を MusicDTO.EditData に復元し、EditData に反映します。
        /// Long ノーツは連結情報（prev / next）を復元して構築します。
        /// </summary>
        public static void Deserialize(string json)
        {
            var editData = UnityEngine.JsonUtility.FromJson<MusicDTO.EditData>(json);
            var notePresenter = EditNotesPresenter.Instance;

            // 楽曲設定を反映
            EditData.BPM.Value = editData.BPM;
            EditData.MaxBlock.Value = editData.maxBlock;
            EditData.OffsetSamples.Value = editData.offset;

            EditData.BpmChanges.Clear();
            if (editData.bpmChanges != null)
            {
                foreach (var b in editData.bpmChanges)
                {
                    EditData.BpmChanges.Add(new BpmChange(b.tick, b.bpm));
                }
            }

            // ノート復元
            foreach (var note in editData.notes)
            {
                if (note.type == 1)
                {
                    // 単ノーツ
                    notePresenter.AddNote(ToNoteObject(note));
                    continue;
                }

                // Long ノーツ（親 → 子 の順に復元）
                var longNoteObjects = new[] { note }.Concat(note.notes)
                    .Select(note_ =>
                    {
                        notePresenter.AddNote(ToNoteObject(note_));
                        return EditData.Notes[ToNoteObject(note_).position];
                    })
                    .ToList();

                // prev / next を連結
                for (int i = 1; i < longNoteObjects.Count; i++)
                {
                    longNoteObjects[i].note.prev = longNoteObjects[i - 1].note.position;
                    longNoteObjects[i - 1].note.next = longNoteObjects[i].note.position;
                }

                EditState.LongNoteTailPosition.Value = NotePosition.None;
            }
        }

        /// <summary>
        /// NoteObject → MusicDTO.Note へ変換します。
        /// Long ノーツの場合は type=2、Single は type=1 として保存します。
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
        /// MusicDTO.Note → NoteObject へ変換します。
        /// </summary>
        public static Note ToNoteObject(MusicDTO.Note musicNote)
        {
            return new Note(
                new NotePosition(musicNote.LPB, musicNote.num, musicNote.block),
                musicNote.type == 1 ? NoteTypes.Single : NoteTypes.Long);
        }
    }
}
