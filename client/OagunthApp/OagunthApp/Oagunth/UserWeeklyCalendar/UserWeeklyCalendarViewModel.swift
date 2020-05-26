//
//  UserWeeklyCalendarRowViewModel.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation
import Combine
import SwiftUI

enum UserWeeklyCalendarStatus {
    case Locked
    case ReadyForSumission
    case ReadyToBeSaved
}

enum WeekAcationRules {
    case noActionToTake
    case canBeSaved
    case canBeSubmitted
    case isLocked
}

class UserWeeklyCalendarViewModel : ObservableObject, Identifiable {
    let id = UUID()
    var weeklyCalendarWithStatus : WeeklyCalendarWithStatus
    private var disposables = Set<AnyCancellable>()
    private var client : OagunthFetchable
    
    @Published var dataSource : [UserDailyCalendarViewModel] = []
    @Published var timeTrackingState = WeekAcationRules.noActionToTake {
        didSet {
            if (timeTrackingState == .isLocked) {
                for source in dataSource {
                    source.isActive = false
                }
            }
        }
    }
    
    @Published var total = 0.0
    @Published var isComplete = true

    var weekLabel : String {
        let lbl = "Week \(self.weeklyCalendarWithStatus.calendar.weekNumber)"
        return lbl
    }
    
    var weekStartsOn : String {
        guard let startsOn = self.weeklyCalendarWithStatus.calendar.days.sorted().first else {
            return "N/A"
        }
        
        let fmt = DateFormatter()
        fmt.dateFormat = "E d MMMM YYYY"
        return fmt.string(from: startsOn)
    }
    
    private var n = 0
    
    func refresh() {
        if weeklyCalendarWithStatus.calendar.weekNumber == 18 {
            n = n + 1
        }
        self.total = self.dataSource.reduce(0.0, { acc,current in current.total + acc})
        if weeklyCalendarWithStatus.calendar.weekNumber == 18 &&
            self.total == 0 {
            debugPrint("call \(n) WHY !")
        }
        self.isComplete = (Double(self.weeklyCalendarWithStatus.calendar.days.count) == self.total)
        switch self.weeklyCalendarWithStatus.status {
        case .New:
                self.timeTrackingState = .canBeSaved
        case .Submitted:
            self.timeTrackingState = .isLocked
        case .Saved :
            if isComplete {
                self.timeTrackingState = .canBeSubmitted
            }
        }
    }
    
    func submit() {
        guard let oneDate = self.dataSource.first?.day else {
            return
        }
        let calendar = Foundation.Calendar.current
        let monthComponent = Foundation.Calendar.Component.month
        let yearComponent = Foundation.Calendar.Component.year
        let components = calendar.dateComponents([monthComponent,yearComponent], from: oneDate)
        
        guard let month = components.month, let year = components.year else {
            return
        }
        
        _ = self.client.submitActivities(month: month, year: year, weekNumber: self.weeklyCalendarWithStatus.calendar.weekNumber)
        .print()
            .receive(on: DispatchQueue.main)
            .sink(receiveCompletion: {
                debugPrint($0)
            }, receiveValue: { _ in
                self.weeklyCalendarWithStatus.status = .Submitted
                self.refresh()
            })
            .store(in: &disposables)
    }
    
    func save() {
        var a = [SaveActivity]()
        
        guard let oneDate = self.dataSource.first?.day else {
            return
        }
        
        for vm in self.dataSource {
            let logsToSave = vm.getLogs()
            
            guard let date = logsToSave.first?.date else {
                return
            }
            
            let calendar = Foundation.Calendar.current
            let dayComponent = Foundation.Calendar.Component.day
            let monthComponent = Foundation.Calendar.Component.month
            let yearComponent = Foundation.Calendar.Component.year
            let components = calendar.dateComponents([dayComponent,monthComponent,yearComponent], from: date)
            
            guard
                let day =  components.day,
                let month = components.month,
                let year = components.year else {
                    return
            }
            
            let activities =
                logsToSave
                    .map { Log(time: $0.time, activityRef: $0.activityId)}
                    .map { SaveActivity(day: day, month: month, year: year, log: $0)}
            a.append(contentsOf: activities)
        }
        
        let b = SaveActivities(days: a)
        _ = self.client.saveActivities(day: oneDate, logs: b)
            .receive(on: DispatchQueue.main)
            .sink(receiveCompletion: {
                debugPrint($0)
            }, receiveValue: { _ in
                self.weeklyCalendarWithStatus.status = .Saved
                self.refresh()
            })
            .store(in: &disposables)
    }
    
    init(forWeek weekWithStatus:WeeklyCalendarWithStatus, with logs:[ActivityLog], and activities:[Activity], api:OagunthFetchable) {
        
        self.client = api
        self.weeklyCalendarWithStatus = weekWithStatus
        
        for day in self.weeklyCalendarWithStatus.calendar.days.sorted() {
            let logsForTheDay = logs.filter { $0.date == day }
            let dailyVm = UserDailyCalendarViewModel(day:day, logs:logsForTheDay, activities:activities )
            dataSource.append(dailyVm)
        }
        
        self.refresh()
        Publishers.Sequence(sequence: dataSource.map{ $0.$total })
            .last()
            .receive(on: DispatchQueue.main)
            .sink{ _ in
                self.refresh()
        }
        .store(in:&disposables)
        
    }
}
