module GDUT.Auth.Utils

open System
open System.Net

let CookiesCollectionToString (cookies: CookieCollection) =
    String.Join("; ", (cookies |> Seq.map (fun c -> $"{c.Name}={c.Value}")))

let StringToCookieCollection (cookieString: string) : CookieCollection =
    let cookies = CookieCollection()

    cookieString.Split([| ';'; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.iter (fun cookie ->
        let parts = cookie.Split([| '=' |], 2)
        let key = parts[0]
        let value = parts[1]

        let cookie = Cookie(key, value)
        cookie.Domain <- "authserver.gdut.edu.cn"
        cookie |> cookies.Add)

    cookies
