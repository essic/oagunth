namespace Oagunth.Core.Tests
open Expecto
open FsCheck
open Oagunth.Core.Time
open System

module Constant =
    let allMonthOfTheYear = [1;2;3;4;5;6;7;8;9;10;11;12]

module Gen =
    type InvalidMonth = InvalidMonth of int
    let invalidMonth =
        Gen.choose (Int64.MinValue |> int ,Int64.MaxValue |> int)
        |> Gen.filter ( fun i -> Constant.allMonthOfTheYear |> List.contains i  |> not)
        |> Gen.map InvalidMonth
        |> Arb.fromGen
        
//    type ValidMonth = ValidMonth of MonthName
    let validMonth =
        Gen.elements (MonthName.ofYear)
        |> Arb.fromGen

    let addToConfig config =
        { config with arbitrary = typeof<InvalidMonth>.DeclaringType::config.arbitrary }
        
[<AutoOpen>]
module Auto =
    let private config = Gen.addToConfig FsCheckConfig.defaultConfig
    let testProp name = testPropertyWithConfig config name
    let ptestProp name = ptestPropertyWithConfig config name
    let ftestProp name = ftestPropertyWithConfig config name
    let etestProp stdgen name = etestPropertyWithConfig stdgen config name  

module Time =
    let rec private dirtySequence (x:Result<'a,'b> list) : Result<'a list, 'b> =
        match x with
        | (Error b)::_ -> Error b
        | [] -> Ok []
        | (Ok a)::xs -> dirtySequence xs |> Result.map (fun i -> a :: i )  
    
    [<Tests>]
    let monthNameComputationTests =
        testList "Month computations" [
            testCase "Given valid month from 1 to 12 when computing to month name" <| fun _ ->
                let results =
                    Constant.allMonthOfTheYear
                    |> List.map (MonthName.fromInt)
                    |> dirtySequence
                Expect.isOk results "then it should succeed"
            
            testCase "Given valid month from 1 to 12 when checking for all month of one year, in order" <| fun _ ->
                let result =
                    Constant.allMonthOfTheYear
                    |> List.map (MonthName.fromInt)
                    |> dirtySequence
                let expected = MonthName.ofYear |> List.ofSeq |> Ok
                Expect.equal result expected "then they should all be found"
                
            testCase "Given all month name for a year when computing its number" <| fun _ ->
                let result = MonthName.ofYear |> Set.map MonthName.toInt |> Seq.toList
                let expected = Constant.allMonthOfTheYear
                Expect.equal result expected "then it should be between 1 and 12"
                
            ptestProp "Given an invalid month number when computing its month name"
                <| fun (Gen.InvalidMonth e) ->
                    let result = MonthName.fromInt e
                    Expect.isError result "then it should fail"
                    
            ptestProp "Given a month name and a year when generating a calendar"
                <| fun (month:MonthName) (year:Year) -> 
                let calendar = MonthlyCalendar.from year month
                let month = MonthName.toInt month
                let areAllDaysInTheSameMonth =
                    calendar.GetWeeksInOrder()
                    |> Seq.collect
                        ( fun weekPeriod ->
                           weekPeriod.WeekTerminatesOn
                           :: weekPeriod.WeekStartsOn
                           :: List.concat [weekPeriod.BusinessDays;weekPeriod.WeekEndDays] )
                    |> Seq.forall ( fun day -> day.Month = month )
                Expect.isTrue areAllDaysInTheSameMonth "then all days should be within the month"
        ]