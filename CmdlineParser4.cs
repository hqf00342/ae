using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/*
コマンドラインパーサー CmdlineParser4 ver4.2
CmdlineAttribute を設定したクラスにパースするライブラリ

## 変更履歴

  2021-4-25   (v4.2)Help生成機能を実装
  2021-4-25   Attribute名をCmdlineからCmdOptionに変更
  2021-4-22   CreateOptionDictionary()の型キャストを最適化
  2021-1-28   コンストラクタで例外発生しないように辞書作成をParseに移動。辞書作成時の例外処理を最適化
  2021-1-12   ver4.1: 引数あり短オプションの連続表記に対応。例：-d100
  2021-1-11   net5固有の範囲演算子[..]をやめて.netFramework4.6.1でも利用できるようにした。
  2021-1-10   ライブラリ名を4に変更。コンストラクタ内でParseすると例外対処困難なのでParse()をpublicとし別にする。
  2021-1-9    Attributeのプロパティにsetter復活。名前付きパラメータの設定ができないバグを修正
              プロパティ代入時のTryParseをを判定(SetProperty)。
              パース失敗時,未定義オプション利用時に ArgumentException をthrowする。
  2020-11-3   Attributeにヘルプ追加。実装はまだ。
  2020-3-22   float, double, DateTime に対応
  2020-3-22   dotnet core3.1 で利用。不要な関数などを削除
  2019-7-2    属性を複数つけられるように修正
  2017-4-8    C#7.0形式に変更
  2017-3-5    =を使った引数指定に対応
  2017-3-5    Lengthを実装。NonOptionArgs.Lengthを返す。
  2017-3-5    this[]を実装。インデクサでオプションではない引数を参照可能にした。
  2017-2-23   短いオプション名のまとめ表記(-fr など)に対応 (sgrep)
              オプション属性記載時にハイフンをつけない。
  2017-2-23   初版。長いオプション名に対応 (sgrep)

## 使い方

コンストラクタにコマンドライン引数を渡す。
T はオプションクラス

var cmdline = new CmdlineParser4<T>();

解析は以下のようにする。 args は Main()に渡されたコマンドライン引数

try {
    cmdline.Parse(args);
} catch(ArgumentException e){
    // 例外：未定義オプションの利用、数値/日付等のParse失敗
    return;
}

解析した結果は以下のプロパティに入っている
    cmdline.Options // オプションを解析した結果。クラスT
    cmdline.Args    // オプションではない引数

## 解析の考え方

コマンドライン引数をオプションとオプションではない引数に分ける。
・オプションは Options プロパティへ
・通常の引数は Args プロパティに代入される。

引数を以下の型に変換。
bool, string, int, long, float, double, DateTime

bool以外は引数がある。スペースもしくは＝でつなげる。
引数はダブルクォートすることも可能。自動で除去

短いオプション
"-アルファベット1文字"。ハイフン1つにまとめることが可能。
まとえた場合、引数が必要なオプションは必ず最後につけること。

  -i
  -f TEST.txt
  -f=TEST.txt       // Same as : -f TEST.txt
  -f="TEST.txt"     // Same as : -f TEST.txt
  -iv               // Same as : -i -v
  -ivf TEST.txt     // Same as : -i -v -f TEST.txt
  -ivf='test'       // Same as : -i -v -f TEST.txt

長いオプション
"--文字列"。まとめることは不可

  --verbose
  --ignorecase
  --filename TEST.txt
  --filename=TEST.txt

### オプションクラス

POCOクラスを1つ作り、CmdlineAttributeでプロパティを修飾することで
コマンドラインオプションと対応させる。

using Mii;
internal class OptionInfo
{
    [Cmdline("i", "ignorecase")]
    public bool IgnoreCase { get; set; } = false; //デフォルトをfalse

    [Cmdline("t")]
    public string TerminalID { get; set; };
}

解析後のcmdline.Optionsは
  -i      ... IgnoreCase プロパティが true になる。
  -t STR1 ... TerminalID プロパティに文字列 "STR1" が設定。

使える型は
  bool      (引数なし)オプションがあるとtrueになる。
  string    続く文字を設定
  int       続く文字を int  に変換して設定
  long      続く文字を long に変換して設定
  float     続く文字を float に変換して設定
  double    続く文字を double に変換して設定
  DateTime  続く文字を DateTime に変換して設定
*/

