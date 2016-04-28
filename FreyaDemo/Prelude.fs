[<AutoOpen>]
module Prelude

open System.Diagnostics
open Microsoft.FSharp.Reflection
open DotLiquid

// Async helper functions copied from https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/ControlCollections.Async.fs
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Async = 
    /// Transforms an Async value using the specified function.
    [<CompiledName("Map")>]
    let map (mapping : 'T -> 'U) (value : Async<'T>) : Async<'U> = async { 
                                                                       // Get the input value.
                                                                       let! x = value
                                                                       // Apply the mapping function and return the result.
                                                                       return mapping x }
    
    // Transforms an Async value using the specified Async function.
    [<CompiledName("Bind")>]
    let bind (binding : 'T -> Async<'U>) (value : Async<'T>) : Async<'U> = async { 
                                                                               // Get the input value.
                                                                               let! x = value
                                                                               // Apply the binding function and return the result.
                                                                               return! binding x }

/// Maybe computation expression builder, copied from ExtCore library
/// https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/Control.fs
[<Sealed>]
type MaybeBuilder() = 
    
    // 'T -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Return value : 'T option = Some value
    
    // M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.ReturnFrom value : 'T option = value
    
    // unit -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Zero() : unit option = Some() // TODO: Should this be None?
    
    // (unit -> M<'T>) -> M<'T>
    [<DebuggerStepThrough>]
    member __.Delay(f : unit -> 'T option) : 'T option = f()
    
    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Combine(r1, r2 : 'T option) : 'T option = 
        match r1 with
        | None -> None
        | Some() -> r2
    
    // M<'T> * ('T -> M<'U>) -> M<'U>
    [<DebuggerStepThrough>]
    member inline __.Bind(value, f : 'T -> 'U option) : 'U option = Option.bind f value
    
    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    [<DebuggerStepThrough>]
    member __.Using(resource : 'T // (unit -> bool) * M<'T> -> M<'T>
                                  // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
                                  // seq<'T> * ('T -> M<'U>) -> M<'U>
                                  // or
                                  // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
                                  when 'T // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
                                          :> System.IDisposable, body : _ -> _ option) : _ option = 
        try 
            body resource
        finally
            if not <| obj.ReferenceEquals(null, box resource) then resource.Dispose()
    
    [<DebuggerStepThrough>]
    member x.While(guard, body : _ option) : _ option = 
        if guard() then 
            x.Bind(body, (fun () -> x.While(guard, body)))
        else x.Zero()
    
    [<DebuggerStepThrough>]
    member x.For(sequence : seq<_>, body : 'T -> unit option) : _ option = 
        x.Using(sequence.GetEnumerator(), fun enum -> x.While(enum.MoveNext, x.Delay(fun () -> body enum.Current)))

let maybe = MaybeBuilder()

let parseTemplate<'T> (template : string) = 
        let rec registerTypeTree ty = 
            if FSharpType.IsRecord ty then 
                let fields = FSharpType.GetRecordFields(ty)
                Template.RegisterSafeType(ty, 
                                          [| for f in fields -> f.Name |])
                for f in fields do
                    registerTypeTree f.PropertyType
            elif ty.IsGenericType && (let t = ty.GetGenericTypeDefinition()
                                      t = typedefof<seq<_>> || t = typedefof<list<_>>)
            then 
                ()
                registerTypeTree (ty.GetGenericArguments().[0])
            else ()
        registerTypeTree typeof<'T>
        let t = Template.Parse(template)
        fun k (v : 'T) -> t.Render(Hash.FromDictionary(dict [ k, box v ]))