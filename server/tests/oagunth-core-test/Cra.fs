namespace Oagunth.Core.Tests
open Expecto
open FsCheck
open Oagunth.Core.Time
open System

module Cra =
    
    [<Tests>]
    let checkUserCalendar =
        testList "Must fail for activities outside of the month" [
            testCase "" <| fun _ ->
                Expect.isTrue true "Not yet implemented"
        ]