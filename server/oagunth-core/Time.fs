namespace Oagunth.Core

open NodaTime

//We create a module, it corresponds to a static class from the CLR perspective
module Time =

    //What a code want... 
    open NodaTime
    //What a code needs...
    open NodaTime.Calendars

    // In here BankHoliday is the type constructor of SpecialDay
    type SpecialDay = BankHoliday of LocalDate

    //Some type aliasing ...
    type WeekNumber = int

    type Year = int

    type Month = int

    type Day = int


    //This is a function.
    //It wraps the call to the LocalDate object. LocalDate comes from NodaTime a C# library
    //No need for the 'new' keyword here
    let mkDate (year: Year) (month: Month) (day: Day): LocalDate =
            LocalDate(year, month, day)
        
    //This is a F# list. It is different from C# List<T>
    //list<T> or T list in F# is an ordered, immutable series of same type values
    //list is implemented as singly linked lists
    let businessDays: IsoDayOfWeek list =
        [ IsoDayOfWeek.Monday; IsoDayOfWeek.Tuesday; IsoDayOfWeek.Wednesday; IsoDayOfWeek.Thursday; IsoDayOfWeek.Friday ]

    //This is also function
    //In here the return type "bool"
    let isBusinessDay (day: LocalDate) =
        //List.contains is equivalent to Linq operation .Any
        businessDays |> List.contains day.DayOfWeek

    //Let's not use the pipe forward operator (|>) to explain what it does ! 
    let isBusinessDay' (day:LocalDate) =
        List.contains day.DayOfWeek businessDays
        
    //Let's check if those works ...

    //Let's do some function composition with (>>)
    // (>>) f g ~ g ( f )
    //isBusinessDay : LocalDate -> bool
    //not : bool -> bool
    // isBusinessDay >> not : LocalDate -> bool
    let isWeekendDay: LocalDate -> bool = isBusinessDay >> not

    //This is a sum type !
    // All values item after '|' are valid values of the type MonthName
    type MonthName =
        | January
        | February
        | March
        | April
        | May
        | June
        | July
        | August
        | September
        | October
        | November
        | December

    //One way to extend the type 'MonthName' in here we name a module 'MonthName'
    //This allows to use the type as a namespace
    module MonthName =
        //This is an array !
        let ofYear =
            [| January; February; March; April; May; June; July; August; September; October; November; December |]
            |> Set.ofArray

        //Simple function to convert MonthName to Month (alias of int)
        // Month is the return type of the function
        // The type is inferred from its usage bellow
        let toInt value: Month =
            ofYear //You better have wrriten things in the correct order !
            |> Array.ofSeq
            |> Array.findIndex (fun m -> m = value)
            |> fun i -> i + 1
        
        //Let's write an alternative version of 'toInt' ... 
        let toInt' value : Month =
            //This is pattern matching !
            //Order in our case does not matter but pattern matching is evaluation in order...
            match value with
            | January -> 1
            | February -> 2
            | March -> 3
            | April -> 4
            | May -> 5
            | June -> 6
            | July -> 7
            | August -> 8
            | September -> 9
            | October -> 10
            | November -> 11
            | _ -> 12 // _ means anything else ! In our case it can only be December

        // The return type is inferred from the last value evaluated !
        // In here the type is Result<MonthName,string>
        // Result is an F# type which has two possible value ...
        // 'OK of MonthName' or 'Error of string'
        // It is a sum type.
        let fromInt (value: Month) =
            let monthlyIndex = value - 1
            //Ladies and gents, behold the 'if ... then ... else ...' expression !
            if monthlyIndex < 0 || monthlyIndex >= ofYear.Count then
                value
                |> sprintf "Invalid value for month %i" //this is how we format string
                |> OagunthError.insideSingle
                |> Error // from OagunthError to Error of OagunthError
            else
                //To access an array you do '.[]' as bellow
                (ofYear |> Array.ofSeq).[monthlyIndex] |> Ok // from Month (alias of int) to Ok of MonthName
        
        //Pure in practice, Impure in theory :p
        let fromLocalDate(value:LocalDate) =
            match value.Month |> fromInt with
            | Ok monthName -> monthName
            | Error _ -> failwith "The impossible occured !"

    //This is a record
    type WeekId =
        { WeekNumber: WeekNumber
          WeekYear: Year }

    //TODO: Custom Comparison & equality ?
    type WeeklyPeriod =
        { Id: WeekId
          WeekStartsOn: LocalDate
          WeekTerminatesOn: LocalDate
          BusinessDays: LocalDate list
          WeekEndDays: list<LocalDate> }

    //The type of day is inferred from its usage
    let getWeekNumberFromDate day: WeekNumber =
        WeekYearRules.Iso.GetWeekOfWeekYear(day) //A call from NodaTime library...
    let getWeekYearFromDate day: Year = WeekYearRules.Iso.GetWeekYear(day)

    //Generating all weeks for a given year...
    //The type of 'year' and the return type of 'getAllWeeksForYear' are inferred from usage
    let getAllWeeksForYear year =
        //Recursive function, all recursive function in F# are marked with the keyword 'rec'
        //We make it tail call, but it's not really needed
        //Yes this is a function definition within a function definition ...
        //The equivalent of Local function from C# 7 except that we got it from the start ... (~ 2005)
        let rec generateDates (a: LocalDate) (b: LocalDate) (acc: LocalDate list) =
            //List.cons appends at the front of the list to make a new list
            //Since we put it on the front, insert are O(1)
            if a < b then generateDates (a.PlusDays(1)) b (List.Cons(a, acc)) //recursive call
            else acc

        let beginningOfYear = mkDate year 1 1
        let beginningOfNextYear = beginningOfYear.PlusYears(1)
        generateDates beginningOfYear beginningOfNextYear [] //We call our recursive function ...
        //'List' is a module which provides function for the list<> type
        //groupBy = Linq GroupBy
        |> List.groupBy (fun day ->
            { WeekNumber = getWeekNumberFromDate day
              WeekYear = getWeekYearFromDate day })
        //map = Linq Select
        |> List.map (fun (weekId, days) ->
            { Id = weekId
              WeekStartsOn = days |> List.min
              WeekTerminatesOn = days |> List.max
              BusinessDays =
                  days
                  |> List.filter isBusinessDay //We only keep days from Monday to Friday
                  |> List.sort //~ to C# List.Sort
              WeekEndDays =
                  days
                  |> List.filter isWeekendDay // We only keep Saturdays & Sundays
                  |> List.sort })
        //sortBy ~ Linq OrderBy and ThenBy combined
        |> List.sortBy (fun weeklyPeriod -> weeklyPeriod.Id.WeekYear, weeklyPeriod.Id.WeekNumber)

    //Some proof.

    //Let's call LinQ in from our F# code

    //Let's include needed headers ...
    open System.Linq
    open System.Collections.Generic

    //Strictly equivalent version of getAllWeeksForYear but with some Linq
    let getAllWeeksForYear' year =
        //Initialize a C# List<T>
        let listOfDays = List<LocalDate>()
        let beginningOfYear = mkDate year 1 1
        let beginningOfNextYear = beginningOfYear.PlusYears(1)

        //Let's avoid recursion here to do some looping this time
        //Some mutable variable to hold the current value
        let mutable dateCursor = beginningOfYear
        //Let's loop ...
        while (dateCursor < beginningOfNextYear) do
            listOfDays.Add(dateCursor)
            dateCursor <- dateCursor.PlusDays(1)

        listOfDays
            .GroupBy(fun day ->
                  { WeekNumber = getWeekNumberFromDate (day)
                    WeekYear = getWeekYearFromDate (day) })
            //Linq Select
            .Select(fun grouping ->
                  { Id = grouping.Key
                    WeekStartsOn = grouping.Min()
                    WeekTerminatesOn = grouping.Max()
                    //Converts from List<T> (of C#) to T list or list<T> (of F#)
                    BusinessDays = grouping.Where(isBusinessDay).OrderBy(fun d -> d) |> List.ofSeq
                    WeekEndDays = grouping.Where(isWeekendDay).OrderBy(fun d -> d) |> List.ofSeq })
            //Linq OrderBy
            .OrderBy(fun weeklyPeriod -> weeklyPeriod.Id.WeekYear)
            //Linq ThenBy
            .ThenBy(fun weeklyPeriod -> weeklyPeriod.Id.WeekNumber)
        |> List.ofSeq 

    //This is an Active pattern
    //Active pattern allows us to name conditions given data as input
    //It can then be used in Pattern Matching
    let private (|WeekPartOfTheMonth|WeekNotPartOfTheMonth|) (week, month, year) =
        let monthValue = month |> MonthName.toInt
        let monthStart = mkDate year monthValue 1
        let monthEnd = monthStart.With(DateAdjusters.EndOfMonth)
        if week.WeekStartsOn >= monthStart && week.WeekTerminatesOn <= monthEnd
        then WeekPartOfTheMonth
        elif week.WeekStartsOn < monthStart && monthStart <= week.WeekTerminatesOn
        then WeekPartOfTheMonth
        elif week.WeekStartsOn <= monthEnd && monthEnd <= week.WeekTerminatesOn
        then WeekPartOfTheMonth
        else WeekNotPartOfTheMonth

    let getWeeklyPeriodsFromMonthAndYear month year =
        getAllWeeksForYear year
        |> List.filter (fun week ->
            //We pattern match using the active pattern we created
            match week, month, year with
            | WeekPartOfTheMonth -> true
            | WeekNotPartOfTheMonth -> false)
        |> List.sortBy (fun w -> w.Id.WeekYear, w.Id.WeekNumber)

    //Inference does not always works ...
    let getWeeklyPeriodFromDate (day: LocalDate) =
        getAllWeeksForYear day.Year
        |> List.find (fun weeklyPeriod -> day >= weeklyPeriod.WeekStartsOn && day <= weeklyPeriod.WeekTerminatesOn)

    //Our monthly calendar type.
    //We make the constructor private so only this module can create a valid instance
    //This is a product type (like a tuple)
    //Part of the strategy to make invalid state non representable ...
    type MonthlyCalendar = private | MonthlyCalendar of month: MonthName * year: Year * content: WeeklyPeriod list
    //Let's add some methods to our type... yes we can
        with //'with' allows us to attach members to this type...
            member x.IsYear year =
                let (MonthlyCalendar (_,year',_)) = x //We deconstruct x to get the value we need !
                year = year' // '=' here is a comparison !
            member x.IsMonth month =
                let (MonthlyCalendar (month',_,_)) = x
                month = MonthName.toInt month'
            member x.IsMonth month =
                let (MonthlyCalendar (month',_,_)) = x
                month = month'          
            member x.GetWeeksInOrder() : WeeklyPeriod array =
                let (MonthlyCalendar (_,_,content)) = x
                content
                |> List.sortBy (fun weeklyPeriod -> weeklyPeriod.Id.WeekYear,weeklyPeriod.Id.WeekNumber)
                |> Array.ofList //This is how we conver a list to an array
            member x.GetStartOfMonthDate() =
                x.GetWeeksInOrder().[0].WeekStartsOn
            member x.GetEndOfMonthDate() =
                x.GetWeeksInOrder() |> Array.last |> (fun x -> x.WeekStartsOn)
                
            member x.TryFindWeek(week:WeekNumber) =
                x.GetWeeksInOrder()
                |> Array.tryFind (fun weekPeriod -> weekPeriod.Id.WeekNumber = week)
            member x.GetCurrentWeek(from:LocalDate) =
                let (MonthlyCalendar (_,_,content)) = x
                content
                |> List.tryFind
                    ( fun weeklyPeriod ->
                        weeklyPeriod.WeekStartsOn <= from && weeklyPeriod.WeekTerminatesOn >= from 
                    )
                |> Option.map (fun weeklyPeriod -> weeklyPeriod.Id )
                
    //Some more active pattern to access data from outside the module, since the content is private
    let (|MonthlyCalendar|) (MonthlyCalendar (m, y, c)) = (m, y, c)
    let (|MonthlyCalendarYear|) (MonthlyCalendar (_, y, _)) = y
    let (|MonthlyCalendarMonth|) (MonthlyCalendar (m, _, _)) = m
    let (|MonthCalendarContent|) (MonthlyCalendar (_, _, c)) = c

    module MonthlyCalendar =
        //Unique constructor of MonthlyCalendar type
        let from year month =
            let monthValue = MonthName.toInt month

            //We like to recurse stuff
            let rec findFirstDayOfMonth (day: LocalDate) =
                if (day.Month < monthValue) then day.PlusDays(1) |> findFirstDayOfMonth
                //'elif' is 'else if' 
                elif day.Month = monthValue then day
                else failwithf "Cannot look for first day of %A %i from date %A" month year day

            //Yes, we do
            let rec findLastDayOfMonth (day: LocalDate) =
                if (day.Month > monthValue) then day.PlusDays(-1) |> findLastDayOfMonth
                elif day.Month = monthValue then day
                //We should not do this though but let's throw some Exception
                else failwithf "Cannot look for last day of %A %i from date %A" month year day

            let removeDaysNotPartOfTheMonth (w: WeeklyPeriod) =
                let doFilter (days: LocalDate list) = days |> List.filter (fun d -> d.Month = monthValue)
                { w with
                      BusinessDays = w.BusinessDays |> doFilter
                      WeekEndDays = w.WeekEndDays |> doFilter
                      WeekStartsOn = w.WeekStartsOn |> findFirstDayOfMonth
                      WeekTerminatesOn = w.WeekTerminatesOn |> findLastDayOfMonth }

            let onlyDaysForMonth =
                getWeeklyPeriodsFromMonthAndYear month year |> List.map removeDaysNotPartOfTheMonth
            MonthlyCalendar <| (month,year,onlyDaysForMonth) //Yes backward pipe exists, not very useful here though