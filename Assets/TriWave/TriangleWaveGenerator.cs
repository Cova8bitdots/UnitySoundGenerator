using System;
using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "TriangleWaveGenerator", menuName = "Audio/Generator/TriangleWaveGenerator")]
public class TriangleWaveGenerator : ScriptableObject, IGeneratorDefinition
{
    public float initialFrequency;

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;

    public Generator CreateRuntime(ControlContext context, DSPConfiguration? nestedConfiguration,
        ControlContext.ProcessorCreationParameters creationParameters)
    {
        return Processor.Allocate(context, initialFrequency);
    }

    [BurstCompile(CompileSynchronously = true)]
    internal struct Processor : Generator.IProcessor
    {
        const float k_Tau = Mathf.PI * 2;

        float m_Frequency;
        float m_Phase;

        public static Generator Allocate(ControlContext context, float frequency)
        {
            return context.AllocateGenerator(new Processor(frequency), new Control());
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;

        Generator.Setup m_Setup;

        Processor(float frequency)
        {
            m_Frequency = frequency;
            m_Phase = 0.0f;
            m_Setup = new Generator.Setup();
        }

        public void Update(UnityEngine.Audio.Processor.UpdatedDataContext context, UnityEngine.Audio.Processor.Pipe pipe)
        {
            var enumerator = pipe.GetAvailableData(context);

			foreach (var element in enumerator)
			{
	            if (element.TryGetData(out FrequencyData data))
    	            m_Frequency = data.Value;
        	    else
            	    Debug.Log("DataAvailable: unknown data."); 
			}
        }

        public Generator.Result Process(in ProcessingContext ctx, UnityEngine.Audio.Processor.Pipe pipe, ChannelBuffer buffer, Generator.Arguments args)
        {
            for (var frame = 0; frame < buffer.frameCount; frame++)
            {
                // 位相を [0,1) に正規化
                float phase = m_Phase - Mathf.Floor(m_Phase);

                // 三角波を -1～+1 で生成
                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;

                // 各チャンネルに書き込み
                for (var channel = 0; channel < buffer.channelCount; channel++)
                {
                    buffer[channel, frame] = tri;
                }

                // 次のサンプルへ時間を進める
                m_Phase += m_Frequency / m_Setup.sampleRate;
                if (m_Phase > 1.0f) m_Phase = 0.0f;
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

        internal struct FrequencyData
        {
            public readonly float Value;

            public FrequencyData(float value)
            {
                Value = value;
            }
        }
    }
}
