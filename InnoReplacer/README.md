# InnoReplacer

Encoding-safe text replacement utility for installers and automation scripts.  
Maintains original file encoding (UTF-8/UTF-16/UTF-32/Shift-JIS etc.) while replacing placeholders.

[![Version](https://img.shields.io/badge/version-1.1.0.0-blue.svg)](#)

---

## 🛈 Project Move (Repository Migration)

This project was **migrated from**:

- Old path: https://github.com/mono-tec/ConsistRunner/tree/master/Tools/InnoReplacer  
- New home (this repo): https://github.com/mono-tec/InnoReplacer/tree/master/InnoReplacer

**Why move?**
- ツール単体での管理・配布性を高めるため  
- テンプレートや他プロジェクトからの再利用を容易にするため  
- リリースや CI をツール単位で運用するため

> 旧パス側はメンテ終了（Archive予定）とし、今後のIssue/PRは本リポジトリで受け付けます。

---

# InnoReplacer v1.1.0.0

Inno Setup などから呼び出して、ファイル内の文字列を安全に置換するためのツールです。  
UTF 系（UTF-8 / UTF-16 / UTF-32, LE/BE）や Shift-JIS などのエンコーディングを保持したまま置換できます。

---

## 🧩 主な変更点（v1.1.0.0）

- **文字コードを明示指定可能に**（UTF-8, UTF-16, UTF-32, Shift-JISなど）  
- **BOM の有無を制御可能に**（`utf8` / `utf8bom` など）  
- **UTF-32 / BigEndian 系エンコーディング** の完全サポート  
- **テキスト先頭の BOM 文字（U+FEFF）を自動除去**（無BOM出力指定時のみ）  
- **単体テストを強化**（全エンコード + BOM 組み合わせを網羅）

---

## 🚀 使い方

### 単一置換（Inno Setup などから呼び出し）

```powershell
InnoReplacer.exe <filePath> <searchText> <replaceText> [encodingOption]
```

例:

```powershell
InnoReplacer.exe "C:\Temp\test.sql" "#PGDATABASE#" "appdb_test" utf8
```

### 複数置換（C#コードから利用）

```csharp
var svc = new FileTextReplaceService();
svc.ReplaceInPlace("TaskTemplate.xml", new [] {
    new KeyValuePair<string,string>("__EXE_PATH__", @"C:\Program Files\App\App.exe"),
    new KeyValuePair<string,string>("__ARGS__", "--ping")
});
```

または、エンコーディング指定付き：

```csharp
var svc = new FileTextReplaceServiceWithEncoding(new UTF8Encoding(false), emitBom: false);
svc.ReplaceInPlace("config.ini", "__TOKEN__", "abcd1234");
```

---

## ⚙️ サポートするエンコーディング指定子

| 指定名 | 文字コード | BOM出力 | 備考 |
|--------|-------------|---------|------|
| `utf8` | UTF-8 | なし | 推奨。Linux/DB向け |
| `utf8bom` | UTF-8 | あり | Windows向けINIなど |
| `sjis` | Shift_JIS (CP932) | - | 非UTF系 |
| `utf16le` | UTF-16 Little Endian | なし | |
| `utf16le-bom` | UTF-16 Little Endian | あり | 既定値 |
| `utf16be` | UTF-16 Big Endian | なし | |
| `utf16be-bom` | UTF-16 Big Endian | あり | |
| `utf32le` | UTF-32 Little Endian | なし | |
| `utf32le-bom` | UTF-32 Little Endian | あり | |
| `utf32be` | UTF-32 Big Endian | なし | |
| `utf32be-bom` | UTF-32 Big Endian | あり | |

---

## 🔍 BOM と不可視文字の扱い

- 入力が BOM 付きでも、出力で「無BOM」を指定した場合は、先頭の **`U+FEFF`（不可視BOM文字）を除去** します。  
  → SQL や BAT ファイルの先頭での実行エラーを防止するためです。
- 同じBOM方針（例: UTF-8→UTF-8BOM）では、入力内容は変更しません。

---

## 📘 注意事項

- InnoReplacer は **エンコーディングを保持したまま安全に置換**するためのツールです。  
  OS・DB・スクリプトなどで BOM が原因の不具合を防ぐことを目的としています。  
- `UTF-8(無BOM)` が推奨設定です（PostgreSQL・PowerShell・bash 互換性が最も高い）。
- このツールは **自己責任で使用**してください。作者は動作保証および損害への責任を負いません。

---

## 🧪 開発メモ

- 対応.NET: **.NET Framework 4.7.2**  
- テスト: **MSTest (Visual Studio)**  
- カバレッジ: UTF-8 / UTF-16 / UTF-32 (LE/BE) + Shift-JIS  
- BOM動作検証済み（全9通り × BOMあり/なし）

---

## License
MIT © 2025 mono-tec
