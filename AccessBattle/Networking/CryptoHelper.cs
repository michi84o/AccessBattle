using System;
using System.Security.Cryptography;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Used for encrypted data transfer between client and server.
    /// Uses symmetric encyption to encrypt data. 
    /// The symmetry key is encrypted asymmetric.
    /// </summary>
    public class CryptoHelper
    {
        /// <summary>Used for asymetric encryption./// </summary>
        RSACryptoServiceProvider _rsa;
        bool _canDecrypt;

        /// <summary>
        /// Default constructor. Creates a RSA private key for decryption.
        /// Decryption only!
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
        /// Constructor that uses existing public key.
        /// Encryption only!
        /// </summary>
        /// <param name="publicKey">Public RSA key to use.</param>
        public CryptoHelper(string publicKey)
        {
            var cspParams = new CspParameters
            {
                ProviderType = 1 // PROV_RSA_FULL
            };
            _rsa = new RSACryptoServiceProvider(cspParams);
            _rsa.FromXmlString(publicKey);
        }

        /// <summary>
        /// Gets the public key as XML string.
        /// </summary>
        /// <returns></returns>
        public string GetPublicKey()
        {
            return _rsa.ToXmlString(false);
        }

        /// <summary>
        /// Encrypts data using the symmetric key and encrypts the symmetric key asymmetrically.
        /// </summary>
        /// <param name="data">Data to encrypt.</param>
        /// <returns>The encrypted data, including the symmetric key.</returns>
        /// <remarks>Credits: http://pages.infinit.net/ctech/20031101-0151.html </remarks>
        public byte[] Encrypt(byte[] data)
        {
            try
            {
                using (var sa = SymmetricAlgorithm.Create())
                {
                    using (var ct = sa.CreateEncryptor())
                    {
                        // encrypt the data symmetrically
                        var enc = ct.TransformFinalBlock(data, 0, data.Length);

                        // Encrypt the symmetric key using RSA (asymmetric)
                        var fmt = new RSAPKCS1KeyExchangeFormatter(_rsa);
                        var keyex = fmt.CreateKeyExchange(sa.Key);

                        // Create output buffer
                        var ret = new byte[keyex.Length + sa.IV.Length + enc.Length];

                        // Part 1: Copy the encrypted symmetric key
                        Buffer.BlockCopy(keyex, 0, ret, 0, keyex.Length);

                        // Part 2: Copy the initialization vector for the symmetric key.
                        //         This information is public
                        Buffer.BlockCopy(sa.IV, 0, ret, keyex.Length, sa.IV.Length);

                        // Part 3: Copy the encrypted data
                        Buffer.BlockCopy(enc, 0, ret, keyex.Length + sa.IV.Length, enc.Length);
                        return ret;
                    }
                }

            }
            catch (Exception e)
            {
                Log.WriteLine("CryptoHelper: Encrypt: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Decrypts data that was encrypted using the Encrypt method.
        /// </summary>
        /// <param name="data">Data to decrypt.</param>
        /// <returns>Decrypted data.</returns>
        /// /// <remarks>Credits: http://pages.infinit.net/ctech/20031101-0151.html </remarks>
        public byte[] Decrypt(byte[] data)
        {
            if (!_canDecrypt) throw new InvalidOperationException("This object has no private key for decryption.");
            try
            {
                using (var sa = SymmetricAlgorithm.Create())
                {
                    // Extract the encrypted symmetric key
                    var keyex = new byte[_rsa.KeySize >> 3];
                    Buffer.BlockCopy(data, 0, keyex, 0, keyex.Length);

                    // Decrypt the symmetric key.
                    var def = new RSAPKCS1KeyExchangeDeformatter(_rsa);
                    var key = def.DecryptKeyExchange(keyex);

                    // Extract the initialization vector
                    var iv = new byte[sa.IV.Length];
                    Buffer.BlockCopy(data, keyex.Length, iv, 0, iv.Length);

                    // Decrypt the data
                    using (var ct = sa.CreateDecryptor(key, iv))
                    {
                        var dec = ct.TransformFinalBlock(data, keyex.Length + iv.Length, data.Length - (keyex.Length + iv.Length));
                        return dec;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine("CryptoHelper Decrypt: " + e.Message);
                return null;
            }
        }
    }
}
