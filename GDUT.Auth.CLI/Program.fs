open System
open GDUTAuth.Ehall
open GDUTAuth.CLI
open Utils

let rec main () =
    let choice =
        [ ("退出", (fun _ -> exitApplication 0 None))
          ("检验是否需要验证码", needCapture)
          ("登录", login)
          ("确认Cookies是否有效", checkIsLogin) ]

    choice |> List.iteri (fun i (name, _) -> printfn $"{i}.{name}")
    let input = inputValueOf "选项" |> Int32.TryParse
    printfn ""

    match input with
    | true, index when index >= 0 && index < choice.Length ->
        let _, f = choice[index]
        f ()
        printfn ""
    | _ -> consoleWithColor ConsoleColor.Red (fun _ -> printfn "无效的选项")

    main ()

main ()
