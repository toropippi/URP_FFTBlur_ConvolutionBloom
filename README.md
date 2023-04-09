# URP_FFTBlur_ConvolutionBloom 
 Blur and convolution bloom implementation using 2DFFT  
 このリポジトリは2D FFTを用いたブラーとConvolution Bloomの実装になります。  
 
# 使い方 How to use 
 ユーザーがいじれるところ主に2ヶ所です。  
## 1.Hierarchyタブにある「FFTObject」  
 ![setumei1](https://user-images.githubusercontent.com/44022497/230786923-027d994a-c45b-46ad-95b3-54b60a882aff.png)  
 
### Convolution Kernel  
 畳み込みの重みの画像を入れるところです。ブラー用途ならガウス分布の画像を入れます。  
![g3](https://user-images.githubusercontent.com/44022497/230787380-14885ebc-4339-478b-a582-e7889fdde548.png)  
 画像サイズはFFTサイズに関係なく特に指定ありません。計算時にFFTサイズに拡縮されます。  
 画像の中心が基準点となるように用意して下さい。例えば512×512サイズの画像なら(256,256)の位置が自分のピクセルの重みとして畳み込み計算されます。  

### Use256x4  
 Convolution Kernelの1枚を使うか、ConvolutionKernel_256x4a～dの4枚を使うか指定できます。  
 主にブラー用途ではチェックなし、Convolution Bloom用途ではチェックして下さい。  
 Convolution Bloomを行なう場合、Kernelにはかなりのダイナミックレンジが要求されます。例えば以下のKernelを使いたいとします。  
![6](https://user-images.githubusercontent.com/44022497/230788304-93bda2eb-9d58-468b-95dc-9a593b132c5b.jpg)  
 このとき画像の中心の一番明るい部分と画面端の黒い部分は本来1万倍～100万倍近く違ってないといけません。この画像では真ん中が白飛びして、端のほうは黒飛びしてしまっています。  
 普通のpng画像はせいぜいRGBAそれぞれ8bitなのでレンジが狭くこのままでは使えません。そこでa～dの4枚用意し、a×1677216+b×65536+c×256+dを本当の色として計算を行ないます。  
 今回サンプルを7つほど用意しているのでそれをお使い下さい。  
 また自前で色深度が十分なpng画像を用意できるならUse256x4のチェックはせずにConvolution Kernelを使うこともできます。  

### Intensity
 畳み込み画像に適応される色倍率です。  
 ブラー用途では1.0、Convolution Bloom用途では100.0くらいが適切な値です。  

## 2.SettingsのUniversalRendererにアタッチしてあるFeature  
 <img width="460" alt="setumei2.png" src="https://user-images.githubusercontent.com/44022497/230786919-4ac6aabd-bdba-4df5-b4fc-e948c3e7cf42.png">
 
### Selected Pass
 3つのモードが選択できます。  
 ![説明5](https://user-images.githubusercontent.com/44022497/230788533-2077b6c8-b43e-4657-a750-9b1c9c36fb64.png)  
 
### BorderRatio
 畳み込み前に画像に余白を持たせて、畳み込み後にその余白部分を使わない設定です。  
 ![巡回](https://user-images.githubusercontent.com/44022497/230788606-912f025e-3f8f-42e9-87f8-37d066d5f3da.jpg)  
 これはBorderRatio=0.0で強いブラーをかけた結果になります。本来右にある太陽ですが、右端と左端がつながっているため画面左にも光が漏れてしまっています。BorderRatioを適切な値に増やすことで漏れ部分を隠すことができます。  

### Threshold
 画面の明るい部分の閾値。  
 Convolution Bloomでのみ使われます。閾値を超えるピクセルにBloomがかかります。  
 
# 3つのモード

## Gauss Blur Render Pass
 単純にブラーを行なうものです。  
 BorderRatioは無視されます。  
 Convolution Kernelにガウス分布の重み画像をセットすることで画面全体にガウスブラーをかけることができます。Convolution Kernel次第でいろんなフィルターをかけることができます。  
 
## Gauss Blur Border Render Pass 
 BorderRatioを考慮したGauss Blur Render Passです。

## Bloom Render Pass
 Convolution Bloomを行なうPassです。  
  
 Unreal-Engineの公式ドキュメントも参照下さい。  
 https://docs.unrealengine.com/5.1/en-US/bloom-in-unreal-engine/  
 https://docs.unrealengine.com/4.27/ja/RenderingAndGraphics/PostProcessEffects/Bloom/  
  
 kernel画像を用意することろが地味に難関ですが、用意できればポストエフェクトの幅が広がります。  
### 例1
 kernel画像と実行結果  
 ![samp5](https://user-images.githubusercontent.com/44022497/230789193-7af7a9c7-92d6-4818-95e0-084f4b6114ee.jpg)
 ![説明8](https://user-images.githubusercontent.com/44022497/230789234-a674cb95-186d-40c6-91ed-b258e7a1c950.gif)  
### 例2 
 kernel画像と実行結果  
 ![samp4](https://user-images.githubusercontent.com/44022497/230789262-ab2d983c-e36c-4115-8b72-362ab7f4928d.jpg)
 ![bloom2](https://user-images.githubusercontent.com/44022497/230789269-b856ad1b-a8bd-4fdc-a105-85b0f3ef065b.jpg)  
### 例3 
 kernel画像と実行結果  
 ![samp1](https://user-images.githubusercontent.com/44022497/230789287-1ceba284-a26a-4083-8145-b2ad88a24465.jpg)
 ![bloom1](https://user-images.githubusercontent.com/44022497/230789289-8824235d-6fde-449a-b016-23ee3781892a.jpg)  
 
# TODOリスト
・FFTサイズは512×512固定だがインスペクターから可変にできるようにする  
・実行中にConvolution Kernelを変更したら自動で重みを再計算するようにする  
・Convolution Kernelのスケールを設定できるようにする  
・Convolution Kernelの中心ピボットを設定できるようにする  
・今はBorderRatioで余白を作って対処しているところを、負巡回畳み込み乗算を使って解決する。計算量は2倍になる(多分)がBorderRatioは0にできるはず  
・Kernel画像の自動生成プログラムもそのうち公開したい  

# その他
・FFTサイズの変更はソースコード内を直接いじって下さい。C#側とCompute Shader側にそれぞれあります。  
・Assets/FFTConvolutionBloom/ConvolutionTexture以下にある全画像ファイルは自分が生成したものですが、商用利用OK、 改変OK、クレジット表記不要の「CC0」ライセンスとします。  


