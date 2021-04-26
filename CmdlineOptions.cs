using System.Security.Cryptography;
using Mii;

namespace ae
{
    public class CmdlineOptions
    {
        [CmdOption("h", "help", "Show help.")]
        public bool ShowHelp { get; set; }

        [CmdOption("v", "version", "Show version of this tool.")]
        public bool ShowVersion { get; set; }

        [CmdOption("e", "encrypt", "Encrypt the file. (default)")]
        public bool IsEncrypt { get; set; }

        [CmdOption("d", "decrypt", "Decrypt the file.")]
        public bool IsDecrypt { get; set; }

        //[Cmdline("P")]
        //public bool PrintKeyIv { get; set; }

        [CmdOption("k", "password", "Specify password")]
        public string Password { get; set; }

        [CmdOption("o", "out", "Specify output-filename")]
        public string OutputFilename { get; set; }

        public int KeySize { get; set; } = 256;

        public CipherMode CipherMode { get; set; } = CipherMode.CBC;

        [CmdOption(LongOption = "aes-256-cbc", Help = "Use AES with keysize=256,mode=cbc (default setting)")]
        public bool aes256cbc
        {
            get => KeySize == 256 && CipherMode == CipherMode.CBC;
            set { KeySize = 256; CipherMode = CipherMode.CBC; }
        }

        [CmdOption(LongOption = "aes-192-cbc", Help = "Use AES with keysize=192,mode=cbc.")]
        public bool aes192cbc
        {
            get => KeySize == 192 && CipherMode == CipherMode.CBC;
            set { KeySize = 192; CipherMode = CipherMode.CBC; }
        }

        [CmdOption(LongOption = "aes-128-cbc", Help = "Use AES with keysize=128,mode=cbc.")]
        public bool aes128cbc
        {
            get => KeySize == 128 && CipherMode == CipherMode.CBC;
            set { KeySize = 128; CipherMode = CipherMode.CBC; }
        }
    }
}