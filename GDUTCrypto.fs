module GDUTAuth.GDUTCrypto

open System
open System.Text
open System.Security.Cryptography

let pkcs7Padding (data: byte[]) (blockSize: int) =
    let pl = blockSize - (Array.length data % blockSize)
    Array.append data (Array.init pl (fun _ -> byte pl))

let pkcs5Padding data = pkcs7Padding data 8

let encrypt (key: string) (data: byte[]) =
    let paddedData = pkcs7Padding data 16
    let keyBytes = Encoding.ASCII.GetBytes(key)
    let aes = Aes.Create()
    aes.Key <- keyBytes
    aes.Mode <- CipherMode.ECB
    use transform = aes.CreateEncryptor()
    let encryptedData = transform.TransformFinalBlock(paddedData, 0, paddedData.Length)
    encryptedData |> Convert.ToBase64String

let decrypt (key: string) (data: string) =
    let keyBytes = Encoding.ASCII.GetBytes(key)
    let base64DecodedData = Convert.FromBase64String data
    let aes = Aes.Create()
    aes.Key <- keyBytes
    aes.Mode <- CipherMode.ECB
    use decryptor = aes.CreateDecryptor()
    let decryptedData = decryptor.TransformFinalBlock(base64DecodedData, 0, base64DecodedData.Length)
    let paddingLength = int(decryptedData.[decryptedData.Length - 1])
    let decryptedTextBytes = Array.sub decryptedData 0 (decryptedData.Length - paddingLength)
    Encoding.ASCII.GetString(decryptedTextBytes)
