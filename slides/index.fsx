(**
- title : Freya 
- description : Introduction to Freya
- author : Tony Williams
- theme : night
- transition : default

***

*)

(*** hide ***)
#I "../packages"
#r "Aether/lib/net40/Aether.dll"
#r "Hekate/lib/net40/Hekate.dll"
#r "FParsec/lib/net40-client/FParsecCS.dll"
#r "FParsec/lib/net40-client/FParsec.dll"
#r "Chiron/lib/net45/Chiron.dll"
#r "Owin/lib/net40/owin.dll"
#r "Arachne.Core/lib/net45/Arachne.Core.dll"
#r "Arachne.Language/lib/net45/Arachne.Language.dll"
#r "Arachne.Uri/lib/net45/Arachne.Uri.dll"
#r "Arachne.Uri.Template/lib/net45/Arachne.Uri.Template.dll"
#r "Arachne.Http/lib/net45/Arachne.Http.dll"
#r "Arachne.Http.Cors/lib/net45/Arachne.Http.Cors.dll"
#r "Freya.Core/lib/net45/Freya.Core.dll"
#r "Freya.Recorder/lib/net45/Freya.Recorder.dll"
#r "Freya.Lenses.Http/lib/net45/Freya.Lenses.Http.dll"
#r "Freya.Lenses.Http.Cors/lib/net45/Freya.Lenses.Http.Cors.dll"
#r "Freya.Machine/lib/net45/Freya.Machine.dll"
#r "Freya.Machine.Extensions.Http/lib/net45/Freya.Machine.Extensions.Http.dll"
#r "Freya.Machine.Extensions.Http.Cors/lib/net45/Freya.Machine.Extensions.Http.Cors.dll"
#r "Freya.Router/lib/net45/Freya.Router.dll"
#r "Freya.Machine.Router/lib/net45/Freya.Machine.Router.dll"
#r "Unquote/lib/net40/Unquote.dll"
open Arachne.Http
open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
open Microsoft.Owin.Hosting
open System


