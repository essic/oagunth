namespace Oagunth.Core

open OagunthCore.Core

module Ports =
    open Time
    open Cra
    open NodaTime
    
    type IHandleUserActivitySubmission =
        abstract member Submit:
            user:User * month:MonthName * year:Year * week:WeekNumber ->Result<Unit,OagunthError>
        
        abstract member HasAlreadyActivitiesSubmittedOrValidated:
            user:User * month:MonthName * year:Year * week:WeekNumber -> Result<bool,OagunthError>
    
        abstract member GetActivitiesStatus:
            user:User * month:MonthName * year:Year * week:WeekNumber -> Result<ActivityTrackingStatus,OagunthError>
    
    type IHandleUserActivityTracking =
        abstract member FetchUserActivities:
            user:User * month:MonthName * year:Year -> Result<Map<LocalDate,ActivitiesTrackedForOneDay>,OagunthError>
            
        abstract member InsertOrUpdateActivities:
            user: User * month: MonthName * year: Year * data: Set<LocalDate * ActivitiesTrackedForOneDay> -> Result<Unit,OagunthError>
            
    type IManageUser =
        abstract member CreateUser :
            username:string -> Result<User,OagunthError>
        abstract member GetUser :
            username:string -> Result<User,OagunthError>
        abstract member DeleteUser :
            username:string -> Result<Unit,OagunthError>
            
    type IReferenceActivities =
        abstract member CreateActivities :
            activities: string seq -> Result<Activity list,OagunthError>
        abstract member RemoveActivityWithName :
            activityName : string -> Result<Unit,OagunthError>
        abstract member GetAllActivities :
            Unit -> Result<Set<Activity>,OagunthError> 