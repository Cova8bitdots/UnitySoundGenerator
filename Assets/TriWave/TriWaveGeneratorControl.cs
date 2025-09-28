namespace TriWave
{
    using System;
    using UnityEngine;
    using UnityEngine.Audio;

    [RequireComponent(typeof(AudioSource))]
    public class TriWaveGeneratorControl : MonoBehaviour
    {
        [SerializeField] AudioSource m_AudioSource;
        [Range(100f, 5000f)]
        public float frequency = 432;

        float m_PreviousFrequency;

        void Reset()
        {
            TryGetComponent(out m_AudioSource);
        }

        private void Awake()
        {
            m_AudioSource ??= GetComponent<AudioSource>();
        }

        void Update()
        {
            var handle = m_AudioSource.generatorHandle;

            if (!m_AudioSource.isPlaying
                || !ControlContext.builtIn.Exists(handle)
                || Mathf.Approximately(frequency, m_PreviousFrequency))
                return;

            ControlContext.builtIn.SendData(handle, new TriangleWaveGenerator.Processor.FrequencyData(frequency));
            m_PreviousFrequency = frequency;
        }
    }

}