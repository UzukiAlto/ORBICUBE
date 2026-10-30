# ORBICUBE

3D空間で視点を回転させながらパドル（パネル）を操作してブロックを崩す、3Dブロック崩しゲームです。

---

## ゲーム概要

- **タイトル**: ORBICUBE
- **ジャンル**: 3Dブロック崩しアクション
- **対応環境**: PC（Windows）
- **GitHubリポジトリ**: [https://github.com/UzukiAlto/ORBICUBE](https://github.com/UzukiAlto/ORBICUBE)
- **特徴**:
  - 3D空間内のキューブ型ステージを自由な角度から見回し、操作する面を切り替えながら戦う立体アクション
  - ボールの軌道予測表示による立体的な空間把握のサポート
  - 難易度選択および感度・音量等のコンフィグ設定機能

---

## 操作方法

- **パドル移動**: `W` `A` `S` `D` キー
- **カメラ回転（視点移動）**: マウス左ドラッグ
- **操作面の変更**: カメラを操作したい方向（面）に向けて `Space` キー
- **メニュー画面の決定**: 選択したい項目にカメラを向けて `Space` キー
- **ポーズ / メニュー**: `Esc` キー または 画面右上のメニューキューブをクリック

---

## 開発環境・使用技術

- **ゲームエンジン**: Unity `2022.3.62f2` (LTS)
- **言語**: C#
- **主要パッケージ・ライブラリ**:
  - Unity Input System (`1.14.2`)
  - TextMesh Pro (`3.0.7`)
  - DOTween (Demigiant)

---

## ディレクトリ構成（`Assets/Script`）

- `MainSystem/`: 画面遷移管理、オーディオ管理、入力管理（Input System）、基底クラス群
- `Title/`: タイトル画面の演出および進行制御
- `ModeSelect/`: モード選択・難易度選択・コンフィグ設定画面の制御
- `Game/`: インゲームの進行管理、パドル・カメラ制御、ボール・ブロック制御、ポーズUI管理

---

## クレジット・使用素材

- **BGM**: [Blue Sun](https://opentracks.com/bgm/detail/5920)
- **SE**: [効果音ラボ](https://soundeffect-lab.info/)
- **フォント**: GLT-CivilizationSans
- **スカイボックス**: 3 Skyboxes (Unity Asset Store ※現在配布終了)
- **アイコン画像**: [Freepik](https://www.freepik.com/)
- **アニメーションライブラリ**: DOTween (HOTween v2)
