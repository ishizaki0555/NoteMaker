// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SettingsSerializer.cs
// Settings（エディタ設定）と SettingsDTO（保存用データ）の相互変換を行います。
// ワークスペースパス、最大ブロック数、ノート入力キー設定を JSON として
// 保存・読み込みするためのシリアライザです。
//
//========================================

using NoteMaker.DTO;
using System.Linq;
using UnityEngine;

namespace NoteMaker.Model
{
    /// <summary>
    /// Settings と SettingsDTO の相互変換を行うシリアライザです。
    /// ・Deserialize() : JSON → Settings（読み込み）
    /// ・Serialize()   : Settings → JSON（保存）
    /// ノート入力キーの復元、ワークスペースパスの補完、最大ブロック数の反映などを行います。
    /// </summary>
    public class SettingsSerializer
    {
        /// <summary>
        /// JSON を SettingsDTO に復元し、Settings に反映します。
        /// ノート入力キーは KeyCode に変換し、ワークスペースパスは空の場合
        /// persistentDataPath を使用します。
        /// </summary>
        public static void Deserialize(string json)
        {
            var dto = JsonUtility.FromJson<SettingsDTO>(json);

            // ノート入力キーを KeyCode に変換して反映
            Settings.NoteInputKeyCodes.Value = dto.noteInputKeyCodes
                .Select(keyCodeNum => (KeyCode)keyCodeNum)
                .ToList();

            // 最大ブロック数を反映
            Settings.MaxBlock = dto.maxBlock;

            // ワークスペースパス（空ならデフォルトパスを使用）
            Settings.WorkSpacePath.Value = string.IsNullOrEmpty(dto.workSpacePath)
                ? Application.persistentDataPath
                : dto.workSpacePath;
        }

        /// <summary>
        /// Settings の内容を SettingsDTO に変換し、JSON 文字列として返します。
        /// ノート入力キーは最大ブロック数に合わせて切り詰めて保存します。
        /// </summary>
        public static string Serialize()
        {
            var data = new SettingsDTO();

            data.workSpacePath = Settings.WorkSpacePath.Value;
            data.maxBlock = EditData.MaxBlock.Value;

            // ノート入力キーを最大ブロック数に合わせて保存
            data.noteInputKeyCodes = Settings.NoteInputKeyCodes.Value
                .Take(EditData.MaxBlock.Value)
                .Select(keyCode => (int)keyCode)
                .ToList();

            return JsonUtility.ToJson(data);
        }
    }
}
