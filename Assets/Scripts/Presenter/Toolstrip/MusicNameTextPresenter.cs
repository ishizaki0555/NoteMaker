// ========================================
//
// NoteMaker Project
//
// ========================================
//
// MusicNameTextPresenter.cs
// 現在編集中の楽曲名（EditData.Name）を UI テキストへ反映する
// シンプルなプレゼンターです。
// ReactiveProperty と Text を自動同期させるだけの役割を持ちます。
//
//========================================

using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 楽曲名（EditData.Name）を Text コンポーネントへ反映するクラスです。
    /// ・ReactiveProperty の変更を SubscribeToText で自動反映  
    /// ・MusicSelect → EditData.Name 更新 → UI に即時反映  
    /// </summary>
    public class MusicNameTextPresenter : MonoBehaviour
    {
        [SerializeField] Text musicNameText = default; // 楽曲名表示テキスト

        void Awake()
        {
            // EditData.Name の変更を UI に反映
            EditData.Name.SubscribeToText(musicNameText);
        }
    }
}