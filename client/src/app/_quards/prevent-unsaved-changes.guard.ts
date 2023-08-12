import {CanDeactivateFn } from '@angular/router';
import { MemberEditComponent } from '../members/member-edit/member-edit.component';

export const preventUnsavedChangesGuard: CanDeactivateFn<MemberEditComponent> = (component) => {
  if(component.editForm?.dirty){
    return confirm('Хотите продолжить? Все несохраненные данные будут утеряны')
  }
  
  return true;
};
