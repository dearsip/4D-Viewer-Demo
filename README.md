# 4D Viewer Demo

![4D Viewer Demo](https://gyazo.com/84cf5137b8543312921756f57a6d8cb2.jpg)
![4D Viewer Demo](https://gyazo.com/bd58350e133aae05051f0b068b47e402.jpg)

陰面消去（陰胞消去）を搭載した多胞体表示ソフト。[→動画](https://www.dropbox.com/s/pkbq28rvxaob5k6/2019-02-26-2326-06_Clip.mp4?dl=0)

[4D Blocks](http://www.urticator.net/blocks/v6/index.html)のソースコードを流用している。
また、実装には[4dforvive](https://github.com/leo92613/4dforvive)を一部参考にしている。

流用している部分はコメントもそのまま載せている。追加、変更した箇所については日本語でコメントを書いている。

## 操作方法
トリガー: 押している間、図形を回転させる。

グリップボタン: 押している間、ディスプレイの位置を動かす。

また、`Assets/Scripts/ThreeDDisplay.cs`内のコンストラクタの`shapes`の定義を変更することで、異なる多胞体を表示できる。

## 更新予定
- VRChatワールド化
- 表示できる多胞体の追加
- より簡単な多胞体定義
