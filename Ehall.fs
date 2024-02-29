module GDUT.Auth.Ehall

open HtmlAgilityPack
open System
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open EhallCrypto

let host = "https://authserver.gdut.edu.cn"

let uriBuilder () = UriBuilder(host)

let map2PostUrlCodeString mapData =
    let content = new FormUrlEncodedContent(mapData)
    new StringContent(content.ReadAsStringAsync().Result, Encoding.UTF8, "application/x-www-form-urlencoded")

let getResponse (client: HttpClient) (uri: Uri) =
    async {
        try
            let! response = client.GetAsync(uri) |> Async.AwaitTask
            return Ok response
        with ex ->
            return Error $"请求失败：{ex.Message}"
    }

let getResponseContent (response: HttpResponseMessage) =
    try
        let content =
            response.Content.ReadAsStringAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Ok content
    with ex ->
        Error $"读取失败：{ex.Message}"

let isNeedCaptcha username timestamp =
    let path = "/authserver/checkNeedCaptcha.htl"

    let url username timestamp =
        let builder = uriBuilder ()
        builder.Path <- path
        builder.Query <- $"username={username}&_={timestamp}"
        builder.Uri

    async {
        use client = new HttpClient()

        let! response = getResponse client (url username timestamp)
        let content = response |> Result.bind getResponseContent
        let json = content |> Result.bind (JsonDocument.Parse >> Ok)

        let needCaptcha =
            json
            |> Result.bind (fun json ->
                try
                    Ok <| json.RootElement.GetProperty("isNeed").GetBoolean()
                with ex ->
                    Error $"解析失败{ex}")

        return needCaptcha
    }

let login username password (timestampProvider: _ -> int64) =
    let underGraduateLoginPath = "/authserver/login"

    let jwfwOssLoginUri = Uri("http://jxfw.gdut.edu.cn/new/ssoLogin")

    let underGraduateLoginUri =
        let builder = uriBuilder ()
        builder.Path <- underGraduateLoginPath
        builder.Query <- "service=https%3A%2F%2Fjxfw.gdut.edu.cn%2Fnew%2FssoLogin"
        builder.Uri

    let getPage (client: HttpClient) (uri: Uri) =
        async {
            let! response = getResponse client uri
            let page = response |> Result.bind getResponseContent
            return page
        }

    let getDocument (page: string) =
        let doc = HtmlDocument()
        doc.LoadHtml(page)
        Ok doc

    let loginBody username password salt =
        let encryptedPassword = encrypt password salt

        [ ("", salt)
          ("username", username)
          ("password", encryptedPassword)
          ("captcha", "")
          ("rememberMe", "true") ]

    let getHiddenNodes (doc: HtmlDocument) =
        let elements = doc.DocumentNode.SelectNodes("//*[@id='pwdFromId']")
        elements |> Seq.collect (_.SelectNodes(".//input[@type='hidden']")) |> Ok

    let getHiddenParams (nodes: HtmlNode seq) =
        nodes
        |> Seq.map (fun e -> e.Attributes["name"], e.Attributes["value"])
        |> Seq.map (fun (name, value) ->
            match (name, value) with
            | null, v -> ("", v.Value)
            | n, null -> (n.Value, "")
            | n, v -> (n.Value, v.Value))

    let getSaltFromParams (nodes: HtmlNode seq) =
        nodes
        |> Seq.filter (fun n -> n.Id = "pwdEncryptSalt")
        |> Seq.map (_.Attributes["value"].Value)
        |> Seq.head

    async {
        let cookieContainer = CookieContainer()

        let handler =
            new HttpClientHandler(CookieContainer = cookieContainer, AllowAutoRedirect = true)

        use client = new HttpClient(handler)

        let! page = getPage client jwfwOssLoginUri
        let doc = page |> Result.bind getDocument
        let nodes = doc |> Result.bind getHiddenNodes

        match nodes with
        | Error errorValue -> return Error errorValue
        | Ok nodesValue ->
            let salt = nodesValue |> getSaltFromParams
            let hiddenParams = nodesValue |> getHiddenParams
            let loginParams = loginBody username password salt
            let content = dict (Seq.append <| hiddenParams <| loginParams)

            let requestBody = map2PostUrlCodeString content
            let! response = client.PostAsync(underGraduateLoginUri, requestBody) |> Async.AwaitTask

            match response.StatusCode with
            | HttpStatusCode.OK -> return Ok <| cookieContainer.GetAllCookies()
            | HttpStatusCode.Unauthorized -> return Error "用户名或密码错误"
            | _ -> return Error "未知错误"
    }

let isLogin (cookies: CookieCollection) =
    let jxfwHost = "jxfw.gdut.edu.cn"

    let checkResponseIsOk (response: HttpResponseMessage) =
        match response.IsSuccessStatusCode with
        | true -> Ok response
        | _ -> Error "请求失败"

    let checkResponseIsAuthorized (response: HttpResponseMessage) =
        let host = response.RequestMessage.RequestUri.Host

        match host with
        | _ when host = jxfwHost -> Ok true
        | _ -> Ok false


    let uri = Uri $"http://{jxfwHost}/"
    let cookieContainer = CookieContainer()
    cookies |> Seq.iter cookieContainer.Add

    let handler =
        new HttpClientHandler(CookieContainer = cookieContainer, AllowAutoRedirect = true)

    use client = new HttpClient(handler)

    let response =
        getResponse client uri
        |> Async.RunSynchronously
        |> Result.bind checkResponseIsOk

    let isAuthorized = response |> Result.bind checkResponseIsAuthorized
    isAuthorized
