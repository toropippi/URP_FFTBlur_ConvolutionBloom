using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomFFTRenderer : ScriptableRendererFeature
{
    class BloomFFTPass : ScriptableRenderPass
    {
        private Material _material;
        private FFTBloom _fft;
        private RenderTargetIdentifier _source;
        private RenderTargetHandle _temp;
        public BloomFFTVolume settings;

        public BloomFFTPass(Shader shader)
        {
            _material = CoreUtils.CreateEngineMaterial(shader);
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            _temp.Init("_BloomFFTTemp");
        }

        public void Setup(RenderTargetIdentifier source)
        {
            _source = source;
        }

        public void SetFFT(FFTBloom fft) => _fft = fft;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings == null || !settings.IsActive() || _fft == null)
                return;

            var cmd = CommandBufferPool.Get("Bloom FFT");

            _material.SetFloat("_Intensity", settings.intensity.value);
            _material.SetFloat("_Threshold", settings.threshold.value);

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(_temp.id, desc);

            cmd.Blit(_source, _temp.Identifier());
            _fft.FFTConvolutionFromRenderPass(cmd, _temp.id, _temp.id);
            cmd.Blit(_temp.Identifier(), _source, _material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [SerializeField] Shader shader;
    BloomFFTPass _pass;

    public override void Create()
    {
        _pass = new BloomFFTPass(shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack.GetComponent<BloomFFTVolume>();
        if (stack == null || !stack.IsActive())
            return;

        if (_pass == null)
            Create();

        _pass.settings = stack;
        _pass.Setup(renderer.cameraColorTarget);

        var fft = renderingData.cameraData.camera.GetComponent<FFTBloom>();
        if (fft != null)
        {
            _pass.SetFFT(fft);
            renderer.EnqueuePass(_pass);
        }
    }
}
