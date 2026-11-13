using UnityEngine;

namespace TechTalk
{
    internal readonly struct Telemetry
    {
        public readonly float Celsius, Vibration, Rpm, Severity;
        public Telemetry(float celsius, float vibration, float rpm, float severity)
        {
            Celsius = celsius;
            Vibration = vibration;
            Rpm = rpm;
            Severity = severity;
        }
    }

    internal static class TelemetrySimulator
    {
        public static Telemetry Sample(float time, float failureTime)
        {
            float severity = Mathf.SmoothStep(0,1,Mathf.Clamp01((failureTime - 1f) / 9f));
            return new Telemetry(52 + 85*severity + 2*Mathf.Sin(time*.73f),
                1.2f + severity*8.6f + .2f*Mathf.Sin(time*2.4f),
                3160 - 980*severity + 38*Mathf.Sin(time*.45f), severity);
        }
    }

    internal sealed class FailureScenarioController
    {
        public bool Running { get; private set; }
        public float Time { get; private set; }
        public void Start() { Running = true; Time = 0; }
        public void Reset() { Running = false; Time = 0; }
        public void Tick(float dt)
        {
            if (Running) Time = Mathf.Min(Time + dt, 16);
        }
    }

    internal sealed class TelemetryToShaderBinder
    {
        private readonly MaterialPropertyBlock properties = new();
        private static readonly int Mode = Shader.PropertyToID("_Mode");
        private static readonly int Temperature = Shader.PropertyToID("_Temp");
        private static readonly int Phase = Shader.PropertyToID("_Phase");
        private static readonly int Amplitude = Shader.PropertyToID("_Amplitude");

        public void Apply(Renderer renderer, int mode, float celsius, float time, float amplitude)
        {
            properties.SetFloat(Mode,mode);
            properties.SetFloat(Temperature,celsius);
            properties.SetFloat(Phase,time);
            properties.SetFloat(Amplitude,amplitude);
            renderer.SetPropertyBlock(properties);
        }
    }
}
