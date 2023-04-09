using UnityEngine;
using UnityEngine.Rendering;

public class FFTBloom : MonoBehaviour
{
    //Bloom�ݒ����
    const int fftSize = 1024;
    private RenderTextureFormat bloomTexFormat = RenderTextureFormat.ARGBFloat;//or ARGBHalf

    //compute shader����
    [SerializeField] private ComputeShader cs;
    [SerializeField] private Texture2D convolutionKernel;
    [SerializeField] private float intensity;
    [SerializeField] private FFTBloomRenderFeature _feature;//feature�����炱����FFT�֐����Ă΂��

    private float intensity_back;//convolutionKernel�Ɏw�肵���摜�����邭�Ă��Â��Ă�1�ɂȂ�悤���K������Ӗ�������
    private int kernelFFTX, kernelIFFTX;
    private int kernelFFTY_HADAMARD_IFFTY;
    private int kernelFFTWY, kernelCopySlide;

    private RenderTexture rtWeight = null;//convolutionKernel��FFT�v�Z��̏d�݂�����

    //PropertyToID�֘A
    private int rt34_i;
    private RenderTargetIdentifier rtWeight_i;

    //Descriptor�֘A
    private RenderTextureDescriptor descriptor34, descriptor44, descriptor43, out_descriptor;//RGBA=4,RGB=3
    public RenderTextureDescriptor Descriptor => out_descriptor;//�o�͂̉摜�̑傫��


    private void Awake()
    {
        //RenderFeature���炱���̊֐����Ăяo����悤��
        _feature.SetFFT(this.GetComponent<FFTBloom>());

        //descriptor�Z�b�g
        DescriptorInit();
        ShaderIDInit();

        //kernel�Z�b�g
        kernelFFTX = cs.FindKernel("FFTX");
        kernelIFFTX = cs.FindKernel("IFFTX");
        kernelFFTY_HADAMARD_IFFTY = cs.FindKernel("FFTY_HADAMARD_IFFTY");
        kernelFFTWY = cs.FindKernel("FFTWY");
        kernelCopySlide = cs.FindKernel("CopySlide");

        //RenderTexture�֘A
        rtWeight = new RenderTexture(descriptor43);
        rtWeight.Create();
        rtWeight_i = new RenderTargetIdentifier(rtWeight);

        //convolutionKernel����fft���Weight���v�Z
        CreateWeight();
    }

    void DescriptorInit() 
    {
        descriptor34 = new RenderTextureDescriptor(fftSize / 4 * 3 + 2, fftSize, bloomTexFormat, 0);
        descriptor34.enableRandomWrite = true;
        descriptor43 = new RenderTextureDescriptor(fftSize, fftSize / 4 * 3 + 2, bloomTexFormat, 0);
        descriptor43.enableRandomWrite = true;
        descriptor44 = new RenderTextureDescriptor(fftSize, fftSize, bloomTexFormat, 0);
        descriptor44.enableRandomWrite = true;
        out_descriptor = new RenderTextureDescriptor(fftSize, fftSize, bloomTexFormat, 0);
        out_descriptor.enableRandomWrite = true;
    }
    void ShaderIDInit()
    {
        rt34_i = Shader.PropertyToID("_rt34");
    }