namespace Mii
{
    public class CmdlineParser4<T> where T : class, new()
    {
        //オプション辞書(短いオプション用)
        private readonly Dictionary<string, PropertyInfo> _shortDic = new Dictionary<string, PropertyInfo>();

        //オプション辞書(長いオプション用)
        private readonly Dictionary<string, PropertyInfo> _longDic = new Dictionary<string, PropertyInfo>();

        /// <summary>解析されたオプション結果</summary>
        public T Options { get; }

        /// <summary>オプションではない通常の引数</summary>
        public List<string> Args { get; } = new List<string>();

        /// <summary>
        /// 唯一のコンストラクター。これ以外のpublicメソッドはなし。
        /// コンストラクタ内で解析実施し、結果をOptions,Argsプロパティに反映する。
        /// </summary>
        /// <param name="args">Main()のargsをそのまま渡す</param>
        /// <param name="opt">省略可能。指定した場合はこれをデフォルト値としオプションで上書きする</param>
        public CmdlineParser4(T opt = null)
        {
            Options = opt ?? new T();

            //例外が発生する可能性があるのでParse()へ移動
            //CreateOptionDictionary();
        }

        /// <summary>
        /// コマンドラインをパースし Options, NonOptionArgs を生成する。
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        /// <returns>生成された文字列</returns>
        public void Parse(string[] args)
        {
            //オプションを分析
            CreateOptionDictionary();

            for (int i = 0; i < args.Length; i++)
            {
                //分析対象の引数、続引数
                var arg1 = args[i];
                var arg2 = (i < args.Length - 1 ? args[i + 1] : null)?.Trim('"', '\'');

                if (arg1.StartsWith("--", StringComparison.Ordinal))
                {
                    i += ParseLongOption(arg1, arg2);
                }
                else if (arg1.StartsWith("-", StringComparison.Ordinal))
                {
                    i += ParseShortOption(arg1, arg2);
                }
                else
                {
                    //ハイフン始まりではない = オプションではない
                    Args.Add(args[i]);
                }
            }
        }

        /// <summary>
        /// 短いオプションを解析し、Optionsに投入する
        /// 第2引数を利用した場合は1を返す
        /// 　- 短いオプションは1文字(-d)
        ///   - bool型の場合は次の引数を使わない
        ///   - それ以外の型は次の引数をParseして代入
        ///   - -d=10 -s="str ing" のように=でつなぐことも可能
        /// </summary>
        /// <param name="opt">解析対象のオプション文字列</param>
        /// <param name="secondArg">第2引数</param>
        /// <returns>第2引数を利用した場合は1、利用しない場合は0</returns>
        private int ParseShortOption(string opt, string secondArg)
        {
            opt = opt.TrimStart('-');

            //短いオプションはまとめることを許容するため1文字ずつループする
            for (int x = 0; x < opt.Length; x++)
            {
                var optChar = opt[x].ToString();
                var nextc = (x < opt.Length - 1) ? opt[x + 1] : char.MinValue;

                switch (nextc)
                {
                    case '=':
                        //残りの文字列はすべて引数とみなす
                        secondArg = opt.Substring(x + 2).Trim('"', '\'');
                        TrySetShortOption(optChar, secondArg);
                        return 0; //=を使ったので第2引数は利用せず

                    case char.MinValue:
                        //最後の文字なのでnextArgを使う権利あり
                        return TrySetShortOption(optChar, secondArg);

                    default:
                        //次の文字がある.
                        //連続時はboolオプションしかない前提なら以下の1行のみ。
                        //TrySetShortOption(optChar, null);
                        //bool以外も想定するため以下の実装に変更。

                        //2020-1-12 ver 4.1
                        //boolかもしれないが引数有オプションの可能性もある。
                        //そのため残りの文字列を引数とみなして渡す。
                        var ret = TrySetShortOption(optChar, opt.Substring(x + 1));
                        if (ret > 0)
                        {
                            //次文字を引数として使ったのでループを抜ける。
                            //secondArgを使っていないので 戻り値は0
                            return 0;
                        }
                        //使わない場合はboolだったので残り文字を処理
                        break;
                }
            } //for

            //全も時チェック完了。ここまで来たときは次引数を利用していない
            return 0;

            int TrySetShortOption(string optChar, string nextArg)
            {
                if (_shortDic.TryGetValue(optChar, out PropertyInfo pInfo))
                {
                    //続引数は次のargsを（そのまま）使う。
                    return SetProperty(pInfo, $"-{optChar}", nextArg);
                }
                else
                {
                    throw new ArgumentException($"No such option: -{optChar}", nameof(optChar));
                }
            }
        }

