using System;
using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SquareWaveGenerator", menuName = "Audio/Generator/SquareWaveGenerator")]
public class SquareWaveGenerator : ScriptableObject, IGeneratorDefinition
{
    public float initialFrequency;
    public float initRatio;

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;

    public Generator CreateRuntime(ControlContext context, DSPConfiguration? nestedConfiguration,
        ControlContext.ProcessorCreationParameters creationParameters)
    {
        return Processor.Allocate(context, initialFrequency, Mathf.Clamp01(initRatio));
    }

    [BurstCompile(CompileSynchronously = true)]
    internal struct Processor : Generator.IProcessor
    {
        const float k_Tau = Mathf.PI * 2;

        float m_Frequency;
        private float m_DutyRatio;
        float m_Phase;

        public static Generator Allocate(ControlContext context, float frequency, float dutyRatio)
        {
            return context.AllocateGenerator(new Processor(frequency, dutyRatio), new Control());
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;

        Generator.Setup m_Setup;

        Processor(float frequency, float dutyRatio)
        {
            m_Frequency = frequency;
            m_Phase = 0.0f;
            m_DutyRatio = dutyRatio;
            m_Setup = new Generator.Setup();
        }

        public void Update(UnityEngine.Audio.Processor.UpdatedDataContext context, UnityEngine.Audio.Processor.Pipe pipe)
        {
            var enumerator = pipe.GetAvailableData(context);

			foreach (var element in enumerator)
			{
                if (element.TryGetData(out WaveData data))
                {
                    m_Frequency = data.Freq;
                    m_DutyRatio = data.Ratio;
                    
                }
        	    else
            	    Debug.Log("DataAvailable: unknown data."); 
			}
        }

        public Generator.Result Process(in ProcessingContext ctx, UnityEngine.Audio.Processor.Pipe pipe, ChannelBuffer buffer, Generator.Arguments args)
        {
            for (var frame = 0; frame < buffer.frameCount; frame++)
            {
                // 0..1 の位相
                float phase = m_Phase;

                // Dutyは安全にクランプ（完全0%/100%で無音や直流化を避ける）
                float d = Mathf.Clamp01(m_DutyRatio);
                d = Mathf.Clamp(d, 0.001f, 0.999f);

                // 矩形波（±1）
                float v = (phase < d) ? 1f : -1f;

                // 出力
                for (var channel = 0; channel < buffer.channelCount; channel++)
                {
                    buffer[channel, frame] = v;
                }

                // 位相進行とラップ
                m_Phase += m_Frequency / m_Setup.sampleRate;   // 1周期=1.0
                if (m_Phase >= 1f) m_Phase -= 1f;
            }

            return buffer.frameCount;
        }

        struct Control : Generator.IControl<Processor>
        {
            public void Configure(ControlContext context, ref Processor generator, in DSPConfiguration config, out Generator.Setup setup, ref Generator.Properties p)
            {
                generator.m_Setup = new Generator.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator.m_Setup;
            }

            public void Dispose(ControlContext context, ref Processor processor) { }

            public void Update(ControlContext context, UnityEngine.Audio.Processor.Pipe pipe) { }

            public UnityEngine.Audio.Processor.MessageStatus OnMessage(ControlContext context, UnityEngine.Audio.Processor.Pipe pipe, UnityEngine.Audio.Processor.Message message)
            {
                return UnityEngine.Audio.Processor.MessageStatus.Unhandled;
            }
        }

        internal readonly struct WaveData
        {
            public readonly float Freq;
            public readonly float Ratio;

            public WaveData(float freq, float ratio)
            {
                Freq = freq;
                Ratio = ratio;
            }
        }
    }
}
