module GDUTAuth.CLI.Utils

open System

let consoleWithColor color f =
    let originalColor = Console.ForegroundColor
    Console.ForegroundColor <- color
    f ()
    Console.ForegroundColor <- originalColor

let AskToCopy name content =
    printfn $"是否复制{name}？ [Y]es"
    let input = Console.ReadLine()
    match input with
    | _ when input.Trim().ToLower().StartsWith('y') ->
        TextCopy.ClipboardService.SetText(content)
        consoleWithColor ConsoleColor.Green (fun _ -> printfn $"{name}已复制！")
    | _ -> ()

let exitApplication code message =
    consoleWithColor ConsoleColor.Red (fun _ ->
        match message with
        | Some m -> printfn $"退出程序：{m}"
        | _ -> printfn "退出程序")

    exit code

let getTimeStamp () =
    DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()

let rec inputValueOf name =
    printfn $"请输入{name}： "
    let input = Console.ReadLine()

    match input with
    | _ when String.IsNullOrEmpty input ->
        consoleWithColor ConsoleColor.Red (fun _ -> printfn $"{name} 不能为空")
        inputValueOf name
    | _ -> input

let rec readSecureValuePaddingWith (p: string) (s: string) =
    let key = Console.ReadKey(true)

    match key.Key with
    | ConsoleKey.Enter ->
        printfn ""
        s
    | ConsoleKey.Backspace ->
        let sn =
            if s.Length > 0 then
                let spaces = String.replicate p.Length " "
                Console.Write($"\b{spaces}\b")
                s[0 .. s.Length - 2]
            else
                s

        readSecureValuePaddingWith p sn
    | _ ->
        p |> Console.Write
        let sn = $"%s{s}%c{key.KeyChar}"
        readSecureValuePaddingWith p sn

let readSecureValue (s: string) = readSecureValuePaddingWith "*" s

let rec inputSecureValueOf name =
    printfn $"请输入{name}： "

    let input = readSecureValuePaddingWith "" String.Empty

    match input with
    | _ when String.IsNullOrEmpty input ->
        consoleWithColor ConsoleColor.Red (fun _ -> printfn $"{name} 不能为空")
        inputSecureValueOf name
    | _ -> input

let needCapture () =
    let userName = inputValueOf "学号"
    let result = isNeedCaptcha userName getTimeStamp |> Async.RunSynchronously

    match result with
    | Ok resultValue ->
        consoleWithColor ConsoleColor.Green (fun _ ->
            match resultValue with
            | true -> printfn "需要验证码"
            | false -> printfn "不需要验证码")
    | Error errorValue -> consoleWithColor ConsoleColor.Red (fun _ -> printfn $"发生错误: {errorValue}")

let login () =
    let username = inputValueOf "学号"
    let password = inputSecureValueOf "密码"
    let cookies = login username password getTimeStamp |> Async.RunSynchronously

    match cookies with
    | Error errorValue -> consoleWithColor ConsoleColor.Red (fun _ -> printfn $"发生错误: {errorValue}")
    | Ok resultValue ->
        let cookiesString = resultValue |> GDUTAuth.Utils.CookiesCollectionToString

        consoleWithColor ConsoleColor.Green (fun _ ->
            printfn $"""Cookies:{"\n"}{cookiesString |> (fun s -> s.Replace("; ", "\n"))}""")

        AskToCopy "Cookies" cookiesString

        match isLogin resultValue with
        | Error errorValue -> consoleWithColor ConsoleColor.Red (fun _ -> printfn $"发生错误: {errorValue}")
        | Ok resultValue ->
            consoleWithColor
            <| (if resultValue then ConsoleColor.Green else ConsoleColor.Red)
            <| (fun _ -> printfn $"已登录: {resultValue}")

let checkIsLogin () =
    let cookies = inputValueOf "Cookies"
    let result = isLogin <| GDUTAuth.Utils.StringToCookieCollection(cookies)

    match result with
    | Error errorValue -> consoleWithColor ConsoleColor.Red (fun _ -> printfn $"发生错误: {errorValue}")
    | Ok resultValue ->
        consoleWithColor
        <| (if resultValue then ConsoleColor.Green else ConsoleColor.Red)
        <| (fun _ -> printfn $"已登录: {resultValue}")