        /// <summary>
        /// 長いオプションを解析し、Optionsに投入する
        /// 次の引数を利用した場合は1を返す
        /// </summary>
        /// <param name="arg">解析対象の文字列</param>
        /// <param name="secondArg">次引数</param>
        /// <exception cref="ArgumentException">数値や日付に変換できない際に発生</exception>
        /// <returns>次引数を利用した場合は1、利用しない場合は0</returns>
        private int ParseLongOption(string arg, string secondArg)
        {
            var haveEqual = false;
            arg = arg.TrimStart('-');

            var ix = arg.IndexOf('=');
            if (ix >= 0)
            {
                //＝が含まれている. 分割し第2引数とする。
                secondArg = arg.Substring(ix + 1).Trim('"', '\'');
                arg = arg.Substring(0, ix);
                haveEqual = true;
            }

            if (_longDic.TryGetValue(arg, out PropertyInfo pInfo))
            {
                //続引数を使ったらインデックスポインタを変更する
                var ret = SetProperty(pInfo, $"--{arg}", secondArg);
                return (haveEqual) ? 0 : ret;
            }
            else
            {
                throw new ArgumentException($"No such option: --{arg}", nameof(arg));
            }
        }

        /// <summary>
        /// このクラスの根幹
        /// プロパティを解釈し、指定された文字列をOptionsに代入する
        /// 以下の型に対応。
        ///   bool, string, int, long, float, double, DateTime
        /// </summary>
        /// <exception cref="ArgumentException">数値や日付に変換できない際に発生</exception>
        /// <param name="pInfo">プロパティ情報</param>
        /// <param name="secondArg">次の引数。型に応じてParseする</param>
        /// <returns>nextArgを消費したら１、bool型で消費しないときは0</returns>
        private int SetProperty(PropertyInfo pInfo, string optionName, string secondArg)
        {
            // 型別に値を取り込む。bool型
            if (pInfo.PropertyType == typeof(bool))
            {
                pInfo.SetValue(Options, true);
                //boolは引数消費がないので0を返す
                return 0;
            }

            //bool型以外は引数有りなのでチェック
            if (string.IsNullOrEmpty(secondArg))
            {
                throw new ArgumentException($"{optionName}: Too few argument.");
            }

            //bool型以外
            if (pInfo.PropertyType == typeof(string))
            {
                pInfo.SetValue(Options, secondArg);
            }
            else if (pInfo.PropertyType == typeof(int))
            {
                if (int.TryParse(secondArg, out int val))
                    pInfo.SetValue(Options, val);
                else
                    throw new ArgumentException($"{optionName}: could not convert {secondArg} to number(int).");
            }
            else if (pInfo.PropertyType == typeof(long))
            {
                if (long.TryParse(secondArg, out long val))
                    pInfo.SetValue(Options, val);
                else
                    throw new ArgumentException($"{optionName}: could not convert {secondArg} to number(long)");
            }
            else if (pInfo.PropertyType == typeof(float))
            {
                if (float.TryParse(secondArg, out float val))
                    pInfo.SetValue(Options, val);
                else
                    throw new ArgumentException($"{optionName}: could not convert {secondArg} to number(float)");
            }
            else if (pInfo.PropertyType == typeof(double))
            {
                if (double.TryParse(secondArg, out double val))
                    pInfo.SetValue(Options, val);
                else
                    throw new ArgumentException($"{optionName}: could not convert {secondArg} to number(double)");
            }
            else if (pInfo.PropertyType == typeof(DateTime))
            {
                if (DateTime.TryParse(secondArg, out DateTime val))
                    pInfo.SetValue(Options, val);
                else
                    throw new ArgumentException($"{optionName}: could not convert {secondArg} to DateTime");
            }

            //bool以外
            //引数を1消費。Parseに失敗しても引数は消費する
            return 1;
        }

