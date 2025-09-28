using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SqrWaveGenerator
{
    public enum DutyRatio : int
    {
        UNDEFINED = 0,
        Eightth = 8,
        Quarter = 4,
        Half = 2,
    }
    [RequireComponent(typeof(AudioSource))]
    public class SquareWaveGeneratorControl : MonoBehaviour
    {
        [SerializeField] AudioSource m_AudioSource;
        [Range(100f, 5000f)] public float frequency = 432;
        public DutyRatio dutyRatio = DutyRatio.Eightth;

        float m_PreviousFrequency;
        DutyRatio m_prevRatio = DutyRatio.UNDEFINED;

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
                || (Mathf.Approximately(frequency, m_PreviousFrequency) && dutyRatio == m_prevRatio)
                || dutyRatio == DutyRatio.UNDEFINED
                )
                return;

            ControlContext.builtIn.SendData(handle, new SquareWaveGenerator.Processor.WaveData(frequency, 1f/(float)dutyRatio));
            m_PreviousFrequency = frequency;
            m_prevRatio = dutyRatio;
        }
    }

}