using System.Collections.Generic;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// The tamagotchi on the kiosk counter. Pick it up and it comes alive: the little pet runs
    /// through its animation frames until it is put back down, where it returns to its spot.
    /// </summary>
    internal sealed class TamagotchiActor : MonoBehaviour
    {
        private static readonly int TexturePropertyId = Shader.PropertyToID("_BaseMap");

        [Header("Parts")]
        [SerializeField]
        private GrabInteractable interactable;

        [SerializeField]
        private Renderer screenRenderer;

        [Header("Frames")]
        [SerializeField]
        private List<Texture2D> frames = new();

        [Min(0.05f)]
        [SerializeField]
        private float frameDurationSeconds = 0.3f;

        [Min(0f)]
        [SerializeField]
        private float playCooldownSeconds = 5f;

        [Header("Home")]
        [Min(0.1f)]
        [SerializeField]
        private float returnSpeed = 4f;

        private MaterialPropertyBlock block;
        private IProgressSystem progressSystem;

        private int frameIndex;
        private float nextFrameTimeSeconds;
        private float nextPlayTimeSeconds;
        private bool wasSelected;

        private Vector3 homePosition;
        private Quaternion homeRotation;

        /// <summary>
        /// Wire the parts and the animation frames from the editor tools.
        /// </summary>
        public void Initialize(
            GrabInteractable grabInteractable,
            Renderer renderer,
            IEnumerable<Texture2D> animationFrames
        )
        {
            interactable = grabInteractable;
            screenRenderer = renderer;

            frames.Clear();

            if (animationFrames != null)
            {
                foreach (var frame in animationFrames)
                {
                    if (frame)
                    {
                        frames.Add(frame);
                    }
                }
            }
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            GameManager.TryGetSystem(out progressSystem);

            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        private void Start()
        {
            Apply(0);
        }

        private void Update()
        {
            if (interactable == false)
            {
                return;
            }

            var isSelected = interactable.IsSelected;

            if (isSelected != wasSelected)
            {
                wasSelected = isSelected;

                if (isSelected)
                {
                    OnPickedUp();
                }
                else
                {
                    Apply(0);
                }
            }

            if (wasSelected)
            {
                UpdateFrames();

                return;
            }

            ReturnHome();
        }

        private void OnPickedUp()
        {
            if (Time.time < nextPlayTimeSeconds)
            {
                return;
            }

            nextPlayTimeSeconds = Time.time + playCooldownSeconds;

            progressSystem?.NotifyTamagotchiPlayed();
        }

        private void UpdateFrames()
        {
            if (frames.Count <= 1 || Time.time < nextFrameTimeSeconds)
            {
                return;
            }

            frameIndex = (frameIndex + 1) % frames.Count;
            nextFrameTimeSeconds = Time.time + frameDurationSeconds;

            Apply(frameIndex);
        }

        /// <summary>
        /// Put the toy back on its spot after it was dropped somewhere in the kiosk.
        /// </summary>
        private void ReturnHome()
        {
            var position = transform.position;

            if ((position - homePosition).sqrMagnitude > 0.0004f)
            {
                transform.position = Vector3.Lerp(
                    position,
                    homePosition,
                    Time.deltaTime * returnSpeed
                );
            }

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                homeRotation,
                Time.deltaTime * returnSpeed
            );
        }

        private void Apply(int index)
        {
            if (screenRenderer == false || frames.Count == 0)
            {
                return;
            }

            var texture = frames[Mathf.Clamp(index, 0, frames.Count - 1)];
            if (texture == false)
            {
                return;
            }

            screenRenderer.GetPropertyBlock(block);
            block.SetTexture(TexturePropertyId, texture);
            screenRenderer.SetPropertyBlock(block);
        }
    }
}
