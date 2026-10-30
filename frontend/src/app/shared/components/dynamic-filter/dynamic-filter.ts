import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, input, output, signal } from '@angular/core';
import { FormControl, FormRecord, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { fieldErrorMessage } from '../../forms/form-errors';
import { FilterField, FilterValues } from './dynamic-filter.models';

/**
 * Panel de filtros dinámicos: el usuario agrega los filtros que necesita desde un menú,
 * los aplica, y puede quitarlos individualmente (desde el formulario o desde los chips).
 */
@Component({
  selector: 'app-dynamic-filter',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatButtonModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './dynamic-filter.html',
  styleUrl: './dynamic-filter.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicFilter implements OnInit {
  readonly fields = input.required<FilterField[]>();
  /** Filtros con los que inicia el panel (por ejemplo, desde parámetros de la URL). */
  readonly initialValues = input<FilterValues>({});
  readonly filtersChange = output<FilterValues>();

  protected readonly form = new FormRecord<FormControl<unknown>>({});
  protected readonly activeKeys = signal<string[]>([]);
  protected readonly applied = signal<FilterValues>({});

  protected readonly activeFields = computed(() =>
    this.activeKeys()
      .map((key) => this.fields().find((field) => field.key === key))
      .filter((field): field is FilterField => !!field),
  );
  protected readonly availableFields = computed(() =>
    this.fields().filter((field) => !this.activeKeys().includes(field.key)),
  );
  protected readonly appliedEntries = computed(() =>
    Object.entries(this.applied()).map(([key, value]) => ({
      key,
      field: this.fields().find((field) => field.key === key),
      value,
    })),
  );

  protected readonly fieldErrorMessage = fieldErrorMessage;

  ngOnInit(): void {
    const initial = this.initialValues();
    for (const [key, value] of Object.entries(initial)) {
      const field = this.fields().find((f) => f.key === key);
      if (field) this.addFilter(field, value);
    }
    this.applied.set({ ...initial });
  }

  protected addFilter(field: FilterField, value: unknown = null): void {
    const validators = field.type === 'number' && field.min !== undefined ? [Validators.min(field.min)] : [];
    this.form.addControl(field.key, new FormControl<unknown>(value, validators));
    this.activeKeys.update((keys) => [...keys, field.key]);
  }

  protected removeFilter(key: string): void {
    this.form.removeControl(key);
    this.activeKeys.update((keys) => keys.filter((k) => k !== key));

    if (key in this.applied()) this.apply();
  }

  protected apply(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values: FilterValues = {};
    for (const [key, value] of Object.entries(this.form.getRawValue())) {
      if (value === null || value === undefined) continue;
      if (typeof value === 'string' && value.trim() === '') continue;
      values[key] = typeof value === 'string' ? value.trim() : (value as number | Date);
    }

    this.applied.set(values);
    this.filtersChange.emit(values);
  }

  protected clear(): void {
    for (const key of this.activeKeys()) this.form.removeControl(key);
    this.activeKeys.set([]);
    this.applied.set({});
    this.filtersChange.emit({});
  }

  protected optionLabel(field: FilterField | undefined, value: unknown): string {
    return field?.options?.find((option) => option.value === value)?.label ?? String(value);
  }

  protected isDate(value: unknown): value is Date {
    return value instanceof Date;
  }
}
