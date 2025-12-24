// ========================================
//
// SettingsSerializer.cs
//
// ========================================
//
// Settings（エディタ設定）を JSON に保存・読み込みするためのシリアライザ
//
// ========================================

using NoteMaker.DTO;
using System.Linq;
using UnityEngine;

namespace NoteMaker.Model
{
    public class SettingsSerializer
    {
        /// <summary>
        /// JSON 文字列から設定を復元する。
        /// </summary>
        public static void Deserialize(string json)
        {
            var dto = JsonUtility.FromJson<SettingsDTO>(json);

            // ノート入力キー設定を復元
            Settings.NoteInputKeyCodes.Value = dto.noteInputKeyCodes
                .Select(keyCodeNum => (KeyCode)keyCodeNum)
                .ToList();

            // 最大ブロック数を復元
            Settings.MaxBlock = dto.maxBlock;

            // ワークスペースパスを復元（未設定なら persistentDataPath を使用）
            Settings.WorkSpacePath.Value = string.IsNullOrEmpty(dto.workSpacePath)
                ? Application.persistentDataPath
                : dto.workSpacePath;
        }

        /// <summary>
        /// 現在の設定を JSON 文字列に変換する。
        /// </summary>
        public static string Serialize()
        {
            var data = new SettingsDTO();

            // ワークスペースパス
            data.workSpacePath = Settings.WorkSpacePath.Value;

            // 最大ブロック数
            data.maxBlock = EditData.MaxBlock.Value;

            // ノート入力キー設定（ブロック数に合わせて切り詰め）
            data.noteInputKeyCodes = Settings.NoteInputKeyCodes.Value
                .Take(EditData.MaxBlock.Value)
                .Select(keyCode => (int)keyCode)
                .ToList();

            return JsonUtility.ToJson(data);
        }
    }
}
