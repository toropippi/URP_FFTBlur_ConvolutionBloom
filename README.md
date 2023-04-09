# URP_FFTBlur_ConvolutionBloom 
 Blur and convolution bloom implementation using 2DFFT  
 このリポジトリは2D FFTを用いたブラーとConvolution Bloomの実装になります。  
 
# 使い方 How to use 
 ユーザーがいじれるところ主に2つです。  
## 1.Hierarchyタブにある「FFTObject」  
 ![setumei1](https://user-images.githubusercontent.com/44022497/230786923-027d994a-c45b-46ad-95b3-54b60a882aff.png)  
 
・Convolution Kernel  
畳み込みの重みの画像を入れるところです。ブラー用途ならガウス分布の画像を入れます。  
![g3](https://user-images.githubusercontent.com/44022497/230787380-14885ebc-4339-478b-a582-e7889fdde548.png)  
画像サイズはFFTサイズに関係なく特に指定ありません。  
画像の中心が基準点となるように用意して下さい。例えば512*512サイズの画像なら(256,256)の位置が自分のピクセルの重みとして畳み込み計算されます。  
 
## 2.SettingsのUniversalRendererにアタッチしてあるFeature  
 <img width="460" alt="setumei2.png" src="https://user-images.githubusercontent.com/44022497/230786919-4ac6aabd-bdba-4df5-b4fc-e948c3e7cf42.png">
 
 
 
 
 
 そして2のSelected Passのところで下記の3つのモードが選択できます。  
 
## Gauss Blur Render Pass
 単純にガウスブラーを行なうものです。  
 Convolution Kernelにガウス分布の画像をセットすることで画面全体にブラーをかけることができます。  

## TODOリスト
・今はFFTサイズが512*512固定だが可変にできるようにする  
・実行中にConvolution Kernelを変更したら自動で重みを再計算するようにする  
・Convolution Kernelのスケールを設定できるようにする  
・Convolution Kernelの中心ピボットを設定できるようにする  

## その他
・FFTサイズの変更はソースコード内を直接いじって下さい。C#側とCompute Shader側にそれぞれあります。  