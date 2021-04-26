# ae.exe

opensslコマンド互換のファイル暗号ソフト。  
LinuxとWindows間で暗号化ファイルを相互にやり取り可能です。  

## 使い方

.NET Framework 4.6以降が必要。（Windows10は標準でインストール済）

```
ae.exe [options] <input file>

options
  -h,--help               ヘルプを表示
  -v,--version            バージョンを表示
  -e,--encrypt            暗号化する(デフォルト)
  -d,--decrypt            復号する
  -k,--password <string>  パスワードを指定
  -o,--out <string>       出力ファイル名を指定
  --aes-256-cbc           keysize=256,mode=cbcのaesを利用
  --aes-192-cbc           keysize=192,mode=cbcのaesを利用
  --aes-128-cbc           keysize=128,mode=cbcのaesを利用
```

デフォルトの動作は `-e --aes-256-cbc` です。また `-o` `--out` で出力ファイル名を指定しなかった場合は出力ファイル名を以下のルールで付与します。

- 暗号化時：元ファイル名に拡張子.aeを付加
- 復号時：元ファイルに拡張子.aeがあれば.aeを除く。.aeがない場合は.decodedを付加。

### 利用例

aaa.txtを暗号化。（aaa.txt.aeを出力）

    ae.exe aaa.txt

aaa.txt.aeを復号。（aaa.txtを出力）

    ae.exe -d aaa.txt.ae

Linuxのopensslコマンドで同じことをするには以下のようになる。

```
暗号化
openssl aes-256-cbc -in aaa.txt -out aaa.txt.ae

復号化
openssl enc -d -aes-256-cbc -in aaa.txt.ae -out aaa.txt

```


## 対応する暗号方式

- aes-128-cbc
- aes-192-cbc
- aes-256-cbc (デフォルト)

必要に応じて増やす予定。

### 参考：暗号ファイルフォーマット

opensslコマンドが生成するファイルと互換。

| offset | bytes | data               |
|--------|-------|--------------------|
| +0     | 8     | 文字列 "Salted__"  |
| +8     | 8     | Salt               |
| +16    | -     | 暗号化されたデータ |

Saltなしの場合は先頭16バイトがなくなる。


## ライセンス

[MIT](https://opensource.org/licenses/MIT)
