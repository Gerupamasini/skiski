# skiski

**Unity 6.3 LTS + URP（Universal Render Pipeline）** で作るゲームプロジェクトです。

| 項目 | バージョン |
| --- | --- |
| Unity Editor | **6000.3.27f2**（Unity 6.3 LTS） |
| Universal RP | 17.3 |
| Input System | 1.20.1 |

---

## 1. 事前準備（初回のみ）

1. **Unity Hub** をインストール: <https://unity.com/download>
2. Hub でサインインし、ライセンスを有効化する（個人・小規模なら Unity Personal）
   - Hub 左下の歯車 › **Licenses** › **Add license**
3. **Unity Editor 6000.3.27f2** をインストール
   - Hub › **Installs** › **Install Editor** › 「LTS」の **6000.3.x** を選ぶ
   - ⚠ Hub の「Recommended」はより新しい Supported 版（6.6 など）を指していることがあります。このプロジェクトは **6.3 LTS** を使います
   - 追加モジュール（必要なものだけ）
     - Windows: *Microsoft Visual Studio Community*（コードエディタが無い場合）
     - ビルドしたいプラットフォーム: *Windows Build Support (IL2CPP)* / *Mac Build Support* / *Android Build Support* / *iOS Build Support* / *Web Build Support*
4. **コードエディタ**: Visual Studio 2022 / JetBrains Rider / VS Code（+ Unity 拡張機能）のどれか
   - Unity 側で *Edit › Preferences › External Tools › External Script Editor* を選んでおく
5. **Git** と **Git LFS**（画像・モデル・音声などのバイナリを LFS で管理しています）
   - Windows の Git for Windows には Git LFS が同梱されています

## 2. プロジェクトを開く

```bash
git clone https://github.com/Gerupamasini/skiski.git
```

```bash
cd skiski
```

```bash
git lfs install
```

Unity Hub › **Projects** › **Add ▼** › **Add project from disk** › クローンした `skiski` フォルダを選択 › プロジェクトをクリックして開く。

> 初回は `Library/` の生成とパッケージのダウンロードで数分かかります。

## 3. 初回起動時のダイアログ

起動中に次のダイアログが出ます。

1. **Input System**:「…Do you want to enable the backends?」→ **Yes**（エディタが再起動します）
2. **Skiski プロジェクトセットアップ**:「URP がまだ設定されていません…」→ **セットアップする**

セットアップ（`Assets/Scripts/Editor/ProjectSetup.cs`）が自動で行うこと:

| 内容 | 生成・変更されるもの |
| --- | --- |
| URP アセットと Renderer の作成 | `Assets/Settings/URP-Default.asset`, `URP-Default_Renderer.asset` |
| Graphics 設定に URP を割り当て | *Project Settings › Graphics › Default Render Pipeline* |
| カラースペースを Linear に | *Project Settings › Player › Other Settings › Color Space* |
| 動作確認用のサンプルシーン | `Assets/Scenes/SampleScene.unity`（Build の Scene List にも登録） |
| サンプル用マテリアル（URP/Lit） | `Assets/Materials/*.mat` |
| ポストエフェクト（Tonemapping / Bloom） | `Assets/Settings/SampleScene_VolumeProfile.asset` |

「あとで」を選んだ場合や、もう一度実行したい場合はメニュー **Tools › Skiski › Setup Project (URP)** から実行できます（既にあるアセットは上書きしません）。
サンプルシーンだけ作り直したいときは **Tools › Skiski › Recreate Sample Scene**。

## 4. 動作確認

`SampleScene` が開いた状態で ▶ **Play**。

| 操作 | キーボード | ゲームパッド |
| --- | --- | --- |
| 移動 | WASD / 矢印キー | 左スティック |
| ジャンプ | Space | South ボタン（A / ×） |

URP が有効になっているかは *Edit › Project Settings › Graphics* の **Default Render Pipeline** が `URP-Default` になっていることで確認できます。

## 5. 生成されたファイルをコミットする

初回セットアップ後、Unity が生成した設定ファイルをコミットしてください（チーム全員が同じ設定になります）。

