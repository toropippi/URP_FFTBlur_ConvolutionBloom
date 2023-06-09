using UnityEngine;
using UnityEngine.Rendering;

public class FFTBloom : MonoBehaviour
{
    //Bloom設定周り
    const int fftSize = 512;
    private RenderTextureFormat bloomTexFormat = RenderTextureFormat.ARGBFloat;//or ARGBHalf

    //compute shader周り
    [SerializeField] private ComputeShader cs;
    [SerializeField] private Texture2D convolutionKernel;//深度8bitのpng。1色あたり256しかない
    [SerializeField] private bool use256x4;//256x4のほうを使うか普通のconvolutionKernelを使うか。基本ダイナミックレンジでやったほうが見た目がいい
    [SerializeField] private Texture2D convolutionKernel_256x4a;//深度8bitのpngを4枚つかってダイナミックレンジを擬似的に再現
    [SerializeField] private Texture2D convolutionKernel_256x4b;//深度8bitのpngを4枚つかってダイナミックレンジを擬似的に再現
    [SerializeField] private Texture2D convolutionKernel_256x4c;//深度8bitのpngを4枚つかってダイナミックレンジを擬似的に再現
    [SerializeField] private Texture2D convolutionKernel_256x4d;//深度8bitのpngを4枚つかってダイナミックレンジを擬似的に再現
    [SerializeField] private float intensity;
    [SerializeField] private FFTBloomRenderFeature _feature;//feature側からここのFFT関数が呼ばれる
    
    private float intensity_back;//convolutionKernelに指定した画像が明るくても暗くても1になるよう正規化する意味がある
    private int kernelFFTX, kernelIFFTX;
    private int kernelFFTY_HADAMARD_IFFTY;
    private int kernelFFTX_DWT, kernelIFFTX_DWT;
    private int kernelFFTY_HADAMARD_IFFTY_DWT;
    private int kernelFFTWY, kernelFFTWY_DWT;
    private int kernelCopySlide;

    private RenderTexture rtWeight = null;//convolutionKernelのFFT計算後の重みが入る
    private RenderTexture rtWeightDWT = null;//convolutionKernelのFFT計算後の重みが入る。DWT用

    //PropertyToID関連
    private int rt34_i, rt64_i;
    private RenderTargetIdentifier rtWeight_i, rtWeightDWT_i;

    //Descriptor関連
    private RenderTextureDescriptor descriptor34, descriptor44, descriptor43, descriptor412, descriptor64, out_descriptor;//RGBA=4,RGB=3
    public RenderTextureDescriptor Descriptor => out_descriptor;//出力の画像の大きさ

    private void Awake()
    {
        //RenderFeatureからここの関数を呼び出せるように
        _feature.SetFFT(this.GetComponent<FFTBloom>());

        //descriptorセット
        DescriptorInit();
        ShaderIDInit();

        //kernelセット
        SetKernles();

        //RenderTexture関連
        rtWeight = new RenderTexture(descriptor43);
        rtWeight.Create();
        rtWeight_i = new RenderTargetIdentifier(rtWeight);
        rtWeightDWT = new RenderTexture(descriptor412);
        rtWeightDWT.Create();
        rtWeightDWT_i = new RenderTargetIdentifier(rtWeightDWT);

        //convolutionKernelからfft後のWeightを計算
        CreateWeight();
    }

    void SetKernles()
    {
        kernelFFTX = cs.FindKernel("FFTX");
        kernelIFFTX = cs.FindKernel("IFFTX");
        kernelFFTY_HADAMARD_IFFTY = cs.FindKernel("FFTY_HADAMARD_IFFTY");
        kernelFFTWY = cs.FindKernel("FFTWY");
        kernelFFTX_DWT = cs.FindKernel("FFTX_DWT");
        kernelIFFTX_DWT = cs.FindKernel("IFFTX_DWT");
        kernelFFTY_HADAMARD_IFFTY_DWT = cs.FindKernel("FFTY_HADAMARD_IFFTY_DWT");
        kernelFFTWY_DWT = cs.FindKernel("FFTWY_DWT");
        kernelCopySlide = cs.FindKernel("CopySlide");
    }

