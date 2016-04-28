module ContentNeg

open System
open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Router
open Freya.Machine.Extensions.Http
open Arachne.Http
open Freya.Lenses.Http

type Car = 
    { Id : int
      Make : string
      Model : string
      NumberOfWheels : int
      Reg : String }

let car1 = 
    { Id = 1
      Make = "Ford"
      Model = "Fiesta"
      NumberOfWheels = 4
      Reg = "NV61 BYD" }

let car2 = 
    { Id = 2
      Make = "Audi"
      Model = "A1"
      NumberOfWheels = 3
      Reg = "L1am" }

let carsDB = 
    [ car1; car2 ]
    |> List.map (fun c -> c.Id, c)
    |> Map.ofList

module Template =

    let car = parseTemplate<Car> """<p>Car Id:{{ car.id }}</p>
    <p>Car Make:{{ car.make }}</p>
    <p>Car Model:{{ car.model }}</p>
    <p>Car Wheels:{{ car.number_of_wheels }}</p>
    <p>Car Reg:{{ car.reg }}</p
"""

let carId = 
    freya { 
        let! carIdOpt = Freya.Optic.get (Route.atom_ "carId")
        return match carIdOpt with
               | None -> None
               | Some carId -> 
                   match Int32.TryParse carId with
                   | true, id -> Some id
                   | _ -> None
    }

let getFromDB = 
    freya { 
        let! carIdOpt = carId
        return match carIdOpt with
               | None -> None
               | Some carId -> Map.tryFind carId carsDB
    } |> Freya.memo

// Decisions
let isMalformed = 
    freya { 
        let! meth = Freya.Optic.get Request.method_
        match meth with
        | GET -> let! carId = carId
                 return Option.isNone carId
        | CONNECT 
        | DELETE 
        | HEAD 
        | OPTIONS 
        | POST 
        | PUT 
        | TRACE 
        | Method.Custom(_) -> return false
       
    }

let doesExist = freya { let! carOpt = getFromDB
                        return Option.isSome carOpt }
// Properties
let mediaTypes = [ MediaType.Html; MediaType.Json ]
let methods = [ GET ]

let writeHtml model = 
    freya { 
        let html = model |> Template.car "car" |> Text.Encoding.UTF8.GetBytes
        return { Description = 
                     { Charset = Some Charset.Utf8
                       Encodings = None
                       MediaType = Some MediaType.Html
                       Languages = None }
                 Data = html }
    }

let writeJson model = 
    freya { 
        let json = Newtonsoft.Json.JsonConvert.SerializeObject model |> Text.Encoding.UTF8.GetBytes
        return { Description = 
                     { Charset = Some Charset.Utf8
                       Encodings = None
                       MediaType = Some MediaType.Json
                       Languages = None }
                 Data = json }
    }

// Handlers
let content (spec : Specification) = 
    freya { 
        let! carOpt = getFromDB
        let car = carOpt.Value
        match spec.MediaTypes with
        | Negotiation.Negotiated l when l |> List.exists (fun m -> m = MediaType.Html) -> return! writeHtml car
        | Negotiation.Negotiated l when l |> List.exists (fun m -> m = MediaType.Json) -> return! writeJson car
        | _ -> return Representation.empty
    }

// Resource
let machine = 
    freyaMachine { 
        using http
        mediaTypesSupported mediaTypes
        methodsSupported methods
        handleOk content
        exists doesExist
        malformed isMalformed
    }

let router = freyaRouter { resource "/cars/{carId}" machine }

type ContentNeg() = 
    member __.Configuration() = OwinAppFunc.ofFreya router
