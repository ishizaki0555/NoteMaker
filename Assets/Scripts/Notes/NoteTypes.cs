// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// NoteTypes.cs
// ノートの種類を表す列挙体です。Single（単ノーツ）と Long（ロングノーツ）
// の 2 種類のみを扱います。
// 
//========================================

namespace NoteMaker.Notes
{
    /// <summary>
    /// ノートの種類を表す列挙体です。
    /// ・Single … 単ノーツ  
    /// ・Long   … ロングノーツ（prev / next による連結を持つ）  
    /// </summary>
    public enum NoteTypes
    {
        Single,
        Long
    }
}