    void DescriptorInit()
    {
        descriptor34 = new RenderTextureDescriptor(fftSize / 4 * 3 + 2, fftSize, bloomTexFormat, 0);
        descriptor34.enableRandomWrite = true;
        descriptor64 = new RenderTextureDescriptor(fftSize / 4 * 6, fftSize, bloomTexFormat, 0);
        descriptor64.enableRandomWrite = true;
        descriptor43 = new RenderTextureDescriptor(fftSize, fftSize / 4 * 3 + 2, bloomTexFormat, 0);
        descriptor43.enableRandomWrite = true;
        descriptor412 = new RenderTextureDescriptor(fftSize, fftSize / 4 * 12, bloomTexFormat, 0);
        descriptor412.enableRandomWrite = true;
        descriptor44 = new RenderTextureDescriptor(fftSize, fftSize, bloomTexFormat, 0);
        descriptor44.enableRandomWrite = true;
        out_descriptor = new RenderTextureDescriptor(fftSize, fftSize, bloomTexFormat, 0);
        out_descriptor.enableRandomWrite = true;
    }

    void ShaderIDInit()
    {
        rt34_i = Shader.PropertyToID("_rt34");
    }


    void Uchar4toFloat(RenderTexture outtex)
    {
        var c32a = convolutionKernel_256x4a.GetPixels32(0);
        var c32b = convolutionKernel_256x4b.GetPixels32(0);
        var c32c = convolutionKernel_256x4c.GetPixels32(0);
        var c32d = convolutionKernel_256x4d.GetPixels32(0);
        var w = convolutionKernel_256x4a.width;
        var h = convolutionKernel_256x4a.height;

        float[] host_data = new float[w * h * 4];

        float ms = 0.0f;
        for (int i = 0; i < w * h; i++)
        {
            uint ir = (((uint)c32a[i].r * 256 + c32b[i].r) * 256 + c32c[i].r) * 256 + c32d[i].r;
            uint ig = (((uint)c32a[i].g * 256 + c32b[i].g) * 256 + c32c[i].g) * 256 + c32d[i].g;
            uint ib = (((uint)c32a[i].b * 256 + c32b[i].b) * 256 + c32c[i].b) * 256 + c32d[i].b;
            host_data[i * 4 + 0] = (float)ir / 65536.0f / 65536.0f;
            host_data[i * 4 + 1] = (float)ig / 65536.0f / 65536.0f;
            host_data[i * 4 + 2] = (float)ib / 65536.0f / 65536.0f;
            host_data[i * 4 + 3] = 0.0f;
            ms += host_data[i * 4 + 0];
        }

        //SetPixelsのほうではうまくいかなかったのでComputeShaderを介してRenderTextureにコピーする
        var tmpbuf = new ComputeBuffer(w * h * 4, 4);
        tmpbuf.SetData(host_data);
        var tmprtex = new RenderTexture(w, h, 0, bloomTexFormat);
        tmprtex.enableRandomWrite = true;
        tmprtex.Create();
        int kernel = cs.FindKernel("BufToTex");
        cs.SetInt("width", w);
        cs.SetInt("height", h);
        cs.SetTexture(kernel, "Tex", tmprtex);
        cs.SetBuffer(kernel, "Buf_ro", tmpbuf);
        cs.Dispatch(kernel, (w + 7) / 8, (h + 7) / 8, 1);

        Graphics.Blit(tmprtex, outtex);//ComputeShaderはRenderTextureしか扱えないので
        tmpbuf.Release();
        tmprtex.Release();
    }


