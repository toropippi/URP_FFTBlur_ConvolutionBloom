using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussBlurRenderPass : ScriptableRenderPass
{
    private const string CommandBufferName = nameof(GaussBlurRenderPass);

    private RenderTargetIdentifier _colorTarget;
    private FFTBloom _fFTBloom = null;

    //PropertyToID関連
    private int _fftTempID1, _fftTempID2;

    public GaussBlurRenderPass()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        _fftTempID1 = Shader.PropertyToID("_fftTempID1");//FFTの入力
        _fftTempID2 = Shader.PropertyToID("_fftTempID2");//FFTの出力
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera) return;
        if (_fFTBloom == null) return;

        var commandBuffer = CommandBufferPool.Get(CommandBufferName);

        commandBuffer.GetTemporaryRT(_fftTempID1, _fFTBloom.Descriptor, FilterMode.Bilinear);//入力。xyサイズは_fFTBloom.Descriptorじゃなくても良い
        commandBuffer.GetTemporaryRT(_fftTempID2, _fFTBloom.Descriptor, FilterMode.Bilinear);//出力


        // 現在のカメラ描画画像をRenderTextureにコピー
        commandBuffer.Blit(_colorTarget, _fftTempID1);

        //ここでFFTConvolution実行
        _fFTBloom.FFTConvolutionFromRenderPass(commandBuffer, _fftTempID1, _fftTempID2);

        // RenderTextureを現在のRenderTarget（カメラ）にコピー
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