```bash
git add -A
```

```bash
git commit -m "Unity の初回セットアップで生成された設定を追加"
```

```bash
git push
```

- コミットするもの: `Assets/`（**`.meta` ファイルも必ず**）、`Packages/manifest.json`、`Packages/packages-lock.json`、`ProjectSettings/`
- コミットしないもの（`.gitignore` 済み）: `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`*.csproj`、`*.sln` など

---

## フォルダ構成

```
Assets/
  Scripts/
    Runtime/    ゲーム本体のスクリプト（PlayerController, CameraFollow）
    Editor/     エディタ拡張（ProjectSetup: 初回セットアップ）
  Scenes/       ← セットアップで生成（SampleScene）
  Settings/     ← セットアップで生成（URP アセット・Volume Profile）
  Materials/    ← セットアップで生成
Packages/
  manifest.json 使用パッケージの一覧
ProjectSettings/
  ProjectVersion.txt  使用する Unity のバージョン
```

## 導入済みパッケージ

| パッケージ | 用途 |
| --- | --- |
| `com.unity.render-pipelines.universal` | URP 本体（Shader Graph などを含む） |
| `com.unity.inputsystem` | 新しい入力システム（キーボード・マウス・ゲームパッド） |
| `com.unity.ugui` | uGUI（Canvas ベースの UI） |
| `com.unity.test-framework` | テスト（Test Runner） |
| `com.unity.ide.visualstudio` / `com.unity.ide.rider` | IDE 連携 |
| `com.unity.modules.*` | 物理・アニメーション・オーディオなどのビルトインモジュール |

追加のパッケージは *Window › Package Manager* から入れられます（例: カメラ制御の **Cinemachine**、2D 用の **2D** 機能セット、ナビゲーションの **AI Navigation**）。

---

## よくあるトラブル

**Safe Mode で起動した / Console に赤いエラーが出る**
スクリプトのコンパイルエラーです。Console のエラー内容を確認してください。Unity のバージョンが 6000.3 より古いと URP 17.3 が使えずエラーになります。

**マテリアルがピンク色になる**
Built-in レンダーパイプライン用のシェーダーを使っているマテリアルです（Asset Store の素材を入れたときによく起きます）。
*Window › Rendering › Render Pipeline Converter* › *Built-in to URP* › *Material Upgrade* で URP 用に変換できます。

**別バージョンの Unity で開いたら警告が出た**
6000.3.x の別パッチ（6000.3.28f1 など）であればそのまま開いて問題ありません。開くと `ProjectSettings/ProjectVersion.txt` が更新されるので、バージョンを上げる場合はチームで揃えてからコミットしてください。

**2D ゲームにしたい**
1. *Window › Package Manager* で **2D** 機能セットを追加
2. Project ウィンドウで *Create › Rendering › URP Asset (with 2D Renderer)*
3. 作ったアセットを *Project Settings › Graphics › Default Render Pipeline* に設定

## （任意）Unity のシーン・プレハブのマージ設定

`.gitattributes` で Unity の YAML ファイルに `merge=unityyamlmerge` を指定しています。Unity 付属の **UnityYAMLMerge** を Git に登録すると、シーンやプレハブのコンフリクトを自動で解決しやすくなります（未設定でも通常のテキストマージになるだけで問題はありません）。

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
```

```bash
git config merge.unityyamlmerge.driver "'<UnityYAMLMerge のパス>' merge -h -p --force %O %B %A %A"
```

```bash
git config merge.unityyamlmerge.recursive binary
```

`<UnityYAMLMerge のパス>` の例:

- Windows: `C:/Program Files/Unity/Hub/Editor/6000.3.27f2/Editor/Data/Tools/UnityYAMLMerge.exe`
- macOS: `/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app/Contents/Tools/UnityYAMLMerge`

## 参考リンク

- [Unity Learn](https://learn.unity.com/)（公式チュートリアル）
- [Universal Render Pipeline マニュアル](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/urp-introduction.html)
- [Input System マニュアル](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html)
