import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Reminder, ReminderPriority } from '../../../../core/models/reminder.models';
import { ModalComponent } from '../../../../shared/components/ui/modal/modal.component';
import { parseUtcDate } from '../../../../core/utils/date.utils';

@Component({
  selector: 'app-reminder-form-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './reminder-form-modal.component.html',
  styleUrl: './reminder-form-modal.component.css'
})
export class ReminderFormModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() isOpen = false;
  @Input() reminderToEdit: Reminder | null = null;
  @Input() isSaving = false;

  @Output() save = new EventEmitter<{ isEdit: boolean; id?: number; data: any }>();
  @Output() cancel = new EventEmitter<void>();

  readonly form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    dueDate: ['', [Validators.required]],
    priority: [ReminderPriority.Medium, [Validators.required]],
    remindBeforeMinutes: [15, [Validators.required, Validators.min(0), Validators.max(10080)]],
    webhookUrl: [''],
    isPublic: [false],
    isCompleted: [false]
  });

  get isEditMode(): boolean {
    return !!this.reminderToEdit;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      if (this.reminderToEdit) {
        const localDateTime = this.formatDateForInput(this.reminderToEdit.dueDate);
        this.form.patchValue({
          title: this.reminderToEdit.title,
          description: this.reminderToEdit.description || '',
          dueDate: localDateTime,
          priority: this.reminderToEdit.priority,
          remindBeforeMinutes: this.reminderToEdit.remindBeforeMinutes ?? 15,
          webhookUrl: this.reminderToEdit.webhookUrl || '',
          isPublic: this.reminderToEdit.isPublic,
          isCompleted: this.reminderToEdit.isCompleted
        });
      } else {
        const pad = (n: number) => (n < 10 ? '0' + n : n);
        const defaultDate = new Date();
        defaultDate.setDate(defaultDate.getDate() + 1);
        const y = defaultDate.getFullYear();
        const m = pad(defaultDate.getMonth() + 1);
        const d = pad(defaultDate.getDate());
        const defaultLocal = `${y}-${m}-${d}T10:00`;

        this.form.reset({
          title: '',
          description: '',
          dueDate: defaultLocal,
          priority: ReminderPriority.Medium,
          remindBeforeMinutes: 15,
          webhookUrl: '',
          isPublic: false,
          isCompleted: false
        });
      }
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formVal = this.form.value;
    const isoDueDate = new Date(formVal.dueDate).toISOString();

    const data = {
      title: formVal.title.trim(),
      description: formVal.description ? formVal.description.trim() : null,
      dueDate: isoDueDate,
      priority: Number(formVal.priority),
      remindBeforeMinutes: Number(formVal.remindBeforeMinutes),
      webhookUrl: formVal.webhookUrl ? formVal.webhookUrl.trim() : null,
      isPublic: !!formVal.isPublic,
      ...(this.isEditMode ? { isCompleted: !!formVal.isCompleted } : {})
    };

    this.save.emit({
      isEdit: this.isEditMode,
      id: this.reminderToEdit?.id,
      data
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }

  private formatDateForInput(isoDateString: string): string {
    const date = parseUtcDate(isoDateString) ?? new Date(isoDateString);
    const pad = (n: number) => (n < 10 ? '0' + n : n);
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }
}
