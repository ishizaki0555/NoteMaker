// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// SettingsDTO.cs
// エディタ設定（作業フォルダ・レーン数・入力キー設定など）を
// 保存・読み込みするためのデータ転送オブジェクト（DTO）を定義します。
// 
//========================================

using System.Collections.Generic;

namespace NoteMaker.DTO
{
    /// <summary>
    /// エディタ全体の設定情報を保持する DTO クラスです。
    /// 作業フォルダのパス、最大レーン数、ノート入力キー設定など、
    /// エディタの動作に必要な基本設定をまとめて管理します。
    /// </summary>
    [System.Serializable]
    public class SettingsDTO
    {
        public string workSpacePath;          // 作業フォルダのパス
        public int maxBlock;                  // 使用可能なレーン（ブロック）数
        public List<int> noteInputKeyCodes;   // ノート入力に使用するキーコード一覧

        /// <summary>
        /// デフォルト設定を生成して返します。
        /// 新規プロジェクト作成時や設定ファイルが存在しない場合に使用されます。
        /// </summary>
        /// <returns>初期値が設定された SettingsDTO。</returns>
        public static SettingsDTO GetDefaultSettings()
        {
            return new SettingsDTO
            {
                workSpacePath = "",
                maxBlock = 5,
                noteInputKeyCodes = new List<int> { 114, 99, 103, 121, 98 }
            };
        }
    }
}
