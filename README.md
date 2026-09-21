# ポート番

Windows で「どのポートが誰に占領されているか」を見るための軽量アプリです。待ち受け中の TCP / UDP だけを一覧し、用途の目安を出し、そのプロセスを終了できます。閉じるか最小化すると通知領域に残ります。

## できること

- 待ち受けポート、プロトコル、アドレス（全公開かローカルのみか）、PID、プロセス名、用途を表示する
- ポート番号・プロセス名・用途で絞り込む
- 3 秒ごとに自動更新する。更新ボタンでも取り直せる
- 行の「終了」で、そのポートを使っているプロセスを子プロセスごと終了する
- 閉じる・最小化でトレイに常駐する。終了はトレイメニューの「終了」だけ

確立済みの接続は出しません。見たいのはポートを掴んでいるプロセスだからです。

用途は次の順で推定します。

1. コマンドライン（同じ `node.exe` でも Next.js と Vite を分ける）
2. プロセス名（Ollama、PostgreSQL など）
3. よく使うポート番号（Ollama 11434、ComfyUI 8188、Gradio 7860、Jupyter、Vite など）
4. 実行ファイルの説明

推定できないときは「用途不明」と出します。

## トレイ

- ウィンドウを閉じる、または最小化すると通知領域に残る
- アイコンをクリックするか、右クリックの「表示」で戻る
- アプリを終わらせるときは右クリックの「終了」

## プロセスの終了

「終了」はポートだけを閉じるのではなく、そのプロセスを終了します。保存していない作業は失われます。確認ダイアログを出してから実行します。

システムプロセスや、権限のないプロセスは終了できません。権限が足りないときは「管理者として起動し直してください」と出します。ポート番が勝手に管理者へ昇格することはありません。

## 開発しながら動かす

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) が必要です。

```bash
dotnet run
dotnet test
```

## Windows 向けの単一 exe

ランタイムのインストールは不要です。次のコマンドで `PortBan.exe` を作ります。

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

出力先は `PortBan.exe` だけです。

```text
bin/Release/net8.0/win-x64/publish/PortBan.exe
```

Windows では待ち受け一覧を IP Helper（`GetExtendedTcpTable` / `GetExtendedUdpTable`）で取り、失敗したときだけ `netstat -ano` に戻します。

## Linux で画面を確認する場合

このリポジトリには、開発用に `/proc/net` から待ち受けポートを読む実装も入っています。プロセス終了も、権限があれば動きます。配布対象は Windows です。