    /// Convolution kernelの事前FFT計算
    /// ついでにintensity_backの計算も
    private void CreateWeight()
    {
        RenderTexture rtex1 = new RenderTexture(descriptor44);
        rtex1.Create();
        RenderTexture rtex2 = new RenderTexture(descriptor44);
        rtex2.Create();
        RenderTexture rtex3 = new RenderTexture(descriptor34);
        rtex3.Create();
        RenderTexture rtex4 = new RenderTexture(descriptor64);
        rtex4.Create();

        uint[] res = new uint[4];
        for (int i = 0; i < 4; i++) res[i] = 0;
        ComputeBuffer SumBuf = new ComputeBuffer(4, 4);//R,G,Bそれぞれにおける全画面の値の総計。intensity_backの計算のため
        SumBuf.SetData(res);

        //Convolution kernel読み込み→rtex2
        if (use256x4)
        {
            Uchar4toFloat(rtex2);
        }
        else 
        {
            Graphics.Blit(convolutionKernel, rtex2);//ComputeShaderはRenderTextureしか扱えないので
        }

        //テクスチャの中心を0,0に移動。ついでにSumBufに全画素値合計する
        cs.SetBuffer(kernelCopySlide, "SumBuf", SumBuf);
        cs.SetTexture(kernelCopySlide, "Tex_ro", rtex2);
        cs.SetTexture(kernelCopySlide, "Tex", rtex1);
        cs.SetBool("use256x4", use256x4);
        cs.SetInt("width", fftSize);
        cs.SetInt("height", fftSize);
        cs.SetBool("isWeightcalc", false);
        
        cs.Dispatch(kernelCopySlide, fftSize / 8, fftSize / 8, 1);//これで中心が0,0に移動したものがrtex1にはいる

        //正巡回畳み込みのほう
        //1
        cs.SetTexture(kernelFFTX, "Tex_ro", rtex1);
        cs.SetTexture(kernelFFTX, "Tex", rtex3);
        cs.Dispatch(kernelFFTX, fftSize, 1, 1);
        //2
        cs.SetTexture(kernelFFTWY, "Tex_ro", rtex3);
        cs.SetTexture(kernelFFTWY, "Tex", rtWeight);
        cs.Dispatch(kernelFFTWY, fftSize / 4 * 3 + 2, 1, 1);//FFTY_HADAMARD_IFFTYでのTextureアクセス高速化のため転置している


        //正巡回畳+負巡回畳み込みのほう(xで正負,yで正負なので4倍)
        cs.SetBool("isWeightcalc", true);
        //1
        cs.SetTexture(kernelFFTX_DWT, "Tex_ro", rtex1);
        cs.SetTexture(kernelFFTX_DWT, "Tex", rtex4);
        cs.Dispatch(kernelFFTX_DWT, fftSize, 1, 1);
        //2
        cs.SetTexture(kernelFFTWY_DWT, "Tex_ro", rtex4);
        cs.SetTexture(kernelFFTWY_DWT, "Tex", rtWeightDWT);
        cs.Dispatch(kernelFFTWY_DWT, fftSize / 4 * 6, 1, 1);//FFTY_HADAMARD_IFFTYでのTextureアクセス高速化のため転置している
        cs.SetBool("isWeightcalc", false);

        //intensity_backの計算
        SumBuf.GetData(res);
        intensity_back = 1.0f * res[0];
        for (int i = 1; i < 3; i++)
            intensity_back = Mathf.Max(intensity_back, res[i]);
        if (intensity_back != 0.0f)
        {
            intensity_back = 255.0f / intensity_back;
        }
        else 
        {
            Debug.Log("ConvolutionKernelが真っ黒です");
        }
        
        //Release
        RenderTexture.active = null;//これがないとrtex2の解放でReleasing render texture that is set to be RenderTexture.active!が発生する
        rtex4.Release();
        rtex3.Release();
        rtex2.Release();
        rtex1.Release();
        SumBuf.Release();
    }


