//
//  Responses.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation

struct Calendar : Codable {
    var currentWeekYear : Int
    var currentWeekNumber : Int
    var weeks : [WeeklyCalendar]
}

struct ActivityLog : Codable {
    var activityId : UUID
    var time : Double
    var date : Date
}

struct WeeklyCalendar : Codable {
    var weekNumber : Int
    var weekYear : Int
    var days : [Date]
}

enum WeekStatus : CustomStringConvertible {
    case New
    case Submitted
    case Saved
    
    var description: String {
        switch self {
        case .New :
            return "New"
        case .Submitted :
            return "Submitted"
        case .Saved :
            return "Saved"
        }
    }
}

struct WeekState : Codable {
    var weekYear : Int
    var weekNumber : Int
    var status : String
}

extension WeekState {
    func getStatusEnum() -> WeekStatus? {
        switch self.status {
        case WeekStatus.New.description:
            return Optional.some(.New)
        case WeekStatus.Submitted.description:
            return Optional.some(.Submitted)
        default:
            return Optional.none
        }
    }
}

struct MonthlyCalendar : Codable {
    var currentDate : String
    var activities : [ActivityLog]
    var calendar : Calendar
    var allWeeksStatus : [WeekState]
}

struct Activity : Codable, Hashable {
    var id : UUID
    var name : String
}
