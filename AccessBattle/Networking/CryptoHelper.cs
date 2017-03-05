using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Uses symmetric encyption to encrypt a byte array. 
    /// Then encrypts the symmetric key using asymmetric encryption.
    /// Used for encrypted data transfer between client and server.
    /// </summary>
    public class CryptoHelper
    {
        RSACryptoServiceProvider _rsa;
        bool _canDecrypt;

        /// <summary>
        /// Default constructor. 
        /// Uses private key to decrypt. Encryption done by communication partner!
        /// </summary>
        public CryptoHelper()
        {
            var cspParams = new CspParameters
            {
                ProviderType = 1, // PROV_RSA_FULL
                Flags = CspProviderFlags.UseArchivableKey,
                KeyNumber = (int)KeyNumber.Exchange
            };
            _rsa = new RSACryptoServiceProvider(cspParams);
            _canDecrypt = true;
        }

        /// <summary>
        /// Use existing public key.
        /// Encryption only!
        /// </summary>
        /// <param name="publicKey"></param>
        public CryptoHelper(string publicKey)
        {
            var cspParams = new CspParameters
            {
                ProviderType = 1 // PROV_RSA_FULL
            };
            _rsa = new RSACryptoServiceProvider(cspParams);
            _rsa.FromXmlString(publicKey);
        }

        public string GetPublicKey()
        {
            return _rsa.ToXmlString(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <remarks>Credits: http://pages.infinit.net/ctech/20031101-0151.html </remarks>
        public byte[] Encrypt(byte[] data)
        {
            try
            {
                using (var sa = SymmetricAlgorithm.Create())
                {
                    using (var ct = sa.CreateEncryptor())
                    {
                        var enc = ct.TransformFinalBlock(data, 0, data.Length);
                        var fmt = new RSAPKCS1KeyExchangeFormatter(_rsa);
                        var keyex = fmt.CreateKeyExchange(sa.Key);
                        var ret = new byte[keyex.Length + sa.IV.Length + enc.Length];
                        Buffer.BlockCopy(keyex, 0, ret, 0, keyex.Length);
                        Buffer.BlockCopy(sa.IV, 0, ret, keyex.Length, sa.IV.Length);
                        Buffer.BlockCopy(enc, 0, ret, keyex.Length + sa.IV.Length, enc.Length);
                        return ret;
                    }
                }
                
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// /// <remarks>Credits: http://pages.infinit.net/ctech/20031101-0151.html </remarks>
        public byte[] Decrypt(byte[] data)
        {
            if (!_canDecrypt) throw new InvalidOperationException("This object has no private key for decryption.");
            try
            {
                using (var sa = SymmetricAlgorithm.Create())
                {
                    var keyex = new byte[_rsa.KeySize >> 3];
                    Buffer.BlockCopy(data, 0, keyex, 0, keyex.Length);
                    var def = new RSAPKCS1KeyExchangeDeformatter(_rsa);
                    var key = def.DecryptKeyExchange(keyex);
                    var iv = new byte[sa.IV.Length];
                    Buffer.BlockCopy(data, keyex.Length, iv, 0, iv.Length);
                    using (var ct = sa.CreateDecryptor(key, iv))
                    {
                        var dec = ct.TransformFinalBlock(data, keyex.Length + iv.Length, data.Length - (keyex.Length + iv.Length));
                        return dec;
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
