using System.Collections.Generic;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// The little TV inside the kiosk. It flips through the channel sprites every few seconds, which
    /// is what makes the stall feel lived in.
    /// </summary>
    internal sealed class TvActor : MonoBehaviour
    {
        private static readonly int TexturePropertyId = Shader.PropertyToID("_BaseMap");

        [Header("Screen")]
        [SerializeField]
        private Renderer screenRenderer;

        [SerializeField]
        private List<Texture2D> channels = new();

        [Header("Zapping")]
        [Min(1f)]
        [SerializeField]
        private float channelDurationSeconds = 18f;

        [Tooltip("Chance that the next channel is picked at random instead of in order.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float randomChannelChance = 0.5f;

        private MaterialPropertyBlock block;
        private int channelIndex;
        private float nextSwitchTimeSeconds;

        public IReadOnlyList<Texture2D> Channels => channels;

        /// <summary>
        /// Wire the screen and its channels from the editor tools.
        /// </summary>
        public void Initialize(Renderer renderer, IEnumerable<Texture2D> channelTextures)
        {
            screenRenderer = renderer;
            channels.Clear();

            if (channelTextures != null)
            {
                foreach (var texture in channelTextures)
                {
                    if (texture)
                    {
                        channels.Add(texture);
                    }
                }
            }
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void Start()
        {
            Apply(channelIndex);

            nextSwitchTimeSeconds = Time.time + channelDurationSeconds;
        }

        private void Update()
        {
            if (channels.Count <= 1 || Time.time < nextSwitchTimeSeconds)
            {
                return;
            }

            channelIndex = randomChannelChance > 0f && Random.value < randomChannelChance
                ? Random.Range(0, channels.Count)
                : (channelIndex + 1) % channels.Count;

            nextSwitchTimeSeconds = Time.time + channelDurationSeconds;

            Apply(channelIndex);
        }

        private void Apply(int index)
        {
            if (screenRenderer == false || channels.Count == 0)
            {
                return;
            }

            var texture = channels[Mathf.Clamp(index, 0, channels.Count - 1)];
            if (texture == false)
            {
                return;
            }

            // Блок свойств может быть не создан, если Start обогнал OnEnable.
            block ??= new MaterialPropertyBlock();

            screenRenderer.GetPropertyBlock(block);
            block.SetTexture(TexturePropertyId, texture);
            screenRenderer.SetPropertyBlock(block);
        }
    }
}
