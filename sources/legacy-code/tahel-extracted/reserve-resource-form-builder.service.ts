import { Injectable } from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { FullEventItem } from 'src/app/events/models/event-item.model';
import { FullReservationItem } from '../models/full-reservation-item.model';
import { FormBuilderHelperService } from './form-builder-helper.service';
import { FullReservationsService } from './full-reservations.service';
import { ScheduleEventItem } from 'src/app/schedule/models/schedule-event-item.model';

@Injectable({
  providedIn: 'root'
})
export class ReserveResourceFormBuilderService {

  public reservationForm: FormGroup;
  public eventForm: FormGroup;
  private reservationItem: FullReservationItem;
  private eventItem: FullEventItem;

  constructor(private formBuilder: FormBuilder,
    private formBuilderHelper: FormBuilderHelperService,
    private fullReservationsService: FullReservationsService) { }


  public buildReserveResourceForm(reserveResource: ScheduleEventItem) {
    const grandparentForm = this.reservationForm ? this.reservationForm : this.eventForm;

    const ReserveResourceForm = this.formBuilder.group({
      id: [reserveResource.id],
      start: [reserveResource.start],
      end: [reserveResource.end],
      activityDayLookup: [reserveResource.activityDayLookup],
      isResource: [reserveResource.isResource],
      isNeedRefreshments: [reserveResource.isNeedRefreshments],
      isNeedModifyArrangement: [reserveResource.isNeedModifyArrangement],
      division: [reserveResource.division],
      numberOfVisitors: [reserveResource.numberOfVisitors],
      numberOfGroup: [reserveResource.numberOfGroup],
      eventStatus: [reserveResource.eventStatus],
      reservationColor: [reserveResource.reservationColor],
      eventType: [reserveResource.eventType],
      reservation: [reserveResource.reservation],
      eventReservation: [reserveResource.eventReservation],
      paymentRate: [reserveResource.paymentRate],
      unitPrice: [reserveResource.unitPrice],
      priceAfterDiscount: [reserveResource.priceAfterDiscount],
      currency: [reserveResource.currency],
      shortDescription:[reserveResource.shortDescription],
      description:[reserveResource.description],
      comments:[reserveResource.comments],
      scheduleItem:[reserveResource.scheduleItem],
      isDirty: [false]
    });

    //learningDivisionForm.updateValueAndValidity();

    this.formBuilderHelper.markAsDirty(ReserveResourceForm, grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'isResource', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'isNeedRefreshments', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'isNeedModifyArrangement', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'division', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'numberOfVisitors', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'numberOfGroup', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'eventStatus', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'reservationColor', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'eventType', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'reservation', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'eventReservation', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'scheduleItem', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'comments', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'description', grandparentForm);
    this.formBuilderHelper.registerToChangesSetDirty(ReserveResourceForm, 'shortDescription', grandparentForm);
    return ReserveResourceForm;
  }

  public buildNewReserveResourceForm(activityDayForm: FormGroup) {

    const newReserve = this.fullReservationsService.defaultScheduleEventItem;
    const reserveResourceForm = this.buildReserveResourceForm(newReserve);
    return reserveResourceForm;
  }

  public buildReserveResourcesListForm(reserveResourcesList: ScheduleEventItem[], activityDayForm: FormGroup) {
    const reserveResourcesListForm = activityDayForm.get('reserveResourcesList') as FormArray;
    if (reserveResourcesList) {
      reserveResourcesList.forEach(reserveResource => {
        reserveResourcesListForm.push(this.buildReserveResourceForm(reserveResource));
      });
    }
  }

}
