# Avatar Parameters Saver

Play時の現在のパラメーターをParameter Driverとして保存出来るツールです。

## インストール

### VCCによる方法

1. https://vpm.narazaka.net/ から「Add to VCC」ボタンを押してリポジトリをVCCにインストールします。
2. VCCでSettings→Packages→Installed Repositoriesの一覧中で「Narazaka VPM Listing」にチェックが付いていることを確認します。
3. アバタープロジェクトの「Manage Project」から「Avatar Parameters Saver」をインストールします。

## 使い方

1. 「Tools」→「Avatar Parameters Saver」をクリックし、ツールを立ち上げます。

2. アバターを選択し、アバター以下の制御したいパラメーターを選択します。

3. 「保存」ボタンを押して保存場所を選択すれば、Modular AvatarのPrefabが出来上がります。

4. Prefabをアバターの中（直下でなくても良い）に置けば、Modular Avatarによってメニューが統合されます。

## 更新履歴

- 3.0.0-beta.2
  - IEditorOnly
- 3.0.0-beta.1
  - データはコンポーネントに保持するように
    - NDMF利用の事後生成に
    - Play中に変更したデータをいちいちprefabにしなくてもそのままアバター内のコンポーネントに書き出されるように
    - 以前のアセットは新しいコンポーネントに変換可能（そのままでは読み込めません）
    - 子メニューのインストール先を変えたい場合は同名の子オブジェクトにMA Menu Installerを付ければ可能
  - パラメーター名を指定せずとも自動でコンポーネントのオブジェクト名になるように
  - アイコンの指定が出来るように
- 2.1.4
  - asmdef修正
- 2.1.3
  - floatパラメーター変更が反映されなかった問題を修正
- 2.1.2
  - パラメーター変更にすぐに追従するように
- 2.1.1
  - VCCバグへの対応
- 2.1.0
  - インデックスオフセット機能追加
- 2.0.2
  - バグ修正
- 2.0.1
  - バグ修正
- 2.0.0
  - Intのみで複数切り換え出来るようなメニュー生成
- 1.1.0
  - UI便利に
- 1.0.0
  - リリース

## License

[Zlib License](LICENSE.txt)
