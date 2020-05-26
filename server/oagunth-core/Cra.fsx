#load @"../.paket/load/netcoreapp3.1/main.group.fsx"
#loaf "Errors.fs"
#load "Time.fs"
#load "Cra.fs"
open Oagunth.Core
open Oagunth.Core.Time
open Oagunth.Core.Cra
open System

//isValidForOneDay (-0.25<day>)
//isValidForOneDay (30.<day>)
//isValidForOneDay 0.25<day>
//isValidForOneDay 1.<day>
//isValidForOneDay 1.0

//let calendar = MonthlyCalendar.from 2020 May

//let fakeUser =
//    { UserId = Guid.NewGuid() |> Id
//      UserName = "toto" }
//UserCalendarTracking.make fakeUser calendar (Map.empty)
