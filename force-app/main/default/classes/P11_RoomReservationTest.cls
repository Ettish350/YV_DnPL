/**
 * P11 | A11-09, A11-15, V11-10 | Tests for P11_CancelRoomReservations and P11_CloseResourcePrivate.
 * Every reservation is linked to an order, so the no-order rules (A11-11, V11-06) do not apply here.
 */
@IsTest
private class P11_RoomReservationTest {
    // 09:00 to 10:00 UTC, 11:00 to 12:00 in Jerusalem
    private static final Datetime START_DT = Datetime.newInstanceGmt(Date.today().addDays(20), Time.newInstance(9, 0, 0, 0));
    private static final Datetime END_DT = START_DT.addHours(1);

    @TestSetup
    static void setup() {
        insert new List<B25__Reservation_Status__c>{
            new B25__Reservation_Status__c(Name = 'אושרה', B25__AllowDoubleBooking__c = false, B25__Is_Active__c = true),
            new B25__Reservation_Status__c(Name = 'בוטלה', B25__AllowDoubleBooking__c = true, B25__Is_Active__c = true),
            new B25__Reservation_Status__c(Name = 'מחכה לאישור', B25__AllowDoubleBooking__c = true, B25__Is_Active__c = true)
        };
        B25__Resource_Type__c rootType = new B25__Resource_Type__c(Name = 'P11 root type');
        insert rootType;
        B25__Resource_Type__c sharedType = new B25__Resource_Type__c(Name = 'P11 shared', B25__Parent__c = rootType.Id, B25__AllowDoubleBookings__c = true, Buffer_Before_Min__c = 0, Buffer_After_Min__c = 0);
        insert sharedType;
        B25__Resource_Type__c roomType = new B25__Resource_Type__c(Name = 'P11 room', B25__Parent__c = rootType.Id, B25__AllowDoubleBookings__c = false, Buffer_Before_Min__c = 0, Buffer_After_Min__c = 0);
        insert roomType;
        B25__Resource__c root = new B25__Resource__c(Name = 'P11 root', B25__Resource_Type__c = rootType.Id, B25__Is_Active__c = true);
        insert root;
        B25__Resource__c museum = new B25__Resource__c(Name = 'P11 museum', B25__Parent__c = root.Id, B25__Resource_Type__c = sharedType.Id, B25__Capacity__c = 100, B25__Is_Active__c = true);
        B25__Resource__c roomA = new B25__Resource__c(Name = 'P11 room A', B25__Parent__c = root.Id, B25__Resource_Type__c = roomType.Id, B25__Capacity__c = 30, B25__Is_Active__c = true);
        B25__Resource__c roomB = new B25__Resource__c(Name = 'P11 room B', B25__Parent__c = root.Id, B25__Resource_Type__c = roomType.Id, B25__Capacity__c = 30, B25__Is_Active__c = true);
        insert new List<B25__Resource__c>{ museum, roomA, roomB };
        B25__Resource_Type__c subType = new B25__Resource_Type__c(Name = 'P11 sub-room', B25__Parent__c = roomType.Id, B25__Rentable__c = true, Buffer_Before_Min__c = 0, Buffer_After_Min__c = 0);
        insert subType;
        B25__Resource__c split = new B25__Resource__c(Name = 'P11 split', B25__Parent__c = root.Id, B25__Resource_Type__c = roomType.Id, B25__Capacity__c = 60, B25__Is_Active__c = true, Is_Splittable__c = true);
        insert split;
        B25__Resource__c sub1 = new B25__Resource__c(Name = 'P11 sub 1', B25__Parent__c = split.Id, B25__Resource_Type__c = subType.Id, B25__Capacity__c = 30, B25__Is_Active__c = true);
        B25__Resource__c sub2 = new B25__Resource__c(Name = 'P11 sub 2', B25__Parent__c = split.Id, B25__Resource_Type__c = subType.Id, B25__Capacity__c = 30, B25__Is_Active__c = true);
        insert new List<B25__Resource__c>{ sub1, sub2 };
        List<B25__Availability__c> hours = new List<B25__Availability__c>();
        for (B25__Resource__c r : new List<B25__Resource__c>{ museum, roomA, roomB, split, sub1, sub2 }) {
            hours.add(new B25__Availability__c(
                B25__Resource2__c = r.Id,
                B25__Days__c = 'Sunday;Monday;Tuesday;Wednesday;Thursday;Friday;Saturday',
                B25__Start_Hour__c = 0, B25__Start_Minute__c = 0, B25__End_Hour__c = 24, B25__End_Minute__c = 0
            ));
        }
        insert hours;
        Opportunity order = new Opportunity(
            Name = 'P11 test order',
            StageName = 'New',
            CloseDate = Date.today().addDays(30),
            RecordTypeId = Schema.SObjectType.Opportunity.getRecordTypeInfosByDeveloperName().get('Order').getRecordTypeId()
        );
        insert order;
    }


