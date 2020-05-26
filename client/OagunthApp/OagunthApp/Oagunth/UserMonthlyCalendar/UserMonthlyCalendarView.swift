//
//  UserMonthlyCalendarView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 23/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI
import Combine

struct UserMonthlyCalendarView : View {
    @ObservedObject var viewModel: UserMonthlyCalendarViewModel

    init(viewModel: UserMonthlyCalendarViewModel) {
        self.viewModel = viewModel
    }

    var body: some View {
        VStack(alignment: .center) {
            List(viewModel.dataSource){ vm in
                NavigationLink(destination: UserWeeklyCalendarDetailView(viewModel: vm)) {
                    UserWeeklyCalendarRowView(viewModel: vm)
                }
            }.padding()
        }
    }
}

#if DEBUG
struct UserMonthlyCalendarView_Previews: PreviewProvider {
    static let vm = UserMonthlyCalendarViewModel(api: FakeClient())
    static var previews: some View {
        return UserMonthlyCalendarView(viewModel: vm)
    }
}
#endif
