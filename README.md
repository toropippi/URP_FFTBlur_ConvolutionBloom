# URP_FFTBlur_ConvolutionBloom 
 Blur and convolution bloom implementation using 2DFFT  
 このリポジトリは2D FFTを用いたブラーとConvolution Bloomの実装になります。  
 
# 使い方 How to use 
 ユーザーがいじれるところ主に2つです。  
 1.Hierarchyタブにある「FFTObject」  
 
 2.SettingsのUniversalRendererにアタッチしてあるFeature  
 
 そして2のSelected Passのところで下記の3つのモードが選択できます。  
 
## Gauss Blur Render Pass
 単純にガウスブラーを行なうものです。  
 Convolution Kernelにガウス分布の画像をセットすることで画面全体にブラーをかけることができます。  
 