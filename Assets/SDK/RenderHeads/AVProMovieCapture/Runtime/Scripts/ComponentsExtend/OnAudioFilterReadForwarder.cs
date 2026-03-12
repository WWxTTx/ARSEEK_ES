using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RenderHeads.Media.AVProMovieCapture
{
    public class OnAudioFilterReadForwarder : MonoBehaviour
    {
        public enum MuteBehaviour
        {
            None,
            Before,
            After
        }

        public MuteBehaviour _MuteBehaviour;

        public System.Action<float[], int> Callback { get; internal set; }

        public bool Streaming;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!Streaming || _MuteBehaviour == MuteBehaviour.Before)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }

            Callback?.Invoke(data, channels);

            if (_MuteBehaviour == MuteBehaviour.After)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }
        }
    }
}