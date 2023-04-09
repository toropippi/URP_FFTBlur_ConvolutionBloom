using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussBlurRenderPass : ScriptableRenderPass
{
    private const string CommandBufferName = nameof(GaussBlurRenderPass);

    private RenderTargetIdentifier _colorTarget;
    private FFTBloom _fFTBloom = null;

    //PropertyToID�֘A
    private int _fftTempID1, _fftTempID2;

    public GaussBlurRenderPass()
    {
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
        commandBuffer.Blit(_colorTarget, _fftTempID1);

        //������FFTConvolution���s
        _fFTBloom.FFTConvolutionFromRenderPass(commandBuffer, _fftTempID1, _fftTempID2);

        // RenderTexture�����݂�RenderTarget�i�J�����j�ɃR�s�[
        commandBuffer.Blit(_fftTempID2, _colorTarget);

        context.ExecuteCommandBuffer(commandBuffer);
        context.Submit();
        CommandBufferPool.Release(commandBuffer);
    }

    public void SetParam(RenderTargetIdentifier colorTarget)
    {
        _colorTarget = colorTarget;
    }

    public void SetFFT(FFTBloom fFTBloom) 
    {
        _fFTBloom = fFTBloom;
    }
}