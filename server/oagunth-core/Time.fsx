#load @"../.paket/load/netcoreapp3.1/main.group.fsx"
#load "Time.fs"
open Oagunth.Core.Time
open NodaTime

(*
let saturday16thMay = LocalDate(2020,5,16)
saturday16thMay |> isBusinessDay = false
saturday16thMay |> isWeekendDay

let wednesday13thMay = LocalDate(2020,5,13)
wednesday13thMay |> isWeekendDay = false
wednesday13thMay |> isBusinessDay
*)

(*
//Some F# and C# comparison
[1950..2030]
|> List.iter ( fun year ->
    if getAllWeeksForYear year <> getAllWeeksForYear' year then failwithf "Difference spotted for %i !" year  )
*)


//MonthName.toInt December
//MonthName.toInt January

//MonthName.fromInt 1
//MonthName.fromInt 12
//MonthName.fromInt 33
//MonthName.fromInt (-10)


//MonthlyCalendar.from 2021 May
//MonthlyCalendar.from 2020 May
//MonthlyCalendar.from 1996 January
//MonthlyCalendar.from 1995 January

//getAllWeeksForYear 2020
//getAllWeeksForYear 2019
//getAllWeeksForYear 1995

//getAllWeeksForYear 1996 = getAllWeeksForYear' 1996
//getAllWeeksForYear 1996 = getAllWeeksForYear' 2020
//getAllWeeksForYear 2020 = getAllWeeksForYear' 2020

//mkDate 2020 5 5 |> getWeeklyPeriodFromDate 
//mkDate 2020 5 1 |> getWeeklyPeriodFromDate //interesting stuff here.

//MonthlyCalendar.from 2020 May
//MonthlyCalendar.from 2020 June