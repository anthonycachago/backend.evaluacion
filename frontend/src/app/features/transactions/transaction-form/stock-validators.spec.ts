import { FormControl, FormGroup } from '@angular/forms';
import { TransactionType } from '../../../core/models/transaction.models';
import { saleWithinStockValidator, selectedOptionValidator } from './stock-validators';

function buildForm(type: TransactionType, quantity: number | null, available: number | null) {
  const form = new FormGroup({
    type: new FormControl<TransactionType>(type),
    quantity: new FormControl<number | null>(quantity, [saleWithinStockValidator(() => available)]),
  });
  form.controls.quantity.updateValueAndValidity();
  return form;
}

describe('saleWithinStockValidator', () => {
  it('rechaza una venta mayor al stock disponible', () => {
    const form = buildForm('Sale', 6, 5);

    expect(form.controls.quantity.errors).toEqual({ insufficientStock: { available: 5 } });
  });

  it('acepta una venta igual al stock disponible', () => {
    expect(buildForm('Sale', 5, 5).controls.quantity.valid).toBe(true);
  });

  it('no limita las compras', () => {
    expect(buildForm('Purchase', 1000, 5).controls.quantity.valid).toBe(true);
  });

  it('no valida mientras no haya producto seleccionado', () => {
    expect(buildForm('Sale', 10, null).controls.quantity.valid).toBe(true);
  });
});

describe('selectedOptionValidator', () => {
  it('exige elegir un producto de la lista y no solo escribir texto', () => {
    expect(selectedOptionValidator(new FormControl('mouse'))).toEqual({ productNotSelected: true });
    expect(selectedOptionValidator(new FormControl(null))).toEqual({ required: true });
    expect(selectedOptionValidator(new FormControl({ id: '1', name: 'Mouse' }))).toBeNull();
  });
});
