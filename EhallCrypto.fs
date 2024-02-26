module GDUTAuth.EhallCrypto

open System
open System.Security.Cryptography
open System.Text

let pad (data: byte[]) (blockSize: int) : byte[] =
    let paddingLength = blockSize - (data.Length % blockSize)
    let padding = byte paddingLength
    Array.concat [ data; Array.create paddingLength padding ]

let encrypt v (k:string) =
    let input = pad <| Encoding.ASCII.GetBytes(String.replicate 4 "J69IVxcXqvqNhvk1" + v) <| 16
    let crypt = Aes.Create()
    crypt.Mode <- CipherMode.CBC
    crypt.Key <- k |> Encoding.ASCII.GetBytes
    crypt.IV <- "Jisniwqjwqjwqjww" |> Encoding.ASCII.GetBytes
    crypt.Padding <- PaddingMode.None
    use transform = crypt.CreateEncryptor()
    let encryptedData = transform.TransformFinalBlock(input, 0, input.Length)
    encryptedData |> Convert.ToBase64String
