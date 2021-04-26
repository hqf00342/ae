using System;
using System.Text;

namespace Mii
{
    public static class PasswordReader
    {
        /// <summary>
        /// キーボードからパスワードを入力し、読み取る。
        /// 通常のConsole｡ReadLine()だと入力文字が表示されてしまうため
        /// それを隠す。
        /// </summary>
        /// <remarks>
        /// キーコードについては以下でソースコードを確認
        /// https://referencesource.microsoft.com/#mscorlib/system/consolekey.cs
        /// </remarks>
        /// <returns>読み取ったパスワード文字列</returns>
        public static string Ask(string msg = "password:")
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            Console.Write(msg);

            do
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace)
                {
                    //バックスペースなので１文字削除
                    if (password.Length > 0)
                    {
                        password.Length--;
                        Console.CursorLeft--;
                        Console.Write(' ');
                        Console.CursorLeft--;
                    }
                }
                else if (key.Key == ConsoleKey.Tab
                    || key.Key == ConsoleKey.Escape
                    || key.Key == ConsoleKey.Enter
                    || Char.IsControl(key.KeyChar)
                    || key.KeyChar == 0
                    )
                {
                    //バックスペースの処理をした後
                    //コントロール系キー入力を除外
                    continue;
                }
                else
                {
                    //通常のキーは対象として追加。画面に*を表示
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();

            return password.ToString();
        }

        /// <summary>
        /// Ask password twice.
        /// </summary>
        /// <param name="msg1">the first message</param>
        /// <param name="msg2">confirm message </param>
        /// <returns></returns>
        public static string AskTwice(string msg1 = null, string msg2 = null)
        {
            var pass1 = Ask(msg1 ?? "enter password:");
            var pass2 = Ask(msg2 ?? "Verifying - enter password:");
            return pass1 == pass2 ? pass1 : string.Empty;
        }
    }
}