    /** A user of the running user's profile; with the P11_Scheduling_Manager permission set when asked. */
    private static User testUser(Boolean scheduler) {
        User me = [SELECT ProfileId FROM User WHERE Id = :UserInfo.getUserId()];
        String key = String.valueOf(Crypto.getRandomInteger()).replace('-', '');
        User u = new User(
            ProfileId = me.ProfileId, LastName = 'P11 test', Alias = 'p11t', Email = 'p11test' + key + '@example.com',
            Username = 'p11test' + key + '@example.com.p11', TimeZoneSidKey = 'Asia/Jerusalem', LocaleSidKey = 'en_US',
            EmailEncodingKey = 'UTF-8', LanguageLocaleKey = 'en_US'
        );
        System.runAs(new User(Id = UserInfo.getUserId())) {
            insert u;
            if (scheduler) {
                insert new PermissionSetAssignment(
                    AssigneeId = u.Id,
                    PermissionSetId = [SELECT Id FROM PermissionSet WHERE Name = 'P11_Scheduling_Manager' LIMIT 1].Id
                );
            }
        }
        return u;
    }

    private static Opportunity order() {
        return [SELECT Id FROM Opportunity WHERE Name = 'P11 test order' LIMIT 1];
    }

    private static Id resourceId(String name) {
        return [SELECT Id FROM B25__Resource__c WHERE Name = :name LIMIT 1].Id;
    }

    private static Id statusId(String name) {
        return [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :name LIMIT 1].Id;
    }

    private static B25__Reservation__c reservation(Id resourceId, Id orderId, Id parentId, Decimal quantity) {
        return new B25__Reservation__c(
            B25__Resource__c = resourceId,
            B25__Opportunity__c = orderId,
            B25__Parent_Reservation__c = parentId,
            B25__Start__c = START_DT,
            B25__End__c = END_DT,
            B25__Quantity__c = quantity,
            B25__Status__c = statusId('אושרה')
        );
    }

    @IsTest
    static void cancellingReservationCancelsRoomsButNotGuides() {
        Opportunity o = order();
        B25__Reservation__c day = new B25__Reservation__c(B25__Opportunity__c = o.Id, B25__Start__c = START_DT, B25__End__c = END_DT, B25__Status__c = statusId('אושרה'));
        insert day;
        B25__Reservation__c roomA = reservation(resourceId('P11 room A'), null, day.Id, 10);
        B25__Reservation__c roomB = reservation(resourceId('P11 room B'), null, day.Id, 10);
        insert new List<B25__Reservation__c>{ roomA, roomB };
        insert new Reservation_Staff__c(Reservation__c = roomB.Id);

        Test.startTest();
        P11_CancelRoomReservations.Request request = new P11_CancelRoomReservations.Request();
        request.reservationId = day.Id;
        request.cancelReason = 'אחר';
        P11_CancelRoomReservations.cancel(new List<P11_CancelRoomReservations.Request>{ request });
        Test.stopTest();

        B25__Reservation__c a = [SELECT B25__Status__r.Name, Cancel_Reason__c FROM B25__Reservation__c WHERE Id = :roomA.Id];
        B25__Reservation__c b = [SELECT B25__Status__r.Name FROM B25__Reservation__c WHERE Id = :roomB.Id];
        Assert.areEqual('בוטלה', a.B25__Status__r.Name, 'room reservation is cancelled');
        Assert.areEqual('אחר', a.Cancel_Reason__c, 'the reason is copied');
        Assert.areNotEqual('בוטלה', b.B25__Status__r.Name, 'a reservation with a guide line stays');
    }

