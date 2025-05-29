using System.Security.Cryptography;
using System.Text;

namespace Project_site
{
    public class Encryption
    {
        string alphabet = "";
        private string GetString(uint u)
        {
            return ((char)Encoding.GetEncoding(866).GetString([(byte)u])[0]).ToString();
        }
        public Encryption()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            for (uint i = 32; i <= 255; i++)
            {
                alphabet += GetString(i);
            }
        }
        public string GetHash(string password, double telephone)
        {
            var telephone_s = telephone.ToString();
            for (int i = 0; i < password.Length; i++)
            {
                password = password.Insert(i, alphabet[(alphabet.IndexOf(password[i].ToString()) + int.Parse(telephone_s[i % telephone_s.Length].ToString())) % alphabet.Length].ToString());
                password = password.Remove(i + 1, 1);
            }
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        //Перевод изображения в байты
        public byte[] ImageToByteString(IFormFile image)
        {
            var memoryStream = new MemoryStream();
            image.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
