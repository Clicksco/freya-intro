module Program
open System
open Microsoft.Owin.Hosting

[<EntryPoint>]
let main _ =
    let url = "http://localhost:8080"
    let _ = WebApp.Start<ContentNeg.ContentNeg> (url)
    Console.WriteLine("Serving site at " + url)
    Console.WriteLine("Press ENTER to cancel")
    let _ = Console.ReadLine ()
    0