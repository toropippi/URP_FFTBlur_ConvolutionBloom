# URP_FFTBlur_ConvolutionBloom 
 Blur and convolution bloom implementation using 2DFFT  
 このリポジトリは2D FFTを用いたブラーとConvolution Bloomの実装になります。  
 
# 使い方 How to use 
 ユーザーがいじれるところ主に2つです。  
 1.Hierarchyタブにある「FFTObject」  
 ![setumei1](https://user-images.githubusercontent.com/44022497/230786923-027d994a-c45b-46ad-95b3-54b60a882aff.png)  
 2.SettingsのUniversalRendererにアタッチしてあるFeature  
 <img width="300" alt="setumei2.png" src="https://user-images.githubusercontent.com/44022497/230786919-4ac6aabd-bdba-4df5-b4fc-e948c3e7cf42.png">
 
 
 
 
 
 そして2のSelected Passのところで下記の3つのモードが選択できます。  
 
## Gauss Blur Render Pass
 単純にガウスブラーを行なうものです。  
 Convolution Kernelにガウス分布の画像をセットすることで画面全体にブラーをかけることができます。  
 