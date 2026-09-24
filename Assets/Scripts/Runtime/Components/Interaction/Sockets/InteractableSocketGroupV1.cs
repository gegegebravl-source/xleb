using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Sockets
{
    internal sealed class InteractableSocketGroupV1 : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
#else
        [Header("Events")]
#endif
        [SerializeField]
        private UnityEvent onSocketed;

        public event Action OnSocketed;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Debug")]
        [Sirenix.OdinInspector.ListDrawerSettings(ListElementLabelName = nameof(InteractableSocket.Name))]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        private readonly List<InteractableSocket> sockets = new();

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Debug")]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        private int socketedCount;

        private void Awake()
        {
            InitializeSockets();
        }

        private void OnEnable()
        {
            foreach (var socket in sockets)
            {
                socket.OnSocketed += OnSocketSocketed;
            }
        }

        private void OnDisable()
        {
            foreach (var socket in sockets)
            {
                socket.OnSocketed -= OnSocketSocketed;
            }
        }

        private void OnSocketSocketed()
        {
            socketedCount++;

            if (socketedCount >= sockets.Count)
            {
                OnSocketed?.Invoke();
                onSocketed.Invoke();

                Destroy(this);
            }
        }

        private void InitializeSockets()
        {
            sockets.Clear();
            sockets.AddRange(GetComponentsInChildren<InteractableSocket>());
        }
    }
}
