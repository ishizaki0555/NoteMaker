// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// KeyInput.cs
// Shift / Alt / Ctrl の組み合わせ入力や、押下中のキー検出など、
// エディタ全体で利用されるキーボード入力ユーティリティです。
// Unity の Input API をラップし、可読性と再利用性を高めています。
// 
//========================================

using UnityEngine;

namespace NoteMaker.Utility
{
    /// <summary>
    /// キーボード入力を扱うユーティリティクラスです。
    /// ・Shift / Alt / Ctrl + 任意キーの同時押し判定  
    /// ・修飾キーの押下 / 押下開始判定  
    /// ・現在押されているキーの取得  
    /// など、エディタ操作で頻繁に使う処理をまとめています。
    /// </summary>
    public class KeyInput
    {
        /// <summary>
        /// Shift + 指定キーが押されたかどうか。
        /// </summary>
        public static bool ShiftPlus(KeyCode keyCode)
        {
            return ShiftKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Shift キーが押されているかどうか。
        /// </summary>
        public static bool ShiftKey()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        /// <summary>
        /// Shift キーが押された瞬間かどうか。
        /// </summary>
        public static bool ShiftKeyDown()
        {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }

        /// <summary>
        /// Alt + 指定キーが押されたかどうか。
        /// </summary>
        public static bool AltPlus(KeyCode keyCode)
        {
            return AltKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Alt キーが押されているかどうか。
        /// </summary>
        public static bool AltKey()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        /// <summary>
        /// Alt キーが押された瞬間かどうか。
        /// </summary>
        public static bool AltKeyDown()
        {
            return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
        }

        /// <summary>
        /// Ctrl + 指定キーが押されたかどうか。
        /// Mac の Command キーも Ctrl として扱います。
        /// </summary>
        public static bool CtrlPlus(KeyCode keyCode)
        {
            return CtrlKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Ctrl（または Command）キーが押されているかどうか。
        /// </summary>
        public static bool CtrlKey()
        {
            return Input.GetKey(KeyCode.LeftControl) ||
                   Input.GetKey(KeyCode.LeftCommand) ||
                   Input.GetKey(KeyCode.RightControl) ||
                   Input.GetKey(KeyCode.RightCommand);
        }

        /// <summary>
        /// Ctrl（または Command）キーが押された瞬間かどうか。
        /// </summary>
        public static bool CtrlKeyDown()
        {
            return Input.GetKeyDown(KeyCode.LeftControl) ||
                   Input.GetKeyDown(KeyCode.LeftCommand) ||
                   Input.GetKeyDown(KeyCode.RightControl) ||
                   Input.GetKeyDown(KeyCode.RightCommand);
        }

        /// <summary>
        /// 現在押されているキーを 1 つ返します。
        /// 何も押されていない場合は KeyCode.None を返します。
        /// </summary>
        public static KeyCode FetchKey()
        {
            // KeyCode 列挙体の全値をループして、押されているキーを検出します。
            int length = System.Enum.GetNames(typeof(KeyCode)).Length;

            // KeyCode.None は 0 なので、1 から開始して押されているキーを探します。
            for (int i = 0; i < length; i++)
            {
                if (Input.GetKey((KeyCode)i))
                    return (KeyCode)i;
            }

            return KeyCode.None;
        }
    }
}
