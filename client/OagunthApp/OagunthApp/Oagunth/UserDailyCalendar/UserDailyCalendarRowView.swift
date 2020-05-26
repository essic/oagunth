//
//  UserDailyCalendarView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 23/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI

struct UserDailyCalendarRowView: View {
    @ObservedObject var viewModel : UserDailyCalendarViewModel
    
    init(viewModel:UserDailyCalendarViewModel) {
        self.viewModel = viewModel
    }
    
    var body: some View {
        HStack {
            Text(viewModel.dateString)
            Spacer()
            Text("Total : \(self.viewModel.total, specifier: "%.2f")")
                .foregroundColor(self.viewModel.total == 1.0 ? .green : .none)
        }.padding()
            .onAppear{
                self.viewModel.refresh()
        }
    }
}

#if DEBUG
struct UserDailyCalendarRowView_Previews: PreviewProvider {
    static let id = UUID()
    static let vm = UserDailyCalendarViewModel(day: Date(), logs: [ActivityLog(activityId: id, time: 0.5, date: Date())], activities: [Activity(id: id, name: "Testons")])
    static var previews: some View {
        UserDailyCalendarRowView(viewModel: vm)
    }
}
#endif
