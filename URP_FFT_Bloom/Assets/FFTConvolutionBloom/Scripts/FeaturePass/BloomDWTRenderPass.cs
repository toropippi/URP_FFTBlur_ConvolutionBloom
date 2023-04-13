using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomDWTRenderPass : ScriptableRenderPass
{
    private const string CommandBufferName = nameof(BloomDWTRenderPass);

    private RenderTargetIdentifier _colorTarget;
    private FFTBloom _fFTBloom = null;

    private Material mat_first;
    private Material mat_final;
    float _threshold;//Bloomかける明るさのしきい値

    //PropertyToID関連
    private int _fftTempID1, _fftTempID2;

    public BloomDWTRenderPass(Shader shader_first, Shader shader_final)
    {
        mat_first = CoreUtils.CreateEngineMaterial(shader_first);
        mat_final = CoreUtils.CreateEngineMaterial(shader_final);
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
        // 他のサンプルみたく回り込まないFFT計算をしているのでborderRatioは常に1.0
        commandBuffer.SetGlobalFloat("_ScalingRatio", 1f);
        commandBuffer.SetGlobalFloat("_threshold", _threshold);
        commandBuffer.Blit(_colorTarget, _fftTempID1, mat_first);

        //ここでFFTConvolution実行
        _fFTBloom.FFTConvolutionFromRenderPassDWT(commandBuffer, _fftTempID1, _fftTempID2);

        // RenderTextureを現在のRenderTarget（カメラ）にコピー
        // 余白の分を考慮
        commandBuffer.SetGlobalFloat("_ScalingRatio", 1f);
        commandBuffer.Blit(_fftTempID2, _colorTarget, mat_final);

        context.ExecuteCommandBuffer(commandBuffer);
        context.Submit();
        CommandBufferPool.Release(commandBuffer);
    }

    public void SetParam(RenderTargetIdentifier colorTarget, float threshold)
    {
        _colorTarget = colorTarget;
        _threshold = threshold;
    }

    public void SetFFT(FFTBloom fFTBloom)
    {
        _fFTBloom = fFTBloom;
    }
}