module CHaRM.Util.Observable

open System
open System.Collections.Generic
open System.Reactive.Linq
open System.Reactive.Threading.Tasks
open FSharp.Control.Reactive

type 't obs = IObservable<'t>

type IObservable<'t> with
    static member Map (x: IObservable<'t>, f) = Observable.map f x
    static member GroupBy (x: IObservable<'t>, f) =
        Observable.groupBy f x
        |> Observable.map (fun i -> i.Key, i.AsObservable())

let unit<'t> = Observable.empty<'t>
let get filter input =
    input
    |> Observable.filter filter
    |> Observable.take 1
let lift input = Observable.Return input
let toTask (input: _ obs) = input.ToTask()
let toTaskList (input: _ obs) =
    input
    |> Observable.toList
    |> Observable.map (fun (lst: IList<'a>) -> lst :?> List<'a>)
    |> toTask
let throwIf e (condition: 'a obs -> bool obs) (input: 'a obs): 'a obs =
    Observable.Create(subscribe = fun (observer: 'a IObserver) ->
        input
        |> Observable.subscribeWithCallbacks
            observer.OnNext
            observer.OnError
            (fun () ->
                input
                |> condition
                |> Observable.subscribe (fun value ->
                    if value then observer.OnError e
                    else observer.OnCompleted ())
                |> ignore))

module Operators =
    let (<*>) x f = Observable.map f x
    let (>>=) x f = Observable.bind f x
    let (=<<) f x = Observable.bind f x
    let (>=>) f g = f >> (Observable.bind g)
    let (<=<) g f = f >> (Observable.bind g)
