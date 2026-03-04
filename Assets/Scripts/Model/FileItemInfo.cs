// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// FileItemInfo.cs
// ファイルブラウザなどで使用する、単一のファイルまたはディレクトリ情報を
// 保持するためのシンプルなデータクラスです。
// 
//========================================

namespace NoteMaker.Model
{
    /// <summary>
    /// ファイルまたはディレクトリの情報を保持するクラスです。
    /// isDirectory で種類を判定し、fullName にフルパスを保持します。
    /// </summary>
    public class FileItemInfo
    {
        public bool isDirectory; // ディレクトリかどうか
        public string fullName;  // フルパス（ファイル名またはディレクトリ名を含む）

        /// <summary>
        /// FileItemInfo の新しいインスタンスを生成します。
        /// </summary>
        /// <param name="isDirectory">ディレクトリなら true、ファイルなら false。</param>
        /// <param name="fullName">対象のフルパス。</param>
        public FileItemInfo(bool isDirectory, string fullName)
        {
            this.isDirectory = isDirectory;
            this.fullName = fullName;
        }
    }
}