    @IsTest
    static void cancellingOrderCancelsItsRooms() {
        Opportunity o = order();
        B25__Reservation__c room = reservation(resourceId('P11 room A'), o.Id, null, 10);
        insert room;

        Test.startTest();
        P11_CancelRoomReservations.Request request = new P11_CancelRoomReservations.Request();
        request.opportunityId = o.Id;
        request.cancelReason = 'מזג אוויר';
        P11_CancelRoomReservations.cancel(new List<P11_CancelRoomReservations.Request>{ request });
        Test.stopTest();

        B25__Reservation__c r = [SELECT B25__Status__r.Name, Cancel_Reason__c FROM B25__Reservation__c WHERE Id = :room.Id];
        Assert.areEqual('בוטלה', r.B25__Status__r.Name);
        Assert.areEqual('מזג אוויר', r.Cancel_Reason__c);
    }

    @IsTest
    static void emptyRequestDoesNothing() {
        P11_CancelRoomReservations.cancel(new List<P11_CancelRoomReservations.Request>{ new P11_CancelRoomReservations.Request() });
        Assert.areEqual(0, [SELECT COUNT() FROM B25__Reservation__c WHERE B25__Status__r.Name = 'בוטלה']);
    }

    @IsTest
    static void closeWithoutPermissionIsRefused() {
        B25__Reservation__c source = reservation(resourceId('P11 museum'), order().Id, null, 40);
        insert source;
        P11_CloseResourcePrivate.Request request = new P11_CloseResourcePrivate.Request();
        request.reservationId = source.Id;
        List<P11_CloseResourcePrivate.Result> results;
        Test.startTest();
        System.runAs(testUser(false)) {
            results = P11_CloseResourcePrivate.close(new List<P11_CloseResourcePrivate.Request>{ request });
        }
        Test.stopTest();
        Assert.isFalse(results[0].closed);
        Assert.areEqual(P11_CloseResourcePrivate.NO_PERMISSION_MESSAGE, results[0].message);
    }

    @IsTest
    static void closeFillsTheFreePlaces() {
        User me = testUser(true);
        Id museumId = resourceId('P11 museum');
        Id orderId = order().Id;
        B25__Reservation__c source = reservation(museumId, orderId, null, 40);
        B25__Reservation__c other = reservation(museumId, orderId, null, 10);
        insert new List<B25__Reservation__c>{ source, other };
        B25__Reservation__c room = reservation(resourceId('P11 room A'), orderId, null, 10);
        insert room;

        List<P11_CloseResourcePrivate.Result> results;
        Test.startTest();
        System.runAs(me) {
            P11_CloseResourcePrivate.Request museumRequest = new P11_CloseResourcePrivate.Request();
            museumRequest.reservationId = source.Id;
            P11_CloseResourcePrivate.Request roomRequest = new P11_CloseResourcePrivate.Request();
            roomRequest.reservationId = room.Id;
            results = P11_CloseResourcePrivate.close(new List<P11_CloseResourcePrivate.Request>{ museumRequest, roomRequest });
        }
        Test.stopTest();

        Assert.isTrue(results[0].closed, results[0].message);
        B25__Reservation__c closing = [SELECT B25__Quantity__c, Booking_Kind__c, B25__Parent_Reservation__c FROM B25__Reservation__c WHERE Id = :results[0].closingReservationId];
        Assert.areEqual(50, closing.B25__Quantity__c, 'capacity 100 minus 50 booked');
        Assert.areEqual(P11_CloseResourcePrivate.PRIVATE_CLOSE_KIND, closing.Booking_Kind__c);
        Assert.areEqual(source.Id, closing.B25__Parent_Reservation__c);
        Assert.isTrue(results[1].closed, 'an exclusive room is already held in full');
        Assert.isNull(results[1].closingReservationId);
    }

