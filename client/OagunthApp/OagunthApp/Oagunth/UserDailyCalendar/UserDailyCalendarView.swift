//
//  UserDailyCalendarView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 24/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI

struct UserDailyCalendarView: View {
    @ObservedObject var viewModel : UserDailyCalendarViewModel
    @State private var showAddActivityPanel = false
    private let emptyString = ""
    @State private var selectedActivityToAdd : Activity? = Optional.none
    
    init(viewModel : UserDailyCalendarViewModel) {
        self.viewModel = viewModel
    }
    
    var body: some View {
        VStack{
            Text(self.viewModel.dateString)
                .font(.largeTitle)
            
            ScrollView{
                ForEach(viewModel.dataSource) { vm in
                    VStack {
                        ActivityLogView(viewModel: vm)
                        Button(action: { self.viewModel.removeLog(log: vm) } ){
                            Text("Remove")
                                .buttonStyle(NeumorphicButtonStyle(bgColor: .clear))
                        }
                    }
                }
            }.frame(width: UIScreen.main.bounds.width - 20,
                    height: UIScreen.main.bounds.height - 250)
            HStack{
                if (!self.viewModel.remainingActivities.isEmpty) {
                    Button(action:{self.showAddActivityPanel.toggle()}) {
                        Text("Add Activity")
                            .bold()
                            .sheet(isPresented: self.$showAddActivityPanel){
                                VStack{
                                    List(self.viewModel.remainingActivities, id:\Activity.id) { currentActivity in
                                        Text(currentActivity.name)
                                            .onTapGesture {
                                                self.viewModel.addActivity(activity: currentActivity)
                                                self.showAddActivityPanel.toggle()
                                        }
                                    }.frame(height: UIScreen.main.bounds.height * 0.4)
                                }
                                
                        }
                    }
                }
            }.buttonStyle(NeumorphicButtonStyle(bgColor: .clear))
        }.padding()
    }
    
}

#if DEBUG

func makeVm(_ date:Date) -> UserDailyCalendarViewModel {
    var logs = [ActivityLog]()
    var activities = [Activity]()
    let time = 0.5
    
    for i in 1...10 {
        let name = "Testons \(i)"
        let id = UUID()
        activities.append(Activity(id: id, name: name))
        logs.append(ActivityLog(activityId: id, time: time, date: date))
    }
    return UserDailyCalendarViewModel(day: date, logs: logs, activities: activities)
}

struct UserDailyCalendarView_Previews: PreviewProvider {
    static let id = UUID()
    static let vm = makeVm(Date())
    static var previews: some View {
        UserDailyCalendarView(viewModel: vm)
    }
}

#endif
