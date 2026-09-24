using System;
using System.Collections.Generic;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Меню плеера с наушниками: прокручиваемый список песен, выбор запускает музыку.
    /// Список строится из шаблона строки, как в меню коробки доставки.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class MusicPlayerView : View
    {
        #region Serialized

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text nowPlayingText;
        [SerializeField] private RectTransform rowsContent;
        [SerializeField] private MusicPlayerRowView rowTemplate;

        [Header("Footer")]
        [SerializeField] private Button closeButton;

        #endregion

        #region Events

        public event Action OnCloseClicked;

        public event Action<int> OnSongClicked;

        #endregion

        #region Cached state

        private readonly List<MusicPlayerRowView> rows = new();

        #endregion

        #region Public API

        /// <summary>Пересобрать список песен. Порядок совпадает с порядком у контроллера.</summary>
        public void SetSongs(IReadOnlyList<string> titles)
        {
            ClearRows();

            if (rowsContent == null || rowTemplate == null || titles == null)
            {
                return;
            }

            rowTemplate.gameObject.SetActive(false);

            for (var index = 0; index < titles.Count; index++)
            {
                var row = Instantiate(rowTemplate, rowsContent);
                row.gameObject.SetActive(true);
                row.Initialize(index, titles[index], OnSongClickedInternal);

                rows.Add(row);
            }
        }

        /// <summary>Строка «сейчас играет» под списком.</summary>
        public void SetNowPlaying(string text)
        {
            if (nowPlayingText != null)
            {
                nowPlayingText.text = text;
            }
        }

        /// <summary>Подсветить выбранную песню в списке.</summary>
        public void SetSelectedRow(int index)
        {
            for (var row = 0; row < rows.Count; row++)
            {
                if (rows[row] != null)
                {
                    rows[row].SetSelected(row == index);
                }
            }
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClickedInternal);
                closeButton.onClick.AddListener(OnCloseClickedInternal);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClickedInternal);
            }
        }

        #endregion

        #region Helpers

        private void ClearRows()
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            rows.Clear();
        }

        private void OnCloseClickedInternal()
        {
            OnCloseClicked?.Invoke();
        }

        private void OnSongClickedInternal(int index)
        {
            OnSongClicked?.Invoke(index);
        }

        #endregion
    }
}
