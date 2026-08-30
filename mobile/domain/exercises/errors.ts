export class ExerciseError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class ExerciseStructureError extends ExerciseError {
  constructor(message = 'This exercise is not valid for its type.') {
    super(message);
  }
}

export class ExerciseAttemptError extends ExerciseError {
  constructor(message = 'That answer does not match this exercise type.') {
    super(message);
  }
}
