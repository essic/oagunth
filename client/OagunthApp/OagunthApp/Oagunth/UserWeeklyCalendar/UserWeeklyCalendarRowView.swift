//
//  UserWeeklyCalendarRowView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 23/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI

struct UserWeeklyCalendarRowView: View {
    @ObservedObject var viewModel : UserWeeklyCalendarViewModel
    
    init(viewModel: UserWeeklyCalendarViewModel) {
        self.viewModel = viewModel
    }
    
    var body: some View {
        HStack {
            VStack(alignment:.leading) {
                Text(viewModel.weekLabel)
                Text(viewModel.weekStartsOn)
                    .font(.footnote)
            }
            Spacer()
            Text("Total : \(self.viewModel.total, specifier: "%.2f")")
                .foregroundColor(self.viewModel.isComplete ? .green : .none)
        }.padding()
            .onAppear{
                self.viewModel.refresh()
        }
    }
}

struct UserWeeklyCalendarRowView_Previews: PreviewProvider {
    static let id = UUID()
    static let date = Date()
    static let wc = WeeklyCalendar(weekNumber: 18, weekYear: 2020, days: [date])
    static let wcws = WeeklyCalendarWithStatus(calendar: wc, status: .New)
    static let vm = UserWeeklyCalendarViewModel(forWeek: wcws, with:
        [ActivityLog(activityId: id, time: 0.5, date: date)] , and: [Activity(id: id, name: "Testons")], api: FakeClient())
    
    static var previews: some View {
        UserWeeklyCalendarRowView(viewModel: vm)
    }
}