    @IsTest
    static void closeFillsEachPieceOfTheRange() {
        User me = testUser(true);
        Id museumId = resourceId('P11 museum');
        Id orderId = order().Id;
        B25__Reservation__c source = reservation(museumId, orderId, null, 40);
        B25__Reservation__c later = reservation(museumId, orderId, null, 10);
        later.B25__Start__c = START_DT.addMinutes(30);
        insert new List<B25__Reservation__c>{ source, later };

        List<P11_CloseResourcePrivate.Result> results;
        Test.startTest();
        System.runAs(me) {
            P11_CloseResourcePrivate.Request request = new P11_CloseResourcePrivate.Request();
            request.reservationId = source.Id;
            results = P11_CloseResourcePrivate.close(new List<P11_CloseResourcePrivate.Request>{ request });
        }
        Test.stopTest();

        Assert.isTrue(results[0].closed, results[0].message);
        List<B25__Reservation__c> closings = [
            SELECT B25__Start__c, B25__Quantity__c FROM B25__Reservation__c
            WHERE B25__Parent_Reservation__c = :source.Id ORDER BY B25__Start__c
        ];
        Assert.areEqual(2, closings.size(), 'one closing reservation per piece of the range');
        Assert.areEqual(60, closings[0].B25__Quantity__c, 'first half: 100 minus 40');
        Assert.areEqual(50, closings[1].B25__Quantity__c, 'second half: 100 minus 50');
    }

    @IsTest
    static void splittableRoomBooksItsSubRooms() {
        Id orderId = order().Id;
        B25__Reservation__c booking = reservation(resourceId('P11 split'), orderId, null, 20);
        Test.startTest();
        insert booking;
        List<B25__Reservation__c> copies = [SELECT Id, B25__Resource__r.Name, B25__Start__c, B25__Status__r.Name FROM B25__Reservation__c WHERE Split_Parent__c = :booking.Id];
        Assert.areEqual(2, copies.size(), 'one parallel reservation per sub-room');

        booking.B25__Start__c = START_DT.addMinutes(15);
        update booking;
        for (B25__Reservation__c c : [SELECT B25__Start__c FROM B25__Reservation__c WHERE Split_Parent__c = :booking.Id]) {
            Assert.areEqual(START_DT.addMinutes(15), c.B25__Start__c, 'copies follow the new start');
        }

        update new B25__Reservation__c(Id = booking.Id, B25__Status__c = statusId('בוטלה'), Cancel_Reason__c = 'אחר');
        Test.stopTest();
        for (B25__Reservation__c c : [SELECT B25__Status__r.Name, Cancel_Reason__c FROM B25__Reservation__c WHERE Split_Parent__c = :booking.Id]) {
            Assert.areEqual('בוטלה', c.B25__Status__r.Name, 'copies are cancelled with the room');
            Assert.areEqual('אחר', c.Cancel_Reason__c);
        }
    }

    @IsTest
    static void splittableRoomIsRefusedWhenASubRoomCannotBeBooked() {
        // GoMeddo does not check conflicts in unit tests, so the sub-room copy is made to fail on V11-08 instead
        update new B25__Resource__c(Id = resourceId('P11 sub 2'), Is_Two_Mode__c = true);
        Test.startTest();
        Database.SaveResult saved = Database.insert(reservation(resourceId('P11 split'), order().Id, null, 20), false);
        Test.stopTest();
        Assert.isFalse(saved.isSuccess(), 'the room is not booked when a sub-room copy cannot be saved');
        Assert.isTrue(saved.getErrors()[0].getMessage().contains('חדרי המשנה'), saved.getErrors()[0].getMessage());
        Assert.areEqual(0, [SELECT COUNT() FROM B25__Reservation__c WHERE B25__Resource__r.Name = 'P11 sub 1'], 'nothing is left behind');
    }

}