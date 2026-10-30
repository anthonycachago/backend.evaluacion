import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { DynamicFilter } from './dynamic-filter';
import { FilterField, FilterValues } from './dynamic-filter.models';

const FIELDS: FilterField[] = [
  { key: 'search', label: 'Nombre', type: 'text' },
  { key: 'minPrice', label: 'Precio mínimo', type: 'number', min: 0 },
];

/** Acceso a los miembros protegidos del componente para las pruebas. */
type DynamicFilterApi = {
  addFilter(field: FilterField, value?: unknown): void;
  removeFilter(key: string): void;
  apply(): void;
  clear(): void;
  form: { get(key: string): { setValue(value: unknown): void } | null };
};

describe('DynamicFilter', () => {
  let fixture: ComponentFixture<DynamicFilter>;
  let api: DynamicFilterApi;
  let emitted: FilterValues[];

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [DynamicFilter], providers: [provideNativeDateAdapter()] });
    fixture = TestBed.createComponent(DynamicFilter);
    fixture.componentRef.setInput('fields', FIELDS);
    fixture.detectChanges();

    api = fixture.componentInstance as unknown as DynamicFilterApi;
    emitted = [];
    fixture.componentInstance.filtersChange.subscribe((values) => emitted.push(values));
  });

  it('emite solo los filtros con valor', () => {
    api.addFilter(FIELDS[0], '  mouse ');
    api.addFilter(FIELDS[1], null);
    api.apply();

    expect(emitted.at(-1)).toEqual({ search: 'mouse' });
  });

  it('no aplica si un filtro es inválido', () => {
    api.addFilter(FIELDS[1], -5);
    api.apply();

    expect(emitted).toEqual([]);
  });

  it('al quitar un filtro aplicado vuelve a emitir sin él', () => {
    api.addFilter(FIELDS[0], 'mouse');
    api.addFilter(FIELDS[1], 10);
    api.apply();
    api.removeFilter('minPrice');

    expect(emitted.at(-1)).toEqual({ search: 'mouse' });
  });

  it('limpiar quita todos los filtros', () => {
    api.addFilter(FIELDS[0], 'mouse');
    api.apply();
    api.clear();

    expect(emitted.at(-1)).toEqual({});
  });
});
