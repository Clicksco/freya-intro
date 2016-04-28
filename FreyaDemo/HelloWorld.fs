module HelloWorld

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
open Arachne.Http

// Properties
let mediaTypes = [ MediaType.Text ]
let methods = [ POST ]

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
         Data = "Hello World!"B } 

// Resource
let helloWorld =
    freyaMachine {
        using http
        mediaTypesSupported mediaTypes
        methodsSupported methods
        serviceAvailable available
        handleOk content } 

type HelloWorld() =
    member __.Configuration() =
        OwinAppFunc.ofFreya helloWorld