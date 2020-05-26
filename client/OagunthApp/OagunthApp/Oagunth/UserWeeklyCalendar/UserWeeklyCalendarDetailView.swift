//
//  UserWeeklyCalendarView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 23/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI


//Code from https://sarunw.com/posts/swiftui-buttonstyle/
//Thanks !
struct NeumorphicButtonStyle: ButtonStyle {
    var bgColor: Color
    
    func makeBody(configuration: Self.Configuration) -> some View {
        configuration.label
            .padding(20)
            .background(
                ZStack {
                    RoundedRectangle(cornerRadius: 10, style: .continuous)
                        .shadow(color: .white, radius: configuration.isPressed ? 7: 10, x: configuration.isPressed ? -5: -15, y: configuration.isPressed ? -5: -15)
                        .shadow(color: .black, radius: configuration.isPressed ? 7: 10, x: configuration.isPressed ? 5: 15, y: configuration.isPressed ? 5: 15)
                        .blendMode(.overlay)
                    RoundedRectangle(cornerRadius: 10, style: .continuous)
                        .fill(bgColor)
                }
        )
            .scaleEffect(configuration.isPressed ? 0.95: 1)
            .foregroundColor(.primary)
            .animation(.spring())
    }
}

struct UserWeeklyCalendarDetailView: View {
    @ObservedObject var viewModel : UserWeeklyCalendarViewModel
    
    init(viewModel: UserWeeklyCalendarViewModel) {
        self.viewModel = viewModel
    }
    
    func makeButton(rule:WeekAcationRules) -> some View {
        switch rule {
        case .canBeSaved :
            let e = Button(action: {self.viewModel.save()}) {
                Text("Save")
                    .fontWeight(.bold)
                    .font(.largeTitle)
            }.buttonStyle(NeumorphicButtonStyle(bgColor: .clear))
            return AnyView(e)
        case .canBeSubmitted :
            let e = Button(action: {self.viewModel.submit()}) {
                Text("Submit")
                    .fontWeight(.bold)
                    .font(.largeTitle)
            }.buttonStyle(NeumorphicButtonStyle(bgColor: .clear))
            return AnyView(e)
        case .isLocked :
            return AnyView(Text("No more to do here ..."))
        case .noActionToTake:
            return AnyView(Text("Let's CRA together!"))
        }
    }
    
    var body: some View {
        VStack {
            makeButton(rule: self.viewModel.timeTrackingState)
            
            List(viewModel.dataSource) { vm in
                if vm.isActive {
                    NavigationLink(destination: UserDailyCalendarView(viewModel: vm)) {
                        UserDailyCalendarRowView(viewModel: vm)
                    }
                } else {
                    UserDailyCalendarRowView(viewModel: vm)
                }
                
            }.padding()
        }.onAppear{
            self.viewModel.refresh()
        }
    }
}
#if DEBUG
struct UserWeeklyCalendarDetailView_Previews: PreviewProvider {
    static let wc = WeeklyCalendar(weekNumber: 18, weekYear: 2020, days: [Date()])
    static let wcws = WeeklyCalendarWithStatus(calendar: wc, status: .New)
    static let vm = UserWeeklyCalendarViewModel(forWeek: wcws, with:
        [ActivityLog(activityId: UUID(), time: 1.0, date: Date())] , and: [Activity(id: UUID(), name: "Testons")], api: FakeClient())
    static var previews: some View {
        UserWeeklyCalendarDetailView(viewModel: vm)
    }
}
#endif
