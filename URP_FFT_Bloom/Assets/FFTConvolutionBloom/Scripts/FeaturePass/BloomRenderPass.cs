using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomRenderPass : ScriptableRenderPass
{
    private const string CommandBufferName = nameof(BloomRenderPass);

    private RenderTargetIdentifier _colorTarget;
    private FFTBloom _fFTBloom = null;

    private Material mat_first;
    private Material mat_final;
    private float _borderRatio;
    float _threshold;//Bloomかける明るさのしきい値

    //PropertyToID関連
    private int _fftTempID1, _fftTempID2;

    public BloomRenderPass(Shader shader_first, Shader shader_final)
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
        // borderRatioを0.0より大きくすることで画面端に余白を持たせることができる。fft計算で端から端に回り込んでブラーがかかるための対処
        // convolution kernelの内容にあわせて調整を
        // _colorTargetのfilter moder=clampにすることで画面端を引き伸ばすことができる
        commandBuffer.SetGlobalFloat("_ScalingRatio", 1f / (1f - 2f * _borderRatio));
        commandBuffer.SetGlobalFloat("_threshold", _threshold);
        commandBuffer.Blit(_colorTarget, _fftTempID1, mat_first);

        //ここでFFTConvolution実行
        _fFTBloom.FFTConvolutionFromRenderPass(commandBuffer, _fftTempID1, _fftTempID2);

        // RenderTextureを現在のRenderTarget（カメラ）にコピー
        // 余白の分を考慮
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