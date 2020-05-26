//
//  UserMonthlyCalendarViewModel.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI
import Combine

class UserMonthlyCalendarViewModel : ObservableObject {
    private var dispoables = Set<AnyCancellable>()
    private var apiClient : OagunthFetchable
    @Published var dataSource : [UserWeeklyCalendarViewModel] = []
    
    init(api:OagunthFetchable){
        self.apiClient = api
        self.apiClient.fetchActivities()
            .flatMap(self.weeklyCalendarStream(activities:))
            .map { UserWeeklyCalendarViewModel(forWeek: $0.0, with: $0.2, and: $0.1,api: self.apiClient) }
            .receive(on: DispatchQueue.main)
            .sink(receiveCompletion: { [weak self] value in
                guard let self = self else {return}
                switch value {
                case .failure :
                    self.dataSource = []
                case .finished :
                    debugPrint("COMPLETED")
                    break
                }
                }, receiveValue: { [weak self] value in
                    guard let self = self else {return}
                    debugPrint("Just adding WeeklyViewModel for week -> \(value.weeklyCalendarWithStatus.calendar.weekNumber)")
                    self.dataSource.append(value)
            })
            .store(in: &dispoables)
    }
}

struct WeeklyCalendarWithStatus {
    var calendar : WeeklyCalendar
    var status : WeekStatus
}

extension MonthlyCalendar {
    func WeeksWithStatus() throws -> [WeeklyCalendarWithStatus]{
        let onlyWeeksWithDays =
            self.calendar.weeks
                .filter { (!$0.days.isEmpty) }
        var result = [WeeklyCalendarWithStatus]()
        for week in onlyWeeksWithDays {
            guard let rawStatus =
                self.allWeeksStatus.first(where: { $0.weekNumber == week.weekNumber }) else {
                    throw OagunthError.parsing(desc: "Can't find status for week !")
            }
            
            guard let status = rawStatus.getStatusEnum() else {
                throw OagunthError.parsing(desc: "something else")
            }
            
            result.append(WeeklyCalendarWithStatus(calendar: week, status: status))
        }
        return result
    }
}

extension UserMonthlyCalendarViewModel {
    private var calendarStream : AnyPublisher<MonthlyCalendar,OagunthError> {
        self.apiClient.fetchUserCalendarForCurrentMonth()
            .eraseToAnyPublisher()
    }
    
    private func sortedWeeklyCalendar(_ monthlyCalendar:MonthlyCalendar) -> AnyPublisher<WeeklyCalendarWithStatus,OagunthError> {
        
        let sortFunc : (WeeklyCalendarWithStatus,WeeklyCalendarWithStatus) -> Bool = {
            return ($0.calendar.weekYear < $1.calendar.weekYear || $0.calendar.weekNumber < $1.calendar.weekNumber)
        }
        
        do {
            let weeks = try monthlyCalendar.WeeksWithStatus()
            return Publishers
                .Sequence(sequence: weeks.sorted(by: sortFunc))
                .eraseToAnyPublisher()
        }
        catch {
            return Fail(error: OagunthError.parsing(desc: error.localizedDescription))
                .eraseToAnyPublisher()
        }
    }
    
    private func weeklyCalendarStream(activities: [Activity]) -> AnyPublisher<(WeeklyCalendarWithStatus,[Activity],[ActivityLog]),OagunthError> {
        calendarStream
            .flatMap { monthlyCalendar in
                return self.sortedWeeklyCalendar(monthlyCalendar)
                    .map { ($0,activities,monthlyCalendar.activities) }
        }
        .map { weeklyCalendar,activities,activityLogs in
            let sortedDaysOfWeek = weeklyCalendar.calendar.days.sorted()
            
            guard activityLogs.isEmpty == false, let start = sortedDaysOfWeek.first,
                let end = sortedDaysOfWeek.last else {
                return (weeklyCalendar,activities,[])
            }
            
            let activityLogOfWeek = activityLogs.filter { $0.date >= start && $0.date <= end }
            
            return (weeklyCalendar,activities,activityLogOfWeek)
        }
        .eraseToAnyPublisher()
    }

}
