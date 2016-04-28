module Routing

open System
open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Router
open Freya.Machine.Extensions.Http
open Arachne.Http

let someRoutingThing = 
    freya {
        return! Freya.Optic.get (Route.atom_ "someRoutingThing")
    }

// Properties
let mediaTypes = [ MediaType.Text ]
let methods = [ GET ]

// Handlers
let content _ =
    freya {
        let! dataFromUriOpt = someRoutingThing
        let contentData = match dataFromUriOpt with
                          | None -> "Hello World"
                          | Some dataFromUri -> sprintf "Uri Content: %s" dataFromUri
                          |> Text.Encoding.UTF8.GetBytes
        let content = {  Description =
                            {  Charset = Some Charset.Utf8
                               Encodings = None
                               MediaType = Some MediaType.Text
                               Languages = None }
                         Data = contentData } 

        return content
    }

// Resource
let machine =
    freyaMachine {
        using http
        mediaTypesSupported mediaTypes
        methodsSupported methods
        handleOk content } 

let router = 
    freyaRouter {
        resource "/api/helloWorld" machine
        resource "/api/{someRoutingThing}" machine
    }

type Routing() =
    member __.Configuration() =
        OwinAppFunc.ofFreya router