    /// srcID画像を入力するとFFT Convolutionを実行した結果がdstIDの画像にはいる
    /// srcIDの画像サイズがfftできるサイズじゃなければsrc_w,src_hを入力すること
    /// 正巡回畳み込みであることに注意(画像の端から端に回り込んでしまう)
    public void FFTConvolutionFromRenderPass(CommandBuffer commandBuffer, int srcID, int dstID, int src_w = -1, int src_h = -1)
    {
        if (rtWeight == null) Awake();
        if (src_w == -1) src_w = descriptor44.width;
        if (src_h == -1) src_h = descriptor44.height;

        commandBuffer.GetTemporaryRT(rt34_i, descriptor34);

        commandBuffer.SetComputeFloatParam(cs, "_intensity", intensity * intensity_back);//intensity_backも乗算することで自動明るさ調整
        commandBuffer.SetComputeIntParam(cs, "width", src_w);
        commandBuffer.SetComputeIntParam(cs, "height", src_h);

        //1
        commandBuffer.SetComputeTextureParam(cs, kernelFFTX, "Tex_ro", srcID);
        commandBuffer.SetComputeTextureParam(cs, kernelFFTX, "Tex", rt34_i);
        commandBuffer.DispatchCompute(cs, kernelFFTX, fftSize, 1, 1);

        //2
        commandBuffer.SetComputeTextureParam(cs, kernelFFTY_HADAMARD_IFFTY, "Tex", rt34_i);
        commandBuffer.SetComputeTextureParam(cs, kernelFFTY_HADAMARD_IFFTY, "Tex_ro", rtWeight_i);
        commandBuffer.DispatchCompute(cs, kernelFFTY_HADAMARD_IFFTY, fftSize / 4 * 3 + 2, 1, 1);

        //3
        commandBuffer.SetComputeTextureParam(cs, kernelIFFTX, "Tex_ro", rt34_i);
        commandBuffer.SetComputeTextureParam(cs, kernelIFFTX, "Tex", dstID);
        commandBuffer.DispatchCompute(cs, kernelIFFTX, fftSize, 1, 1);
        return;
    }

    /// srcID画像を入力するとFFT Convolutionを実行した結果がdstIDの画像にはいる
    /// srcIDの画像サイズがfftできるサイズじゃなければsrc_w,src_hを入力すること
    /// 正巡回+負巡回で回り込んでしまう影響をなくしたver。離散荷重変換DWTを使っているが3.3倍重い
    /// こちら使う子を一応推奨
    public void FFTConvolutionFromRenderPassDWT(CommandBuffer commandBuffer, int srcID, int dstID, int src_w = -1, int src_h = -1)
    {
        if (rtWeight == null) Awake();
        if (src_w == -1) src_w = descriptor44.width;
        if (src_h == -1) src_h = descriptor44.height;

        commandBuffer.GetTemporaryRT(rt64_i, descriptor64);

        commandBuffer.SetComputeFloatParam(cs, "_intensity", intensity * intensity_back);//intensity_backも乗算することで自動明るさ調整
        commandBuffer.SetComputeIntParam(cs, "width", src_w);
        commandBuffer.SetComputeIntParam(cs, "height", src_h);

        //1
        commandBuffer.SetComputeTextureParam(cs, kernelFFTX_DWT, "Tex_ro", srcID);
        commandBuffer.SetComputeTextureParam(cs, kernelFFTX_DWT, "Tex", rt64_i);
        commandBuffer.DispatchCompute(cs, kernelFFTX_DWT, fftSize, 1, 1);

        //2
        commandBuffer.SetComputeTextureParam(cs, kernelFFTY_HADAMARD_IFFTY_DWT, "Tex", rt64_i);
        commandBuffer.SetComputeTextureParam(cs, kernelFFTY_HADAMARD_IFFTY_DWT, "Tex_ro", rtWeightDWT_i);
        commandBuffer.DispatchCompute(cs, kernelFFTY_HADAMARD_IFFTY_DWT, fftSize / 4 * 6, 1, 1);

        //3
        commandBuffer.SetComputeTextureParam(cs, kernelIFFTX_DWT, "Tex_ro", rt64_i);
        commandBuffer.SetComputeTextureParam(cs, kernelIFFTX_DWT, "Tex", dstID);
        commandBuffer.DispatchCompute(cs, kernelIFFTX_DWT, fftSize, 1, 1);

        return;
    }


    private void OnDisable()
    {
        rtWeight.Release();
        rtWeightDWT.Release();
    }
}