(**
### Freya

![Freya visual debugging](images/freya.png)

***

# Contents

* History
* Freya
* Examples
* Machine/Leness
* Questions?

***

# History

---

# OWIN

> Open Web Interface for .NET 

---

### Problems:

* Hard to break away from Asp.net/IIS
* Lot of repetition (E.G. auth for each framework)

' Designed to solve a couple of existing problems
' IIS = Slow, System.Web = Heavy 

---

### Goal: A standard interface between the server and application 

' And thats what we've got now'
    
---

## Support Includes:

### Host
* Katana
* Kestrel
* Nowin
* Suave
* IIS/Express Adapter

### Application

* Nancy
* Signal R
* Asp.net
* ServiceStack
* etc

***

# Freya

---

## Gist

* Function style web programming stack built on OWIN
* HTTP is Functional
* Enforce the pit of success
* It's a library, not a framework

---

# Library
![Lib](http://tomasp.net/blog/2015/library-frameworks/diagram-narrow.png) 

---

# Framework
![Framework](http://tomasp.net/blog/2015/library-frameworks/compose.png)

---

*)

module MyWebSite

open System
open System.IO
open Freya.Core
open Microsoft.Owin.Hosting

let helloWorld =
    freya {
        let text = "Hello World"B
        let! state = Freya.State.get
        state.Environment.["owin.ResponseStatusCode"] <- 200
        state.Environment.["owin.RepsonseReasonPhrase"] <- "Awesome"
        state.Environment.["owin.ResponseBody"] :?> Stream
            |> fun x -> x.Write (text, 0, text.Length)
        }

type Exercise() =
    member __.Configuration() =
        OwinAppFunc.ofFreya helloWorld

[<EntryPoint>]
let main _ =
    let url = "http://localhost:8080"
    WebApp.Start<Exercise> (url) |> ignore
    Console.WriteLine("Serving site at " + url)
    Console.WriteLine("Press ENTER to cancel")
    Console.ReadLine |> ignore

(**
  
---
### Provides several layers of abstractions
    
<img src="images/freya-stack.svg" alt="Freya logo"  style="background-color:#fff;" />

' start at Freya.Core
' work way up to Types/Arachne then lenses then machine
' Show previous slide and how weakly type it is due to owin'
    
---

### Strongly Typed with Arachne

* RFC 7230 – Message Syntax and Routing
* RFC 7231 – Semantics and Content
* RFC 7232 – Conditional Requests
* RFC 7233 – Range Requests
* RFC 7234 – Caching
* RFC 7235 – Authentication
* Everything HTTP is typed

---

*)

// Working with Freya lenses
open Freya.Lenses.Http

// Working directly with the types if required
open Arachne.Http

// The previous way, using raw access to the state
let readPathRaw =
    freya {
        let! state = Freya.getState
        return state.Environment.["owin.RequestPath"] :?> string }

// The lens way
let readPathLens =
    freya {
        return! Freya.get Request.path_ }

(**
---

### Typed

*)

let readAccept =
    freya {
        return! Freya.get Request.Headers.accept_ }

// Might return something like...

Some (Accept [
    AcceptableMedia (
        Open (Parameters (Map.empty)),
        Some (AcceptParameters (Weight 0.3, Extensions (Map.empty))))
    AcceptableMedia (
        Partial (Type "text", Parameters (Map.empty)),
        Some (AcceptParameters (Weight 0.9, Extensions (Map.empty)))) ])

(**
***

# Lenses

>Lenses are a functional technique to enable us to work with complex data structures more easily

---

*)

type Page = { Number: int; Footnote: string; Content: string }
type Book = { Title: string; Page: Page }
type Author = { Name: string; Book: Book }

// Given a Book we can trivially retrieve its page's content:
let pageContent = author.Book.Page.Content

// Setting the page nummber is a bit more problematic though. 
//If this were mutable, we could just do:
author.Book.Page.Number <- 15

// But it's not, so we use F# copy-and-update syntax:
let author2 = { author with Book = 
                { author.Book with Page = 
                    { author.Book.Page with Number = 15 } } }
                    
(**

' Lens get around this issue but the syntax is up part of the language and up to each library, Freya makes use of it.

***

# Machines

> A way of navigating graphs

' Machine is simple model, but it can be quite a big shift in thinking for those who are new to using it. It works differently to how people are using to programming for the web, which is commonly quite imperative and often focuses primarily on the mechanical nature of web programming. Even within the most commonly used “high level” frameworks such as ASP.NET MVC, while the mechanics of dealing with requests and responses are quite significantly abstracted, the logic of handling requests and deciding on appropriate responses is largely left to the programmer to implement.'

---

# HTTP is complex

' The basic concept at the heart of the machine approach is to treat the handling of a request as a series of decisions. Each decision will get us closer to being able to send the most appropriate reponse to the client. These decisions when chained together form a decision graph and the machine navigates through it 

---

![Http machine](images/http-state-diagram.png)

' webmachine (Erlang - riak makers) and Liberator (Clojure). Attempt the standardise web programing in functional languages

---

### A request
![Graph](images/graph.png)

' The machine navigating a decision for a request

---

*)

// Properties
let mediaTypes = [ MediaType.Text ]
let methods = [ GET ]

// Decisions
let available = Freya.init true

// Handlers
let content _ =
    Freya.init
      {  Description =
            {  Charset = Some Charset.Utf8
               Encodings = None
               MediaType = Some MediaType.Text
               Languages = None }
         Data = "Hello World!"B } }

// Resource
let helloWorld =
    freyaMachine {
        using http
        mediaTypesSupported mediaTypes
        methodsSupported methods
        serviceAvailable available
        handleOk content } 

type Exercise() =
    member __.Configuration() =
        OwinAppFunc.ofFreya helloWorld

[<EntryPoint>]
let main _ =
    let url = "http://localhost:8080"
    let _ = WebApp.Start<Exercise> (url)
    Console.WriteLine("Serving site at " + url)
    Console.WriteLine("Press ENTER to cancel")
    let _ = Console.ReadLine ()
    0

(**  

***

# Examples

* Routing
* Content Negotiation
* HTML Rendering


***

# Questions?

*)