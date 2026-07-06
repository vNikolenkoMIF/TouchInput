using System;
using System.Collections;
using System.Collections.Generic;
using TouchInput.Source.Gestures.Scripts.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TouchInput.Source.Gestures.Scripts
{
    [Serializable]
    public class TapEvent : UnityEvent<TapEventData> {}

    /// <summary>
    /// Counts consecutive taps/clicks and fires when the count reaches the configured maximum
    /// in the listening window. Requires a Graphic with raycastTarget=true on the same GameObject.
    /// </summary>
    public sealed class TapGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip("Time in seconds to wait for additional taps before firing with the current count.")]
        private float _listeningTime = 0.35f;

        [SerializeField]
        [Tooltip("Tap count at which the event fires immediately without waiting for the listening window.")]
        private int _maxTapCount = 2;

        public TapEvent OnTap = new TapEvent();

        public event Action<TapEventData> Tapped;

        private int _tapCount;
        private Vector2 _firstTapPosition;
        private Coroutine _fireCoroutine;

        private readonly HashSet<int> _activePointers = new();

        private void OnDisable()
        {
            CancelFire();
            _activePointers.Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _activePointers.Add(eventData.pointerId);

            if (_activePointers.Count > 1)
            {
                CancelFire();
                _tapCount = 0;
            }
            else
            {
                if (_tapCount == 0)
                    _firstTapPosition = eventData.position;

                _tapCount++;
                CancelFire();

                if (_tapCount >= _maxTapCount)
                    FireAndReset();
                else
                    _fireCoroutine = StartCoroutine(ListenAndFire());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _activePointers.Remove(eventData.pointerId);
        }

        private IEnumerator ListenAndFire()
        {
            yield return new WaitForSecondsRealtime(_listeningTime);
            FireAndReset();
        }

        private void FireAndReset()
        {
            if (_tapCount >= _maxTapCount) {
                
                var data = new TapEventData
                {
                    Count = _tapCount,
                    Position = _firstTapPosition
                };
                
                Tapped?.Invoke(data);
                OnTap.Invoke(data);
            }
            
            _tapCount = 0;
            _fireCoroutine = null;


        }

        private void CancelFire()
        {
            if (_fireCoroutine == null)
                return;

            StopCoroutine(_fireCoroutine);
            _fireCoroutine = null;
        }
    }
}
