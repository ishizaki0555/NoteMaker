// ========================================
//
// FileItemInfo.cs
//
// ========================================
//
// ファイルブラウザ用の項目情報（ディレクトリかどうか・フルパス）を保持するクラス
//
// ========================================

namespace NoteMaker.Model
{
    public class FileItemInfo
    {
        public bool isDirectory;   // ディレクトリかどうか
        public string fullName;    // フルパスまたはファイル名

        /// <summary>
        /// ディレクトリかどうかとフルパスを指定して項目情報を生成する。
        /// </summary>
        public FileItemInfo(bool isDirectory, string fullName)
        {
            this.isDirectory = isDirectory;
            this.fullName = fullName;
        }
    }
}
