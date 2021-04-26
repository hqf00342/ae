using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;

namespace ae
{
    public static class AesHelper
    {
        //OpenSSLのSALTサイン。8バイト
        private static readonly byte[] SALT_SIGN = Encoding.ASCII.GetBytes("Salted__");

        private static byte[] CreateSalt(int size)
        {
            var buffer = new byte[size];
            var r = new RNGCryptoServiceProvider();
            r.GetBytes(buffer);
            r.Dispose();
            return buffer;
        }

        private static AesManaged CreateAesManaged(string password, byte[] salt, int keysize = 256, CipherMode ciphermode = CipherMode.CBC)
        {
            var passbytes = Encoding.UTF8.GetBytes(password);

            var md5 = MD5.Create();
            var d1 = md5.ComputeHash(passbytes.Concat(salt).ToArray());             // 16byte
            var d2 = md5.ComputeHash(d1.Concat(passbytes).Concat(salt).ToArray());  // 16byte
            var d3 = md5.ComputeHash(d2.Concat(passbytes).Concat(salt).ToArray());  // 16byte
            md5.Dispose();

            //Key,IV生成
            byte[] key;
            byte[] iv;
            if (keysize == 256)
            {
                key = d1.Concat(d2).ToArray(); // 32byte(256bit)
                iv = d3;                       // 16byte
            }
            else if (keysize == 192)
            {
                key = d1.Concat(d2).Take(24).ToArray();        // 24byte(192bit)
                iv = d2.Skip(8).Concat(d3).Take(16).ToArray(); // 16byte
            }
            else if (keysize == 128)
            {
                key = d1;  //16byte(128bit)
                iv = d2;   //16byte
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(keysize));
            }

            //AES生成
            return new AesManaged()
            {
                KeySize = keysize,  // 最初に宣言する必要あり.おそらくKeyプロパティをクリアしてしまう。
                BlockSize = 128,    // AESでは128固定
                Key = key,
                IV = iv,
                Mode = ciphermode,
                Padding = PaddingMode.PKCS7,
            };
        }

        public static void Encrypt(Stream reader, string password, Stream writer, int keysize, CipherMode cmode)
        {
            var salt = CreateSalt(8);
            using (var aes = CreateAesManaged(password, salt, keysize, cmode))
            using (var encrypter = aes.CreateEncryptor())
            using (var cStream = new CryptoStream(writer, encrypter, CryptoStreamMode.Write))
            {
                //SALT書き出し
                writer.Write(SALT_SIGN, 0, 8);
                writer.Write(salt, 0, 8);

                //全データ暗号化
                reader.CopyTo(cStream);
            }
        }

        public static void Decrypt(Stream reader, string password, Stream writer, int keysize, CipherMode cmode)
        {
            //ファイルにOpenSSL形式のSALTがあるかを確認
            var signiture = new byte[8];
            byte[] salt = Array.Empty<byte>();
            reader.Read(signiture, 0, 8);
            if (!signiture.SequenceEqual(SALT_SIGN))
            {
                reader.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                salt = new byte[8];
                reader.Read(salt, 0, 8);
            }

            //CryptoStreamで実施
            using (var aes = CreateAesManaged(password, salt, keysize, cmode))
            using (var dec = aes.CreateDecryptor())
            using (var cStream = new CryptoStream(reader, dec, CryptoStreamMode.Read))
            {
                cStream.CopyTo(writer);
            }
        }
    }
}
