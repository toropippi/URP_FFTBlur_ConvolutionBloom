using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FFTBloomRenderFeature : ScriptableRendererFeature
{
    //passŠÖ˜A
    GaussBlurRenderPass _renderPassGaussBlur;
    GaussBlurBorderRenderPass _renderPassGaussBlurBorder;
    BloomRenderPass _renderPassBloom;
    BloomDWTRenderPass _renderPassBloomDWT;
    public enum SelectedPass
    {
        GaussBlurRenderPass,
        GaussBlurBorderRenderPass,
        BloomRenderPass,
        BloomDWTRenderPass
    }

    [SerializeField] SelectedPass selectedPass = SelectedPass.GaussBlurRenderPass;

    //shaderŠÖ˜A
    FFTBloom _fFTBloom;
    [SerializeField] Shader scalingShader;
    [SerializeField] Shader bloomFirstShader;
    [SerializeField] Shader bloomFinalShader;
    [SerializeField] [Range(0f, 0.499f)] float borderRatio;
    [SerializeField] float threshold = 2.5f;//Bloom‚©‚¯‚é–¾‚é‚³‚Ì‚µ‚«‚¢’l

    public override void Create()
    {
        _renderPassGaussBlur = new GaussBlurRenderPass();
        _renderPassGaussBlurBorder = new GaussBlurBorderRenderPass(scalingShader);
        _renderPassBloom = new BloomRenderPass(bloomFirstShader, bloomFinalShader);
        _renderPassBloomDWT = new BloomDWTRenderPass(bloomFirstShader, bloomFinalShader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        switch (selectedPass)
        {
            case SelectedPass.GaussBlurRenderPass:
                _renderPassGaussBlur.SetParam(renderer.cameraColorTarget);
                _renderPassGaussBlur.SetFFT(_fFTBloom);
                renderer.EnqueuePass(_renderPassGaussBlur);
                break;

            case SelectedPass.GaussBlurBorderRenderPass:
                _renderPassGaussBlurBorder.SetParam(renderer.cameraColorTarget, borderRatio);
                _renderPassGaussBlurBorder.SetFFT(_fFTBloom);
                renderer.EnqueuePass(_renderPassGaussBlurBorder);
                break;

            case SelectedPass.BloomRenderPass:
                _renderPassBloom.SetParam(renderer.cameraColorTarget, borderRatio, threshold);
                _renderPassBloom.SetFFT(_fFTBloom);
                renderer.EnqueuePass(_renderPassBloom);
                break;
            case SelectedPass.BloomDWTRenderPass:
                _renderPassBloomDWT.SetParam(renderer.cameraColorTarget, threshold);
                _renderPassBloomDWT.SetFFT(_fFTBloom);
                renderer.EnqueuePass(_renderPassBloomDWT);
                break;
            default:
                break;
        }
    }

    public void SetFFT(FFTBloom fFTBloom)
    {
        _fFTBloom = fFTBloom;
    }
}
