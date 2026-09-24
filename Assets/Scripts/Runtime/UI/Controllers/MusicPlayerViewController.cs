using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Меню плеера с наушниками. Открывается по E, когда игрок смотрит на плеер:
    /// прокручиваемый список песен из Resources/Music, выбор запускает трек.
    /// Музыка продолжает играть после закрытия меню — это колонка киоска.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class MusicPlayerViewController : ViewController<MusicPlayerView>
    {
        private const string MusicResourcesFolder = "Music";
        private const string NothingPlayingText = "Ничего не играет";
        private const string NowPlayingFormat = "Сейчас играет: {0}";

        private readonly List<AudioClip> songs = new();
        private readonly List<string> songTitles = new();

        private ICursorSystem cursorSystem;

        private AudioSource audioSource;
        private int currentSong = -1;
        private bool subscribed;

        /// <summary><c>true</c>, пока меню плеера на экране.</summary>
        public bool IsOpen => View != null && ViewState is ViewVisibilityState.Showing or ViewVisibilityState.Shown;

        protected override void Awake()
        {
            base.Awake();

            SystemsUtility.TryGetSystem(out cursorSystem);
            SystemsUtility.TryAddListener<GamePausedMessage>(OnGamePaused);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == false)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        private void OnDestroy()
        {
            SystemsUtility.TryRemoveListener<GamePausedMessage>(OnGamePaused);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Subscribe();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Unsubscribe();
        }

        #region Public API

        /// <summary>Открыть меню плеера.</summary>
        public void Open()
        {
            LoadSongs();

            var view = View;

            if (view == null)
            {
                return;
            }

            view.SetSongs(songTitles);
            view.SetSelectedRow(currentSong);
            view.SetNowPlaying(
                currentSong >= 0 && currentSong < songTitles.Count
                    ? string.Format(NowPlayingFormat, songTitles[currentSong])
                    : NothingPlayingText
            );

            View.Show();
        }

        /// <summary>Закрыть меню плеера. Музыка продолжает играть.</summary>
        public void Close()
        {
            if (IsOpen)
            {
                View.Hide();
            }
        }

        /// <summary>Открыть, если закрыто, и наоборот.</summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();

                return;
            }

            Open();
        }

        #endregion

        #region View subscription

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            var view = View;

            if (view == null)
            {
                return;
            }

            view.OnCloseClicked += Close;
            view.OnSongClicked += OnSongClickedInternal;

            view.OnShowEntered += OnViewShowEntered;
            view.OnHideEntered += OnViewHideEntered;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed == false)
            {
                return;
            }

            var view = View;

            if (view != null)
            {
                view.OnCloseClicked -= Close;
                view.OnSongClicked -= OnSongClickedInternal;

                view.OnShowEntered -= OnViewShowEntered;
                view.OnHideEntered -= OnViewHideEntered;
            }

            subscribed = false;
        }

        private void OnViewShowEntered()
        {
            cursorSystem?.UnLockCursor();
        }

        private void OnViewHideEntered()
        {
            cursorSystem?.LockCursor();
        }

        private void OnGamePaused(GamePausedMessage message)
        {
            Close();
        }

        #endregion

        #region Songs

        private void LoadSongs()
        {
            if (songs.Count > 0)
            {
                return;
            }

            var clips = Resources.LoadAll<AudioClip>(MusicResourcesFolder);

            foreach (var clip in clips)
            {
                if (clip == false)
                {
                    continue;
                }

                songs.Add(clip);
                songTitles.Add(PrettifyTitle(clip.name));
            }

            if (songs.Count == 0)
            {
                Debug.LogWarning($"[MusicPlayer] В Resources/{MusicResourcesFolder} нет ни одного трека.", this);
            }
        }

        /// <summary>
        /// Название трека из имени файла: подчёркивания становятся пробелами.
        /// </summary>
        private static string PrettifyTitle(string clipName)
        {
            return clipName.Replace('_', ' ').Trim();
        }

        private void OnSongClickedInternal(int index)
        {
            if (index < 0 || index >= songs.Count)
            {
                return;
            }

            if (audioSource != false && songs[index] != false)
            {
                // Повторный клик по играющей песне ставит её на паузу.
                if (currentSong == index && audioSource.isPlaying)
                {
                    audioSource.Pause();
                    View.SetNowPlaying($"Пауза: {songTitles[index]}");

                    return;
                }

                audioSource.clip = songs[index];
                audioSource.Play();
            }

            currentSong = index;

            View.SetSelectedRow(currentSong);
            View.SetNowPlaying(string.Format(NowPlayingFormat, songTitles[currentSong]));
        }

        #endregion
    }
}
