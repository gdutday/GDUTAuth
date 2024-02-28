module GDUTAuthCLI.Utils

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
