using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomRenderPass : ScriptableRenderPass
{
    private const string CommandBufferName = nameof(GaussBlurRenderPass);

    private RenderTargetIdentifier _colorTarget;
    private FFTBloom _fFTBloom = null;

    private Material mat_first;
    private Material mat_final;
    private float _borderRatio;
    float _threshold;//Bloom�����閾�邳�̂������l

    //PropertyToID�֘A
    private int _fftTempID1, _fftTempID2;

    public BloomRenderPass(Shader shader_first, Shader shader_final)
    {
        mat_first = CoreUtils.CreateEngineMaterial(shader_first);
        mat_final = CoreUtils.CreateEngineMaterial(shader_final);
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        _fftTempID1 = Shader.PropertyToID("_fftTempID1");//FFT�̓���
        _fftTempID2 = Shader.PropertyToID("_fftTempID2");//FFT�̏o��
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera) return;
        if (_fFTBloom == null) return;

        var commandBuffer = CommandBufferPool.Get(CommandBufferName);

        commandBuffer.GetTemporaryRT(_fftTempID1, _fFTBloom.Descriptor, FilterMode.Bilinear);//���́Bxy�T�C�Y��_fFTBloom.Descriptor����Ȃ��Ă��ǂ�
        commandBuffer.GetTemporaryRT(_fftTempID2, _fFTBloom.Descriptor, FilterMode.Bilinear);//�o��


        // ���݂̃J�����`��摜��RenderTexture�ɃR�s�[
        // borderRatio��0.0���傫�����邱�Ƃŉ�ʒ[�ɗ]�����������邱�Ƃ��ł���Bfft�v�Z�Œ[����[�ɉ�荞��Ńu���[�������邽�߂̑Ώ�
        // convolution kernel�̓��e�ɂ��킹�Ē�����
        // _colorTarget��filter moder=clamp�ɂ��邱�Ƃŉ�ʒ[�������L�΂����Ƃ��ł���
        commandBuffer.SetGlobalFloat("_ScalingRatio", 1f / (1f - 2f * _borderRatio));
        commandBuffer.SetGlobalFloat("_threshold", _threshold);
        commandBuffer.Blit(_colorTarget, _fftTempID1, mat_first);

        //������FFTConvolution���s
        _fFTBloom.FFTConvolutionFromRenderPass(commandBuffer, _fftTempID1, _fftTempID2);

        // RenderTexture�����݂�RenderTarget�i�J�����j�ɃR�s�[
        // �]���̕����l��
        commandBuffer.SetGlobalFloat("_ScalingRatio", 1f - 2f * _borderRatio);
        commandBuffer.Blit(_fftTempID2, _colorTarget, mat_final);

        context.ExecuteCommandBuffer(commandBuffer);
        context.Submit();
        CommandBufferPool.Release(commandBuffer);
    }

    public void SetParam(RenderTargetIdentifier colorTarget, float borderRatio, float threshold)
    {
        _colorTarget = colorTarget;
        _borderRatio = borderRatio;
        _threshold = threshold;
    }

    public void SetFFT(FFTBloom fFTBloom)
    {
        _fFTBloom = fFTBloom;
    }
}