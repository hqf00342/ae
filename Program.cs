using System;
using System.Collections.Generic;
using System.IO;
using Mii;

namespace ae
{
    internal class Program
    {
        private const string ENC_EXTENSION = ".ae";
        private const string DEC_EXTENSION = ".decoded";

        private static void Main(string[] args)
        {
            var cmd = new CmdlineParser4<CmdlineOptions>();
            try
            {
                cmd.Parse(args);
            }
            catch (ArgumentException e)
            {
                ErrorEnd(e.Message);
            }

            //show help.
            if (cmd.Options.ShowHelp)
            {
                ShowVersion();
                Help(cmd.CreateOptionHelpMessages());
                return;
            }

            //show version.
            if (cmd.Options.ShowVersion)
            {
                ShowVersion();
                return;
            }

            //check the input file exists.
            if (cmd.Args.Count < 1)
            {
                ErrorEnd("No input filename");
            }

            var inFilename = cmd.Args[0];
            if (!File.Exists(inFilename))
            {
                ErrorEnd($"not found: {inFilename}");
            }

            //set output filename.
            var outFilename = cmd.Options.OutputFilename;
            if (string.IsNullOrEmpty(outFilename))
            {
                if (cmd.Options.IsDecrypt)
                {
                    outFilename = Path.GetExtension(inFilename) == ENC_EXTENSION
                        ? Path.GetFileNameWithoutExtension(inFilename)
                        : inFilename + DEC_EXTENSION;
                }
                else
                {
                    outFilename = inFilename + ENC_EXTENSION;
                }
            }

            //make temporary filename
            var tempfile = GetTempFilename(Path.GetDirectoryName(outFilename));

            //Encrypt / Decrypt
            var isSuccess = false;
            using (var reader = new FileStream(inFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var writer = new FileStream(tempfile, FileMode.Create))
            {
                if (cmd.Options.IsDecrypt)
                {
                    //復号
                    var password = cmd.Options.Password ?? PasswordReader.Ask();
                    try
                    {
                        AesHelper.Decrypt(reader, password, writer, cmd.Options.KeySize, cmd.Options.CipherMode);
                        isSuccess = true;
                    }
                    catch
                    {
                        Console.Error.WriteLine("Decryption failed.");
                    }
                }
                else
                {
                    var password = cmd.Options.Password ?? PasswordReader.AskTwice();
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.Error.WriteLine("bad password read");
                        return;
                    }

                    //暗号化
                    try
                    {
                        AesHelper.Encrypt(reader, password, writer, cmd.Options.KeySize, cmd.Options.CipherMode);
                        isSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Encryption failed.");
                        Console.Error.WriteLine(e.Message);
                    }
                }
            }

            //Clean up the temporary file.
            if (isSuccess)
            {
                //Successful
                if (File.Exists(outFilename))
                    File.Delete(outFilename);
                File.Move(tempfile, outFilename);
            }
            else
            {
                //Failed.
                if (File.Exists(tempfile))
                    File.Delete(tempfile);
            }
        }

        private static string GetTempFilename(string path)
        {
            while (true)
            {
                var filename = Path.Combine(path, Path.GetRandomFileName());
                if (!File.Exists(filename))
                    return filename;
            }
        }

        static private void Help(IEnumerable<string> msgs)
        {
            Console.WriteLine("Encryption/decryption tool compatible with openssl cipher files.");
            Console.WriteLine();
            Console.WriteLine("Usage: ae.exe [options] <input-filename>");
            Console.WriteLine("options:");

            foreach (var msg in msgs)
            {
                Console.WriteLine(msg);
            }
        }

        static private void ErrorEnd(string msg)
        {
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine("Try 'ae.exe --help' for more information.");
            Environment.Exit(1);
        }

        private static void ShowVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"ae.exe {v.Major}.{v.Minor}.{v.Build}");
        }
    }
}