        /// <summary>
        /// ユーザ定義のコマンドライン引数クラス T のプロパティ一覧から
        /// CmdOptionAttribute を見つけ、_shortOptions, _longOptions に登録。
        /// </summary>
        /// <exception cref="ArgumentException">オプションが重複登録された場合に発生</exception>
        /// <returns>オプション名とPropertyInfoを収納した辞書</returns>
        private void CreateOptionDictionary()
        {
            // 型Tのプロパティを1つずつ
            foreach (var property in typeof(T).GetProperties())
            {
                var attrList = Attribute.GetCustomAttributes(property, typeof(CmdOptionAttribute));
                foreach (CmdOptionAttribute attr in attrList)
                {
                    //短いオプションを登録
                    if (!string.IsNullOrEmpty(attr.ShortOption))
                    {
                        if (_shortDic.ContainsKey(attr.ShortOption))
                        {
                            throw new ArgumentException($"Duplicate definition: -{attr.ShortOption}");
                        }
                        _shortDic.Add(attr.ShortOption, property);
                    }

                    //長いオプションを登録
                    if (!string.IsNullOrEmpty(attr.LongOption))
                    {
                        if (_longDic.ContainsKey(attr.LongOption))
                        {
                            throw new ArgumentException($"Duplicate definition: --{attr.LongOption}");
                        }
                        _longDic.Add(attr.LongOption, property);
                    }
                }
            }
        }

        /// <summary>
        /// オプションのヘルプメッセージを生成する。
        /// -short,--long="string"  説明Help
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> CreateOptionHelpMessages()
        {
            //Dcitionary<左辺文字列,Help>を生成する
            var dic = new Dictionary<string, string>();

            var maxLeftLen = 0;
            foreach (var property in typeof(T).GetProperties())
            {
                foreach (CmdOptionAttribute attr
                    in Attribute.GetCustomAttributes(property, typeof(CmdOptionAttribute)))
                {
                    string s;
                    if (!string.IsNullOrEmpty(attr.ShortOption) && !string.IsNullOrEmpty(attr.LongOption))
                        s = $"-{attr.ShortOption},--{attr.LongOption}";
                    else if (!string.IsNullOrEmpty(attr.ShortOption))
                        s = $"-{attr.ShortOption}";
                    else
                        s = $"--{attr.LongOption}";

                    var itemtype = property.PropertyType;
                    if (itemtype == typeof(int)) s += " <int>";
                    if (itemtype == typeof(long)) s += " <long>";
                    if (itemtype == typeof(float)) s += " <float>";
                    if (itemtype == typeof(double)) s += " <double>";
                    if (itemtype == typeof(string)) s += " <string>";
                    dic.Add(s, attr.Help);
                    if (s.Length > maxLeftLen) maxLeftLen = s.Length;
                }
            }

            //長さを整えて出力
            return dic.Select(item => $"{item.Key.PadRight(maxLeftLen)}  {item.Value}");
        }
    }

    /// <summary>
    /// コマンドラインオプション用属性。
    /// 1プロパティに複数オプションに対応。
    /// Short, Longともに -, -- は不要。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class CmdOptionAttribute : Attribute
    {
        /// <summary>短いオプション。通常一文字。ハイフン不要</summary>
        public string ShortOption { get; set; }

        /// <summary>長いオプション。ハイフン２つ不要</summary>
        public string LongOption { get; set; }

        /// <summary>ヘルプ用文字列</summary>
        public string Help { get; set; }

        public CmdOptionAttribute() { }

        public CmdOptionAttribute(string shortOption, string longOption = null, string help = null)
        {
            ShortOption = shortOption;
            LongOption = longOption;
            Help = help;
        }
    }
}