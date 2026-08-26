export class CatalogError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class CatalogValidationError extends CatalogError {}

export class CatalogApiError extends CatalogError {}