    /// Convolution kernel�̎��OFFT�v�Z
    /// ���ł�intensity_back�̌v�Z��
    private void CreateWeight()
    {
        RenderTexture rtex1 = new RenderTexture(descriptor44);
        rtex1.Create();
        RenderTexture rtex2 = new RenderTexture(descriptor44);
        rtex2.Create();
        RenderTexture rtex3 = new RenderTexture(descriptor34);
        rtex3.Create();

        uint[] res = new uint[4];
        for (int i = 0; i < 4; i++) res[i] = 0;
        ComputeBuffer SumBuf = new ComputeBuffer(4, 4);//R,G,B���ꂼ��ɂ�����S��ʂ̒l�̑��v�Bintensity_back�̌v�Z�̂���
        SumBuf.SetData(res);

        //Convolution kernel�ǂݍ��݁�rtex2
        Graphics.Blit(convolutionKernel, rtex2);//ComputeShader��RenderTexture���������Ȃ��̂�

        //�e�N�X�`���̒��S��0,0�Ɉړ��B���ł�SumBuf�ɑS��f�l���v����
        cs.SetBuffer(kernelCopySlide, "SumBuf", SumBuf);
        cs.SetTexture(kernelCopySlide, "Tex_ro", rtex2);
        cs.SetTexture(kernelCopySlide, "Tex", rtex1);
        cs.SetInt("width", fftSize);
        cs.SetInt("height", fftSize);
        cs.Dispatch(kernelCopySlide, fftSize / 8, fftSize / 8, 1);

        //1
        cs.SetTexture(kernelFFTX, "Tex_ro", rtex1);
        cs.SetTexture(kernelFFTX, "Tex", rtex3);
        cs.Dispatch(kernelFFTX, fftSize, 1, 1);
        //2
        cs.SetTexture(kernelFFTWY, "Tex_ro", rtex3);
        cs.SetTexture(kernelFFTWY, "Tex", rtWeight);
        cs.Dispatch(kernelFFTWY, fftSize / 4 * 3 + 2, 1, 1);//FFTY_HADAMARD_IFFTY�ł�Texture�A�N�Z�X�������̂��ߓ]�u���Ă���

        //intensity_back�̌v�Z
        SumBuf.GetData(res);
        intensity_back = 1.0f * res[0];
        for (int i = 1; i < 3; i++)
            intensity_back = Mathf.Max(intensity_back, res[i]);
        if (intensity_back != 0.0f)
            intensity_back = 255.0f / intensity_back;

        //Release
        RenderTexture.active = null;//���ꂪ�Ȃ���rtex2�̉����Releasing render texture that is set to be RenderTexture.active!����������
        rtex3.Release();
        rtex2.Release();
        rtex1.Release();
        SumBuf.Release();
    }



    /// RenderTexture����͂����FFT Convolution�����s�������ʂ�RenderTexture�ŕԂ�
    public RenderTexture FFTConvolution(RenderTexture source)
    {
        RenderTexture rtex1 = RenderTexture.GetTemporary(descriptor34);
        rtex1.Create();
        RenderTexture returnRT = RenderTexture.GetTemporary(descriptor44);
        returnRT.Create();

        cs.SetFloat("_intensity", intensity * intensity_back);//weight���v�Z�����Ƃ���intensity_back����Z
        cs.SetInt("width", source.width);
        cs.SetInt("height", source.height);

        //1
        cs.SetTexture(kernelFFTX, "Tex_ro", source);
        cs.SetTexture(kernelFFTX, "Tex", rtex1);
        cs.Dispatch(kernelFFTX, fftSize, 1, 1);

        //2
        cs.SetTexture(kernelFFTY_HADAMARD_IFFTY, "Tex", rtex1);
        cs.SetTexture(kernelFFTY_HADAMARD_IFFTY, "Tex_ro", rtWeight);
        cs.Dispatch(kernelFFTY_HADAMARD_IFFTY, fftSize / 4 * 3 + 2, 1, 1);

        //3
        cs.SetTexture(kernelIFFTX, "Tex_ro", rtex1);
        cs.SetTexture(kernelIFFTX, "Tex", returnRT);
        cs.Dispatch(kernelIFFTX, fftSize, 1, 1);

        RenderTexture.ReleaseTemporary(rtex1);
        return returnRT;
    }



    /// srcID�摜����͂����FFT Convolution�����s�������ʂ�dstID�̉摜�ɂ͂���
    /// srcID�̉摜�T�C�Y��fft�ł���T�C�Y����Ȃ����src_w,src_h����͂��邱��
    public void FFTConvolutionFromRenderPass(CommandBuffer commandBuffer, int srcID, int dstID, int src_w = -1, int src_h = -1)
    {
        if (rtWeight == null) Awake();
        if (src_w == -1) src_w = descriptor44.width;
        if (src_h == -1) src_h = descriptor44.height;

        commandBuffer.GetTemporaryRT(rt34_i, descriptor34);

        commandBuffer.SetComputeFloatParam(cs, "_intensity", intensity * intensity_back);//intensity_back����Z���邱�ƂŎ������邳����
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




    private void OnDisable()
    {
        rtWeight.Release();
    }

}

