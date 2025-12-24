// ========================================
//
// SettingDTO.cs
//
// ========================================
//
// アプリ全体の設定データを保持する DTO
//
// ========================================

using System.Collections.Generic;

namespace NoteMaker.DTO
{
    [System.Serializable]
    public class SettingDTO
    {
        public string workSpacepath;        // ワークスペースのパス
        public int maxBlock;                // 最大ブロック数
        public List<int> noteInputKeyCodes; // ノート入力に使うキーコード一覧

        /// <summary>
        /// デフォルト設定を生成して返す。
        /// 設定ファイルが存在しない場合などに使用される。
        /// </summary>
        public static SettingDTO GetDefaultSettings()
        {
            return new SettingDTO
            {
                workSpacepath = "",
                maxBlock = 5,
                noteInputKeyCodes = new List<int> { 114, 99, 103, 121, 98 } // デフォルトキー
            };
        }
    }
}
