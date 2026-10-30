import { FormControl } from '@angular/forms';
import { AppValidators } from './app-validators';

describe('AppValidators', () => {
  it('notBlank rechaza vacíos y solo espacios', () => {
    expect(AppValidators.notBlank(new FormControl('   '))).toEqual({ required: true });
    expect(AppValidators.notBlank(new FormControl(''))).toEqual({ required: true });
    expect(AppValidators.notBlank(new FormControl('Mouse'))).toBeNull();
  });

  it('httpUrl acepta solo URLs http(s) y permite vacío', () => {
    expect(AppValidators.httpUrl(new FormControl('https://img.test/a.png'))).toBeNull();
    expect(AppValidators.httpUrl(new FormControl(''))).toBeNull();
    expect(AppValidators.httpUrl(new FormControl('ftp://img.test/a.png'))).toEqual({ url: true });
    expect(AppValidators.httpUrl(new FormControl('no es url'))).toEqual({ url: true });
  });

  it('integer rechaza decimales', () => {
    expect(AppValidators.integer(new FormControl(2.5))).toEqual({ integer: true });
    expect(AppValidators.integer(new FormControl(3))).toBeNull();
  });

  it('maxDecimals limita la cantidad de decimales', () => {
    const twoDecimals = AppValidators.maxDecimals(2);
    expect(twoDecimals(new FormControl(10.555))).toEqual({ maxDecimals: { decimals: 2 } });
    expect(twoDecimals(new FormControl(10.55))).toBeNull();
    expect(twoDecimals(new FormControl(10))).toBeNull();
  });
});
