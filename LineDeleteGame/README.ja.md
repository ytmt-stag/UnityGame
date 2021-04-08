# LineDeleteGame

<p align="center">
    <img src="img/teaser.png" width="70%" height="70%">
</p>

**[English](README.md)**

## 概要

リアルタイム通信の勉強用に作ったゲームです。
このREADMEでは各人のPCでの環境構築方法について解説します。

## 開発環境

### OS

* 割とどこでも
  * Unity部分はWindows, Macで動作確認してます。
  * .Net Coreのサーバー部分はWindows, Mac, そして AWS EC2サーバーで確認しました。
* エディタにはVisualStudioとVSCodeを使用しています。

### 各人で準備が必要なもの

* Unity / 2020.2.2f1
* Docker and Docker compose

### このリポジトリに含んでいるもの

* MagicOnion / 4.1.2
* MessagePack.Unity / 2.1.12
* UniTask / 2.1.2
* LogicLooper / 1.0.2

## Setup方法

### リポジトリのクローン

```console
# clone this repostory
$ git clone git@github.com:y-tomita/UnityGames.git

# move to LineDeleteGame directory
$ cd LineDeleteGame
```

### gRPCライブラリを公式ページからダウンロード

[gRPC packages ページ](https://packages.grpc.io/archive/2019/08/e14154481baf7e23710b60b78a1e91299985ec80-41fb322e-6416-4509-9a6f-2b17085b6272/index.xml)
から"grpc_unity_package.2.23.0-dev.zip"をダウンロード、解凍してください。
次に下記フォルダを "LineDeleteGame/Assets/Plugins" ディレクトリにコピーしてください。

* Google.Protobuf
* Grpc.Core
* Grpc.Core.Api

## オフラインモードとしてUnityで実行

Unity 2020.2.2f1でLineDeleteGameプロジェクトを開いてください。
次にUnityEditor上で "Assets/Scenes/BlockGameMain.unity" シーンへ移動後、再生ボタンを押してください。
あとはタイトル画面で上矢印を押せばオフラインモードでゲームが遊べます。

## localhostとして".Net Core"をVisual Studio上で実行

".Net Core" ホスティング用プロジェクトはUnityプロジェクト用のソリューションファイルに統合して実行可能です。

まず、UnityからC#ファイルを開いてVisualStudioを起動してください。次にソリューションを右クリックし、下記のcsprojをソリューションズに追加してください。

* "LineDeleteGame/App.Server/App.Server.csproj"
* "LineDeleteGame/App.Shared/App.Shared.csproj"

そして、ソリューション内の"App.Server"プロジェクトを右クリックし「スタートアッププロジェクトに設定」を選択すると、VisualStudio上でサーバーをlocalhostとして実行できます。

UnityEditor上の実行からこのlocalhostサーバーへ接続可能です。
"LineDeleteGame/Assets/Scripts/App/Server/Shared/Common/SharedConstant.cs"ファイルの"GRPC_CONNECT_ADDRESS"変数を"http://localhost:5001"、もしくは自身のPCのローカルIPアドレスに変更してください。

## Dockerコンテナ上で ".Net Core" ホスティングサーバーを起動する

Dockerコンテナでも起動可能です。

### Docker / Docker-composeをインストール

動作予定のPCにDockerとDocker-composeをインストールしてください。

### Certファイルの生成

Docker上で動作させるには専用のCertファイルを作成する必要があります。
[Generate self-signed certificates with the .NET CLI](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-openssl) documentを参照の上、動作PC上にCertファイルを作成してください。

### ".env"ファイルを作成


"LineDeleteGame/sample.env"を"LineDeleteGame/.env"としてコピーの上作成してください。
そして.env内の変数を作成したCertファイルのパスとパスワードに変更してください。

```console
CERT_PATH=your_cert_path
CERT_PASSWORD=your_cert_password
```

### コンテナ起動

docker-composeファイルを用意しているので下記docker-composeコマンドを実行すれば ".Net Core"ホスティングサーバーをコンテナ上で実行可能です。

```console
$ cd UnityGames/LineDeleteGame

# build container
$ docker-compose build

# launch container
$ docker-compose up -d
```

ここまで実行できれば動作PCのIPアドレスで疎通可能です。